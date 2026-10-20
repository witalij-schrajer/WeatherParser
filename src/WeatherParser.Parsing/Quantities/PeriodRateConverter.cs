using System.Numerics;

namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// Converts accumulated quantities into mean rates over a known period (contract C-45).
/// </summary>
/// <remarks>
/// Only RadiationEnergy (J/m²) → Irradiance (W/m²) and PrecipitationAccumulation (mm) → PrecipitationRate (mm/h) exist; there is no lux ↔ W/m² path. Results are unrounded.
/// An unrepresentable result raises QuantityConversionException.
/// </remarks>
public sealed class PeriodRateConverter : IPeriodRateConverter
{
    private const long SecondsPerHour = 3600;

    /// <inheritdoc />
    public PeriodQuantity Convert(QuantityKind sourceKind, QuantityKind targetKind, decimal value, AveragingPeriod? period)
    {
        var isEnergyToIrradiance = sourceKind == QuantityKind.RadiationEnergy && targetKind == QuantityKind.Irradiance;
        var isAccumulationToRate = sourceKind == QuantityKind.PrecipitationAccumulation && targetKind == QuantityKind.PrecipitationRate;

        if (!isEnergyToIrradiance && !isAccumulationToRate)
            throw new QuantityConversionException($"No conversion {sourceKind} -> {targetKind} is defined; gusts, probabilities, illuminance and other kinds are never substituted for another kind (contract C-45).");

        if (period is null)
            throw new QuantityConversionException($"Converting {sourceKind} to {targetKind} requires a known averaging period (REQ-055).");

        var seconds = period.Duration.Ticks / TimeSpan.TicksPerSecond;
        var result = isEnergyToIrradiance ? value / seconds : ToRatePerHour(value, seconds, sourceKind, targetKind);
        return new PeriodQuantity(result, targetKind, period);
    }

    /// <summary>
    /// Computes value × 3600 ÷ seconds without an avoidable intermediate overflow: the ratio is reduced first, the product is tried before the quotient, and only an unrepresentable result fails.
    /// </summary>
    private static decimal ToRatePerHour(decimal value, long seconds, QuantityKind sourceKind, QuantityKind targetKind)
    {
        var divisor = (long)BigInteger.GreatestCommonDivisor(SecondsPerHour, seconds);
        var multiplier = (decimal)(SecondsPerHour / divisor);
        var denominator = (decimal)(seconds / divisor);
        try
        {
            return value * multiplier / denominator;
        }
        catch (OverflowException)
        {
            // The product overflowed; dividing first can still give a representable result.
        }

        try
        {
            return value / denominator * multiplier;
        }
        catch (OverflowException exception)
        {
            throw new QuantityConversionException($"Converting {sourceKind} to {targetKind} gives a value outside the decimal range.", exception);
        }
    }
}
