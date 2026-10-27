namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// The validated raw content of one location of a MOSMIX forecast file: issue time, timeline, location and the raw token series per element; units and periods are not interpreted (contracts MF-07, MF-08).
/// </summary>
public sealed class MosmixForecastDocument
{
    /// <summary>
    /// Gets or sets the MOSMIX ID.
    /// </summary>
    public required string MosmixId { get; init; }

    /// <summary>
    /// Gets or sets the issue time in UTC.
    /// </summary>
    public required DateTime IssuedAtUtc { get; init; }

    /// <summary>
    /// Gets or sets the timeline.
    /// </summary>
    public required IReadOnlyList<DateTime> Timeline { get; init; }

    /// <summary>
    /// Gets or sets the location name.
    /// </summary>
    public required string LocationName { get; init; }

    /// <summary>
    /// Gets or sets the latitude in decimal degrees.
    /// </summary>
    public required decimal Latitude { get; init; }

    /// <summary>
    /// Gets or sets the longitude in decimal degrees.
    /// </summary>
    public required decimal Longitude { get; init; }

    /// <summary>
    /// Gets or sets the series of values for each element.
    /// </summary>
    public required IReadOnlyList<MosmixForecastSeries> Series { get; init; }

    /// <summary>
    /// Gets or sets the altitude in meters.
    /// </summary>
    public decimal? AltitudeM { get; init; }
}
