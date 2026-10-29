using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Selection;
using WeatherParser.Parsing.Tests.Export;
using WeatherParser.Parsing.Tests.Forecasts.Mosmix;

namespace WeatherParser.Parsing.Tests.Mqtt;

[TestClass]
public sealed class MqttForecastMessageTests
{
    private MockRepository _mocks = null!;
    private Mock<HttpMessageHandler> _handler = null!;
    private HttpClient _http = null!;
    private ManualTimeProvider _clock = null!;
    private readonly List<Uri> _requests = new();

    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _handler = _mocks.Create<HttpMessageHandler>();
        _http = new HttpClient(_handler.Object, disposeHandler: false);
        _clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 26, 16, 16, 30, TimeSpan.Zero));
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _http.Dispose();
        _mocks.VerifyAll();
    }

    private ServiceProvider Provider()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.AddSingleton(_http);
        services.AddSingleton<TimeProvider>(_clock);
        services.AddSingleton(new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") });
        services.AddSingleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz") });
        services.AddSingleton(new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = TimeSpan.FromHours(8) });
        services.AddSingleton(new MosmixSForecastOptions { MaxRunAge = TimeSpan.FromHours(8) });
        var mqttOptions = new MqttTopicOptions
        {
            Mappings = new Dictionary<MqttMessageType, MqttTopicMapping>
            {
                [MqttMessageType.Observation] = new MqttTopicMapping { TopicTemplate = "haus/weather/measurements/{source}/state" },
                [MqttMessageType.Forecast] = new MqttTopicMapping { TopicTemplate = "haus/weather/forecasts/{source}/state" }
            }
        };
        services.Replace(ServiceDescriptor.Singleton(mqttOptions));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private void Respond(byte[] kmz) =>
        _handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback((HttpRequestMessage request, CancellationToken _) => _requests.Add(request.RequestUri!))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(kmz) });

    private static string Ttt => MosmixForecastKmzBuilder.Forecast("TTT", "283.15 284.15 285.15");

    private static int StepCount(MqttMessage message) => JsonDocument.Parse(message.Payload).RootElement.GetProperty("forecasts").GetArrayLength();

    private static async Task<byte[]> JsonBytes(ForecastRun run)
    {
        using var stream = new MemoryStream();
        await new JsonExportSerializer().SerializeAsync(run, stream, default);
        return stream.ToArray();
    }

    [TestMethod]
    public async Task MosmixLRun_OneMessageWithEveryStep()
    {
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixLParser>();
        var serializer = provider.GetRequiredService<IMqttMessageSerializer>();
        Respond(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "BEISPIELSTADT", "9.6,50.5,250.0", Ttt)));

        var result = await parser.ParseAsync("10637", CancellationToken.None);
        var message = await serializer.SerializeAsync(result.Run, CancellationToken.None);

        message.MessageType.Should().Be(MqttMessageType.Forecast);
        message.Topic.Should().Be("haus/weather/forecasts/dwd-mosmix-l:10637/state");
        message.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        message.Retain.Should().BeTrue();
        StepCount(message).Should().Be(result.Run.Forecasts.Count);
        var payload = Encoding.UTF8.GetString(message.Payload.Span);
        payload.Should().Contain("\"issued_at\"");
        payload.Should().NotContain("\"measured_at\"");
        message.Payload.ToArray().Should().Equal(await JsonBytes(result.Run));
    }

    [TestMethod]
    public async Task MosmixSRun_OneMessageWithEveryStep()
    {
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixSParser>();
        var serializer = provider.GetRequiredService<IMqttMessageSerializer>();
        Respond(MosmixForecastKmzBuilder.Kmz(
            MosmixForecastKmzBuilder.Document(
                MosmixForecastKmzBuilder.ProductDefinition(),
                MosmixForecastKmzBuilder.Placemark("P0001", "OTHER", "8.0,51.0,100.0", Ttt),
                MosmixForecastKmzBuilder.Placemark("10637", "BEISPIELSTADT", "9.6,50.5,250.0", Ttt)),
            memberName: MosmixForecastKmzBuilder.AllStationsMemberName));

        var result = await parser.ParseAsync("10637", CancellationToken.None);
        var message = await serializer.SerializeAsync(result.Run, CancellationToken.None);

        message.MessageType.Should().Be(MqttMessageType.Forecast);
        message.Topic.Should().Be("haus/weather/forecasts/dwd-mosmix-s:10637/state");
        message.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        message.Retain.Should().BeTrue();
        StepCount(message).Should().Be(result.Run.Forecasts.Count);
        var payload = Encoding.UTF8.GetString(message.Payload.Span);
        payload.Should().Contain("\"issued_at\"");
        payload.Should().NotContain("\"measured_at\"");
        message.Payload.ToArray().Should().Equal(await JsonBytes(result.Run));
    }

    [TestMethod]
    public async Task SameIdBothProducts_DistinctTopics()
    {
        using var provider = Provider();
        var lParser = provider.GetRequiredService<IMosmixLParser>();
        var sParser = provider.GetRequiredService<IMosmixSParser>();
        var serializer = provider.GetRequiredService<IMqttMessageSerializer>();
        Respond(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "BEISPIELSTADT", "9.6,50.5,250.0", Ttt)));

        var lResult = await lParser.ParseAsync("10637", CancellationToken.None);
        var sResult = await sParser.ParseAsync("10637", CancellationToken.None);
        var lMessage = await serializer.SerializeAsync(lResult.Run, CancellationToken.None);
        var sMessage = await serializer.SerializeAsync(sResult.Run, CancellationToken.None);

        lMessage.Topic.Should().NotBe(sMessage.Topic);
    }

    [TestMethod]
    public async Task TwoLocations_DistinctTopics()
    {
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixLParser>();
        var serializer = provider.GetRequiredService<IMqttMessageSerializer>();
        _handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback((HttpRequestMessage request, CancellationToken _) => _requests.Add(request.RequestUri!))
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                var id = request.RequestUri!.AbsolutePath.Contains("P0489") ? "P0489" : "10637";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single(id, "BEISPIELSTADT", "9.6,50.5,250.0", Ttt)))
                };
            });

        var result1 = await parser.ParseAsync("10637", CancellationToken.None);
        var result2 = await parser.ParseAsync("P0489", CancellationToken.None);
        var message1 = await serializer.SerializeAsync(result1.Run, CancellationToken.None);
        var message2 = await serializer.SerializeAsync(result2.Run, CancellationToken.None);

        message1.Topic.Should().NotBe(message2.Topic);
    }

    [TestMethod]
    public async Task ForecastPayload_HasNoMeasuredAt()
    {
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixLParser>();
        var serializer = provider.GetRequiredService<IMqttMessageSerializer>();
        Respond(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "BEISPIELSTADT", "9.6,50.5,250.0", Ttt)));

        var result = await parser.ParseAsync("10637", CancellationToken.None);
        var message = await serializer.SerializeAsync(result.Run, CancellationToken.None);

        var payload = Encoding.UTF8.GetString(message.Payload.Span);
        payload.Should().NotContain("\"measured_at\"");
        payload.Should().Contain("\"issued_at\"");
    }

    [TestMethod]
    public async Task ForecastRun_AlwaysForecastType()
    {
        using var provider = Provider();
        var lParser = provider.GetRequiredService<IMosmixLParser>();
        var sParser = provider.GetRequiredService<IMosmixSParser>();
        var serializer = provider.GetRequiredService<IMqttMessageSerializer>();
        Respond(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "BEISPIELSTADT", "9.6,50.5,250.0", Ttt)));

        var lResult = await lParser.ParseAsync("10637", CancellationToken.None);
        var sResult = await sParser.ParseAsync("10637", CancellationToken.None);
        var lMessage = await serializer.SerializeAsync(lResult.Run, CancellationToken.None);
        var sMessage = await serializer.SerializeAsync(sResult.Run, CancellationToken.None);

        lMessage.MessageType.Should().Be(MqttMessageType.Forecast);
        sMessage.MessageType.Should().Be(MqttMessageType.Forecast);
    }

    [TestMethod]
    public async Task ObservationAndForecastTopics_NeverEqual_ForExampleConfiguration()
    {
        using var provider = Provider();
        var lParser = provider.GetRequiredService<IMosmixLParser>();
        var serializer = provider.GetRequiredService<IMqttMessageSerializer>();
        Respond(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "BEISPIELSTADT", "9.6,50.5,250.0", Ttt)));

        var lResult = await lParser.ParseAsync("10637", CancellationToken.None);
        var lMessage = await serializer.SerializeAsync(lResult.Run, CancellationToken.None);
        var obsMessage = await serializer.SerializeAsync(ObservationTestData.ValidDataset(), CancellationToken.None);

        lMessage.Topic.Should().NotBe(obsMessage.Topic);
        lMessage.Topic.Should().StartWith("haus/weather/forecasts/");
        obsMessage.Topic.Should().StartWith("haus/weather/measurements/");
    }

    [TestMethod]
    public async Task StaleRun_MarkerInPayload()
    {
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixLParser>();
        var serializer = provider.GetRequiredService<IMqttMessageSerializer>();
        Respond(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "BEISPIELSTADT", "9.6,50.5,250.0", Ttt)));
        _clock.Advance(TimeSpan.FromHours(9));

        var result = await parser.ParseAsync("10637", CancellationToken.None);
        var message = await serializer.SerializeAsync(result.Run, CancellationToken.None);

        result.Run.RunAgeExceeded.Should().BeTrue();
        var payload = Encoding.UTF8.GetString(message.Payload.Span);
        payload.Should().Contain("\"run_age_exceeded\":true");
    }

    [TestMethod]
    public async Task LargestRunPayloadSize_Recorded()
    {
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixLParser>();
        var serializer = provider.GetRequiredService<IMqttMessageSerializer>();
        static string Series(string value) => string.Join(" ", Enumerable.Repeat(value, 1000));
        Respond(MosmixForecastKmzBuilder.Kmz(
            MosmixForecastKmzBuilder.Document(
                MosmixForecastKmzBuilder.ProductDefinition(timeSteps: 1000),
                MosmixForecastKmzBuilder.Placemark("10637", "BEISPIELSTADT", "9.6,50.5,250.0",
                    MosmixForecastKmzBuilder.Forecast("TTT", Series("283.15")),
                    MosmixForecastKmzBuilder.Forecast("PPPP", Series("101325.0")),
                    MosmixForecastKmzBuilder.Forecast("DD", Series("270.0")),
                    MosmixForecastKmzBuilder.Forecast("FF", Series("5.14")),
                    MosmixForecastKmzBuilder.Forecast("FX1", Series("12.35")),
                    MosmixForecastKmzBuilder.Forecast("RR1c", Series("0.55")),
                    MosmixForecastKmzBuilder.Forecast("Rad1h", Series("1234.5"))))));

        var result = await parser.ParseAsync("10637", CancellationToken.None);
        var message = await serializer.SerializeAsync(result.Run, CancellationToken.None);

        StepCount(message).Should().Be(1000);
        TestContext.WriteLine($"payload bytes: {message.Payload.Length}");
        message.Payload.Length.Should().BeGreaterThan(1000);
        message.Payload.ToArray().Should().Equal(await JsonBytes(result.Run));
    }

    [TestMethod]
    public async Task SameForecastRun_DifferentRetrievedAt_SameSourceAndIssuedAt()
    {
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixLParser>();
        var serializer = provider.GetRequiredService<IMqttMessageSerializer>();
        Respond(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "BEISPIELSTADT", "9.6,50.5,250.0", Ttt)));

        var result1 = await parser.ParseAsync("10637", CancellationToken.None);
        _clock.Advance(TimeSpan.FromMinutes(1));
        var result2 = await parser.ParseAsync("10637", CancellationToken.None);
        var message1 = await serializer.SerializeAsync(result1.Run, CancellationToken.None);
        var message2 = await serializer.SerializeAsync(result2.Run, CancellationToken.None);

        message1.Topic.Should().Be(message2.Topic);
        result1.Run.Source.Should().Be(result2.Run.Source);
        result1.Run.IssuedAtUtc.Should().Be(result2.Run.IssuedAtUtc);
        result1.Run.RetrievedAtUtc.Should().NotBe(result2.Run.RetrievedAtUtc);
        message1.Payload.Span.ToArray().Should().NotEqual(message2.Payload.Span.ToArray());
    }
}
