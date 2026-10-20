namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// Why a source value was rejected and must be omitted (contract C-42).
/// </summary>
public enum QuantityRejectionReason
{
    /// <summary>
    /// No value delivered.
    /// </summary>
    Missing,

    /// <summary>
    /// NaN or infinity.
    /// </summary>
    NonFinite,

    /// <summary>
    /// Finite but outside the valid range of its kind or of <see cref="decimal"/>.
    /// </summary>
    OutOfRange
}
