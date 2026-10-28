using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Selection;

namespace WeatherParser.Parsing.Tests.Forecasts.Mosmix;

[TestClass]
public sealed class MosmixParsePipelineFinalCheckpointTests
{
    private MockRepository _mocks = null!;
    private Mock<IMosmixElementMapper> _mapper = null!;
    private Mock<IForecastRunBuilder> _builder = null!;
    private ManualTimeProvider _clock = null!;
    private CancellationTokenSource _caller = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _mapper = _mocks.Create<IMosmixElementMapper>();
        _builder = _mocks.Create<IForecastRunBuilder>();
        _clock = new(new DateTimeOffset(2026, 10, 26, 16, 16, 30, TimeSpan.Zero));
        _caller = new();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
        _caller.Dispose();
    }

    private static MosmixForecastDocument Doc() => new()
    {
        MosmixId = "10637",
        IssuedAtUtc = new DateTime(2026, 10, 26, 15, 0, 0, DateTimeKind.Utc),
        Timeline = new[] { new DateTime(2026, 10, 26, 16, 0, 0, DateTimeKind.Utc) },
        LocationName = "Test",
        Latitude = 49.0m,
        Longitude = 8.0m,
        Series = new[]
        {
            new MosmixForecastSeries
            {
                ElementName = "TTT",
                Values = new[] { "283.15" }
            }
        }
    };

    private static readonly ForecastRunInput Input = new()
    {
        Product = MosmixProduct.MosmixS,
        MosmixId = "10637"
    };

    private static MosmixMappingResult Mapping() => new()
    {
        Input = Input,
        AbsentElements = Array.Empty<string>(),
        NotExportedElements = Array.Empty<string>()
    };

    private static ForecastRun Run() => new()
    {
        MosmixId = "10637",
        Product = MosmixProduct.MosmixS
    };

    private static OmittedForecastValue Omission() => new(
        ValidAtUtc: new DateTime(2026, 10, 26, 16, 0, 0, DateTimeKind.Utc),
        Kind: QuantityKind.Temperature,
        Reason: OmissionReason.Missing);

    private static MosmixParseRequest Request(ILogger logger) => new()
    {
        Product = MosmixProduct.MosmixS,
        ParseDeadline = TimeSpan.FromSeconds(60),
        MaxRunAge = TimeSpan.FromHours(2),
        SanitizedEndpoint = "https://example.org/mosmix/all_stations/MOSMIX_S_LATEST_240.kmz",
        Retrieve = (id, read, deadline, token) => read(Stream.Null, deadline),
        Read = (stream, id, token) => Task.FromResult(Doc()),
        Logger = logger
    };

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_CallerCancelledDuringFinalOmissionLogging_ThrowsWithCallerToken()
    {
        var logger = new CancellingLogger(_caller);
        _mapper.Setup(m => m.Map(It.IsAny<MosmixForecastDocument>(), MosmixProduct.MosmixS, new DateTime(2026, 10, 26, 16, 16, 30, DateTimeKind.Utc), false)).Returns(Mapping());
        _builder.Setup(b => b.Build(Input)).Returns(new ForecastRunBuildResult(Run(), new[] { Omission(), Omission() }));

        var pipeline = new MosmixParsePipeline(_mapper.Object, _builder.Object, _clock);
        Func<Task> act = () => pipeline.ParseAsync(Request(logger), "10637", _caller.Token);

        await act.Should().ThrowExactlyAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == _caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineExpiresDuringFinalOmissionLogging_ThrowsDeadlineExceeded()
    {
        var logger = new ExpiringLogger(_clock);
        _mapper.Setup(m => m.Map(It.IsAny<MosmixForecastDocument>(), MosmixProduct.MosmixS, new DateTime(2026, 10, 26, 16, 16, 30, DateTimeKind.Utc), false)).Returns(Mapping());
        _builder.Setup(b => b.Build(Input)).Returns(new ForecastRunBuildResult(Run(), new[] { Omission(), Omission() }));

        var pipeline = new MosmixParsePipeline(_mapper.Object, _builder.Object, _clock);
        Func<Task> act = () => pipeline.ParseAsync(Request(logger), "10637", CancellationToken.None);

        await act.Should().ThrowExactlyAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == MosmixForecastErrorIds.DeadlineExceeded && ex.Product == MosmixProduct.MosmixS);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_CancellationAndDeadlineDuringFinalOmissionLogging_CancellationWins()
    {
        var logger = new BothLogger(_caller, _clock);
        _mapper.Setup(m => m.Map(It.IsAny<MosmixForecastDocument>(), MosmixProduct.MosmixS, new DateTime(2026, 10, 26, 16, 16, 30, DateTimeKind.Utc), false)).Returns(Mapping());
        _builder.Setup(b => b.Build(Input)).Returns(new ForecastRunBuildResult(Run(), new[] { Omission(), Omission() }));

        var pipeline = new MosmixParsePipeline(_mapper.Object, _builder.Object, _clock);
        Func<Task> act = () => pipeline.ParseAsync(Request(logger), "10637", _caller.Token);

        await act.Should().ThrowExactlyAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == _caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_OmissionsLoggedWithoutInterruption_ReturnsResult()
    {
        var logger = new CapturingLogger<MosmixSParser>();
        _mapper.Setup(m => m.Map(It.IsAny<MosmixForecastDocument>(), MosmixProduct.MosmixS, new DateTime(2026, 10, 26, 16, 16, 30, DateTimeKind.Utc), false)).Returns(Mapping());
        _builder.Setup(b => b.Build(Input)).Returns(new ForecastRunBuildResult(Run(), new[] { Omission(), Omission() }));

        var pipeline = new MosmixParsePipeline(_mapper.Object, _builder.Object, _clock);
        var result = await pipeline.ParseAsync(Request(logger), "10637", CancellationToken.None);

        result.OmittedValues.Should().HaveCount(2);
        logger.Count(MosmixNoticeIds.ValueOmitted).Should().Be(2);
    }

    private sealed class CancellingLogger(CancellationTokenSource source) : ILogger
    {
        private bool _first = true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (_first && eventId.Name == MosmixNoticeIds.ValueOmitted)
            {
                source.Cancel();
                _first = false;
            }
        }
    }

    private sealed class ExpiringLogger(ManualTimeProvider clock) : ILogger
    {
        private bool _first = true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (_first && eventId.Name == MosmixNoticeIds.ValueOmitted)
            {
                clock.Advance(TimeSpan.FromSeconds(61));
                _first = false;
            }
        }
    }

    private sealed class BothLogger(CancellationTokenSource source, ManualTimeProvider clock) : ILogger
    {
        private bool _first = true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (_first && eventId.Name == MosmixNoticeIds.ValueOmitted)
            {
                source.Cancel();
                clock.Advance(TimeSpan.FromSeconds(61));
                _first = false;
            }
        }
    }
}
