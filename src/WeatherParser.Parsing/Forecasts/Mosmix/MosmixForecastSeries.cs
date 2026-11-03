namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// A series of values for one element in a MOSMIX forecast.
/// </summary>
public sealed class MosmixForecastSeries
{
    /// <summary>
    /// Gets or sets the name of the element.
    /// </summary>
    public required string ElementName { get; init; }

    /// <summary>
    /// Gets or sets the values for each time step.
    /// </summary>
    public required IReadOnlyList<string?> Values { get; init; }
}
