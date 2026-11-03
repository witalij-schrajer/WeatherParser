using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using WeatherParser.Parsing.Exceptions;
using Microsoft.Extensions.Logging;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Reads the MOSMIX station catalog (SC-04, SC-10).
/// </summary>
public sealed class MosmixLocationCatalogReader : IMosmixLocationCatalogReader
{
    private const string ExpectedHeader = "ID ICAO NAME LAT LON ELEV";
    private const int MaxLineLength = 2048;

    private static readonly Regex IdPattern = new(@"\A[A-Z0-9]{4,5}\z", RegexOptions.CultureInvariant);
    private static readonly Regex CoordinatePattern = new(@"\A-?[0-9]{1,3}\.[0-9]{2}\z", RegexOptions.CultureInvariant);
    private static readonly Regex ElevationPattern = new(@"\A-?[0-9]{1,5}\z", RegexOptions.CultureInvariant);
    private static readonly Regex DashLinePattern = new(@"\A[- ]*-[- ]*\z", RegexOptions.CultureInvariant);

    private readonly ILogger<MosmixLocationCatalogReader> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixLocationCatalogReader"/> class.
    /// </summary>
    /// <param name="logger">The logger for the missing-altitude notice (SC-08).</param>
    public MosmixLocationCatalogReader(ILogger<MosmixLocationCatalogReader> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MosmixLocationCatalog> ReadAsync(Stream catalog, string endpointName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(endpointName);
        cancellationToken.ThrowIfCancellationRequested();

        var valid = new List<MosmixLocationRecord>();
        var diagnostics = new List<CatalogDiagnostic>();

        using var reader = new StreamReader(catalog, Encoding.Latin1, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);

        var header = await reader.ReadLineAsync(cancellationToken);
        if (header is null)
        {
            throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "The MOSMIX catalog header is missing (contract SC-10).");
        }

        if (header.Length > MaxLineLength)
        {
            throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "A MOSMIX catalog line is longer than 2048 characters (contract SC-10).");
        }

        if (!string.Equals(string.Join(' ', header.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)), ExpectedHeader, StringComparison.Ordinal))
        {
            throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "The MOSMIX catalog header does not match the SC-04 format.");
        }

        var dashLine = await reader.ReadLineAsync(cancellationToken);
        if (dashLine is null)
        {
            throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "The MOSMIX catalog dash line is missing (contract SC-10).");
        }

        if (dashLine.Length > MaxLineLength)
        {
            throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "A MOSMIX catalog line is longer than 2048 characters (contract SC-10).");
        }

        if (!DashLinePattern.IsMatch(dashLine))
        {
            throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "The MOSMIX catalog dash line does not match the SC-04 format.");
        }

        var lineNumber = 3;
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (line.Length > MaxLineLength)
            {
                throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "A MOSMIX catalog line is longer than 2048 characters (contract SC-10).");
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                lineNumber++;
                continue;
            }

            if (TryParseRecord(line, lineNumber, out var record, out var reason))
            {
                valid.Add(record);
            }
            else
            {
                string? idOrNull = null;
                if (line.Length >= 5)
                {
                    var idField = line.Substring(0, 5).Trim();
                    if (IdPattern.IsMatch(idField))
                    {
                        idOrNull = idField;
                    }
                }

                diagnostics.Add(new CatalogDiagnostic(CatalogDiagnostic.RecordSkippedId, endpointName, lineNumber, idOrNull, reason));
            }

            lineNumber++;
        }

        // SC-10: If no record was parsed successfully, throw a format exception.
        if (valid.Count == 0)
        {
            throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "The MOSMIX catalog contains no valid record (contract SC-10).");
        }

        // SC-10: Remove records with duplicate IDs
        var idGroups = valid.GroupBy(r => r.MosmixId, StringComparer.Ordinal);
        var duplicates = idGroups.Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.Ordinal);

        foreach (var record in valid.Where(r => duplicates.Contains(r.MosmixId)))
        {
            diagnostics.Add(new CatalogDiagnostic(CatalogDiagnostic.RecordSkippedId, endpointName, record.LineNumber, record.MosmixId, "The MOSMIX ID occurs more than once in the catalog; all its records are excluded (contract SC-10)."));
        }
        var remaining = valid.Where(r => !duplicates.Contains(r.MosmixId)).ToList();

        // Missing altitude: for each remaining record with AltitudeM is null, add diagnostic and log notice
        foreach (var record in remaining)
        {
            if (record.AltitudeM is null)
            {
                diagnostics.Add(new CatalogDiagnostic(CatalogDiagnostic.MosmixLocationNoAltitudeId, endpointName, record.LineNumber, record.MosmixId, "The location has no elevation and is excluded from the selection (REQ-102, contract SC-04).") { AlreadyLogged = true });
                _logger.LogWarning(new EventId(0, CatalogDiagnostic.MosmixLocationNoAltitudeId), "MOSMIX location {MosmixId} on line {LineNumber} of endpoint {EndpointName} has no elevation and is excluded from the selection.", record.MosmixId, record.LineNumber, endpointName);
            }
        }

        return new MosmixLocationCatalog(remaining, diagnostics.OrderBy(d => d.LineNumber).ToList());
    }

    private static bool TryParseRecord(string line, int lineNumber, [NotNullWhen(true)] out MosmixLocationRecord? record, out string reason)
    {
        record = null;
        reason = string.Empty;

        if (line.Length < 31)
        {
            reason = "The record is too short (contract SC-04).";
            return false;
        }

        var id = line.Substring(0, 5).Trim();
        if (!IdPattern.IsMatch(id))
        {
            reason = "The MOSMIX ID does not match the SC-04 grammar.";
            return false;
        }

        if (line[5] != ' ')
        {
            reason = "Character 5 must be a space (contract SC-04).";
            return false;
        }

        var name = line.Substring(11, 20).Trim();
        if (string.IsNullOrEmpty(name))
        {
            reason = "The name is empty (contract SC-04).";
            return false;
        }

        var rest = line.Substring(31).Trim();
        var tokens = rest.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length != 2 && tokens.Length != 3)
        {
            reason = "Expected latitude, longitude and an optional elevation (contract SC-04).";
            return false;
        }

        var latitudeStr = tokens[0];
        if (!CoordinatePattern.IsMatch(latitudeStr))
        {
            reason = "A coordinate does not match the SC-04 grammar.";
            return false;
        }

        var longitudeStr = tokens[1];
        if (!CoordinatePattern.IsMatch(longitudeStr))
        {
            reason = "A coordinate does not match the SC-04 grammar.";
            return false;
        }

        decimal? elevation = null;
        if (tokens.Length == 3)
        {
            var elevationStr = tokens[2];
            if (!ElevationPattern.IsMatch(elevationStr))
            {
                reason = "The elevation does not match the SC-04 grammar.";
                return false;
            }

            var elevationValue = decimal.Parse(elevationStr, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            if (elevationValue < -500 || elevationValue > 9000)
            {
                reason = "The elevation lies outside -500 to 9000 m (contract SC-04).";
                return false;
            }

            elevation = elevationValue;
        }

        if (!TryConvertDegreesMinutes(latitudeStr, 90m, out var latitude))
        {
            reason = "A coordinate has minutes above 59 or lies outside the permitted range (contract SC-04).";
            return false;
        }

        if (!TryConvertDegreesMinutes(longitudeStr, 180m, out var longitude))
        {
            reason = "A coordinate has minutes above 59 or lies outside the permitted range (contract SC-04).";
            return false;
        }

        record = new MosmixLocationRecord(id, name, latitude, longitude, elevation, lineNumber);
        return true;
    }

    private static bool TryConvertDegreesMinutes(string token, decimal limit, out decimal value)
    {
        var negative = token[0] == '-';
        var body = negative ? token[1..] : token;
        var dot = body.IndexOf('.');
        var degrees = int.Parse(body[..dot], NumberStyles.None, CultureInfo.InvariantCulture);
        var minutes = int.Parse(body[(dot + 1)..], NumberStyles.None, CultureInfo.InvariantCulture);
        value = 0m;
        if (minutes > 59)
        {
            return false;
        }
        var magnitude = degrees + minutes / 60m;
        if (magnitude > limit)
        {
            return false;
        }
        value = negative ? -magnitude : magnitude;
        return true;
    }
}
