namespace WeatherParser.Parsing.Quantities;

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

        return new PressureValues(
            _rounding.Round(QuantityKind.Pressure, mmHg * MmHgToHpa),
            _rounding.Round(QuantityKind.Pressure, mmHg),
            reference,
            PressureUnit.MillimetreOfMercury);
    }
}
