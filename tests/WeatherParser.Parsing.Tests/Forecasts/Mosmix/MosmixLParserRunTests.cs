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
public sealed class MosmixLParserRunTests
{
    private MockRepository _mocks = null!;
    private Mock<IMosmixLForecastSource> _source = null!;
    private Mock<IMosmixForecastDocumentReader> _reader = null!;
    private Mock<IMosmixElementMapper> _mapper = null!;
    private Mock<IForecastRunBuilder> _builder = null!;
    private ManualTimeProvider _clock = null!;
    private CapturingLogger<MosmixLParser> _logger = null!;
    private CancellationTokenSource _caller = null!;
    private MosmixLParser _subject = null!;

    private const string Endpoint = "https://example.org/mosmix/single_stations/";

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _source = _mocks.Create<IMosmixLForecastSource>();
        _reader = _mocks.Create<IMosmixForecastDocumentReader>();
        _mapper = _mocks.Create<IMosmixElementMapper>();
        _builder = _mocks.Create<IForecastRunBuilder>();
        _clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 26, 16, 16, 30, 250, TimeSpan.Zero));
        _logger = new();
        _caller = new();
        _source.SetupGet(s => s.SanitizedEndpoint).Returns(Endpoint);
        _subject = Create(Options());
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _caller.Dispose();
        _mocks.VerifyAll();
    }

    private MosmixLParser Create(MosmixLForecastOptions options) => new(options, _source.Object, _reader.Object, new MosmixParsePipeline(_mapper.Object, _builder.Object, _clock), _logger);

    private static MosmixLForecastOptions Options() => new() { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = TimeSpan.FromHours(8) };

    private static DateTime Retrieved => new(2026, 10, 26, 16, 16, 30, DateTimeKind.Utc);           // the clock truncated to whole seconds

    private static DateTime Issued => new(2026, 10, 26, 15, 0, 0, DateTimeKind.Utc);

    private static MosmixForecastDocument Doc(DateTime? issued = null) => new()
    {
        MosmixId = "10637", IssuedAtUtc = issued ?? Issued, Timeline = new[] { new DateTime(2026, 10, 26, 16, 0, 0, DateTimeKind.Utc) },
        LocationName = "BEISPIELSTADT", Latitude = 50.5m, Longitude = 9.6m, AltitudeM = 250m,
        Series = new[] { new MosmixForecastSeries { ElementName = "TTT", Values = new string?[] { "283.15" } } },
    };

    private static readonly ForecastRunInput Input = new() { Product = MosmixProduct.MosmixL, MosmixId = "10637" };

    private static MosmixMappingResult Mapping(string[]? absent = null, string[]? notExported = null) => new()
    {
        Input = Input,
        AbsentElements = absent ?? Array.Empty<string>(),
        NotExportedElements = notExported ?? Array.Empty<string>()
    };

    private static ForecastRun Run() => new() { MosmixId = "10637", Product = MosmixProduct.MosmixL };

    // The source runs the parser's reader callback, optionally doing something with the deadline token first.
    private void ArrangeSource(MosmixForecastDocument document, Func<CancellationToken, Task>? during = null)
    {
        _source.Setup(s => s.RetrieveAsync("10637", It.IsAny<Func<Stream, CancellationToken, Task<MosmixForecastDocument>>>(), It.IsAny<CancellationToken>(), _caller.Token))
            .Returns(async (string _, Func<Stream, CancellationToken, Task<MosmixForecastDocument>> read, CancellationToken deadline, CancellationToken _) =>
            {
                if (during is not null) { await during(deadline); }
                return await read(new MemoryStream(), deadline);
            });
        _reader.Setup(r => r.ReadSingleLocationAsync(It.IsAny<Stream>(), "10637", It.IsAny<CancellationToken>())).ReturnsAsync(document);
    }

    private void ArrangeSourceThrows(Func<CancellationToken, Exception> failure) =>
        _source.Setup(s => s.RetrieveAsync("10637", It.IsAny<Func<Stream, CancellationToken, Task<MosmixForecastDocument>>>(), It.IsAny<CancellationToken>(), _caller.Token))
            .Returns((string _, Func<Stream, CancellationToken, Task<MosmixForecastDocument>> _, CancellationToken deadline, CancellationToken _) => Task.FromException<MosmixForecastDocument>(failure(deadline)));

    private void ArrangeMap(MosmixForecastDocument document, bool stale, MosmixMappingResult mapping) =>
        _mapper.Setup(m => m.Map(document, MosmixProduct.MosmixL, Retrieved, stale)).Returns(mapping);

    private void ArrangeBuild(params OmittedForecastValue[] omitted) =>
        _builder.Setup(b => b.Build(Input)).Returns(new ForecastRunBuildResult(Run(), omitted));

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_RetrievedAtSampledAfterRead()
    {
        var doc = Doc();
        ArrangeSource(doc, during: _ => { _clock.Advance(TimeSpan.FromSeconds(5)); return Task.CompletedTask; });
        _mapper.Setup(m => m.Map(doc, MosmixProduct.MosmixL, new DateTime(2026, 10, 26, 16, 16, 35, DateTimeKind.Utc), false)).Returns(Mapping());
        ArrangeBuild();

        var result = await _subject.ParseAsync("10637", _caller.Token);

        result.RunAge.Should().Be(new TimeSpan(1, 16, 35));
        _clock.GetUtcNowCalls.Should().Be(1);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_AgeAtLimit_Fresh()
    {
        _subject = Create(new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = new TimeSpan(1, 16, 30) });

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
        _subject = Create(new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = new TimeSpan(1, 16, 29) });

        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, true, Mapping());
        ArrangeBuild();

        var result = await _subject.ParseAsync("10637", _caller.Token);

        result.Freshness.Should().Be(MosmixRunFreshness.Stale);
        _logger.Count("MOSMIX-RUN-STALE").Should().Be(1);
        var entry = _logger.Entries.Single(e => e.EventName == "MOSMIX-RUN-STALE");
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Message.Should().Contain("01:16:30").And.Contain("01:16:29");
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
        result.Run.Product.Should().Be(MosmixProduct.MosmixL);
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
        ex.And.Product.Should().Be(MosmixProduct.MosmixL);
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
        ex.And.Product.Should().Be(MosmixProduct.MosmixL);
        ex.And.NotExportedElements.Should().Equal("wwP", "R101");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_FormatFailure_MappedWithProduct()
    {
        var doc = Doc();
        ArrangeSource(doc);
        _reader.Setup(r => r.ReadSingleLocationAsync(It.IsAny<Stream>(), "10637", It.IsAny<CancellationToken>())).Throws(new MosmixForecastFormatException("MOSMIX-FORMAT-INVALID", "bad file"));

        Func<Task> act = () => _subject.ParseAsync("10637", _caller.Token);

        var ex = await act.Should().ThrowAsync<MosmixForecastException>();
        ex.And.ErrorId.Should().Be("MOSMIX-FORMAT-INVALID");
        ex.And.Product.Should().Be(MosmixProduct.MosmixL);
    }

    [TestMethod]
    public void MosmixParseResult_CopiesListsAndRejectsNull()
    {
        var run = new ForecastRun();
        var omitted = new List<OmittedForecastValue> { new(new DateTime(2026, 10, 26, 16, 0, 0, DateTimeKind.Utc), QuantityKind.Temperature, OmissionReason.OutOfRange) };
        var notExported = new List<string> { "wwP" };
        var absent = new List<string> { "PPPP" };

        var result = new MosmixParseResult(run, omitted, notExported, absent, MosmixRunFreshness.Stale, TimeSpan.FromHours(9));

        result.OmittedValues.Should().Equal(omitted);
        result.NotExportedElements.Should().Equal(notExported);
        result.AbsentElements.Should().Equal(absent);
        result.Freshness.Should().Be(MosmixRunFreshness.Stale);
        result.RunAge.Should().Be(TimeSpan.FromHours(9));

        // Clear the lists
        omitted.Clear();
        notExported.Clear();
        absent.Clear();

        // The result should still hold the original values
        result.OmittedValues.Should().Equal(new OmittedForecastValue(new DateTime(2026, 10, 26, 16, 0, 0, DateTimeKind.Utc), QuantityKind.Temperature, OmissionReason.OutOfRange));
        result.NotExportedElements.Should().Equal("wwP");
        result.AbsentElements.Should().Equal("PPPP");

        // Test null arguments
        Action nullRun = () => new MosmixParseResult(null!, omitted, notExported, absent, MosmixRunFreshness.Stale, TimeSpan.FromHours(9));
        nullRun.Should().Throw<ArgumentNullException>();

        Action nullOmitted = () => new MosmixParseResult(run, null!, notExported, absent, MosmixRunFreshness.Stale, TimeSpan.FromHours(9));
        nullOmitted.Should().Throw<ArgumentNullException>();

        Action nullNotExported = () => new MosmixParseResult(run, omitted, null!, absent, MosmixRunFreshness.Stale, TimeSpan.FromHours(9));
        nullNotExported.Should().Throw<ArgumentNullException>();

        Action nullAbsent = () => new MosmixParseResult(run, omitted, notExported, null!, MosmixRunFreshness.Stale, TimeSpan.FromHours(9));
        nullAbsent.Should().Throw<ArgumentNullException>();
    }
}
