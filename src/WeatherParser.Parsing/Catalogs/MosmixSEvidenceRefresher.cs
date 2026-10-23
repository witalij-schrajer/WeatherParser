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
            using var counting = new CountingStream(member, _maxMemberBytes, cancellationToken);
            using var guarded = new MarkupGuardStream(counting);
            using var reader = XmlReader.Create(guarded, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, IgnoreWhitespace = true, IgnoreComments = true, IgnoreProcessingInstructions = true, NameTable = new BoundedNameTable() });
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

    private sealed class CountingStream : ReadOnlyStreamBase
    {
        private readonly Stream _inner;
        private readonly long _maxBytes;
        private readonly CancellationToken _cancellationToken;
        private long _total;

        public CountingStream(Stream inner, long maxBytes, CancellationToken cancellationToken)
        {
            _inner = inner;
            _maxBytes = maxBytes;
            _cancellationToken = cancellationToken;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var read = _inner.Read(buffer, offset, count);
            _total += read;
            if (_total > _maxBytes)
            {
                throw Invalid("the unpacked .kml member exceeds the size bound");
            }

            return read;
        }
    }

    // Bounds the length of every markup construct (a tag with its attributes, a CDATA section, a comment, a processing instruction or a declaration)
    // in the unpacked member before XmlReader materializes it, so no single XML token can allocate more than MaxMarkupBytes (SC-22 streaming bound).
    // Text content stays unbounded here; the parser reads it in chunks. '<', '>', quotes, '!', '?', '-' and ']' are single bytes in UTF-8 and ISO-8859-1.
    private sealed class MarkupGuardStream : ReadOnlyStreamBase
    {
        private const int MaxMarkupBytes = 1048576;
        private const int Text = 0;
        private const int AfterOpen = 1;
        private const int AfterBang = 2;
        private const int AfterBangDash = 3;
        private const int Tag = 4;
        private const int Cdata = 5;
        private const int Comment = 6;
        private const int Instruction = 7;

        private readonly Stream _inner;
        private int _state = Text;
        private long _length;
        private byte _quote;
        private int _closing;
        private int _prefixState;
        private int _bomBytes;
        private int _depth;
        private bool _endTag;
        private byte _previous;

        public MarkupGuardStream(Stream inner)
        {
            _inner = inner;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, count);
            for (var i = offset; i < offset + read; i++)
            {
                Observe(buffer[i]);
            }

            return read;
        }

        private static ReadOnlySpan<byte> Utf8Bom => new byte[] { 0xEF, 0xBB, 0xBF };

        private void Observe(byte value)
        {
            if (_prefixState == 0)
            {
                // Optional UTF-8 byte order mark, then optional ASCII whitespace, then '<'.
                if (_bomBytes < 3 && value == Utf8Bom[_bomBytes] && (_bomBytes > 0 || _length == 0))
                {
                    _bomBytes++;
                    return;
                }

                if (value is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
                {
                    return;
                }

                if (value != (byte)'<' || (_bomBytes is not (0 or 3)))
                {
                    throw Invalid("the .kml member must be encoded in UTF-8 or ISO-8859-1");
                }

                _prefixState = 1;
                _state = AfterOpen;
                _length = 1;
                _endTag = false;
                _previous = 0;
                return;
            }
            else if (_state == Text)
            {
                if (value == (byte)'<')
                {
                    _state = AfterOpen;
                    _length = 1;
                    _endTag = false;
                    _previous = 0;
                }

                return;
            }

            _length++;
            if (_length > MaxMarkupBytes)
            {
                throw Invalid("a tag, CDATA section, comment or processing instruction exceeds 1 MiB");
            }

            switch (_state)
            {
                case AfterOpen:
                    if (value == 0)
                    {
                        throw Invalid("the .kml member must be encoded in UTF-8 or ISO-8859-1");
                    }
                    _endTag = value == (byte)'/';
                    _state = value switch
                    {
                        (byte)'!' => AfterBang,
                        (byte)'?' => Instruction,
                        _ => Tag,
                    };
                    ObserveTag(value);
                    break;
                case AfterBang:
                    _state = value switch
                    {
                        (byte)'-' => AfterBangDash,
                        (byte)'[' => Cdata,
                        _ => Tag,
                    };
                    _closing = 0;
                    ObserveTag(value);
                    break;
                case AfterBangDash:
                    _state = value == (byte)'-' ? Comment : Tag;
                    _closing = 0;
                    ObserveTag(value);
                    break;
                case Tag:
                    ObserveTag(value);
                    break;
                case Cdata:
                    ObserveTerminator(value, (byte)']');
                    break;
                case Comment:
                    ObserveTerminator(value, (byte)'-');
                    break;
                default:
                    // Instruction: ends at "?>".
                    if (value == (byte)'>' && _closing == 1)
                    {
                        _state = Text;
                    }

                    _closing = value == (byte)'?' ? 1 : 0;
                    break;
            }
        }

        private void ObserveTag(byte value)
        {
            if (_state != Tag)
            {
                return;
            }

            if (_quote != 0)
            {
                if (value == _quote)
                {
                    _quote = 0;
                }
            }
            else if (value == (byte)'"' || value == (byte)'\'')
            {
                _quote = value;
            }
            else if (value == (byte)'>')
            {
                // Element depth (SC-22 streaming bound): a start tag opens a level, an end tag closes one, a self-closing tag ("/>") neither.
                if (_endTag)
                {
                    _depth--;
                }
                else if (_previous != (byte)'/')
                {
                    _depth++;
                    if (_depth > MaxDepth)
                    {
                        throw Invalid($"the KML is nested deeper than {MaxDepth} elements");
                    }
                }

                _state = Text;
            }
            else
            {
                _previous = value;
            }
        }

        // CDATA ends at "]]>", a comment at "-->": two marker bytes followed by '>'.
        private void ObserveTerminator(byte value, byte marker)
        {
            if (value == (byte)'>' && _closing >= 2)
            {
                _state = Text;
                _closing = 0;
            }
            else if (value == marker)
            {
                _closing = Math.Min(_closing + 1, 2);
            }
            else
            {
                _closing = 0;
            }
        }
    }

    // Bounds the total length of all distinct names the reader interns (element, attribute and prefix names and namespace URIs, also of skipped elements),
    // so that many distinct names cannot accumulate beyond the streaming memory bound (SC-22).
    private sealed class BoundedNameTable : XmlNameTable
    {
        private const int MaxNameCharacters = 1048576;
        private readonly NameTable _inner = new();
        private long _characters;

        public override string Add(char[] key, int start, int len)
        {
            if (Get(key, start, len) is { } existing)
            {
                return existing;
            }

            Count(len);
            return _inner.Add(key, start, len);
        }

        public override string Add(string key)
        {
            if (Get(key) is { } existing)
            {
                return existing;
            }

            Count(key.Length);
            return _inner.Add(key);
        }

        public override string? Get(char[] key, int start, int len) => _inner.Get(key, start, len);

        public override string? Get(string value) => _inner.Get(value);

        private void Count(int length)
        {
            _characters += length;
            if (_characters > MaxNameCharacters)
            {
                throw Invalid("the distinct XML names exceed 1048576 characters");
            }
        }
    }
}
