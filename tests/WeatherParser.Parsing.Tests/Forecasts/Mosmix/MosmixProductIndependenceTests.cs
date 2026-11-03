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
public sealed class MosmixProductIndependenceTests
{
    private MockRepository _mocks = null!;
    private Mock<HttpMessageHandler> _handler = null!;
    private HttpClient _http = null!;
    private ManualTimeProvider _clock = null!;
    private CapturingLogger<MosmixLParser> _lLogger = null!;
    private CapturingLogger<MosmixSParser> _sLogger = null!;
    private readonly List<Uri> _lRequests = new();
    private readonly List<Uri> _sRequests = new();
    private Func<HttpResponseMessage> _lResponse = null!;
    private Func<HttpResponseMessage> _sResponse = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _handler = _mocks.Create<HttpMessageHandler>();
        _http = new HttpClient(_handler.Object, disposeHandler: false);
        _clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 26, 16, 16, 30, TimeSpan.Zero));
        _lLogger = new();
        _sLogger = new();
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
        services.AddSingleton(new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://l.example.org/mosmix/single_stations/") });
        services.AddSingleton(new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = TimeSpan.FromHours(8) });
        services.AddSingleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://s.example.org/mosmix/MOSMIX_S_LATEST_240.kmz") });
        services.AddSingleton(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = TimeSpan.FromHours(1) });
        services.AddSingleton<ILogger<MosmixLParser>>(_lLogger);
        services.AddSingleton<ILogger<MosmixSParser>>(_sLogger);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private void SetupResponses()
    {
        _handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback((HttpRequestMessage request, CancellationToken _) =>
            {
                if (request.RequestUri!.Host == "l.example.org")
                {
                    _lRequests.Add(request.RequestUri);
                }
                else if (request.RequestUri!.Host == "s.example.org")
                {
                    _sRequests.Add(request.RequestUri);
                }
            })
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                if (request.RequestUri!.Host == "l.example.org")
                {
                    return _lResponse();
                }
                else if (request.RequestUri!.Host == "s.example.org")
                {
                    return _sResponse();
                }

                throw new InvalidOperationException("Unexpected host");
            });
    }

    private static string Ttt => MosmixForecastKmzBuilder.Forecast("TTT", "283.15 284.15 285.15");

    [TestMethod]
    [Timeout(10000)]
    public async Task BothParsers_OneContainer_EachUsesOwnEndpointAndOptions()
    {
        // Arrange
        using var provider = Provider();
        var l = provider.GetRequiredService<IMosmixLParser>();
        var s = provider.GetRequiredService<IMosmixSParser>();
        _lResponse = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "L-STADT", "9.6,50.5,250.0", Ttt))) };
        _sResponse = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(MosmixForecastKmzBuilder.Kmz(
            MosmixForecastKmzBuilder.Document(
                MosmixForecastKmzBuilder.ProductDefinition(),
                MosmixForecastKmzBuilder.Placemark("P0001", "OTHER", "8.0,51.0,100.0", Ttt),
                MosmixForecastKmzBuilder.Placemark("10637", "S-STADT", "9.6,50.5,250.0", Ttt)),
            memberName: MosmixForecastKmzBuilder.AllStationsMemberName)) };
        SetupResponses();

        // Act
        var lResult = await l.ParseAsync("10637", CancellationToken.None);
        var sResult = await s.ParseAsync("10637", CancellationToken.None);

        // Assert
        _lRequests.Should().Equal(new Uri("https://l.example.org/mosmix/single_stations/10637/kml/MOSMIX_L_LATEST_10637.kmz"));
        _sRequests.Should().Equal(new Uri("https://s.example.org/mosmix/MOSMIX_S_LATEST_240.kmz"));
        lResult.Freshness.Should().Be(MosmixRunFreshness.Fresh);
        lResult.Run.RunAgeExceeded.Should().BeFalse();
        sResult.Freshness.Should().Be(MosmixRunFreshness.Stale);
        sResult.Run.RunAgeExceeded.Should().BeTrue();
        _sLogger.Count("MOSMIX-RUN-STALE").Should().Be(1);
        _lLogger.Count("MOSMIX-RUN-STALE").Should().Be(0);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task BothParsers_SameId_DistinctSourcesAndProducts()
    {
        // Arrange
        using var provider = Provider();
        var l = provider.GetRequiredService<IMosmixLParser>();
        var s = provider.GetRequiredService<IMosmixSParser>();
        _lResponse = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "L-STADT", "9.6,50.5,250.0", Ttt))) };
        _sResponse = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(MosmixForecastKmzBuilder.Kmz(
            MosmixForecastKmzBuilder.Document(
                MosmixForecastKmzBuilder.ProductDefinition(),
                MosmixForecastKmzBuilder.Placemark("P0001", "OTHER", "8.0,51.0,100.0", Ttt),
                MosmixForecastKmzBuilder.Placemark("10637", "S-STADT", "9.6,50.5,250.0", Ttt)),
            memberName: MosmixForecastKmzBuilder.AllStationsMemberName)) };
        SetupResponses();

        // Act
        var lResult = await l.ParseAsync("10637", CancellationToken.None);
        var sResult = await s.ParseAsync("10637", CancellationToken.None);

        // Assert
        lResult.Run.Source.Should().Be("dwd-mosmix-l:10637");
        lResult.Run.Product.Should().Be(MosmixProduct.MosmixL);
        lResult.Run.Location.Name.Should().Be("L-STADT");
        sResult.Run.Source.Should().Be("dwd-mosmix-s:10637");
        sResult.Run.Product.Should().Be(MosmixProduct.MosmixS);
        sResult.Run.Location.Name.Should().Be("S-STADT");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task BothParsers_FailureOfOne_DoesNotAffectOther()
    {
        // Arrange
        using var provider = Provider();
        var l = provider.GetRequiredService<IMosmixLParser>();
        var s = provider.GetRequiredService<IMosmixSParser>();
        _lResponse = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "L-STADT", "9.6,50.5,250.0", Ttt))) };
        _sResponse = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(MosmixForecastKmzBuilder.Kmz(
            MosmixForecastKmzBuilder.Document(
                MosmixForecastKmzBuilder.ProductDefinition(),
                MosmixForecastKmzBuilder.Placemark("P0001", "OTHER", "8.0,51.0,100.0", Ttt),
                MosmixForecastKmzBuilder.Placemark("10637", "S-STADT", "9.6,50.5,250.0", Ttt)),
            memberName: MosmixForecastKmzBuilder.AllStationsMemberName)) };
        SetupResponses();

        // Act & Assert
        _sResponse = () => new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await FluentActions.Awaiting(() => s.ParseAsync("10637", CancellationToken.None)).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-RETRIEVAL-FAILED" && ex.Product == MosmixProduct.MosmixS);

        // Act & Assert
        var lResult = await l.ParseAsync("10637", CancellationToken.None);
        lResult.Run.Product.Should().Be(MosmixProduct.MosmixL);

        _lResponse = () => new HttpResponseMessage(HttpStatusCode.NotFound);
        _sResponse = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(MosmixForecastKmzBuilder.Kmz(
            MosmixForecastKmzBuilder.Document(
                MosmixForecastKmzBuilder.ProductDefinition(),
                MosmixForecastKmzBuilder.Placemark("P0001", "OTHER", "8.0,51.0,100.0", Ttt),
                MosmixForecastKmzBuilder.Placemark("10637", "S-STADT", "9.6,50.5,250.0", Ttt)),
            memberName: MosmixForecastKmzBuilder.AllStationsMemberName)) };

        await FluentActions.Awaiting(() => l.ParseAsync("10637", CancellationToken.None)).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-ID-UNKNOWN" && ex.Product == MosmixProduct.MosmixL);

        var sResult = await s.ParseAsync("10637", CancellationToken.None);
        sResult.Run.Product.Should().Be(MosmixProduct.MosmixS);

        foreach (var entry in _sLogger.Entries)
        {
            entry.Message.Should().Contain("MOSMIX_S");
            entry.Message.Should().NotContain("MOSMIX_L");
        }

        foreach (var entry in _lLogger.Entries)
        {
            entry.Message.Should().Contain("MOSMIX_L");
            entry.Message.Should().NotContain("MOSMIX_S");
        }
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task BothParsers_ConcurrentParses_NoSharedState()
    {
        // Arrange
        using var provider = Provider();
        var l = provider.GetRequiredService<IMosmixLParser>();
        var s = provider.GetRequiredService<IMosmixSParser>();

        var gate = new TaskCompletionSource();
        var lKmz = MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "L-STADT", "9.6,50.5,250.0", Ttt));
        var sKmz = MosmixForecastKmzBuilder.Kmz(
            MosmixForecastKmzBuilder.Document(
                MosmixForecastKmzBuilder.ProductDefinition(),
                MosmixForecastKmzBuilder.Placemark("P0001", "OTHER", "8.0,51.0,100.0", Ttt),
                MosmixForecastKmzBuilder.Placemark("10637", "S-STADT", "9.6,50.5,250.0", Ttt)),
            memberName: MosmixForecastKmzBuilder.AllStationsMemberName);

        _lResponse = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new GatedStream(lKmz, gate.Task)) };
        _sResponse = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new GatedStream(sKmz, gate.Task)) };
        SetupResponses();

        // Act
        var lTask = l.ParseAsync("10637", CancellationToken.None);
        var sTask = s.ParseAsync("10637", CancellationToken.None);

        while (_lRequests.Count < 1 || _sRequests.Count < 1)
        {
            await Task.Delay(10);
        }

        lTask.IsCompleted.Should().BeFalse();
        sTask.IsCompleted.Should().BeFalse();

        gate.SetResult();
        await Task.WhenAll(lTask, sTask);

        // Assert
        var lResult = await lTask;
        var sResult = await sTask;
        lResult.Run.Product.Should().Be(MosmixProduct.MosmixL);
        sResult.Run.Product.Should().Be(MosmixProduct.MosmixS);
        lResult.Run.Source.Should().Be("dwd-mosmix-l:10637");
        sResult.Run.Source.Should().Be("dwd-mosmix-s:10637");
        _lRequests.Count.Should().Be(1);
        _sRequests.Count.Should().Be(1);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task BothParsers_DeadlineOfOne_DoesNotExpireOther()
    {
        // Arrange
        using var provider = Provider();
        var l = provider.GetRequiredService<IMosmixLParser>();
        var s = provider.GetRequiredService<IMosmixSParser>();

        var gate = new TaskCompletionSource();
        var lKmz = MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single("10637", "L-STADT", "9.6,50.5,250.0", Ttt));
        var sKmz = MosmixForecastKmzBuilder.Kmz(
            MosmixForecastKmzBuilder.Document(
                MosmixForecastKmzBuilder.ProductDefinition(),
                MosmixForecastKmzBuilder.Placemark("P0001", "OTHER", "8.0,51.0,100.0", Ttt),
                MosmixForecastKmzBuilder.Placemark("10637", "S-STADT", "9.6,50.5,250.0", Ttt)),
            memberName: MosmixForecastKmzBuilder.AllStationsMemberName);

        _lResponse = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new GatedStream(lKmz, gate.Task)) };
        _sResponse = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new GatedStream(sKmz, gate.Task)) };
        SetupResponses();

        // Act
        var lTask = l.ParseAsync("10637", CancellationToken.None);
        var sTask = s.ParseAsync("10637", CancellationToken.None);

        while (_lRequests.Count < 1 || _sRequests.Count < 1)
        {
            await Task.Delay(10);
        }

        _clock.Advance(TimeSpan.FromSeconds(61));

        var lException = (await FluentActions.Awaiting(() => lTask).Should().ThrowAsync<MosmixForecastException>()).Which;
        lException.ErrorId.Should().Be("MOSMIX-DEADLINE-EXCEEDED");
        lException.Product.Should().Be(MosmixProduct.MosmixL);

        sTask.IsCompleted.Should().BeFalse();

        gate.SetResult();
        var sResult = await sTask;

        // Assert
        sResult.Run.Product.Should().Be(MosmixProduct.MosmixS);
        sResult.Run.Location.Name.Should().Be("S-STADT");
        _lLogger.Count("MOSMIX-DEADLINE-EXCEEDED").Should().Be(1);
        _sLogger.Count("MOSMIX-DEADLINE-EXCEEDED").Should().Be(0);
    }

    [TestMethod]
    public void PublicApi_NoProductSwitch()
    {
        var types = typeof(MosmixLParser).Assembly.GetTypes()
            .Where(t => t.Namespace == "WeatherParser.Parsing.Forecasts.Mosmix" && t.Name.EndsWith("Parser"))
            .Where(t => t.IsPublic)
            .Select(t => t.Name)
            .ToHashSet();

        types.Should().BeEquivalentTo(new[] { "IMosmixLParser", "MosmixLParser", "IMosmixSParser", "MosmixSParser" });

        foreach (var type in types)
        {
            var t = typeof(MosmixLParser).Assembly.GetType("WeatherParser.Parsing.Forecasts.Mosmix." + type)!;
            foreach (var constructor in t.GetConstructors())
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    parameter.ParameterType.Should().NotBe(typeof(MosmixProduct));
                }
            }

            foreach (var method in t.GetMethods())
            {
                if (method.DeclaringType == t)
                {
                    foreach (var parameter in method.GetParameters())
                    {
                        parameter.ParameterType.Should().NotBe(typeof(MosmixProduct));
                    }
                }
            }
        }
    }

    private sealed class GatedStream : Stream
    {
        private readonly MemoryStream _inner;
        private readonly Task _gate;

        public GatedStream(byte[] data, Task gate)
        {
            _inner = new(data);
            _gate = gate;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inner.Read(buffer, offset, count);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken);
            return await _inner.ReadAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
