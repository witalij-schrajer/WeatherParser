namespace WeatherParser.Parsing.Quantities;

using System.Globalization;

/// <summary>
/// Rounds values according to the output increment requirements.
/// </summary>
/// <remarks>
/// Rounds only for output, after every conversion; exact midpoint ties are rounded away from zero.
/// A value that cannot carry its output scale raises QuantityConversionException.
/// </remarks>
public sealed class OutputRounding : IOutputRounding
{
    /// <summary>
    /// Rounds an already converted value to the REQ-057 output increment of its kind; kinds without an increment are returned unchanged.
    /// </summary>
    public decimal Round(QuantityKind kind, decimal value)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Undefined quantity kind.");
        }

        var places = DecimalPlaces(kind);
        if (places == null)
        {
            return value;
        }

        var rounded = decimal.Round(value, places.Value, MidpointRounding.AwayFromZero);
        var zeroWithScale = new decimal(0, 0, 0, false, (byte)places.Value);
        var result = rounded == 0m ? zeroWithScale : rounded + zeroWithScale;

        if (result.Scale != places.Value)
        {
            throw new QuantityConversionException($"The {kind} value {value.ToString(CultureInfo.InvariantCulture)} cannot carry its output scale of {places.Value} decimal place(s) (contract C-65).");
        }

        return result;
    }

    private static int? DecimalPlaces(QuantityKind kind) => kind switch
    {
        QuantityKind.Temperature => 1,
        QuantityKind.Pressure or
        QuantityKind.RelativeHumidity or
        QuantityKind.MeanWindSpeed or
        QuantityKind.WindGust => 0,
        _ => null
    };
}
