using System.Globalization;
using System.Text.RegularExpressions;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Geography;

/// <summary>
/// Parses decimal degrees or degrees, minutes and seconds with hemisphere into decimal degrees (SC-07).
/// </summary>
public sealed class CoordinateParser : ICoordinateParser
{
    private static readonly Regex DecimalPattern = new(@"\A[+-]?[0-9]{1,3}(\.[0-9]{1,20})?\z", RegexOptions.CultureInvariant);
    private static readonly Regex DmsPattern = new(@"\A(?<deg>[0-9]{1,3})\u00B0 *(?<min>[0-9]{1,2})['\u2032](?: *(?<sec>[0-9]{1,2}(?:\.[0-9]{1,15})?)[""\u2033])? *(?<hem>[NSEWnsew])\z", RegexOptions.CultureInvariant);

    /// <inheritdoc />
    public decimal ParseLatitude(string text)
    {
        return Parse(text, -90m, 90m, "Latitude", 'N', 'S');
    }

    /// <inheritdoc />
    public decimal ParseLongitude(string text)
    {
        return Parse(text, -180m, 180m, "Longitude", 'E', 'W');
    }

    private static decimal Parse(string text, decimal min, decimal max, string axis, char positive, char negative)
    {
        ArgumentNullException.ThrowIfNull(text);

        var trimmed = text.Trim();
        decimal value;
        var dmsMatch = DmsPattern.Match(trimmed);
        if (DecimalPattern.IsMatch(trimmed))
        {
            value = decimal.Parse(trimmed, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }
        else if (dmsMatch.Success)
        {
            value = ParseDms(dmsMatch, positive, negative, axis);
        }
        else
        {
            throw new CoordinateFormatException($"{axis} must be decimal degrees or degrees, minutes and optional seconds with a hemisphere letter (contract SC-07).");
        }

        if (value < min || value > max)
        {
            throw new CoordinateFormatException($"{axis} must be between {min.ToString(CultureInfo.InvariantCulture)} and {max.ToString(CultureInfo.InvariantCulture)} degrees inclusive (contract SC-07).");
        }

        return value;
    }

    private static decimal ParseDms(Match match, char positive, char negative, string axis)
    {
        var hemisphere = char.ToUpperInvariant(match.Groups["hem"].Value[0]);
        if (hemisphere != positive && hemisphere != negative)
        {
            throw new CoordinateFormatException($"{axis} requires the hemisphere {positive} or {negative} (contract SC-07).");
        }

        var degrees = decimal.Parse(match.Groups["deg"].Value, CultureInfo.InvariantCulture);
        var minutes = decimal.Parse(match.Groups["min"].Value, CultureInfo.InvariantCulture);
        var seconds = match.Groups["sec"].Success
            ? decimal.Parse(match.Groups["sec"].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture)
            : 0m;

        if (minutes >= 60m || seconds >= 60m)
        {
            throw new CoordinateFormatException($"{axis} minutes and seconds must be less than 60 (contract SC-07).");
        }

        var value = degrees + minutes / 60m + seconds / 3600m;
        return hemisphere == negative ? -value : value;
    }
}
