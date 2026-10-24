using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Reads CDC <c>now</c> station lists (SC-02, SC-10).
/// </summary>
public sealed class CdcStationCatalogReader : ICdcStationCatalogReader
{
    private const string ExpectedHeader = "Stations_id von_datum bis_datum Stationshoehe geoBreite geoLaenge Stationsname Bundesland Abgabe";
    private const int MaxLineLength = 2048;

    private static readonly Regex StationIdPattern = new(@"\A[0-9]{5}\z", RegexOptions.CultureInvariant);
    private static readonly Regex DatePattern = new(@"\A[0-9]{8}\z", RegexOptions.CultureInvariant);
    private static readonly Regex AltitudePattern = new(@"\A-?[0-9]+\z", RegexOptions.CultureInvariant);
    private static readonly Regex CoordinatePattern = new(@"\A-?[0-9]+\.[0-9]+\z", RegexOptions.CultureInvariant);
    private static readonly Regex DashLinePattern = new(@"\A[- ]*-[- ]*\z", RegexOptions.CultureInvariant);

    /// <inheritdoc />
    public async Task<CdcStationCatalog> ReadAsync(Stream catalog, string endpointName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(endpointName);
        cancellationToken.ThrowIfCancellationRequested();

        var records = new List<CdcStationRecord>();
        var diagnostics = new List<CatalogDiagnostic>();

        using var reader = new StreamReader(catalog, Encoding.Latin1, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);

        var header = await reader.ReadLineAsync(cancellationToken);
        if (header is null)
        {
            throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "The station list header is missing (contract SC-10).");
        }

        if (header.Length > MaxLineLength)
        {
            throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "A station list line is longer than 2048 characters (contract SC-10).");
        }

        if (!string.Equals(string.Join(' ', header.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)), ExpectedHeader, StringComparison.Ordinal))
        {
            throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "The station list header does not match the SC-02 format.");
        }

        var dashLine = await reader.ReadLineAsync(cancellationToken);
        if (dashLine is null)
        {
            throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "The station list dash line is missing (contract SC-10).");
        }

        if (dashLine.Length > MaxLineLength)
        {
            throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "A station list line is longer than 2048 characters (contract SC-10).");
        }

        if (!DashLinePattern.IsMatch(dashLine))
        {
            throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "The station list dash line does not match the SC-02 format.");
        }

        var lineNumber = 3;
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (line.Length > MaxLineLength)
            {
                throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "A station list line is longer than 2048 characters (contract SC-10).");
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                lineNumber++;
                continue;
            }

            if (TryParseRecord(line, lineNumber, out var record, out var reason))
            {
                records.Add(record);
            }
            else
            {
                string? stationIdOrNull = null;
                var idField = Field(line, 0, 4);
                if (StationIdPattern.IsMatch(idField))
                {
                    stationIdOrNull = idField;
                }

                diagnostics.Add(new CatalogDiagnostic(CatalogDiagnostic.RecordSkippedId, endpointName, lineNumber, stationIdOrNull, reason));
            }

            lineNumber++;
        }

        // SC-10: If no record was parsed successfully, throw a format exception.
        if (records.Count == 0)
        {
            throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "The station list contains no valid record (contract SC-10).");
        }

        // SC-10: Remove duplicates and add diagnostics for them.
        var duplicateStationIds = records
            .GroupBy(r => r.StationId, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.Ordinal);

        if (duplicateStationIds.Count > 0)
        {
            var filteredRecords = new List<CdcStationRecord>();
            foreach (var record in records)
            {
                if (duplicateStationIds.Contains(record.StationId))
                {
                    diagnostics.Add(new CatalogDiagnostic(CatalogDiagnostic.RecordSkippedId, endpointName, record.LineNumber, record.StationId, "Station ID occurs more than once in the station list."));
                }
                else
                {
                    filteredRecords.Add(record);
                }
            }

            records = filteredRecords;
        }

        return new CdcStationCatalog(records, diagnostics.OrderBy(d => d.LineNumber).ToList());
    }

    private static string Field(string line, int start, int endInclusive)
    {
        if (start >= line.Length)
        {
            return string.Empty;
        }

        var length = Math.Min(endInclusive, line.Length - 1) - start + 1;
        return line.Substring(start, length).Trim();
    }

    private static bool IsSeparator(string line, int position) => position >= line.Length || line[position] == ' ';

    private static bool TryParseRecord(string line, int lineNumber, [NotNullWhen(true)] out CdcStationRecord? record, out string reason)
    {
        record = null;
        reason = string.Empty;

        if (!IsSeparator(line, 5) || !IsSeparator(line, 14) || !IsSeparator(line, 60) || !IsSeparator(line, 101))
        {
            reason = "Field separators are not at the SC-02 positions.";
            return false;
        }

        var id = Field(line, 0, 4);
        if (!StationIdPattern.IsMatch(id))
        {
            reason = "Station ID must be exactly five digits.";
            return false;
        }

        var fromStr = Field(line, 6, 13);
        if (!TryParseDate(fromStr, out var from))
        {
            reason = "Dates must be valid yyyyMMdd calendar dates.";
            return false;
        }

        var toStr = Field(line, 15, 22);
        if (!TryParseDate(toStr, out var to))
        {
            reason = "Dates must be valid yyyyMMdd calendar dates.";
            return false;
        }

        var altitudeStr = Field(line, 23, 38);
        decimal? altitude;
        if (string.IsNullOrEmpty(altitudeStr))
        {
            altitude = null;
        }
        else
        {
            if (!AltitudePattern.IsMatch(altitudeStr) || !decimal.TryParse(altitudeStr, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsedAltitude) || parsedAltitude < -500m || parsedAltitude > 5000m)
            {
                reason = "Altitude must be whole metres between -500 and 5000.";
                return false;
            }

            altitude = parsedAltitude;
        }

        var latitudeStr = Field(line, 39, 50);
        if (!TryParseCoordinate(latitudeStr, 90m, out var latitude))
        {
            reason = "Coordinates must be decimal degrees within range.";
            return false;
        }

        var longitudeStr = Field(line, 51, 60);
        if (!TryParseCoordinate(longitudeStr, 180m, out var longitude))
        {
            reason = "Coordinates must be decimal degrees within range.";
            return false;
        }

        var name = Field(line, 61, 100);
        if (string.IsNullOrEmpty(name))
        {
            reason = "Station name must not be empty.";
            return false;
        }

        var state = Field(line, 102, 141);

        record = new CdcStationRecord(id, from, to, altitude, latitude, longitude, name, state, lineNumber);
        return true;
    }

    private static bool TryParseDate(string value, out DateOnly date)
    {
        if (DatePattern.IsMatch(value) && DateOnly.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        date = default;
        return false;
    }

    private static bool TryParseCoordinate(string value, decimal limit, out decimal coordinate)
    {
        if (CoordinatePattern.IsMatch(value) && decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out coordinate) && coordinate >= -limit && coordinate <= limit)
        {
            return true;
        }

        coordinate = 0m;
        return false;
    }
}
