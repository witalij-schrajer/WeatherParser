using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Mqtt;

[TestClass]
public sealed class MqttTopicOptionsValidatorTests
{
    private IMqttTopicOptionsValidator _validator = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _validator = new MqttTopicOptionsValidator(new MqttTopicTemplateParser());
    }

    [TestMethod]
    public void Validate_EmptyOptions_ValidWithoutMappings()
    {
        var options = new MqttTopicOptions();
        var result = _validator.Validate(options);

        result.GetMapping(MqttMessageType.Observation).Should().BeNull();
        result.GetMapping(MqttMessageType.Forecast).Should().BeNull();
        result.GetMapping(MqttMessageType.CdcStationList).Should().BeNull();
        result.GetMapping(MqttMessageType.MosmixLocationList).Should().BeNull();
        result.MaxPayloadBytes.Should().BeNull();
    }

    [TestMethod]
    public void Validate_ObservationAndForecastDefaults_QosOneRetained()
    {
        var options = Options(
            (MqttMessageType.Observation, "o/{source}"),
            (MqttMessageType.Forecast, "f/{source}")
        );
        var result = _validator.Validate(options);

        var obs = result.GetMapping(MqttMessageType.Observation);
        obs.Should().NotBeNull();
        obs!.Template.Text.Should().Be("o/{source}");
        obs.Template.MessageType.Should().Be(MqttMessageType.Observation);
        obs.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        obs.Retain.Should().BeTrue();

        var fc = result.GetMapping(MqttMessageType.Forecast);
        fc.Should().NotBeNull();
        fc!.Template.Text.Should().Be("f/{source}");
        fc.Template.MessageType.Should().Be(MqttMessageType.Forecast);
        fc.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        fc.Retain.Should().BeTrue();
    }

    [TestMethod]
    public void Validate_StationListDefaults_QosOneNotRetained()
    {
        var options = Options(
            (MqttMessageType.CdcStationList, "s/dwd"),
            (MqttMessageType.MosmixLocationList, "s/mosmix")
        );
        var result = _validator.Validate(options);

        var cdc = result.GetMapping(MqttMessageType.CdcStationList);
        cdc.Should().NotBeNull();
        cdc!.Template.Text.Should().Be("s/dwd");
        cdc.Template.MessageType.Should().Be(MqttMessageType.CdcStationList);
        cdc.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        cdc.Retain.Should().BeFalse();

        var mosmix = result.GetMapping(MqttMessageType.MosmixLocationList);
        mosmix.Should().NotBeNull();
        mosmix!.Template.Text.Should().Be("s/mosmix");
        mosmix.Template.MessageType.Should().Be(MqttMessageType.MosmixLocationList);
        mosmix.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        mosmix.Retain.Should().BeFalse();
    }

    [TestMethod]
    public void Validate_Overrides_Kept()
    {
        var options = new MqttTopicOptions
        {
            Mappings = new Dictionary<MqttMessageType, MqttTopicMapping>
            {
                [MqttMessageType.Observation] = new() { TopicTemplate = "o/{source}", QualityOfService = MqttQualityOfService.AtMostOnce, Retain = false },
                [MqttMessageType.CdcStationList] = new() { TopicTemplate = "s/dwd", QualityOfService = MqttQualityOfService.ExactlyOnce, Retain = true }
            }
        };
        var result = _validator.Validate(options);

        var obs = result.GetMapping(MqttMessageType.Observation);
        obs.Should().NotBeNull();
        obs!.Template.Text.Should().Be("o/{source}");
        obs.Template.MessageType.Should().Be(MqttMessageType.Observation);
        obs.QualityOfService.Should().Be(MqttQualityOfService.AtMostOnce);
        obs.Retain.Should().BeFalse();

        var cdc = result.GetMapping(MqttMessageType.CdcStationList);
        cdc.Should().NotBeNull();
        cdc!.Template.Text.Should().Be("s/dwd");
        cdc.Template.MessageType.Should().Be(MqttMessageType.CdcStationList);
        cdc.QualityOfService.Should().Be(MqttQualityOfService.ExactlyOnce);
        cdc.Retain.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(3)]
    public void Validate_QosOutsideRange_ThrowsOptionsInvalid(int value)
    {
        var options = new MqttTopicOptions
        {
            Mappings = new Dictionary<MqttMessageType, MqttTopicMapping>
            {
                [MqttMessageType.Observation] = new() { TopicTemplate = "o/{source}", QualityOfService = (MqttQualityOfService)value }
            }
        };
        var act = () => _validator.Validate(options);
        ShouldBeOptionsInvalid(act, MqttMessageType.Observation);
    }

    [TestMethod]
    public void Validate_NullOptionsOrMapping_Throws()
    {
        ShouldThrowArgumentNullException(() => _validator.Validate(null!));

        var options1 = new MqttTopicOptions { Mappings = null };
        var act1 = () => _validator.Validate(options1);
        ShouldBeOptionsInvalid(act1, null);

        var options2 = new MqttTopicOptions
        {
            Mappings = new Dictionary<MqttMessageType, MqttTopicMapping>
            {
                [MqttMessageType.Forecast] = null!
            }
        };
        var act2 = () => _validator.Validate(options2);
        ShouldBeOptionsInvalid(act2, MqttMessageType.Forecast);

        var options3 = new MqttTopicOptions
        {
            Mappings = new Dictionary<MqttMessageType, MqttTopicMapping>
            {
                [MqttMessageType.Observation] = new() { TopicTemplate = null }
            }
        };
        var act3 = () => _validator.Validate(options3);
        ShouldBeOptionsInvalid(act3, MqttMessageType.Observation);

        var options4 = new MqttTopicOptions
        {
            Mappings = new Dictionary<MqttMessageType, MqttTopicMapping>
            {
                [MqttMessageType.Observation] = new() { TopicTemplate = "" }
            }
        };
        var act4 = () => _validator.Validate(options4);
        ShouldBeOptionsInvalid(act4, MqttMessageType.Observation);

        var options5 = new MqttTopicOptions
        {
            Mappings = new Dictionary<MqttMessageType, MqttTopicMapping>
            {
                [(MqttMessageType)9] = new() { TopicTemplate = "x" }
            }
        };
        var act5 = () => _validator.Validate(options5);
        ShouldBeOptionsInvalid(act5, null);
    }

    [TestMethod]
    public void Validate_SnapshotIgnoresLaterChanges()
    {
        var options = new MqttTopicOptions
        {
            Mappings = new Dictionary<MqttMessageType, MqttTopicMapping>
            {
                [MqttMessageType.Observation] = new() { TopicTemplate = "o/{source}" }
            }
        };
        var result = _validator.Validate(options);

        options.Mappings![MqttMessageType.Observation]!.TopicTemplate = "x/{source}";
        options.Mappings![MqttMessageType.Observation]!.QualityOfService = MqttQualityOfService.AtMostOnce;
        options.Mappings[MqttMessageType.Forecast] = new() { TopicTemplate = "f/{source}" };
        options.MaxPayloadBytes = 5;

        var obs = result.GetMapping(MqttMessageType.Observation);
        obs.Should().NotBeNull();
        obs!.Template.Text.Should().Be("o/{source}");
        obs.Template.MessageType.Should().Be(MqttMessageType.Observation);
        obs.QualityOfService.Should().Be(MqttQualityOfService.AtLeastOnce);
        obs.Retain.Should().BeTrue();

        result.GetMapping(MqttMessageType.Forecast).Should().BeNull();
        result.MaxPayloadBytes.Should().BeNull();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesOptionsValidator()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<IMqttTopicOptionsValidator>().Should().BeOfType<MqttTopicOptionsValidator>();
    }

    [TestMethod]
    public void Validate_InvalidTemplate_ThrowsOptionsInvalidWithMessageType()
    {
        var options = Options(
            (MqttMessageType.Forecast, "f/{station_id}")
        );
        var act = () => _validator.Validate(options);
        ShouldBeOptionsInvalid(act, MqttMessageType.Forecast);
    }

    [TestMethod]
    [DataRow("weather/obs")]
    [DataRow("weather/{provider}")]
    [DataRow("weather/{station_id}")]
    public void Validate_ObservationWithoutDistinguishingPlaceholder_Throws(string template)
    {
        var options = Options(
            (MqttMessageType.Observation, template)
        );
        var act = () => _validator.Validate(options);
        ShouldBeOptionsInvalid(act, MqttMessageType.Observation);
    }

    [TestMethod]
    [DataRow("o/{source}/state")]
    [DataRow("{source}")]
    public void Validate_ObservationWithSource_Accepted(string template)
    {
        var options = Options(
            (MqttMessageType.Observation, template)
        );
        var result = _validator.Validate(options);
        result.GetMapping(MqttMessageType.Observation).Should().NotBeNull();
    }

    [TestMethod]
    [DataRow("o/x{source}")]
    [DataRow("o/{source}-state")]
    [DataRow("o/{station_id}{source}{provider}")]
    public void Validate_SourceNotWholeLevel_Throws(string template)
    {
        var options = Options(
            (MqttMessageType.Observation, template)
        );
        var act = () => _validator.Validate(options);
        ShouldBeOptionsInvalid(act, MqttMessageType.Observation);
    }

    [TestMethod]
    [DataRow("o/{provider}/{source}")]
    [DataRow("o/{source}/x{provider}")]
    public void Validate_ExtraPlaceholdersInOtherLevels_Accepted(string template)
    {
        var options = Options(
            (MqttMessageType.Observation, template)
        );
        var result = _validator.Validate(options);
        result.GetMapping(MqttMessageType.Observation).Should().NotBeNull();
    }

    [TestMethod]
    [DataRow("o/{provider}/{station_id}")]
    [DataRow("o/{station_id}/x/{provider}")]
    public void Validate_ObservationWithProviderAndStationId_Accepted(string template)
    {
        var options = Options(
            (MqttMessageType.Observation, template)
        );
        var result = _validator.Validate(options);
        result.GetMapping(MqttMessageType.Observation).Should().NotBeNull();
    }

    [TestMethod]
    [DataRow("weather/{mosmix_id}")]
    [DataRow("weather/{product}")]
    [DataRow("weather/{provider}/{mosmix_id}")]
    public void Validate_ForecastWithoutProductAndId_Throws(string template)
    {
        var options = Options(
            (MqttMessageType.Forecast, template)
        );
        var act = () => _validator.Validate(options);
        ShouldBeOptionsInvalid(act, MqttMessageType.Forecast);
    }

    [TestMethod]
    public void Validate_ForecastWithSource_Accepted()
    {
        var options = Options(
            (MqttMessageType.Forecast, "f/{source}/state")
        );
        var result = _validator.Validate(options);
        result.GetMapping(MqttMessageType.Forecast).Should().NotBeNull();
    }

    [TestMethod]
    public void Validate_ForecastWithProductAndMosmixId_Accepted()
    {
        var options = Options(
            (MqttMessageType.Forecast, "f/{product}/{mosmix_id}")
        );
        var result = _validator.Validate(options);
        result.GetMapping(MqttMessageType.Forecast).Should().NotBeNull();
    }

    [TestMethod]
    [DataRow("o/{provider}-{station_id}", MqttMessageType.Observation)]
    [DataRow("o/{provider}/{station_id}x", MqttMessageType.Observation)]
    [DataRow("f/{product}{mosmix_id}", MqttMessageType.Forecast)]
    [DataRow("f/{product}/x{mosmix_id}", MqttMessageType.Forecast)]
    public void Validate_PairPlaceholdersNotWholeLevels_Throws(string template, MqttMessageType type)
    {
        var options = Options(
            (type, template)
        );
        var act = () => _validator.Validate(options);
        ShouldBeOptionsInvalid(act, type);
    }

    [TestMethod]
    public void Validate_IdenticalTemplatesForTwoTypes_Throws()
    {
        var options = Options(
            (MqttMessageType.Observation, "w/{source}"),
            (MqttMessageType.Forecast, "w/{source}")
        );
        var act = () => _validator.Validate(options);
        ShouldBeOptionsInvalid(act, null);
    }

    [TestMethod]
    [DataRow("weather/{source}", "weather/{product}/{mosmix_id}")]
    [DataRow("w/{source}", "w/{source}/x")]
    public void Validate_DifferentTemplatesThatCanCollide_Throws(string obsTemplate, string fcTemplate)
    {
        var options = Options(
            (MqttMessageType.Observation, obsTemplate),
            (MqttMessageType.Forecast, fcTemplate)
        );
        var act = () => _validator.Validate(options);
        ShouldBeOptionsInvalid(act, null);
    }

    [TestMethod]
    public void Validate_LiteralPrefixIsPrefixOfAnother_Throws()
    {
        var options = Options(
            (MqttMessageType.CdcStationList, "a/b"),
            (MqttMessageType.Observation, "a/b/{source}")
        );
        var act = () => _validator.Validate(options);
        ShouldBeOptionsInvalid(act, null);
    }

    [TestMethod]
    public void Validate_EmptyPrefixWithAnotherMapping_Throws()
    {
        var options = Options(
            (MqttMessageType.Observation, "{source}/o"),
            (MqttMessageType.Forecast, "f/{source}")
        );
        var act = () => _validator.Validate(options);
        ShouldBeOptionsInvalid(act, null);
    }

    [TestMethod]
    public void Validate_DivergingPrefixes_Accepted()
    {
        var options = Options(
            (MqttMessageType.Observation, "o/{source}"),
            (MqttMessageType.Forecast, "f/{source}"),
            (MqttMessageType.CdcStationList, "s/dwd"),
            (MqttMessageType.MosmixLocationList, "s/mosmix")
        );
        var result = _validator.Validate(options);
        result.GetMapping(MqttMessageType.Observation).Should().NotBeNull();
        result.GetMapping(MqttMessageType.Forecast).Should().NotBeNull();
        result.GetMapping(MqttMessageType.CdcStationList).Should().NotBeNull();
        result.GetMapping(MqttMessageType.MosmixLocationList).Should().NotBeNull();
    }

    [TestMethod]
    public void Validate_ExampleConfiguration_Accepted()
    {
        var options = Options(
            (MqttMessageType.Observation, "haus/weather/measurements/{source}/state"),
            (MqttMessageType.Forecast, "haus/weather/forecasts/{source}/state"),
            (MqttMessageType.CdcStationList, "haus/weather/stations/dwd/selection"),
            (MqttMessageType.MosmixLocationList, "haus/weather/stations/mosmix/selection")
        );
        var result = _validator.Validate(options);
        result.GetMapping(MqttMessageType.Observation)!.Template.Text.Should().Be("haus/weather/measurements/{source}/state");
        result.GetMapping(MqttMessageType.Forecast)!.Template.Text.Should().Be("haus/weather/forecasts/{source}/state");
        result.GetMapping(MqttMessageType.CdcStationList)!.Template.Text.Should().Be("haus/weather/stations/dwd/selection");
        result.GetMapping(MqttMessageType.MosmixLocationList)!.Template.Text.Should().Be("haus/weather/stations/mosmix/selection");
    }

    private static IEnumerable<object[]> AcceptedConfigurations()
    {
        yield return new object[]
        {
            "examples",
            new (MqttMessageType Type, string Template)[]
            {
                (MqttMessageType.Observation, "haus/weather/measurements/{source}/state"),
                (MqttMessageType.Forecast, "haus/weather/forecasts/{source}/state"),
                (MqttMessageType.CdcStationList, "haus/weather/stations/dwd/selection"),
                (MqttMessageType.MosmixLocationList, "haus/weather/stations/mosmix/selection")
            }
        };
        yield return new object[]
        {
            "pairs",
            new (MqttMessageType Type, string Template)[]
            {
                (MqttMessageType.Observation, "o/{provider}/{station_id}"),
                (MqttMessageType.Forecast, "f/{product}/{mosmix_id}"),
                (MqttMessageType.CdcStationList, "c/{provider}"),
                (MqttMessageType.MosmixLocationList, "m/{provider}")
            }
        };
        yield return new object[]
        {
            "diverging",
            new (MqttMessageType Type, string Template)[]
            {
                (MqttMessageType.Observation, "o/{source}"),
                (MqttMessageType.Forecast, "f/{source}"),
                (MqttMessageType.CdcStationList, "s/dwd"),
                (MqttMessageType.MosmixLocationList, "s/mosmix")
            }
        };
        yield return new object[]
        {
            "source-state",
            new (MqttMessageType Type, string Template)[]
            {
                (MqttMessageType.Observation, "o/{source}/state")
            }
        };
        yield return new object[]
        {
            "source-only",
            new (MqttMessageType Type, string Template)[]
            {
                (MqttMessageType.Observation, "{source}")
            }
        };
        yield return new object[]
        {
            "provider-then-source",
            new (MqttMessageType Type, string Template)[]
            {
                (MqttMessageType.Observation, "o/{provider}/{source}")
            }
        };
        yield return new object[]
        {
            "source-then-provider",
            new (MqttMessageType Type, string Template)[]
            {
                (MqttMessageType.Observation, "o/{source}/x{provider}")
            }
        };
        yield return new object[]
        {
            "reordered-pair",
            new (MqttMessageType Type, string Template)[]
            {
                (MqttMessageType.Observation, "o/{station_id}/x/{provider}")
            }
        };
        yield return new object[]
        {
            "repeated-source",
            new (MqttMessageType Type, string Template)[]
            {
                (MqttMessageType.Observation, "o/x{source}/{source}")
            }
        };
        yield return new object[]
        {
            "forecast-source-state",
            new (MqttMessageType Type, string Template)[]
            {
                (MqttMessageType.Forecast, "f/{source}/state")
            }
        };
        yield return new object[]
        {
            "forecast-pair",
            new (MqttMessageType Type, string Template)[]
            {
                (MqttMessageType.Forecast, "f/{product}/{mosmix_id}")
            }
        };
    }

    [TestMethod]
    [DynamicData(nameof(AcceptedConfigurations))]
    public void Validate_AcceptedConfigurations_ResolveDistinctTopics(string name, (MqttMessageType Type, string Template)[] mappings)
    {
        var result = _validator.Validate(Options(mappings));
        var topicValidator = new MqttTopicValidator();

        var topicsPerType = new Dictionary<MqttMessageType, List<string>>();

        foreach (var type in Enum.GetValues<MqttMessageType>())
        {
            if (result.GetMapping(type) is not null)
            {
                topicsPerType[type] = new List<string>();
            }
        }

        if (topicsPerType.TryGetValue(MqttMessageType.Observation, out var obsTopics))
        {
            foreach (var provider in new[] { "a", "ab", "b", "dwd" })
            {
                foreach (var stationId in new[] { "1", "12", "2", "a" })
                {
                    var values = new Dictionary<string, string>
                    {
                        ["source"] = $"{provider}:{stationId}",
                        ["provider"] = provider,
                        ["station_id"] = stationId
                    };

                    foreach (var placeholder in new[] { "source", "provider", "station_id" })
                    {
                        topicValidator.ValidatePlaceholderValue(placeholder, values[placeholder]);
                    }

                    var topic = Resolve(result.GetMapping(MqttMessageType.Observation)!.Template, values);
                    topicValidator.ValidateTopic(topic);
                    obsTopics.Add(topic);
                }
            }
        }

        if (topicsPerType.TryGetValue(MqttMessageType.Forecast, out var fcTopics))
        {
            foreach (var product in new[] { "MOSMIX_L", "MOSMIX_S" })
            {
                foreach (var id in new[] { "P1", "P12", "1" })
                {
                    var source = (product == "MOSMIX_L" ? "dwd-mosmix-l:" : "dwd-mosmix-s:") + id;
                    var values = new Dictionary<string, string>
                    {
                        ["source"] = source,
                        ["provider"] = source.Substring(0, source.IndexOf(':')),
                        ["mosmix_id"] = id,
                        ["product"] = product
                    };

                    foreach (var placeholder in new[] { "source", "provider", "mosmix_id", "product" })
                    {
                        topicValidator.ValidatePlaceholderValue(placeholder, values[placeholder]);
                    }

                    var topic = Resolve(result.GetMapping(MqttMessageType.Forecast)!.Template, values);
                    topicValidator.ValidateTopic(topic);
                    fcTopics.Add(topic);
                }
            }
        }

        if (topicsPerType.TryGetValue(MqttMessageType.CdcStationList, out var cdcTopics))
        {
            foreach (var provider in new[] { "dwd", "x" })
            {
                var values = new Dictionary<string, string>
                {
                    ["provider"] = provider
                };

                topicValidator.ValidatePlaceholderValue("provider", values["provider"]);
                var topic = Resolve(result.GetMapping(MqttMessageType.CdcStationList)!.Template, values);
                topicValidator.ValidateTopic(topic);
                cdcTopics.Add(topic);
            }
        }

        if (topicsPerType.TryGetValue(MqttMessageType.MosmixLocationList, out var mosmixTopics))
        {
            foreach (var provider in new[] { "dwd", "x" })
            {
                var values = new Dictionary<string, string>
                {
                    ["provider"] = provider
                };

                topicValidator.ValidatePlaceholderValue("provider", values["provider"]);
                var topic = Resolve(result.GetMapping(MqttMessageType.MosmixLocationList)!.Template, values);
                topicValidator.ValidateTopic(topic);
                mosmixTopics.Add(topic);
            }
        }

        if (topicsPerType.TryGetValue(MqttMessageType.Observation, out var obs))
        {
            obs.Should().OnlyHaveUniqueItems(because: name);
        }

        if (topicsPerType.TryGetValue(MqttMessageType.Forecast, out var fc))
        {
            fc.Should().OnlyHaveUniqueItems(because: name);
        }

        foreach (var type1 in Enum.GetValues<MqttMessageType>())
        {
            if (topicsPerType.TryGetValue(type1, out var topics1) && topics1.Count > 0)
            {
                foreach (var type2 in Enum.GetValues<MqttMessageType>())
                {
                    if (type1 != type2 && topicsPerType.TryGetValue(type2, out var topics2) && topics2.Count > 0)
                    {
                        topics1.Intersect(topics2).Should().BeEmpty(because: $"{name}: {type1} and {type2} should not have intersecting topics");
                    }
                }
            }
        }
    }

    [TestMethod]
    public void GetMapping_UndefinedMessageType_ReturnsNull()
    {
        var mappings = new (MqttMessageType Type, string Template)[]
        {
            (MqttMessageType.Observation, "o/{source}"),
            (MqttMessageType.Forecast, "f/{source}"),
            (MqttMessageType.CdcStationList, "s/dwd"),
            (MqttMessageType.MosmixLocationList, "s/mosmix")
        };

        var result = _validator.Validate(Options(mappings));
        result.GetMapping((MqttMessageType)9).Should().BeNull();
    }

    [TestMethod]
    public void Validate_FailureMessages_NameTypeAndRuleNotTemplate()
    {
        var options = Options(
            (MqttMessageType.Observation, "secret-marker/{provider}")
        );
        var act = () => _validator.Validate(options);
        var exception = act.Should().Throw<MqttMessageException>().Which;
        exception.Message.Should().Contain("Observation").And.Contain("{source}").And.NotContain("secret-marker");

        options = Options(
            (MqttMessageType.Observation, "secret-marker/{source}"),
            (MqttMessageType.Forecast, "secret-marker/{source}/x")
        );
        act = () => _validator.Validate(options);
        exception = act.Should().Throw<MqttMessageException>().Which;
        exception.Message.Should().Contain("Observation").And.Contain("Forecast").And.Contain("literal prefixes").And.NotContain("secret-marker");

        options = Options(
            (MqttMessageType.Forecast, null!)
        );
        act = () => _validator.Validate(options);
        exception = act.Should().Throw<MqttMessageException>().Which;
        exception.Message.Should().Contain("Forecast").And.Contain("no topic template");

        options = new MqttTopicOptions { MaxPayloadBytes = 0 };
        act = () => _validator.Validate(options);
        exception = act.Should().Throw<MqttMessageException>().Which;
        exception.Message.Should().Contain("MaxPayloadBytes").And.Contain("268435450");
    }

    [TestMethod]
    public void Validate_MaxPayloadBytesNull_MeansProtocolBound()
    {
        var options = new MqttTopicOptions { MaxPayloadBytes = null };
        var result = _validator.Validate(options);
        result.MaxPayloadBytes.Should().BeNull();
    }

    [TestMethod]
    [DataRow(268435450)]
    [DataRow(1)]
    public void Validate_MaxPayloadBytesAtProtocolLimit_Accepted(int value)
    {
        var options = new MqttTopicOptions { MaxPayloadBytes = value };
        var result = _validator.Validate(options);
        result.MaxPayloadBytes.Should().Be(value);
    }

    [TestMethod]
    [DataRow(268435451)]
    [DataRow(int.MaxValue)]
    public void Validate_MaxPayloadBytesAbove_ThrowsOptionsInvalid(int value)
    {
        var options = new MqttTopicOptions { MaxPayloadBytes = value };
        var act = () => _validator.Validate(options);
        ShouldBeOptionsInvalid(act, null);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void Validate_MaxPayloadBytesZeroOrNegative_ThrowsOptionsInvalid(int value)
    {
        var options = new MqttTopicOptions { MaxPayloadBytes = value };
        var act = () => _validator.Validate(options);
        ShouldBeOptionsInvalid(act, null);
    }

    [TestMethod]
    public void Validate_PlaceholderAlsoInOtherLevel_WholeLevelOccurrenceCounts()
    {
        var options = Options(
            (MqttMessageType.Observation, "o/x{source}/{source}")
        );
        var result = _validator.Validate(options);
        result.GetMapping(MqttMessageType.Observation).Should().NotBeNull();

        var invalid = Options((MqttMessageType.Observation, "o/x{source}/y{source}"));
        var act = () => _validator.Validate(invalid);
        ShouldBeOptionsInvalid(act, MqttMessageType.Observation);
    }

    private static void ShouldThrowArgumentNullException(Action act)
    {
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    private static void ShouldBeOptionsInvalid(Func<ValidatedMqttTopicConfiguration> act, MqttMessageType? type)
    {
        var exception = act.Should().Throw<MqttMessageException>().Which;
        exception.ErrorId.Should().Be(MqttMessageErrorIds.OptionsInvalid);
        exception.MessageType.Should().Be(type);
    }

    private static MqttTopicOptions Options(params (MqttMessageType Type, string Template)[] mappings)
    {
        var options = new MqttTopicOptions();
        foreach (var (type, template) in mappings)
        {
            if (options.Mappings == null)
                options.Mappings = new Dictionary<MqttMessageType, MqttTopicMapping>();
            options.Mappings[type] = new() { TopicTemplate = template };
        }
        return options;
    }

    private static string Resolve(MqttTopicTemplate template, IReadOnlyDictionary<string, string> values)
    {
        return string.Concat(template.Segments.Select(s => s.IsPlaceholder ? values[s.Text] : s.Text));
    }
}
