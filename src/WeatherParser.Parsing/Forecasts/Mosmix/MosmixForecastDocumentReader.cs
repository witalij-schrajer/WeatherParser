using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using WeatherParser.Parsing.Evidence.Kmz;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Reads MOSMIX_L per-location and MOSMIX_S all-stations KMZ files securely into a validated raw forecast document (contracts MF-05 to MF-08): bounded archive and member, one .kml member compared by name only, CRC-32 verified, DTDs prohibited, no resolver, nothing written to disk.
/// </summary>
public sealed partial class MosmixForecastDocumentReader : IMosmixForecastDocumentReader
{
    private const string KmlNamespace = "http://www.opengis.net/kml/2.2";
    private const string DwdNamespace = "https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd";
    private const int MaxDepth = 32;
    private const int MaxShortTextLength = 256;
    private const int BufferSize = 81920;

    private static readonly Regex InstantPattern = new(@"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.([0-9]{1,3}))?Z\z", RegexOptions.CultureInvariant);

    private readonly MosmixForecastReaderLimits _limits;

    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixForecastDocumentReader"/> class.
    /// </summary>
    public MosmixForecastDocumentReader() : this(MosmixForecastReaderLimits.Contract)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixForecastDocumentReader"/> class.
    /// </summary>
    /// <param name="limits">The reader limits.</param>
    internal MosmixForecastDocumentReader(MosmixForecastReaderLimits limits)
    {
        _limits = limits ?? throw new ArgumentNullException(nameof(limits));
    }

    /// <inheritdoc />
    public Task<MosmixForecastDocument> ReadSingleLocationAsync(Stream kmz, string mosmixId, CancellationToken cancellationToken) => ReadAsync(kmz, mosmixId, single: true, cancellationToken);

    /// <inheritdoc />
    public Task<MosmixForecastDocument> ReadAllStationsAsync(Stream kmz, string mosmixId, CancellationToken cancellationToken) => ReadAsync(kmz, mosmixId, single: false, cancellationToken);

    private async Task<MosmixForecastDocument> ReadAsync(Stream kmz, string mosmixId, bool single, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(kmz);
        ArgumentNullException.ThrowIfNull(mosmixId);

        cancellationToken.ThrowIfCancellationRequested();

        var buffer = new MemoryStream();
        var readBuffer = new Memory<byte>(new byte[BufferSize]);
        long totalRead = 0;
        while (true)
        {
            var read = await kmz.ReadAsync(readBuffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
            if (totalRead > (single ? _limits.SingleCompressedBytes : _limits.AllStationsCompressedBytes))
            {
                throw TooLarge();
            }

            await buffer.WriteAsync(readBuffer[..read], cancellationToken);
        }

        return Parse(buffer.ToArray(), mosmixId, single, cancellationToken);
    }

    private MosmixForecastDocument Parse(byte[] bytes, string mosmixId, bool single, CancellationToken cancellationToken)
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
            Func<string, Exception> invalid = Invalid;
            using var crc = new KmzCrc32Stream(member, members[0].Crc32, invalid);
            using var counting = new KmzCountingStream(crc, single ? _limits.SingleUnpackedBytes : _limits.AllStationsUnpackedBytes, cancellationToken, _ => TooLarge());
            using var guarded = new KmzMarkupGuardStream(counting, MaxDepth, invalid);
            using var reader = XmlReader.Create(guarded, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, IgnoreWhitespace = true, IgnoreComments = true, IgnoreProcessingInstructions = true, NameTable = new KmzBoundedNameTable(invalid) });
            reader.MoveToContent();
            if (reader.LocalName != "kml" || reader.NamespaceURI != KmlNamespace)
            {
                throw Invalid("the root element must be kml:kml");
            }

            var documents = 0;
            DateTime? issuedAt = null;
            IReadOnlyList<DateTime>? timeline = null;
            var definitions = 0;
            var locations = new HashSet<string>(StringComparer.Ordinal);
            PlacemarkResult? kept = null;
            var placemarks = 0;
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
                        ReadDocumentExtendedData(reader, ref definitions, ref issuedAt, ref timeline);
                    }
                    else if (reader.NamespaceURI == KmlNamespace && reader.LocalName == "Placemark")
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (issuedAt is null)
                        {
                            throw Invalid("a placemark must not appear before the product definition");
                        }

                        placemarks++;
                        var placemark = ReadPlacemark(reader, timeline!.Count, single, mosmixId);
                        if (single)
                        {
                            if (kept != null)
                            {
                                throw Invalid("a MOSMIX_L file must contain exactly one placemark");
                            }

                            kept = placemark;
                        }
                        else
                        {
                            if (!locations.Add(placemark.Id))
                            {
                                throw Invalid("a placemark ID occurs more than once");
                            }

                            if (locations.Count > _limits.MaxPlacemarks)
                            {
                                throw Invalid("the file contains too many placemarks");
                            }

                            if (placemark.Id == mosmixId)
                            {
                                kept = placemark;
                            }
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

            if (placemarks == 0)
            {
                throw Invalid("the file contains no placemark");
            }

            // Read the member to its end: XmlReader rejects any further root or malformed suffix, and the counting stream sees every byte.
            reader.ReadEndElement();
            while (reader.Read())
            {
            }

            var drain = new byte[BufferSize];
            while (guarded.Read(drain, 0, drain.Length) > 0)
            {
            }

            if (single && kept!.Id != mosmixId)
            {
                throw new MosmixForecastFormatException(MosmixForecastErrorIds.LocationMismatch, "The MOSMIX_L file belongs to another location (contract MF-07).");
            }

            if (!single && kept is null)
            {
                throw new MosmixForecastFormatException(MosmixForecastErrorIds.IdUnknown, "The requested MOSMIX location is not in the all-stations file (contract MF-07).");
            }

            return ToDocument(kept!, issuedAt.Value, timeline!);
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

    private void ReadDocumentExtendedData(XmlReader reader, ref int definitions, ref DateTime? issuedAt, ref IReadOnlyList<DateTime>? timeline)
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

                (var issued, timeline) = ReadProductDefinition(reader);
                issuedAt = issued;
            }
            else
            {
                reader.Skip();
            }
        }

        reader.ReadEndElement();
    }

    private (DateTime IssuedAt, IReadOnlyList<DateTime> Timeline) ReadProductDefinition(XmlReader reader)
    {
        string? productId = null;
        string? issueTime = null;
        var productIds = 0;
        var issueTimes = 0;
        var stepLists = 0;
        var issuerCount = 0;
        var generatingProcessCount = 0;
        var metElementDefinitionCount = 0;
        var formatCfgCount = 0;
        var defaultUndefSignCount = 0;
        var referencedModelCount = 0;
        var timeSteps = new List<DateTime>();

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
                    if (productIds > 1)
                    {
                        throw Invalid("dwd:ProductID must occur at most once");
                    }

                    productId = ReadBoundedText(reader, MaxShortTextLength, "dwd:ProductID", trim: false);
                }
                else if (reader.LocalName == "IssueTime")
                {
                    issueTimes++;
                    if (issueTimes > 1)
                    {
                        throw Invalid("dwd:IssueTime must occur exactly once");
                    }

                    issueTime = ReadBoundedText(reader, MaxShortTextLength, "dwd:IssueTime", trim: false);
                }
                else if (reader.LocalName == "ForecastTimeSteps")
                {
                    stepLists++;
                    if (stepLists > 1)
                    {
                        throw Invalid("dwd:ForecastTimeSteps must occur exactly once");
                    }

                    timeSteps = ReadTimeSteps(reader);
                }
                else if (reader.LocalName == "Issuer")
                {
                    issuerCount++;
                    if (issuerCount > 1)
                    {
                        throw Invalid("dwd:Issuer must occur at most once");
                    }

                    ReadBoundedText(reader, MaxShortTextLength, "dwd:Issuer", trim: false);
                }
                else if (reader.LocalName == "GeneratingProcess")
                {
                    generatingProcessCount++;
                    if (generatingProcessCount > 1)
                    {
                        throw Invalid("dwd:GeneratingProcess must occur at most once");
                    }

                    ReadBoundedText(reader, MaxShortTextLength, "dwd:GeneratingProcess", trim: false);
                }
                else if (reader.LocalName == "MetElementDefinition")
                {
                    metElementDefinitionCount++;
                    if (metElementDefinitionCount > 1)
                    {
                        throw Invalid("dwd:MetElementDefinition must occur at most once");
                    }

                    ReadBoundedText(reader, MaxShortTextLength, "dwd:MetElementDefinition", trim: false);
                }
                else if (reader.LocalName == "FormatCfg")
                {
                    formatCfgCount++;
                    if (formatCfgCount > 1)
                    {
                        throw Invalid("dwd:FormatCfg must occur at most once");
                    }

                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                    }
                    else
                    {
                        reader.ReadStartElement();
                        while (MoveToChildElement(reader))
                        {
                            if (reader.NamespaceURI == DwdNamespace && reader.LocalName == "DefaultUndefSign")
                            {
                                defaultUndefSignCount++;
                                if (defaultUndefSignCount > 1)
                                {
                                    throw Invalid("dwd:DefaultUndefSign must occur at most once");
                                }

                                var value = ReadBoundedText(reader, MaxShortTextLength, "dwd:FormatCfg/dwd:DefaultUndefSign", trim: false);
                                if (value != "-")
                                {
                                    throw Invalid("dwd:DefaultUndefSign must be '-'");
                                }
                            }
                            else
                            {
                                reader.Skip();
                            }
                        }

                        reader.ReadEndElement();
                    }
                }
                else if (reader.LocalName == "ReferencedModel")
                {
                    referencedModelCount++;
                    if (referencedModelCount > 1)
                    {
                        throw Invalid("dwd:ReferencedModel must occur at most once");
                    }

                    ReadReferencedModel(reader);
                }
                else
                {
                    // All other children are not used (contract MF-07).
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
            throw new MosmixForecastFormatException(MosmixForecastErrorIds.IssueTimeMissing, "The MOSMIX forecast file has no dwd:IssueTime (contract MF-07).");
        }

        var parsed = ParseInstant(issueTime!, "dwd:IssueTime");

        if (stepLists != 1)
        {
            throw Invalid("dwd:ForecastTimeSteps must occur exactly once");
        }

        if (timeSteps.Count < 1)
        {
            throw Invalid("dwd:ForecastTimeSteps must contain at least one dwd:TimeStep");
        }

        return (DateTime.SpecifyKind(parsed, DateTimeKind.Utc), timeSteps);
    }

    private void ReadReferencedModel(XmlReader reader)
    {
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        reader.ReadStartElement();
        var buffer = new char[MaxShortTextLength];
        var totalLength = 0;
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (reader.NodeType == XmlNodeType.Element && reader.NamespaceURI == DwdNamespace && reader.LocalName == "Model")
            {
                ReadBoundedText(reader, MaxShortTextLength, "dwd:Model");
            }
            else if (reader.NodeType == XmlNodeType.Element)
            {
                // Other elements are not used.
                reader.Skip();
            }
            else
            {
                // Only text-like nodes remain under the reader settings (comments, processing instructions and DTDs are excluded).
                int read;
                while ((read = reader.ReadValueChunk(buffer, 0, buffer.Length)) > 0)
                {
                    totalLength += read;
                    if (totalLength > MaxShortTextLength)
                    {
                        throw Invalid("dwd:ReferencedModel is too long");
                    }
                }

                reader.Read();
            }
        }

        reader.ReadEndElement();
    }

    private List<DateTime> ReadTimeSteps(XmlReader reader)
    {
        var timeSteps = new List<DateTime>();
        if (reader.IsEmptyElement)
        {
            reader.Read();
            return timeSteps;
        }

        reader.ReadStartElement();
        while (MoveToChildElement(reader))
        {
            if (reader.NamespaceURI == DwdNamespace && reader.LocalName == "TimeStep")
            {
                if (timeSteps.Count >= _limits.MaxTimeSteps)
                {
                    throw Invalid("dwd:ForecastTimeSteps must contain at most the bounded number of steps");
                }

                var text = ReadBoundedText(reader, MaxShortTextLength, "dwd:TimeStep", trim: false);
                var step = ParseInstant(text, "dwd:TimeStep");

                if (timeSteps.Count > 0 && step <= timeSteps[^1])
                {
                    throw Invalid("dwd:ForecastTimeSteps must be strictly ascending");
                }

                timeSteps.Add(step);
            }
            else
            {
                reader.Skip();
            }
        }

        reader.ReadEndElement();
        return timeSteps;
    }

    private static DateTime ParseInstant(string text, string path)
    {
        var match = InstantPattern.Match(text);
        if (!match.Success || (match.Groups[2].Success && match.Groups[2].Value.Any(c => c != '0'))
            || !DateTime.TryParseExact(text[..19], "yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
        {
            throw Invalid(path + " is not a valid UTC time with a zero fractional part");
        }

        return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
    }

    private static MosmixForecastFormatException Invalid(string reason) => new(MosmixForecastErrorIds.FormatInvalid, $"The MOSMIX forecast file is invalid: {reason} (contract MF-07).");

    private static MosmixForecastFormatException TooLarge() => new(MosmixForecastErrorIds.TooLarge, "The MOSMIX forecast archive or its .kml member exceeds its size bound (contract MF-05).");

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
