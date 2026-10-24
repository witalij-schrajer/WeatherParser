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
using WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class CatalogDownloaderCancellationTests
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

    private static CatalogRequest Request(CancellationToken deadline = default, CatalogDeadlineKind kind = CatalogDeadlineKind.Selection, long maxBytes = 100) => new("AirTemperatureStationList", new Uri("https://example.org/cdc/list.txt"), maxBytes, false, deadline, kind);

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

    [TestMethod]
    [Timeout(10000)]
    public async Task BodyReadFailsAfterCallerCancellation_ThrowsOperationCanceledWithCallerToken()
    {
        using var caller = new CancellationTokenSource();
        Respond(_ => Task.FromResult(HttpTestContent.Ok(new StreamContent(new CancelThenFailStream(() => caller.Cancel())))));
        var task = _subject.DownloadAsync(Request(), ReadAll, caller.Token);
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task BodyReadFailsAfterSelectionDeadline_ThrowsSelectionLimitException()
    {
        using var deadline = new CancellationTokenSource();
        Respond(_ => Task.FromResult(HttpTestContent.Ok(new StreamContent(new CancelThenFailStream(() => deadline.Cancel())))));
        var task = _subject.DownloadAsync(Request(deadline.Token, CatalogDeadlineKind.Selection), ReadAll, CancellationToken.None);
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<SelectionLimitException>()
            .Where(ex => ex.ErrorId == "SELECTION-DEADLINE-EXCEEDED");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task BodyReadFailsAfterRequestTimeout_ThrowsCatalogTimeout()
    {
        Respond(_ => Task.FromResult(HttpTestContent.Ok(new StreamContent(new CancelThenFailStream(() => _time.Advance(TimeSpan.FromSeconds(30)))))));
        var task = _subject.DownloadAsync(Request(), ReadAll, CancellationToken.None);
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<CatalogRetrievalException>()
            .Where(ex => ex.ErrorId == "CATALOG-TIMEOUT");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task SynchronousBodyReadFailsAfterCallerCancellation_ThrowsOperationCanceledWithCallerToken()
    {
        using var caller = new CancellationTokenSource();
        var stream = new CancelThenFailStream(() => caller.Cancel());
        Respond(_ => Task.FromResult(HttpTestContent.Ok(new StreamContent(stream))));
        var task = _subject.DownloadAsync(Request(), (passed, token) =>
        {
            var buffer = new byte[16];
            int read;
            while ((read = passed.Read(buffer, 0, 16)) > 0)
            {
                // Read all data
            }
            return Task.FromResult(new byte[0]);
        }, caller.Token);
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == caller.Token);
    }
}
