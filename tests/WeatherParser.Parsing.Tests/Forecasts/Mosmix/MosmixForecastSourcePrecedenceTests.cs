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
using WeatherParser.Parsing.Tests;

[TestClass]
public sealed class MosmixForecastSourcePrecedenceTests
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
    private static async Task<byte[]> Drain(Stream stream, CancellationToken token) { var buffer = new byte[81920]; while (await stream.ReadAsync(buffer, token) > 0) { } return Array.Empty<byte>(); }

    private static MosmixLForecastOptions LOptions() => new() { MaxRunAge = TimeSpan.FromHours(8) };
    private static MosmixSForecastOptions SOptions() => new() { MaxRunAge = TimeSpan.FromHours(2) };

    [TestMethod]
    [Timeout(10000)]
    [DataRow("CATALOG-TOO-LARGE")]
    [DataRow("CATALOG-RETRIEVAL-FAILED")]
    public async Task MosmixL_CallerCancelledThenRetrievalFailure_ThrowsOperationCanceled(string catalogId)
    {
        // Arrange
        var caller = new CancellationTokenSource();
        var deadline = new CancellationTokenSource();
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = LOptions();
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, _downloader.Object);
        _downloader.Setup(d => d.DownloadAsync(It.IsAny<CatalogRequest>(), It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(), It.IsAny<CancellationToken>()))
            .Callback<CatalogRequest, Func<Stream, CancellationToken, Task<byte[]>>, CancellationToken>((_, _, ct) =>
            {
                caller.Cancel();
                deadline.Cancel();
            })
            .ThrowsAsync(new CatalogRetrievalException(catalogId, "x", "m"));

        // Act
        var act = () => source.RetrieveAsync("10637", ReadAll, deadline.Token, caller.Token);

        // Assert
        var exception = (await FluentActions.Awaiting(act).Should().ThrowAsync<OperationCanceledException>()).Which;
        exception.CancellationToken.Should().Be(caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    [DataRow("CATALOG-TOO-LARGE")]
    [DataRow("CATALOG-RETRIEVAL-FAILED")]
    public async Task MosmixL_DeadlineExpiredThenRetrievalFailure_ThrowsDeadlineExceeded(string catalogId)
    {
        // Arrange
        var caller = new CancellationTokenSource();
        var deadline = new CancellationTokenSource();
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = LOptions();
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, _downloader.Object);
        _downloader.Setup(d => d.DownloadAsync(It.IsAny<CatalogRequest>(), It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(), It.IsAny<CancellationToken>()))
            .Callback<CatalogRequest, Func<Stream, CancellationToken, Task<byte[]>>, CancellationToken>((_, _, ct) =>
            {
                deadline.Cancel();
            })
            .ThrowsAsync(new CatalogRetrievalException(catalogId, "x", "m"));

        // Act
        var act = () => source.RetrieveAsync("10637", ReadAll, deadline.Token, caller.Token);

        // Assert
        await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-DEADLINE-EXCEEDED" && ex.Product == MosmixProduct.MosmixL);
    }

    [TestMethod]
    [Timeout(10000)]
    [DataRow("CATALOG-TOO-LARGE")]
    [DataRow("CATALOG-RETRIEVAL-FAILED")]
    public async Task MosmixS_CallerCancelledThenRetrievalFailure_ThrowsOperationCanceled(string catalogId)
    {
        // Arrange
        var caller = new CancellationTokenSource();
        var deadline = new CancellationTokenSource();
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz") };
        var options = SOptions();
        var source = new MosmixSForecastSource(endpoints, options, _uriValidator, _downloader.Object);
        _downloader.Setup(d => d.DownloadAsync(It.IsAny<CatalogRequest>(), It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(), It.IsAny<CancellationToken>()))
            .Callback<CatalogRequest, Func<Stream, CancellationToken, Task<byte[]>>, CancellationToken>((_, _, ct) =>
            {
                caller.Cancel();
                deadline.Cancel();
            })
            .ThrowsAsync(new CatalogRetrievalException(catalogId, "x", "m"));

        // Act
        var act = () => source.RetrieveAsync(ReadAll, deadline.Token, caller.Token);

        // Assert
        var exception = (await FluentActions.Awaiting(act).Should().ThrowAsync<OperationCanceledException>()).Which;
        exception.CancellationToken.Should().Be(caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    [DataRow("CATALOG-TOO-LARGE")]
    [DataRow("CATALOG-RETRIEVAL-FAILED")]
    public async Task MosmixS_DeadlineExpiredThenRetrievalFailure_ThrowsDeadlineExceeded(string catalogId)
    {
        // Arrange
        var caller = new CancellationTokenSource();
        var deadline = new CancellationTokenSource();
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz") };
        var options = SOptions();
        var source = new MosmixSForecastSource(endpoints, options, _uriValidator, _downloader.Object);
        _downloader.Setup(d => d.DownloadAsync(It.IsAny<CatalogRequest>(), It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(), It.IsAny<CancellationToken>()))
            .Callback<CatalogRequest, Func<Stream, CancellationToken, Task<byte[]>>, CancellationToken>((_, _, ct) =>
            {
                deadline.Cancel();
            })
            .ThrowsAsync(new CatalogRetrievalException(catalogId, "x", "m"));

        // Act
        var act = () => source.RetrieveAsync(ReadAll, deadline.Token, caller.Token);

        // Assert
        await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-DEADLINE-EXCEEDED" && ex.Product == MosmixProduct.MosmixS);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task MosmixL_RetrievalFailure_MessageNamesSanitizedFileUri()
    {
        // Arrange
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = LOptions();
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, _downloader.Object);
        _downloader.Setup(d => d.DownloadAsync(It.IsAny<CatalogRequest>(), It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CatalogRetrievalException("CATALOG-RETRIEVAL-FAILED", "x", "m"));

        // Act
        var act = () => source.RetrieveAsync("10637", ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()).Which.Message.Should().Contain("https://example.org/mosmix/single_stations/10637/kml/MOSMIX_L_LATEST_10637.kmz");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task MosmixL_NotFound_MessageNamesSanitizedFileUri()
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
        (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()).Which.ErrorId.Should().Be("MOSMIX-ID-UNKNOWN");
        (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()).Which.Message.Should().Contain("https://example.org/mosmix/single_stations/10637/kml/MOSMIX_L_LATEST_10637.kmz");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task MosmixS_RetrievalFailure_MessageNamesSanitizedUriWithoutQuery()
    {
        // Arrange
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org:8443/mosmix/MOSMIX_S_LATEST_240.kmz?mirror=secret") };
        var options = SOptions();
        var source = new MosmixSForecastSource(endpoints, options, _uriValidator, _downloader.Object);
        _downloader.Setup(d => d.DownloadAsync(It.IsAny<CatalogRequest>(), It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CatalogRetrievalException("CATALOG-RETRIEVAL-FAILED", "x", "m"));

        // Act
        var act = () => source.RetrieveAsync(ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()).Which.Message.Should().Contain("https://example.org:8443/mosmix/MOSMIX_S_LATEST_240.kmz");
        (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()).Which.Message.Should().NotContain("mirror");
        (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()).Which.Message.Should().NotContain("secret");
        (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()).Which.Message.Should().NotContain("?");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task MosmixS_ContentLengthAboveBound_TooLarge()
    {
        // Arrange
        var handler = _mocks.Create<HttpMessageHandler>();
        var http = new HttpClient(handler.Object, disposeHandler: false);
        var downloader = new CatalogDownloader(http, _time);
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz") };
        var options = SOptions();
        var source = new MosmixSForecastSource(endpoints, options, _uriValidator, downloader);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(HttpTestContent.Ok(HttpTestContent.WithLength(HttpTestContent.Bytes(16), CatalogRequest.MosmixSAllStationsMaxBytes + 1)));

        // Act
        var act = () => source.RetrieveAsync(ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-TOO-LARGE" && ex.Product == MosmixProduct.MosmixS);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task MosmixS_StreamedBodyAboveBound_TooLarge()
    {
        // Arrange
        var handler = _mocks.Create<HttpMessageHandler>();
        var http = new HttpClient(handler.Object, disposeHandler: false);
        var downloader = new CatalogDownloader(http, _time);
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz") };
        var options = SOptions();
        var source = new MosmixSForecastSource(endpoints, options, _uriValidator, downloader);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(HttpTestContent.Ok(HttpTestContent.Unknown(HttpTestContent.Bytes(checked((int)CatalogRequest.MosmixSAllStationsMaxBytes + 1)))));

        // Act
        var act = () => source.RetrieveAsync(Drain, CancellationToken.None, CancellationToken.None);

        // Assert
        await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-TOO-LARGE" && ex.Product == MosmixProduct.MosmixS);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task MosmixS_RequestBoundExpiresWithoutDeadline_RetrievalFailed()
    {
        // Arrange
        var handler = _mocks.Create<HttpMessageHandler>();
        var http = new HttpClient(handler.Object, disposeHandler: false);
        var downloader = new CatalogDownloader(http, _time);
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz") };
        var options = new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromSeconds(10), MaxRunAge = TimeSpan.FromHours(2) };
        var source = new MosmixSForecastSource(endpoints, options, _uriValidator, downloader);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StallStream()) });

        // Act
        var task = source.RetrieveAsync(ReadAll, CancellationToken.None, CancellationToken.None);
        await Task.Yield();
        _time.Advance(TimeSpan.FromSeconds(11));

        // Assert
        await FluentActions.Awaiting(() => task).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-RETRIEVAL-FAILED" && ex.Product == MosmixProduct.MosmixS);
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
