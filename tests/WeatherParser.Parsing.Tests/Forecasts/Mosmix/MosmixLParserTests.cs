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
public sealed class MosmixLParserTests
{
    private const string Endpoint = "https://example.org/mosmix/single_stations/";

    private MockRepository _mocks = null!;
    private Mock<IMosmixLForecastSource> _source = null!;
    private Mock<IMosmixForecastDocumentReader> _reader = null!;
    private Mock<IMosmixElementMapper> _mapper = null!;
    private Mock<IForecastRunBuilder> _builder = null!;
    private ManualTimeProvider _clock = null!;
    private CapturingLogger<MosmixLParser> _logger = null!;
    private CancellationTokenSource _caller = null!;
    private MosmixLParser _subject = null!;

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

    private MosmixLParser Create(MosmixLForecastOptions options) => new(options, _source.Object, _reader.Object, _mapper.Object, _builder.Object, _clock, _logger);
    private static MosmixLForecastOptions Options() => new() { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = TimeSpan.FromHours(8) };
    private static DateTime Retrieved => new(2026, 10, 26, 16, 16, 30, DateTimeKind.Utc);
    private static DateTime Issued => new(2026, 10, 26, 15, 0, 0, DateTimeKind.Utc);
    private static MosmixForecastDocument Doc(DateTime? issued = null) => new()
    {
        MosmixId = "10637", IssuedAtUtc = issued ?? Issued, Timeline = new[] { new DateTime(2026, 10, 26, 16, 0, 0, DateTimeKind.Utc) },
        LocationName = "BEISPIELSTADT", Latitude = 50.5m, Longitude = 9.6m, AltitudeM = 250m,
        Series = new[] { new MosmixForecastSeries { ElementName = "TTT", Values = new string?[] { "283.15" } } },
    };
    private static readonly ForecastRunInput Input = new() { Product = MosmixProduct.MosmixL, MosmixId = "10637" };
    private static MosmixMappingResult Mapping(string[]? absent = null, string[]? notExported = null) => new() { Input = Input, AbsentElements = absent ?? Array.Empty<string>(), NotExportedElements = notExported ?? Array.Empty<string>() };
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
    public async Task ParseAsync_NullId_ThrowsArgumentNullException()
    {
        Func<Task> act = () => _subject.ParseAsync(null!, _caller.Token);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [DataRow("")]
    [DataRow(" ")]
    [DataRow("106 37")]
    [DataRow("10:37")]
    [DataRow("106377")]
    [DataRow("abcd")]
    [DataRow("1063a")]
    [DataRow("1O63\u00e4")]
    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_InvalidId_ThrowsBeforeRequestAndTimer(string id)
    {
        var act = () => _subject.ParseAsync(id, _caller.Token);
        var exception = (await act.Should().ThrowAsync<MosmixForecastException>()).Which;
        exception.ErrorId.Should().Be("MOSMIX-ID-INVALID");
        exception.Product.Should().Be(MosmixProduct.MosmixL);

        _clock.CreatedTimers.Should().Be(0);
        _logger.Count("MOSMIX-ID-INVALID").Should().Be(1);

        if (id.Length >= 3)
        {
            var logEntry = _logger.Entries.Single();
            logEntry.Message.Should().NotContain(id);
        }
    }

    [DataRow("01001")]
    [DataRow("X123A")]
    [DataRow("ZZZZ")]
    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_ValidIdsKeptExactly(string id)
    {
        var capturedId = "";
        _source.Setup(s => s.RetrieveAsync(id, It.IsAny<Func<Stream, CancellationToken, Task<MosmixForecastDocument>>>(), It.IsAny<CancellationToken>(), _caller.Token))
            .Returns((string mosmixId, Func<Stream, CancellationToken, Task<MosmixForecastDocument>> _, CancellationToken _, CancellationToken _) =>
            {
                capturedId = mosmixId;
                return Task.FromException<MosmixForecastDocument>(new MosmixForecastException(MosmixForecastErrorIds.IdUnknown, MosmixProduct.MosmixL, "m"));
            });

        Func<Task> act = () => _subject.ParseAsync(id, _caller.Token);
        await act.Should().ThrowAsync<MosmixForecastException>();
        capturedId.Should().Be(id);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineFromOptionsAndTimeProvider()
    {
        _subject = Create(new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(45), MaxRunAge = TimeSpan.FromHours(8) });

        var deadlineIsCancellationRequested = new List<bool>();
        ArrangeSourceThrows((deadline) =>
        {
            _clock.Advance(TimeSpan.FromSeconds(44));
            deadlineIsCancellationRequested.Add(deadline.IsCancellationRequested);
            _clock.Advance(TimeSpan.FromSeconds(1));
            deadlineIsCancellationRequested.Add(deadline.IsCancellationRequested);
            return new MosmixForecastException(MosmixForecastErrorIds.DeadlineExceeded, MosmixProduct.MosmixL, "d");
        });

        Func<Task> act = () => _subject.ParseAsync("10637", _caller.Token);
        var exception = (await act.Should().ThrowAsync<MosmixForecastException>()).Which;
        exception.ErrorId.Should().Be("MOSMIX-DEADLINE-EXCEEDED");
        exception.Product.Should().Be(MosmixProduct.MosmixL);

        deadlineIsCancellationRequested.Should().Equal(false, true);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_CallerCancelledDuringRetrieval_ThrowsOperationCanceled()
    {
        ArrangeSourceThrows((deadline) =>
        {
            _caller.Cancel();
            return new OperationCanceledException(_caller.Token);
        });

        Func<Task> act = () => _subject.ParseAsync("10637", _caller.Token);
        await act.Should().ThrowAsync<OperationCanceledException>()
            .Where(e => e.CancellationToken == _caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineDuringRetrieval_ThrowsDeadlineExceeded()
    {
        ArrangeSourceThrows((deadline) =>
        {
            _clock.Advance(TimeSpan.FromSeconds(60));
            return new MosmixForecastException(MosmixForecastErrorIds.RetrievalFailed, MosmixProduct.MosmixL, "r");
        });

        Func<Task> act = () => _subject.ParseAsync("10637", _caller.Token);
        var exception = (await act.Should().ThrowAsync<MosmixForecastException>()).Which;
        exception.ErrorId.Should().Be("MOSMIX-DEADLINE-EXCEEDED");
        exception.Product.Should().Be(MosmixProduct.MosmixL);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_BothAtOnce_CallerCancellationWins()
    {
        ArrangeSourceThrows((deadline) =>
        {
            _clock.Advance(TimeSpan.FromSeconds(60));
            _caller.Cancel();
            return new MosmixForecastException(MosmixForecastErrorIds.RetrievalFailed, MosmixProduct.MosmixL, "r");
        });

        Func<Task> act = () => _subject.ParseAsync("10637", _caller.Token);
        await act.Should().ThrowAsync<OperationCanceledException>()
            .Where(e => e.CancellationToken == _caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_CallerCancelled_SourceCancelledWithDeadlineToken_RethrownWithCallerToken()
    {
        ArrangeSourceThrows((deadline) =>
        {
            _caller.Cancel();
            return new OperationCanceledException(deadline);
        });

        Func<Task> act = () => _subject.ParseAsync("10637", _caller.Token);
        (await act.Should().ThrowAsync<OperationCanceledException>()).Which.CancellationToken.Should().Be(_caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineAfterRead_BeforeMapping_ThrowsDeadlineExceeded()
    {
        var doc = Doc();
        ArrangeSource(doc, _ => { _clock.Advance(TimeSpan.FromSeconds(60)); return Task.CompletedTask; });
        // No mapper setup

        Func<Task> act = () => _subject.ParseAsync("10637", _caller.Token);
        var exception = (await act.Should().ThrowAsync<MosmixForecastException>()).Which;
        exception.ErrorId.Should().Be("MOSMIX-DEADLINE-EXCEEDED");
        exception.Product.Should().Be(MosmixProduct.MosmixL);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineDuringBuild_WinsOverBuilderRejection()
    {
        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, false, Mapping());

        _builder.Setup(b => b.Build(Input))
            .Callback(() => _clock.Advance(TimeSpan.FromSeconds(60)))
            .Throws(new ForecastRunRejectedException("no value", Array.Empty<OmittedForecastValue>()));

        Func<Task> act = () => _subject.ParseAsync("10637", _caller.Token);
        var exception = (await act.Should().ThrowAsync<MosmixForecastException>()).Which;
        exception.ErrorId.Should().Be("MOSMIX-DEADLINE-EXCEEDED");
        exception.Product.Should().Be(MosmixProduct.MosmixL);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_CancellationAndDeadlineDuringBuild_CancellationWins()
    {
        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, false, Mapping());

        _builder.Setup(b => b.Build(Input))
            .Callback(() =>
            {
                _clock.Advance(TimeSpan.FromSeconds(60));
                _caller.Cancel();
            })
            .Throws(new ForecastRunRejectedException("no value", Array.Empty<OmittedForecastValue>()));

        Func<Task> act = () => _subject.ParseAsync("10637", _caller.Token);
        await act.Should().ThrowAsync<OperationCanceledException>()
            .Where(e => e.CancellationToken == _caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineTimerDisposed_Success()
    {
        var doc = Doc();
        ArrangeSource(doc);
        ArrangeMap(doc, false, Mapping());
        ArrangeBuild();

        await _subject.ParseAsync("10637", _caller.Token);

        _clock.ActiveTimers.Should().Be(0);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineTimerDisposed_Failure()
    {
        ArrangeSourceThrows(_ => new MosmixForecastException(MosmixForecastErrorIds.RetrievalFailed, MosmixProduct.MosmixL, "r"));

        Func<Task> act = () => _subject.ParseAsync("10637", _caller.Token);
        await act.Should().ThrowAsync<MosmixForecastException>();

        _clock.ActiveTimers.Should().Be(0);
    }

    [DataRow("MOSMIX-ID-UNKNOWN")]
    [DataRow("MOSMIX-RETRIEVAL-FAILED")]
    [DataRow("MOSMIX-TOO-LARGE")]
    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_RetrievalFailures_Propagate(string errorId)
    {
        var failure = new MosmixForecastException(errorId, MosmixProduct.MosmixL, "f");
        ArrangeSourceThrows(_ => failure);

        Func<Task> act = () => _subject.ParseAsync("10637", _caller.Token);
        (await act.Should().ThrowAsync<MosmixForecastException>()).Which.Should().BeSameAs(failure);
    }

    [DataRow("MOSMIX-FORMAT-INVALID")]
    [DataRow("MOSMIX-ISSUE-TIME-MISSING")]
    [DataRow("MOSMIX-LOCATION-MISMATCH")]
    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_FormatFailures_Propagate(string errorId)
    {
        _source.Setup(s => s.RetrieveAsync("10637", It.IsAny<Func<Stream, CancellationToken, Task<MosmixForecastDocument>>>(), It.IsAny<CancellationToken>(), _caller.Token))
            .Returns(async (string _, Func<Stream, CancellationToken, Task<MosmixForecastDocument>> read, CancellationToken deadline, CancellationToken _) =>
            {
                var doc = await read(new MemoryStream(), deadline);
                return doc;
            });
        _reader.Setup(r => r.ReadSingleLocationAsync(It.IsAny<Stream>(), "10637", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MosmixForecastFormatException(errorId, "bad file"));

        Func<Task> act = () => _subject.ParseAsync("10637", _caller.Token);
        var exception = (await act.Should().ThrowAsync<MosmixForecastException>()).Which;
        exception.ErrorId.Should().Be(errorId);
        exception.Product.Should().Be(MosmixProduct.MosmixL);
    }

    [DataRow("deadlineZero")]
    [DataRow("deadlineAboveHour")]
    [DataRow("maxRunAgeUnset")]
    [DataRow("maxRunAgeZero")]
    [DataRow("maxRunAgeAbove240h")]
    [TestMethod]
    [Timeout(10000)]
    public void Construct_InvalidOptions_ThrowsOptionsInvalid(string scenario)
    {
        var options = new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = TimeSpan.FromHours(8) };
        switch (scenario)
        {
            case "deadlineZero":
                options.ParseDeadline = TimeSpan.Zero;
                break;
            case "deadlineAboveHour":
                options.ParseDeadline = TimeSpan.FromHours(1).Add(TimeSpan.FromTicks(1));
                break;
            case "maxRunAgeUnset":
                options.MaxRunAge = null;
                break;
            case "maxRunAgeZero":
                options.MaxRunAge = TimeSpan.Zero;
                break;
            case "maxRunAgeAbove240h":
                options.MaxRunAge = TimeSpan.FromHours(240).Add(TimeSpan.FromTicks(1));
                break;
        }

        Action act = () => Create(options);
        var exception = act.Should().Throw<MosmixForecastException>().Which;
        exception.ErrorId.Should().Be("MOSMIX-OPTIONS-INVALID");
        exception.Product.Should().Be(MosmixProduct.MosmixL);
    }

    [TestMethod]
    [Timeout(10000)]
    public void Construct_NullArguments_Throw()
    {
        Action act = () => new MosmixLParser(null!, _source.Object, _reader.Object, _mapper.Object, _builder.Object, _clock, _logger);
        act.Should().Throw<ArgumentNullException>();

        act = () => new MosmixLParser(Options(), null!, _reader.Object, _mapper.Object, _builder.Object, _clock, _logger);
        act.Should().Throw<ArgumentNullException>();

        act = () => new MosmixLParser(Options(), _source.Object, null!, _mapper.Object, _builder.Object, _clock, _logger);
        act.Should().Throw<ArgumentNullException>();

        act = () => new MosmixLParser(Options(), _source.Object, _reader.Object, null!, _builder.Object, _clock, _logger);
        act.Should().Throw<ArgumentNullException>();

        act = () => new MosmixLParser(Options(), _source.Object, _reader.Object, _mapper.Object, null!, _clock, _logger);
        act.Should().Throw<ArgumentNullException>();

        act = () => new MosmixLParser(Options(), _source.Object, _reader.Object, _mapper.Object, _builder.Object, null!, _logger);
        act.Should().Throw<ArgumentNullException>();

        act = () => new MosmixLParser(Options(), _source.Object, _reader.Object, _mapper.Object, _builder.Object, _clock, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ParseAsync_DeadlineExpired_SourceCancelledWithDeadlineToken_ThrowsDeadlineExceeded()
    {
        ArrangeSourceThrows(deadline => { _clock.Advance(TimeSpan.FromSeconds(60)); return new OperationCanceledException(deadline); });

        Func<Task> act = () => _subject.ParseAsync("10637", _caller.Token);
        var exception = (await act.Should().ThrowAsync<MosmixForecastException>()).Which;
        exception.ErrorId.Should().Be("MOSMIX-DEADLINE-EXCEEDED");
        exception.Product.Should().Be(MosmixProduct.MosmixL);
    }
}
