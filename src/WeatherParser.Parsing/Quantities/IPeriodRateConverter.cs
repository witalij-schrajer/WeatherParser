namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// Provides methods to convert period-aware quantities.
/// </summary>
public interface IPeriodRateConverter
{
    /// <summary>
    /// Converts a quantity from one kind to another, taking into account the averaging period.
    /// </summary>
    /// <param name="sourceKind">The source quantity kind.</param>
    /// <param name="targetKind">The target quantity kind.</param>
    /// <param name="value">The value to convert.</param>
    /// <param name="period">The averaging or accumulation period.</param>
    /// <returns>The converted quantity with the specified period.</returns>
    /// <exception cref="QuantityConversionException">Thrown when conversion is not defined or period is missing.</exception>
    PeriodQuantity Convert(QuantityKind sourceKind, QuantityKind targetKind, decimal value, AveragingPeriod? period);
}
