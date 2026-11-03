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
public sealed class MosmixLForecastSourceIdTests
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

    [TestMethod]
    [Timeout(10000)]
    [DataRow("")]
    [DataRow("..")]
    [DataRow(".")]
    [DataRow("../x")]
    [DataRow("abcd")]
    [DataRow("1063a")]
    [DataRow("106")]
    [DataRow("106377")]
    [DataRow("10 37")]
    [DataRow("10%37")]
    [DataRow("1063/")]
    [DataRow("1O63\u00e4")]
    public async Task RetrieveAsync_InvalidId_ThrowsIdInvalidWithoutRequest(string id)
    {
        // Arrange
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = LOptions();
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, _downloader.Object);

        // Act
        var act = () => source.RetrieveAsync(id, ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-ID-INVALID" && ex.Product == MosmixProduct.MosmixL);
    }

    [TestMethod]
    [Timeout(10000)]
    [DataRow("abcd")]
    [DataRow("106377")]
    [DataRow("10%37")]
    [DataRow("../x")]
    public async Task RetrieveAsync_InvalidId_MessageOmitsId(string id)
    {
        // Arrange
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = LOptions();
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, _downloader.Object);

        // Act
        var act = () => source.RetrieveAsync(id, ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        (await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>())
            .Which.Message.Should().NotContain(id);
    }

    [TestMethod]
    [Timeout(10000)]
    [DataRow("1063")]
    [DataRow("10637")]
    [DataRow("X123A")]
    [DataRow("01001")]
    [DataRow("ZZZZZ")]
    public async Task RetrieveAsync_ValidId_Accepted(string id)
    {
        // Arrange
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/") };
        var options = LOptions();
        var source = new MosmixLForecastSource(endpoints, options, _uriValidator, _downloader.Object);
        CatalogRequest? captured = null;
        _downloader.Setup(d => d.DownloadAsync(It.IsAny<CatalogRequest>(), It.IsAny<Func<Stream, CancellationToken, Task<byte[]>>>(), It.IsAny<CancellationToken>()))
            .Callback<CatalogRequest, Func<Stream, CancellationToken, Task<byte[]>>, CancellationToken>((req, _, _) => captured = req)
            .ReturnsAsync(new byte[] { 1 });

        // Act
        await source.RetrieveAsync(id, ReadAll, CancellationToken.None, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.Uri.AbsoluteUri.Should().Be($"https://example.org/mosmix/single_stations/{id}/kml/MOSMIX_L_LATEST_{id}.kmz");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task NotFound_AfterCallerCancellation_ThrowsOperationCanceledWithCallerToken()
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
            .ReturnsAsync((byte[]?)null);

        // Act
        var act = () => source.RetrieveAsync("10637", ReadAll, CancellationToken.None, caller.Token);

        // Assert
        await FluentActions.Awaiting(act).Should().ThrowAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task NotFound_AfterDeadlineExpiry_ThrowsDeadlineExceeded()
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
            .ReturnsAsync((byte[]?)null);

        // Act
        var act = () => source.RetrieveAsync("10637", ReadAll, deadline.Token, caller.Token);

        // Assert
        await FluentActions.Awaiting(act).Should().ThrowAsync<MosmixForecastException>()
            .Where(ex => ex.ErrorId == "MOSMIX-DEADLINE-EXCEEDED" && ex.Product == MosmixProduct.MosmixL);
    }
}
