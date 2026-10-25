using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
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
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Export;
using WeatherParser.Parsing.Tests.Parsing;
using WeatherParser.Parsing.Tests.Schemas;
using WeatherParser.Parsing.Tests.Selection;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Observations.Dwd;

[TestClass]
public sealed class DwdObservationExportContractTests
{
    private MockRepository _mocks = null!;
    private Mock<ICatalogDownloader> _downloader = null!;
    private ManualTimeProvider _clock = null!;
    private CapturingLogger<DwdObservationParser> _logger = null!;
    private IDwdObservationParser _subject = null!;
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;

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
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private static byte[] ListBytes()
    {
        using var stream = CdcStationListBuilder.Stream(new[] { CdcStationListBuilder.Line("00044", "20000101", "20261024", "44", "52.9336", "8.2370", "W\u00fcrzburg", "Niedersachsen") });
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

    private void ServeFullStation(bool windMissing = false)
    {
        ServeLists();

        var tuRow = CdcNowArchiveBuilder.RowWithQn("44", "202610241200", "3", "1013.25", "20.25", "10.1", "50.5", "8.9");
        var ffRow = CdcNowArchiveBuilder.RowWithQn("44", "202610241150", "2", "3.47", "270");
        var rrRow = CdcNowArchiveBuilder.RowWithQn("44", "202610241200", "1", "10", "0.6", "1");
        var sdRow = CdcNowArchiveBuilder.RowWithQn("44", "202610241200", "2", "1.0", "36.0", "0.1", "20.0");

        ServeArchive("AirTemperatureNowDirectory", "tu", tuRow);
        if (windMissing)
        {
            ServeNotFound("WindNowDirectory");
        }
        else
        {
            ServeArchive("WindNowDirectory", "ff", ffRow);
        }
        ServeArchive("PrecipitationNowDirectory", "rr", rrRow);
        ServeArchive("SolarNowDirectory", "sd", sdRow);
    }

    private async Task<(byte[] Json, byte[] Xml)> Serialize(ObservationDataset dataset)
    {
        using var jsonStream = new MemoryStream();
        using var xmlStream = new MemoryStream();
        await _json.SerializeAsync(dataset, jsonStream, CancellationToken.None);
        await _xml.SerializeAsync(dataset, xmlStream, CancellationToken.None);
        return (jsonStream.ToArray(), xmlStream.ToArray());
    }

    private static Dictionary<string, string> JsonMap(byte[] json)
    {
        var map = new Dictionary<string, string>();
        using var document = JsonDocument.Parse(json);
        ExportEquivalence.FlattenJson(document.RootElement, string.Empty, map);
        return map;
    }

    private static Dictionary<string, string> XmlMap(byte[] xml)
    {
        var map = new Dictionary<string, string>();
        foreach (var element in XDocument.Parse(Encoding.UTF8.GetString(xml)).Root!.Elements())
        {
            ExportEquivalence.FlattenXml(element, string.Empty, map);
        }
        return map;
    }

    private static void AssertEquivalent(byte[] json, byte[] xml)
    {
        using var document = JsonDocument.Parse(json);
        ExportEquivalence.AssertEquivalent(document.RootElement, XDocument.Parse(Encoding.UTF8.GetString(xml)).Root!, "observation_dataset");
    }

    [TestMethod]
    public async Task Export_FullStation_JsonAndXmlEquivalent()
    {
        ServeFullStation();
        var dataset = await _subject.ParseAsync("00044", CancellationToken.None);
        var (json, xml) = await Serialize(dataset);
        AssertEquivalent(json, xml);
        JsonMap(json).Should().BeEquivalentTo(XmlMap(xml));
    }

    [TestMethod]
    public async Task Export_FullStation_XmlValidAgainstV1Schema()
    {
        ServeFullStation();
        var dataset = await _subject.ParseAsync("00044", CancellationToken.None);
        var (_, xml) = await Serialize(dataset);
        ExportSchemaValidation.Validate(xml).Should().BeEmpty();
    }

    [TestMethod]
    public async Task Export_MissingProduct_JsonAndXmlEquivalent()
    {
        ServeFullStation(windMissing: true);
        var dataset = await _subject.ParseAsync("00044", CancellationToken.None);
        var (json, xml) = await Serialize(dataset);
        AssertEquivalent(json, xml);
        var jsonMap = JsonMap(json);
        var xmlMap = XmlMap(xml);
        jsonMap.Keys.Should().NotContain(key => key.StartsWith("measurements/wind_speed_kmh/", StringComparison.Ordinal));
        jsonMap.Keys.Should().NotContain(key => key.StartsWith("measurements/wind_direction_deg/", StringComparison.Ordinal));
        xmlMap.Keys.Should().NotContain(key => key.StartsWith("measurements/wind_speed_kmh/", StringComparison.Ordinal));
        xmlMap.Keys.Should().NotContain(key => key.StartsWith("measurements/wind_direction_deg/", StringComparison.Ordinal));
        ExportSchemaValidation.Validate(xml).Should().BeEmpty();
    }

    [TestMethod]
    public async Task Export_SerializationNeedsNoFurtherRetrieval()
    {
        ServeFullStation();
        var dataset = await _subject.ParseAsync("00044", CancellationToken.None);
        var requests = _downloader.Invocations.Count;
        requests.Should().Be(8); // four lists and four archives
        await Serialize(dataset);
        _downloader.Invocations.Count.Should().Be(requests);
    }

    [TestMethod]
    public async Task Export_WireValues_PerContract()
    {
        ServeFullStation();
        var dataset = await _subject.ParseAsync("00044", CancellationToken.None);
        var (json, xml) = await Serialize(dataset);
        var jsonMap = JsonMap(json);
        var xmlMap = XmlMap(xml);

        foreach (var map in new[] { jsonMap, xmlMap })
        {
            map["source"].Should().Be("dwd:00044");
            map["timestamp_quality"].Should().Be("source");
            map["retrieved_at"].Should().Be("2026-10-24T12:20:30");
            map["measured_at"].Should().Be("2026-10-24T12:00:00");
            map["location/name"].Should().Be("W\u00fcrzburg");
            map["location/station_id"].Should().Be("00044");
            map["measurements/humidity_percent/measured_at"].Should().Be("2026-10-24T12:00:00");
            map["measurements/pressure_hpa/measured_at"].Should().Be("2026-10-24T12:00:00");
            map["measurements/pressure_mmhg/measured_at"].Should().Be("2026-10-24T12:00:00");
            map["measurements/rain_rate_mm_h/measured_at"].Should().Be("2026-10-24T12:00:00");
            map["measurements/solar_irradiance_w_m2/measured_at"].Should().Be("2026-10-24T12:00:00");

            map["measurements/temperature_c/value"].Should().Be("20.3");
            map["measurements/temperature_c/measured_at"].Should().Be("2026-10-24T12:00:00");

            map["measurements/humidity_percent/value"].Should().Be("51");

            map["measurements/pressure_hpa/value"].Should().Be("1013");
            map["measurements/pressure_mmhg/value"].Should().Be("760");
            map["measurements/pressure_hpa/pressure_reference"].Should().Be("sensor");
            map["measurements/pressure_mmhg/pressure_reference"].Should().Be("sensor");

            map["measurements/wind_speed_kmh/value"].Should().Be("12");
            map["measurements/wind_speed_kmh/measured_at"].Should().Be("2026-10-24T11:50:00");
            map["measurements/wind_speed_kmh/period/start"].Should().Be("2026-10-24T11:40:00");
            map["measurements/wind_speed_kmh/period/end"].Should().Be("2026-10-24T11:50:00");

            map["measurements/wind_direction_deg/measured_at"].Should().Be("2026-10-24T11:50:00");
            map["measurements/wind_direction_deg/period/start"].Should().Be("2026-10-24T11:40:00");
            map["measurements/wind_direction_deg/period/end"].Should().Be("2026-10-24T11:50:00");

            map["measurements/rain_rate_mm_h/value"].Should().Be("3.6");
            map["measurements/rain_rate_mm_h/period/start"].Should().Be("2026-10-24T11:50:00");
            map["measurements/rain_rate_mm_h/period/end"].Should().Be("2026-10-24T12:00:00");

            map["measurements/solar_irradiance_w_m2/value"].Should().Be("600");
            map["measurements/solar_irradiance_w_m2/period/start"].Should().Be("2026-10-24T11:50:00");
            map["measurements/solar_irradiance_w_m2/period/end"].Should().Be("2026-10-24T12:00:00");

            map.Keys.Should().NotContain(key => key.StartsWith("measurements/temperature_c/period", StringComparison.Ordinal));
            map.Keys.Should().NotContain(key => key.StartsWith("measurements/humidity_percent/period", StringComparison.Ordinal));
            map.Keys.Should().NotContain(key => key.StartsWith("measurements/pressure_hpa/period", StringComparison.Ordinal));

            map.Keys.Should().NotContain(key => key.StartsWith("measurements/illuminance_lux", StringComparison.Ordinal));
        }

        foreach (var map in new[] { jsonMap, xmlMap })
        {
            map["measurements/temperature_c/quality"].Should().Be("3");
            map["measurements/humidity_percent/quality"].Should().Be("3");
            map["measurements/pressure_hpa/quality"].Should().Be("3");
            map["measurements/pressure_mmhg/quality"].Should().Be("3");
            map["measurements/wind_speed_kmh/quality"].Should().Be("2");
            map["measurements/wind_direction_deg/quality"].Should().Be("2");
            map["measurements/rain_rate_mm_h/quality"].Should().Be("1");
            map["measurements/solar_irradiance_w_m2/quality"].Should().Be("2");
        }
    }

    [TestMethod]
    public async Task Export_AllCalmWind_NoWindDirectionMember()
    {
        ServeLists();

        var tuRow = CdcNowArchiveBuilder.Row("44", "202610241200", "1013.25", "20.25", "10.1", "50.5", "8.9");
        var ffRow1 = CdcNowArchiveBuilder.Row("44", "202610241150", "0.0", "180");
        var ffRow2 = CdcNowArchiveBuilder.Row("44", "202610241200", "0.0", "180");

        ServeArchive("AirTemperatureNowDirectory", "tu", tuRow);
        ServeArchive("WindNowDirectory", "ff", ffRow1, ffRow2);
        ServeNotFound("PrecipitationNowDirectory");
        ServeNotFound("SolarNowDirectory");

        var dataset = await _subject.ParseAsync("00044", CancellationToken.None);
        var (json, xml) = await Serialize(dataset);
        AssertEquivalent(json, xml);
        ExportSchemaValidation.Validate(xml).Should().BeEmpty();

        var jsonMap = JsonMap(json);
        var xmlMap = XmlMap(xml);

        jsonMap.Keys.Should().NotContain(key => key.StartsWith("measurements/wind_direction_deg/", StringComparison.Ordinal));
        xmlMap.Keys.Should().NotContain(key => key.StartsWith("measurements/wind_direction_deg/", StringComparison.Ordinal));

        foreach (var map in new[] { jsonMap, xmlMap })
        {
            map["measurements/wind_speed_kmh/value"].Should().Be("0");
            map["measurements/wind_speed_kmh/measured_at"].Should().Be("2026-10-24T12:00:00");
            map["measurements/wind_speed_kmh/period/start"].Should().Be("2026-10-24T11:50:00");
            map["measurements/wind_speed_kmh/period/end"].Should().Be("2026-10-24T12:00:00");
        }
    }

    [TestMethod]
    public async Task Export_AllZeroDirection_NoWindDirectionMember()
    {
        ServeLists();

        var tuRow = CdcNowArchiveBuilder.Row("44", "202610241200", "1013.25", "20.25", "10.1", "50.5", "8.9");
        var ffRow1 = CdcNowArchiveBuilder.Row("44", "202610241150", "3.0", "0");
        var ffRow2 = CdcNowArchiveBuilder.Row("44", "202610241200", "3.0", "0");

        ServeArchive("AirTemperatureNowDirectory", "tu", tuRow);
        ServeArchive("WindNowDirectory", "ff", ffRow1, ffRow2);
        ServeNotFound("PrecipitationNowDirectory");
        ServeNotFound("SolarNowDirectory");

        var dataset = await _subject.ParseAsync("00044", CancellationToken.None);
        var (json, xml) = await Serialize(dataset);
        AssertEquivalent(json, xml);
        ExportSchemaValidation.Validate(xml).Should().BeEmpty();

        var jsonMap = JsonMap(json);
        var xmlMap = XmlMap(xml);

        jsonMap.Keys.Should().NotContain(key => key.StartsWith("measurements/wind_direction_deg/", StringComparison.Ordinal));
        xmlMap.Keys.Should().NotContain(key => key.StartsWith("measurements/wind_direction_deg/", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Export_MissingColumnWithUsableSiblings_OmittedInBothFormats()
    {
        ServeLists();

        var tuRow = CdcNowArchiveBuilder.Row("44", "202610241200", "-999", "20.25", "10.1", "50.5", "8.9");
        var ffRow = CdcNowArchiveBuilder.Row("44", "202610241150", "3.47", "270");
        var rrRow = CdcNowArchiveBuilder.Row("44", "202610241200", "10", "0.6", "1");
        var sdRow = CdcNowArchiveBuilder.Row("44", "202610241200", "1.0", "36.0", "0.1", "20.0");

        ServeArchive("AirTemperatureNowDirectory", "tu", tuRow);
        ServeArchive("WindNowDirectory", "ff", ffRow);
        ServeNotFound("PrecipitationNowDirectory");
        ServeNotFound("SolarNowDirectory");

        var dataset = await _subject.ParseAsync("00044", CancellationToken.None);
        var (json, xml) = await Serialize(dataset);
        AssertEquivalent(json, xml);
        ExportSchemaValidation.Validate(xml).Should().BeEmpty();

        var jsonMap = JsonMap(json);
        var xmlMap = XmlMap(xml);

        foreach (var map in new[] { jsonMap, xmlMap })
        {
            map.Keys.Should().NotContain(key => key.StartsWith("measurements/pressure_hpa/", StringComparison.Ordinal));
            map.Keys.Should().NotContain(key => key.StartsWith("measurements/pressure_mmhg/", StringComparison.Ordinal));

            map["measurements/temperature_c/value"].Should().Be("20.3");
            map["measurements/humidity_percent/value"].Should().Be("51");
        }
    }
}
