using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Export;
using WeatherParser.Parsing.Tests.Observations.Osthessen;
using WeatherParser.Parsing.Tests.Parsing;
using WeatherParser.Parsing.Tests.Selection;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Mqtt;

[TestClass]
public sealed class MqttSourceMessageTests
{
    private StaticHttpHandler _handler = null!;
    private ServiceProvider _provider = null!;
    private IMqttMessageSerializer _serializer = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _handler = new StaticHttpHandler();
        _provider = DwdProvider(_handler);
        _serializer = _provider.GetRequiredService<IMqttMessageSerializer>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _provider.Dispose();
    }

    [TestMethod]
    public async Task DwdObservation_MessageTopicPayloadQosRetain()
    {
        var (dataset, message) = await DwdMessage();
        message.Topic.Should().Be("haus/weather/measurements/dwd:00044/state");
        message.Payload.ToArray().Should().Equal(await JsonBytes(dataset));
        message.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        message.Retain.Should().BeTrue();
        message.MessageType.Should().Be(MqttMessageType.Observation);
    }

    [TestMethod]
    public async Task OsthessenWetterObservation_Synthetic_GenericMessage()
    {
        var (dataset, message) = await OsthessenMessage();
        message.Topic.Should().Be("haus/weather/measurements/osthessenwetter:bimbach/state");
        message.Payload.ToArray().Should().Equal(await JsonBytes(dataset));
        message.MessageType.Should().Be(MqttMessageType.Observation);
    }

    [TestMethod]
    public async Task TwoStations_DistinctTopics()
    {
        var dataset1 = ObservationTestData.ValidDataset();
        var dataset2 = ObservationTestData.ValidDataset();
        dataset2.Source = "dwd:01766";
        dataset2.Location.StationId = "01766";

        var message1 = await _serializer.SerializeAsync(dataset1, CancellationToken.None);
        var message2 = await _serializer.SerializeAsync(dataset2, CancellationToken.None);

        message1.Topic.Should().NotBe(message2.Topic);
    }

    [TestMethod]
    public async Task CdcStationList_NotRetained()
    {
        using var fixture = new SelectionPipelineFixture(new DateTimeOffset(2026, 10, 21, 12, 0, 0, 500, TimeSpan.Zero), ExampleOptions());
        fixture.ArrangeCdc();
        var list = await fixture.SelectCdcAsync(SelectionPipelineFixture.Parameters());
        var message = await fixture.Provider.GetRequiredService<IMqttMessageSerializer>().SerializeAsync(list, CancellationToken.None);

        message.Topic.Should().Be("haus/weather/stations/dwd/selection");
        message.Retain.Should().BeFalse();
        message.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        message.Payload.ToArray().Should().Equal(await fixture.JsonAsync(list));
    }

    [TestMethod]
    public async Task MosmixLocationList_NotRetained()
    {
        using var fixture = new SelectionPipelineFixture(new DateTimeOffset(2026, 10, 21, 12, 0, 0, 500, TimeSpan.Zero), ExampleOptions());
        await fixture.ArrangeMosmixAsync();
        var list = await fixture.SelectMosmixAsync(SelectionPipelineFixture.Parameters());
        var message = await fixture.Provider.GetRequiredService<IMqttMessageSerializer>().SerializeAsync(list, CancellationToken.None);

        message.Topic.Should().Be("haus/weather/stations/mosmix/selection");
        message.Retain.Should().BeFalse();
        message.Payload.ToArray().Should().Equal(await fixture.JsonAsync(list));
    }

    [TestMethod]
    public async Task LeadingZeroIdsAndUmlauts_UnchangedInTopicAndPayload()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Source = "dwd:00044";
        dataset.Location.Name = "M\u00FCnster \u00C4\u00D6\u00DC\u00DF";

        var handler = new StaticHttpHandler();
        using var provider = DwdProvider(handler, new MqttTopicOptions
        {
            Mappings = new Dictionary<MqttMessageType, MqttTopicMapping>
            {
                [MqttMessageType.Observation] = new MqttTopicMapping { TopicTemplate = "o/{provider}/{station_id}" }
            }
        });
        var serializer = provider.GetRequiredService<IMqttMessageSerializer>();
        var message = await serializer.SerializeAsync(dataset, CancellationToken.None);

        message.Topic.Should().Be("o/dwd/00044");
        var text = Encoding.UTF8.GetString(message.Payload.ToArray());
        text.Should().Contain("\"station_id\":\"00044\"");
        text.Should().Contain("\"name\":\"M\u00FCnster \u00C4\u00D6\u00DC\u00DF\"");
    }

    [TestMethod]
    public async Task ObservationPayload_EqualsXmlContent()
    {
        var (dataset, message) = await DwdMessage();
        ExportEquivalence.AssertEquivalent(
            JsonDocument.Parse(message.Payload).RootElement,
            XDocument.Load(new MemoryStream(await XmlBytes(dataset))).Root!,
            "observation_dataset");
    }

    [TestMethod]
    public async Task IdenticalDatasets_ByteIdenticalPayloads()
    {
        var dataset1 = ObservationTestData.ValidDataset();
        var dataset2 = ObservationTestData.ValidDataset();

        var message1 = await _serializer.SerializeAsync(dataset1, CancellationToken.None);
        var message2 = await _serializer.SerializeAsync(dataset2, CancellationToken.None);

        message1.Payload.ToArray().Should().Equal(message2.Payload.ToArray());
        message1.Topic.Should().Be(message2.Topic);
    }

    [TestMethod]
    public async Task SameDatasetMeasuredAt_ChangedQuantityMeasuredAt_DifferentPayloads()
    {
        var datasetA = ObservationTestData.ValidDataset();
        datasetA.Measurements.TemperatureC = ObservationTestData.Value(12.0m, new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc));
        datasetA.Measurements.HumidityPercent = ObservationTestData.Value(80m, new DateTime(2026, 7, 15, 11, 50, 0, DateTimeKind.Utc));

        var datasetB = ObservationTestData.ValidDataset();
        datasetB.Measurements.TemperatureC = ObservationTestData.Value(12.0m, new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc));
        datasetB.Measurements.HumidityPercent = ObservationTestData.Value(80m, new DateTime(2026, 7, 15, 11, 55, 0, DateTimeKind.Utc));

        var messageA = await _serializer.SerializeAsync(datasetA, CancellationToken.None);
        var messageB = await _serializer.SerializeAsync(datasetB, CancellationToken.None);

        messageA.Topic.Should().Be(messageB.Topic);
        messageA.Payload.ToArray().Should().NotEqual(messageB.Payload.ToArray());

        using var docA = JsonDocument.Parse(messageA.Payload);
        using var docB = JsonDocument.Parse(messageB.Payload);
        docA.RootElement.GetProperty("source").GetString().Should().Be(docB.RootElement.GetProperty("source").GetString());
        docA.RootElement.GetProperty("measured_at").GetString().Should().Be("2026-07-15T12:00:00");
        docB.RootElement.GetProperty("measured_at").GetString().Should().Be("2026-07-15T12:00:00");
        docA.RootElement.GetProperty("measurements").GetProperty("temperature_c").GetProperty("measured_at").GetString().Should().Be("2026-07-15T12:00:00");
        docB.RootElement.GetProperty("measurements").GetProperty("temperature_c").GetProperty("measured_at").GetString().Should().Be("2026-07-15T12:00:00");
        docA.RootElement.GetProperty("measurements").GetProperty("humidity_percent").GetProperty("measured_at").GetString().Should().Be("2026-07-15T11:50:00");
        docB.RootElement.GetProperty("measurements").GetProperty("humidity_percent").GetProperty("measured_at").GetString().Should().Be("2026-07-15T11:55:00");
    }

    [TestMethod]
    public async Task CorrectedValueSameQuantityMeasuredAt_DifferentPayloads()
    {
        var datasetA = ObservationTestData.ValidDataset();
        var datasetB = ObservationTestData.ValidDataset();
        datasetB.Measurements.TemperatureC!.Value = 20.4m;

        var messageA = await _serializer.SerializeAsync(datasetA, CancellationToken.None);
        var messageB = await _serializer.SerializeAsync(datasetB, CancellationToken.None);

        messageA.Topic.Should().Be(messageB.Topic);
        messageA.Payload.ToArray().Should().NotEqual(messageB.Payload.ToArray());
    }

    [TestMethod]
    public async Task SameReadingsLaterRetrievedAt_DifferentPayloads()
    {
        var datasetA = ObservationTestData.ValidDataset();
        var datasetB = ObservationTestData.ValidDataset();
        datasetB.RetrievedAtUtc = datasetA.RetrievedAtUtc.AddMinutes(1);

        var messageA = await _serializer.SerializeAsync(datasetA, CancellationToken.None);
        var messageB = await _serializer.SerializeAsync(datasetB, CancellationToken.None);

        messageA.Topic.Should().Be(messageB.Topic);
        messageA.Payload.ToArray().Should().NotEqual(messageB.Payload.ToArray());
    }

    private static MqttTopicOptions ExampleOptions() => new()
    {
        Mappings = new Dictionary<MqttMessageType, MqttTopicMapping>
        {
            [MqttMessageType.Observation] = new MqttTopicMapping { TopicTemplate = "haus/weather/measurements/{source}/state" },
            [MqttMessageType.Forecast] = new MqttTopicMapping { TopicTemplate = "haus/weather/forecasts/{source}/state" },
            [MqttMessageType.CdcStationList] = new MqttTopicMapping { TopicTemplate = "haus/weather/stations/dwd/selection" },
            [MqttMessageType.MosmixLocationList] = new MqttTopicMapping { TopicTemplate = "haus/weather/stations/mosmix/selection" }
        }
    };

    private static async Task<byte[]> JsonBytes<T>(T root) where T : class
    {
        using var stream = new MemoryStream();
        await new JsonExportSerializer().SerializeAsync(root, stream, default);
        return stream.ToArray();
    }

    private static async Task<byte[]> XmlBytes<T>(T root) where T : class
    {
        using var stream = new MemoryStream();
        await new XmlExportSerializer().SerializeAsync(root, stream, default);
        return stream.ToArray();
    }

    private static ServiceProvider DwdProvider(StaticHttpHandler handler, MqttTopicOptions? options = null)
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.AddSingleton(new HttpClient(handler));
        services.AddSingleton<TimeProvider>(new ManualTimeProvider(new DateTimeOffset(2026, 10, 24, 12, 20, 30, TimeSpan.Zero)));
        services.AddSingleton(options ?? ExampleOptions());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private async Task<(ObservationDataset Dataset, MqttMessage Message)> DwdMessage()
    {
        var endpoints = _provider.GetRequiredService<CdcCatalogEndpoints>();
        var list = CdcStationListBuilder.Stream(new[] { CdcStationListBuilder.Line("00044", "20000101", "20261024", "44", "52.9336", "8.2370", "Grossenkneten", "Niedersachsen") }).ToArray();
        _handler.Add(endpoints.AirTemperatureStationList!, list);
        _handler.Add(endpoints.WindStationList!, list);
        _handler.Add(endpoints.PrecipitationStationList!, list);
        _handler.Add(endpoints.SolarStationList!, list);

        _handler.Add(new Uri(endpoints.AirTemperatureNowDirectory!, "10minutenwerte_TU_00044_now.zip"), Archive("tu", CdcNowArchiveBuilder.Row("44", "202610241200", "1001.2", "12.3", "10.1", "85.0", "8.9")));
        _handler.Add(new Uri(endpoints.WindNowDirectory!, "10minutenwerte_wind_00044_now.zip"), Archive("ff", CdcNowArchiveBuilder.Row("44", "202610241200", "3.0", "270")));
        _handler.Add(new Uri(endpoints.PrecipitationNowDirectory!, "10minutenwerte_nieder_00044_now.zip"), Archive("rr", CdcNowArchiveBuilder.Row("44", "202610241200", "10", "0.6", "1")));
        _handler.Add(new Uri(endpoints.SolarNowDirectory!, "10minutenwerte_SOLAR_00044_now.zip"), Archive("sd", CdcNowArchiveBuilder.Row("44", "202610241200", "1.0", "36.0", "0.1", "20.0")));

        var dataset = await _provider.GetRequiredService<IDwdObservationParser>().ParseAsync("00044", CancellationToken.None);
        var message = await _serializer.SerializeAsync(dataset, CancellationToken.None);
        return (dataset, message);
    }

    private static byte[] Archive(string productCode, string row) => CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName(productCode, "00044"), CdcNowArchiveBuilder.Content(CdcNowArchiveBuilder.Header(productCode), new[] { row })));


    private static async Task<(ObservationDataset Dataset, MqttMessage Message)> OsthessenMessage()
    {
        var page = OsthessenWetterHtml.Page(
            OsthessenWetterHtml.Table(
                OsthessenWetterHtml.OverviewHeader(),
                OsthessenWetterHtml.OverviewRow("Bimbach", time: "09:31 Uhr")),
            null);
        var handler = new StaticHttpHandler();
        handler.Add(new Uri("https://example.org/messwerte/"), Encoding.UTF8.GetBytes(page));

        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.AddSingleton(new HttpClient(handler));
        services.AddSingleton<TimeProvider>(new ManualTimeProvider(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero)));
        services.AddSingleton(OsthessenOptions());
        services.AddSingleton(new OsthessenWetterEndpoints { PageUri = new Uri("https://example.org/messwerte/") });
        services.AddSingleton<ILogger<OsthessenWetterParser>>(new CapturingLogger<OsthessenWetterParser>());
        services.AddSingleton(ExampleOptions());
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var result = await provider.GetRequiredService<IOsthessenWetterParser>().ParseAsync("*", CancellationToken.None);
        var dataset = result.Datasets.Single();
        var message = await provider.GetRequiredService<IMqttMessageSerializer>().SerializeAsync(dataset, CancellationToken.None);
        handler.Requests.Should().ContainSingle();
        return (dataset, message);
    }

    private static OsthessenWetterOptions OsthessenOptions() => new()
    {
        Stations =
        {
            new OsthessenWetterStationMetadata { Name = "Bimbach", Latitude = 50.53m, Longitude = 9.72m, AltitudeM = 312 },
            new OsthessenWetterStationMetadata { Name = "Ro\u00DFbach", Latitude = 50.6m, Longitude = 9.9m },
            new OsthessenWetterStationMetadata { Name = "Langenbieber", Latitude = 50.5m, Longitude = 9.8m }
        }
    };
}
