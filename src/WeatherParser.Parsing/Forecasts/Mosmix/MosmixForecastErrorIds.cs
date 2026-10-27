namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Stable error IDs of the MOSMIX forecast parsers (contract MF-14).
/// </summary>
public static class MosmixForecastErrorIds
{
    /// <summary>
    /// The file violates the structure of contract MF-07.
    /// </summary>
    public const string FormatInvalid = "MOSMIX-FORMAT-INVALID";

    /// <summary>
    /// The file has no dwd:IssueTime.
    /// </summary>
    public const string IssueTimeMissing = "MOSMIX-ISSUE-TIME-MISSING";

    /// <summary>
    /// The MOSMIX_L file belongs to another location.
    /// </summary>
    public const string LocationMismatch = "MOSMIX-LOCATION-MISMATCH";

    /// <summary>
    /// The requested location is not published.
    /// </summary>
    public const string IdUnknown = "MOSMIX-ID-UNKNOWN";

    /// <summary>
    /// The archive or its member exceeds its size bound.
    /// </summary>
    public const string TooLarge = "MOSMIX-TOO-LARGE";
}
