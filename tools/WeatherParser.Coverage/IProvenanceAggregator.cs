namespace WeatherParser.Coverage;

/// <summary>
/// Aggregates block counters of production modules after applying exclusions.
/// </summary>
public interface IProvenanceAggregator
{
    /// <summary>
    /// Aggregates the block counters from all modules in the report, excluding those matching any exclusion pattern.
    /// </summary>
    /// <param name="report">The coverage report to aggregate.</param>
    /// <param name="exclusions">List of exclusion patterns to filter out modules.</param>
    /// <returns>The aggregated block counters for production modules.</returns>
    BlockCounters Aggregate(CoverageReport report, IReadOnlyList<string> exclusions);
}
