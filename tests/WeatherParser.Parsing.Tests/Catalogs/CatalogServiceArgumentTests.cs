using System;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class CatalogServiceArgumentTests
{
    [TestMethod]
    public void CdcCatalogSource_NullArguments_Throw()
    {
        var options = new CdcCatalogEndpoints();
        var validator = new Mock<ICdcCatalogEndpointsValidator>().Object;
        var downloader = new Mock<ICatalogDownloader>().Object;
        var listReader = new Mock<ICdcStationCatalogReader>().Object;
        var evidenceReader = new Mock<ICdcNowEvidenceReader>().Object;

        FluentActions.Invoking(() => new CdcCatalogSource(null!, validator, downloader, listReader, evidenceReader))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("options");

        FluentActions.Invoking(() => new CdcCatalogSource(options, null!, downloader, listReader, evidenceReader))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("validator");

        FluentActions.Invoking(() => new CdcCatalogSource(options, validator, null!, listReader, evidenceReader))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("downloader");

        FluentActions.Invoking(() => new CdcCatalogSource(options, validator, downloader, null!, evidenceReader))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("listReader");

        FluentActions.Invoking(() => new CdcCatalogSource(options, validator, downloader, listReader, null!))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("evidenceReader");
    }

    [TestMethod]
    public void MosmixCatalogSource_NullArguments_Throw()
    {
        var options = new MosmixCatalogEndpoints();
        var validator = new Mock<IMosmixCatalogEndpointsValidator>().Object;
        var downloader = new Mock<ICatalogDownloader>().Object;
        var catalogReader = new Mock<IMosmixLocationCatalogReader>().Object;
        var evidenceReader = new Mock<IMosmixLEvidenceReader>().Object;
        var timeProvider = new Mock<TimeProvider>().Object;

        FluentActions.Invoking(() => new MosmixCatalogSource(null!, validator, downloader, catalogReader, evidenceReader, timeProvider))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("options");

        FluentActions.Invoking(() => new MosmixCatalogSource(options, null!, downloader, catalogReader, evidenceReader, timeProvider))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("validator");

        FluentActions.Invoking(() => new MosmixCatalogSource(options, validator, null!, catalogReader, evidenceReader, timeProvider))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("downloader");

        FluentActions.Invoking(() => new MosmixCatalogSource(options, validator, downloader, null!, evidenceReader, timeProvider))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("catalogReader");

        FluentActions.Invoking(() => new MosmixCatalogSource(options, validator, downloader, catalogReader, null!, timeProvider))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("evidenceReader");

        FluentActions.Invoking(() => new MosmixCatalogSource(options, validator, downloader, catalogReader, evidenceReader, null!))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("timeProvider");
    }

    [TestMethod]
    public void MosmixSEvidenceRefresher_NullArguments_Throw()
    {
        var options = new MosmixCatalogEndpoints();
        var validator = new Mock<IMosmixCatalogEndpointsValidator>().Object;
        var downloader = new Mock<ICatalogDownloader>().Object;
        var cache = new Mock<IMosmixEvidenceCache>().Object;
        var timeProvider = new Mock<TimeProvider>().Object;

        FluentActions.Invoking(() => new MosmixSEvidenceRefresher(null!, validator, downloader, cache, timeProvider))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("options");

        FluentActions.Invoking(() => new MosmixSEvidenceRefresher(options, null!, downloader, cache, timeProvider))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("validator");

        FluentActions.Invoking(() => new MosmixSEvidenceRefresher(options, validator, null!, cache, timeProvider))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("downloader");

        FluentActions.Invoking(() => new MosmixSEvidenceRefresher(options, validator, downloader, null!, timeProvider))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("cache");

        FluentActions.Invoking(() => new MosmixSEvidenceRefresher(options, validator, downloader, cache, null!))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("timeProvider");
    }

    [TestMethod]
    public void CdcCatalogEndpointsValidator_NullEndpointValidator_Throws()
    {
        FluentActions.Invoking(() => new CdcCatalogEndpointsValidator(null!))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("endpointValidator");
    }

    [TestMethod]
    public async Task CdcCatalogSource_DuplicateEvidenceKey_ThrowsArgumentException()
    {
        var options = new CdcCatalogEndpoints
        {
            AirTemperatureStationList = new Uri("https://example.org/cdc/air_temperature/now/list.txt"),
            AirTemperatureNowDirectory = new Uri("https://example.org/cdc/air_temperature/now/"),
            WindStationList = new Uri("https://example.org/cdc/wind/now/list.txt"),
            WindNowDirectory = new Uri("https://example.org/cdc/wind/now/"),
            PrecipitationStationList = new Uri("https://example.org/cdc/precipitation/now/list.txt"),
            PrecipitationNowDirectory = new Uri("https://example.org/cdc/precipitation/now/"),
            SolarStationList = new Uri("https://example.org/cdc/solar/now/list.txt"),
            SolarNowDirectory = new Uri("https://example.org/cdc/solar/now/"),
        };
        var validator = new CdcCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator()));
        var downloader = new Mock<ICatalogDownloader>().Object;
        var listReader = new Mock<ICdcStationCatalogReader>().Object;
        var evidenceReader = new Mock<ICdcNowEvidenceReader>().Object;
        var source = new CdcCatalogSource(options, validator, downloader, listReader, evidenceReader);

        (await FluentActions.Awaiting(() => source.ReadEvidenceAsync(new[] { new CdcEvidenceKey("00044", "tu"), new CdcEvidenceKey("00044", "tu") }, CancellationToken.None, CancellationToken.None)).Should().ThrowAsync<ArgumentException>())
            .Which.Message.Should().Contain("00044/tu");
    }

    [TestMethod]
    public async Task Sources_NullCollections_Throw()
    {
        var options = new CdcCatalogEndpoints
        {
            AirTemperatureStationList = new Uri("https://example.org/cdc/air_temperature/now/list.txt"),
            AirTemperatureNowDirectory = new Uri("https://example.org/cdc/air_temperature/now/"),
            WindStationList = new Uri("https://example.org/cdc/wind/now/list.txt"),
            WindNowDirectory = new Uri("https://example.org/cdc/wind/now/"),
            PrecipitationStationList = new Uri("https://example.org/cdc/precipitation/now/list.txt"),
            PrecipitationNowDirectory = new Uri("https://example.org/cdc/precipitation/now/"),
            SolarStationList = new Uri("https://example.org/cdc/solar/now/list.txt"),
            SolarNowDirectory = new Uri("https://example.org/cdc/solar/now/"),
        };
        var validator = new CdcCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator()));
        var downloader = new Mock<ICatalogDownloader>().Object;
        var listReader = new Mock<ICdcStationCatalogReader>().Object;
        var evidenceReader = new Mock<ICdcNowEvidenceReader>().Object;
        var source = new CdcCatalogSource(options, validator, downloader, listReader, evidenceReader);

        (await FluentActions.Awaiting(() => source.ReadEvidenceAsync(null!, CancellationToken.None, CancellationToken.None)).Should().ThrowAsync<ArgumentNullException>())
            .Which.ParamName.Should().Be("keys");

        var mosmixOptions = new MosmixCatalogEndpoints
        {
            CatalogUri = new Uri("https://example.org/mosmix/stations.cfg"),
            MosmixLSingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/"),
            MosmixSAllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz?mirror=1"),
            EvidenceCacheDirectory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "weatherparser-evidence-test"),
            MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
        };
        var mosmixValidator = new MosmixCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator()));
        var mosmixSource = new MosmixCatalogSource(mosmixOptions, mosmixValidator, downloader, new Mock<IMosmixLocationCatalogReader>().Object, new Mock<IMosmixLEvidenceReader>().Object, new Mock<TimeProvider>().Object);

        (await FluentActions.Awaiting(() => mosmixSource.ReadMosmixLEvidenceAsync(null!, CancellationToken.None, CancellationToken.None)).Should().ThrowAsync<ArgumentNullException>())
            .Which.ParamName.Should().Be("mosmixIds");
    }
}
