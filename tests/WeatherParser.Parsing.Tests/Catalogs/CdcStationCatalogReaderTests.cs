using System.Globalization;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class CdcStationCatalogReaderTests
{
    private const string Endpoint = "tu-list";
    private ICdcStationCatalogReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new CdcStationCatalogReader();
    }

    [TestMethod]
    public async Task ReadAsync_ValidFixture_ReturnsAllRecords()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00044", "20070209", "20261021", "44", "52.9336", "8.2370", "Musterhausen-Nord", "Niedersachsen"),
            CdcStationListBuilder.Line("01234", "19960301", "20261020", "-3", "47.5012", "10.2201", "W\u00FCrzburg-Testfeld", "Bayern"),
            CdcStationListBuilder.Line("12345", "20100101", "20261021", "1456", "48.0001", "13.9999", "Gro\u00DFer Probeberg", "Baden-W\u00FCrttemberg")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(3);
        result.Records[0].StationId.Should().Be("00044");
        result.Records[0].FromDate.Should().Be(new DateOnly(2007, 2, 9));
        result.Records[0].ToDate.Should().Be(new DateOnly(2026, 10, 21));
        result.Records[0].AltitudeM.Should().Be(44m);
        result.Records[0].Latitude.Should().Be(52.9336m);
        result.Records[0].Longitude.Should().Be(8.2370m);
        result.Records[0].Name.Should().Be("Musterhausen-Nord");
        result.Records[0].FederalState.Should().Be("Niedersachsen");
        result.Records[0].LineNumber.Should().Be(3);

        result.Records[1].StationId.Should().Be("01234");
        result.Records[1].FromDate.Should().Be(new DateOnly(1996, 3, 1));
        result.Records[1].ToDate.Should().Be(new DateOnly(2026, 10, 20));
        result.Records[1].AltitudeM.Should().Be(-3m);
        result.Records[1].Latitude.Should().Be(47.5012m);
        result.Records[1].Longitude.Should().Be(10.2201m);
        result.Records[1].Name.Should().Be("W\u00FCrzburg-Testfeld");
        result.Records[1].FederalState.Should().Be("Bayern");
        result.Records[1].LineNumber.Should().Be(4);

        result.Records[2].StationId.Should().Be("12345");
        result.Records[2].FromDate.Should().Be(new DateOnly(2010, 1, 1));
        result.Records[2].ToDate.Should().Be(new DateOnly(2026, 10, 21));
        result.Records[2].AltitudeM.Should().Be(1456m);
        result.Records[2].Latitude.Should().Be(48.0001m);
        result.Records[2].Longitude.Should().Be(13.9999m);
        result.Records[2].Name.Should().Be("Gro\u00DFer Probeberg");
        result.Records[2].FederalState.Should().Be("Baden-W\u00FCrttemberg");
        result.Records[2].LineNumber.Should().Be(5);

        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_KeepsLeadingZeroIds()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00044"),
            CdcStationListBuilder.Line("01234")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records[0].StationId.Should().Be("00044");
        result.Records[1].StationId.Should().Be("01234");
    }

    [TestMethod]
    public async Task ReadAsync_PreservesUmlautsInNames()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line(name: "Gro\u00DFe M\u00FChle \u00D6d\u00E4")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records[0].Name.Should().Be("Gro\u00DFe M\u00FChle \u00D6d\u00E4");
    }

    [TestMethod]
    public async Task ReadAsync_ParsesCoordinatesAndAltitudeInvariantCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

            var lines = new[]
            {
                CdcStationListBuilder.Line(latitude: "50.1234", longitude: "9.5678", altitude: "123")
            };

            using var stream = CdcStationListBuilder.Stream(lines);
            var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

            result.Records[0].Latitude.Should().Be(50.1234m);
            result.Records[0].Longitude.Should().Be(9.5678m);
            result.Records[0].AltitudeM.Should().Be(123m);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [TestMethod]
    public async Task ReadAsync_LeavesStreamOpenOnSuccess()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line()
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);
        stream.CanRead.Should().BeTrue();
    }

    [TestMethod]
    public async Task ReadAsync_InvalidHeaderToken_IsFormatFailure()
    {
        // Missing last token
        var header1 = "Stations_id von_datum bis_datum Stationshoehe geoBreite geoLaenge Stationsname Bundesland";
        using (var stream = CdcStationListBuilder.Stream(new[] { CdcStationListBuilder.Line() }, header1))
            await ShouldBeFormatFailure(stream);

        // Extra token
        var header2 = CdcStationListBuilder.Header + " Extra";
        using (var stream = CdcStationListBuilder.Stream(new[] { CdcStationListBuilder.Line() }, header2))
            await ShouldBeFormatFailure(stream);

        // Wrong token
        var header3 = "Stations_id von_datum bis_datum Hoehe geoBreite geoLaenge Stationsname Bundesland Abgabe";
        using (var stream = CdcStationListBuilder.Stream(new[] { CdcStationListBuilder.Line() }, header3))
            await ShouldBeFormatFailure(stream);
    }

    [TestMethod]
    public async Task ReadAsync_InvalidDashLine_IsFormatFailure()
    {
        var dashLine = "----- abc -----";
        using (var stream = CdcStationListBuilder.Stream(new[] { CdcStationListBuilder.Line() }, dashLine: dashLine))
            await ShouldBeFormatFailure(stream);
    }

    [TestMethod]
    public async Task ReadAsync_InvalidDate_Skipped()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002", fromDate: "20260230")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().ContainSingle();
        result.Records[0].StationId.Should().Be("00001");
        ShouldBeSkipped(result, 4);
    }

    [TestMethod]
    public async Task ReadAsync_InvalidAltitudeFormat_Skipped()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002", altitude: "12a")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().ContainSingle();
        result.Records[0].StationId.Should().Be("00001");
        ShouldBeSkipped(result, 4);
    }

    [TestMethod]
    public async Task ReadAsync_InvalidCoordinateFormat_Skipped()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002", latitude: "50"),
            CdcStationListBuilder.Line("00003", longitude: "9,5678")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().ContainSingle();
        result.Records[0].StationId.Should().Be("00001");
        ShouldBeSkipped(result, 4);
        ShouldBeSkipped(result, 5);
    }

    [TestMethod]
    public async Task ReadAsync_NonDigitId_Skipped()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("A1234")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().ContainSingle();
        result.Records[0].StationId.Should().Be("00001");
        ShouldBeSkipped(result, 4);
    }

    [TestMethod]
    public async Task ReadAsync_FourOrSixDigitId_Skipped()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("1234"),
            "123456" + CdcStationListBuilder.Line().Substring(6)
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().ContainSingle();
        result.Records[0].StationId.Should().Be("00001");
        ShouldBeSkipped(result, 4);
        ShouldBeSkipped(result, 5);
    }

    [TestMethod]
    public async Task ReadAsync_EmptyStationName_Skipped()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002", name: "")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().ContainSingle();
        result.Records[0].StationId.Should().Be("00001");
        ShouldBeSkipped(result, 4);
    }

    [TestMethod]
    public async Task ReadAsync_EmptyAltitude_NullNotZero()
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
    public async Task ReadAsync_MixedValidInvalidCatalog_SkipsInvalidAndReturnsValid()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002", toDate: "2026102X"),
            CdcStationListBuilder.Line("00003")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(2);
        result.Records[0].StationId.Should().Be("00001");
        result.Records[1].StationId.Should().Be("00003");
        ShouldBeSkipped(result, 4);
    }

    [TestMethod]
    public async Task AddWeatherParsing_ResolvesCdcStationCatalogReader()
    {
        var services = new ServiceCollection().AddWeatherParsing();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var reader = provider.GetRequiredService<ICdcStationCatalogReader>();
        reader.Should().BeOfType<CdcStationCatalogReader>();
    }

    private Task<CdcStationCatalog> Read(Stream s) => _subject.ReadAsync(s, Endpoint, CancellationToken.None);

    private static void ShouldBeSkipped(CdcStationCatalog result, int lineNumber)
    {
        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == lineNumber)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics.Single(d => d.LineNumber == lineNumber).EndpointName.Should().Be(Endpoint);
        result.Diagnostics.Single(d => d.LineNumber == lineNumber).Reason.Should().NotBeEmpty();
    }

    private async Task ShouldBeFormatFailure(Stream s)
    {
        (await FluentActions.Awaiting(() => Read(s)).Should().ThrowAsync<CatalogFormatException>())
            .Which.ErrorId.Should().Be("CATALOG-FORMAT-INVALID");
    }
}
