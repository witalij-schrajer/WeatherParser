using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Xml.Serialization;
using WeatherParser.Parsing.Export;

namespace WeatherParser.SchemaGeneration;

/// <summary>
/// Sets required and optional elements from the C-05 omission, typed timestamps and constants, and removes nillable (contracts C-99 to C-102).
/// </summary>
public sealed class ExportSchemaRefiner : IExportSchemaRefiner
{
    private static readonly XNamespace Xs = "http://www.w3.org/2001/XMLSchema";
    private const string TimestampPattern = @"[0-9]{4}-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])T([01][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]";

    /// <inheritdoc/>
    public void Refine(XDocument schema, IReadOnlyList<Type> rootTypes)
    {
        var root = schema.Root!;
        var refined = new HashSet<string>(StringComparer.Ordinal);

        foreach (var type in ReachableTypes(rootTypes))
        {
            var complexType = root.Elements(Xs + "complexType").SingleOrDefault(t => (string?)t.Attribute("name") == type.Name) ?? throw new InvalidOperationException($"No complexType '{type.Name}' was exported.");

            var handled = new HashSet<string>(StringComparer.Ordinal);

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var wireName = WireName(property);
                if (wireName is null)
                {
                    if (property.GetCustomAttribute<XmlIgnoreAttribute>() is null)
                        continue;
                }
                else
                {
                    handled.Add(wireName);

                    var element = complexType.Descendants(Xs + "element").SingleOrDefault(e => (string?)e.Attribute("name") == wireName) ?? throw new InvalidOperationException($"No element '{wireName}' was exported for {type.Name}.{property.Name}.");

                    element.SetAttributeValue("minOccurs", IsOptional(property) ? "0" : "1");

                    if (IsTimestamp(property))
                    {
                        element.SetAttributeValue("type", "tns:UtcTimestamp");
                    }
                    else if (wireName == "schema_version")
                    {
                        element.SetAttributeValue("type", "tns:SchemaVersionV1");
                    }
                    else if (type == typeof(ForecastRun) && wireName == "dataset_type")
                    {
                        element.SetAttributeValue("type", "tns:ForecastDatasetType");
                    }
                }
            }

            foreach (var exported in complexType.Descendants(Xs + "element").Select(e => (string?)e.Attribute("name")))
            {
                if (exported is null || !handled.Contains(exported))
                {
                    throw new InvalidOperationException($"{type.Name}.{exported} is exported without an explicit XML mapping; every serialized export member needs XmlElement or XmlArray (contract C-08).");
                }
            }

            refined.Add(type.Name);
        }

        foreach (var name in root.Elements(Xs + "complexType").Select(t => (string?)t.Attribute("name")))
        {
            if (name is null || (!name.StartsWith("ArrayOf", StringComparison.Ordinal) && !refined.Contains(name)))
            {
                throw new InvalidOperationException($"The exported complex type '{name}' was not reached by the refinement; its members would stay unclassified (contract C-99).");
            }
        }

        foreach (var attribute in root.DescendantsAndSelf().Attributes("nillable").ToList())
            attribute.Remove();

        var simpleTypes = new List<XElement>();

        simpleTypes.Add(new XElement(Xs + "simpleType",
            new XAttribute("name", "UtcTimestamp"),
            new XElement(Xs + "restriction",
                new XAttribute("base", "xs:string"),
                new XElement(Xs + "length",
                    new XAttribute("value", "19")),
                new XElement(Xs + "pattern",
                    new XAttribute("value", TimestampPattern)))));

        simpleTypes.Add(new XElement(Xs + "simpleType",
            new XAttribute("name", "SchemaVersionV1"),
            new XElement(Xs + "restriction",
                new XAttribute("base", "xs:string"),
                new XElement(Xs + "enumeration",
                    new XAttribute("value", ExportNamespaces.SchemaVersionV1)))));

        simpleTypes.Add(new XElement(Xs + "simpleType",
            new XAttribute("name", "ForecastDatasetType"),
            new XElement(Xs + "restriction",
                new XAttribute("base", "xs:string"),
                new XElement(Xs + "enumeration",
                    new XAttribute("value", new ForecastRun().DatasetType)))));

        foreach (var simpleType in simpleTypes)
            root.Add(simpleType);
    }

    private static string? WireName(PropertyInfo property)
    {
        if (property.GetCustomAttribute<XmlIgnoreAttribute>() is not null)
            return null;

        var element = property.GetCustomAttribute<XmlElementAttribute>();
        if (element is not null)
            return element.ElementName;

        var array = property.GetCustomAttribute<XmlArrayAttribute>();
        if (array is not null)
            return array.ElementName;

        return null;
    }

    private static bool IsOptional(PropertyInfo property) => property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition == JsonIgnoreCondition.WhenWritingNull;

    private static bool IsTimestamp(PropertyInfo property)
    {
        if (property.PropertyType != typeof(string))
            return false;

        if (!property.Name.EndsWith("Wire", StringComparison.Ordinal))
            return false;

        var baseName = property.Name[..^4];
        var utcProperty = property.DeclaringType!.GetProperty(baseName + "Utc");
        if (utcProperty is null)
            return false;

        if (utcProperty.PropertyType != typeof(DateTime))
            return false;

        if (utcProperty.GetCustomAttribute<JsonIgnoreAttribute>() is null)
            return false;

        if (utcProperty.GetCustomAttribute<XmlIgnoreAttribute>() is null)
            return false;

        return true;
    }

    private static IEnumerable<Type> ReachableTypes(IReadOnlyList<Type> rootTypes)
    {
        var visited = new HashSet<Type>();
        var queue = new Queue<Type>(rootTypes);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
                continue;

            foreach (var property in current.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var wireName = WireName(property);
                if (wireName is null)
                    continue;

                var propType = property.PropertyType;
                while (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    propType = propType.GetGenericArguments()[0];

                var enumerableInterface = propType.GetInterfaces().Concat(new[] { propType }).FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                if (enumerableInterface is not null && propType != typeof(string))
                {
                    propType = enumerableInterface.GetGenericArguments()[0];
                }

                if (propType.IsClass && propType.Assembly == rootTypes[0].Assembly)
                    queue.Enqueue(propType);
            }

            if (current.BaseType is { IsClass: true } baseType && baseType.Assembly == rootTypes[0].Assembly)
            {
                queue.Enqueue(baseType);
            }
        }

        return visited;
    }
}
