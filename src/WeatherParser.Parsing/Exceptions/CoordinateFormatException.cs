namespace WeatherParser.Parsing.Exceptions;

/// <summary>
/// Raised when a coordinate format is invalid (contract SC-07, SC-17).
/// </summary>
public sealed class CoordinateFormatException : Exception
{
    /// <summary>
    /// Error identifier for invalid coordinates.
    /// </summary>
    public const string CoordinateInvalidErrorId = "COORDINATE-INVALID";

    /// <summary>
    /// Gets the error identifier for this exception.
    /// </summary>
    public string ErrorId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoordinateFormatException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public CoordinateFormatException(string message) : base(message)
    {
        ErrorId = CoordinateInvalidErrorId;
    }
}
