using System;
using System.Collections.Generic;
using System.Text;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Default implementation of <see cref="IOsthessenWetterStationIdentity"/>; stateless.
/// </summary>
public sealed class OsthessenWetterStationIdentity : IOsthessenWetterStationIdentity
{
    /// <inheritdoc />
    public string ToStationId(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var normalized = name.Normalize(NormalizationForm.FormC).ToLowerInvariant();
        var builder = new StringBuilder();

        bool pendingSeparator = false;

        foreach (char c in normalized)
        {
            string? replacement = Transliterate(c);
            if (replacement is null)
            {
                pendingSeparator = true;
            }
            else
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(replacement);
                pendingSeparator = false;
            }
        }

        string result = builder.ToString();

        if (string.IsNullOrEmpty(result))
        {
            throw new OsthessenWetterException(
                OsthessenWetterException.FormatChangedErrorId,
                "An overview station name contains no letter or digit a-z or 0-9; the page deviates from the supported revision (contract OW-03, OW-11).");
        }

        return result;
    }

    private static string? Transliterate(char c)
    {
        return c switch
        {
            >= 'a' and <= 'z' or >= '0' and <= '9' => c.ToString(),
            '\u00e4' => "ae",
            '\u00f6' => "oe",
            '\u00fc' => "ue",
            '\u00df' => "ss",
            _ => null
        };
    }

    /// <inheritdoc />
    public void EnsureUnique(IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);

        var slugs = new HashSet<string>(StringComparer.Ordinal);

        foreach (string name in names)
        {
            string slug = ToStationId(name);
            if (!slugs.Add(slug))
            {
                throw new OsthessenWetterException(
                    OsthessenWetterException.StationIdCollisionErrorId,
                    $"Two overview station names map to the station ID '{slug}'; station IDs must be unique (contract OW-03).");
            }
        }
    }
}
