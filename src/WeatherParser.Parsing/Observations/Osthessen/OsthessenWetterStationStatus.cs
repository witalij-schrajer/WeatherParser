namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// The status of a station as read from the page.
/// </summary>
public enum OsthessenWetterStationStatus
{
    /// <summary>
    /// The status image title is Station online.
    /// </summary>
    Online,

    /// <summary>
    /// The status image title is Station offline.
    /// </summary>
    Offline
}
