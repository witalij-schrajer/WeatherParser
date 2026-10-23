using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Evidence.MosmixS;

/// <summary>
/// File-system store of the MOSMIX_S evidence index (SC-22).
/// </summary>
public sealed class FileSystemMosmixEvidenceCache : IMosmixEvidenceCache
{
    private const int FormatVersion = 1;
    private const int MaxIndexBytes = 8388608;
    private const int MaxDepth = 8;
    private const int MaxLocations = 20000;
    private const int MaxNamesPerLocation = 200;
    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss";

    private static readonly Regex IdRegex = new(@"\A[A-Z0-9]{4,5}\z", RegexOptions.CultureInvariant);
    private static readonly Regex NameRegex = new(@"\A[A-Za-z0-9_]{1,16}\z", RegexOptions.CultureInvariant);
    private static readonly Regex Sha256Regex = new(@"\A[0-9a-f]{64}\z", RegexOptions.CultureInvariant);
    private static readonly Regex TimestampRegex = new(@"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}\z", RegexOptions.CultureInvariant);

    private readonly string _directory;
    private readonly string _sourceKey;
    private readonly string _indexPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemMosmixEvidenceCache"/> class.
    /// </summary>
    /// <param name="options">The catalog endpoints options.</param>
    /// <param name="validator">The catalog endpoints validator.</param>
    public FileSystemMosmixEvidenceCache(MosmixCatalogEndpoints options, IMosmixCatalogEndpointsValidator validator)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(validator);
        validator.Validate(options);

        _directory = options.EvidenceCacheDirectory!;
        _sourceKey = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(options.MosmixSAllStationsUri!.OriginalString)));
        _indexPath = Path.Combine(_directory, "mosmix_s_evidence_" + _sourceKey[..16] + ".json");
    }

    /// <inheritdoc />
    public async Task<MosmixSEvidenceIndex?> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] bytes;
        try
        {
            await using var stream = OpenIndexForRead(_indexPath);
            if (stream.Length > MaxIndexBytes)
            {
                return null;
            }

            bytes = new byte[stream.Length];
            await stream.ReadExactlyAsync(bytes, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        try
        {
            return Parse(bytes);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Opens the index file for reading with the cache's sharing mode (test seam for concurrent replacement).</summary>
    /// <remarks>
    /// Readers allow deletion so that <see cref="WriteAsync"/> can atomically replace the index while a reader still reads its old snapshot (SC-22).
    /// </remarks>
    internal static FileStream OpenIndexForRead(string path) => new(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 4096, useAsync: true);

    private MosmixSEvidenceIndex? Parse(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = MaxDepth });
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!root.TryGetProperty("format_version", out var formatVersionElement) ||
            formatVersionElement.ValueKind != JsonValueKind.Number ||
            !formatVersionElement.TryGetInt32(out var formatVersion) ||
            formatVersion != FormatVersion)
        {
            return null;
        }

        if (!root.TryGetProperty("source_key", out var sourceKeyElement) ||
            sourceKeyElement.ValueKind != JsonValueKind.String ||
            !string.Equals(sourceKeyElement.GetString(), _sourceKey, StringComparison.Ordinal))
        {
            return null;
        }

        if (!root.TryGetProperty("source_uri", out var sourceUriElement) ||
            sourceUriElement.ValueKind != JsonValueKind.String ||
            !IsSanitizedSourceUri(sourceUriElement.GetString()))
        {
            return null;
        }

        var sourceUri = sourceUriElement.GetString()!;

        if (!root.TryGetProperty("downloaded_at", out var downloadedAtElement) ||
            downloadedAtElement.ValueKind != JsonValueKind.String ||
            !TryParseTimestamp(downloadedAtElement.GetString(), out var downloadedAt))
        {
            return null;
        }

        if (!root.TryGetProperty("issued_at", out var issuedAtElement) ||
            issuedAtElement.ValueKind != JsonValueKind.String ||
            !TryParseTimestamp(issuedAtElement.GetString(), out var issuedAt))
        {
            return null;
        }

        if (!root.TryGetProperty("file_sha256", out var fileSha256Element) ||
            fileSha256Element.ValueKind != JsonValueKind.String ||
            !Sha256Regex.IsMatch(fileSha256Element.GetString()!))
        {
            return null;
        }

        var fileSha256 = fileSha256Element.GetString()!;

        if (!root.TryGetProperty("file_size", out var fileSizeElement) ||
            fileSizeElement.ValueKind != JsonValueKind.Number ||
            !fileSizeElement.TryGetInt64(out var fileSize) ||
            fileSize < 0)
        {
            return null;
        }

        if (!root.TryGetProperty("locations", out var locationsElement) ||
            locationsElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var locations = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var locationsCount = 0;

        foreach (var location in locationsElement.EnumerateObject())
        {
            if (locationsCount >= MaxLocations)
            {
                return null;
            }

            if (!IdRegex.IsMatch(location.Name))
            {
                return null;
            }

            if (location.Value.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var names = new List<string>();
            var namesCount = 0;

            foreach (var name in location.Value.EnumerateArray())
            {
                if (namesCount >= MaxNamesPerLocation)
                {
                    return null;
                }

                if (name.ValueKind != JsonValueKind.String ||
                    !NameRegex.IsMatch(name.GetString()!))
                {
                    return null;
                }

                names.Add(name.GetString()!);
                namesCount++;
            }

            // Check for duplicates within the array
            if (names.Count != new HashSet<string>(names, StringComparer.Ordinal).Count)
            {
                return null;
            }

            if (!locations.TryAdd(location.Name, names))
            {
                return null;
            }

            locationsCount++;
        }

        return new MosmixSEvidenceIndex(sourceUri, downloadedAt, issuedAt, fileSha256, fileSize, locations);
    }

    private static bool TryParseTimestamp(string? value, out DateTime utc)
    {
        utc = default;

        if (value == null || !TimestampRegex.IsMatch(value))
        {
            return false;
        }

        if (!DateTime.TryParseExact(value, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return false;
        }

        utc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        return true;
    }

    /// <inheritdoc />
    public async Task WriteAsync(MosmixSEvidenceIndex index, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(index);
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsSanitizedSourceUri(index.SourceUri))
        {
            throw Failed("the source URI is not a sanitized absolute HTTP(S) URI without user information, query or fragment");
        }

        if (index.DownloadedAtUtc.Kind != DateTimeKind.Utc ||
            index.DownloadedAtUtc.Ticks % TimeSpan.TicksPerSecond != 0)
        {
            throw Failed("the downloaded timestamp is not in the C-06 form");
        }

        if (index.IssuedAtUtc.Kind != DateTimeKind.Utc ||
            index.IssuedAtUtc.Ticks % TimeSpan.TicksPerSecond != 0)
        {
            throw Failed("the issued timestamp is not in the C-06 form");
        }

        if (!Sha256Regex.IsMatch(index.FileSha256))
        {
            throw Failed("the SHA-256 is invalid");
        }

        if (index.FileSize < 0)
        {
            throw Failed("the file size is negative");
        }

        if (index.Locations == null)
        {
            throw Failed("the locations are missing");
        }

        if (index.Locations.Count > MaxLocations)
        {
            throw Failed("the number of locations exceeds the limit");
        }

        foreach (var location in index.Locations)
        {
            if (!IdRegex.IsMatch(location.Key))
            {
                throw Failed("a location ID is invalid");
            }

            if (location.Value == null)
            {
                throw Failed("a location's element names are missing");
            }

            if (location.Value.Count > MaxNamesPerLocation)
            {
                throw Failed("the number of element names per location exceeds the limit");
            }

            foreach (var name in location.Value)
            {
                if (!NameRegex.IsMatch(name))
                {
                    throw Failed("an element name is invalid");
                }
            }

            // Check for duplicates within the list
            if (location.Value.Count != new HashSet<string>(location.Value, StringComparer.Ordinal).Count)
            {
                throw Failed("the element names per location contain duplicates");
            }
        }

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            writer.WriteNumber("format_version", FormatVersion);
            writer.WriteString("source_key", _sourceKey);
            writer.WriteString("source_uri", index.SourceUri);
            writer.WriteString("downloaded_at", index.DownloadedAtUtc.ToString(TimestampFormat, CultureInfo.InvariantCulture));
            writer.WriteString("issued_at", index.IssuedAtUtc.ToString(TimestampFormat, CultureInfo.InvariantCulture));
            writer.WriteString("file_sha256", index.FileSha256);
            writer.WriteNumber("file_size", index.FileSize);

            writer.WritePropertyName("locations");
            writer.WriteStartObject();

            foreach (var location in index.Locations)
            {
                writer.WritePropertyName(location.Key);
                writer.WriteStartArray();

                foreach (var name in location.Value)
                {
                    writer.WriteStringValue(name);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        var bytes = stream.ToArray();

        if (bytes.Length > MaxIndexBytes)
        {
            throw Failed("the serialized index exceeds 8 MiB");
        }

        if (!Directory.Exists(_directory))
        {
            throw Failed("the evidence cache directory does not exist");
        }

        var temporaryPath = _indexPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken);

            // Atomic replacement (SC-22): File.Replace also succeeds while readers hold the old index open with FileShare.Delete;
            // the first index of a source key has nothing to replace and is moved into place.
            if (File.Exists(_indexPath))
            {
                File.Replace(temporaryPath, _indexPath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(temporaryPath, _indexPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw Failed("the index could not be written");
        }
        finally
        {
            DeleteTemporaryFile(temporaryPath);
        }
    }

    private static bool IsSanitizedSourceUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && uri.UserInfo.Length == 0
        && uri.Query.Length == 0
        && uri.Fragment.Length == 0;

    private static void DeleteTemporaryFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception)
        {
            // Best-effort cleanup after a failed write: a cleanup failure must not replace the reported failure or cancellation,
            // and a leftover temporary file never replaces the index.
        }
    }

    private static EvidenceCacheException Failed(string reason) => new(EvidenceCacheException.CacheFailedErrorId, $"The MOSMIX_S evidence index could not be written: {reason} (contract SC-22).");
}
