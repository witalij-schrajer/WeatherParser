using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Observations.Dwd;

[TestClass]
public sealed class DwdObservationSourceDeadlineTests
{
    private MockRepository _mocks = null!;
    private Mock<ICatalogDownloader> _downloader = null!;
    private Mock<ICdcStationCatalogReader> _listReader = null!;
    private Mock<ICdcNowEvidenceReader> _evidenceReader = null!;
    private ICdcCatalogEndpointsValidator _validator = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _downloader = _mocks.Create<ICatalogDownloader>();
        _listReader = _mocks.Create<ICdcStationCatalogReader>();
        _evidenceReader = _mocks.Create<ICdcNowEvidenceReader>();
        _validator = new CdcCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator()));
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private static CdcCatalogEndpoints ValidEndpoints()
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

    private DwdObservationSource Create(TimeSpan deadline) => new(new DwdObservationOptions { ObservationDeadline = deadline }, ValidEndpoints(), _validator, _downloader.Object, _listReader.Object, _evidenceReader.Object, new CdcRequestComposer());

    private void ShouldBeLimitInvalid(TimeSpan deadline) { FluentActions.Invoking(() => Create(deadline)).Should().Throw<ObservationLimitException>().Which.Should().Match<ObservationLimitException>(ex => ex.ErrorId == ObservationLimitException.LimitInvalidErrorId && ex.Message.Contains("ObservationDeadline")); }

    [TestMethod]
    public void DwdObservationOptions_DefaultDeadline_Is120Seconds()
    {
        new DwdObservationOptions().ObservationDeadline.Should().Be(TimeSpan.FromSeconds(120));
    }

    [TestMethod]
    public void DwdObservationSource_ZeroDeadline_ThrowsLimitInvalid()
    {
        ShouldBeLimitInvalid(TimeSpan.Zero);
    }

    [TestMethod]
    public void DwdObservationSource_NegativeDeadline_ThrowsLimitInvalid()
    {
        ShouldBeLimitInvalid(TimeSpan.FromSeconds(-1));
    }

    [TestMethod]
    public void DwdObservationSource_InfiniteDeadline_ThrowsLimitInvalid()
    {
        ShouldBeLimitInvalid(Timeout.InfiniteTimeSpan);
    }

    [TestMethod]
    public void DwdObservationSource_DeadlineOneTickAboveApprovedMaximum_ThrowsLimitInvalid()
    {
        ShouldBeLimitInvalid(TimeSpan.FromHours(1) + TimeSpan.FromTicks(1));
    }

    [TestMethod]
    public void DwdObservationSource_MaxValueDeadline_ThrowsLimitInvalid()
    {
        ShouldBeLimitInvalid(TimeSpan.MaxValue);
    }

    [TestMethod]
    public void DwdObservationSource_SmallestPositiveDeadline_IsAccepted()
    {
        FluentActions.Invoking(() => Create(TimeSpan.FromTicks(1))).Should().NotThrow();
    }

    [TestMethod]
    public void DwdObservationSource_DeadlineAtApprovedMaximum_IsAccepted()
    {
        FluentActions.Invoking(() => Create(TimeSpan.FromHours(1))).Should().NotThrow();
    }

    [TestMethod]
    public void DwdObservationSource_NullOptions_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new DwdObservationSource(null!, ValidEndpoints(), _validator, _downloader.Object, _listReader.Object, _evidenceReader.Object, new CdcRequestComposer())).Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void DwdObservationSource_NullEndpoints_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new DwdObservationSource(new DwdObservationOptions(), null!, _validator, _downloader.Object, _listReader.Object, _evidenceReader.Object, new CdcRequestComposer())).Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void DwdObservationSource_NullValidator_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new DwdObservationSource(new DwdObservationOptions(), ValidEndpoints(), null!, _downloader.Object, _listReader.Object, _evidenceReader.Object, new CdcRequestComposer())).Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void DwdObservationSource_NullDownloader_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new DwdObservationSource(new DwdObservationOptions(), ValidEndpoints(), _validator, null!, _listReader.Object, _evidenceReader.Object, new CdcRequestComposer())).Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void DwdObservationSource_NullListReader_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new DwdObservationSource(new DwdObservationOptions(), ValidEndpoints(), _validator, _downloader.Object, null!, _evidenceReader.Object, new CdcRequestComposer())).Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void DwdObservationSource_NullEvidenceReader_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new DwdObservationSource(new DwdObservationOptions(), ValidEndpoints(), _validator, _downloader.Object, _listReader.Object, null!, new CdcRequestComposer())).Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void DwdObservationSource_NullComposer_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new DwdObservationSource(new DwdObservationOptions(), ValidEndpoints(), _validator, _downloader.Object, _listReader.Object, _evidenceReader.Object, null!)).Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("composer");
    }

    [TestMethod]
    public void DwdObservationSource_InvalidEndpoints_ThrowsEndpointInvalidAtConstruction()
    {
        var endpoints = ValidEndpoints();
        endpoints.AirTemperatureStationList = new Uri("list.txt", UriKind.Relative);
        FluentActions.Invoking(() => new DwdObservationSource(new DwdObservationOptions(), endpoints, _validator, _downloader.Object, _listReader.Object, _evidenceReader.Object, new CdcRequestComposer())).Should().Throw<ParserEndpointValidationException>().Which.ErrorId.Should().Be("ENDPOINT-INVALID");
    }

    [TestMethod]
    public void DwdObservationSource_Construction_MakesNoRequest()
    {
        Create(TimeSpan.FromSeconds(120)).Should().NotBeNull();
    }

    [DataRow("OBSERVATION-DEADLINE-EXCEEDED")]
    [DataRow("OBSERVATION-LIMIT-INVALID")]
    [TestMethod]
    public void ObservationLimitException_AcceptsEachContractId(string errorId)
    {
        var ex = new ObservationLimitException(errorId, "test message");
        ex.ErrorId.Should().Be(errorId);
        ex.Message.Should().Be("test message");
    }

    [TestMethod]
    public void ObservationLimitException_UnknownErrorId_ThrowsArgumentException()
    {
        FluentActions.Invoking(() => new ObservationLimitException("UNKNOWN-ERROR-ID", "test message")).Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesDwdObservationSource()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IDwdObservationSource>().Should().BeOfType<DwdObservationSource>();
    }

    [TestMethod]
    public void DwdObservationSource_SelectionLimits_DoNotApply()
    {
        var endpoints = ValidEndpoints();
        endpoints.MaxEvidenceCandidates = 0;
        endpoints.SelectionDeadline = TimeSpan.Zero;
        FluentActions.Invoking(() => new DwdObservationSource(new DwdObservationOptions(), endpoints, _validator, _downloader.Object, _listReader.Object, _evidenceReader.Object, new CdcRequestComposer())).Should().NotThrow();
    }
}
