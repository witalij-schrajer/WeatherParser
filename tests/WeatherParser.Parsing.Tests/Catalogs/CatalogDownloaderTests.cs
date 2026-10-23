namespace WeatherParser.Parsing.Tests.Catalogs;

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

[TestClass]
public sealed class CatalogDownloaderTests
{
    private MockRepository _mocks = null!;
    private Mock<HttpMessageHandler> _handler = null!;
    private HttpClient _http = null!;
    private ManualTimeProvider _time = null!;
    private ICatalogDownloader _subject = null!;

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
    }

    private static readonly Uri ListUri = new("https://example.org/cdc/zehn_now_tu_Beschreibung_Stationen.txt?version=2");

    private static CatalogRequest Request(long maxBytes = 100, bool notFoundIsAbsent = false) => new("AirTemperatureStationList", ListUri, maxBytes, notFoundIsAbsent, CancellationToken.None, CatalogDeadlineKind.Selection);

    private void Expect(HttpResponseMessage response)
    {
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get && r.RequestUri!.OriginalString == ListUri.OriginalString),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private static async Task<byte[]> ReadAll(Stream stream, CancellationToken cancellationToken)
    {
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy, cancellationToken);
        return copy.ToArray();
    }

    private static async Task ShouldBe(Func<Task> act, string errorId)
    {
        (await FluentActions.Awaiting(act).Should().ThrowAsync<CatalogRetrievalException>())
            .Which
            .ErrorId
            .Should()
            .Be(errorId);
    }

    [TestMethod]
    public async Task DownloadAsync_Success_ReturnsContent()
    {
        Expect(HttpTestContent.Ok(HttpTestContent.Known(HttpTestContent.Bytes(80))));
        var result = await _subject.DownloadAsync(Request(), ReadAll, CancellationToken.None);
        result.Should().Equal(HttpTestContent.Bytes(80));
    }

    [TestMethod]
    public async Task DownloadAsync_ContentLengthAboveBound_FailsBeforeReadingBody()
    {
        var called = false;
        Expect(HttpTestContent.Ok(HttpTestContent.WithLength(HttpTestContent.Bytes(101), 101)));
        await ShouldBe(
            () => _subject.DownloadAsync(Request(maxBytes: 100), async (stream, ct) =>
            {
                called = true;
                return await ReadAll(stream, ct);
            }, CancellationToken.None),
            "CATALOG-TOO-LARGE");
        called.Should().BeFalse();
    }

    [TestMethod]
    public async Task DownloadAsync_BodyAboveBoundWithoutContentLength_StopsAtBound()
    {
        Expect(HttpTestContent.Ok(HttpTestContent.Unknown(HttpTestContent.Bytes(200))));
        await ShouldBe(
            () => _subject.DownloadAsync(Request(maxBytes: 100), ReadAll, CancellationToken.None),
            "CATALOG-TOO-LARGE");
    }

    [TestMethod]
    public async Task DownloadAsync_BodyAboveBoundWithWrongContentLength_StopsAtBound()
    {
        Expect(HttpTestContent.Ok(HttpTestContent.WithLength(HttpTestContent.Bytes(200), 50)));
        await ShouldBe(
            () => _subject.DownloadAsync(Request(maxBytes: 100), ReadAll, CancellationToken.None),
            "CATALOG-TOO-LARGE");
    }

    [TestMethod]
    public async Task DownloadAsync_BodyExactlyAtBound_Succeeds()
    {
        Expect(HttpTestContent.Ok(HttpTestContent.Known(HttpTestContent.Bytes(100))));
        var result = await _subject.DownloadAsync(Request(maxBytes: 100), ReadAll, CancellationToken.None);
        result.Should().Equal(HttpTestContent.Bytes(100));

        // Second call with unknown length
        Expect(HttpTestContent.Ok(HttpTestContent.Unknown(HttpTestContent.Bytes(100))));
        var result2 = await _subject.DownloadAsync(Request(maxBytes: 100), ReadAll, CancellationToken.None);
        result2.Should().Equal(HttpTestContent.Bytes(100));
    }

    [TestMethod]
    public async Task DownloadAsync_StreamReadableDuringReader_DisposedAfterSuccess()
    {
        var tracking = new HttpTestContent.TrackingStream(HttpTestContent.Bytes(10));
        Expect(HttpTestContent.Ok(new StreamContent(tracking)));
        var result = await _subject.DownloadAsync(
            Request(),
            async (stream, ct) =>
            {
                stream.CanRead.Should().BeTrue();
                tracking.Disposed.Should().BeFalse();
                return await ReadAll(stream, ct);
            }, CancellationToken.None);
        tracking.Disposed.Should().BeTrue();
    }

    [TestMethod]
    public async Task DownloadAsync_NotFoundAsAbsent_ReturnsNullWithoutReading()
    {
        Expect(new HttpResponseMessage(HttpStatusCode.NotFound));
        var result = await _subject.DownloadAsync<byte[]>(
            Request(notFoundIsAbsent: true),
            (stream, ct) => throw new InvalidOperationException("The reader must not be called for an absent file."),
            CancellationToken.None);
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task DownloadAsync_UsesInjectedHttpClient_Unchanged()
    {
        _http.DefaultRequestHeaders.Add("X-Consumer", "configured");
        var timeout = _http.Timeout;
        Expect(HttpTestContent.Ok(HttpTestContent.Known(HttpTestContent.Bytes(5))));
        await _subject.DownloadAsync(Request(), ReadAll, CancellationToken.None);
        _http.Timeout.Should().Be(timeout);
        _http.DefaultRequestHeaders.GetValues("X-Consumer").Should().Contain("configured");
    }

    [TestMethod]
    public async Task AddWeatherParsing_ResolvesCatalogDownloader()
    {
        var provider = new ServiceCollection()
            .AddWeatherParsing()
            .AddTestConsumerServices()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var downloader = provider.GetRequiredService<ICatalogDownloader>();
        downloader.Should().BeOfType<CatalogDownloader>();
    }
}
