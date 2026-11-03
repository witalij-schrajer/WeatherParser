using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using WeatherParser.Parsing.Evidence.Kmz;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Refreshes the MOSMIX_S evidence index from the all-stations file (SC-22).
/// </summary>
public sealed class MosmixSEvidenceRefresher : IMosmixSEvidenceRefresher
{
    private const long MaxMemberBytesLimit = 2147483648;
    private const string KmlNamespace = "http://www.opengis.net/kml/2.2";
    private const string DwdNamespace = "https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd";
    private const string EndpointName = "MosmixSAllStationsUri";
    private const int MaxPlacemarks = 20000;
    private const int MaxForecastsPerPlacemark = 200;
    private const int MaxShortTextLength = 256;
    private const int MaxTokenLength = 17;
    private const int MaxDepth = 32;

    private static readonly Regex IssueTimePattern = new(@"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.([0-9]{1,3}))?Z\z", RegexOptions.CultureInvariant);
    private static readonly Regex IdPattern = new(@"\A[A-Z0-9]{4,5}\z", RegexOptions.CultureInvariant);
    private static readonly Regex NamePattern = new(@"\A[A-Za-z0-9_]{1,16}\z", RegexOptions.CultureInvariant);
    private static readonly Regex TokenPattern = new(@"\A-?[0-9]{1,9}(\.[0-9]{1,6})?\z", RegexOptions.CultureInvariant);

    private readonly MosmixCatalogEndpoints _options;
    private readonly IMosmixCatalogEndpointsValidator _validator;
    private readonly ICatalogDownloader _downloader;
    private readonly IMosmixEvidenceCache _cache;
    private readonly TimeProvider _timeProvider;
    private readonly long _maxMemberBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixSEvidenceRefresher"/> class.
    /// </summary>
    /// <param name="options">The MOSMIX catalog endpoints options.</param>
    /// <param name="validator">The MOSMIX catalog endpoints validator.</param>
    /// <param name="downloader">The catalog downloader.</param>
    /// <param name="cache">The MOSMIX evidence cache.</param>
    /// <param name="timeProvider">The time provider.</param>
    public MosmixSEvidenceRefresher(
        MosmixCatalogEndpoints options,
        IMosmixCatalogEndpointsValidator validator,
        ICatalogDownloader downloader,
        IMosmixEvidenceCache cache,
        TimeProvider timeProvider)
        : this(options, validator, downloader, cache, timeProvider, MaxMemberBytesLimit)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixSEvidenceRefresher"/> class.
    /// </summary>
    /// <param name="options">The MOSMIX catalog endpoints options.</param>
    /// <param name="validator">The MOSMIX catalog endpoints validator.</param>
    /// <param name="downloader">The catalog downloader.</param>
    /// <param name="cache">The MOSMIX evidence cache.</param>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="maxMemberBytes">The maximum member bytes allowed.</param>
    internal MosmixSEvidenceRefresher(
        MosmixCatalogEndpoints options,
        IMosmixCatalogEndpointsValidator validator,
        ICatalogDownloader downloader,
        IMosmixEvidenceCache cache,
        TimeProvider timeProvider,
        long maxMemberBytes)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        _validator.Validate(options);

        _maxMemberBytes = maxMemberBytes;
    }

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        using var deadline = new CancellationTokenSource(_options.MosmixSRefreshDeadline, _timeProvider);
        var uri = _options.MosmixSAllStationsUri!;
        var request = new CatalogRequest(EndpointName, uri, CatalogRequest.MosmixSAllStationsMaxBytes, false, deadline.Token, CatalogDeadlineKind.Refresh);
        var bytes = (await _downloader.DownloadAsync(request, async (stream, token) =>
        {
            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy, token);
            return copy.ToArray();
        }, cancellationToken))!;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var downloadedAt = new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        try
        {
            var (issuedAt, locations) = Parse(bytes, linked.Token);
            var index = new MosmixSEvidenceIndex(
                uri.Scheme + "://" + uri.Host + (uri.IsDefaultPort ? string.Empty : ":" + uri.Port.ToString(CultureInfo.InvariantCulture)) + uri.AbsolutePath,
                downloadedAt,
                issuedAt,
                Convert.ToHexStringLower(SHA256.HashData(bytes)),
                bytes.LongLength,
                locations);
            await _cache.WriteAsync(index, linked.Token);
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("The MOSMIX_S evidence refresh was cancelled by the caller.", cancellationToken);
            }

            throw new EvidenceCacheException(EvidenceCacheException.RefreshDeadlineExceededErrorId, "The MOSMIX_S evidence refresh exceeded MosmixSRefreshDeadline (contract SC-22).");
        }
    }

    private (DateTime IssuedAt, Dictionary<string, IReadOnlyList<string>> Locations) Parse(byte[] bytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var archive = OpenArchive(bytes);
        List<ZipArchiveEntry> members;
        try
        {
            members = archive.Entries.Where(e => e.FullName.EndsWith(".kml", StringComparison.OrdinalIgnoreCase)).ToList();
        }
        catch (InvalidDataException)
        {
            throw Invalid("the archive is not a valid ZIP archive");
        }

        if (members.Count != 1)
        {
            throw Invalid("the archive must contain exactly one .kml member");
        }

        try
        {
            using var member = members[0].Open();
            // One delegate for the three guards, so the compiler-generated delegate cache has a single creation site.
            Func<string, Exception> invalid = Invalid;
            using var counting = new KmzCountingStream(member, _maxMemberBytes, cancellationToken, invalid);
            using var guarded = new KmzMarkupGuardStream(counting, MaxDepth, invalid);
            using var reader = XmlReader.Create(guarded, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, IgnoreWhitespace = true, IgnoreComments = true, IgnoreProcessingInstructions = true, NameTable = new KmzBoundedNameTable(invalid) });
            reader.MoveToContent();
            if (reader.LocalName != "kml" || reader.NamespaceURI != KmlNamespace)
            {
                throw Invalid("the root element must be kml:kml");
            }

            var documents = 0;
            DateTime? issuedAt = null;
            var timeSteps = 0;
            var definitions = 0;
            var locations = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            reader.ReadStartElement();
            while (MoveToChildElement(reader))
            {
                if (reader.LocalName != "Document" || reader.NamespaceURI != KmlNamespace)
                {
                    reader.Skip();
                    continue;
                }

                documents++;
                if (documents > 1)
                {
                    throw Invalid("kml:Document must occur exactly once");
                }

                if (reader.IsEmptyElement)
                {
                    reader.Read();
                    continue;
                }

                reader.ReadStartElement();
                while (MoveToChildElement(reader))
                {
                    if (reader.NamespaceURI == KmlNamespace && reader.LocalName == "ExtendedData")
                    {
                        ReadDocumentExtendedData(reader, ref definitions, ref issuedAt, ref timeSteps);
                    }
                    else if (reader.NamespaceURI == KmlNamespace && reader.LocalName == "Placemark")
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (issuedAt is null)
                        {
                            throw Invalid("a placemark must not appear before the product definition");
                        }

                        var (id, available) = ReadPlacemark(reader, timeSteps);
                        if (!locations.TryAdd(id, available))
                        {
                            throw Invalid($"the placemark ID {id} occurs more than once");
                        }

                        if (locations.Count > MaxPlacemarks)
                        {
                            throw Invalid("the file contains more than 20000 placemarks");
                        }
                    }
                    else
                    {
                        reader.Skip();
                    }
                }

                reader.ReadEndElement();
            }

            if (documents != 1)
            {
                throw Invalid("kml:Document must occur exactly once");
            }

            if (issuedAt is null)
            {
                throw Invalid("dwd:ProductDefinition must occur exactly once");
            }

            if (locations.Count == 0)
            {
                throw Invalid("the file contains no placemark");
            }

            // Read the member to its end: XmlReader rejects any further root or malformed suffix, and the counting stream sees every byte.
            reader.ReadEndElement();
            while (reader.Read())
            {
            }

            return (issuedAt.Value, locations);
        }
        catch (XmlException)
        {
            throw Invalid("the KML is not well-formed or contains a DTD");
        }
        catch (InvalidDataException)
        {
            throw Invalid("the .kml member is corrupt");
        }
    }

    private static ZipArchive OpenArchive(byte[] bytes)
    {
        try
        {
            return new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        }
        catch (InvalidDataException)
        {
            throw Invalid("the archive is not a valid ZIP archive");
        }
    }

    // Reads the text of the current element without materializing more than maxLength characters (SC-22: never hold a large subtree in memory).
    // The reader is positioned on the start tag and ends after the end tag; child elements are not allowed.
    private static string ReadBoundedText(XmlReader reader, int maxLength, string path, bool trim = false)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return string.Empty;
        }

        var text = new StringBuilder();
        var buffer = new char[maxLength + 1];
        reader.Read();
        var pendingWhitespace = false;
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (reader.NodeType is not (XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace))
            {
                throw Invalid(path + " must contain only text");
            }

            int read;
            while ((read = reader.ReadValueChunk(buffer, 0, buffer.Length)) > 0)
            {
                if (trim)
                {
                    for (var i = 0; i < read; i++)
                    {
                        var c = buffer[i];
                        if (char.IsWhiteSpace(c))
                        {
                            pendingWhitespace = text.Length > 0;
                        }
                        else
                        {
                            if (pendingWhitespace)
                            {
                                text.Append(' ');
                                pendingWhitespace = false;
                            }

                            text.Append(c);
                        }

                        if (text.Length > maxLength)
                        {
                            throw Invalid(path + " is too long");
                        }
                    }
                }
                else
                {
                    text.Append(buffer, 0, read);
                    if (text.Length > maxLength)
                    {
                        throw Invalid(path + " is too long");
                    }
                }
            }

            reader.Read();
        }

        reader.Read();
        var result = text.ToString();
        return trim ? result.Trim() : result;
    }

    private static void ReadDocumentExtendedData(XmlReader reader, ref int definitions, ref DateTime? issuedAt, ref int timeSteps)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        reader.ReadStartElement();
        while (MoveToChildElement(reader))
        {
            if (reader.NamespaceURI == DwdNamespace && reader.LocalName == "ProductDefinition")
            {
                definitions++;
                if (definitions > 1)
                {
                    throw Invalid("dwd:ProductDefinition must occur exactly once");
                }

                (var issued, timeSteps) = ReadProductDefinition(reader);
                issuedAt = issued;
            }
            else
            {
                reader.Skip();
            }
        }

        reader.ReadEndElement();
    }

    private static (DateTime IssuedAt, int TimeSteps) ReadProductDefinition(XmlReader reader)
    {
        string? productId = null;
        string? issueTime = null;
        var productIds = 0;
        var issueTimes = 0;
        var stepLists = 0;
        var steps = 0;
        if (reader.IsEmptyElement)
        {
            reader.Read();
        }
        else
        {
            reader.ReadStartElement();
            while (MoveToChildElement(reader))
            {
                if (reader.NamespaceURI != DwdNamespace)
                {
                    reader.Skip();
                }
                else if (reader.LocalName == "ProductID")
                {
                    productIds++;
                    productId = ReadBoundedText(reader, MaxShortTextLength, "dwd:ProductID", trim: false);
                }
                else if (reader.LocalName == "IssueTime")
                {
                    issueTimes++;
                    issueTime = ReadBoundedText(reader, MaxShortTextLength, "dwd:IssueTime", trim: false);
                }
                else if (reader.LocalName == "ForecastTimeSteps")
                {
                    stepLists++;
                    steps = CountTimeSteps(reader);
                }
                else
                {
                    reader.Skip();
                }
            }

            reader.ReadEndElement();
        }

        if (productIds != 1)
        {
            throw Invalid("dwd:ProductID must occur exactly once");
        }

        if (productId != "MOSMIX")
        {
            throw Invalid("dwd:ProductID must be MOSMIX");
        }

        if (issueTimes != 1)
        {
            throw Invalid("dwd:IssueTime must occur exactly once");
        }

        var match = IssueTimePattern.Match(issueTime!);
        if (!match.Success || (match.Groups[2].Success && match.Groups[2].Value.Any(c => c != '0'))
            || !DateTime.TryParseExact(issueTime![..19], "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
        {
            throw Invalid("dwd:IssueTime is not a valid UTC time with a zero fractional part");
        }

        if (stepLists != 1)
        {
            throw Invalid("dwd:ForecastTimeSteps must occur exactly once");
        }

        if (steps < 1)
        {
            throw Invalid("dwd:ForecastTimeSteps must contain at least one dwd:TimeStep");
        }

        return (DateTime.SpecifyKind(parsed, DateTimeKind.Utc), steps);
    }

    private static int CountTimeSteps(XmlReader reader)
    {
        var steps = 0;
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return steps;
        }

        reader.ReadStartElement();
        while (MoveToChildElement(reader))
        {
            if (reader.NamespaceURI == DwdNamespace && reader.LocalName == "TimeStep")
            {
                steps++;
            }

            reader.Skip();
        }

        reader.ReadEndElement();
        return steps;
    }

    private static (string Id, IReadOnlyList<string> Available) ReadPlacemark(XmlReader reader, int timeSteps)
    {
        string? id = null;
        var nameCount = 0;
        var extendedDataCount = 0;
        var names = new HashSet<string>(StringComparer.Ordinal);
        var available = new List<string>();
        var forecasts = 0;
        if (reader.IsEmptyElement)
        {
            reader.Read();
        }
        else
        {
            reader.ReadStartElement();
            while (MoveToChildElement(reader))
            {
                if (reader.NamespaceURI == KmlNamespace && reader.LocalName == "name")
                {
                    nameCount++;
                    id = ReadBoundedText(reader, MaxShortTextLength, "kml:name", trim: true).Trim();
                }
                else if (reader.NamespaceURI == KmlNamespace && reader.LocalName == "ExtendedData")
                {
                    extendedDataCount++;
                    if (extendedDataCount > 1)
                    {
                        throw Invalid("kml:Placemark/kml:ExtendedData must occur at most once");
                    }

                    forecasts = ReadPlacemarkForecasts(reader, timeSteps, names, available);
                }
                else
                {
                    reader.Skip();
                }
            }

            reader.ReadEndElement();
        }

        if (nameCount != 1)
        {
            throw Invalid("kml:name must occur exactly once");
        }

        if (!IdPattern.IsMatch(id!))
        {
            throw Invalid("a placemark ID does not match ^[A-Z0-9]{4,5}$");
        }

        available.Sort(StringComparer.Ordinal);
        return (id!, available);
    }

    private static int ReadPlacemarkForecasts(XmlReader reader, int timeSteps, HashSet<string> names, List<string> available)
    {
        var forecasts = 0;
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return forecasts;
        }

        reader.ReadStartElement();
        while (MoveToChildElement(reader))
        {
            if (reader.NamespaceURI != DwdNamespace || reader.LocalName != "Forecast")
            {
                reader.Skip();
                continue;
            }

            forecasts++;
            if (forecasts > MaxForecastsPerPlacemark)
            {
                throw Invalid("a placemark has more than 200 forecasts");
            }

            var name = reader.GetAttribute("elementName", DwdNamespace);
            if (name is null || !NamePattern.IsMatch(name) || !names.Add(name))
            {
                throw Invalid("a forecast element name is missing, invalid or duplicated");
            }

            if (ReadForecastValue(reader, timeSteps, name))
            {
                available.Add(name);
            }
        }

        reader.ReadEndElement();
        return forecasts;
    }

    // Reads one dwd:Forecast: exactly one dwd:value whose whitespace-separated tokens are scanned incrementally (at most MaxTokenLength characters per token,
    // at most timeSteps tokens); returns true when the series has at least one numeric value.
    private static bool ReadForecastValue(XmlReader reader, int timeSteps, string name)
    {
        var values = 0;
        var hasValue = false;
        if (reader.IsEmptyElement)
        {
            reader.Read();
        }
        else
        {
            reader.ReadStartElement();
            while (MoveToChildElement(reader))
            {
                if (reader.NamespaceURI != DwdNamespace || reader.LocalName != "value")
                {
                    reader.Skip();
                    continue;
                }

                values++;
                if (values > 1)
                {
                    throw Invalid("dwd:value must occur exactly once");
                }

                hasValue = ScanTokens(reader, timeSteps, name);
            }

            reader.ReadEndElement();
        }

        if (values != 1)
        {
            throw Invalid("dwd:value must occur exactly once");
        }

        return hasValue;
    }

    private static bool ScanTokens(XmlReader reader, int timeSteps, string name)
    {
        var tokens = 0;
        var hasValue = false;
        var token = new StringBuilder(MaxTokenLength);
        var buffer = new char[4096];

        void Complete()
        {
            if (token.Length == 0)
            {
                return;
            }

            tokens++;
            if (tokens > timeSteps)
            {
                throw Invalid($"the series {name} does not have one token per time step");
            }

            var text = token.ToString();
            token.Clear();
            if (text == "-")
            {
                return;
            }

            if (!TokenPattern.IsMatch(text))
            {
                throw Invalid($"the series {name} contains an invalid token");
            }

            hasValue = true;
        }

        if (reader.IsEmptyElement)
        {
            reader.Read();
        }
        else
        {
            reader.Read();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType is not (XmlNodeType.Text or XmlNodeType.CDATA or XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace))
                {
                    throw Invalid("dwd:value must contain only text");
                }

                int read;
                while ((read = reader.ReadValueChunk(buffer, 0, buffer.Length)) > 0)
                {
                    for (var i = 0; i < read; i++)
                    {
                        if (char.IsWhiteSpace(buffer[i]))
                        {
                            Complete();
                        }
                        else
                        {
                            token.Append(buffer[i]);
                            if (token.Length > MaxTokenLength)
                            {
                                throw Invalid($"the series {name} contains an invalid token");
                            }
                        }
                    }
                }

                reader.Read();
            }

            reader.Read();
        }

        Complete();
        if (tokens != timeSteps)
        {
            throw Invalid($"the series {name} does not have one token per time step");
        }

        return hasValue;
    }

    private static CatalogFormatException Invalid(string reason) => new(CatalogFormatException.MosmixEvidenceInvalidErrorId, $"The MOSMIX_S all-stations file is invalid: {reason} (contracts SC-20, SC-22).");

    // Moves to the next child element of the current container; text, whitespace (also significant whitespace under xml:space="preserve")
    // and other non-element children are ignored as XElement.Elements() ignored them. Returns false at the container's end tag.
    private static bool MoveToChildElement(XmlReader reader)
    {
        // XmlNodeType.None (end of input) also ends the loop; XmlReader has already thrown for an unclosed container by then.
        while (reader.NodeType is not (XmlNodeType.Element or XmlNodeType.EndElement or XmlNodeType.None))
        {
            reader.Read();
        }

        return reader.NodeType == XmlNodeType.Element;
    }
}
