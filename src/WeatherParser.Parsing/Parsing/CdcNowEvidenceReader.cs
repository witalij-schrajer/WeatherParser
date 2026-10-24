using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Parsing;

/// <summary>
/// Reads CDC <c>now</c> evidence archives under the SC-19 contract.
/// </summary>
public sealed class CdcNowEvidenceReader : ICdcNowEvidenceReader
{
    private const int MaxBytes = 1048576;
    private const int MaxRows = 2000;
    private const int BufferSize = 81920;

    private static readonly Regex RequestedStationIdPattern = new(@"\A[0-9]{5}\z", RegexOptions.CultureInvariant);
    private static readonly Regex RowStationIdPattern = new(@"\A[0-9]{1,5}\z", RegexOptions.CultureInvariant);
    private static readonly Regex MessDatumPattern = new(@"\A[0-9]{12}\z", RegexOptions.CultureInvariant);
    private static readonly Regex QnPattern = new(@"\A[0-9]{1,3}\z", RegexOptions.CultureInvariant);
    private static readonly Regex ValuePattern = new(@"\A-?[0-9]{1,6}(\.[0-9]{1,3})?\z", RegexOptions.CultureInvariant);

    /// <inheritdoc />
    public async Task<CdcNowEvidence> ReadAsync(Stream archive, string stationId, string productCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentNullException.ThrowIfNull(stationId);
        ArgumentNullException.ThrowIfNull(productCode);

        if (!RequestedStationIdPattern.IsMatch(stationId))
        {
            throw new ArgumentException("Station ID must be exactly five digits.", nameof(stationId));
        }

        var columns = ProductColumns(productCode);
        cancellationToken.ThrowIfCancellationRequested();

        using var compressed = await ReadBoundedAsync(archive, cancellationToken) ?? throw Invalid("The evidence archive is larger than 1 MiB (contract SC-19).");

        byte[] content;
        try
        {
            using var zip = new ZipArchive(compressed, ZipArchiveMode.Read);
            if (zip.Entries.Count != 1)
            {
                throw Invalid("The evidence archive must contain exactly one member (contract SC-19).");
            }

            var entry = zip.Entries[0];
            var expectedName = new Regex($@"\Aprodukt_zehn_now_{productCode}_[0-9]{{8}}_[0-9]{{8}}_{stationId}\.txt\z", RegexOptions.CultureInvariant);
            if (!expectedName.IsMatch(entry.FullName))
            {
                throw Invalid("The evidence archive member name does not match the requested station and product (contract SC-19).");
            }

            using var entryStream = entry.Open();
            using var uncompressed = await ReadBoundedAsync(entryStream, cancellationToken) ?? throw Invalid("The evidence file is larger than 1 MiB (contract SC-19).");
            content = uncompressed.ToArray();
        }
        catch (InvalidDataException)
        {
            throw Invalid("The evidence archive is not a valid ZIP archive (contract SC-19).");
        }

        return Parse(Encoding.Latin1.GetString(content), stationId, productCode, columns);
    }

    private static string[] ProductColumns(string productCode) => productCode switch
    {
        "tu" => ["PP_10", "TT_10", "RF_10"],
        "ff" => ["FF_10", "DD_10"],
        "rr" => ["RWS_10"],
        "sd" => ["GS_10"],
        _ => throw new ArgumentException("Product code must be tu, ff, rr or sd.", nameof(productCode))
    };

    private static CatalogFormatException Invalid(string message) => new(CatalogFormatException.CdcEvidenceInvalidErrorId, message);

    private static async Task<MemoryStream?> ReadBoundedAsync(Stream source, CancellationToken cancellationToken)
    {
        var memory = new MemoryStream();
        var buffer = new byte[BufferSize];
        long total = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = await source.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (read == 0)
            {
                break;
            }

            total += read;
            if (total > MaxBytes)
            {
                memory.Dispose();
                return null;
            }

            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        memory.Position = 0;
        return memory;
    }

    private static CdcNowEvidence Parse(string text, string stationId, string productCode, string[] columns)
    {
        var reader = new StringReader(text);
        var line = reader.ReadLine();
        if (line is null)
        {
            throw Invalid("The evidence file is missing a header line (contract SC-19).");
        }

        var names = line.Split(';').Select(f => f.Trim()).ToArray();

        // Validate header
        if (names.Length < 4)
        {
            throw Invalid("The evidence file header must contain at least four fields (contract SC-19).");
        }

        if (names[0] != "STATIONS_ID")
        {
            throw Invalid("The evidence file header must start with 'STATIONS_ID' (contract SC-19).");
        }

        if (names[1] != "MESS_DATUM")
        {
            throw Invalid("The evidence file header must have 'MESS_DATUM' as the second field (contract SC-19).");
        }

        if (names[2] != "QN")
        {
            throw Invalid("The evidence file header must have 'QN' as the third field (contract SC-19).");
        }

        if (names[^1] != "eor")
        {
            throw Invalid("The evidence file header must end with 'eor' (contract SC-19).");
        }

        var uniqueNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            if (!uniqueNames.Add(name))
            {
                throw Invalid("The evidence file header must not contain duplicate field names (contract SC-19).");
            }
        }

        foreach (var column in columns)
        {
            if (!names.Contains(column, StringComparer.Ordinal))
            {
                throw Invalid("The evidence file header must contain all product-specific fields (contract SC-19).");
            }
        }

        var requestedNumber = int.Parse(stationId, CultureInfo.InvariantCulture);
        var rows = new List<CdcNowEvidenceRow>();
        var timestamps = new HashSet<DateTime>();

        var lineNumber = 1;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            if (lineNumber > MaxRows + 1)
            {
                throw Invalid("The evidence file must contain at most 2000 data rows (contract SC-19).");
            }

            var fields = line.Split(';').Select(f => f.Trim()).ToArray();

            if (fields.Length != names.Length)
            {
                throw Invalid($"The evidence file row at line {lineNumber} must contain exactly {names.Length} fields (contract SC-19).");
            }

            if (!RowStationIdPattern.IsMatch(fields[0]))
            {
                throw Invalid($"The evidence file row at line {lineNumber} has an invalid station ID format (contract SC-19).");
            }

            var rowStationNumber = int.Parse(fields[0], CultureInfo.InvariantCulture);
            if (rowStationNumber != requestedNumber)
            {
                throw Invalid($"The evidence file row at line {lineNumber} has a mismatched station ID (contract SC-19).");
            }

            if (!MessDatumPattern.IsMatch(fields[1]))
            {
                throw Invalid($"The evidence file row at line {lineNumber} has an invalid measurement date format (contract SC-19).");
            }

            if (!DateTime.TryParseExact(fields[1], "yyyyMMddHHmm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var measuredAt))
            {
                throw Invalid($"The evidence file row at line {lineNumber} has an invalid measurement date (contract SC-19).");
            }

            if (!timestamps.Add(measuredAt))
            {
                throw Invalid($"The evidence file row at line {lineNumber} has a duplicate measurement date (contract SC-19).");
            }

            if (!QnPattern.IsMatch(fields[2]))
            {
                throw Invalid($"The evidence file row at line {lineNumber} has an invalid quality number format (contract SC-19).");
            }

            if (fields[^1] != "eor")
            {
                throw Invalid($"The evidence file row at line {lineNumber} must end with 'eor' (contract SC-19).");
            }

            foreach (var column in columns)
            {
                var index = Array.IndexOf(names, column);
                if (!ValuePattern.IsMatch(fields[index]))
                {
                    throw Invalid($"The evidence file row at line {lineNumber} has an invalid value format for '{column}' (contract SC-19).");
                }
            }

            rows.Add(new CdcNowEvidenceRow(
                DateTime.SpecifyKind(measuredAt, DateTimeKind.Utc),
                columns.ToDictionary(c => c, c => fields[Array.IndexOf(names, c)], StringComparer.Ordinal)));
        }

        return new CdcNowEvidence(stationId, productCode, rows);
    }
}
