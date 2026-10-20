namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// Both rounded pressure outputs derived from one unrounded original (contract C-40).
/// </summary>
/// <param name="Hpa">The rounded pressure in hectopascals.</param>
/// <param name="MmHg">The rounded pressure in millimetres of mercury.</param>
/// <param name="Reference">The unchanged reference level of the pressure value.</param>
/// <param name="OriginalUnit">The unit of the authoritative original value.</param>
public sealed record PressureValues(decimal Hpa, decimal MmHg, PressureReference Reference, PressureUnit OriginalUnit);