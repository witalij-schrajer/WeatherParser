namespace WeatherParser.Parsing.Tests.Catalogs;

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

[TestClass]
public sealed class CatalogSourceConstructionTests
{
    private MockRepository _mocks = null!;
    private ICdcCatalogEndpointsValidator _cdcValidator = null!;
    private IMosmixCatalogEndpointsValidator _mosmixValidator = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _cdcValidator = new CdcCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator()));
        _mosmixValidator = new MosmixCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator()));
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private static CdcCatalogEndpoints ValidCdcOptions()
    {
        return new CdcCatalogEndpoints
        {
            AirTemperatureStationList = new Uri("https://example.org/cdc/air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt"),
            AirTemperatureNowDirectory = new Uri("https://example.org/cdc/air_temperature/now/"),
            WindStationList = new Uri("https://example.org/cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt"),
            WindNowDirectory = new Uri("https://example.org/cdc/wind/now/"),
            PrecipitationStationList = new Uri("https://example.org/cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt"),
            PrecipitationNowDirectory = new Uri("https://example.org/cdc/precipitation/now/"),
            SolarStationList = new Uri("https://example.org/cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt"),
            SolarNowDirectory = new Uri("https://example.org/cdc/solar/now/"),
        };
    }

    private static MosmixCatalogEndpoints ValidMosmixOptions()
    {
        return new MosmixCatalogEndpoints
        {
            CatalogUri = new Uri("https://example.org/mosmix/stations.cfg"),
            MosmixLSingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/"),
            MosmixSAllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz"),
            EvidenceCacheDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-evidence-test"),
            MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
        };
    }

    [TestMethod]
    public void CdcCatalogSource_InvalidOrRelativeEndpoint_ThrowsBeforeAnyRequest()
    {
        var downloader = _mocks.Create<ICatalogDownloader>();
        var listReader = _mocks.Create<ICdcStationCatalogReader>();
        var evidenceReader = _mocks.Create<ICdcNowEvidenceReader>();

        var options = ValidCdcOptions();
        options.AirTemperatureStationList = new Uri("list.txt", UriKind.Relative);

        Action act = () => new CdcCatalogSource(options, _cdcValidator, downloader.Object, listReader.Object, evidenceReader.Object, new CdcRequestComposer());
        act.Should().Throw<ParserEndpointValidationException>().Which.ErrorId.Should().Be("ENDPOINT-INVALID");
    }

    [TestMethod]
    public void CdcCatalogSource_MissingEndpoint_Throws()
    {
        var downloader = _mocks.Create<ICatalogDownloader>();
        var listReader = _mocks.Create<ICdcStationCatalogReader>();
        var evidenceReader = _mocks.Create<ICdcNowEvidenceReader>();

        var options = ValidCdcOptions();
        options.SolarNowDirectory = null;

        Action act = () => new CdcCatalogSource(options, _cdcValidator, downloader.Object, listReader.Object, evidenceReader.Object, new CdcRequestComposer());
        act.Should().Throw<ParserEndpointValidationException>().Which.ErrorId.Should().Be("ENDPOINT-INVALID");
    }

    [TestMethod]
    public void CdcCatalogSource_InvalidLimit_Throws()
    {
        var downloader = _mocks.Create<ICatalogDownloader>();
        var listReader = _mocks.Create<ICdcStationCatalogReader>();
        var evidenceReader = _mocks.Create<ICdcNowEvidenceReader>();

        var options = ValidCdcOptions();
        options.MaxEvidenceCandidates = 0;

        Action act = () => new CdcCatalogSource(options, _cdcValidator, downloader.Object, listReader.Object, evidenceReader.Object, new CdcRequestComposer());
        act.Should().Throw<SelectionLimitException>().Which.ErrorId.Should().Be("SELECTION-LIMIT-INVALID");
    }

    [TestMethod]
    public void MosmixCatalogSource_InvalidEndpoint_ThrowsBeforeAnyRequest()
    {
        var downloader = _mocks.Create<ICatalogDownloader>();
        var catalogReader = _mocks.Create<IMosmixLocationCatalogReader>();
        var evidenceReader = _mocks.Create<IMosmixLEvidenceReader>();

        var options = ValidMosmixOptions();
        options.CatalogUri = null;

        Action act = () => new MosmixCatalogSource(options, _mosmixValidator, downloader.Object, catalogReader.Object, evidenceReader.Object, TimeProvider.System);
        act.Should().Throw<ParserEndpointValidationException>().Which.ErrorId.Should().Be("ENDPOINT-INVALID");
    }

    [TestMethod]
    public void MosmixCatalogSource_InvalidCacheSetting_Throws()
    {
        var downloader = _mocks.Create<ICatalogDownloader>();
        var catalogReader = _mocks.Create<IMosmixLocationCatalogReader>();
        var evidenceReader = _mocks.Create<IMosmixLEvidenceReader>();

        var options = ValidMosmixOptions();
        options.MosmixSEvidenceMaxAge = null;

        Action act = () => new MosmixCatalogSource(options, _mosmixValidator, downloader.Object, catalogReader.Object, evidenceReader.Object, TimeProvider.System);
        act.Should().Throw<EvidenceCacheException>().Which.ErrorId.Should().Be("EVIDENCE-CACHE-SETTINGS-INVALID");
    }

    [TestMethod]
    public void CdcCatalogSource_ValidOptions_ConstructsWithoutRequest()
    {
        var downloader = _mocks.Create<ICatalogDownloader>();
        var listReader = _mocks.Create<ICdcStationCatalogReader>();
        var evidenceReader = _mocks.Create<ICdcNowEvidenceReader>();

        var options = ValidCdcOptions();

        Action act = () => new CdcCatalogSource(options, _cdcValidator, downloader.Object, listReader.Object, evidenceReader.Object, new CdcRequestComposer());
        act.Should().NotThrow();
    }

    [TestMethod]
    public void MosmixCatalogSource_ValidOptions_ConstructsWithoutRequest()
    {
        var downloader = _mocks.Create<ICatalogDownloader>();
        var catalogReader = _mocks.Create<IMosmixLocationCatalogReader>();
        var evidenceReader = _mocks.Create<IMosmixLEvidenceReader>();

        var options = ValidMosmixOptions();

        Action act = () => new MosmixCatalogSource(options, _mosmixValidator, downloader.Object, catalogReader.Object, evidenceReader.Object, TimeProvider.System);
        act.Should().NotThrow();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesCdcCatalogSource()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<ICdcCatalogSource>().Should().BeOfType<CdcCatalogSource>();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesMosmixCatalogSource()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IMosmixCatalogSource>().Should().BeOfType<MosmixCatalogSource>();
    }

    [TestMethod]
    public void Container_MissingEndpointOptions_FailsAtValidation()
    {
        var services = new ServiceCollection().AddWeatherParsing();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(new HttpClient());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(ValidMosmixOptions());

        Action act = () => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        act.Should().Throw<AggregateException>();
    }
}
