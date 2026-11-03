using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Tests.Evidence.MosmixL;

namespace WeatherParser.Parsing.Tests.Evidence.MosmixL;

[TestClass]
public sealed class MosmixLEvidenceReaderTests
{
    private IMosmixLEvidenceReader _subject = null!;
    private const string Id = "X123A";

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new MosmixLEvidenceReader();
    }

    [TestMethod]
    public async Task Read_ReturnsElementsAndIssueTime_WhenValidKmlFileProvided()
    {
        var result = await ReadKml(MosmixKmzBuilder.Kml());
        result.MosmixId.Should().Be("X123A");
        result.IssuedAtUtc.Should().Be(new DateTime(2026, 10, 21, 3, 0, 0, DateTimeKind.Utc));
        result.AvailableElements.Should().Equal("FF", "TTT");
    }

    [TestMethod]
    public async Task Read_AcceptsIssueTimeWithoutFraction()
    {
        var result = await ReadKml(MosmixKmzBuilder.Kml(issueTime: "2026-10-21T03:00:00Z"));
        result.IssuedAtUtc.Should().Be(new DateTime(2026, 10, 21, 3, 0, 0, DateTimeKind.Utc));
    }

    [TestMethod]
    [DataRow("2026-10-21T03:00:00.000Z")]
    [DataRow("2026-10-21T03:00:00.0Z")]
    [DataRow("2026-10-21T03:00:00.00Z")]
    public async Task Read_AcceptsIssueTimeWithZeroFraction(string issueTime)
    {
        var result = await ReadKml(MosmixKmzBuilder.Kml(issueTime: issueTime));
        result.IssuedAtUtc.Should().Be(new DateTime(2026, 10, 21, 3, 0, 0, DateTimeKind.Utc));
    }

    [TestMethod]
    [DataRow("2026-02-30T03:00:00Z")]
    [DataRow("2026-10-21T03:00:00")]
    [DataRow("2026-10-21T03:00:00+02:00")]
    [DataRow("2026-10-21 03:00:00Z")]
    [DataRow("2026-10-21T03:00:00.0000Z")]
    [DataRow("2026-10-21T24:00:00Z")]
    public async Task Read_ThrowsCatalogFormatException_WhenIssueTimeIsInvalidUtc(string issueTime)
    {
        await ShouldBeInvalid(() => ReadKml(MosmixKmzBuilder.Kml(issueTime: issueTime)));
    }

    [TestMethod]
    [DataRow("2026-10-21T03:00:00.500Z")]
    [DataRow("2026-10-21T03:00:00.001Z")]
    public async Task Read_ThrowsCatalogFormatException_WhenIssueTimeHasNoZeroFractionalSeconds(string issueTime)
    {
        await ShouldBeInvalid(() => ReadKml(MosmixKmzBuilder.Kml(issueTime: issueTime)));
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenDtdPresent()
    {
        var kml = MosmixKmzBuilder.Kml();
        var dtdLine = "<!DOCTYPE kml:kml [<!ENTITY x \"y\">]>\n";
        var index = kml.IndexOf("\n") + 1;
        kml = kml.Insert(index, dtdLine);
        await ShouldBeInvalid(() => ReadKml(kml));
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenMoreThanOneKmlMember()
    {
        var bytes = Encoding.Latin1.GetBytes(MosmixKmzBuilder.Kml());
        await ShouldBeInvalid(() => _subject.ReadAsync(
            new MemoryStream(MosmixKmzBuilder.Zip(CompressionLevel.Optimal, ("a.kml", bytes), ("b.KML", bytes))),
            Id,
            CancellationToken.None));
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenKmlMemberCountIsZero()
    {
        var kml = MosmixKmzBuilder.Kml();
        await ShouldBeInvalid(() => _subject.ReadAsync(
            new MemoryStream(MosmixKmzBuilder.Zip(CompressionLevel.Optimal, ("readme.txt", Encoding.Latin1.GetBytes(kml)))),
            Id,
            CancellationToken.None));
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenArchiveIsNotZip()
    {
        var kml = MosmixKmzBuilder.Kml();
        await ShouldBeInvalid(() => _subject.ReadAsync(
            new MemoryStream(Encoding.Latin1.GetBytes(kml)),
            Id,
            CancellationToken.None));
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenXmlIsNotWellFormed()
    {
        var kml = MosmixKmzBuilder.Kml();
        kml = kml.Replace("</kml:kml>", "");
        await ShouldBeInvalid(() => ReadKml(kml));
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenProductDefinitionIsMissing()
    {
        var kml = MosmixKmzBuilder.Kml();
        kml = Regex.Replace(kml, "<dwd:ProductDefinition>.*?</dwd:ProductDefinition>", "", RegexOptions.Singleline);
        await ShouldBeInvalid(() => ReadKml(kml));
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenProductIDIsNotMOSMIX()
    {
        var kml = MosmixKmzBuilder.Kml();
        kml = kml.Replace("<dwd:ProductID>MOSMIX</dwd:ProductID>", "<dwd:ProductID>OTHER</dwd:ProductID>");
        await ShouldBeInvalid(() => ReadKml(kml));
    }

    [TestMethod]
    [DataRow("<dwd:ProductID>.*?</dwd:ProductID>")]
    [DataRow("<dwd:IssueTime>.*?</dwd:IssueTime>")]
    [DataRow("<dwd:ForecastTimeSteps>.*?</dwd:ForecastTimeSteps>")]
    [DataRow("<kml:Placemark>.*?</kml:Placemark>")]
    [DataRow("<kml:name>.*?</kml:name>")]
    public async Task Read_ThrowsCatalogFormatException_WhenRequiredPathsAreMissing(string pattern)
    {
        var kml = MosmixKmzBuilder.Kml();
        kml = Regex.Replace(kml, pattern, "", RegexOptions.Singleline);
        await ShouldBeInvalid(() => ReadKml(kml));
    }

    [TestMethod]
    [DataRow("<dwd:ProductID>MOSMIX</dwd:ProductID>", "<dwd:ProductID>MOSMIX</dwd:ProductID><dwd:ProductID>MOSMIX</dwd:ProductID>")]
    [DataRow("<dwd:IssueTime>2026-10-21T03:00:00.000Z</dwd:IssueTime>", "<dwd:IssueTime>2026-10-21T03:00:00.000Z</dwd:IssueTime><dwd:IssueTime>2026-10-21T03:00:00.000Z</dwd:IssueTime>")]
    [DataRow("<kml:name>X123A</kml:name>", "<kml:name>X123A</kml:name><kml:name>X123A</kml:name>")]
    [DataRow("</kml:Document>", "<kml:Placemark><kml:name>X123A</kml:name></kml:Placemark></kml:Document>")]
    [DataRow("<kml:description>BEISPIELSTADT</kml:description>", "<kml:description>BEISPIELSTADT</kml:description><kml:ExtendedData></kml:ExtendedData>")]
    [DataRow("</kml:kml>", "<kml:Document></kml:Document></kml:kml>")]
    public async Task Read_ThrowsCatalogFormatException_WhenDuplicateRequiredNodesExist(string find, string replacement)
    {
        var kml = MosmixKmzBuilder.Kml();
        kml = kml.Replace(find, replacement);
        await ShouldBeInvalid(() => ReadKml(kml));
    }

    [TestMethod]
    [DataRow("X123B")]
    [DataRow("x123a")]
    public async Task Read_ThrowsCatalogFormatException_WhenPlacemarkNameDoesNotMatchRequestedId(string id)
    {
        await ShouldBeInvalid(() => ReadKml(MosmixKmzBuilder.Kml(id: id)));
    }

    [TestMethod]
    [DataRow(MosmixKmzBuilder.DwdNs, "https://example.org/other.xsd")]
    [DataRow(MosmixKmzBuilder.KmlNs, "http://www.opengis.net/kml/2.3")]
    public async Task Read_ThrowsCatalogFormatException_WhenNamespaceUriIsWrong(string find, string replacement)
    {
        var kml = MosmixKmzBuilder.Kml();
        kml = kml.Replace(find, replacement);
        await ShouldBeInvalid(() => ReadKml(kml));
    }

    [TestMethod]
    public async Task Read_AcceptsOtherPrefixesBoundToTheCorrectNamespaceUris()
    {
        var kml = MosmixKmzBuilder.Kml();
        kml = kml.Replace("xmlns:kml=", "xmlns:k=").Replace("xmlns:dwd=", "xmlns:d=").Replace("kml:", "k:").Replace("dwd:", "d:");
        var result = await ReadKml(kml);
        result.AvailableElements.Should().Equal("FF", "TTT");
    }

    [TestMethod]
    public async Task Read_CountsOnlyForecastsAtTheSpecifiedPath()
    {
        var kml = MosmixKmzBuilder.Kml();
        var forecastInProductDefinition = MosmixKmzBuilder.Forecast("XX", "1.00 2.00 3.00");
        kml = kml.Replace("<dwd:ProductDefinition>", $"<dwd:ProductDefinition>{forecastInProductDefinition}");
        var forecastInPlacemarkFolder = "<kml:Folder>" + MosmixKmzBuilder.Forecast("YY", "1.00 2.00 3.00") + "</kml:Folder>";
        kml = kml.Replace("</kml:description>", "</kml:description>" + forecastInPlacemarkFolder);
        var result = await ReadKml(kml);
        result.AvailableElements.Should().Equal("FF", "TTT");
    }

    [TestMethod]
    public async Task Read_PlacemarkWithoutExtendedData_HasNoElements()
    {
        var kml = MosmixKmzBuilder.Kml();
        kml = Regex.Replace(kml, "</kml:description>\\s*<kml:ExtendedData>.*?</kml:ExtendedData>", "</kml:description>", RegexOptions.Singleline);
        var result = await ReadKml(kml);
        result.AvailableElements.Should().BeEmpty();
        result.IssuedAtUtc.Should().Be(new DateTime(2026, 10, 21, 3, 0, 0, DateTimeKind.Utc));
    }

    [TestMethod]
    public async Task Read_CancelledToken_ThrowsOperationCanceled()
    {
        var cts = new CancellationToken(true);
        Func<Task> act = () => _subject.ReadAsync(new MemoryStream(), Id, cts);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task Read_NullArguments_ThrowArgumentNullException()
    {
        Func<Task> act1 = () => _subject.ReadAsync(null!, Id, CancellationToken.None);
        await act1.Should().ThrowAsync<ArgumentNullException>();

        Func<Task> act2 = () => _subject.ReadAsync(new MemoryStream(), null!, CancellationToken.None);
        await act2.Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public async Task AddWeatherParsing_ResolvesMosmixLEvidenceReader()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var reader = provider.GetRequiredService<IMosmixLEvidenceReader>();
        reader.Should().BeOfType<MosmixLEvidenceReader>();
    }

    private Task<MosmixLEvidence> Read(byte[] kmz) => _subject.ReadAsync(new MemoryStream(kmz), Id, CancellationToken.None);

    private Task<MosmixLEvidence> ReadKml(string kml) => Read(MosmixKmzBuilder.Kmz(kml));

    private static async Task ShouldBeInvalid(Func<Task> act)
        => (await act.Should().ThrowAsync<CatalogFormatException>()).Which.ErrorId.Should().Be(CatalogFormatException.MosmixEvidenceInvalidErrorId);
}
