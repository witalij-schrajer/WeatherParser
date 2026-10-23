using System;
using System.IO;
using System.Net;
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
public sealed class CatalogDownloaderReadAfterCancellationTests
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

    private static async Task<byte[]> ReadAll(Stream stream, CancellationToken token)
    {
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy, token);
        return copy.ToArray();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ReadSucceedsAfterCallerCancellation_ThrowsOperationCanceledWithCallerToken()
    {
        using var caller = new CancellationTokenSource();
        Respond(_ => Task.FromResult(HttpTestContent.Ok(HttpTestContent.Known(HttpTestContent.Bytes(40)))));
        var task = _subject.DownloadAsync(Request(maxBytes: 100), async (passed, token) =>
        {
            caller.Cancel();
            var buffer = new byte[16];
            while (await passed.ReadAsync(buffer, 0, buffer.Length) > 0)
            {
            }
            return await Task.FromResult(new byte[0]);
        }, caller.Token);
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ReadAboveBoundAfterRequestTimeout_ThrowsCatalogTimeout()
    {
        Respond(_ => Task.FromResult(HttpTestContent.Ok(HttpTestContent.Unknown(HttpTestContent.Bytes(200)))));
        var task = _subject.DownloadAsync(Request(maxBytes: 100), async (passed, token) =>
        {
            _time.Advance(TimeSpan.FromSeconds(30));
            var buffer = new byte[16];
            while (await passed.ReadAsync(buffer, 0, buffer.Length) > 0)
            {
            }
            return await Task.FromResult(new byte[0]);
        }, CancellationToken.None);
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<CatalogRetrievalException>()
            .Where(ex => ex.ErrorId == "CATALOG-TIMEOUT");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task TruncatedReadAfterSelectionDeadline_ThrowsSelectionLimitException()
    {
        using var deadline = new CancellationTokenSource();
        Respond(_ => Task.FromResult(HttpTestContent.Ok(HttpTestContent.WithLength(HttpTestContent.Bytes(50), 80))));
        var task = _subject.DownloadAsync(Request(deadline.Token, CatalogDeadlineKind.Selection, 100), async (passed, token) =>
        {
            deadline.Cancel();
            var buffer = new byte[16];
            while (await passed.ReadAsync(buffer, 0, buffer.Length) > 0)
            {
            }
            return await Task.FromResult(new byte[0]);
        }, CancellationToken.None);
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<SelectionLimitException>()
            .Where(ex => ex.ErrorId == "SELECTION-DEADLINE-EXCEEDED");
    }
}
