using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Moq.Protected;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class CatalogDownloaderFailureTests
{
    private MockRepository _mocks = null!;
    private Mock<HttpMessageHandler> _handler = null!;
    private HttpClient _http = null!;
    private ManualTimeProvider _time = null!;
    private ICatalogDownloader _subject = null!;

    private static readonly Uri SecretUri = new("https://user:secret@example.org/cdc/list.txt?token=hidden");
    private const string Sanitized = "https://example.org/cdc/list.txt";

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _handler = _mocks.Create<HttpMessageHandler>();
        _http = new HttpClient(_handler.Object, disposeHandler: false);
        _time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 23, 4, 0, 0, TimeSpan.Zero));
        _subject = new CatalogDownloader(_http, _time);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
        _http.Dispose();
    }

    private static CatalogRequest Request(CancellationToken deadline = default, CatalogDeadlineKind kind = CatalogDeadlineKind.Selection, long maxBytes = 100) => new("AirTemperatureStationList", SecretUri, maxBytes, false, deadline, kind);

    private void Respond(Func<CancellationToken, Task<HttpResponseMessage>> respond) => _handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).Returns((HttpRequestMessage _, CancellationToken token) => respond(token));

    private static async Task<HttpResponseMessage> Stall(CancellationToken token) { await Task.Delay(Timeout.Infinite, token); throw new InvalidOperationException("unreachable"); }

    private static async Task<byte[]> ReadAll(Stream stream, CancellationToken token) { using var copy = new MemoryStream(); await stream.CopyToAsync(copy, token); return copy.ToArray(); }

    private static async Task<CatalogRetrievalException> Retrieval(Func<Task> act, string errorId)
    {
        var ex = await FluentActions.Awaiting(act).Should().ThrowAsync<CatalogRetrievalException>();
        ex.Which.ErrorId.Should().Be(errorId);
        ex.Which.EndpointName.Should().Be("AirTemperatureStationList");
        ex.Which.InnerException.Should().BeNull();
        ex.Which.Message.Should().Contain("AirTemperatureStationList");
        ex.Which.Message.Should().Contain(Sanitized);
        ex.Which.Message.Should().NotContain("secret");
        ex.Which.Message.Should().NotContain("hidden");
        return ex.Which;
    }

    private sealed class StallingStream : Stream
    {
        public bool Disposed { get; private set; }

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

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class FailingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            throw new IOException("connection reset for https://user:secret@example.org/cdc/list.txt?token=hidden");

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [TestMethod]
    [Timeout(10000)]
    [DataRow(404)]
    [DataRow(503)]
    public async Task DownloadAsync_NonSuccessStatus_ThrowsWithEndpointNameAndStatus(int status)
    {
        Respond(_ => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)));
        (await Retrieval(() => _subject.DownloadAsync(Request(), ReadAll, CancellationToken.None), "CATALOG-RETRIEVAL-FAILED")).Message.Should().Contain(status.ToString());
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_ConnectionFailure_ThrowsRetrievalException()
    {
        Respond(_ => throw new HttpRequestException("No connection to https://user:secret@example.org/cdc/list.txt?token=hidden"));
        (await Retrieval(() => _subject.DownloadAsync(Request(), ReadAll, CancellationToken.None), "CATALOG-RETRIEVAL-FAILED")).Message.Should().Contain("HttpRequestException");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_TruncatedBody_FollowsFailureSemantics()
    {
        Respond(_ => Task.FromResult(HttpTestContent.Ok(HttpTestContent.WithLength(HttpTestContent.Bytes(50), 80))));
        await FluentActions.Awaiting(() => _subject.DownloadAsync(Request(), ReadAll, CancellationToken.None))
            .Should().ThrowAsync<CatalogRetrievalException>()
            .Where(ex => ex.ErrorId == "CATALOG-RETRIEVAL-FAILED");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_BodyReadFailure_IsRetrievalFailureWithTypeNameOnly()
    {
        var content = new StreamContent(new FailingStream());
        Respond(_ => Task.FromResult(HttpTestContent.Ok(content)));
        (await Retrieval(() => _subject.DownloadAsync(Request(), ReadAll, CancellationToken.None), "CATALOG-RETRIEVAL-FAILED")).Message.Should().Contain("IOException");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_HeadersStall_TimesOut()
    {
        Respond(Stall);
        var task = _subject.DownloadAsync(Request(), ReadAll, CancellationToken.None);
        _time.Advance(TimeSpan.FromSeconds(29));
        task.IsCompleted.Should().BeFalse();
        _time.Advance(TimeSpan.FromSeconds(1));
        await Retrieval(() => task, "CATALOG-TIMEOUT");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_BodyStallsAfterHeaders_TimesOut()
    {
        var stalling = new StallingStream();
        Respond(_ => Task.FromResult(HttpTestContent.Ok(new StreamContent(stalling))));
        var task = _subject.DownloadAsync(Request(), ReadAll, CancellationToken.None);
        _time.Advance(TimeSpan.FromSeconds(30));
        await Retrieval(() => task, "CATALOG-TIMEOUT");
        stalling.Disposed.Should().BeTrue();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_CallerCancellation_ThrowsOperationCanceledWithCallerToken()
    {
        using var caller = new CancellationTokenSource();
        Respond(Stall);
        var task = _subject.DownloadAsync(Request(), ReadAll, caller.Token);
        caller.Cancel();
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_BodyStallsAfterHeaders_CallerCancellation_ThrowsWithCallerToken()
    {
        var stalling = new StallingStream();
        Respond(_ => Task.FromResult(HttpTestContent.Ok(new StreamContent(stalling))));
        using var caller = new CancellationTokenSource();
        var task = _subject.DownloadAsync(Request(), ReadAll, caller.Token);
        caller.Cancel();
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task SelectionDeadlineExpiry_ThrowsSelectionLimitException()
    {
        using var deadline = new CancellationTokenSource();
        Respond(Stall);
        var task = _subject.DownloadAsync(Request(deadline.Token, CatalogDeadlineKind.Selection), ReadAll, CancellationToken.None);
        deadline.Cancel();
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<SelectionLimitException>()
            .Where(ex => ex.ErrorId == "SELECTION-DEADLINE-EXCEEDED");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RefreshDeadlineExpiry_ThrowsEvidenceCacheException()
    {
        using var deadline = new CancellationTokenSource();
        Respond(Stall);
        var task = _subject.DownloadAsync(Request(deadline.Token, CatalogDeadlineKind.Refresh), ReadAll, CancellationToken.None);
        deadline.Cancel();
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<EvidenceCacheException>()
            .Where(ex => ex.ErrorId == "MOSMIX-S-REFRESH-DEADLINE-EXCEEDED");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ConsumerHttpClientTimeoutFirst_ThrowsCatalogTimeout()
    {
        var shortHandler = _mocks.Create<HttpMessageHandler>();
        shortHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns((HttpRequestMessage _, CancellationToken token) => Stall(token));
        using var shortClient = new HttpClient(shortHandler.Object, disposeHandler: false) { Timeout = TimeSpan.FromMilliseconds(50) };
        var subject = new CatalogDownloader(shortClient, _time);
        await Retrieval(() => subject.DownloadAsync(Request(), ReadAll, CancellationToken.None), "CATALOG-TIMEOUT");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task CallerCancellationAndSelectionDeadline_ThrowsOperationCanceledWithCallerToken()
    {
        using var caller = new CancellationTokenSource();
        using var deadline = new CancellationTokenSource();
        Respond(token => { deadline.Cancel(); caller.Cancel(); return Stall(token); });
        var task = _subject.DownloadAsync(Request(deadline.Token, CatalogDeadlineKind.Selection), ReadAll, caller.Token);
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task CallerCancellationAndRefreshDeadline_ThrowsOperationCanceledWithCallerToken()
    {
        using var caller = new CancellationTokenSource();
        using var deadline = new CancellationTokenSource();
        Respond(token => { deadline.Cancel(); caller.Cancel(); return Stall(token); });
        var task = _subject.DownloadAsync(Request(deadline.Token, CatalogDeadlineKind.Refresh), ReadAll, caller.Token);
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task CallerCancellationAndRequestTimeout_ThrowsOperationCanceledWithCallerToken()
    {
        using var caller = new CancellationTokenSource();
        Respond(token => { _time.Advance(TimeSpan.FromSeconds(30)); caller.Cancel(); return Stall(token); });
        var task = _subject.DownloadAsync(Request(), ReadAll, caller.Token);
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task SelectionAndTimeout_ThrowsSelectionLimitException()
    {
        using var deadline = new CancellationTokenSource();
        Respond(token => { _time.Advance(TimeSpan.FromSeconds(30)); deadline.Cancel(); return Stall(token); });
        var task = _subject.DownloadAsync(Request(deadline.Token, CatalogDeadlineKind.Selection), ReadAll, CancellationToken.None);
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<SelectionLimitException>()
            .Where(ex => ex.ErrorId == "SELECTION-DEADLINE-EXCEEDED");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RefreshDeadlineAndRequestTimeout_RefreshDeadlineWins()
    {
        using var deadline = new CancellationTokenSource();
        Respond(token => { _time.Advance(TimeSpan.FromSeconds(30)); deadline.Cancel(); return Stall(token); });
        var task = _subject.DownloadAsync(Request(deadline.Token, CatalogDeadlineKind.Refresh), ReadAll, CancellationToken.None);
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<EvidenceCacheException>()
            .Where(ex => ex.ErrorId == "MOSMIX-S-REFRESH-DEADLINE-EXCEEDED");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_NoRetry_SingleRequestOnFailure()
    {
        Respond(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        await FluentActions.Awaiting(() => _subject.DownloadAsync(Request(), ReadAll, CancellationToken.None))
            .Should().ThrowAsync<CatalogRetrievalException>();
        _handler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_ReaderFailureCancellationOrSizeLimit_DisposesResponseAndStream()
    {
        // Part (a)
        var tracking = new HttpTestContent.TrackingStream(HttpTestContent.Bytes(10));
        Respond(_ => Task.FromResult(HttpTestContent.Ok(new StreamContent(tracking))));
        await FluentActions.Awaiting(() => _subject.DownloadAsync<byte[]>(Request(), (_, _) => throw new InvalidOperationException("reader failed"), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .Where(ex => ex.Message == "reader failed");
        tracking.Disposed.Should().BeTrue();

        // Part (b)
        tracking = new HttpTestContent.TrackingStream(HttpTestContent.Bytes(200));
        Respond(_ => Task.FromResult(HttpTestContent.Ok(new StreamContent(tracking))));
        await FluentActions.Awaiting(() => _subject.DownloadAsync(Request(maxBytes: 100), ReadAll, CancellationToken.None))
            .Should().ThrowAsync<CatalogRetrievalException>()
            .Where(ex => ex.ErrorId == "CATALOG-TOO-LARGE");
        tracking.Disposed.Should().BeTrue();

        // Part (c)
        var stalling = new StallingStream();
        Respond(_ => Task.FromResult(HttpTestContent.Ok(new StreamContent(stalling))));
        using var caller = new CancellationTokenSource();
        var task = _subject.DownloadAsync(Request(), ReadAll, caller.Token);
        caller.Cancel();
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == caller.Token);
        stalling.Disposed.Should().BeTrue();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_ReaderFormatFailure_PropagatesUnchanged()
    {
        Respond(_ => Task.FromResult(HttpTestContent.Ok(HttpTestContent.Known(HttpTestContent.Bytes(5)))));
        await FluentActions.Awaiting(() => _subject.DownloadAsync<byte[]>(Request(), (_, _) => throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "bad list"), CancellationToken.None))
            .Should().ThrowAsync<CatalogFormatException>()
            .Where(ex => ex.ErrorId == CatalogFormatException.CatalogFormatInvalidErrorId);
    }
}
