using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Selection;

namespace WeatherParser.Package.Tests;

[TestClass]
public sealed class PackageLibraryIntegrationTests
{
    private const string BaseUri = "https://a.example.invalid/";
    private ConsumerSelectionHost _host = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _host = ConsumerLibrary.Host(BaseUri);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _host.Dispose();
    }

    [TestMethod]
    public async Task Library_SelectDwd_ToJsonXmlMqtt()
    {
        _host.ArrangeCdc();

        var list = await _host.Provider.GetRequiredService<ICdcStationSelector>().SelectAsync(ConsumerSelectionHost.Parameters(), CancellationToken.None);

        var json = await _host.JsonAsync(list);
        var xml = await _host.XmlAsync(list);
        var mqtt = await ConsumerLibrary.Mqtt(_host, list);

        var jsonText = Encoding.UTF8.GetString(json);
        jsonText.Should().Contain("\"source\":\"dwd:cdc-10min-now\"");

        ConsumerSelectionHost.ValidateAgainstPackagedSchema(xml).Should().BeEmpty();

        mqtt.Topic.Should().Be("haus/weather/stations/dwd/selection");
        mqtt.Retain.Should().BeFalse();
        mqtt.MessageType.Should().Be(MqttMessageType.CdcStationList);
        mqtt.Payload.ToArray().Should().Equal(json);

        _host.Handler.Requests.Should().BeEquivalentTo(new[]
        {
            _host.Cdc.AirTemperatureStationList!.AbsoluteUri,
            _host.Cdc.WindStationList!.AbsoluteUri,
            _host.Cdc.PrecipitationStationList!.AbsoluteUri,
            _host.Cdc.SolarStationList!.AbsoluteUri,
            new Uri(_host.Cdc.AirTemperatureNowDirectory!, "10minutenwerte_TU_00044_now.zip").AbsoluteUri,
            new Uri(_host.Cdc.WindNowDirectory!, "10minutenwerte_wind_00044_now.zip").AbsoluteUri,
            new Uri(_host.Cdc.PrecipitationNowDirectory!, "10minutenwerte_nieder_00044_now.zip").AbsoluteUri,
            new Uri(_host.Cdc.SolarNowDirectory!, "10minutenwerte_SOLAR_00044_now.zip").AbsoluteUri
        });
    }

    [TestMethod]
    public async Task Library_SelectMosmix_ToJsonXmlMqtt()
    {
        _host.ArrangeMosmix();
        await _host.Provider.GetRequiredService<IMosmixSEvidenceRefresher>().RefreshAsync(CancellationToken.None);

        var list = await _host.Provider.GetRequiredService<IMosmixLocationSelector>().SelectAsync(ConsumerSelectionHost.Parameters(), CancellationToken.None);

        var json = await _host.JsonAsync(list);
        var xml = await _host.XmlAsync(list);
        var mqtt = await ConsumerLibrary.Mqtt(_host, list);

        var jsonText = Encoding.UTF8.GetString(json);
        jsonText.Should().Contain("\"source\":\"dwd:mosmix-stationskatalog\"");

        ConsumerSelectionHost.ValidateAgainstPackagedSchema(xml).Should().BeEmpty();

        mqtt.Topic.Should().Be("haus/weather/stations/mosmix/selection");
        mqtt.Retain.Should().BeFalse();
        mqtt.MessageType.Should().Be(MqttMessageType.MosmixLocationList);
        mqtt.Payload.ToArray().Should().Equal(json);

        _host.Handler.Requests.Count.Should().Be(4);
    }

    [TestMethod]
    public async Task Library_ParseDwd_ToJsonXmlMqtt()
    {
        _host.ArrangeCdcFullStation();

        var dataset = await _host.Provider.GetRequiredService<IDwdObservationParser>().ParseAsync("00044", CancellationToken.None);

        var json = await _host.JsonAsync(dataset);
        var xml = await _host.XmlAsync(dataset);
        var mqtt = await ConsumerLibrary.Mqtt(_host, dataset);

        mqtt.Topic.Should().Be("haus/weather/measurements/dwd:00044/state");
        mqtt.Retain.Should().BeTrue();
        mqtt.MessageType.Should().Be(MqttMessageType.Observation);
        mqtt.Payload.ToArray().Should().Equal(json);

        ConsumerSelectionHost.ValidateAgainstPackagedSchema(xml).Should().BeEmpty();

        _host.Handler.Requests.Count.Should().Be(8);
    }

    [TestMethod]
    public async Task Library_ParseOsthessenWetter_Synthetic_ToJsonXmlMqtt()
    {
        ConsumerLibrary.ArrangeOsthessen(_host, BaseUri);

        var result = await _host.Provider.GetRequiredService<IOsthessenWetterParser>().ParseAsync("Bimbach", CancellationToken.None);
        var dataset = result.Datasets.Single();

        var json = await _host.JsonAsync(dataset);
        var xml = await _host.XmlAsync(dataset);
        var mqtt = await ConsumerLibrary.Mqtt(_host, dataset);

        mqtt.Topic.Should().Be("haus/weather/measurements/osthessenwetter:bimbach/state");
        mqtt.Retain.Should().BeTrue();
        mqtt.MessageType.Should().Be(MqttMessageType.Observation);
        mqtt.Payload.ToArray().Should().Equal(json);

        ConsumerSelectionHost.ValidateAgainstPackagedSchema(xml).Should().BeEmpty();

        _host.Handler.Requests.Should().Equal(new[] { ConsumerLibrary.PageUri(BaseUri).AbsoluteUri });
    }

    [TestMethod]
    public async Task Library_ParseMosmixL_ToJsonXmlMqtt()
    {
        ConsumerLibrary.ArrangeMosmixL(_host, BaseUri, "10637");

        var result = await _host.Provider.GetRequiredService<IMosmixLParser>().ParseAsync("10637", CancellationToken.None);

        var json = await _host.JsonAsync(result.Run);
        var xml = await _host.XmlAsync(result.Run);
        var mqtt = await ConsumerLibrary.Mqtt(_host, result.Run);

        mqtt.Topic.Should().Be("haus/weather/forecasts/dwd-mosmix-l:10637/state");
        mqtt.Retain.Should().BeTrue();
        mqtt.MessageType.Should().Be(MqttMessageType.Forecast);
        mqtt.Payload.ToArray().Should().Equal(json);

        var jsonText = Encoding.UTF8.GetString(json);
        jsonText.Should().NotContain("measured_at");

        ConsumerSelectionHost.ValidateAgainstPackagedSchema(xml).Should().BeEmpty();

        _host.Handler.Requests.Should().Equal(new[] { ConsumerLibrary.MosmixLUri(BaseUri, "10637").AbsoluteUri });
    }

    [TestMethod]
    public async Task Library_ParseMosmixS_ToJsonXmlMqtt()
    {
        ConsumerLibrary.ArrangeMosmixS(_host, BaseUri, "10637");

        var result = await _host.Provider.GetRequiredService<IMosmixSParser>().ParseAsync("10637", CancellationToken.None);

        var json = await _host.JsonAsync(result.Run);
        var xml = await _host.XmlAsync(result.Run);
        var mqtt = await ConsumerLibrary.Mqtt(_host, result.Run);

        mqtt.Topic.Should().Be("haus/weather/forecasts/dwd-mosmix-s:10637/state");
        mqtt.Retain.Should().BeTrue();
        mqtt.MessageType.Should().Be(MqttMessageType.Forecast);
        mqtt.Payload.ToArray().Should().Equal(json);

        var jsonText = Encoding.UTF8.GetString(json);
        jsonText.Should().NotContain("measured_at");

        ConsumerSelectionHost.ValidateAgainstPackagedSchema(xml).Should().BeEmpty();

        _host.Handler.Requests.Should().Equal(new[] { ConsumerLibrary.MosmixSUri(BaseUri).AbsoluteUri });
    }

    [TestMethod]
    public async Task Library_TwoContainers_IndependentEndpointsAndTopics()
    {
        using var hostA = ConsumerLibrary.Host("https://a.example.invalid/");
        using var hostB = ConsumerLibrary.Host("https://b.example.invalid/", ConsumerLibrary.ExampleTopics("other/weather"));

        ConsumerLibrary.ArrangeMosmixL(hostA, "https://a.example.invalid/", "10637");
        ConsumerLibrary.ArrangeMosmixL(hostB, "https://b.example.invalid/", "10637");

        var resultA = await hostA.Provider.GetRequiredService<IMosmixLParser>().ParseAsync("10637", CancellationToken.None);
        var resultB = await hostB.Provider.GetRequiredService<IMosmixLParser>().ParseAsync("10637", CancellationToken.None);

        var mqttA = await ConsumerLibrary.Mqtt(hostA, resultA.Run);
        var mqttB = await ConsumerLibrary.Mqtt(hostB, resultB.Run);

        mqttA.Topic.Should().StartWith("haus/weather/forecasts/");
        mqttB.Topic.Should().StartWith("other/weather/forecasts/");

        hostA.Handler.Requests.Should().Equal(new[] { "https://a.example.invalid/forecast/single_stations/10637/kml/MOSMIX_L_LATEST_10637.kmz" });
        hostB.Handler.Requests.Should().Equal(new[] { "https://b.example.invalid/forecast/single_stations/10637/kml/MOSMIX_L_LATEST_10637.kmz" });

        var serializerA = hostA.Provider.GetRequiredService<IMqttMessageSerializer>();
        var serializerB = hostB.Provider.GetRequiredService<IMqttMessageSerializer>();

        serializerA.Should().NotBeSameAs(serializerB);
    }

    [TestMethod]
    public async Task Library_NoBrokerOrCliAssembly()
    {
        var assembly = typeof(MqttMessageSerializer).Assembly;
        var referenced = assembly.GetReferencedAssemblies();

        referenced.Should().NotContain(a => (a.Name ?? string.Empty).Contains("Mqtt", StringComparison.OrdinalIgnoreCase) && !(a.Name ?? string.Empty).Equals(assembly.GetName().Name, StringComparison.OrdinalIgnoreCase));
        referenced.Should().NotContain(a => (a.Name ?? string.Empty).Contains("Cli", StringComparison.OrdinalIgnoreCase));

        var publicTypes = assembly.GetExportedTypes();
        publicTypes.Should().NotContain(t => t.FullName!.Contains(".Cli.", StringComparison.OrdinalIgnoreCase));
    }
}
