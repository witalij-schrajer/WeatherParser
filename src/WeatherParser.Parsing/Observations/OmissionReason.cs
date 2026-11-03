namespace WeatherParser.Parsing.Observations;

/// <summary>
/// Represents the reason why an observation reading was omitted.
/// </summary>
public enum OmissionReason
{
    /// <summary>
    /// The reading kind is not exportable.
    /// </summary>
    NotExportable,

    /// <summary>
    /// The reading value is missing.
    /// </summary>
    Missing,

    /// <summary>
    /// The reading value is not finite (NaN or infinity).
    /// </summary>
    NonFinite,

    /// <summary>
    /// The reading value is out of range.
    /// </summary>
    OutOfRange,

    /// <summary>
    /// The reading time is missing.
    /// </summary>
    MissingTime,

    /// <summary>
    /// The reading period is missing.
    /// </summary>
    MissingPeriod,

    /// <summary>
    /// The reading value cannot be represented in the output format.
    /// </summary>
    Unrepresentable
}
