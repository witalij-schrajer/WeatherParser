using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;

namespace WeatherParser.Cli.Tests.Configuration;

[TestClass]
public class ConfigurationValidatorServicesTests
{
    private ConfigurationValidator _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new ConfigurationValidator(new ReferenceStationResolver(), new CredentialProvider(new EnvironmentReader(), new FileModeReader()));
    }

    [TestCategory("T-P3-05")]
    [TestMethod]
    [DataRow(CliCommand.SelectDwd, OutputFormat.Json)]
    [DataRow(CliCommand.SelectDwd, OutputFormat.Mqtt)]
    [DataRow(CliCommand.SelectMosmix, OutputFormat.Json)]
    [DataRow(CliCommand.SelectMosmix, OutputFormat.Mqtt)]
    [DataRow(CliCommand.ParseDwd, OutputFormat.Json)]
    [DataRow(CliCommand.ParseDwd, OutputFormat.Mqtt)]
    [DataRow(CliCommand.ParseOstHessen, OutputFormat.Json)]
    [DataRow(CliCommand.ParseOstHessen, OutputFormat.Mqtt)]
    [DataRow(CliCommand.ParseMosmix, OutputFormat.Json)]
    [DataRow(CliCommand.ParseMosmix, OutputFormat.Mqtt)]
    public void Validate_ValidConfiguration_EveryCommand_NoException(CliCommand command, OutputFormat output)
    {
        var configuration = TestConfigurations.Valid();
        var request = TestConfigurations.Request(command, output);
        using var provider = TestConfigurations.Build(configuration, request);
        Action act = () => _subject.Validate(provider, configuration, request);
        act.Should().NotThrow();
    }

    [TestCategory("T-P3-05")]
    [TestMethod]
    public void Validate_ParseMosmixS_ValidConfiguration_NoException()
    {
        var request = new ParseMosmixRequest(
            new CommonOptions(OutputFormat.Json, null, null, false),
            MosmixProduct.MosmixS,
            "10637");
        using var provider = TestConfigurations.Build(TestConfigurations.Valid(), request);
        Action act = () => _subject.Validate(provider, TestConfigurations.Valid(), request);
        act.Should().NotThrow();
    }

    [TestCategory("T-P3-05")]
    [TestMethod]
    public void Validate_SelectMosmixWithRefresh_ValidConfiguration_NoException()
    {
        var request = new SelectMosmixRequest(
            new CommonOptions(OutputFormat.Json, null, null, false),
            new SearchParameterFactory().Create(50.5m, 9.7m, 300m, 50m, 200m),
            true);
        using var provider = TestConfigurations.Build(TestConfigurations.Valid(), request);
        Action act = () => _subject.Validate(provider, TestConfigurations.Valid(), request);
        act.Should().NotThrow();
    }

    [TestCategory("T-P3-05")]
    [TestMethod]
    public void Validate_InvalidEndpoint_ConfigurationInvalidWithLibraryId()
    {
        var configuration = new CliConfiguration
        {
            Dwd = new DwdSection { Catalog = new CdcCatalogEndpoints() },
            Mosmix = TestConfigurations.Valid().Mosmix,
            OsthessenWetter = TestConfigurations.Valid().OsthessenWetter,
            Mqtt = TestConfigurations.Valid().Mqtt
        };
        var request = TestConfigurations.Request(CliCommand.SelectDwd, OutputFormat.Json);
        using var provider = TestConfigurations.Build(configuration, request);
        Action act = () => _subject.Validate(provider, configuration, request);
        var ex = act.Should().Throw<CliException>().Which;
        ex.ErrorId.Should().Be("CLI-CONFIG-INVALID");
        ex.Message.Should().StartWith("ENDPOINT-INVALID: ");
    }

    [TestCategory("T-P3-05")]
    [TestMethod]
    public void Validate_InvalidLimit_ConfigurationInvalidWithLibraryId()
    {
        var configuration = TestConfigurations.Valid();
        configuration.Dwd!.Catalog!.MaxEvidenceCandidates = 0;
        var request = TestConfigurations.Request(CliCommand.SelectDwd, OutputFormat.Json);
        using var provider = TestConfigurations.Build(configuration, request);
        Action act = () => _subject.Validate(provider, configuration, request);
        var ex = act.Should().Throw<CliException>().Which;
        ex.ErrorId.Should().Be("CLI-CONFIG-INVALID");
        ex.Message.Should().StartWith("SELECTION-LIMIT-INVALID: ");
    }

    [TestCategory("T-P3-05")]
    [TestMethod]
    public void Validate_EvidenceCacheSettingsMissing_ConfigurationInvalid()
    {
        var configuration = TestConfigurations.Valid();
        configuration.Mosmix!.Catalog = new MosmixCatalogEndpoints
        {
            CatalogUri = new Uri("https://example.invalid/mosmix/stations.cfg"),
            MosmixLSingleStationsDirectory = new Uri("https://example.invalid/mosmix/single_stations/"),
            MosmixSAllStationsUri = new Uri("https://example.invalid/mosmix/MOSMIX_S_LATEST_240.kmz"),
            EvidenceCacheDirectory = Path.Combine(Path.GetTempPath(), "weatherparser-cli-test-evidence"),
            MosmixSEvidenceMaxAge = null
        };
        var request = TestConfigurations.Request(CliCommand.SelectMosmix, OutputFormat.Json);
        using var provider = TestConfigurations.Build(configuration, request);
        Action act = () => _subject.Validate(provider, configuration, request);
        var ex = act.Should().Throw<CliException>().Which;
        ex.ErrorId.Should().Be("CLI-CONFIG-INVALID");
        ex.Message.Should().StartWith("EVIDENCE-CACHE-SETTINGS-INVALID: ");
    }

    [TestCategory("T-P3-05")]
    [TestMethod]
    public void Validate_ObservationDeadlineZero_ConfigurationInvalid()
    {
        var configuration = TestConfigurations.Valid();
        configuration.Dwd!.Observation = new DwdObservationOptions { ObservationDeadline = TimeSpan.Zero };
        var request = TestConfigurations.Request(CliCommand.ParseDwd, OutputFormat.Json);
        using var provider = TestConfigurations.Build(configuration, request);
        Action act = () => _subject.Validate(provider, configuration, request);
        var ex = act.Should().Throw<CliException>().Which;
        ex.ErrorId.Should().Be("CLI-CONFIG-INVALID");
        ex.Message.Should().StartWith("OBSERVATION-LIMIT-INVALID: ");
    }

    [TestCategory("T-P3-05")]
    [TestMethod]
    public void Validate_OsthessenPageDeadlineZero_ConfigurationInvalid()
    {
        var configuration = TestConfigurations.Valid();
        configuration.OsthessenWetter!.PageDeadline = TimeSpan.Zero;
        var request = TestConfigurations.Request(CliCommand.ParseOstHessen, OutputFormat.Json);
        using var provider = TestConfigurations.Build(configuration, request);
        Action act = () => _subject.Validate(provider, configuration, request);
        var ex = act.Should().Throw<CliException>().Which;
        ex.ErrorId.Should().Be("CLI-CONFIG-INVALID");
        ex.Message.Should().StartWith("OSTHESSEN-OPTIONS-INVALID: ");
    }

    [TestCategory("T-P3-05")]
    [TestMethod]
    public void Validate_MosmixParseDeadlineZero_ConfigurationInvalid()
    {
        var configuration = TestConfigurations.Valid();
        configuration.Mosmix!.L = new MosmixLSection { ParseDeadline = TimeSpan.Zero };
        var request = TestConfigurations.Request(CliCommand.ParseMosmix, OutputFormat.Json);
        using var provider = TestConfigurations.Build(configuration, request);
        Action act = () => _subject.Validate(provider, configuration, request);
        var ex = act.Should().Throw<CliException>().Which;
        ex.ErrorId.Should().Be("CLI-CONFIG-INVALID");
        ex.Message.Should().StartWith("MOSMIX-OPTIONS-INVALID: ");
    }

    [TestCategory("T-P3-05")]
    [TestMethod]
    public void Validate_InvalidTopicTemplate_ConfigurationInvalidWithMqttOptionsInvalid()
    {
        var configuration = TestConfigurations.Valid();
        configuration.Mqtt!.Topics = new TopicsSection
        {
            Observation = new TopicSection { TopicTemplate = "haus/#/state" }
        };
        var request = TestConfigurations.Request(CliCommand.ParseDwd, OutputFormat.Mqtt);
        using var provider = TestConfigurations.Build(configuration, request);
        Action act = () => _subject.Validate(provider, configuration, request);
        var ex = act.Should().Throw<CliException>().Which;
        ex.ErrorId.Should().Be("CLI-CONFIG-INVALID");
        ex.Message.Should().StartWith("MQTT-OPTIONS-INVALID: ");
    }

    [TestCategory("T-P3-05")]
    [TestMethod]
    [DataRow(CliCommand.SelectDwd)]
    [DataRow(CliCommand.SelectMosmix)]
    [DataRow(CliCommand.ParseDwd)]
    [DataRow(CliCommand.ParseOstHessen)]
    [DataRow(CliCommand.ParseMosmix)]
    public void Validate_MqttMappingMissingForCommand_ConfigurationInvalid(CliCommand command)
    {
        var configuration = TestConfigurations.Valid();
        configuration.Mqtt!.Topics = new TopicsSection();
        var request = TestConfigurations.Request(command, OutputFormat.Mqtt);
        using var provider = TestConfigurations.Build(configuration, request);
        Action act = () => _subject.Validate(provider, configuration, request);
        var ex = act.Should().Throw<CliException>().Which;
        ex.ErrorId.Should().Be("CLI-CONFIG-INVALID");
        ex.Message.Should().StartWith("MQTT-MAPPING-MISSING: ");
    }

    [TestCategory("T-P3-05")]
    [TestMethod]
    [DataRow(CliCommand.SelectDwd, "CdcStationList")]
    [DataRow(CliCommand.SelectMosmix, "MosmixLocationList")]
    [DataRow(CliCommand.ParseDwd, "Observation")]
    [DataRow(CliCommand.ParseOstHessen, "Observation")]
    [DataRow(CliCommand.ParseMosmix, "Forecast")]
    public void Validate_OnlyRequiredMapping_Accepted(CliCommand command, string topicToKeep)
    {
        var configuration = TestConfigurations.Valid();
        var topics = configuration.Mqtt!.Topics!;
        topics.Observation = topicToKeep == "Observation" ? topics.Observation : null;
        topics.Forecast = topicToKeep == "Forecast" ? topics.Forecast : null;
        topics.CdcStationList = topicToKeep == "CdcStationList" ? topics.CdcStationList : null;
        topics.MosmixLocationList = topicToKeep == "MosmixLocationList" ? topics.MosmixLocationList : null;
        var request = TestConfigurations.Request(command, OutputFormat.Mqtt);
        using var provider = TestConfigurations.Build(configuration, request);
        Action act = () => _subject.Validate(provider, configuration, request);
        act.Should().NotThrow();
    }

    [TestCategory("T-P3-05")]
    [TestMethod]
    public void Validate_JsonMode_NoMqttSection_NoException()
    {
        var configuration = TestConfigurations.Valid();
        configuration.Mqtt = null;
        var request = TestConfigurations.Request(CliCommand.ParseDwd, OutputFormat.Json);
        using var provider = TestConfigurations.Build(configuration, request);
        Action act = () => _subject.Validate(provider, configuration, request);
        act.Should().NotThrow();
    }

    [TestCategory("T-P3-05")]
    [TestMethod]
    public void Validate_UnexpectedException_Propagates()
    {
        var mock = new Mock<IServiceProvider>(MockBehavior.Strict);
        mock.Setup(s => s.GetService(It.IsAny<Type>())).Throws(new InvalidOperationException("boom"));
        var configuration = TestConfigurations.Valid();
        var request = TestConfigurations.Request(CliCommand.ParseDwd, OutputFormat.Json);
        Action act = () => _subject.Validate(mock.Object, configuration, request);
        act.Should().Throw<InvalidOperationException>().WithMessage("boom");
        mock.VerifyAll();
    }
}
