using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Selection;

namespace WeatherParser.Parsing.Tests.Forecasts.Mosmix;

[TestClass]
public sealed class ComposedMosmixLPipelineTests
{
    private MockRepository _mocks = null!;
    private Mock<HttpMessageHandler> _handler = null!;
    private HttpClient _http = null!;
    private ManualTimeProvider _clock = null!;
    private CapturingLogger<MosmixLParser> _logger = null!;
    private readonly List<Uri> _requests = new();

    [TestInitialize] public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _handler = _mocks.Create<HttpMessageHandler>();
        _http = new HttpClient(_handler.Object, disposeHandler: false);
        _clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 26, 16, 16, 30, TimeSpan.Zero));
        _logger = new();
    }
    [TestCleanup] public void TestCleanup() { _http.Dispose(); _mocks.VerifyAll(); }

    private ServiceProvider Provider(MosmixLForecastOptions? options = null, Uri? directory = null, bool validate = true)
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.AddSingleton(_http);
        services.AddSingleton<TimeProvider>(_clock);
        services.AddSingleton(new MosmixLForecastEndpoints { SingleStationsDirectory = directory ?? new Uri("https://example.org/mosmix/single_stations/") });
        services.AddSingleton(options ?? new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = TimeSpan.FromHours(8) });
        services.AddSingleton<ILogger<MosmixLParser>>(_logger);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = validate, ValidateScopes = true });
    }

    private void Respond(byte[] kmz) =>
        _handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback((HttpRequestMessage request, CancellationToken _) => _requests.Add(request.RequestUri!))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(kmz) });

    private static string Ttt => MosmixForecastKmzBuilder.Forecast("TTT", "283.15 284.15 285.15");

    [TestMethod]
    public void AddWeatherParsing_ResolvesMosmixLParser()
    {
        using var provider = Provider();
        var parser1 = provider.GetRequiredService<IMosmixLParser>();
        var parser2 = provider.GetRequiredService<IMosmixLParser>();
        parser1.Should().BeOfType<MosmixLParser>();
        parser1.Should().BeSameAs(parser2);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_ValidFile_RunWithProductSourceAndLocation()
    {
        // Arrange
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixLParser>();
        Respond(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "BEISPIELSTADT", "9.6,50.5,250.0", Ttt)));

        // Act
        var result = await parser.ParseAsync("10637", CancellationToken.None);

        // Assert
        result.Run.Product.Should().Be(MosmixProduct.MosmixL);
        result.Run.Source.Should().Be("dwd-mosmix-l:10637");
        result.Run.MosmixId.Should().Be("10637");
        result.Run.Location.Name.Should().Be("BEISPIELSTADT");
        result.Run.Location.Latitude.Should().Be(50.5m);
        result.Run.Location.Longitude.Should().Be(9.6m);
        result.Run.Location.AltitudeM.Should().Be(250m);
        result.Run.RunAgeExceeded.Should().BeFalse();
        result.Freshness.Should().Be(MosmixRunFreshness.Fresh);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_IssuedAtFromFile_ValidAtPerStep_NoMeasuredAt()
    {
        // Arrange
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixLParser>();
        Respond(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "BEISPIELSTADT", "9.6,50.5,250.0", Ttt)));

        // Act
        var result = await parser.ParseAsync("10637", CancellationToken.None);

        // Assert
        result.Run.IssuedAtUtc.Should().Be(new DateTime(2026, 10, 26, 15, 0, 0, DateTimeKind.Utc));
        result.Run.RetrievedAtUtc.Should().Be(new DateTime(2026, 10, 26, 16, 16, 30, DateTimeKind.Utc));
        result.Run.Forecasts.Select(f => f.ValidAtUtc).Should().Equal(
            new DateTime(2026, 10, 26, 16, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 26, 17, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 26, 18, 0, 0, DateTimeKind.Utc));
        System.Text.Json.JsonSerializer.Serialize(result.Run).Should().NotContain("measured_at");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_OneRunOneRequest()
    {
        // Arrange
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixLParser>();
        Respond(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "BEISPIELSTADT", "9.6,50.5,250.0", Ttt)));

        // Act
        await parser.ParseAsync("10637", CancellationToken.None);

        // Assert
        _requests.Should().Equal(new Uri("https://example.org/mosmix/single_stations/10637/kml/MOSMIX_L_LATEST_10637.kmz"));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_Composed_BodyStalledAfterHeaders_DeadlineExceeded()
    {
        // Arrange
        using var provider = Provider(new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(10), MaxRunAge = TimeSpan.FromHours(8) });
        var parser = provider.GetRequiredService<IMosmixLParser>();
        _handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StallStream()) });

        // Act
        var task = parser.ParseAsync("10637", CancellationToken.None);
        await Task.Yield();
        _clock.Advance(TimeSpan.FromSeconds(11));

        // Assert
        await FluentActions.Awaiting(() => task).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-DEADLINE-EXCEEDED" && ex.Product == MosmixProduct.MosmixL);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_NotExportedElements_FileOrder()
    {
        // Arrange
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixLParser>();
        var forecasts = new[] { Ttt, MosmixForecastKmzBuilder.Forecast("wwP", "10.00 20.00 30.00"), MosmixForecastKmzBuilder.Forecast("R101", "1.00 2.00 3.00") };
        Respond(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "BEISPIELSTADT", "9.6,50.5,250.0", forecasts)));

        // Act
        var result = await parser.ParseAsync("10637", CancellationToken.None);

        // Assert
        result.NotExportedElements.Should().Equal("wwP", "R101");
        _logger.Count("MOSMIX-ELEMENT-NOT-EXPORTED").Should().Be(2);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_Logs_NoValuesNoFileTextNoMemberName()
    {
        // Arrange
        using var provider = Provider(new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = TimeSpan.FromHours(1) });
        var parser = provider.GetRequiredService<IMosmixLParser>();
        var forecasts = new[] { Ttt, MosmixForecastKmzBuilder.Forecast("SunD1", "1234.56 2345.67 3456.78") };
        Respond(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "host.example user42", "9.6,50.5,250.0", forecasts), memberName: "MARKERq7z_10637.kml"));

        // Act
        var result = await parser.ParseAsync("10637", CancellationToken.None);

        // Assert
        _logger.Entries.Should().NotBeEmpty();
        foreach (var entry in _logger.Entries)
        {
            entry.Message.Should().NotContain("283.15");
            entry.Message.Should().NotContain("1234.56");
            entry.Message.Should().NotContain("host.example");
            entry.Message.Should().NotContain("user42");
            entry.Message.Should().NotContain("MARKERq7z");
            entry.Message.Should().Contain("https://example.org/mosmix/single_stations/");
        }
        _logger.Entries.Should().NotContain(e => e.Message.Contains("MARKERq7z"));
        result.Run.Location.Name.Should().Be("host.example user42");
        result.Freshness.Should().Be(MosmixRunFreshness.Stale);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_FormatFailure_MemberNameNeverInLogsOrMessages()
    {
        // Arrange
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixLParser>();
        var kml = MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(issueTime: null), MosmixForecastKmzBuilder.Placemark("10637", "BEISPIELSTADT", "9.6,50.5,250.0", Ttt));
        Respond(MosmixForecastKmzBuilder.Kmz(kml, memberName: "MARKERq7z_10637.kml"));

        // Act
        var act = () => parser.ParseAsync("10637", CancellationToken.None);
        var exception = (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()).Which;

        // Assert
        exception.ErrorId.Should().Be("MOSMIX-ISSUE-TIME-MISSING");
        exception.Product.Should().Be(MosmixProduct.MosmixL);
        exception.Message.Should().NotContain("MARKERq7z");
        foreach (var entry in _logger.Entries)
        {
            entry.Message.Should().NotContain("MARKERq7z");
        }
        var errorEntry = _logger.Entries.Where(e => e.Level == LogLevel.Error).Single();
        errorEntry.EventName.Should().Be("MOSMIX-ISSUE-TIME-MISSING");
        errorEntry.Message.Should().Contain("https://example.org/mosmix/single_stations/");
        errorEntry.Message.Should().Contain("2026-10-26T16:16:30");
    }

    [TestMethod]
    public void Construct_EndpointWithUserInfo_RejectedWithoutSecretInMessageOrLogs()
    {
        using var provider = Provider(directory: new Uri("https://user:secret@example.org:8443/mosmix/single_stations/"), validate: false);
        Action act = () => provider.GetRequiredService<IMosmixLParser>();
        var exception = act.Should().Throw<MosmixForecastException>().Which;
        exception.ErrorId.Should().Be("MOSMIX-OPTIONS-INVALID");
        exception.Message.Should().NotContain("secret");
        _logger.Entries.Should().NotContain(e => e.Message.Contains("secret"));
    }

    private sealed class StallStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
