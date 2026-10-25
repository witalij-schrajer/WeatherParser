namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Consumer-supplied location metadata of one OsthessenWetter station (contract OW-04, C-51).
/// </summary>
public sealed class OsthessenWetterStationMetadata
{
    /// <summary>
    /// The overview station name as shown on the page; matched exactly after Unicode normalization form C.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The latitude in decimal degrees, from -90 to 90 inclusive.
    /// </summary>
    public decimal Latitude { get; set; }

    /// <summary>
    /// The longitude in decimal degrees, from -180 to 180 inclusive.
    /// </summary>
    public decimal Longitude { get; set; }

    /// <summary>
    /// The altitude in metres, or null when unknown; never replaced by zero.
    /// </summary>
    public decimal? AltitudeM { get; set; }
}
