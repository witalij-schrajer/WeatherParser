using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Forecasts;

/// <summary>
/// Represents the input for building a forecast run.
/// </summary>
public sealed class ForecastRunInput
{
    /// <summary>
    /// Gets or sets the product of the forecast run.
    /// </summary>
    public MosmixProduct? Product { get; init; }

    /// <summary>
    /// Gets or sets the MOSMIX ID of the forecast run.
    /// </summary>
    public string? MosmixId { get; init; }

    /// <summary>
    /// Gets or sets the location of the forecast run.
    /// </summary>
    public ForecastLocation? Location { get; init; }

    /// <summary>
    /// Gets or sets the UTC time when the forecast was issued.
    /// </summary>
    public DateTime? IssuedAtUtc { get; init; }

    /// <summary>
    /// Gets or sets the UTC time when the data was retrieved.
    /// </summary>
    public DateTime RetrievedAtUtc { get; init; }

    /// <summary>
    /// Gets or sets the timeline of the forecast run.
    /// </summary>
    public IReadOnlyList<DateTime>? Timeline { get; init; }

    /// <summary>
    /// Gets or sets the series of the forecast run.
    /// </summary>
    public IReadOnlyList<ForecastSeries?>? Series { get; init; }
}
