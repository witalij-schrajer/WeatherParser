using System;
using System.Globalization;
using System.IO;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherParser.Parsing.Catalogs;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class MosmixLocationCatalogReaderNormalizationTests
{
    private const string Endpoint = "mosmix-catalog";
    private MockRepository _mocks = null!;
    private Mock<ILogger<MosmixLocationCatalogReader>> _logger = null!;
    private MosmixLocationCatalogReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _logger = _mocks.Create<ILogger<MosmixLocationCatalogReader>>();
        _subject = new MosmixLocationCatalogReader(_logger.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private Task<MosmixLocationCatalog> Read(params string[] lines) => _subject.ReadAsync(new MemoryStream(MosmixCatalogBuilder.Catalog(lines)), Endpoint, CancellationToken.None);

    private static void ShouldBeSkipped(MosmixLocationCatalog result, int lineNumber)
    {
        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == lineNumber && d.Id == CatalogDiagnostic.RecordSkippedId && d.EndpointName == Endpoint);
    }

    private void ExpectNoAltitudeNotice()
    {
        _logger.Setup(l => l.Log(
            LogLevel.Warning,
            It.Is<EventId>(e => e.Name == "MOSMIX-LOCATION-NO-ALTITUDE"),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains(Endpoint)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }

    private void VerifyNoAltitudeNotices(int count) => _logger.Verify(l => l.Log(
            LogLevel.Warning,
            It.Is<EventId>(e => e.Name == "MOSMIX-LOCATION-NO-ALTITUDE"),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(count));

    [TestMethod]
    public async Task ReadAsync_ConvertsDegreesMinutesToDecimalDegrees()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("A001", latitude: "50.30", longitude: "9.15"),
            MosmixCatalogBuilder.Line("A002", latitude: "47.45", longitude: "11.06"),
            MosmixCatalogBuilder.Line("A003", latitude: "70.56", longitude: "-8.40"));

        result.Records.Should().HaveCount(3);
        result.Records[0].Latitude.Should().Be(50.5m);
        result.Records[0].Longitude.Should().Be(9.25m);
        result.Records[1].Latitude.Should().Be(47.75m);
        result.Records[1].Longitude.Should().Be(11.1m);
        result.Records[2].Latitude.Should().BeApproximately(70.933333333333333m, 0.000000000000001m);
        result.Records[2].Longitude.Should().BeApproximately(-8.666666666666667m, 0.000000000000001m);
    }

    [TestMethod]
    public async Task ReadAsync_NegativeTokens_AreSouthAndWest()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("A001", latitude: "-12.30", longitude: "-45.15"),
            MosmixCatalogBuilder.Line("A002", latitude: "-0.30", longitude: "-0.06"));

        result.Records.Should().HaveCount(2);
        result.Records[0].Latitude.Should().Be(-12.5m);
        result.Records[0].Longitude.Should().Be(-45.25m);
        result.Records[1].Latitude.Should().Be(-0.5m);
        result.Records[1].Longitude.Should().Be(-0.1m);
    }

    [TestMethod]
    public async Task ReadAsync_MinuteSixty_Skipped()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("A001"),
            MosmixCatalogBuilder.Line("37531", name: "MINUTENDORF", latitude: "41.60", longitude: "44.07", elevation: "132"));

        result.Records.Should().ContainSingle();
        result.Records[0].MosmixId.Should().Be("A001");
        ShouldBeSkipped(result, 4);
    }

    [TestMethod]
    public async Task ReadAsync_MinuteFiftyNine_Accepted()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("A001", latitude: "41.59", longitude: "44.59"));

        result.Records.Should().HaveCount(1);
        result.Records[0].Latitude.Should().BeApproximately(41.983333333333333m, 0.000000000000001m);
        result.Records[0].Longitude.Should().BeApproximately(44.983333333333333m, 0.000000000000001m);
    }

    [TestMethod]
    public async Task ReadAsync_ElevationBounds_BothSides()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("A001", elevation: "-501"),
            MosmixCatalogBuilder.Line("A002", elevation: "-500"),
            MosmixCatalogBuilder.Line("A003", elevation: "9000"),
            MosmixCatalogBuilder.Line("A004", elevation: "9001"));

        result.Records.Should().HaveCount(2);
        result.Records[0].MosmixId.Should().Be("A002");
        result.Records[0].AltitudeM.Should().Be(-500m);
        result.Records[1].MosmixId.Should().Be("A003");
        result.Records[1].AltitudeM.Should().Be(9000m);
        ShouldBeSkipped(result, 3);
        ShouldBeSkipped(result, 6);
        result.Diagnostics.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task ReadAsync_CoordinatesAtBoundsAccepted_AndJustOutsideRejected()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("A001", latitude: "90.00", longitude: "180.00"),
            MosmixCatalogBuilder.Line("A002", latitude: "-90.00", longitude: "-180.00"),
            MosmixCatalogBuilder.Line("A003", latitude: "90.01", longitude: "9.00"),
            MosmixCatalogBuilder.Line("A004", latitude: "50.00", longitude: "180.01"));

        result.Records.Should().HaveCount(2);
        result.Records[0].Latitude.Should().Be(90m);
        result.Records[0].Longitude.Should().Be(180m);
        result.Records[1].Latitude.Should().Be(-90m);
        result.Records[1].Longitude.Should().Be(-180m);
        ShouldBeSkipped(result, 5);
        ShouldBeSkipped(result, 6);
        result.Diagnostics.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task ReadAsync_MinuteComponentLimits_Skipped()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("A001"),
            MosmixCatalogBuilder.Line("A002", latitude: "50.6"),
            MosmixCatalogBuilder.Line("A003", latitude: "50.600"),
            MosmixCatalogBuilder.Line("A004", latitude: "50.60"));

        result.Records.Should().ContainSingle();
        result.Records[0].MosmixId.Should().Be("A001");
        ShouldBeSkipped(result, 4);
        ShouldBeSkipped(result, 5);
        ShouldBeSkipped(result, 6);
    }

    [TestMethod]
    public async Task ReadAsync_MissingAltitude_IsNullNotZero_WithDiagnostic()
    {
        ExpectNoAltitudeNotice();
        var result = await Read(
            MosmixCatalogBuilder.Line("A001", elevation: null),
            MosmixCatalogBuilder.Line("A002"));

        result.Records.Should().HaveCount(2);
        result.Records[0].MosmixId.Should().Be("A001");
        result.Records[0].AltitudeM.Should().BeNull();
        result.Records[1].MosmixId.Should().Be("A002");
        result.Records[1].AltitudeM.Should().NotBeNull();

        var diagnostic = result.Diagnostics.Should().ContainSingle().Which;
        diagnostic.Id.Should().Be(CatalogDiagnostic.MosmixLocationNoAltitudeId);
        diagnostic.EndpointName.Should().Be(Endpoint);
        diagnostic.LineNumber.Should().Be(3);
        diagnostic.StationId.Should().Be("A001");
        diagnostic.AlreadyLogged.Should().BeTrue();

        VerifyNoAltitudeNotices(1);
    }

    [TestMethod]
    public async Task ReadAsync_ZeroAltitude_IsZeroNotMissing()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("A001", elevation: "0"));

        result.Records.Should().ContainSingle();
        result.Records[0].MosmixId.Should().Be("A001");
        result.Records[0].AltitudeM.Should().Be(0m);
        result.SelectableRecords.Should().ContainSingle();
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_NegativeAltitude_Kept()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("A001", elevation: "-350"));

        result.Records.Should().ContainSingle();
        result.Records[0].MosmixId.Should().Be("A001");
        result.Records[0].AltitudeM.Should().Be(-350m);
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_ParsesUnderGermanCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

            var result = await Read(
                MosmixCatalogBuilder.Line("A001", latitude: "50.30", longitude: "9.15", elevation: "250"));

            result.Records.Should().ContainSingle();
            result.Records[0].Latitude.Should().Be(50.5m);
            result.Records[0].Longitude.Should().Be(9.25m);
            result.Records[0].AltitudeM.Should().Be(250m);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [TestMethod]
    public async Task ReadAsync_TwoTokenLine_ExcludedWithNoAltitudeNotice()
    {
        ExpectNoAltitudeNotice();
        var result = await Read(
            MosmixCatalogBuilder.Line("A001"),
            MosmixCatalogBuilder.Line("A002", elevation: null));

        result.Records.Should().HaveCount(2);
        result.Records[0].MosmixId.Should().Be("A001");
        result.Records[0].AltitudeM.Should().Be(250m);
        result.Records[1].MosmixId.Should().Be("A002");
        result.Records[1].AltitudeM.Should().BeNull();

        result.SelectableRecords.Should().ContainSingle();
        result.SelectableRecords[0].MosmixId.Should().Be("A001");

        VerifyNoAltitudeNotices(1);
    }

    [TestMethod]
    public async Task ReadAsync_AllRecordsWithoutAltitude_IsNotFormatFailure()
    {
        ExpectNoAltitudeNotice();
        var result = await Read(
            MosmixCatalogBuilder.Line("A001", elevation: null),
            MosmixCatalogBuilder.Line("A002", elevation: null));

        result.Records.Should().HaveCount(2);
        result.Records[0].MosmixId.Should().Be("A001");
        result.Records[0].AltitudeM.Should().BeNull();
        result.Records[1].MosmixId.Should().Be("A002");
        result.Records[1].AltitudeM.Should().BeNull();

        result.SelectableRecords.Should().BeEmpty();
        result.Diagnostics.Should().HaveCount(2);
        result.Diagnostics[0].Id.Should().Be(CatalogDiagnostic.MosmixLocationNoAltitudeId);
        result.Diagnostics[0].AlreadyLogged.Should().BeTrue();
        result.Diagnostics[1].Id.Should().Be(CatalogDiagnostic.MosmixLocationNoAltitudeId);
        result.Diagnostics[1].AlreadyLogged.Should().BeTrue();

        VerifyNoAltitudeNotices(2);
    }
}
