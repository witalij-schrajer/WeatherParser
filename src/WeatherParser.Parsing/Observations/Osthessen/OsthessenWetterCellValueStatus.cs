namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// The outcome of reading one value cell (contract OW-11).
/// </summary>
public enum OsthessenWetterCellValueStatus
{
    /// <summary>
    /// The cell holds a number with the documented unit.
    /// </summary>
    Present,

    /// <summary>
    /// The cell is empty or absent.
    /// </summary>
    Missing,

    /// <summary>
    /// The cell holds text outside the number grammar or with another unit.
    /// </summary>
    Unreadable
}
