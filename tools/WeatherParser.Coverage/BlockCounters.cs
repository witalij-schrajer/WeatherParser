namespace WeatherParser.Coverage;

/// <summary>
/// Native Microsoft.CodeCoverage block counters.
/// </summary>
/// <param name="Covered">Number of blocks covered.</param>
/// <param name="NotCovered">Number of blocks not covered.</param>
public sealed record BlockCounters(long Covered, long NotCovered)
{
    /// <summary>
    /// Total number of blocks (covered + not covered).
    /// </summary>
    public long Total => Covered + NotCovered;
}
