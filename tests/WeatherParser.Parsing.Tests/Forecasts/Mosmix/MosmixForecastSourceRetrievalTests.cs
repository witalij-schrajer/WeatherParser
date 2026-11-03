using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
public sealed class MosmixForecastSourceRetrievalTests
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
    [Timeout(10000)]
    public async Task MosmixL_OneRequestNoRetry()
    {
        // Arrange
        var handler = _mocks.Create<HttpMessageHandler>();
        var http = new HttpClient(handler.Object, disposeHandler: false);
        var downloader = new CatalogDownloader(http, _time);
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = LOptions();
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, downloader);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act
        var act = () => source.RetrieveAsync("10637", ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-RETRIEVAL-FAILED" && ex.Product == MosmixProduct.MosmixL);
        handler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task MosmixS_OneRequestNoRetry()
    {
        // Arrange
        var handler = _mocks.Create<HttpMessageHandler>();
        var http = new HttpClient(handler.Object, disposeHandler: false);
        var downloader = new CatalogDownloader(http, _time);
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz") };
        var options = SOptions();
        var source = new MosmixSForecastSource(endpoints, options, _uriValidator, downloader);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act
        var act = () => source.RetrieveAsync(ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-RETRIEVAL-FAILED" && ex.Product == MosmixProduct.MosmixS);
        handler.Protected().Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task MosmixL_TransportFailure_RetrievalFailed()
    {
        // Arrange
        var handler = _mocks.Create<HttpMessageHandler>();
        var http = new HttpClient(handler.Object, disposeHandler: false);
        var downloader = new CatalogDownloader(http, _time);
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = LOptions();
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, downloader);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection reset"));

        // Act
        var act = () => source.RetrieveAsync("10637", ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-RETRIEVAL-FAILED" && ex.Product == MosmixProduct.MosmixL);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task MosmixS_TransportFailure_RetrievalFailed()
    {
        // Arrange
        var handler = _mocks.Create<HttpMessageHandler>();
        var http = new HttpClient(handler.Object, disposeHandler: false);
        var downloader = new CatalogDownloader(http, _time);
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz") };
        var options = SOptions();
        var source = new MosmixSForecastSource(endpoints, options, _uriValidator, downloader);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection reset"));

        // Act
        var act = () => source.RetrieveAsync(ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-RETRIEVAL-FAILED" && ex.Product == MosmixProduct.MosmixS);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task MosmixL_StreamedBodyAboveBound_TooLarge()
    {
        // Arrange
        var handler = _mocks.Create<HttpMessageHandler>();
        var http = new HttpClient(handler.Object, disposeHandler: false);
        var downloader = new CatalogDownloader(http, _time);
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = LOptions();
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, downloader);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(HttpTestContent.Ok(HttpTestContent.Unknown(HttpTestContent.Bytes(1048577))));

        // Act
        var act = () => source.RetrieveAsync("10637", ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-TOO-LARGE" && ex.Product == MosmixProduct.MosmixL);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task MosmixL_BodyStalledAfterHeaders_EndsByDeadline()
    {
        // Arrange
        var handler = _mocks.Create<HttpMessageHandler>();
        var http = new HttpClient(handler.Object, disposeHandler: false);
        var downloader = new CatalogDownloader(http, _time);
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(10), MaxRunAge = TimeSpan.FromHours(8) };
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, downloader);
        var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10), _time);
        handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new StallStream()) });

        // Act
        var task = source.RetrieveAsync("10637", ReadAll, deadline.Token, CancellationToken.None);
        await Task.Yield();
        _time.Advance(TimeSpan.FromSeconds(11));

        // Assert
        await FluentActions.Awaiting(() => task).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-DEADLINE-EXCEEDED" && ex.Product == MosmixProduct.MosmixL);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task MosmixL_RequestCarriesNoTimeout()
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
        await source.RetrieveAsync("10637", ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured.RequestTimeout.Should().BeNull();
        captured.MaxBytes.Should().Be(CatalogRequest.PerStationFileMaxBytes);
        captured.NotFoundIsAbsent.Should().BeTrue();
        captured.DeadlineKind.Should().Be(CatalogDeadlineKind.MosmixL);
        captured.EndpointName.Should().Be("MosmixLSingleStationsDirectory");
    }

    [TestMethod]
    [Timeout(10000)]
    [DataRow(1)]
    [DataRow(3600)]
    public async Task MosmixS_RequestCarriesTimeoutEqualToParseDeadline(int seconds)
    {
        // Arrange
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz") };
        var options = new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromSeconds(seconds), MaxRunAge = TimeSpan.FromHours(2) };
        var source = new MosmixSForecastSource(endpoints, options, _uriValidator, _downloader.Object);
        CatalogRequest? captured = null;
        _downloader.Setup(d => d.DownloadAsync(It.IsAny<CatalogRequest>(), It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(), It.IsAny<CancellationToken>()))
            .Callback<CatalogRequest, Func<Stream, CancellationToken, Task<byte[]>>, CancellationToken>((req, _, _) => captured = req)
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        // Act
        await source.RetrieveAsync(ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured.RequestTimeout.Should().Be(TimeSpan.FromSeconds(seconds));
    }

    [TestMethod]
    [Timeout(10000)]
    public void AddWeatherParsing_ResolvesBothSourcesWithIndependentOptions()
    {
        // Arrange
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var lSource = provider.GetRequiredService<IMosmixLForecastSource>();
        var sSource = provider.GetRequiredService<IMosmixSForecastSource>();

        // Act & Assert
        lSource.Should().BeOfType<MosmixLForecastSource>();
        sSource.Should().BeOfType<MosmixSForecastSource>();

        // Arrange second collection
        var services2 = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        services2.Replace(ServiceDescriptor.Singleton(new MosmixLForecastOptions()));
        using var provider2 = services2.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = false });

        // Act & Assert
        FluentActions.Invoking(() => provider2.GetRequiredService<IMosmixSForecastSource>())
            .Should().NotThrow();
        var exception = FluentActions.Invoking(() => provider2.GetRequiredService<IMosmixLForecastSource>())
            .Should().Throw<MosmixForecastException>().Which;
        exception.ErrorId.Should().Be("MOSMIX-OPTIONS-INVALID");
        exception.Product.Should().Be(MosmixProduct.MosmixL);
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
