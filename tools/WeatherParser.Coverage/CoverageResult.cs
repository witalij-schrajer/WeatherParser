namespace WeatherParser.Coverage;

/// <summary>
/// Result of coverage evaluation.
/// </summary>
/// <param name="Passed">True if all rules passed, false otherwise.</param>
/// <param name="Current">Aggregated block counters for current run, or null if input error occurred.</param>
/// <param name="Rules">List of rule results in order of evaluation.</param>
public sealed record CoverageResult(bool Passed, BlockCounters? Current, IReadOnlyList<RuleResult> Rules);
