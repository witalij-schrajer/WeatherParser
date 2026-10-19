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
    public CoverageGate(
        ICoverageReportReader reportReader,
        IBaselineMetadataReader baselineReader,
        IChangedFileListReader changesReader,
        IProvenanceAggregator aggregator,
        IRuleEvaluator evaluator,
        IBaselineComparabilityChecker comparabilityChecker,
        IPullRequestClassifier classifier)
    {
        _reportReader = reportReader ?? throw new ArgumentNullException(nameof(reportReader));
        _baselineReader = baselineReader ?? throw new ArgumentNullException(nameof(baselineReader));
        _changesReader = changesReader ?? throw new ArgumentNullException(nameof(changesReader));
        _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _comparabilityChecker = comparabilityChecker ?? throw new ArgumentNullException(nameof(comparabilityChecker));
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
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

            // 5. Evaluate rules; an incomparable baseline is never used for comparison and is not bootstrap.
            var ruleResults = comparabilityResult is { Verdict: false }
                ? _evaluator.Evaluate(aggregated, null, category, false)
                : _evaluator.Evaluate(aggregated, baseline?.Blocks, category, request.IsInitialBootstrap);

            // 6. Build result
            IReadOnlyList<RuleResult> allRuleResults = comparabilityResult != null
                ? [comparabilityResult, .. ruleResults]
                : ruleResults;
            var passed = allRuleResults.All(r => r.Verdict);

            return new CoverageResult(passed, aggregated, allRuleResults);
        }
        catch (CoverageInputException ex)
        {
            return new CoverageResult(false, null, [new RuleResult("Input", false, ex.Message)]);
        }
    }
}
