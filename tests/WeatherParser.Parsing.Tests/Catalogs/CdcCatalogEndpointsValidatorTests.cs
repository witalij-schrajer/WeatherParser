namespace WeatherParser.Parsing.Tests.Catalogs;

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

[TestClass]
public sealed class CdcCatalogEndpointsValidatorTests
{
    private ICdcCatalogEndpointsValidator _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new CdcCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator()));
    }

    private static CdcCatalogEndpoints Valid()
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

    private static CdcCatalogEndpoints ValidWith(Action<CdcCatalogEndpoints> change)
    {
        var options = Valid();
        change(options);
        return options;
    }

    private static void Set(CdcCatalogEndpoints options, string property, Uri? value)
    {
        typeof(CdcCatalogEndpoints).GetProperty(property)!.SetValue(options, value);
    }

    private void ShouldBeEndpointInvalid(CdcCatalogEndpoints o)
    {
        Action act = () => _subject.Validate(o);
        act.Should().Throw<ParserEndpointValidationException>().Which.ErrorId.Should().Be("ENDPOINT-INVALID");
    }

    private void ShouldBeLimitInvalid(CdcCatalogEndpoints o, string option)
    {
        Action act = () => _subject.Validate(o);
        var exception = act.Should().Throw<SelectionLimitException>().Which;
        exception.ErrorId.Should().Be("SELECTION-LIMIT-INVALID");
        exception.Message.Should().Contain(option);
    }

    [TestMethod]
    public void Validate_Defaults_Accepted()
    {
        var options = new CdcCatalogEndpoints();
        options.MaxEvidenceCandidates.Should().Be(100);
        options.SelectionDeadline.Should().Be(TimeSpan.FromSeconds(120));
        _subject.Validate(Valid());
    }

    [DataRow("AirTemperatureStationList")]
    [DataRow("AirTemperatureNowDirectory")]
    [DataRow("WindStationList")]
    [DataRow("WindNowDirectory")]
    [DataRow("PrecipitationStationList")]
    [DataRow("PrecipitationNowDirectory")]
    [DataRow("SolarStationList")]
    [DataRow("SolarNowDirectory")]
    [TestMethod]
    public void Validate_MissingOrRelativeEndpoint_ThrowsEndpointInvalid(string property)
    {
        var options = Valid();
        Set(options, property, null);
        ShouldBeEndpointInvalid(options);

        options = Valid();
        Set(options, property, new Uri("relative/", UriKind.Relative));
        ShouldBeEndpointInvalid(options);

        options = Valid();
        Set(options, property, new Uri("ftp://example.org/cdc/x/"));
        ShouldBeEndpointInvalid(options);
    }

    [DataRow("AirTemperatureNowDirectory")]
    [DataRow("WindNowDirectory")]
    [DataRow("PrecipitationNowDirectory")]
    [DataRow("SolarNowDirectory")]
    [TestMethod]
    public void Validate_InvalidDirectoryUri_TrailingSlashQueryFragment_Throws(string property)
    {
        var options = ValidWith(o => Set(o, property, new Uri("https://example.org/cdc/dir")));
        ShouldBeEndpointInvalid(options);

        options = ValidWith(o => Set(o, property, new Uri("https://example.org/cdc/dir/?a=1")));
        ShouldBeEndpointInvalid(options);

        options = ValidWith(o => Set(o, property, new Uri("https://example.org/cdc/dir/#part")));
        ShouldBeEndpointInvalid(options);
    }

    [TestMethod]
    public void Validate_StationListWithQuery_AcceptedUnchanged()
    {
        var options = ValidWith(o => o.WindStationList = new Uri("https://example.org/cdc/wind/now/list.txt?version=2"));
        _subject.Validate(options);
        options.WindStationList!.OriginalString.Should().Be("https://example.org/cdc/wind/now/list.txt?version=2");
    }

    [TestMethod]
    public void Validate_MaxEvidenceCandidates_Boundaries()
    {
        var options = ValidWith(o => o.MaxEvidenceCandidates = 0);
        ShouldBeLimitInvalid(options, "MaxEvidenceCandidates");

        options = ValidWith(o => o.MaxEvidenceCandidates = 10001);
        ShouldBeLimitInvalid(options, "MaxEvidenceCandidates");

        options = ValidWith(o => o.MaxEvidenceCandidates = 1);
        _subject.Validate(options);

        options = ValidWith(o => o.MaxEvidenceCandidates = 10000);
        _subject.Validate(options);
    }

    [TestMethod]
    public void Validate_SelectionDeadline_Boundaries()
    {
        var options = ValidWith(o => o.SelectionDeadline = TimeSpan.Zero);
        ShouldBeLimitInvalid(options, "SelectionDeadline");

        options = ValidWith(o => o.SelectionDeadline = TimeSpan.FromSeconds(-1));
        ShouldBeLimitInvalid(options, "SelectionDeadline");

        options = ValidWith(o => o.SelectionDeadline = Timeout.InfiniteTimeSpan);
        ShouldBeLimitInvalid(options, "SelectionDeadline");

        options = ValidWith(o => o.SelectionDeadline = TimeSpan.FromHours(1) + TimeSpan.FromTicks(1));
        ShouldBeLimitInvalid(options, "SelectionDeadline");

        options = ValidWith(o => o.SelectionDeadline = TimeSpan.FromTicks(1));
        _subject.Validate(options);

        options = ValidWith(o => o.SelectionDeadline = TimeSpan.FromHours(1));
        _subject.Validate(options);
    }

    [TestMethod]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        Action act = () => _subject.Validate(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesCdcCatalogEndpointsValidator()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<ICdcCatalogEndpointsValidator>().Should().BeOfType<CdcCatalogEndpointsValidator>();
    }

    [TestMethod]
    public void ValidateEndpoints_ValidEndpoints_Accepted()
    {
        FluentActions.Invoking(() => _subject.ValidateEndpoints(Valid())).Should().NotThrow();
    }

    [TestMethod]
    public void ValidateEndpoints_IgnoresSelectionLimits()
    {
        var options = ValidWith(o => { o.MaxEvidenceCandidates = 0; o.SelectionDeadline = TimeSpan.Zero; });
        FluentActions.Invoking(() => _subject.ValidateEndpoints(options)).Should().NotThrow();
        FluentActions.Invoking(() => _subject.Validate(options)).Should().Throw<SelectionLimitException>().Which.ErrorId.Should().Be(SelectionLimitException.LimitInvalidErrorId);
    }

    [TestMethod]
    public void ValidateEndpoints_RelativeUri_ThrowsEndpointInvalid()
    {
        var options = ValidWith(o => o.AirTemperatureStationList = new Uri("list.txt", UriKind.Relative));
        FluentActions.Invoking(() => _subject.ValidateEndpoints(options)).Should().Throw<ParserEndpointValidationException>().Which.ErrorId.Should().Be("ENDPOINT-INVALID");
    }

    [TestMethod]
    public void ValidateEndpoints_MissingEndpoint_ThrowsEndpointInvalid()
    {
        var options = ValidWith(o => o.SolarNowDirectory = null);
        FluentActions.Invoking(() => _subject.ValidateEndpoints(options)).Should().Throw<ParserEndpointValidationException>();
    }

    [TestMethod]
    public void ValidateEndpoints_DirectoryWithoutTrailingSlash_ThrowsEndpointInvalid()
    {
        var options = ValidWith(o => o.WindNowDirectory = new Uri("https://example.org/cdc/wind/now"));
        FluentActions.Invoking(() => _subject.ValidateEndpoints(options)).Should().Throw<ParserEndpointValidationException>();
    }

    [TestMethod]
    public void ValidateEndpoints_Null_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => _subject.ValidateEndpoints(null!)).Should().Throw<ArgumentNullException>();
    }
}
