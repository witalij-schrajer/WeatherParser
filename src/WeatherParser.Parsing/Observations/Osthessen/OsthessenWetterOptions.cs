namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Consumer options of the OsthessenWetter parser (contract OW-04, OW-10).
/// </summary>
public sealed class OsthessenWetterOptions
{
    /// <summary>
    /// The station metadata entries; names are unique after Unicode normalization form C.
    /// </summary>
    public IList<OsthessenWetterStationMetadata> Stations { get; set; } = new List<OsthessenWetterStationMetadata>();

    /// <summary>
    /// The overall deadline of one parse, greater than zero and at most one hour; default 60 seconds (contract OW-10).
    /// </summary>
    public TimeSpan PageDeadline { get; set; } = TimeSpan.FromSeconds(60);
}
