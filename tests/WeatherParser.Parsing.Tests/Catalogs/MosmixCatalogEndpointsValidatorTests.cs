using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class MosmixCatalogEndpointsValidatorTests
{
    private MockRepository _mocks = null!;
    private IMosmixCatalogEndpointsValidator _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _subject = new MosmixCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator()));
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private static MosmixCatalogEndpoints Valid() => new()
    {
        CatalogUri = new Uri("https://example.org/mosmix/stations.cfg"),
        MosmixLSingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/"),
        MosmixSAllStationsUri = new Uri("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz"),
        EvidenceCacheDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-evidence-test"),
        MosmixSEvidenceMaxAge = TimeSpan.FromHours(6)
    };

    private static MosmixCatalogEndpoints ValidWith(Action<MosmixCatalogEndpoints> change)
    {
        var options = Valid();
        change(options);
        return options;
    }

    private void ShouldBeLimitInvalid(MosmixCatalogEndpoints o, string optionName)
    {
        var act = () => _subject.Validate(o);
        act.Should().Throw<SelectionLimitException>()
            .WithMessage($"*{optionName}*")
            .Which.ErrorId.Should().Be("SELECTION-LIMIT-INVALID");
    }

    private void ShouldBeSettingsInvalid(MosmixCatalogEndpoints o, string optionName)
    {
        var act = () => _subject.Validate(o);
        act.Should().Throw<EvidenceCacheException>()
            .WithMessage($"*{optionName}*")
            .Which.ErrorId.Should().Be("EVIDENCE-CACHE-SETTINGS-INVALID");
    }

    private void ShouldBeEndpointInvalid(MosmixCatalogEndpoints o)
    {
        var act = () => _subject.Validate(o);
        act.Should().Throw<ParserEndpointValidationException>();
    }

    [TestMethod]
    public void Validate_Defaults_Accepted()
    {
        var options = new MosmixCatalogEndpoints();
        options.MaxEvidenceCandidates.Should().Be(100);
        options.SelectionDeadline.Should().Be(TimeSpan.FromSeconds(120));
        options.MosmixSRefreshDeadline.Should().Be(TimeSpan.FromMinutes(10));
        options.MosmixSEvidenceMaxAge.Should().BeNull();

        _subject.Validate(Valid());
    }

    [TestMethod]
    public void Validate_MaxEvidenceCandidates_Boundaries()
    {
        ShouldBeLimitInvalid(ValidWith(o => o.MaxEvidenceCandidates = 0), "MaxEvidenceCandidates");
        ShouldBeLimitInvalid(ValidWith(o => o.MaxEvidenceCandidates = 10001), "MaxEvidenceCandidates");

        var act1 = () => _subject.Validate(ValidWith(o => o.MaxEvidenceCandidates = 1));
        act1.Should().NotThrow();

        var act2 = () => _subject.Validate(ValidWith(o => o.MaxEvidenceCandidates = 10000));
        act2.Should().NotThrow();
    }

    [TestMethod]
    public void Validate_SelectionDeadline_Boundaries()
    {
        ShouldBeLimitInvalid(ValidWith(o => o.SelectionDeadline = TimeSpan.Zero), "SelectionDeadline");
        ShouldBeLimitInvalid(ValidWith(o => o.SelectionDeadline = TimeSpan.FromSeconds(-1)), "SelectionDeadline");
        ShouldBeLimitInvalid(ValidWith(o => o.SelectionDeadline = Timeout.InfiniteTimeSpan), "SelectionDeadline");
        ShouldBeLimitInvalid(ValidWith(o => o.SelectionDeadline = TimeSpan.FromHours(1) + TimeSpan.FromTicks(1)), "SelectionDeadline");

        var act1 = () => _subject.Validate(ValidWith(o => o.SelectionDeadline = TimeSpan.FromTicks(1)));
        act1.Should().NotThrow();

        var act2 = () => _subject.Validate(ValidWith(o => o.SelectionDeadline = TimeSpan.FromHours(1)));
        act2.Should().NotThrow();
    }

    [TestMethod]
    public void Validate_MosmixSEvidenceMaxAge_Boundaries()
    {
        ShouldBeSettingsInvalid(ValidWith(o => o.MosmixSEvidenceMaxAge = null), "MosmixSEvidenceMaxAge");
        ShouldBeSettingsInvalid(ValidWith(o => o.MosmixSEvidenceMaxAge = TimeSpan.FromMinutes(1) - TimeSpan.FromTicks(1)), "MosmixSEvidenceMaxAge");
        ShouldBeSettingsInvalid(ValidWith(o => o.MosmixSEvidenceMaxAge = TimeSpan.FromDays(365) + TimeSpan.FromTicks(1)), "MosmixSEvidenceMaxAge");

        var act1 = () => _subject.Validate(ValidWith(o => o.MosmixSEvidenceMaxAge = TimeSpan.FromMinutes(1)));
        act1.Should().NotThrow();

        var act2 = () => _subject.Validate(ValidWith(o => o.MosmixSEvidenceMaxAge = TimeSpan.FromDays(365)));
        act2.Should().NotThrow();
    }

    [TestMethod]
    public void Validate_MosmixSRefreshDeadline_Boundaries()
    {
        ShouldBeSettingsInvalid(ValidWith(o => o.MosmixSRefreshDeadline = TimeSpan.Zero), "MosmixSRefreshDeadline");
        ShouldBeSettingsInvalid(ValidWith(o => o.MosmixSRefreshDeadline = TimeSpan.FromSeconds(-1)), "MosmixSRefreshDeadline");
        ShouldBeSettingsInvalid(ValidWith(o => o.MosmixSRefreshDeadline = Timeout.InfiniteTimeSpan), "MosmixSRefreshDeadline");
        ShouldBeSettingsInvalid(ValidWith(o => o.MosmixSRefreshDeadline = TimeSpan.FromHours(1) + TimeSpan.FromTicks(1)), "MosmixSRefreshDeadline");

        var act1 = () => _subject.Validate(ValidWith(o => o.MosmixSRefreshDeadline = TimeSpan.FromTicks(1)));
        act1.Should().NotThrow();

        var act2 = () => _subject.Validate(ValidWith(o => o.MosmixSRefreshDeadline = TimeSpan.FromHours(1)));
        act2.Should().NotThrow();
    }

    [TestMethod]
    public void Validate_MosmixSAllStationsUriAndCacheDirectory_Invalid()
    {
        ShouldBeSettingsInvalid(ValidWith(o => o.MosmixSAllStationsUri = null), "MosmixSAllStationsUri");
        ShouldBeSettingsInvalid(ValidWith(o => o.MosmixSAllStationsUri = new Uri("mosmix/MOSMIX_S_LATEST_240.kmz", UriKind.Relative)), "MosmixSAllStationsUri");
        ShouldBeSettingsInvalid(ValidWith(o => o.MosmixSAllStationsUri = new Uri("ftp://example.org/mosmix/MOSMIX_S_LATEST_240.kmz")), "MosmixSAllStationsUri");
        ShouldBeSettingsInvalid(ValidWith(o => o.MosmixSAllStationsUri = new Uri("https://example.org/mosmix/") ), "MosmixSAllStationsUri");

        ShouldBeSettingsInvalid(ValidWith(o => o.EvidenceCacheDirectory = null), "EvidenceCacheDirectory");
        ShouldBeSettingsInvalid(ValidWith(o => o.EvidenceCacheDirectory = ""), "EvidenceCacheDirectory");
        ShouldBeSettingsInvalid(ValidWith(o => o.EvidenceCacheDirectory = "   "), "EvidenceCacheDirectory");
        ShouldBeSettingsInvalid(ValidWith(o => o.EvidenceCacheDirectory = "relative/cache"), "EvidenceCacheDirectory");
    }

    [TestMethod]
    public void Validate_DirectoryAndCatalogUriRules()
    {
        ShouldBeEndpointInvalid(ValidWith(o => o.MosmixLSingleStationsDirectory = null));
        ShouldBeEndpointInvalid(ValidWith(o => o.MosmixLSingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations")));
        ShouldBeEndpointInvalid(ValidWith(o => o.MosmixLSingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/?a=1")));
        ShouldBeEndpointInvalid(ValidWith(o => o.MosmixLSingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/#part")));
        ShouldBeEndpointInvalid(ValidWith(o => o.MosmixLSingleStationsDirectory = new Uri("ftp://example.org/mosmix/single_stations/")));
        ShouldBeEndpointInvalid(ValidWith(o => o.MosmixLSingleStationsDirectory = new Uri("single_stations/", UriKind.Relative)));

        ShouldBeEndpointInvalid(ValidWith(o => o.CatalogUri = null));
        ShouldBeEndpointInvalid(ValidWith(o => o.CatalogUri = new Uri("stations.cfg", UriKind.Relative)));
        ShouldBeEndpointInvalid(ValidWith(o => o.CatalogUri = new Uri("ftp://example.org/mosmix/stations.cfg")));

        var options = ValidWith(o => o.CatalogUri = new Uri("https://example.org/mosmix/stations.cfg?version=2"));
        _subject.Validate(options);
        options.CatalogUri!.OriginalString.Should().Be("https://example.org/mosmix/stations.cfg?version=2");
    }

    [TestMethod]
    public void Validate_NoNetworkOrFileSystemAccess()
    {
        var mockEndpointValidator = _mocks.Create<IParserEndpointValidator>();
        mockEndpointValidator.Setup(v => v.ValidateAndMap(It.IsAny<IReadOnlyCollection<ParserEndpointDefinition>>()))
            .Returns(new Dictionary<string, ParserEndpointDefinition>());

        var subject = new MosmixCatalogEndpointsValidator(mockEndpointValidator.Object);
        var options = Valid();
        options.EvidenceCacheDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-missing-" + Guid.NewGuid().ToString("N"));

        var act = () => subject.Validate(options);
        act.Should().NotThrow();

        Directory.Exists(options.EvidenceCacheDirectory).Should().BeFalse();
    }

    [TestMethod]
    public void Validate_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => _subject.Validate(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesMosmixCatalogEndpointsValidator()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var validator = provider.GetRequiredService<IMosmixCatalogEndpointsValidator>();
        validator.Should().BeOfType<MosmixCatalogEndpointsValidator>();
    }
}
