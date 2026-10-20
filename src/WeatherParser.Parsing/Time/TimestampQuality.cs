namespace WeatherParser.Parsing.Time;

/// <summary>
/// Indicates the quality of a timestamp.
/// </summary>
public enum TimestampQuality
{
    /// <summary>
    /// A complete source timestamp.
    /// </summary>
    Source,

    /// <summary>
    /// Date inferred from the retrieval time, REQ-042.
    /// </summary>
    EstimatedDate
}
