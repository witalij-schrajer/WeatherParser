using System;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Tests.Export;

internal static class ExportGraphTraversal
{
    /// <summary>
    /// Gets all custom types reachable from the specified root types, traversing public instance properties.
    /// </summary>
    /// <param name="roots">The root types to start traversal from.</param>
    /// <returns>A collection of custom types reachable from the roots.</returns>
    public static IReadOnlyCollection<Type> CustomTypesReachableFrom(params Type[] roots)
    {
        var visited = new HashSet<Type>();
        var queue = new Queue<Type>(roots);
        var result = new HashSet<Type>();

        foreach (var root in roots)
        {
            if (visited.Add(root))
            {
                result.Add(root);
                queue.Enqueue(root);
            }
        }

        var libraryAssembly = typeof(ObservationDataset).Assembly;

        while (queue.Count > 0)
        {
            var currentType = queue.Dequeue();

            var properties = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                // Skip properties marked with both [JsonIgnore] and [XmlIgnore]
                var jsonIgnore = property.GetCustomAttribute<JsonIgnoreAttribute>();
                var xmlIgnore = property.GetCustomAttribute<XmlIgnoreAttribute>();
                if (jsonIgnore != null && xmlIgnore != null)
                {
                    // Only skip if the property's type is not a library type
                    if (!IsLibraryType(property.PropertyType))
                        continue;
                }

                var propertyType = GetUnderlyingType(property.PropertyType);

                if (visited.Add(propertyType))
                {
                    if (IsLibraryType(propertyType))
                    {
                        result.Add(propertyType);
                        queue.Enqueue(propertyType);
                    }
                }
            }
        }

        return result.OrderBy(t => t.FullName).ToArray();
    }

    private static Type GetUnderlyingType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return type.GetGenericArguments()[0];
        }

        if (type.IsArray)
        {
            return type.GetElementType()!;
        }

        if (type.IsGenericType)
        {
            var args = type.GetGenericArguments();
            foreach (var arg in args)
            {
                if (IsLibraryType(arg))
                {
                    return arg;
                }
            }
        }

        return type;
    }

    private static bool IsLibraryType(Type type)
    {
        return type.Assembly == typeof(ObservationDataset).Assembly;
    }
}
