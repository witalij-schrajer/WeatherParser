namespace WeatherParser.Coverage;

/// <summary>
/// Evaluates coverage rules against current and baseline block counters.
/// </summary>
public interface IRuleEvaluator
{
    /// <summary>
    /// Evaluates a set of rules based on the current coverage, baseline, PR category, and bootstrap status.
    /// </summary>
    /// <param name="current">Current aggregated block counters.</param>
    /// <param name="baseline">Baseline block counters or null if no baseline exists.</param>
    /// <param name="category">Pull request category (DocumentationOnly or Code).</param>
    /// <param name="isInitialBootstrap">True if this is the initial bootstrap run with no baseline yet.</param>
    /// <returns>List of rule results in order of evaluation.</returns>
    IReadOnlyList<RuleResult> Evaluate(
        BlockCounters current,
        BlockCounters? baseline,
        PullRequestCategory category,
        bool isInitialBootstrap);
}
