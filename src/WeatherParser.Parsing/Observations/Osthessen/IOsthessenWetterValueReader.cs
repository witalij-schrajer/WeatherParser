namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Reads the number and checks the unit of one OsthessenWetter value cell (contract OW-06, OW-11, A7).
/// </summary>
public interface IOsthessenWetterValueReader
{
    /// <summary>
    /// Reads cell text such as 10.3 followed by the column unit; the number grammar is an optional minus sign, ASCII digits and an optional point with ASCII digits, parsed culture-invariantly; empty or null text is missing, any other deviation unreadable.
    /// </summary>
    /// <param name="cellText">The text in the cell.</param>
    /// <param name="column">The column that the cell belongs to.</param>
    /// <exception cref="ArgumentOutOfRangeException">When column is not a defined member.</exception>
    OsthessenWetterCellValue Read(string? cellText, OsthessenWetterValueColumn column);
}
