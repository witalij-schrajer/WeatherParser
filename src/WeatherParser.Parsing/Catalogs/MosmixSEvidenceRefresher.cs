using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
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
        XNamespace kml = KmlNamespace;
        XNamespace dwd = DwdNamespace;
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
            using var reader = XmlReader.Create(counting, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, IgnoreWhitespace = true, IgnoreComments = true });
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
            while (reader.NodeType == XmlNodeType.Element)
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
                while (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.NamespaceURI == KmlNamespace && reader.LocalName == "ExtendedData")
                    {
                        var extendedData = (XElement)XNode.ReadFrom(reader);
                        foreach (var definition in extendedData.Elements(dwd + "ProductDefinition"))
                        {
                            definitions++;
                            if (definitions > 1)
                            {
                                throw Invalid("dwd:ProductDefinition must occur exactly once");
                            }

                            (var issued, timeSteps) = ReadProductDefinition(definition, dwd);
                            issuedAt = issued;
                        }
                    }
                    else if (reader.NamespaceURI == KmlNamespace && reader.LocalName == "Placemark")
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var placemark = (XElement)XNode.ReadFrom(reader);
                        if (issuedAt is null)
                        {
                            throw Invalid("a placemark must not appear before the product definition");
                        }

                        var (id, available) = ReadPlacemark(placemark, timeSteps, kml, dwd);
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

    private static XElement Single(XElement parent, XName name, string path)
    {
        var elements = parent.Elements(name).ToList();
        if (elements.Count != 1)
        {
            throw Invalid(path + " must occur exactly once");
        }

        return elements[0];
    }

    private static (DateTime IssuedAt, int TimeSteps) ReadProductDefinition(XElement definition, XNamespace dwd)
    {
        if (Single(definition, dwd + "ProductID", "dwd:ProductID").Value != "MOSMIX")
        {
            throw Invalid("dwd:ProductID must be MOSMIX");
        }

        var issueTime = Single(definition, dwd + "IssueTime", "dwd:IssueTime").Value;
        var match = IssueTimePattern.Match(issueTime);
        if (!match.Success || (match.Groups[2].Success && match.Groups[2].Value.Any(c => c != '0'))
            || !DateTime.TryParseExact(issueTime[..19], "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
        {
            throw Invalid("dwd:IssueTime is not a valid UTC time with a zero fractional part");
        }

        var steps = Single(definition, dwd + "ForecastTimeSteps", "dwd:ForecastTimeSteps").Elements(dwd + "TimeStep").Count();
        if (steps < 1)
        {
            throw Invalid("dwd:ForecastTimeSteps must contain at least one dwd:TimeStep");
        }

        return (DateTime.SpecifyKind(parsed, DateTimeKind.Utc), steps);
    }

    private static (string Id, IReadOnlyList<string> Available) ReadPlacemark(XElement placemark, int timeSteps, XNamespace kml, XNamespace dwd)
    {
        var id = Single(placemark, kml + "name", "kml:name").Value.Trim();
        if (!IdPattern.IsMatch(id))
        {
            throw Invalid("a placemark ID does not match ^[A-Z0-9]{4,5}$");
        }

        var extendedData = placemark.Elements(kml + "ExtendedData").ToList();
        if (extendedData.Count > 1)
        {
            throw Invalid("kml:Placemark/kml:ExtendedData must occur at most once");
        }

        var forecasts = extendedData.Count == 0 ? new List<XElement>() : extendedData[0].Elements(dwd + "Forecast").ToList();
        if (forecasts.Count > MaxForecastsPerPlacemark)
        {
            throw Invalid($"the placemark {id} has more than 200 forecasts");
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        var available = new List<string>();
        foreach (var forecast in forecasts)
        {
            var name = (string?)forecast.Attribute(dwd + "elementName");
            if (name is null || !NamePattern.IsMatch(name) || !names.Add(name))
            {
                throw Invalid($"a forecast element name of placemark {id} is missing, invalid or duplicated");
            }

            var tokens = Single(forecast, dwd + "value", "dwd:value").Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != timeSteps)
            {
                throw Invalid($"the series {name} of placemark {id} does not have one token per time step");
            }

            var hasValue = false;
            foreach (var token in tokens)
            {
                if (token == "-")
                {
                    continue;
                }

                if (!TokenPattern.IsMatch(token))
                {
                    throw Invalid($"the series {name} of placemark {id} contains an invalid token");
                }

                hasValue = true;
            }

            if (hasValue)
            {
                available.Add(name);
            }
        }

        available.Sort(StringComparer.Ordinal);
        return (id, available);
    }

    private static CatalogFormatException Invalid(string reason) => new(CatalogFormatException.MosmixEvidenceInvalidErrorId, $"The MOSMIX_S all-stations file is invalid: {reason} (contracts SC-20, SC-22).");

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
}
