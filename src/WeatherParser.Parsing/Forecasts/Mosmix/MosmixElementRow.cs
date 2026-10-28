namespace WeatherParser.Parsing.Forecasts.Mosmix;

using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;

/// <summary>
/// Represents a row in the MOSMIX element table (contracts MF-10, MF-11).
/// </summary>
internal sealed record MosmixElementRow(
    string ElementName,
    QuantityKind Kind,
    string SourceUnit,
    int? PeriodHours,
    PressureUnit? PressureUnit,
    PressureReference? PressureReference,
    WindSpeedUnit? WindSpeedUnit);
