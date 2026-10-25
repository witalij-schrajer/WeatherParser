using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;

namespace WeatherParser.Parsing.Tests.Export;

/// <summary>
/// JSON and XML export comparison routines shared by the contract tests (moved unchanged from AllRootsEquivalenceTests and ObservationExportEquivalenceTests).
/// </summary>
internal static class ExportEquivalence
{
    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";

    /// <summary>
    /// Asserts that a JSON element and an XML element are equivalent.
    /// </summary>
    public static void AssertEquivalent(JsonElement json, XElement xml, string path)
    {
        xml.Name.Namespace.Should().Be(Ns, path);
        xml.Attributes().Where(a => !a.IsNamespaceDeclaration).Should().BeEmpty(path);

        switch (json.ValueKind)
        {
            case JsonValueKind.Object:
                var jsonProperties = json.EnumerateObject().ToList();
                var xmlChildren = xml.Elements().ToList();
                xmlChildren.Select(e => e.Name.LocalName).Should().Equal(jsonProperties.Select(p => p.Name), path);
                for (int i = 0; i < jsonProperties.Count; i++)
                {
                    AssertEquivalent(jsonProperties[i].Value, xmlChildren[i], path + "/" + jsonProperties[i].Name);
                }
                break;
            case JsonValueKind.Array:
                xml.Elements().Should().HaveCount(json.GetArrayLength(), path);
                var xmlElements = xml.Elements().ToList();
                for (int i = 0; i < xmlElements.Count; i++)
                {
                    AssertEquivalent(json[i], xmlElements[i], path + "[" + i + "]");
                }
                break;
            case JsonValueKind.String:
                xml.HasElements.Should().BeFalse(path);
                xml.Value.Should().Be(json.GetString(), path);
                break;
            case JsonValueKind.Number:
                xml.HasElements.Should().BeFalse(path);
                xml.Value.Should().Be(json.GetRawText(), path);
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
                xml.Value.Should().Be(json.GetRawText(), path);
                break;
            default:
                Assert.Fail(path + " has JSON kind " + json.ValueKind);
                break;
        }
    }

    /// <summary>
    /// Flattens a JSON element into a dictionary.
    /// </summary>
    public static void FlattenJson(JsonElement element, string path, Dictionary<string, string> map)
    {
        foreach (var property in element.EnumerateObject())
        {
            var childPath = path.Length == 0 ? property.Name : $"{path}/{property.Name}";
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    FlattenJson(property.Value, childPath, map);
                    break;
                case JsonValueKind.String:
                    map[childPath] = property.Value.GetString()!;
                    break;
                default:
                    map[childPath] = property.Value.GetRawText();
                    break;
            }
        }
    }

    /// <summary>
    /// Flattens an XML element into a dictionary.
    /// </summary>
    public static void FlattenXml(XElement element, string path, Dictionary<string, string> map)
    {
        var childPath = path.Length == 0 ? element.Name.LocalName : $"{path}/{element.Name.LocalName}";
        if (element.HasElements)
        {
            foreach (var child in element.Elements())
            {
                FlattenXml(child, childPath, map);
            }
        }
        else
        {
            map[childPath] = element.Value;
        }
    }
}
