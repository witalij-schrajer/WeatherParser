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
public sealed class CatalogDownloaderHeaderCancellationTests
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

    private static CatalogRequest AbsentRequest() => new("AirTemperatureStationList", SecretUri, 100, true, CancellationToken.None, CatalogDeadlineKind.Selection);

    private void Respond(Action<CancellationToken> fire, Func<CancellationToken, Task<HttpResponseMessage>> respond)
    {
        _handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns((HttpRequestMessage _, CancellationToken token) =>
            {
                fire(token);
                return respond(token);
            });
    }

    private static async Task<byte[]> ReadAll(Stream stream, CancellationToken token) => new byte[0];

    [TestMethod]
    [Timeout(10000)]
    public async Task NotFoundAfterCallerCancellation_ThrowsOperationCanceledWithCallerToken()
    {
        using var caller = new CancellationTokenSource();
        Respond(token => caller.Cancel(), _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var task = _subject.DownloadAsync(AbsentRequest(), ReadAll, caller.Token);
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task ServiceUnavailableAfterSelectionDeadline_ThrowsSelectionLimitException()
    {
        using var deadline = new CancellationTokenSource();
        Respond(token => deadline.Cancel(), _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var task = _subject.DownloadAsync(Request(deadline.Token, CatalogDeadlineKind.Selection), ReadAll, CancellationToken.None);
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<SelectionLimitException>()
            .Where(ex => ex.ErrorId == "SELECTION-DEADLINE-EXCEEDED");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task OversizedHeadersAfterRequestTimeout_ThrowsCatalogTimeout()
    {
        Respond(token => _time.Advance(TimeSpan.FromSeconds(30)), _ => Task.FromResult(HttpTestContent.Ok(HttpTestContent.Known(HttpTestContent.Bytes(200)))));
        await FluentActions.Awaiting(() => _subject.DownloadAsync(Request(maxBytes: 100), ReadAll, CancellationToken.None))
            .Should().ThrowAsync<CatalogRetrievalException>()
            .Where(ex => ex.ErrorId == "CATALOG-TIMEOUT");
    }
}
