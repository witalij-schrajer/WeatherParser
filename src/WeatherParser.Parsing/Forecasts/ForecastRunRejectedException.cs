namespace WeatherParser.Parsing.Forecasts;

/// <summary>
/// Represents an exception thrown when a forecast run cannot be built.
/// </summary>
public sealed class ForecastRunRejectedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForecastRunRejectedException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ForecastRunRejectedException(string message) : base(message)
    {
    }
}
