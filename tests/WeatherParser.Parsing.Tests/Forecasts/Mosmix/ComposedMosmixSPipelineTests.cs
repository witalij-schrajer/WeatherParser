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
public sealed class ComposedMosmixSPipelineTests
{
    private MockRepository _mocks = null!;
    private Mock<HttpMessageHandler> _handler = null!;
    private HttpClient _http = null!;
    private ManualTimeProvider _clock = null!;
    private CapturingLogger<MosmixSParser> _logger = null!;
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

    private ServiceProvider Provider(MosmixSForecastOptions? options = null)
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services.AddSingleton(_http);
        services.AddSingleton<TimeProvider>(_clock);
        services.AddSingleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz") });
        services.AddSingleton(options ?? new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = TimeSpan.FromHours(2) });
        services.AddSingleton<ILogger<MosmixSParser>>(_logger);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private void Respond(byte[] kmz) =>
        _handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback((HttpRequestMessage request, CancellationToken _) => _requests.Add(request.RequestUri!))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(kmz) });

    private void Respond(HttpContent content) =>
        _handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback((HttpRequestMessage request, CancellationToken _) => _requests.Add(request.RequestUri!))
            .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = content });

    private static string Ttt => MosmixForecastKmzBuilder.Forecast("TTT", "283.15 284.15 285.15");

    private static string Small(params string[] forecasts) => MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(), MosmixForecastKmzBuilder.Placemark("P0001", "OTHER", "8.0,51.0,100.0", Ttt), MosmixForecastKmzBuilder.Placemark("10637", "BEISPIELSTADT", "9.6,50.5,250.0", forecasts), MosmixForecastKmzBuilder.Placemark("P0002", "OTHER2", "8.5,51.5,120.0", Ttt));

    [TestMethod]
    public void AddWeatherParsing_ResolvesMosmixSParser()
    {
        using var provider = Provider();
        var parser1 = provider.GetRequiredService<IMosmixSParser>();
        var parser2 = provider.GetRequiredService<IMosmixSParser>();
        parser1.Should().BeOfType<MosmixSParser>();
        parser1.Should().BeSameAs(parser2);
        provider.GetRequiredService<IMosmixLParser>().Should().NotBeSameAs(parser1);
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task ParseAsync_RunWithProductSourceAndLocation()
    {
        // Arrange
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixSParser>();
        Respond(MosmixForecastKmzBuilder.Kmz(Small(Ttt), memberName: MosmixForecastKmzBuilder.AllStationsMemberName));

        // Act
        var result = await parser.ParseAsync("10637", CancellationToken.None);

        // Assert
        result.Run.Product.Should().Be(MosmixProduct.MosmixS);
        result.Run.Source.Should().Be("dwd-mosmix-s:10637");
        result.Run.MosmixId.Should().Be("10637");
        result.Run.Location.Name.Should().Be("BEISPIELSTADT");
        result.Run.Location.Latitude.Should().Be(50.5m);
        result.Run.Location.Longitude.Should().Be(9.6m);
        result.Run.Location.AltitudeM.Should().Be(250m);
        result.Run.RunAgeExceeded.Should().BeFalse();
        result.Freshness.Should().Be(MosmixRunFreshness.Fresh);
        result.Run.IssuedAtUtc.Should().Be(new DateTime(2026, 10, 26, 15, 0, 0, DateTimeKind.Utc));
        result.Run.RetrievedAtUtc.Should().Be(new DateTime(2026, 10, 26, 16, 16, 30, DateTimeKind.Utc));
        result.Run.Forecasts.Select(f => f.ValidAtUtc).Should().Equal(
            new DateTime(2026, 10, 26, 16, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 26, 17, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 26, 18, 0, 0, DateTimeKind.Utc));
    }

    [DataRow(0)]
    [DataRow(1000)]
    [DataRow(1999)]
    [TestMethod]
    [Timeout(20000)]
    public async Task ParseAsync_PlacemarkFirstMiddleLast_Run(int selectedIndex)
    {
        // Arrange
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixSParser>();
        Respond(MosmixForecastKmzBuilder.LargeAllStationsKmz(2000, selectedIndex, "10637", 24, 10, includeTemperature: true));

        // Act
        var result = await parser.ParseAsync("10637", CancellationToken.None);

        // Assert
        result.Run.MosmixId.Should().Be("10637");
        result.Run.Forecasts.Count.Should().Be(24);
        result.Run.Location.Name.Should().Be("STATION");
        _requests.Count.Should().Be(1);
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task ParseAsync_PlacemarkAbsent_ThrowsIdUnknown()
    {
        // Arrange
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixSParser>();
        Respond(MosmixForecastKmzBuilder.LargeAllStationsKmz(50, 0, "99999", 3, 2, includeTemperature: true));

        // Act
        var act = () => parser.ParseAsync("10637", CancellationToken.None);
        var exception = await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>();

        // Assert
        exception.Which.ErrorId.Should().Be("MOSMIX-ID-UNKNOWN");
        exception.Which.Product.Should().Be(MosmixProduct.MosmixS);
        _logger.Count("MOSMIX-ID-UNKNOWN").Should().Be(1);
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task ParseAsync_DuplicateIdAfterPlacemark_Fails()
    {
        // Arrange
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixSParser>();
        var kml = MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(), MosmixForecastKmzBuilder.Placemark("10637", "A", "9.6,50.5,250.0", Ttt), MosmixForecastKmzBuilder.Placemark("P0001", "B", "8.0,51.0,100.0", Ttt), MosmixForecastKmzBuilder.Placemark("10637", "C", "9.6,50.5,250.0", Ttt));
        Respond(MosmixForecastKmzBuilder.Kmz(kml, memberName: MosmixForecastKmzBuilder.AllStationsMemberName));

        // Act
        var act = () => parser.ParseAsync("10637", CancellationToken.None);
        var exception = await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>();

        // Assert
        exception.Which.ErrorId.Should().Be("MOSMIX-FORMAT-INVALID");
        exception.Which.Product.Should().Be(MosmixProduct.MosmixS);
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task ParseAsync_CorruptAfterPlacemark_Fails()
    {
        // Arrange
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixSParser>();
        var kml = Small(Ttt).Replace("</kml:Document>", "<kml:Broken>");
        Respond(MosmixForecastKmzBuilder.Kmz(kml, memberName: MosmixForecastKmzBuilder.AllStationsMemberName));

        // Act
        var act = () => parser.ParseAsync("10637", CancellationToken.None);
        var exception = await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>();

        // Assert
        exception.Which.ErrorId.Should().Be("MOSMIX-FORMAT-INVALID");
        exception.Which.Product.Should().Be(MosmixProduct.MosmixS);
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task ParseAsync_OneRunOneRequest()
    {
        // Arrange
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixSParser>();
        Respond(MosmixForecastKmzBuilder.Kmz(Small(Ttt), memberName: MosmixForecastKmzBuilder.AllStationsMemberName));

        // Act
        await parser.ParseAsync("10637", CancellationToken.None);

        // Assert
        _requests.Should().Equal(new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz"));
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task ParseAsync_StreamLongerThan30Seconds_WithinDeadline_Succeeds()
    {
        // Arrange
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixSParser>();
        var kmz = MosmixForecastKmzBuilder.Kmz(Small(Ttt), memberName: MosmixForecastKmzBuilder.AllStationsMemberName);
        Respond(new StreamContent(new SlowStartStream(kmz, () => _clock.Advance(TimeSpan.FromSeconds(31)))));

        // Act
        var result = await parser.ParseAsync("10637", CancellationToken.None);

        // Assert
        result.Run.MosmixId.Should().Be("10637");
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task ParseAsync_StalledBeyondDeadline_ClassifiedDeadlineNotTimeout()
    {
        // Arrange
        using var provider = Provider(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromSeconds(10), MaxRunAge = TimeSpan.FromHours(2) });
        var parser = provider.GetRequiredService<IMosmixSParser>();
        Respond(new StreamContent(new StallStream()));

        // Act
        var task = parser.ParseAsync("10637", CancellationToken.None);
        await Task.Yield();
        _clock.Advance(TimeSpan.FromSeconds(11));

        // Assert
        await FluentActions.Awaiting(() => task).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-DEADLINE-EXCEEDED" && ex.Product == MosmixProduct.MosmixS);
        _logger.Count("MOSMIX-DEADLINE-EXCEEDED").Should().Be(1);
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task ParseAsync_NotExportedElements_FileOrder()
    {
        // Arrange
        using var provider = Provider();
        var parser = provider.GetRequiredService<IMosmixSParser>();
        var forecasts = new[] { Ttt, MosmixForecastKmzBuilder.Forecast("wwP", "10.00 20.00 30.00"), MosmixForecastKmzBuilder.Forecast("R101", "1.00 2.00 3.00") };
        Respond(MosmixForecastKmzBuilder.Kmz(Small(forecasts), memberName: MosmixForecastKmzBuilder.AllStationsMemberName));

        // Act
        var result = await parser.ParseAsync("10637", CancellationToken.None);

        // Assert
        result.NotExportedElements.Should().Equal("wwP", "R101");
        _logger.Count("MOSMIX-ELEMENT-NOT-EXPORTED").Should().Be(2);
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task ParseAsync_Logs_NoValuesNoFileTextNoMemberName()
    {
        // Arrange
        using var provider = Provider(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = TimeSpan.FromHours(1) });
        var parser = provider.GetRequiredService<IMosmixSParser>();
        var kml = MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(), MosmixForecastKmzBuilder.Placemark("P0001", "OTHER", "8.0,51.0,100.0", Ttt), MosmixForecastKmzBuilder.Placemark("10637", "host.example user42", "9.6,50.5,250.0", Ttt, MosmixForecastKmzBuilder.Forecast("SunD1", "1234.56 2345.67 3456.78")));
        Respond(MosmixForecastKmzBuilder.Kmz(kml, memberName: "MARKERq7z_240.kml"));

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
            entry.Message.Should().Contain("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz");
            entry.Message.Should().Contain("MOSMIX_S");
        }
        result.Run.Location.Name.Should().Be("host.example user42");
        result.Freshness.Should().Be(MosmixRunFreshness.Stale);
    }

    [TestMethod]
    [Timeout(20000)]
    public async Task ParseAsync_FormatFailure_MemberNameNeverInLogsOrMessages()
    {
        // Arrange
        using var provider = Provider(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = TimeSpan.FromHours(1) });
        var parser = provider.GetRequiredService<IMosmixSParser>();
        var kml = MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(issueTime: null), MosmixForecastKmzBuilder.Placemark("10637", "BEISPIELSTADT", "9.6,50.5,250.0", Ttt));
        Respond(MosmixForecastKmzBuilder.Kmz(kml, memberName: "MARKERq7z_240.kml"));

        // Act
        var act = () => parser.ParseAsync("10637", CancellationToken.None);
        var exception = (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()).Which;

        // Assert
        exception.ErrorId.Should().Be("MOSMIX-ISSUE-TIME-MISSING");
        exception.Product.Should().Be(MosmixProduct.MosmixS);
        exception.Message.Should().NotContain("MARKERq7z");
        foreach (var entry in _logger.Entries)
        {
            entry.Message.Should().NotContain("MARKERq7z");
        }
        var errorEntry = _logger.Entries.Where(e => e.Level == LogLevel.Error).Single();
        errorEntry.EventName.Should().Be("MOSMIX-ISSUE-TIME-MISSING");
        errorEntry.Message.Should().Contain("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz");
        errorEntry.Message.Should().Contain("2026-10-26T16:16:30");
    }

    [TestMethod]
    public void GeneratedAllStationsFile_SizeWithinBounds()
    {
        var kmz = MosmixForecastKmzBuilder.LargeAllStationsKmz(2000, 1000, "10637", 24, 10, includeTemperature: true);
        Console.WriteLine($"generated all-stations KMZ: {kmz.Length} bytes");
        kmz.Length.Should().BeInRange(50_000, 8_000_000);
    }

    private sealed class SlowStartStream : Stream
    {
        private readonly MemoryStream _inner;
        private readonly Action _onFirstRead;
        private bool _first = true;

        public SlowStartStream(byte[] data, Action onFirstRead)
        {
            _inner = new MemoryStream(data);
            _onFirstRead = onFirstRead;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_first)
            {
                _first = false;
                _onFirstRead();
            }

            return await _inner.ReadAsync(buffer, cancellationToken);
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
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
