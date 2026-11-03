using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class MosmixSEvidenceRefresherStreamingStructureTests
{
    private MockRepository _mocks = null!;
    private Mock<ICatalogDownloader> _downloader = null!;
    private Mock<IMosmixEvidenceCache> _cache = null!;
    private ManualTimeProvider _time = null!;
    private MosmixCatalogEndpoints _options = null!;
    private MosmixSEvidenceRefresher _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _downloader = _mocks.Create<ICatalogDownloader>();
        _cache = _mocks.Create<IMosmixEvidenceCache>();
        _time = new(new DateTimeOffset(2026, 10, 23, 4, 12, 7, 500, TimeSpan.Zero));
        _options = new()
        {
            CatalogUri = new("https://example.org/mosmix/stations.cfg"),
            MosmixLSingleStationsDirectory = new("https://example.org/mosmix/single_stations/"),
            MosmixSAllStationsUri = new("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz?mirror=1"),
            EvidenceCacheDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-evidence-test"),
            MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
            MosmixSRefreshDeadline = TimeSpan.FromMinutes(10)
        };
        var validator = new MosmixCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator()));
        _subject = new(_options, validator, _downloader.Object, _cache.Object, _time);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private void ExpectDownload(byte[] kmz, Action? onDownload = null) =>
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == "MosmixSAllStationsUri" && r.Uri.OriginalString == "https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz?mirror=1" && r.MaxBytes == CatalogRequest.MosmixSAllStationsMaxBytes && !r.NotFoundIsAbsent && r.DeadlineKind == CatalogDeadlineKind.Refresh),
                It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (CatalogRequest r, Func<Stream, CancellationToken, Task<byte[]>> read, CancellationToken token) =>
            {
                var bytes = await read(new MemoryStream(kmz), token);
                onDownload?.Invoke();
                return bytes;
            });

    private const string Dwd = "https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd";
    private const string Kml = "http://www.opengis.net/kml/2.2";
    private const string Steps = "<dwd:ForecastTimeSteps><dwd:TimeStep>2026-10-23T04:00:00.000Z</dwd:TimeStep><dwd:TimeStep>2026-10-23T05:00:00.000Z</dwd:TimeStep><dwd:TimeStep>2026-10-23T06:00:00.000Z</dwd:TimeStep></dwd:ForecastTimeSteps>";
    private const string DefinitionInner = "<dwd:ProductID>MOSMIX</dwd:ProductID><dwd:IssueTime>2026-10-23T03:00:00.000Z</dwd:IssueTime>" + Steps;
    private const string PlacemarkInner = "<kml:name>X123A</kml:name><kml:ExtendedData><dwd:Forecast dwd:elementName=\"TTT\"><dwd:value>1.00 - -</dwd:value></dwd:Forecast></kml:ExtendedData>";

    private static string Definition(string inner) => "<kml:ExtendedData><dwd:ProductDefinition>" + inner + "</dwd:ProductDefinition></kml:ExtendedData>";
    private static string Placemark(string inner) => "<kml:Placemark>" + inner + "</kml:Placemark>";
    private static byte[] Document(string body) => MosmixSKmzBuilder.Kmz("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?><kml:kml xmlns:dwd=\"" + Dwd + "\" xmlns:kml=\"" + Kml + "\"><kml:Document>" + body + "</kml:Document></kml:kml>");

    private async Task ShouldBeInvalid(byte[] kmz, string messagePart)
    {
        ExpectDownload(kmz);
        var failure = (await FluentActions.Awaiting(() => _subject.RefreshAsync(CancellationToken.None)).Should().ThrowAsync<CatalogFormatException>()).Which;
        failure.ErrorId.Should().Be(CatalogFormatException.MosmixEvidenceInvalidErrorId);
        failure.Message.Should().Contain(messagePart);
    }

    private async Task<MosmixSEvidenceIndex> ShouldBeValid(byte[] kmz)
    {
        ExpectDownload(kmz);
        MosmixSEvidenceIndex? written = null;
        _cache.Setup(c => c.WriteAsync(It.IsAny<MosmixSEvidenceIndex>(), It.IsAny<CancellationToken>()))
            .Callback<MosmixSEvidenceIndex, CancellationToken>((index, _) => written = index)
            .Returns(Task.CompletedTask);
        await _subject.RefreshAsync(CancellationToken.None);
        return written!;
    }

    private static byte[] KmzFromBytes(byte[] member)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var entry = archive.CreateEntry("MOSMIX_S_2026102303_240.kml", CompressionLevel.Optimal).Open();
            entry.Write(member, 0, member.Length);
        }

        return stream.ToArray();
    }

    private static string Body() => "<kml:kml xmlns:dwd=\"" + Dwd + "\" xmlns:kml=\"" + Kml + "\"><kml:Document>" + Definition(DefinitionInner) + Placemark(PlacemarkInner) + "</kml:Document></kml:kml>";

    private static string Nested(int levels) => string.Concat(Enumerable.Repeat("<x>", levels)) + string.Concat(Enumerable.Repeat("</x>", levels));

    private static byte[] DocumentWithGaps(string rootAttributes, string gap)
    {
        var body = "<kml:kml" + rootAttributes + " xmlns:dwd=\"" + Dwd + "\" xmlns:kml=\"" + Kml + "\"><kml:Document>" + Definition(DefinitionInner) + Placemark(PlacemarkInner) + "</kml:Document></kml:kml>";
        return MosmixSKmzBuilder.Kmz("<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?>" + body.Replace("><", ">" + gap + "<"));
    }

    [TestMethod]
    public async Task EmptyName_IsInvalid()
    {
        var kmz = Document(Definition(DefinitionInner) + Placemark("<kml:name/>" + "<kml:ExtendedData/>"));
        await ShouldBeInvalid(kmz, "does not match");
    }

    [TestMethod]
    public async Task NameWithChildElement_IsInvalid()
    {
        var kmz = Document(Definition(DefinitionInner) + Placemark("<kml:name>X1<kml:b>3A</kml:b></kml:name>"));
        await ShouldBeInvalid(kmz, "kml:name must contain only text");
    }

    [TestMethod]
    public async Task EmptyDocumentExtendedDataBesideTheDefinition_IsIgnored()
    {
        var kmz = Document("<kml:ExtendedData/>" + Definition(DefinitionInner) + Placemark(PlacemarkInner));
        var index = await ShouldBeValid(kmz);
        index.Locations["X123A"].Should().ContainSingle().Which.Should().Be("TTT");
    }

    [TestMethod]
    public async Task EmptyProductDefinition_IsInvalid()
    {
        var kmz = Document("<kml:ExtendedData><dwd:ProductDefinition/></kml:ExtendedData>" + Placemark(PlacemarkInner));
        await ShouldBeInvalid(kmz, "dwd:ProductID must occur exactly once");
    }

    [TestMethod]
    public async Task ForeignElementInProductDefinition_IsIgnored()
    {
        var kmz = Document(Definition("<kml:foo>x</kml:foo>" + DefinitionInner) + Placemark(PlacemarkInner));
        var index = await ShouldBeValid(kmz);
        index.Locations["X123A"].Should().ContainSingle().Which.Should().Be("TTT");
    }

    [TestMethod]
    public async Task DuplicateIssueTime_IsInvalid()
    {
        var kmz = Document(Definition(DefinitionInner + "<dwd:IssueTime>2026-10-23T03:00:00.000Z</dwd:IssueTime>") + Placemark(PlacemarkInner));
        await ShouldBeInvalid(kmz, "dwd:IssueTime must occur exactly once");
    }

    [TestMethod]
    public async Task MissingIssueTime_IsInvalid()
    {
        var kmz = Document(Definition("<dwd:ProductID>MOSMIX</dwd:ProductID>" + Steps) + Placemark(PlacemarkInner));
        await ShouldBeInvalid(kmz, "dwd:IssueTime must occur exactly once");
    }

    [TestMethod]
    public async Task EmptyForecastTimeSteps_IsInvalid()
    {
        var kmz = Document(Definition("<dwd:ProductID>MOSMIX</dwd:ProductID><dwd:IssueTime>2026-10-23T03:00:00.000Z</dwd:IssueTime><dwd:ForecastTimeSteps/>") + Placemark(PlacemarkInner));
        await ShouldBeInvalid(kmz, "at least one dwd:TimeStep");
    }

    [TestMethod]
    public async Task EmptyPlacemark_IsInvalid()
    {
        var kmz = Document(Definition(DefinitionInner) + "<kml:Placemark/>");
        await ShouldBeInvalid(kmz, "kml:name must occur exactly once");
    }

    [TestMethod]
    public async Task EmptyPlacemarkExtendedData_GivesLocationWithoutElements()
    {
        var kmz = Document(Definition(DefinitionInner) + Placemark("<kml:name>X123A</kml:name><kml:ExtendedData/>"));
        var index = await ShouldBeValid(kmz);
        index.Locations["X123A"].Should().BeEmpty();
    }

    [TestMethod]
    public async Task ForeignElementInPlacemarkExtendedData_IsIgnored()
    {
        var kmz = Document(Definition(DefinitionInner) + Placemark("<kml:name>X123A</kml:name><kml:ExtendedData><kml:Data>x</kml:Data><dwd:Forecast dwd:elementName=\"TTT\"><dwd:value>1.00 - -</dwd:value></dwd:Forecast></kml:ExtendedData>"));
        var index = await ShouldBeValid(kmz);
        index.Locations["X123A"].Should().ContainSingle().Which.Should().Be("TTT");
    }

    [TestMethod]
    public async Task EmptyForecast_IsInvalid()
    {
        var kmz = Document(Definition(DefinitionInner) + Placemark("<kml:name>X123A</kml:name><kml:ExtendedData><dwd:Forecast dwd:elementName=\"TTT\"/></kml:ExtendedData>"));
        await ShouldBeInvalid(kmz, "dwd:value must occur exactly once");
    }

    [TestMethod]
    public async Task ForeignElementInForecast_IsIgnored()
    {
        var kmz = Document(Definition(DefinitionInner) + Placemark("<kml:name>X123A</kml:name><kml:ExtendedData><dwd:Forecast dwd:elementName=\"TTT\"><kml:note>x</kml:note><dwd:value>1.00 - -</dwd:value></dwd:Forecast></kml:ExtendedData>"));
        var index = await ShouldBeValid(kmz);
        index.Locations["X123A"].Should().ContainSingle().Which.Should().Be("TTT");
    }

    [TestMethod]
    public async Task DuplicateValue_IsInvalid()
    {
        var kmz = Document(Definition(DefinitionInner) + Placemark("<kml:name>X123A</kml:name><kml:ExtendedData><dwd:Forecast dwd:elementName=\"TTT\"><dwd:value>1.00 - -</dwd:value><dwd:value>1.00 - -</dwd:value></kml:ExtendedData>"));
        await ShouldBeInvalid(kmz, "dwd:value must occur exactly once");
    }

    [TestMethod]
    public async Task WhitespaceRunsBetweenTokens_AreAccepted()
    {
        var kmz = Document(Definition(DefinitionInner) + Placemark("<kml:name>X123A</kml:name><kml:ExtendedData><dwd:Forecast dwd:elementName=\"TTT\"><dwd:value>  1.00   -\n\t- </dwd:value></dwd:Forecast></kml:ExtendedData>"));
        var index = await ShouldBeValid(kmz);
        index.Locations["X123A"].Should().ContainSingle().Which.Should().Be("TTT");
    }

    [TestMethod]
    public async Task EmptyValue_IsInvalid()
    {
        var kmz = Document(Definition(DefinitionInner) + Placemark("<kml:name>X123A</kml:name><kml:ExtendedData><dwd:Forecast dwd:elementName=\"TTT\"><dwd:value/></dwd:Forecast></kml:ExtendedData>"));
        await ShouldBeInvalid(kmz, "does not have one token per time step");
    }

    [TestMethod]
    public async Task ValueWithChildElement_IsInvalid()
    {
        var kmz = Document(Definition(DefinitionInner) + Placemark("<kml:name>X123A</kml:name><kml:ExtendedData><dwd:Forecast dwd:elementName=\"TTT\"><dwd:value>1.00 <kml:b/>- -</dwd:value></dwd:Forecast></kml:ExtendedData>"));
        await ShouldBeInvalid(kmz, "dwd:value must contain only text");
    }

    [TestMethod]
    public async Task TokenLongerThanSeventeenCharacters_IsInvalid()
    {
        var kmz = Document(Definition(DefinitionInner) + Placemark("<kml:name>X123A</kml:name><kml:ExtendedData><dwd:Forecast dwd:elementName=\"TTT\"><dwd:value>1234567890.12345678 - -</dwd:value></dwd:Forecast></kml:ExtendedData>"));
        await ShouldBeInvalid(kmz, "contains an invalid token");
    }

    [TestMethod]
    public async Task NamePaddedBeyondTheTextBound_IsTrimmedAndAccepted()
    {
        var kmz = Document(Definition(DefinitionInner) + Placemark("<kml:name>" + new string(' ', 300) + "X123A" + new string('\t', 300) + "</kml:name><kml:ExtendedData><dwd:Forecast dwd:elementName=\"TTT\"><dwd:value>1.00 - -</dwd:value></dwd:Forecast></kml:ExtendedData>"));
        var index = await ShouldBeValid(kmz);
        index.Locations["X123A"].Should().ContainSingle().Which.Should().Be("TTT");
    }

    [TestMethod]
    public async Task CommentsBetweenElements_AreAccepted()
    {
        var kmz = Document("<!-- leading note - with a dash -->" + Definition(DefinitionInner) + "<!---->" + Placemark(PlacemarkInner));
        var index = await ShouldBeValid(kmz);
        index.Locations["X123A"].Should().ContainSingle().Which.Should().Be("TTT");
    }

    [TestMethod]
    public async Task CdataValue_IsAccepted()
    {
        var placemark = "<kml:name>X123A</kml:name><kml:ExtendedData><dwd:Forecast dwd:elementName=\"TTT\"><dwd:value><![CDATA[1.00 ] - -]]></dwd:value></dwd:Forecast></kml:ExtendedData>";
        var kmz = Document(Definition(DefinitionInner) + Placemark(placemark));
        await ShouldBeInvalid(kmz, "contains an invalid token");
    }

    [TestMethod]
    public async Task CdataValueWithClosingBrackets_ParsesTheText()
    {
        var placemark = "<kml:name>X123A</kml:name><kml:ExtendedData><dwd:Forecast dwd:elementName=\"TTT\"><dwd:value><![CDATA[1.00 - -]]></dwd:value></dwd:Forecast></kml:ExtendedData>";
        var kmz = Document(Definition(DefinitionInner) + Placemark(placemark));
        var index = await ShouldBeValid(kmz);
        index.Locations["X123A"].Should().ContainSingle().Which.Should().Be("TTT");
    }

    [TestMethod]
    public async Task DoctypeDeclaration_IsInvalid()
    {
        var kml = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?><!DOCTYPE kml><kml:kml xmlns:dwd=\"" + Dwd + "\" xmlns:kml=\"" + Kml + "\"><kml:Document>" + Definition(DefinitionInner) + Placemark(PlacemarkInner) + "</kml:Document></kml:kml>";
        var kmz = MosmixSKmzBuilder.Kmz(kml);
        await ShouldBeInvalid(kmz, "not well-formed or contains a DTD");
    }

    [TestMethod]
    public async Task BrokenCommentOpening_IsInvalid()
    {
        var kmz = Document("<!-x>" + Definition(DefinitionInner) + Placemark(PlacemarkInner));
        await ShouldBeInvalid(kmz, "not well-formed");
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task OversizedComment_RejectedByTheMarkupBound()
    {
        var kmz = Document("<!--" + new string('c', 2_000_000) + "-->" + Definition(DefinitionInner) + Placemark(PlacemarkInner));
        await ShouldBeInvalid(kmz, "exceeds 1 MiB");
    }

    [TestMethod]
    public async Task NameWithInternalWhitespaceBeyondTheTextBound_IsInvalid()
    {
        var kmz = Document(Definition(DefinitionInner) + Placemark("<kml:name>X123" + new string(' ', 253) + "A</kml:name><kml:ExtendedData><dwd:Forecast dwd:elementName=\"TTT\"><dwd:value>1.00 - -</dwd:value></dwd:Forecast></kml:ExtendedData>"));
        await ShouldBeInvalid(kmz, "does not match");
    }

    [TestMethod]
    [Timeout(60000)]
    public async Task OversizedFirstConstructWithoutDeclaration_RejectedByTheMarkupBound()
    {
        var member = Encoding.Latin1.GetBytes(Body().Replace("<kml:kml ", "<kml:kml huge=\"" + new string('h', 2_000_000) + "\" "));
        await ShouldBeInvalid(KmzFromBytes(member), "exceeds 1 MiB");
    }

    [TestMethod]
    public async Task Utf8ByteOrderMarkAndLeadingWhitespace_AreAccepted()
    {
        var member = new byte[] { 0xEF, 0xBB, 0xBF };
        member = member.Concat(Encoding.UTF8.GetBytes(" \r\n\t" + Body())).ToArray();
        await ShouldBeValid(KmzFromBytes(member));
    }

    [TestMethod]
    public async Task IncompleteByteOrderMark_IsInvalid()
    {
        var member = new byte[] { 0xEF, 0xBB };
        member = member.Concat(Encoding.UTF8.GetBytes(Body())).ToArray();
        await ShouldBeInvalid(KmzFromBytes(member), "UTF-8 or ISO-8859-1");
    }

    [TestMethod]
    public async Task MemberNotStartingWithMarkup_IsInvalid()
    {
        var member = Encoding.UTF8.GetBytes("x" + Body());
        await ShouldBeInvalid(KmzFromBytes(member), "UTF-8 or ISO-8859-1");
    }

    [TestMethod]
    public async Task Utf16WithoutByteOrderMark_IsInvalid()
    {
        var member = Encoding.Unicode.GetBytes(Body());
        await ShouldBeInvalid(KmzFromBytes(member), "UTF-8 or ISO-8859-1");
    }

    [TestMethod]
    public async Task NestingAtThePermittedDepth_IsAccepted()
    {
        var kmz = Document(Definition(DefinitionInner) + Placemark(PlacemarkInner + "<kml:extra>" + Nested(28) + "</kml:extra>"));
        var index = await ShouldBeValid(kmz);
        index.Locations["X123A"].Should().ContainSingle().Which.Should().Be("TTT");
    }

    [TestMethod]
    public async Task NestingBeyondThePermittedDepth_IsInvalid()
    {
        var kmz = Document(Definition(DefinitionInner) + Placemark(PlacemarkInner + "<kml:extra>" + Nested(29) + "</kml:extra>"));
        await ShouldBeInvalid(kmz, "nested deeper than 32");
    }

    [TestMethod]
    public async Task ProcessingInstructionsBetweenElements_AreIgnored()
    {
        var definition = Definition(DefinitionInner.Replace("</dwd:ProductID>", "</dwd:ProductID><?note b?>").Replace("</dwd:TimeStep><dwd:TimeStep>", "</dwd:TimeStep><?note c?><dwd:TimeStep>"));
        var placemark = Placemark(PlacemarkInner.Replace("</kml:name>", "</kml:name><?note d?>").Replace("<dwd:Forecast ", "<?note e?><dwd:Forecast ").Replace("<dwd:value>", "<?note f?><dwd:value>"));
        var kmz = Document("<?note a?>" + definition + placemark);
        var index = await ShouldBeValid(kmz);
        index.Locations["X123A"].Should().ContainSingle().Which.Should().Be("TTT");
    }

    [TestMethod]
    public async Task ProcessingInstructionsInsideTexts_AreIgnored()
    {
        var definition = Definition(DefinitionInner.Replace(">MOSMIX<", ">MOS<?note a?>MIX<").Replace(">2026-10-23T03:00:00.000Z<", ">2026-10-23T03<?note b?>:00:00.000Z<"));
        var placemark = Placemark(PlacemarkInner.Replace(">X123A<", "> X12<?note c?>3A <").Replace(">1.00 - -<", ">1<?note d?>.00 -<?note e?> -<"));
        var kmz = Document(definition + placemark);
        var index = await ShouldBeValid(kmz);
        index.Locations["X123A"].Should().ContainSingle().Which.Should().Be("TTT");
    }

    [TestMethod]
    public async Task SignificantWhitespaceBetweenChildren_IsIgnored()
    {
        var kmz = DocumentWithGaps(" xml:space=\"preserve\"", "\n  ");
        var index = await ShouldBeValid(kmz);
        index.Locations["X123A"].Should().ContainSingle().Which.Should().Be("TTT");
    }

    [TestMethod]
    public async Task TextBetweenChildren_IsIgnored()
    {
        var kmz = DocumentWithGaps(string.Empty, "note");
        var index = await ShouldBeValid(kmz);
        index.Locations["X123A"].Should().ContainSingle().Which.Should().Be("TTT");
    }
}
