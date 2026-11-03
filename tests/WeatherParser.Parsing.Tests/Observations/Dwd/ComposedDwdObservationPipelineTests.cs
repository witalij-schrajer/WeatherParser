using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Parsing;
using WeatherParser.Parsing.Tests.Selection;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Observations.Dwd;

[TestClass]
public sealed class ComposedDwdObservationPipelineTests
{
    private MockRepository _mocks = null!;
    private Mock<ICatalogDownloader> _downloader = null!;
    private ManualTimeProvider _clock = null!;
    private CapturingLogger<DwdObservationParser> _logger = null!;
    private IDwdObservationParser _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _downloader = _mocks.Create<ICatalogDownloader>();
        _clock = new(new DateTimeOffset(2026, 10, 24, 12, 20, 30, TimeSpan.Zero));
        _logger = new();

        var endpoints = new CdcCatalogEndpoints
        {
            AirTemperatureStationList = new Uri("https://example.org/cdc/air_temperature/now/list.txt?version=2"),
            AirTemperatureNowDirectory = new Uri("https://example.org/cdc/air_temperature/now/"),
            WindStationList = new Uri("https://example.org/cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt"),
            WindNowDirectory = new Uri("https://example.org/cdc/wind/now/"),
            PrecipitationStationList = new Uri("https://example.org/cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt"),
            PrecipitationNowDirectory = new Uri("https://example.org/cdc/precipitation/now/"),
            SolarStationList = new Uri("https://example.org/cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt"),
            SolarNowDirectory = new Uri("https://example.org/cdc/solar/now/"),
        };

        var validator = new CdcCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator()));
        var source = new DwdObservationSource(new DwdObservationOptions(), endpoints, validator, _downloader.Object, new CdcStationCatalogReader(), new CdcNowEvidenceReader(), new CdcRequestComposer());
        var mapper = new DwdObservationMapper(new QuantityValidator(), new SourceTimeNormalizer(new TimeZoneResolver()));
        var builder = new ObservationDatasetBuilder(new QuantityValidator(), new OutputRounding(), new PressureConverter(new OutputRounding()), new PeriodRateConverter(), new WindSpeedConverter());
        _subject = new DwdObservationParser(new DwdObservationOptions(), new DwdStationResolver(), source, mapper, builder, _clock, _logger);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    [TestMethod]
    public async Task Pipeline_AllFourProducts_BuildsDataset()
    {
        ServeLists();

        var tuRow = CdcNowArchiveBuilder.Row("44", "202610241200", "1001.2", "12.3", "10.1", "85.0", "8.9");
        var ffRow = CdcNowArchiveBuilder.Row("44", "202610241200", "3.0", "270");
        var rrRow = CdcNowArchiveBuilder.Row("44", "202610241200", "10", "0.6", "1");
        var sdRow = CdcNowArchiveBuilder.Row("44", "202610241200", "1.0", "36.0", "0.1", "20.0");

        ServeArchive("AirTemperatureNowDirectory", "tu", tuRow);
        ServeArchive("WindNowDirectory", "ff", ffRow);
        ServeArchive("PrecipitationNowDirectory", "rr", rrRow);
        ServeArchive("SolarNowDirectory", "sd", sdRow);

        var result = await _subject.ParseAsync("00044", CancellationToken.None);

        result.Source.Should().Be("dwd:00044");
        result.Location.Name.Should().Be("Grossenkneten");
        result.Measurements.TemperatureC.Should().NotBeNull();
        result.Measurements.PressureHpa.Should().NotBeNull();
        result.Measurements.WindDirectionDeg.Should().NotBeNull();
        result.Measurements.RainRateMmH.Should().NotBeNull();
        result.Measurements.SolarIrradianceWm2.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Pipeline_MissingProduct_ExportsTheOthersAndLogsNotice()
    {
        ServeLists();

        var tuRow = CdcNowArchiveBuilder.Row("44", "202610241200", "1001.2", "12.3", "10.1", "85.0", "8.9");
        var ffRow = CdcNowArchiveBuilder.Row("44", "202610241200", "3.0", "270");
        var rrRow = CdcNowArchiveBuilder.Row("44", "202610241200", "10", "0.6", "1");
        var sdRow = CdcNowArchiveBuilder.Row("44", "202610241200", "1.0", "36.0", "0.1", "20.0");

        ServeArchive("AirTemperatureNowDirectory", "tu", tuRow);
        ServeNotFound("WindNowDirectory");
        ServeArchive("PrecipitationNowDirectory", "rr", rrRow);
        ServeArchive("SolarNowDirectory", "sd", sdRow);

        var result = await _subject.ParseAsync("00044", CancellationToken.None);

        result.Measurements.WindSpeedKmh.Should().BeNull();
        result.Measurements.WindDirectionDeg.Should().BeNull();
        _logger.Count(DwdObservationNoticeIds.ProductMissing).Should().Be(1);
    }

    [TestMethod]
    public async Task Pipeline_OnlyMissingValues_IsRejected()
    {
        ServeLists();

        var tuRow = CdcNowArchiveBuilder.Row("44", "202610241200", "-999", "-999", "-999", "-999", "-999");
        var ffRow = CdcNowArchiveBuilder.Row("44", "202610241200", "-999", "-999");
        var rrRow = CdcNowArchiveBuilder.Row("44", "202610241200", "-999", "-999", "-999");
        var sdRow = CdcNowArchiveBuilder.Row("44", "202610241200", "-999", "-999", "-999", "-999");

        ServeArchive("AirTemperatureNowDirectory", "tu", tuRow);
        ServeArchive("WindNowDirectory", "ff", ffRow);
        ServeArchive("PrecipitationNowDirectory", "rr", rrRow);
        ServeArchive("SolarNowDirectory", "sd", sdRow);

        Func<Task> act = () => _subject.ParseAsync("00044", CancellationToken.None);
        await act.Should().ThrowAsync<ObservationDatasetRejectedException>();
    }

    [TestMethod]
    public async Task Pipeline_AllCalmWind_NoDirectionAndOneNotice()
    {
        ServeLists();

        var tuRow = CdcNowArchiveBuilder.Row("44", "202610241200", "1001.2", "12.3", "10.1", "85.0", "8.9");
        var ffRow1 = CdcNowArchiveBuilder.Row("44", "202610241150", "0.0", "180");
        var ffRow2 = CdcNowArchiveBuilder.Row("44", "202610241200", "0.0", "180");

        ServeArchive("AirTemperatureNowDirectory", "tu", tuRow);
        ServeArchive("WindNowDirectory", "ff", ffRow1, ffRow2);
        ServeNotFound("PrecipitationNowDirectory");
        ServeNotFound("SolarNowDirectory");

        var result = await _subject.ParseAsync("00044", CancellationToken.None);

        result.Measurements.WindDirectionDeg.Should().BeNull();
        result.Measurements.WindSpeedKmh.Should().NotBeNull();
        Omissions("WindDirection").Should().Be(1);
        _logger.Entries
            .Where(e => e.EventName == DwdObservationNoticeIds.ReadingOmitted && e.Message.Contains("CalmWind"))
            .Should()
            .ContainSingle();
    }

    [TestMethod]
    public async Task Pipeline_AllZeroDirection_NoDirectionAndOneNotice()
    {
        ServeLists();

        var tuRow = CdcNowArchiveBuilder.Row("44", "202610241200", "1001.2", "12.3", "10.1", "85.0", "8.9");
        var ffRow1 = CdcNowArchiveBuilder.Row("44", "202610241150", "3.0", "0");
        var ffRow2 = CdcNowArchiveBuilder.Row("44", "202610241200", "3.0", "0");

        ServeArchive("AirTemperatureNowDirectory", "tu", tuRow);
        ServeArchive("WindNowDirectory", "ff", ffRow1, ffRow2);
        ServeNotFound("PrecipitationNowDirectory");
        ServeNotFound("SolarNowDirectory");

        var result = await _subject.ParseAsync("00044", CancellationToken.None);

        result.Measurements.WindDirectionDeg.Should().BeNull();
        Omissions("WindDirection").Should().Be(1);
        _logger.Entries
            .Where(e => e.EventName == DwdObservationNoticeIds.ReadingOmitted && e.Message.Contains("ZeroDirection"))
            .Should()
            .ContainSingle();
    }

    [TestMethod]
    public async Task Pipeline_ZeroDirectionAndEverythingElseMissing_IsRejectedWithOneNoticePerKind()
    {
        ServeLists();

        var tuRow = CdcNowArchiveBuilder.Row("44", "202610241200", "-999", "-999", "-999", "-999", "-999");
        var ffRow = CdcNowArchiveBuilder.Row("44", "202610241200", "-999", "0");

        ServeArchive("AirTemperatureNowDirectory", "tu", tuRow);
        ServeArchive("WindNowDirectory", "ff", ffRow);
        ServeNotFound("PrecipitationNowDirectory");
        ServeNotFound("SolarNowDirectory");

        Func<Task> act = () => _subject.ParseAsync("00044", CancellationToken.None);
        await act.Should().ThrowAsync<ObservationDatasetRejectedException>();

        Omissions("WindDirection").Should().Be(1);
        Omissions("Temperature").Should().Be(1);
        Omissions("MeanWindSpeed").Should().Be(1);
    }

    private static byte[] ListBytes()
    {
        using var stream = CdcStationListBuilder.Stream(new[] { CdcStationListBuilder.Line("00044", "20000101", "20261024", "44", "52.9336", "8.2370", "Grossenkneten", "Niedersachsen") });
        return stream.ToArray();
    }

    private void ServeLists()
    {
        var names = new[]
        {
            "AirTemperatureStationList",
            "WindStationList",
            "PrecipitationStationList",
            "SolarStationList"
        };

        foreach (var name in names)
        {
            _downloader.Setup(d => d.DownloadAsync(
                    It.Is<CatalogRequest>(r => r.EndpointName == name),
                    It.IsAny<Func<Stream, CancellationToken, Task<CdcStationCatalog>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((CatalogRequest r, Func<Stream, CancellationToken, Task<CdcStationCatalog>> read, CancellationToken t) => read(new MemoryStream(ListBytes()), t)!);
        }
    }

    private void ServeArchive(string endpointName, string productCode, params string[] rows)
    {
        var bytes = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName(productCode, "00044"), CdcNowArchiveBuilder.Content(CdcNowArchiveBuilder.Header(productCode), rows)));
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == endpointName),
                It.IsAny<Func<Stream, CancellationToken, Task<CdcNowEvidence>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((CatalogRequest r, Func<Stream, CancellationToken, Task<CdcNowEvidence>> read, CancellationToken t) => read(new MemoryStream(bytes), t)!);
    }

    private void ServeNotFound(string endpointName)
    {
        _downloader.Setup(d => d.DownloadAsync(
                It.Is<CatalogRequest>(r => r.EndpointName == endpointName),
                It.IsAny<Func<Stream, CancellationToken, Task<CdcNowEvidence>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CdcNowEvidence?)null);
    }

    private int Omissions(string kind) => _logger.Entries.Count(e => e.EventName == DwdObservationNoticeIds.ReadingOmitted && e.Message.Contains(kind));
}
