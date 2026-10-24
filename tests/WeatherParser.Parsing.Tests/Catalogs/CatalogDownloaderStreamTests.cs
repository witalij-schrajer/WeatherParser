namespace WeatherParser.Parsing.Tests.Catalogs;

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
public sealed class CatalogDownloaderStreamTests
{
    private MockRepository _mocks = null!;
    private Mock<HttpMessageHandler> _handler = null!;
    private HttpClient _http = null!;
    private ManualTimeProvider _time = null!;
    private CatalogDownloader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _handler = _mocks.Create<HttpMessageHandler>();
        _http = new(_handler.Object, disposeHandler: false);
        _time = new(DateTimeOffset.UtcNow);
        _subject = new(_http, _time);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
        _http.Dispose();
    }

    private void Respond(HttpResponseMessage response) =>
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

    private static async Task<byte[]> ReadSync(Stream stream, CancellationToken token)
    {
        var buffer = new byte[16];
        using var copy = new MemoryStream();
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            copy.Write(buffer, 0, read);
        }
        return copy.ToArray();
    }

    private sealed class SyncFailingStream : MemoryStream
    {
        public override int Read(byte[] buffer, int offset, int count) => throw new IOException("reset");
    }

    [TestMethod]
    public async Task DownloadAsync_SynchronousReads_ReturnContent()
    {
        var body = HttpTestContent.Bytes(40);
        var response = HttpTestContent.Ok(HttpTestContent.Known(body));
        Respond(response);

        var result = await _subject.DownloadAsync(
            new CatalogRequest("AirTemperatureStationList", new Uri("https://example.org/list.txt"), 100, false, CancellationToken.None, CatalogDeadlineKind.Selection),
            ReadSync,
            CancellationToken.None);

        result.Should().Equal(body);
    }

    [TestMethod]
    public async Task DownloadAsync_SynchronousReadAboveBound_IsTooLarge()
    {
        var body = HttpTestContent.Bytes(200);
        var response = HttpTestContent.Ok(HttpTestContent.Unknown(body));
        Respond(response);

        Func<Task> act = () => _subject.DownloadAsync(
            new CatalogRequest("AirTemperatureStationList", new Uri("https://example.org/list.txt"), 100, false, CancellationToken.None, CatalogDeadlineKind.Selection),
            ReadSync,
            CancellationToken.None);

        var exception = await FluentActions.Awaiting(act).Should().ThrowAsync<CatalogRetrievalException>();
        exception.Which.ErrorId.Should().Be(CatalogRetrievalException.TooLargeErrorId);
    }

    [TestMethod]
    public async Task DownloadAsync_SynchronousReadFailure_IsRetrievalFailure()
    {
        var response = HttpTestContent.Ok(new StreamContent(new SyncFailingStream()));
        Respond(response);

        Func<Task> act = () => _subject.DownloadAsync(
            new CatalogRequest("AirTemperatureStationList", new Uri("https://example.org/list.txt"), 100, false, CancellationToken.None, CatalogDeadlineKind.Selection),
            ReadSync,
            CancellationToken.None);

        var exception = await FluentActions.Awaiting(act).Should().ThrowAsync<CatalogRetrievalException>();
        exception.Which.ErrorId.Should().Be(CatalogRetrievalException.RetrievalFailedErrorId);
        exception.Which.Message.Should().Contain("IOException");
    }

    [TestMethod]
    public async Task DownloadAsync_NonDefaultPort_IsKeptInSanitizedUri()
    {
        var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        Respond(response);

        Func<Task> act = () => _subject.DownloadAsync(
            new CatalogRequest("AirTemperatureStationList", new Uri("https://user:secret@example.org:8443/cdc/list.txt?token=hidden"), 100, false, CancellationToken.None, CatalogDeadlineKind.Selection),
            ReadSync,
            CancellationToken.None);

        var exception = await FluentActions.Awaiting(act).Should().ThrowAsync<CatalogRetrievalException>();
        exception.Which.ErrorId.Should().Be(CatalogRetrievalException.RetrievalFailedErrorId);
        exception.Which.Message.Should().Contain("https://example.org:8443/cdc/list.txt");
        exception.Which.Message.Should().NotContain("secret");
        exception.Which.Message.Should().NotContain("hidden");
    }

    [TestMethod]
    public async Task DownloadAsync_NullArguments_Throw()
    {
        Func<Task> act1 = () => _subject.DownloadAsync<byte[]>(null!, ReadSync, CancellationToken.None);
        var exception1 = await FluentActions.Awaiting(act1).Should().ThrowAsync<ArgumentNullException>();
        exception1.Which.ParamName.Should().Be("request");

        Func<Task> act2 = () => _subject.DownloadAsync<byte[]>(
            new CatalogRequest("AirTemperatureStationList", new Uri("https://example.org/list.txt"), 100, false, CancellationToken.None, CatalogDeadlineKind.Selection),
            null!,
            CancellationToken.None);
        var exception2 = await FluentActions.Awaiting(act2).Should().ThrowAsync<ArgumentNullException>();
        exception2.Which.ParamName.Should().Be("read");
    }

    [TestMethod]
    public void CatalogDownloader_NullDependencies_Throw()
    {
        FluentActions.Invoking(() => new CatalogDownloader(null!, _time))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("httpClient");

        FluentActions.Invoking(() => new CatalogDownloader(_http, null!))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("timeProvider");
    }
}
