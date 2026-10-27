namespace WeatherParser.Parsing.Forecasts.Mosmix;

using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;

/// <summary>
/// Provides the MOSMIX element table (contracts MF-10, MF-11).
/// </summary>
internal static class MosmixElementTable
{
    /// <summary>
    /// Gets the rows of the MOSMIX element table.
    /// </summary>
    internal static IReadOnlyList<MosmixElementRow> Rows => new[]
    {
        new MosmixElementRow("TTT", QuantityKind.Temperature, "K", null, null, null, null),
        new MosmixElementRow("PPPP", QuantityKind.Pressure, "Pa", null, PressureUnit.Hectopascal, PressureReference.Unknown, null),
        new MosmixElementRow("DD", QuantityKind.WindDirection, "0..360 degrees", null, null, null, null),
        new MosmixElementRow("FF", QuantityKind.MeanWindSpeed, "m/s", null, null, null, WindSpeedUnit.MetresPerSecond),
        new MosmixElementRow("FX1", QuantityKind.WindGust, "m/s", 1, null, null, WindSpeedUnit.MetresPerSecond),
        new MosmixElementRow("Rad1h", QuantityKind.RadiationEnergy, "kJ/m2", 1, null, null, null),
        new MosmixElementRow("RR1c", QuantityKind.PrecipitationAccumulation, "kg/m2", 1, null, null, null)
    };
}
