namespace WeatherParser.Parsing.Quantities;

using System.Globalization;

/// <summary>
/// Converts pressure between hPa and mmHg from one unrounded original and rounds both outputs for export (contract C-40).
/// </summary>
public sealed class PressureConverter : IPressureConverter
{
    /// <summary>
    /// Factor hPa → mmHg (REQ-054: mmHg = hPa × 0.7500617).
    /// </summary>
    private static readonly decimal HpaToMmHg = 0.7500617m;

    /// <summary>
    /// Factor mmHg → hPa (REQ-054: hPa = mmHg × 1.3332239).
    /// </summary>
    private static readonly decimal MmHgToHpa = 1.3332239m;

    private readonly IOutputRounding _rounding;

    /// <summary>
    /// Initializes a new instance of the <see cref="PressureConverter"/> class.
    /// </summary>
    /// <param name="rounding">The rounding implementation to use.</param>
    public PressureConverter(IOutputRounding rounding)
    {
        _rounding = rounding ?? throw new ArgumentNullException(nameof(rounding));
    }

    /// <inheritdoc/>
    public PressureValues FromHpa(decimal hpa, PressureReference reference)
    {
        if (!Enum.IsDefined(reference))
            throw new ArgumentOutOfRangeException(nameof(reference), reference, "Undefined pressure reference.");

        // The factor is below 1, so this product cannot overflow (contract C-63 applies to FromMmHg).
        return new PressureValues(
            _rounding.Round(QuantityKind.Pressure, hpa),
            _rounding.Round(QuantityKind.Pressure, hpa * HpaToMmHg),
            reference,
            PressureUnit.Hectopascal);
    }

    /// <inheritdoc/>
    public PressureValues FromMmHg(decimal mmHg, PressureReference reference)
    {
        if (!Enum.IsDefined(reference))
            throw new ArgumentOutOfRangeException(nameof(reference), reference, "Undefined pressure reference.");

        decimal hpa;
        try
        {
            hpa = mmHg * MmHgToHpa;
        }
        catch (OverflowException exception)
        {
            throw new QuantityConversionException($"Converting {mmHg.ToString(CultureInfo.InvariantCulture)} mmHg gives a value outside the decimal range (contract C-63).", exception);
        }

        return new PressureValues(
            _rounding.Round(QuantityKind.Pressure, hpa),
            _rounding.Round(QuantityKind.Pressure, mmHg),
            reference,
            PressureUnit.MillimetreOfMercury);
    }
}
