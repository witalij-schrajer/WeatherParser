namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// The outcome of reading one displayed station time (contract OW-07).
/// </summary>
public enum OsthessenWetterTimeStatus
{
    /// <summary>
    /// The time was read and its date inferred.
    /// </summary>
    Resolved,

    /// <summary>
    /// The time text is empty, absent or outside the HH:mm Uhr grammar.
    /// </summary>
    Missing,

    /// <summary>
    /// The inferred local time falls into a daylight saving time gap or overlap in Europe/Berlin.
    /// </summary>
    Unresolved
}
