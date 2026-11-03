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

    /// <summary>
    /// The MOSMIX ID is not valid (contract MF-04).
    /// </summary>
    public const string IdInvalid = "MOSMIX-ID-INVALID";

    /// <summary>
    /// The file could not be retrieved (contract MF-05, MF-07).
    /// </summary>
    public const string RetrievalFailed = "MOSMIX-RETRIEVAL-FAILED";

    /// <summary>
    /// The parse deadline expired (contract MF-05).
    /// </summary>
    public const string DeadlineExceeded = "MOSMIX-DEADLINE-EXCEEDED";

    /// <summary>
    /// The run has no usable value (contract MF-09).
    /// </summary>
    public const string NoUsableValue = "MOSMIX-NO-USABLE-VALUE";

    /// <summary>
    /// The consumer options are invalid (contract MF-04).
    /// </summary>
    public const string OptionsInvalid = "MOSMIX-OPTIONS-INVALID";
}
