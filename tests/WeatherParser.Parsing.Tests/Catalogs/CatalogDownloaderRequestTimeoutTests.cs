using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class CatalogDownloaderRequestTimeoutTests
{
    private MockRepository _mocks = null!;
    private Mock<HttpMessageHandler> _handler = null!;
    private HttpClient _http = null!;
    private ManualTimeProvider _time = null!;
    private ICatalogDownloader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
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

    private static CatalogRequest Request(CancellationToken deadline = default, CatalogDeadlineKind kind = CatalogDeadlineKind.MosmixS, TimeSpan? requestTimeout = null) => new("MosmixSAllStationsUri", new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz"), 100, false, deadline, kind) { RequestTimeout = requestTimeout };

    private void Respond(Func<CancellationToken, Task<HttpResponseMessage>> respond) => _handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>()).Returns((HttpRequestMessage _, CancellationToken token) => respond(token));

    private static async Task<byte[]> ReadAll(Stream stream, CancellationToken token) { using var copy = new MemoryStream(); await stream.CopyToAsync(copy, token); return copy.ToArray(); }

    private sealed class CancelThenFailStream(Action cancel) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) { cancel(); throw new IOException("closed"); }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) { cancel(); throw new IOException("closed"); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private static async Task<HttpResponseMessage> Stall(CancellationToken token)
    {
        await Task.Delay(Timeout.Infinite, token);
        throw new InvalidOperationException("unreachable");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_NoRequestTimeout_Keeps30Seconds()
    {
        Respond(Stall);
        var task = _subject.DownloadAsync(Request(kind: CatalogDeadlineKind.Selection), ReadAll, CancellationToken.None);
        _time.Advance(TimeSpan.FromSeconds(29));
        task.IsCompleted.Should().BeFalse();
        _time.Advance(TimeSpan.FromSeconds(1));
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<CatalogRetrievalException>()
            .Where(ex => ex.ErrorId == "CATALOG-TIMEOUT");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_LongerRequestTimeout_CallbackBeyond30SecondsSucceeds()
    {
        var release = new TaskCompletionSource();
        var content = HttpTestContent.Ok(new ByteArrayContent(new byte[] { 1, 2, 3 }));
        Respond(_ => Task.FromResult(content));
        var task = _subject.DownloadAsync(Request(requestTimeout: TimeSpan.FromMinutes(5)), async (stream, token) =>
        {
            await release.Task;
            return await ReadAll(stream, token);
        }, CancellationToken.None);
        _time.Advance(TimeSpan.FromMinutes(1));
        task.IsCompleted.Should().BeFalse();
        release.SetResult();
        var result = await task;
        result.Should().Equal(1, 2, 3);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_RequestTimeoutExpires_ClassifiedTimeout()
    {
        Respond(Stall);
        var task = _subject.DownloadAsync(Request(requestTimeout: TimeSpan.FromMinutes(5)), ReadAll, CancellationToken.None);
        _time.Advance(TimeSpan.FromSeconds(299));
        task.IsCompleted.Should().BeFalse();
        _time.Advance(TimeSpan.FromSeconds(1));
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<CatalogRetrievalException>()
            .Where(ex => ex.ErrorId == "CATALOG-TIMEOUT");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_DeadlineBeforeRequestTimeout_ClassifiedDeadline()
    {
        using var deadline = new CancellationTokenSource();
        Respond(Stall);
        var task = _subject.DownloadAsync(Request(deadline.Token, requestTimeout: TimeSpan.FromMinutes(5)), ReadAll, CancellationToken.None);
        deadline.Cancel();
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-DEADLINE-EXCEEDED" && ex.Product == MosmixProduct.MosmixS);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_RequestTimeoutEqualsDeadline_ClassifiedDeadline()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(5), _time);
        Respond(Stall);
        var task = _subject.DownloadAsync(Request(deadline.Token, requestTimeout: TimeSpan.FromMinutes(5)), ReadAll, CancellationToken.None);
        _time.Advance(TimeSpan.FromMinutes(5));
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-DEADLINE-EXCEEDED" && ex.Product == MosmixProduct.MosmixS);
    }

    [DataRow(CatalogDeadlineKind.MosmixL, MosmixProduct.MosmixL)]
    [DataRow(CatalogDeadlineKind.MosmixS, MosmixProduct.MosmixS)]
    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_MosmixDeadlineKinds_MappedToProduct(CatalogDeadlineKind kind, MosmixProduct product)
    {
        using var deadline = new CancellationTokenSource();
        Respond(_ => Task.FromResult(HttpTestContent.Ok(new StreamContent(new CancelThenFailStream(() => deadline.Cancel())))));
        var task = _subject.DownloadAsync(Request(deadline.Token, kind), ReadAll, CancellationToken.None);
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-DEADLINE-EXCEEDED" && 
                        ex.Product == product && 
                        ex.Message.Contains("MosmixSAllStationsUri") && 
                        ex.Message.Contains("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz"));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_CallerCancelledAndMosmixDeadline_ThrowsOperationCanceled()
    {
        using var caller = new CancellationTokenSource();
        using var deadline = new CancellationTokenSource();
        Respond(_ => Task.FromResult(HttpTestContent.Ok(new StreamContent(new CancelThenFailStream(() => { deadline.Cancel(); caller.Cancel(); })))));
        var task = _subject.DownloadAsync(Request(deadline.Token, requestTimeout: TimeSpan.FromMinutes(5)), ReadAll, caller.Token);
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == caller.Token);
    }

    [DataRow(0)]
    [DataRow(-1)]
    [TestMethod]
    [Timeout(10000)]
    public async Task DownloadAsync_NonPositiveRequestTimeout_ThrowsArgumentOutOfRange(int seconds)
    {
        Func<Task> act = () => _subject.DownloadAsync(Request(requestTimeout: TimeSpan.FromSeconds(seconds)), ReadAll, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
}
