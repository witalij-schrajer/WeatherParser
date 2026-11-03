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
        OmittedValues = Array.Empty<OmittedForecastValue>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForecastRunRejectedException"/> class with the values the builder omitted (contract MF-02).
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="omittedValues">The omitted values with their reasons.</param>
    public ForecastRunRejectedException(string message, IReadOnlyList<OmittedForecastValue> omittedValues) : base(message)
    {
        OmittedValues = omittedValues ?? throw new ArgumentNullException(nameof(omittedValues));
    }

    /// <summary>
    /// Gets the values the builder omitted with their reasons, so that the caller can log every omission before the rejection propagates (contract MF-02); empty when not supplied.
    /// </summary>
    public IReadOnlyList<OmittedForecastValue> OmittedValues { get; }
}
