using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Tests.Live;

namespace WeatherParser.Parsing.Tests.Mqtt;

[TestClass]
public class ReadmeMqttSectionTests
{
    private static string MqttSection()
    {
        var root = OutputDirectoryGuard.RepositoryRoot() ?? throw new InvalidOperationException("Repository root not found.");
        var readmePath = Path.Combine(root, "README.md");
        var readmeContent = File.ReadAllText(readmePath);
        var lines = readmeContent.Split('\n');

        var mqttHeading = "## MQTT serializer (library)";
        var startIndex = -1;
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimEnd('\r').Equals(mqttHeading))
            {
                startIndex = i;
                break;
            }
        }

        if (startIndex == -1)
        {
            Assert.Fail("README.md has no section '## MQTT serializer (library)'.");
        }

        var endIndex = lines.Length;
        for (var i = startIndex + 1; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("## "))
            {
                endIndex = i;
                break;
            }
        }

        return string.Join('\n', lines.Skip(startIndex + 1).Take(endIndex - startIndex - 1));
    }

    [TestMethod]
    public void Readme_MqttSection_NamesEveryErrorId()
    {
        var section = MqttSection();
        var errorIds = typeof(MqttMessageErrorIds)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        foreach (var id in errorIds)
        {
            section.Should().Contain(id, $"error ID {id}");
        }
    }

    [TestMethod]
    public void Readme_MqttSection_StatesDuplicateRuleAndRestrictions()
    {
        var section = MqttSection();
        section.Should().Contain("byte-identical");
        section.Should().Contain("OW-14");
        section.Should().Contain("MqttTopicOptions");
        section.Should().Contain("not library defaults");
        section.Should().Contain("268 435 450");
        section.Should().Contain("QoS 1");
    }
}
