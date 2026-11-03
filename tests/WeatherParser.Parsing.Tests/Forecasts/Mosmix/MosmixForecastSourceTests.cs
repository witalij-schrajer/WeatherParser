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
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class MosmixForecastSourceTests
{
    private MockRepository _mocks = null!;
    private Mock<ICatalogDownloader> _downloader = null!;
    private IEndpointUriValidator _uriValidator = null!;
    private ManualTimeProvider _time = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _downloader = _mocks.Create<ICatalogDownloader>();
        _uriValidator = new EndpointUriValidator();
        _time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 23, 4, 0, 0, TimeSpan.Zero));
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private static async Task<byte[]> ReadAll(Stream stream, CancellationToken token) { using var copy = new MemoryStream(); await stream.CopyToAsync(copy, token); return copy.ToArray(); }

    private static MosmixLForecastOptions LOptions() => new() { MaxRunAge = TimeSpan.FromHours(8) };
    private static MosmixSForecastOptions SOptions() => new() { MaxRunAge = TimeSpan.FromHours(2) };

    [TestMethod]
    [DataRow("10637", "https://example.org/mosmix/single_stations/10637/kml/MOSMIX_L_LATEST_10637.kmz")]
    [DataRow("01001", "https://example.org/mosmix/single_stations/01001/kml/MOSMIX_L_LATEST_01001.kmz")]
    [DataRow("X123A", "https://example.org/mosmix/single_stations/X123A/kml/MOSMIX_L_LATEST_X123A.kmz")]
    public async Task MosmixL_RetrieveAsync_ComposesSingleStationUri(string id, string expected)
    {
        // Arrange
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = LOptions();
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, _downloader.Object);
        CatalogRequest? captured = null;
        _downloader.Setup(d => d.DownloadAsync(It.IsAny<CatalogRequest>(), It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(), It.IsAny<CancellationToken>()))
            .Callback<CatalogRequest, Func<Stream, CancellationToken, Task<byte[]>>, CancellationToken>((req, _, _) => captured = req)
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        // Act
        var result = await source.RetrieveAsync(id, ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured.Uri.AbsoluteUri.Should().Be(expected);
        captured.EndpointName.Should().Be("MosmixLSingleStationsDirectory");
        captured.MaxBytes.Should().Be(CatalogRequest.PerStationFileMaxBytes);
        captured.NotFoundIsAbsent.Should().BeTrue();
        captured.DeadlineKind.Should().Be(CatalogDeadlineKind.MosmixL);
        captured.RequestTimeout.Should().BeNull();
        result.Should().Equal(new byte[] { 1, 2, 3 });
    }

    [TestMethod]
    public async Task MosmixS_RetrieveAsync_UsesConfiguredUriUnchanged()
    {
        // Arrange
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz?mirror=1") };
        var options = SOptions();
        var source = new MosmixSForecastSource(endpoints, options, _uriValidator, _downloader.Object);
        CatalogRequest? captured = null;
        _downloader.Setup(d => d.DownloadAsync(It.IsAny<CatalogRequest>(), It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(), It.IsAny<CancellationToken>()))
            .Callback<CatalogRequest, Func<Stream, CancellationToken, Task<byte[]>>, CancellationToken>((req, _, _) => captured = req)
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        // Act
        var result = await source.RetrieveAsync(ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured.Uri.OriginalString.Should().Be("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz?mirror=1");
        captured.EndpointName.Should().Be("MosmixSAllStationsUri");
        captured.MaxBytes.Should().Be(CatalogRequest.MosmixSAllStationsMaxBytes);
        captured.NotFoundIsAbsent.Should().BeFalse();
        captured.DeadlineKind.Should().Be(CatalogDeadlineKind.MosmixS);
        captured.RequestTimeout.Should().Be(TimeSpan.FromMinutes(5));
        result.Should().Equal(new byte[] { 1, 2, 3 });
    }

    [TestMethod]
    public async Task MosmixL_NotFound_ThrowsIdUnknown()
    {
        // Arrange
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = LOptions();
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, _downloader.Object);
        _downloader.Setup(d => d.DownloadAsync(It.IsAny<CatalogRequest>(), It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var act = () => source.RetrieveAsync("10637", ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        var exception = (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>().Where(ex => ex.ErrorId == "MOSMIX-ID-UNKNOWN" && ex.Product == MosmixProduct.MosmixL)).Which;
        exception.ErrorId.Should().Be("MOSMIX-ID-UNKNOWN");
        exception.Product.Should().Be(MosmixProduct.MosmixL);
    }

    [TestMethod]
    [DataRow("CATALOG-TOO-LARGE", "MOSMIX-TOO-LARGE", false)]
    [DataRow("CATALOG-RETRIEVAL-FAILED", "MOSMIX-RETRIEVAL-FAILED", false)]
    [DataRow("CATALOG-TIMEOUT", "MOSMIX-RETRIEVAL-FAILED", false)]
    public async Task RetrievalFailures_MappedToProductIds(string catalogId, string expected, bool large)
    {
        // Arrange
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = LOptions();
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, _downloader.Object);
        _downloader.Setup(d => d.DownloadAsync(It.IsAny<CatalogRequest>(), It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CatalogRetrievalException(catalogId, "x", "m"));

        // Act
        var act = () => source.RetrieveAsync("10637", ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        var exception = (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>().Where(ex => ex.ErrorId == expected && ex.Product == MosmixProduct.MosmixL)).Which;
        exception.ErrorId.Should().Be(expected);
        exception.Product.Should().Be(MosmixProduct.MosmixL);
        exception.Message.Should().Contain("MosmixLSingleStationsDirectory")
            .And.Contain("example.org")
            .And.NotContain("mirror");
    }

    [TestMethod]
    [DataRow("CATALOG-TOO-LARGE", "MOSMIX-TOO-LARGE")]
    [DataRow("CATALOG-RETRIEVAL-FAILED", "MOSMIX-RETRIEVAL-FAILED")]
    public async Task MosmixS_RetrievalFailures_MappedToProductIds(string catalogId, string expected)
    {
        // Arrange
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz?mirror=secret") };
        var options = SOptions();
        var source = new MosmixSForecastSource(endpoints, options, _uriValidator, _downloader.Object);
        _downloader.Setup(d => d.DownloadAsync(It.IsAny<CatalogRequest>(), It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CatalogRetrievalException(catalogId, "x", "m"));

        // Act
        var act = () => source.RetrieveAsync(ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        var exception = (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>().Where(ex => ex.ErrorId == expected && ex.Product == MosmixProduct.MosmixS)).Which;
        exception.ErrorId.Should().Be(expected);
        exception.Product.Should().Be(MosmixProduct.MosmixS);
        exception.Message.Should().Contain("MosmixSAllStationsUri")
            .And.Contain("example.org")
            .And.NotContain("mirror")
            .And.NotContain("secret");
    }

    [TestMethod]
    public async Task DeadlineAndCancellation_Propagate()
    {
        // Arrange
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz") };
        var options = SOptions();
        var source = new MosmixSForecastSource(endpoints, options, _uriValidator, _downloader.Object);
        var deadline = new CancellationTokenSource();
        var expected = new MosmixForecastException(MosmixForecastErrorIds.DeadlineExceeded, MosmixProduct.MosmixS, "m");
        _downloader.Setup(d => d.DownloadAsync(It.IsAny<CatalogRequest>(), It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expected);

        // Act
        var act = () => source.RetrieveAsync(ReadAll, deadline.Token, CancellationToken.None);

        // Assert
        (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()).Which.Should().BeSameAs(expected);
    }

    [TestMethod]
    public async Task Sources_DoNotShareState()
    {
        // Arrange
        var lEndpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var sEndpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz") };
        var options = LOptions();
        var lSource = new MosmixLForecastSource(lEndpoints, options, _uriValidator, _downloader.Object);
        var sSource = new MosmixSForecastSource(sEndpoints, SOptions(), _uriValidator, _downloader.Object);
        CatalogRequest? lCaptured = null;
        CatalogRequest? sCaptured = null;
        _downloader.Setup(d => d.DownloadAsync(It.IsAny<CatalogRequest>(), It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(), It.IsAny<CancellationToken>()))
            .Callback<CatalogRequest, Func<Stream, CancellationToken, Task<byte[]>>, CancellationToken>((req, _, _) =>
            {
                if (req.EndpointName == "MosmixLSingleStationsDirectory")
                    lCaptured = req;
                else if (req.EndpointName == "MosmixSAllStationsUri")
                    sCaptured = req;
            })
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        // Act
        await lSource.RetrieveAsync("10637", ReadAll, CancellationToken.None, CancellationToken.None);
        await sSource.RetrieveAsync(ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        lCaptured.Should().NotBeNull();
        lCaptured.EndpointName.Should().Be("MosmixLSingleStationsDirectory");
        lCaptured.Uri.AbsoluteUri.Should().Contain("/10637/kml/");
        sCaptured.Should().NotBeNull();
        sCaptured.EndpointName.Should().Be("MosmixSAllStationsUri");
        sCaptured.Uri.AbsoluteUri.Should().Contain("MOSMIX_S_LATEST_240.kmz");
    }

    [TestMethod]
    public async Task RetrieveAsync_NullArguments_ThrowArgumentNullException()
    {
        // Arrange
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = LOptions();
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, _downloader.Object);

        // Act
        var act1 = () => source.RetrieveAsync((string)null!, ReadAll, CancellationToken.None, CancellationToken.None);
        var act2 = () => source.RetrieveAsync("10637", (Func<Stream, CancellationToken, Task<byte[]>>)null!, CancellationToken.None, CancellationToken.None);
        var act3 = () => source.RetrieveAsync((string)null!, ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        await FluentActions.Awaiting(act1).Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(act2).Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(act3).Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Composed_MosmixL_Http404_ThrowsIdUnknown()
    {
        // Arrange
        var handler = _mocks.Create<HttpMessageHandler>();
        var http = new HttpClient(handler.Object, disposeHandler: false);
        var downloader = new CatalogDownloader(http, _time);
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = LOptions();
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, downloader);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var act = () => source.RetrieveAsync("10637", ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        var exception = (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>().Where(ex => ex.ErrorId == "MOSMIX-ID-UNKNOWN" && ex.Product == MosmixProduct.MosmixL)).Which;
        exception.ErrorId.Should().Be("MOSMIX-ID-UNKNOWN");
        exception.Product.Should().Be(MosmixProduct.MosmixL);
    }

    [TestMethod]
    [Timeout(10000)]
    [DataRow(500)]
    [DataRow(503)]
    public async Task Composed_StatusFailure_RetrievalFailed(int status)
    {
        // Arrange
        var handler = _mocks.Create<HttpMessageHandler>();
        var http = new HttpClient(handler.Object, disposeHandler: false);
        var downloader = new CatalogDownloader(http, _time);
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = LOptions();
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, downloader);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage((HttpStatusCode)status));

        // Act
        var act = () => source.RetrieveAsync("10637", ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        var exception = (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>().Where(ex => ex.ErrorId == "MOSMIX-RETRIEVAL-FAILED" && ex.Product == MosmixProduct.MosmixL)).Which;
        exception.ErrorId.Should().Be("MOSMIX-RETRIEVAL-FAILED");
        exception.Product.Should().Be(MosmixProduct.MosmixL);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Composed_MosmixS_Http404_RetrievalFailed()
    {
        // Arrange
        var handler = _mocks.Create<HttpMessageHandler>();
        var http = new HttpClient(handler.Object, disposeHandler: false);
        var downloader = new CatalogDownloader(http, _time);
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz") };
        var options = SOptions();
        var source = new MosmixSForecastSource(endpoints, options, _uriValidator, downloader);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        // Act
        var act = () => source.RetrieveAsync(ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        var exception = (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>().Where(ex => ex.ErrorId == "MOSMIX-RETRIEVAL-FAILED" && ex.Product == MosmixProduct.MosmixS)).Which;
        exception.ErrorId.Should().Be("MOSMIX-RETRIEVAL-FAILED");
        exception.Product.Should().Be(MosmixProduct.MosmixS);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Composed_ContentLengthAboveBound_TooLarge()
    {
        // Arrange
        var handler = _mocks.Create<HttpMessageHandler>();
        var http = new HttpClient(handler.Object, disposeHandler: false);
        var downloader = new CatalogDownloader(http, _time);
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = LOptions();
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, downloader);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(HttpTestContent.Ok(new ByteArrayContent(new byte[1048577])));

        // Act
        var act = () => source.RetrieveAsync("10637", ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        var exception = (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>().Where(ex => ex.ErrorId == "MOSMIX-TOO-LARGE" && ex.Product == MosmixProduct.MosmixL)).Which;
        exception.ErrorId.Should().Be("MOSMIX-TOO-LARGE");
        exception.Product.Should().Be(MosmixProduct.MosmixL);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Composed_MosmixS_BodyStalledBeyond30Seconds_EndsByDeadline()
    {
        // Arrange
        var handler = _mocks.Create<HttpMessageHandler>();
        var http = new HttpClient(handler.Object, disposeHandler: false);
        var downloader = new CatalogDownloader(http, _time);
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz") };
        var options = SOptions();
        var source = new MosmixSForecastSource(endpoints, options, _uriValidator, downloader);
        var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(5), _time);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StallStream()) });

        // Act
        var task = source.RetrieveAsync(ReadAll, deadline.Token, CancellationToken.None);
        await Task.Yield();
        _time.Advance(TimeSpan.FromSeconds(31));
        await Task.Delay(50);
        task.IsCompleted.Should().BeFalse("the MOSMIX_S request bound equals the 5-minute parse deadline, not the fixed 30 seconds");
        _time.Advance(TimeSpan.FromMinutes(5));
        var exception = (await FluentActions.Awaiting(() => task).Should().ThrowAsync<MosmixForecastException>()).Which;
        exception.ErrorId.Should().Be("MOSMIX-DEADLINE-EXCEEDED");
        exception.Product.Should().Be(MosmixProduct.MosmixS);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Composed_CallerCancellationWins()
    {
        // Arrange
        var handler = _mocks.Create<HttpMessageHandler>();
        var http = new HttpClient(handler.Object, disposeHandler: false);
        var downloader = new CatalogDownloader(http, _time);
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz") };
        var options = SOptions();
        var source = new MosmixSForecastSource(endpoints, options, _uriValidator, downloader);
        var caller = new CancellationTokenSource();
        var deadline = new CancellationTokenSource();
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StallStream()) });

        // Act
        var task = source.RetrieveAsync(ReadAll, deadline.Token, caller.Token);
        caller.Cancel();

        // Assert
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == caller.Token);
    }

    private sealed class StallStream : Stream
    {
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
    }
}
