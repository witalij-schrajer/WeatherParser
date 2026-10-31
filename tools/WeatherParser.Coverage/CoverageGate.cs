namespace WeatherParser.Coverage;

/// <summary>
/// Orchestrates the coverage evaluation process.
/// </summary>
public sealed class CoverageGate : ICoverageGate
{
    private readonly ICoverageReportReader _reportReader;
    private readonly IBaselineMetadataReader _baselineReader;
    private readonly IChangedFileListReader _changesReader;
    private readonly IProvenanceAggregator _aggregator;
    private readonly IRuleEvaluator _evaluator;
    private readonly IBaselineComparabilityChecker _comparabilityChecker;
    private readonly IPullRequestClassifier _classifier;
    private readonly IReconciliationReader _reconciliationReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageGate"/> class.
    /// </summary>
    /// <param name="reportReader">The coverage report reader.</param>
    /// <param name="baselineReader">The baseline metadata reader.</param>
    /// <param name="changesReader">The changed file list reader.</param>
    /// <param name="aggregator">The provenance aggregator.</param>
    /// <param name="evaluator">The rule evaluator.</param>
    /// <param name="comparabilityChecker">The baseline comparability checker.</param>
    /// <param name="classifier">The pull request classifier.</param>
    /// <param name="reconciliationReader">The reader of recorded baseline reconciliations.</param>
    public CoverageGate(
        ICoverageReportReader reportReader,
        IBaselineMetadataReader baselineReader,
        IChangedFileListReader changesReader,
        IProvenanceAggregator aggregator,
        IRuleEvaluator evaluator,
        IBaselineComparabilityChecker comparabilityChecker,
        IPullRequestClassifier classifier,
        IReconciliationReader reconciliationReader)
    {
        _reportReader = reportReader ?? throw new ArgumentNullException(nameof(reportReader));
        _baselineReader = baselineReader ?? throw new ArgumentNullException(nameof(baselineReader));
        _changesReader = changesReader ?? throw new ArgumentNullException(nameof(changesReader));
        _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _comparabilityChecker = comparabilityChecker ?? throw new ArgumentNullException(nameof(comparabilityChecker));
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _reconciliationReader = reconciliationReader ?? throw new ArgumentNullException(nameof(reconciliationReader));
    }

    /// <inheritdoc/>
    public async Task<CoverageResult> EvaluateAsync(CoverageGateRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            // 1. Read coverage report
            var report = await _reportReader.ReadAsync(request.ReportPath, cancellationToken);

            // 2. Aggregate counters
            var aggregated = _aggregator.Aggregate(report, request.Exclusions);

            // 3. Read changed files and classify PR
            var changes = await _changesReader.ReadAsync(request.ChangedFilesPath, cancellationToken);
            var category = _classifier.Classify(changes);

            if (category == PullRequestCategory.Code
                && request.GatedInputs is { Count: > 0 } gatedInputs
                && !_classifier.TouchesGatedInputs(changes, gatedInputs))
            {
                // REQ-086 amendment (CL-19 option C): a code change outside every gated input may keep equal coverage.
                category = PullRequestCategory.GatedInputsUnchanged;
            }

            // 4. Read baseline; the reader returns null for a missing path or file and the rule evaluator decides what that means.
            var baseline = await _baselineReader.ReadAsync(request.BaselinePath, cancellationToken);
            RuleResult? comparabilityResult = null;

            if (baseline != null)
            {
                comparabilityResult = _comparabilityChecker.Check(
                    baseline,
                    request.ExpectedBaselineCommitId,
                    request.ToolName,
                    request.ToolVersion,
                    "native-xml",
                    request.Exclusions);
            }

            // 5. Read reconciliation entries
            IReadOnlyList<ReconciliationEntry> entries = string.IsNullOrEmpty(request.ReconciliationPath)
                ? Array.Empty<ReconciliationEntry>()
                : await _reconciliationReader.ReadAsync(request.ReconciliationPath, cancellationToken);

            // Apply reconciliation if all conditions are met
            RuleResult? reconciliationResult = null;
            if (category == PullRequestCategory.Code
                && baseline != null
                && comparabilityResult is { Verdict: true }
                && !string.IsNullOrEmpty(request.GateName)
                && !string.IsNullOrEmpty(request.HeadRepository)
                && !string.IsNullOrEmpty(request.HeadBranch))
            {
                // REQ-086 amendment 2: a user-approved, recorded baseline reconciliation for this gate, target, head repository and head branch.
                var entry = entries.FirstOrDefault(e =>
                    string.Equals(e.Gate, request.GateName, StringComparison.Ordinal)
                    && string.Equals(e.TargetCommit, request.ExpectedBaselineCommitId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(e.HeadRepository, request.HeadRepository, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(e.HeadBranch, request.HeadBranch, StringComparison.Ordinal));

                if (entry != null)
                {
                    category = PullRequestCategory.Reconciled;
                    reconciliationResult = new RuleResult(
                        "Reconciliation",
                        true,
                        $"Applied the recorded baseline reconciliation for pull request #{entry.PullRequest} (gate {entry.Gate}, target {entry.TargetCommit}, head {entry.HeadRepository}:{entry.HeadBranch}, approved on {entry.ApprovedOn}, record {entry.Record}): equal coverage is accepted, a decrease still fails.");
                }
            }

            // 6. Evaluate rules; an incomparable baseline is never used for comparison and is not bootstrap.
            var ruleResults = comparabilityResult is { Verdict: false }
                ? _evaluator.Evaluate(aggregated, null, category, false)
                : _evaluator.Evaluate(aggregated, baseline?.Blocks, category, request.IsInitialBootstrap);

            // 7. Build result
            var allRuleResults = new List<RuleResult>();
            if (comparabilityResult != null)
            {
                allRuleResults.Add(comparabilityResult);
            }

            if (reconciliationResult != null)
            {
                allRuleResults.Add(reconciliationResult);
            }

            allRuleResults.AddRange(ruleResults);

            var passed = allRuleResults.All(r => r.Verdict);

            return new CoverageResult(passed, aggregated, allRuleResults);
        }
        catch (CoverageInputException ex)
        {
            return new CoverageResult(false, null, [new RuleResult("Input", false, ex.Message)]);
        }
    }
}
