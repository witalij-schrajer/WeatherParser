using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Mqtt;

[TestClass]
public sealed class MqttTopicValidatorTests
{
    private IMqttTopicValidator _validator = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _validator = new MqttTopicValidator();
    }

    [TestMethod]
    [DataRow("dwd:00044")]
    [DataRow("osthessenwetter:fulda-mitte")]
    [DataRow("dwd-mosmix-l:P0489")]
    [DataRow("MOSMIX_L")]
    [DataRow("00044")]
    [DataRow("M\u00FCnchen Stadt")]
    [DataRow("\u00C4")]
    [DataRow("a b")]
    [DataRow("\uD83D\uDE00")]
    public void ValidateValue_SafeValues_Accepted(string value)
    {
        var act = () => _validator.ValidatePlaceholderValue("source", value);
        act.Should().NotThrow();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("a/b")]
    [DataRow("a+b")]
    [DataRow("a#b")]
    [DataRow("/")]
    [DataRow("\u0000")]
    [DataRow("\u0001")]
    [DataRow("\u001F")]
    [DataRow("\u007F")]
    [DataRow("\u0080")]
    [DataRow("\u009F")]
    [DataRow("\uFDD0")]
    [DataRow("\uFDEF")]
    [DataRow("\uFFFE")]
    [DataRow("\uFFFF")]
    [DataRow("\uD83F\uDFFF")]
    [DataRow("\uDBFF\uDFFF")]
    public void ValidateValue_UnsafeValues_ThrowPlaceholderValueInvalid(string value)
    {
        var act = () => _validator.ValidatePlaceholderValue("source", value);
        ShouldBePlaceholderValueInvalid(act);
    }

    [TestMethod]
    public void ValidateValue_UnpairedSurrogates_ThrowPlaceholderValueInvalid()
    {
        ShouldBePlaceholderValueInvalid(() => _validator.ValidatePlaceholderValue("source", "a\uD800"));
        ShouldBePlaceholderValueInvalid(() => _validator.ValidatePlaceholderValue("source", "\uDC00a"));
        ShouldBePlaceholderValueInvalid(() => _validator.ValidatePlaceholderValue("source", "\uDC00\uD800"));
    }

    [TestMethod]
    [DataRow(" ")]
    [DataRow("~")]
    [DataRow("\u00A0")]
    [DataRow("\uFDCF")]
    [DataRow("\uFDF0")]
    [DataRow("\uFFFD")]
    [DataRow("\uD83F\uDFFD")]
    public void ValidateValue_BoundaryNeighbours_Accepted(string value)
    {
        var act = () => _validator.ValidatePlaceholderValue("source", value);
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ValidateValue_Null_Throws()
    {
        var act1 = () => _validator.ValidatePlaceholderValue(null!, "value");
        act1.Should().Throw<ArgumentNullException>().WithParameterName("placeholderName");

        var act2 = () => _validator.ValidatePlaceholderValue("name", null!);
        act2.Should().Throw<ArgumentNullException>().WithParameterName("value");
    }

    [TestMethod]
    [DataRow("haus/weather/measurements/dwd:00044/state")]
    [DataRow("a")]
    [DataRow("a b/c")]
    [DataRow("a$/b")]
    [DataRow("haus/weather/forecasts/dwd-mosmix-s:10637/state")]
    public void ValidateTopic_ValidTopics_Accepted(string topic)
    {
        var act = () => _validator.ValidateTopic(topic);
        act.Should().NotThrow();
    }

    [TestMethod]
    public void ValidateTopic_MaximumBytes_Accepted()
    {
        var act1 = () => _validator.ValidateTopic(new string('a', 65535));
        act1.Should().NotThrow();

        var act2 = () => _validator.ValidateTopic(new string('\u00E4', 32767) + "a");
        act2.Should().NotThrow();

        var act3 = () => _validator.ValidateTopic("a/" + new string('\u20AC', 21844) + "b");
        act3.Should().NotThrow();
    }

    [TestMethod]
    public void ValidateTopic_OneByteAbove_ThrowsTopicInvalid()
    {
        ShouldBeTopicInvalid(() => _validator.ValidateTopic(new string('a', 65536)));
        ShouldBeTopicInvalid(() => _validator.ValidateTopic(new string('\u00E4', 32768)));
        ShouldBeTopicInvalid(() => _validator.ValidateTopic(new string('\u20AC', 21845) + "a"));
    }

    [TestMethod]
    [DataRow("a/+/b")]
    [DataRow("a/#")]
    [DataRow("#")]
    [DataRow("+")]
    [DataRow("a+b/c")]
    public void ValidateTopic_Wildcards_ThrowTopicInvalid(string topic)
    {
        ShouldBeTopicInvalid(() => _validator.ValidateTopic(topic));
    }

    [TestMethod]
    [DataRow("a/\u0000")]
    [DataRow("a/\u0001b")]
    [DataRow("a/\u009F")]
    [DataRow("a/\uFDD0")]
    [DataRow("a/\uFFFF")]
    [DataRow("a/\uD83F\uDFFF")]
    public void ValidateTopic_DisallowedCodePoints_ThrowTopicInvalid(string topic)
    {
        ShouldBeTopicInvalid(() => _validator.ValidateTopic(topic));
    }

    [TestMethod]
    public void ValidateTopic_UnpairedSurrogate_ThrowsTopicInvalid()
    {
        ShouldBeTopicInvalid(() => _validator.ValidateTopic("a/\uD800"));
        ShouldBeTopicInvalid(() => _validator.ValidateTopic("a/\uDC00b"));
    }

    [TestMethod]
    public void ValidateTopic_LeadingDollar_ThrowsTopicInvalid()
    {
        ShouldBeTopicInvalid(() => _validator.ValidateTopic("$SYS/x"));
        ShouldBeTopicInvalid(() => _validator.ValidateTopic("$"));
    }

    [TestMethod]
    [DataRow("a//b")]
    [DataRow("/a")]
    [DataRow("a/")]
    [DataRow("/")]
    [DataRow("//")]
    public void ValidateTopic_EmptyLevels_PerMq07(string topic)
    {
        ShouldBeTopicInvalid(() => _validator.ValidateTopic(topic));
    }

    [TestMethod]
    public void ValidateTopic_EmptyOrNull_Throws()
    {
        ShouldBeTopicInvalid(() => _validator.ValidateTopic(""));
        var act = () => _validator.ValidateTopic(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("topic");
    }

    [TestMethod]
    public void Failures_NameRuleNotValueContent()
    {
        var act1 = () => _validator.ValidatePlaceholderValue("station_id", "secret-marker/x");
        var ex1 = act1.Should().Throw<MqttMessageException>().Which;
        ex1.ErrorId.Should().Be(MqttMessageErrorIds.PlaceholderValueInvalid);
        ex1.MessageType.Should().BeNull();
        ex1.Message.Should().Contain("station_id");
        ex1.Message.Should().NotContain("secret-marker");

        var act2 = () => _validator.ValidateTopic("secret-marker/+");
        var ex2 = act2.Should().Throw<MqttMessageException>().Which;
        ex2.ErrorId.Should().Be(MqttMessageErrorIds.TopicInvalid);
        ex2.MessageType.Should().BeNull();
        ex2.Message.Should().NotContain("secret-marker");
        ex2.Message.Should().Contain("position");
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesTopicValidator()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<IMqttTopicValidator>().Should().BeOfType<MqttTopicValidator>();
    }

    private static void ShouldBePlaceholderValueInvalid(Action act)
    {
        var exception = act.Should().Throw<MqttMessageException>().Which;
        exception.ErrorId.Should().Be(MqttMessageErrorIds.PlaceholderValueInvalid);
        exception.MessageType.Should().BeNull();
    }

    private static void ShouldBeTopicInvalid(Action act)
    {
        var exception = act.Should().Throw<MqttMessageException>().Which;
        exception.ErrorId.Should().Be(MqttMessageErrorIds.TopicInvalid);
        exception.MessageType.Should().BeNull();
    }
}
