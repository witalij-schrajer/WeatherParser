using System.Text;
using FluentAssertions;
using WeatherParser.Parsing.Catalogs;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class CdcStationCatalogReaderBoundaryTests
{
    private const string Endpoint = "tu-list";
    private ICdcStationCatalogReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new CdcStationCatalogReader();
    }

    [TestMethod]
    public async Task ReadAsync_MissingAltitude_IsNullNotZero()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001", altitude: "")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().ContainSingle();
        result.Records[0].AltitudeM.Should().BeNull();
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_ZeroAltitude_IsZeroNotMissing()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001", altitude: "0")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().ContainSingle();
        result.Records[0].AltitudeM.Should().Be(0m);
    }

    [TestMethod]
    public async Task ReadAsync_CoordinatesAtBounds_Accepted()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001", latitude: "90.0000", longitude: "180.0000"),
            CdcStationListBuilder.Line("00002", latitude: "-90.0000", longitude: "-180.0000"),
            CdcStationListBuilder.Line("00003", latitude: "90.0001"),
            CdcStationListBuilder.Line("00004", longitude: "-180.0001"),
            CdcStationListBuilder.Line("00005")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(3);
        result.Records[0].Latitude.Should().Be(90.0000m);
        result.Records[0].Longitude.Should().Be(180.0000m);
        result.Records[1].Latitude.Should().Be(-90.0000m);
        result.Records[1].Longitude.Should().Be(-180.0000m);
        result.Records[2].Latitude.Should().Be(50.1234m);
        result.Records[2].Longitude.Should().Be(9.5678m);

        result.Diagnostics.Should().HaveCount(2);
        ShouldBeSkipped(result, 5);
        ShouldBeSkipped(result, 6);
    }

    [TestMethod]
    public async Task ReadAsync_ActivePeriodFields_Parsed()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001", fromDate: "19991231", toDate: "20000229")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().ContainSingle();
        result.Records[0].FromDate.Should().Be(new DateOnly(1999, 12, 31));
        result.Records[0].ToDate.Should().Be(new DateOnly(2000, 2, 29));
    }

    [TestMethod]
    public async Task ReadAsync_HeaderAndBlankLines_HandledAsDocumented()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            "",
            "   ",
            CdcStationListBuilder.Line("00002")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(2);
        result.Records[0].LineNumber.Should().Be(3);
        result.Records[1].LineNumber.Should().Be(6);
        result.Diagnostics.Should().BeEmpty();

        // Test with extra spaces in header
        var headerWithExtraSpaces = "Stations_id   von_datum  bis_datum Stationshoehe geoBreite geoLaenge Stationsname Bundesland Abgabe";
        using var stream2 = CdcStationListBuilder.Stream(new[] { CdcStationListBuilder.Line("00001") }, header: headerWithExtraSpaces);
        var result2 = await _subject.ReadAsync(stream2, Endpoint, CancellationToken.None);

        result2.Records.Should().ContainSingle();
        result2.Records[0].LineNumber.Should().Be(3);
        result2.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_AltitudeBounds_BothSides()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001", altitude: "-500"),
            CdcStationListBuilder.Line("00002", altitude: "5000"),
            CdcStationListBuilder.Line("00003", altitude: "-501"),
            CdcStationListBuilder.Line("00004", altitude: "5001")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(2);
        result.Records[0].StationId.Should().Be("00001");
        result.Records[1].StationId.Should().Be("00002");

        result.Diagnostics.Should().HaveCount(2);
        result.Diagnostics[0].LineNumber.Should().Be(5);
        result.Diagnostics[1].LineNumber.Should().Be(6);
    }

    [TestMethod]
    public async Task ReadAsync_LfLineEndings_Accepted()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002")
        };

        using var stream = CdcStationListBuilder.Stream(lines, lineEnding: "\n");
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(2);
    }

    private static void ShouldBeSkipped(CdcStationCatalog result, int lineNumber)
    {
        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == lineNumber)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics.Single(d => d.LineNumber == lineNumber).EndpointName.Should().Be(Endpoint);
        result.Diagnostics.Single(d => d.LineNumber == lineNumber).Reason.Should().NotBeEmpty();
    }
}
