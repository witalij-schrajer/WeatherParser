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
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Selection;

namespace WeatherParser.Parsing.Tests.Forecasts.Mosmix;

[TestClass]
public sealed class MosmixSParserDiagnosticsTests
{
    private const string Endpoint = "https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz";

    private MockRepository _mocks = null!;
    private Mock<IMosmixSForecastSource> _source = null!;
    private Mock<IMosmixForecastDocumentReader> _reader = null!;
    private Mock<IMosmixElementMapper> _mapper = null!;
    private Mock<IForecastRunBuilder> _builder = null!;
    private ManualTimeProvider _clock = null!;
    private CapturingLogger<MosmixSParser> _logger = null!;
    private CancellationTokenSource _caller = null!;
    private MosmixSParser _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _source = _mocks.Create<IMosmixSForecastSource>();
        _source.SetupGet(s => s.SanitizedEndpoint).Returns(Endpoint);
        _reader = _mocks.Create<IMosmixForecastDocumentReader>();
        _mapper = _mocks.Create<IMosmixElementMapper>();
        _builder = _mocks.Create<IForecastRunBuilder>();
        _clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 26, 16, 16, 30, 250, TimeSpan.Zero));
        _logger = new();
        _caller = new();
        _subject = Create(Options());
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _caller.Dispose();
        _mocks.VerifyAll();
    }

    private MosmixSParser Create(MosmixSForecastOptions options) => new(options, _source.Object, _reader.Object, new MosmixParsePipeline(_mapper.Object, _builder.Object, _clock), _logger);

    private static MosmixSForecastOptions Options() => new() { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = TimeSpan.FromHours(2) };

    private static DateTime Retrieved => new(2026, 10, 26, 16, 16, 30, DateTimeKind.Utc);           // the clock truncated to whole seconds

    private static DateTime Issued => new(2026, 10, 26, 15, 0, 0, DateTimeKind.Utc);

    private static MosmixForecastDocument Doc(DateTime? issued = null) => new()
    {
        MosmixId = "10637", IssuedAtUtc = issued ?? Issued, Timeline = new[] { new DateTime(2026, 10, 26, 16, 0, 0, DateTimeKind.Utc) },
        LocationName = "BEISPIELSTADT", Latitude = 50.5m, Longitude = 9.6m, AltitudeM = 250m,
        Series = new[] { new MosmixForecastSeries { ElementName = "TTT", Values = new string?[] { "283.15" } } },
    };

    private static readonly ForecastRunInput Input = new() { Product = MosmixProduct.MosmixS, MosmixId = "10637" };

    private static MosmixMappingResult Mapping(string[]? absent = null, string[]? notExported = null) => new() { Input = Input, AbsentElements = absent ?? Array.Empty<string>(), NotExportedElements = notExported ?? Array.Empty<string>() };

    private static ForecastRun Run() => new() { MosmixId = "10637", Product = MosmixProduct.MosmixS };

    // The source runs the parser's reader callback (no ID argument for MOSMIX_S), optionally doing something with the deadline token first.
    private void ArrangeSource(MosmixForecastDocument document, Func<CancellationToken, Task>? during = null)
    {
        _source.Setup(s => s.RetrieveAsync(It.IsAny<Func<Stream, CancellationToken, Task<MosmixForecastDocument>>>(), It.IsAny<CancellationToken>(), _caller.Token))
            .Returns(async (Func<Stream, CancellationToken, Task<MosmixForecastDocument>> read, CancellationToken deadline, CancellationToken _) =>
            {
                if (during is not null) { await during(deadline); }
                return await read(new MemoryStream(), deadline);
            });
        _reader.Setup(r => r.ReadAllStationsAsync(It.IsAny<Stream>(), "10637", It.IsAny<CancellationToken>())).ReturnsAsync(document);
    }

    private void ArrangeSourceThrows(Func<CancellationToken, Exception> failure) =>
        _source.Setup(s => s.RetrieveAsync(It.IsAny<Func<Stream, CancellationToken, Task<MosmixForecastDocument>>>(), It.IsAny<CancellationToken>(), _caller.Token))
            .Returns((Func<Stream, CancellationToken, Task<MosmixForecastDocument>> _, CancellationToken deadline, CancellationToken _) => Task.FromException<MosmixForecastDocument>(failure(deadline)));

    private void ArrangeMap(MosmixForecastDocument document, bool stale, MosmixMappingResult mapping) =>
        _mapper.Setup(m => m.Map(document, MosmixProduct.MosmixS, Retrieved, stale)).Returns(mapping);

    private void ArrangeBuild(params OmittedForecastValue[] omitted) =>
        _builder.Setup(b => b.Build(Input)).Returns(new ForecastRunBuildResult(Run(), omitted));
    
    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_Failures_LoggedWithIdUtcProductEndpoint()
    {
        ArrangeSourceThrows(_ => new MosmixForecastException(MosmixForecastErrorIds.RetrievalFailed, MosmixProduct.MosmixS, "r"));

        var ex = await _subject.Invoking(s => s.ParseAsync("10637", _caller.Token))
            .Should().ThrowAsync<MosmixForecastException>();

        ex.Which.ErrorId.Should().Be(MosmixForecastErrorIds.RetrievalFailed);

        var entry = _logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Error);
        entry.EventName.Should().Be("MOSMIX-RETRIEVAL-FAILED");
        entry.Message.Should().Contain("MOSMIX_S");
        entry.Message.Should().Contain("10637");
        entry.Message.Should().Contain("2026-10-26T16:16:30");
        entry.Message.Should().Contain(Endpoint);
        entry.Message.Should().NotContain("MOSMIX_L");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_Notices_Logged()
    {
        _subject = Create(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = TimeSpan.FromHours(1) });
        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, true, Mapping(absent: new[] { "FX1" }));
        ArrangeBuild();

        var run = await _subject.ParseAsync("10637", _caller.Token);

        _logger.Count("MOSMIX-RUN-STALE").Should().Be(1);
        _logger.Count("MOSMIX-ELEMENT-ABSENT").Should().Be(1);

        var stale = _logger.Entries.Single(e => e.EventName == "MOSMIX-RUN-STALE");
        stale.Level.Should().Be(LogLevel.Warning);

        var absent = _logger.Entries.Single(e => e.EventName == "MOSMIX-ELEMENT-ABSENT");
        absent.Level.Should().Be(LogLevel.Warning);
        absent.Message.Should().Contain("FX1");
        absent.Message.Should().Contain("10637");
        absent.Message.Should().Contain(Endpoint);
        absent.Message.Should().Contain("2026-10-26T16:16:30");

        var staleEntry = _logger.Entries.Single(e => e.EventName == "MOSMIX-RUN-STALE");
        staleEntry.Message.Should().Contain(Endpoint);
        staleEntry.Message.Should().Contain("2026-10-26T16:16:30");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_Omissions_LoggedWithTimeKindReason()
    {
        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, false, Mapping());
        ArrangeBuild(new OmittedForecastValue(new DateTime(2026, 10, 26, 17, 0, 0, DateTimeKind.Utc), QuantityKind.Temperature, OmissionReason.OutOfRange));

        var run = await _subject.ParseAsync("10637", _caller.Token);

        _logger.Count("FORECAST-VALUE-OMITTED").Should().Be(1);

        var omitted = _logger.Entries.Single(e => e.EventName == "FORECAST-VALUE-OMITTED");
        omitted.Level.Should().Be(LogLevel.Warning);
        omitted.Message.Should().Contain("2026-10-26T17:00:00");
        omitted.Message.Should().Contain("Temperature");
        omitted.Message.Should().Contain("OutOfRange");
        omitted.Message.Should().Contain("10637");
        omitted.Message.Should().Contain(Endpoint);
        omitted.Message.Should().Contain("2026-10-26T16:16:30");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_NotExportedElements_LoggedOncePerElement()
    {
        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, false, Mapping(notExported: new[] { "wwP", "R101" }));
        ArrangeBuild();

        var run = await _subject.ParseAsync("10637", _caller.Token);

        var notExported = _logger.Entries
            .Where(e => e.EventName == "MOSMIX-ELEMENT-NOT-EXPORTED")
            .ToList();

        notExported.Should().HaveCount(2);
        notExported[0].Message.Should().Contain("wwP");
        notExported[1].Message.Should().Contain("R101");

        _logger.Entries.Should().NotContain(e => e.Message.Contains("283.15"));

        notExported[0].Message.Should().Contain(Endpoint);
        notExported[0].Message.Should().Contain("2026-10-26T16:16:30");
        notExported[1].Message.Should().Contain(Endpoint);
        notExported[1].Message.Should().Contain("2026-10-26T16:16:30");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_OmissionsLoggedBeforeRejection()
    {
        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, false, Mapping());
        var omission = new OmittedForecastValue(new DateTime(2026, 10, 26, 17, 0, 0, DateTimeKind.Utc), QuantityKind.Temperature, OmissionReason.OutOfRange);
        _builder.Setup(b => b.Build(Input)).Throws(new ForecastRunRejectedException("none", new[] { omission }));

        var ex = await _subject.Invoking(s => s.ParseAsync("10637", _caller.Token))
            .Should().ThrowAsync<MosmixForecastException>();

        ex.Which.ErrorId.Should().Be(MosmixForecastErrorIds.NoUsableValue);

        var omittedIndex = _logger.Entries.FindIndex(e => e.EventName == "FORECAST-VALUE-OMITTED");
        var rejectionIndex = _logger.Entries.FindIndex(e => e.EventName == "MOSMIX-NO-USABLE-VALUE" && e.Level == LogLevel.Error);

        omittedIndex.Should().BeGreaterThanOrEqualTo(0);
        rejectionIndex.Should().BeGreaterThanOrEqualTo(0);
        omittedIndex.Should().BeLessThan(rejectionIndex);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_InvalidId_LogWithoutId()
    {
        await _subject.Invoking(s => s.ParseAsync("ab:cd", _caller.Token))
            .Should().ThrowAsync<MosmixForecastException>();

        var entry = _logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Error);
        entry.EventName.Should().Be("MOSMIX-ID-INVALID");
        entry.Message.Should().Contain("MOSMIX_S");
        entry.Message.Should().NotContain("ab:cd");
        entry.Message.Should().Contain(Endpoint);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_CallerCancellation_NotLoggedAsError()
    {
        ArrangeSourceThrows(_ => { _caller.Cancel(); return new OperationCanceledException(_caller.Token); });

        await _subject.Invoking(s => s.ParseAsync("10637", _caller.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        _logger.Entries.Should().NotContain(e => e.Level == LogLevel.Error);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineExpiry_LoggedOnce()
    {
        ArrangeSourceThrows(_ => { _clock.Advance(TimeSpan.FromSeconds(300)); return new MosmixForecastException(MosmixForecastErrorIds.RetrievalFailed, MosmixProduct.MosmixS, "r"); });

        var ex = await _subject.Invoking(s => s.ParseAsync("10637", _caller.Token))
            .Should().ThrowAsync<MosmixForecastException>();

        ex.Which.ErrorId.Should().Be(MosmixForecastErrorIds.DeadlineExceeded);

        var entry = _logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Error);
        entry.EventName.Should().Be("MOSMIX-DEADLINE-EXCEEDED");
        entry.Message.Should().Contain(Endpoint);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_FormatFailure_LoggedWithEndpointAndUtc()
    {
        _source.Setup(s => s.RetrieveAsync(It.IsAny<Func<Stream, CancellationToken, Task<MosmixForecastDocument>>>(), It.IsAny<CancellationToken>(), _caller.Token))
            .Returns(async (Func<Stream, CancellationToken, Task<MosmixForecastDocument>> read, CancellationToken deadline, CancellationToken _) =>
            {
                return await read(new MemoryStream(), deadline);
            });
        _reader.Setup(r => r.ReadAllStationsAsync(It.IsAny<Stream>(), "10637", It.IsAny<CancellationToken>())).ThrowsAsync(new MosmixForecastFormatException("MOSMIX-FORMAT-INVALID", "bad file"));

        var ex = await _subject.Invoking(s => s.ParseAsync("10637", _caller.Token))
            .Should().ThrowAsync<MosmixForecastException>();

        ex.Which.ErrorId.Should().Be("MOSMIX-FORMAT-INVALID");

        var entry = _logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Error);
        entry.EventName.Should().Be("MOSMIX-FORMAT-INVALID");
        entry.Message.Should().Contain("MOSMIX_S");
        entry.Message.Should().Contain("10637");
        entry.Message.Should().Contain(Endpoint);
        entry.Message.Should().Contain("2026-10-26T16:16:30");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_Rejection_LoggedWithEndpointAndUtc()
    {
        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, false, Mapping());
        _builder.Setup(b => b.Build(Input)).Throws(new ForecastRunRejectedException("none", Array.Empty<OmittedForecastValue>()));

        var ex = await _subject.Invoking(s => s.ParseAsync("10637", _caller.Token))
            .Should().ThrowAsync<MosmixForecastException>();

        ex.Which.ErrorId.Should().Be(MosmixForecastErrorIds.NoUsableValue);

        var entry = _logger.Entries.Should().ContainSingle(e => e.EventName == "MOSMIX-NO-USABLE-VALUE" && e.Level == LogLevel.Error).Subject;
        entry.Message.Should().Contain("MOSMIX_S");
        entry.Message.Should().Contain("10637");
        entry.Message.Should().Contain(Endpoint);
        entry.Message.Should().Contain("2026-10-26T16:16:30");
    }
}
