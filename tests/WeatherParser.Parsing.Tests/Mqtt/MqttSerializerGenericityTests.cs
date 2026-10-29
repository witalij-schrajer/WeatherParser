using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Live;

namespace WeatherParser.Parsing.Tests.Mqtt;

[TestClass]
public class MqttSerializerGenericityTests
{
    private static IReadOnlyList<(string File, string Text)> MqttSources()
    {
        var root = OutputDirectoryGuard.RepositoryRoot() ?? throw new InvalidOperationException("Repository root not found.");
        var dir = Path.Combine(root, "src", "WeatherParser.Parsing", "Mqtt");
        var files = Directory.GetFiles(dir, "*.cs");
        return files.Select(file => (Path.GetFileName(file), File.ReadAllText(file))).ToList();
    }

    private static IEnumerable<string> ForbiddenLiterals(string text)
    {
        var forbiddenPrefixes = new[]
        {
            "dwd:",
            "dwd-mosmix-s",
            "dwd-mosmix-l",
            "osthessenwetter",
            "haus/weather"
        };

        return Regex.Matches(text, "\"(?:[^\"\\\\\\n]|\\\\.)*\"")
            .Select(match => match.Value[1..^1])
            .Where(literal => forbiddenPrefixes.Any(prefix => literal.Contains(prefix, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool ComparesSourceWithLiteral(string text)
    {
        var regex = new Regex(
            @"(?:\b(?:Source|source|provider|Provider)\s*(?:==|!=)\s*""|""[^""\n]*""\s*(?:==|!=)\s*[\w.]*\b(?:Source|source|provider|Provider)\b|\b(?:Source|source|provider|Provider)\.(?:StartsWith|EndsWith|Contains|Equals)\(\s*""|string\.Equals\(\s*[^,\n]*\b(?:Source|source|provider|Provider)\b[^,\n]*,\s*""|string\.Equals\(\s*""[^""\n]*""\s*,\s*[^)\n]*\b(?:Source|source|provider|Provider)\b)",
            RegexOptions.IgnoreCase);

        return regex.IsMatch(text);
    }

    [TestMethod]
    public void MqttTypes_ContainNoProviderLiterals()
    {
        var sources = MqttSources();
        sources.Should().NotBeEmpty();

        foreach (var (file, text) in sources)
        {
            var found = ForbiddenLiterals(text).ToList();
            found.Should().BeEmpty($"because MQTT types should not contain provider literals in file '{file}'");
        }
    }

    [TestMethod]
    public void MqttTypes_DoNotCompareSourceValuesWithLiterals()
    {
        var sources = MqttSources();
        sources.Should().NotBeEmpty();

        foreach (var (file, text) in sources)
        {
            var comparesSourceWithLiteral = ComparesSourceWithLiteral(text);
            comparesSourceWithLiteral.Should().BeFalse($"because MQTT types should not compare source values with literals in file '{file}'");
        }
    }

    [TestMethod]
    public void Scanners_DetectProviderSpecificCode()
    {
        ForbiddenLiterals("var a = \"dwd:00044\";").Should().ContainSingle().Which.Should().Be("dwd:00044");
        ForbiddenLiterals("var t = \"haus/weather/measurements/{source}/state\";").Should().ContainSingle().Which.Should().Be("haus/weather/measurements/{source}/state");
        ForbiddenLiterals("x = $\"osthessenwetter:{id}\";").Should().ContainSingle().Which.Should().Be("osthessenwetter:{id}");

        ForbiddenLiterals("var p = \"provider\";").Should().BeEmpty();

        ComparesSourceWithLiteral("if (dataset.Source == \"dwd:1\")").Should().BeTrue();
        ComparesSourceWithLiteral("if (\"custom\" == dataset.Source)").Should().BeTrue();
        ComparesSourceWithLiteral("if (provider != \"dwd\")").Should().BeTrue();
        ComparesSourceWithLiteral("if (run.Source.StartsWith(\"dwd-mosmix\"))").Should().BeTrue();
        ComparesSourceWithLiteral("string.Equals(dataset.Source, \"custom\")").Should().BeTrue();
        ComparesSourceWithLiteral("string.Equals(\"custom\", provider)").Should().BeTrue();

        ComparesSourceWithLiteral("var value = PlaceholderValue(root, name);").Should().BeFalse();
        ComparesSourceWithLiteral("if (separator <= 0)").Should().BeFalse();
    }

    [TestMethod]
    public void MqttSerializer_DependsOnlyOnMqttRulesAndJsonSerializer()
    {
        var ctor = typeof(MqttMessageSerializer).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var parameters = ctor.GetParameters().Select(p => p.ParameterType).ToArray();

        parameters.Should().Equal(
            typeof(MqttTopicOptions),
            typeof(IMqttTopicOptionsValidator),
            typeof(IMqttTopicValidator),
            typeof(IJsonExportSerializer));
    }

    [TestMethod]
    public void MqttNamespace_HasNoBrokerOrLoggingDependency()
    {
        var mqttAssembly = typeof(MqttMessageSerializer).Assembly;
        var typesInMqtt = mqttAssembly.GetTypes().Where(t => t.Namespace == "WeatherParser.Parsing.Mqtt");

        foreach (var type in typesInMqtt)
        {
            foreach (var ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                foreach (var param in ctor.GetParameters())
                {
                    var paramType = param.ParameterType;
                    if (paramType.Namespace?.StartsWith("Microsoft.Extensions.Logging") == true)
                    {
                        Assert.Fail($"Constructor of '{type.FullName}' has a parameter of type '{paramType.FullName}' which is from Microsoft.Extensions.Logging");
                    }

                    if (paramType.Assembly.GetName().Name?.Contains("Mqtt") == true && paramType.Assembly != mqttAssembly)
                    {
                        Assert.Fail($"Constructor of '{type.FullName}' has a parameter of type '{paramType.FullName}' which is from another MQTT assembly");
                    }
                }
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var fieldType = field.FieldType;
                if (fieldType.Namespace?.StartsWith("Microsoft.Extensions.Logging") == true)
                {
                    Assert.Fail($"Field '{field.Name}' of '{type.FullName}' is of type '{fieldType.FullName}' which is from Microsoft.Extensions.Logging");
                }

                if (fieldType.Assembly.GetName().Name?.Contains("Mqtt") == true && fieldType.Assembly != mqttAssembly)
                {
                    Assert.Fail($"Field '{field.Name}' of '{type.FullName}' is of type '{fieldType.FullName}' which is from another MQTT assembly");
                }
            }
        }
    }
}
