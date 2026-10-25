using System.Globalization;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Default implementation of <see cref="IOsthessenWetterValueReader"/>; stateless.
/// </summary>
public sealed class OsthessenWetterValueReader : IOsthessenWetterValueReader
{
    /// <inheritdoc />
    public OsthessenWetterCellValue Read(string? cellText, OsthessenWetterValueColumn column)
    {
        var unit = UnitOf(column);

        if (string.IsNullOrWhiteSpace(cellText))
        {
            return new OsthessenWetterCellValue(OsthessenWetterCellValueStatus.Missing, null);
        }

        if (!cellText.EndsWith(unit, StringComparison.Ordinal))
        {
            return new OsthessenWetterCellValue(OsthessenWetterCellValueStatus.Unreadable, null);
        }

        var number = cellText.Substring(0, cellText.Length - unit.Length);

        if (!IsNumber(number))
        {
            return new OsthessenWetterCellValue(OsthessenWetterCellValueStatus.Unreadable, null);
        }

        var value = double.Parse(number, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        return new OsthessenWetterCellValue(OsthessenWetterCellValueStatus.Present, value);
    }

    private static string UnitOf(OsthessenWetterValueColumn column)
    {
        return column switch
        {
            OsthessenWetterValueColumn.Temperature => "\u00b0C",
            OsthessenWetterValueColumn.RelativeHumidity => "%",
            OsthessenWetterValueColumn.MeanWindSpeed10Minutes => "km/h",
            _ => throw new ArgumentOutOfRangeException(nameof(column), column, "Unknown value column.")
        };
    }

    private static bool IsNumber(string number)
    {
        if (string.IsNullOrEmpty(number))
        {
            return false;
        }

        var startIndex = 0;
        if (number[0] == '-')
        {
            if (number.Length == 1)
            {
                return false;
            }
            startIndex = 1;
        }

        var hasDecimalPoint = false;
        var hasDigitsBeforePoint = false;
        var hasDigitsAfterPoint = false;

        for (var i = startIndex; i < number.Length; i++)
        {
            var c = number[i];
            if (c == '.')
            {
                if (hasDecimalPoint)
                {
                    return false;
                }
                hasDecimalPoint = true;
            }
            else if (char.IsAsciiDigit(c))
            {
                if (!hasDecimalPoint)
                {
                    hasDigitsBeforePoint = true;
                }
                else
                {
                    hasDigitsAfterPoint = true;
                }
            }
            else
            {
                return false;
            }
        }

        return hasDigitsBeforePoint && (!hasDecimalPoint || hasDigitsAfterPoint);
    }
}
