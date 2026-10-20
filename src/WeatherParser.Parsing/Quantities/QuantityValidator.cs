namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// Validates source values per quantity kind and converts accepted values to decimal;
/// rejected values carry a reason and are omitted by the caller (contract C-42).
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><c>Temperature</c>: Any value is valid.</item>
///   <item><c>Pressure</c>: Must be greater than 0.</item>
///   <item><c>RelativeHumidity</c>: Must be between 0 and 100 inclusive.</item>
///   <item><c>Probability</c>: Must be between 0 and 100 inclusive.</item>
///   <item><c>WindDirection</c>: Must be between 0 and 360 inclusive.</item>
/// </list>
/// Ranges are checked on the original value before the conversion to decimal; afterwards only the strict pressure bound is checked again.
/// </remarks>
public sealed class QuantityValidator : IQuantityValidator
{
    /// <inheritdoc />
    public ValidatedQuantity Validate(QuantityKind kind, double? value)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Undefined quantity kind.");

        if (value is null)
            return ValidatedQuantity.Rejected(QuantityRejectionReason.Missing);

        if (!double.IsFinite(value.Value))
            return ValidatedQuantity.Rejected(QuantityRejectionReason.NonFinite);

        // (double)decimal.MaxValue rounds above decimal.MaxValue, so equality must be rejected too.
        if (Math.Abs(value.Value) >= (double)decimal.MaxValue)
            return ValidatedQuantity.Rejected(QuantityRejectionReason.OutOfRange);

        // Check the original value first: the decimal conversion rounds and could move it into range.
        if (!IsInRange(kind, value.Value))
            return ValidatedQuantity.Rejected(QuantityRejectionReason.OutOfRange);

        var number = (decimal)value.Value;

        // The conversion preserves order and the inclusive bounds 0, 100 and 360 are exact, so only
        // the strict pressure bound can be lost (a tiny positive pressure such as 1e-30 becomes 0).
        if (kind == QuantityKind.Pressure && number <= 0m)
            return ValidatedQuantity.Rejected(QuantityRejectionReason.OutOfRange);

        return ValidatedQuantity.Valid(kind == QuantityKind.WindDirection && number == 360m ? 0m : number);
    }

    private static bool IsInRange(QuantityKind kind, double number) => kind switch
    {
        QuantityKind.Temperature => true,
        QuantityKind.Pressure => number > 0d,
        QuantityKind.RelativeHumidity or QuantityKind.Probability => number >= 0d && number <= 100d,
        QuantityKind.WindDirection => number >= 0d && number <= 360d,
        _ => number >= 0d
    };
}
