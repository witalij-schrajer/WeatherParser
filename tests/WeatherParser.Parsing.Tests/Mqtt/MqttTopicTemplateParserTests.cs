using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Mqtt;

[TestClass]
public sealed class MqttTopicTemplateParserTests
{
    private IMqttTopicTemplateParser _parser = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _parser = new MqttTopicTemplateParser();
    }

    [TestMethod]
    public void Parse_LiteralOnly_OneLiteralSegment()
    {
        var result = _parser.Parse("haus/weather/stations/dwd/selection", MqttMessageType.CdcStationList);

        result.MessageType.Should().Be(MqttMessageType.CdcStationList);
        result.Text.Should().Be("haus/weather/stations/dwd/selection");
        result.Segments.Should().HaveCount(1);
        result.Segments[0].IsPlaceholder.Should().BeFalse();
        result.Segments[0].Text.Should().Be("haus/weather/stations/dwd/selection");
    }

    [TestMethod]
    public void Parse_PlaceholderAtStartMiddleEnd()
    {
        var start = _parser.Parse("{source}/state", MqttMessageType.Observation);
        start.Segments.Should().HaveCount(2);
        start.Segments[0].IsPlaceholder.Should().BeTrue();
        start.Segments[0].Text.Should().Be("source");
        start.Segments[1].IsPlaceholder.Should().BeFalse();
        start.Segments[1].Text.Should().Be("/state");

        var middle = _parser.Parse("a/{source}/b", MqttMessageType.Observation);
        middle.Segments.Should().HaveCount(3);
        middle.Segments[0].IsPlaceholder.Should().BeFalse();
        middle.Segments[0].Text.Should().Be("a/");
        middle.Segments[1].IsPlaceholder.Should().BeTrue();
        middle.Segments[1].Text.Should().Be("source");
        middle.Segments[2].IsPlaceholder.Should().BeFalse();
        middle.Segments[2].Text.Should().Be("/b");

        var end = _parser.Parse("a/{source}", MqttMessageType.Observation);
        end.Segments.Should().HaveCount(2);
        end.Segments[0].IsPlaceholder.Should().BeFalse();
        end.Segments[0].Text.Should().Be("a/");
        end.Segments[1].IsPlaceholder.Should().BeTrue();
        end.Segments[1].Text.Should().Be("source");
    }

    [TestMethod]
    public void Parse_AdjacentPlaceholders()
    {
        var result = _parser.Parse("f/{product}{mosmix_id}", MqttMessageType.Forecast);

        result.Segments.Should().HaveCount(3);
        result.Segments[0].IsPlaceholder.Should().BeFalse();
        result.Segments[0].Text.Should().Be("f/");
        result.Segments[1].IsPlaceholder.Should().BeTrue();
        result.Segments[1].Text.Should().Be("product");
        result.Segments[2].IsPlaceholder.Should().BeTrue();
        result.Segments[2].Text.Should().Be("mosmix_id");
    }

    [TestMethod]
    [DataRow("o/{station}")]
    [DataRow("o/{Source}")]
    [DataRow("o/{measured_at}")]
    public void Parse_UnknownPlaceholder_ThrowsOptionsInvalid(string template)
    {
        ShouldBeOptionsInvalid(() => _parser.Parse(template, MqttMessageType.Observation), MqttMessageType.Observation);
    }

    [TestMethod]
    [DataRow("o/{mosmix_id}", MqttMessageType.Observation)]
    [DataRow("o/{product}", MqttMessageType.Observation)]
    [DataRow("f/{station_id}", MqttMessageType.Forecast)]
    [DataRow("s/{source}", MqttMessageType.CdcStationList)]
    [DataRow("s/{station_id}", MqttMessageType.MosmixLocationList)]
    public void Parse_PlaceholderNotAvailableForMessageType_ThrowsOptionsInvalid(string template, MqttMessageType messageType)
    {
        ShouldBeOptionsInvalid(() => _parser.Parse(template, messageType), messageType);
    }

    [TestMethod]
    public void Parse_EachMessageType_AcceptsItsPlaceholders()
    {
        _parser.Parse("o/{source}/{provider}/{station_id}", MqttMessageType.Observation).Segments.Count(s => s.IsPlaceholder).Should().Be(3);
        _parser.Parse("f/{source}/{provider}/{mosmix_id}/{product}", MqttMessageType.Forecast).Segments.Count(s => s.IsPlaceholder).Should().Be(4);
        _parser.Parse("c/{provider}", MqttMessageType.CdcStationList).Segments.Count(s => s.IsPlaceholder).Should().Be(1);
        _parser.Parse("m/{provider}", MqttMessageType.MosmixLocationList).Segments.Count(s => s.IsPlaceholder).Should().Be(1);
    }

    [TestMethod]
    [DataRow("o/{source")]
    [DataRow("o/source}")]
    [DataRow("o/{{source}}")]
    [DataRow("o/{}")]
    [DataRow("o/{source}}")]
    [DataRow("o/{")]
    [DataRow("}")]
    public void Parse_UnbalancedOrNestedBraces_ThrowsOptionsInvalid(string template)
    {
        ShouldBeOptionsInvalid(() => _parser.Parse(template, MqttMessageType.Observation), MqttMessageType.Observation);
    }

    [TestMethod]
    public void Parse_EmptyTemplate_ThrowsOptionsInvalid()
    {
        ShouldBeOptionsInvalid(() => _parser.Parse("", MqttMessageType.Forecast), MqttMessageType.Forecast);
    }

    [TestMethod]
    [DataRow("+")]
    [DataRow("#")]
    [DataRow("\u0000")]
    [DataRow("\u0001")]
    [DataRow("\u001F")]
    [DataRow("\u007F")]
    [DataRow("\u009F")]
    [DataRow("\uFDD0")]
    [DataRow("\uFDEF")]
    [DataRow("\uFFFE")]
    [DataRow("\uFFFF")]
    public void Parse_LiteralPartInvalidCharacters_ThrowsOptionsInvalid(string bad)
    {
        ShouldBeOptionsInvalid(() => _parser.Parse("o/" + bad + "/{source}", MqttMessageType.Observation), MqttMessageType.Observation);
    }

    [TestMethod]
    public void Parse_LiteralPartUnpairedSurrogate_ThrowsOptionsInvalid()
    {
        // Attribute strings are stored as UTF-8 metadata, which replaces lone surrogates, so these cases are not DataRows.
        ShouldBeOptionsInvalid(() => _parser.Parse("o/\uD800/{source}", MqttMessageType.Observation), MqttMessageType.Observation);
        ShouldBeOptionsInvalid(() => _parser.Parse("o/\uDC00/{source}", MqttMessageType.Observation), MqttMessageType.Observation);
        ShouldBeOptionsInvalid(() => _parser.Parse("o/\uD800", MqttMessageType.CdcStationList), MqttMessageType.CdcStationList);
        ShouldBeOptionsInvalid(() => _parser.Parse("o/\uDC00\uD800/{source}", MqttMessageType.Observation), MqttMessageType.Observation);
    }

    [TestMethod]
    public void Parse_SupplementaryNonCharacter_ThrowsOptionsInvalid()
    {
        ShouldBeOptionsInvalid(() => _parser.Parse("o/\uD83F\uDFFF/{source}", MqttMessageType.Observation), MqttMessageType.Observation);
        ShouldBeOptionsInvalid(() => _parser.Parse("o/\uDBFF\uDFFF/{source}", MqttMessageType.Observation), MqttMessageType.Observation);
    }

    [TestMethod]
    [DataRow(" ")]
    [DataRow("~")]
    [DataRow("\u00A0")]
    [DataRow("\uFDCF")]
    [DataRow("\uFDF0")]
    [DataRow("\uFFFD")]
    [DataRow("\u00E4")]
    [DataRow(":")]
    [DataRow("-")]
    [DataRow("\uD83D\uDE00")]
    public void Parse_LiteralPartNeighbourCharacters_Accepted(string ok)
    {
        var result = _parser.Parse("o/" + ok + "/{source}", MqttMessageType.Observation);

        result.Segments[0].Text.Should().Be("o/" + ok + "/");
    }

    [TestMethod]
    public void Parse_TemplateLongerThanTopicLimit_ThrowsOptionsInvalid()
    {
        // 65 533 literal bytes + 1 for the placeholder = 65 534: accepted
        _parser.Parse(new string('a', 65532) + "/{source}", MqttMessageType.Observation).Should().NotBeNull();
        // 65 534 + 1 = 65 535: accepted
        _parser.Parse(new string('a', 65533) + "/{source}", MqttMessageType.Observation).Should().NotBeNull();
        // 65 535 + 1 = 65 536: rejected
        ShouldBeOptionsInvalid(() => _parser.Parse(new string('a', 65534) + "/{source}", MqttMessageType.Observation), MqttMessageType.Observation);
        // 32 766 two-byte characters + 1 + 1 = 65 534: accepted
        _parser.Parse(new string('\u00E4', 32766) + "/{source}", MqttMessageType.Observation).Should().NotBeNull();
        // 32 767 two-byte characters + 1 + 1 = 65 536: rejected
        ShouldBeOptionsInvalid(() => _parser.Parse(new string('\u00E4', 32767) + "/{source}", MqttMessageType.Observation), MqttMessageType.Observation);
    }

    [TestMethod]
    public void Parse_LeadingDollar_ThrowsOptionsInvalid()
    {
        ShouldBeOptionsInvalid(() => _parser.Parse("$SYS/{source}", MqttMessageType.Observation), MqttMessageType.Observation);
        ShouldBeOptionsInvalid(() => _parser.Parse("$x", MqttMessageType.CdcStationList), MqttMessageType.CdcStationList);
    }

    [TestMethod]
    public void Parse_PlaceholderFirst_NoDollarCheckOnValue()
    {
        _parser.Parse("{source}/x", MqttMessageType.Observation).Segments.Should().HaveCount(2);
        _parser.Parse("a$/{source}", MqttMessageType.Observation).Segments.Should().HaveCount(2);
    }

    [TestMethod]
    [DataRow("a//{source}")]
    [DataRow("/a/{source}")]
    [DataRow("a/{source}/")]
    [DataRow("{source}//x")]
    [DataRow("/")]
    [DataRow("a//b")]
    public void Parse_EmptyLevelsThatAlwaysArise_ThrowOptionsInvalid(string template)
    {
        ShouldBeOptionsInvalid(() => _parser.Parse(template, MqttMessageType.Observation), MqttMessageType.Observation);
    }

    [TestMethod]
    [DataRow("haus/weather/measurements/{source}/state")]
    [DataRow("{source}/state")]
    [DataRow("a/{source}")]
    [DataRow("a/{provider}/{station_id}")]
    [DataRow("{provider}/{station_id}")]
    public void Parse_SlashNextToPlaceholder_Accepted(string template)
    {
        _parser.Parse(template, MqttMessageType.Observation).Text.Should().Be(template);
    }

    [TestMethod]
    public void Parse_FailureMessage_NamesRuleNotTemplateValue()
    {
        var act = () => _parser.Parse("secret-marker/{station}", MqttMessageType.Observation);

        var exception = act.Should().Throw<MqttMessageException>().Which;
        exception.ErrorId.Should().Be(MqttMessageErrorIds.OptionsInvalid);
        exception.MessageType.Should().Be(MqttMessageType.Observation);
        exception.Message.Should().Contain("station");
        exception.Message.Should().NotContain("secret-marker");

        var plain = () => _parser.Parse("o/{source2}", MqttMessageType.Observation);
        plain.Should().Throw<MqttMessageException>().Which.Message.Should().Contain("source2");
    }

    [TestMethod]
    [DataRow("o/{st-ation}")]
    [DataRow("o/{st.ation}")]
    [DataRow("o/{st[ation}")]
    [DataRow("o/{st`ation}")]
    [DataRow("o/{st\u00E4tion}")]
    [DataRow("o/{st~ation}")]
    public void Parse_UnknownPlaceholderWithOtherCharacters_NamedGenerically(string template)
    {
        var act = () => _parser.Parse(template, MqttMessageType.Observation);

        var exception = act.Should().Throw<MqttMessageException>().Which;
        exception.ErrorId.Should().Be(MqttMessageErrorIds.OptionsInvalid);
        exception.MessageType.Should().Be(MqttMessageType.Observation);
        exception.Message.Should().Be("The topic template contains an unknown placeholder.");
    }

    [TestMethod]
    public void Parse_Null_ThrowsArgumentNull()
    {
        var act = () => _parser.Parse(null!, MqttMessageType.Observation);

        act.Should().Throw<ArgumentNullException>().WithParameterName("template");
    }

    [TestMethod]
    public void Parse_UndefinedMessageType_ThrowsArgumentException()
    {
        var act = () => _parser.Parse("test", (MqttMessageType)7);

        act.Should().Throw<ArgumentException>().WithParameterName("messageType");
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesTemplateParser()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<IMqttTopicTemplateParser>().Should().BeOfType<MqttTopicTemplateParser>();
    }

    private static void ShouldBeOptionsInvalid(Action act, MqttMessageType type)
    {
        var exception = act.Should().Throw<MqttMessageException>().Which;
        exception.ErrorId.Should().Be(MqttMessageErrorIds.OptionsInvalid);
        exception.MessageType.Should().Be(type);
    }
}
