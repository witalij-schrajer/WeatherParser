namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// Rounds converted quantity values for output (REQ-057).
/// </summary>
public interface IOutputRounding
{
    /// <summary>
    /// Rounds an already converted value to the REQ-057 output increment of its kind; kinds without an increment are returned unchanged.
    /// </summary>
    /// <param name="kind">The kind of quantity to round.</param>
    /// <param name="value">The value to round.</param>
    /// <returns>The rounded value.</returns>
    decimal Round(QuantityKind kind, decimal value);
}
