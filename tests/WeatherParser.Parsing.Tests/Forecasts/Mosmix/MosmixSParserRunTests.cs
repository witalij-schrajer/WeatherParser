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
public sealed class MosmixSParserRunTests
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

    private static DateTime Retrieved => new(2026, 10, 26, 16, 16, 30, DateTimeKind.Utc);

    private static DateTime Issued => new(2026, 10, 26, 15, 0, 0, DateTimeKind.Utc);

    private static MosmixForecastDocument Doc(DateTime? issued = null) => new()
    {
        MosmixId = "10637", IssuedAtUtc = issued ?? Issued, Timeline = new[] { new DateTime(2026, 10, 26, 16, 0, 0, DateTimeKind.Utc) },
        LocationName = "BEISPIELSTADT", Latitude = 50.5m, Longitude = 9.6m, AltitudeM = 250m,
        Series = new[] { new MosmixForecastSeries { ElementName = "TTT", Values = new string?[] { "283.15" } } },
    };

    private static readonly ForecastRunInput Input = new() { Product = MosmixProduct.MosmixS, MosmixId = "10637" };

    private static MosmixMappingResult Mapping(string[]? absent = null, string[]? notExported = null) => new()
    {
        Input = Input,
        AbsentElements = absent ?? Array.Empty<string>(),
        NotExportedElements = notExported ?? Array.Empty<string>()
    };

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
    public async Task ParseAsync_RetrievedAtSampledAfterRead()
    {
        var doc = Doc();
        ArrangeSource(doc, during: _ => { _clock.Advance(TimeSpan.FromSeconds(5)); return Task.CompletedTask; });
        _mapper.Setup(m => m.Map(doc, MosmixProduct.MosmixS, new DateTime(2026, 10, 26, 16, 16, 35, DateTimeKind.Utc), false)).Returns(Mapping());
        ArrangeBuild();

        var result = await _subject.ParseAsync("10637", _caller.Token);

        result.RunAge.Should().Be(new TimeSpan(1, 16, 35));
        _clock.GetUtcNowCalls.Should().Be(1);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_AgeAtLimit_Fresh()
    {
        _subject = Create(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = new TimeSpan(1, 16, 30) });

        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, false, Mapping());
        ArrangeBuild();

        var result = await _subject.ParseAsync("10637", _caller.Token);

        result.Freshness.Should().Be(MosmixRunFreshness.Fresh);
        result.RunAge.Should().Be(new TimeSpan(1, 16, 30));
        _logger.Count("MOSMIX-RUN-STALE").Should().Be(0);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_AgeOneSecondAbove_StaleWithMarkerAndNotice()
    {
        _subject = Create(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = new TimeSpan(1, 16, 29) });

        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, true, Mapping());
        ArrangeBuild();

        var result = await _subject.ParseAsync("10637", _caller.Token);

        result.Freshness.Should().Be(MosmixRunFreshness.Stale);
        _logger.Count("MOSMIX-RUN-STALE").Should().Be(1);
        var entry = _logger.Entries.Single(e => e.EventName == "MOSMIX-RUN-STALE");
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain("01:16:30").And.Contain("01:16:29").And.Contain("MOSMIX_S").And.Contain(Endpoint);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_IssueTimeAfterRetrieval_FreshWithNegativeAge()
    {
        var doc = Doc(new DateTime(2026, 10, 26, 16, 20, 0, DateTimeKind.Utc));
        ArrangeSource(doc);
        ArrangeMap(doc, false, Mapping());
        ArrangeBuild();

        var result = await _subject.ParseAsync("10637", _caller.Token);

        result.Freshness.Should().Be(MosmixRunFreshness.Fresh);
        result.RunAge.Should().Be(-new TimeSpan(0, 3, 30));
        _logger.Count("MOSMIX-RUN-STALE").Should().Be(0);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_FreshRun_MarkerFalse()
    {
        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, false, Mapping());
        ArrangeBuild();

        var result = await _subject.ParseAsync("10637", _caller.Token);

        result.Freshness.Should().Be(MosmixRunFreshness.Fresh);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_OmissionsReturned()
    {
        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, false, Mapping());
        var omitted = new OmittedForecastValue(new DateTime(2026, 10, 26, 16, 0, 0, DateTimeKind.Utc), QuantityKind.Temperature, OmissionReason.OutOfRange);
        ArrangeBuild(omitted);

        var result = await _subject.ParseAsync("10637", _caller.Token);

        result.OmittedValues.Should().Equal(omitted);
        result.Run.MosmixId.Should().Be("10637");
        result.Run.Product.Should().Be(MosmixProduct.MosmixS);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_NotExportedElements_ReturnedByName()
    {
        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, false, Mapping(notExported: new[] { "wwP", "R101", "N" }));
        ArrangeBuild();

        var result = await _subject.ParseAsync("10637", _caller.Token);

        result.NotExportedElements.Should().Equal("wwP", "R101", "N");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_AbsentElements_NoticesAndRunFromRest()
    {
        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, false, Mapping(absent: new[] { "PPPP", "FX1" }));
        ArrangeBuild();

        var result = await _subject.ParseAsync("10637", _caller.Token);

        result.AbsentElements.Should().Equal("PPPP", "FX1");
        _logger.Count("MOSMIX-ELEMENT-ABSENT").Should().Be(2);
        _logger.Entries.Where(e => e.EventName == "MOSMIX-ELEMENT-ABSENT").Should().Contain(e => e.Message.Contains("PPPP"));
        _logger.Entries.Where(e => e.EventName == "MOSMIX-ELEMENT-ABSENT").Should().Contain(e => e.Message.Contains("FX1"));
        result.Run.Product.Should().Be(MosmixProduct.MosmixS);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_NoUsableValue_ThrowsWithOmissions()
    {
        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, false, Mapping());
        var omitted = new OmittedForecastValue(new DateTime(2026, 10, 26, 16, 0, 0, DateTimeKind.Utc), QuantityKind.Temperature, OmissionReason.OutOfRange);
        _builder.Setup(b => b.Build(Input)).Throws(new ForecastRunRejectedException("none", new[] { omitted }));

        Func<Task> act = () => _subject.ParseAsync("10637", _caller.Token);

        var ex = await act.Should().ThrowAsync<MosmixForecastException>();
        ex.And.ErrorId.Should().Be("MOSMIX-NO-USABLE-VALUE");
        ex.And.Product.Should().Be(MosmixProduct.MosmixS);
        ex.And.OmittedValues.Should().Equal(omitted);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_NoUsableValue_ExceptionCarriesNotExportedElements()
    {
        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, false, Mapping(notExported: new[] { "wwP", "R101" }));
        _builder.Setup(b => b.Build(Input)).Throws(new ForecastRunRejectedException("none", Array.Empty<OmittedForecastValue>()));

        Func<Task> act = () => _subject.ParseAsync("10637", _caller.Token);

        var ex = await act.Should().ThrowAsync<MosmixForecastException>();
        ex.And.ErrorId.Should().Be("MOSMIX-NO-USABLE-VALUE");
        ex.And.Product.Should().Be(MosmixProduct.MosmixS);
        ex.And.NotExportedElements.Should().Equal("wwP", "R101");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_PlacemarkAbsent_ThrowsIdUnknown()
    {
        var doc = Doc();
        ArrangeSource(doc);
        _reader.Setup(r => r.ReadAllStationsAsync(It.IsAny<Stream>(), "10637", It.IsAny<CancellationToken>())).Throws(new MosmixForecastFormatException(MosmixForecastErrorIds.IdUnknown, "absent"));

        Func<Task> act = () => _subject.ParseAsync("10637", _caller.Token);

        var ex = await act.Should().ThrowAsync<MosmixForecastException>();
        ex.And.ErrorId.Should().Be("MOSMIX-ID-UNKNOWN");
        ex.And.Product.Should().Be(MosmixProduct.MosmixS);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_NoMeasuredAtOnRun()
    {
        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, false, Mapping());
        
        var run = new ForecastRun { MosmixId = "10637", Product = MosmixProduct.MosmixS, Source = "dwd-mosmix-s:10637", Location = new ForecastLocation { Name = "BEISPIELSTADT", Latitude = 50.5m, Longitude = 9.6m, AltitudeM = 250m }, IssuedAtUtc = Issued, RetrievedAtUtc = Retrieved, RunAgeExceeded = false };
        _builder.Setup(b => b.Build(Input)).Returns(new ForecastRunBuildResult(run, Array.Empty<OmittedForecastValue>()));

        var result = await _subject.ParseAsync("10637", _caller.Token);

        var json = System.Text.Json.JsonSerializer.Serialize(result.Run);
        json.Should().NotContain("measured_at");
        json.Should().Contain("\"product\":\"MOSMIX_S\"");
        json.Should().Contain("\"source\":\"dwd-mosmix-s:10637\"");
    }
}
