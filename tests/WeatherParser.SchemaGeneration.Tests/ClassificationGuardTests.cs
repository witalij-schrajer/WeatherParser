using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Xml.Serialization;
using FluentAssertions;
using WeatherParser.Parsing.Export;

namespace WeatherParser.SchemaGeneration.Tests;

[TestClass]
public sealed class ClassificationGuardTests
{
    [TestMethod]
    public void EveryMappedProperty_HasItsC05ClassificationInTheSchema()
    {
        var generator = new ExportSchemaGenerator(new ExportSchemaRefiner());
        var schema = generator.Generate();
        var document = XDocument.Parse(Encoding.UTF8.GetString(schema));
        var root = document.Root!;
        XNamespace xs = "http://www.w3.org/2001/XMLSchema";

        var visited = new HashSet<Type>();
        var queue = new Queue<Type>(ExportSchemaGenerator.RootTypes);
        var mismatches = new List<string>();
        var checkedCount = 0;

        while (queue.Any())
        {
            var type = queue.Dequeue();
            if (!visited.Add(type))
                continue;

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var xmlElementAttribute = property.GetCustomAttribute<XmlElementAttribute>();
                var xmlArrayAttribute = property.GetCustomAttribute<XmlArrayAttribute>();

                if (xmlElementAttribute == null && xmlArrayAttribute == null)
                    continue;

                var wireName = xmlElementAttribute?.ElementName ?? xmlArrayAttribute?.ElementName;
                if (wireName == null)
                    continue;

                var expectedMinOccurs = property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition == JsonIgnoreCondition.WhenWritingNull ? "0" : "1";
                var complexType = root.Elements(xs + "complexType").Single(t => (string?)t.Attribute("name") == type.Name);
                var element = complexType.Descendants(xs + "element").Single(e => (string?)e.Attribute("name") == wireName);
                var actualMinOccurs = (string?)element.Attribute("minOccurs");
                if (actualMinOccurs != expectedMinOccurs)
                {
                    mismatches.Add($"{type.Name}.{property.Name}: expected minOccurs {expectedMinOccurs}, found {actualMinOccurs}");
                }
                checkedCount++;

                var propertyType = property.PropertyType;
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    propertyType = propertyType.GetGenericArguments()[0];
                }

                Type? elementType;
                if (propertyType.IsClass && propertyType.Assembly == typeof(ExportDocument).Assembly)
                {
                    queue.Enqueue(propertyType);
                }
                else if (propertyType != typeof(string) && 
                         (elementType = propertyType.GetInterfaces()
                             .Concat(new[] { propertyType })
                             .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))) != null)
                {
                    var genericArg = elementType.GetGenericArguments()[0];
                    if (genericArg.IsClass && genericArg.Assembly == typeof(ExportDocument).Assembly)
                    {
                        queue.Enqueue(genericArg);
                    }
                }
                else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var elementTypeFromList = propertyType.GetGenericArguments()[0];
                    if (elementTypeFromList.IsClass && elementTypeFromList.Assembly == typeof(ExportDocument).Assembly)
                    {
                        queue.Enqueue(elementTypeFromList);
                    }
                }
            }
        }

        mismatches.Should().BeEmpty();
        checkedCount.Should().BeGreaterThan(80, "the guard must cover the whole export graph");
        
        // Assert that every exported entity complex type was visited
        root.Elements(xs + "complexType")
            .Select(t => (string)t.Attribute("name")!)
            .Where(name => !name.StartsWith("ArrayOf", StringComparison.Ordinal))
            .Should()
            .BeEquivalentTo(visited.Select(t => t.Name), "every exported entity type must be classified");
    }
}
