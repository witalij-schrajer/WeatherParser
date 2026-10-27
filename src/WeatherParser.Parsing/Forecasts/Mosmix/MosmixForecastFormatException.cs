namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Represents a MOSMIX forecast file that the document reader rejects; the parsers map it to their product-specific failure (contracts MF-07, MF-14).
/// </summary>
public sealed class MosmixForecastFormatException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixForecastFormatException"/> class.
    /// </summary>
    /// <param name="errorId">The error ID.</param>
    /// <param name="message">The message that describes the error.</param>
    public MosmixForecastFormatException(string errorId, string message) : base(message)
    {
        ErrorId = errorId ?? throw new ArgumentNullException(nameof(errorId));
    }

    /// <summary>
    /// Gets the error ID.
    /// </summary>
    public string ErrorId { get; }
}
