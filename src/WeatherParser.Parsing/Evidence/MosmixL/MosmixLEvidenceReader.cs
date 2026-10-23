using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Evidence.MosmixL;

/// <summary>
/// Reads MOSMIX_L single-station evidence files (SC-20).
/// </summary>
public sealed class MosmixLEvidenceReader : IMosmixLEvidenceReader
{
    private const int MaxCompressedBytes = 1048576;
    private const int MaxUncompressedBytes = 16777216;
    private const int BufferSize = 81920;

    private static readonly Regex IssueTimePattern = new(@"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.([0-9]{1,3}))?Z\z", RegexOptions.CultureInvariant);
    private static readonly Regex TokenPattern = new(@"\A-?[0-9]{1,9}(\.[0-9]{1,6})?\z", RegexOptions.CultureInvariant);
    private const string KmlNamespace = "http://www.opengis.net/kml/2.2";
    private const string DwdNamespace = "https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd";

    /// <inheritdoc />
    public async Task<MosmixLEvidence> ReadAsync(Stream archive, string mosmixId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentNullException.ThrowIfNull(mosmixId);
        XNamespace Kml = KmlNamespace;
        XNamespace Dwd = DwdNamespace;
        cancellationToken.ThrowIfCancellationRequested();

        using var compressed = await ReadBoundedAsync(archive, MaxCompressedBytes, cancellationToken) ?? throw Invalid("the archive exceeds 1 MiB");

        ZipArchive zip;
        try
        {
            zip = new ZipArchive(compressed, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (InvalidDataException)
        {
            throw Invalid("the archive is not a valid ZIP archive");
        }

        using var zipScope = zip;

        // ZipArchive reads the central directory lazily; its corruption surfaces here (contract SC-20).
        List<ZipArchiveEntry> kmlEntries;
        try
        {
            kmlEntries = zip.Entries.Where(e => e.FullName.EndsWith(".kml", StringComparison.OrdinalIgnoreCase)).ToList();
        }
        catch (InvalidDataException)
        {
            throw Invalid("the archive is not a valid ZIP archive");
        }
        if (kmlEntries.Count != 1)
        {
            throw Invalid("the archive must contain exactly one .kml member");
        }

        var entry = kmlEntries[0];
        MemoryStream? copied;
        try
        {
            using var member = entry.Open();
            copied = await ReadBoundedAsync(member, MaxUncompressedBytes, cancellationToken);
        }
        catch (InvalidDataException)
        {
            throw Invalid("the .kml member is corrupt");
        }

        using var uncompressed = copied ?? throw Invalid("the .kml member exceeds 16 MiB");

        XDocument document;
        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var reader = XmlReader.Create(uncompressed, settings);
            document = XDocument.Load(reader);
        }
        catch (XmlException)
        {
            throw Invalid("the KML is not well-formed or contains a DTD");
        }

        var root = document.Root;
        if (root!.Name != Kml + "kml")
        {
            throw Invalid("the root element must be kml:kml");
        }

        var documents = root.Elements(Kml + "Document").ToList();
        if (documents.Count != 1)
        {
            throw Invalid("kml:Document must occur exactly once");
        }

        var documentNode = documents[0];
        var definitions = documentNode.Elements(Kml + "ExtendedData").Elements(Dwd + "ProductDefinition").ToList();
        if (definitions.Count != 1)
        {
            throw Invalid("kml:Document/kml:ExtendedData/dwd:ProductDefinition must occur exactly once");
        }

        var definition = definitions[0];

        var productIds = definition.Elements(Dwd + "ProductID").ToList();
        if (productIds.Count != 1 || productIds[0].Value != "MOSMIX")
        {
            throw Invalid("kml:Document/kml:ExtendedData/dwd:ProductDefinition/dwd:ProductID must occur exactly once and have value 'MOSMIX'");
        }

        var issueTimes = definition.Elements(Dwd + "IssueTime").ToList();
        if (issueTimes.Count != 1)
        {
            throw Invalid("kml:Document/kml:ExtendedData/dwd:ProductDefinition/dwd:IssueTime must occur exactly once");
        }

        var issueTimeValue = issueTimes[0].Value;
        if (!IssueTimePattern.IsMatch(issueTimeValue))
        {
            throw Invalid("kml:Document/kml:ExtendedData/dwd:ProductDefinition/dwd:IssueTime must be a valid ISO 8601 date-time with optional fractional seconds");
        }

        var fractionGroup = issueTimeValue.IndexOf('.');
        if (fractionGroup != -1)
        {
            var fraction = issueTimeValue.Substring(fractionGroup + 1, issueTimeValue.Length - fractionGroup - 2);
            if (!fraction.All(c => c == '0'))
            {
                throw Invalid("kml:Document/kml:ExtendedData/dwd:ProductDefinition/dwd:IssueTime fractional seconds must be all zeros");
            }
        }

        if (!DateTime.TryParseExact(issueTimeValue[..19], "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var issued))
        {
            throw Invalid("kml:Document/kml:ExtendedData/dwd:ProductDefinition/dwd:IssueTime must be a valid ISO 8601 date-time");
        }

        var issuedUtc = DateTime.SpecifyKind(issued, DateTimeKind.Utc);

        var forecastTimeSteps = definition.Elements(Dwd + "ForecastTimeSteps").ToList();
        if (forecastTimeSteps.Count != 1)
        {
            throw Invalid("kml:Document/kml:ExtendedData/dwd:ProductDefinition/dwd:ForecastTimeSteps must occur exactly once");
        }

        var timeSteps = forecastTimeSteps[0].Elements(Dwd + "TimeStep").ToList();
        if (timeSteps.Count < 1)
        {
            throw Invalid("kml:Document/kml:ExtendedData/dwd:ProductDefinition/dwd:ForecastTimeSteps/dwd:TimeStep must occur at least once");
        }

        var placemarks = documentNode.Elements(Kml + "Placemark").ToList();
        if (placemarks.Count != 1)
        {
            throw Invalid("kml:Document/kml:Placemark must occur exactly once");
        }

        var placemark = placemarks[0];

        var names = placemark.Elements(Kml + "name").ToList();
        if (names.Count != 1 || names[0].Value.Trim() != mosmixId)
        {
            throw Invalid("kml:Document/kml:Placemark/kml:name must occur exactly once and match the requested MOSMIX ID");
        }

        var extendedData = placemark.Elements(Kml + "ExtendedData").ToList();
        if (extendedData.Count > 1)
        {
            throw Invalid("kml:Document/kml:Placemark/kml:ExtendedData must occur at most once");
        }

        var available = new HashSet<string>(StringComparer.Ordinal);

        if (extendedData.Count > 0)
        {
            var forecastElements = extendedData[0].Elements(Dwd + "Forecast").ToList();
            var elementNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var forecast in forecastElements)
            {
                var elementNameAttribute = forecast.Attribute(Dwd + "elementName");
                if (elementNameAttribute is null || string.IsNullOrEmpty(elementNameAttribute.Value))
                {
                    throw Invalid("kml:Document/kml:Placemark/kml:ExtendedData/dwd:Forecast must have a non-empty dwd:elementName attribute");
                }

                var elementName = elementNameAttribute.Value;
                if (!elementNames.Add(elementName))
                {
                    throw Invalid("kml:Document/kml:Placemark/kml:ExtendedData/dwd:Forecast elements must have unique names");
                }

                var values = forecast.Elements(Dwd + "value").ToList();
                if (values.Count != 1)
                {
                    throw Invalid("kml:Document/kml:Placemark/kml:ExtendedData/dwd:Forecast must have exactly one dwd:value element");
                }

                var value = values[0].Value;
                var tokens = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != timeSteps.Count)
                {
                    throw Invalid("kml:Document/kml:Placemark/kml:ExtendedData/dwd:Forecast dwd:value must have exactly as many tokens as there are time steps");
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
                        throw Invalid("kml:Document/kml:Placemark/kml:ExtendedData/dwd:Forecast dwd:value tokens must be valid numbers or '-'");
                    }

                    hasValue = true;
                }

                if (hasValue)
                {
                    available.Add(elementName);
                }
            }
        }

        return new MosmixLEvidence(mosmixId, issuedUtc, available.OrderBy(n => n, StringComparer.Ordinal).ToList());
    }

    private static async Task<MemoryStream?> ReadBoundedAsync(Stream source, int maxBytes, CancellationToken cancellationToken)
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
            if (total > maxBytes)
            {
                memory.Dispose();
                return null;
            }

            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        memory.Position = 0;
        return memory;
    }

    private static CatalogFormatException Invalid(string reason) => new(CatalogFormatException.MosmixEvidenceInvalidErrorId, $"The MOSMIX_L evidence file is invalid: {reason} (contract SC-20).");
}
