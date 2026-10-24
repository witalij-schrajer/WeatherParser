namespace WeatherParser.Parsing.Geography;

/// <summary>
/// Outcome of evaluating one candidate against the search area (REQ-022, REQ-102).
/// </summary>
public enum SearchAreaMatch
{
    /// <summary>
    /// Both inclusive limits hold.
    /// </summary>
    Inside,

    /// <summary>
    /// The distance limit or the altitude limit is exceeded.
    /// </summary>
    Outside,

    /// <summary>
    /// The candidate has no altitude and is never treated as altitude 0.
    /// </summary>
    NotFilterable
}
