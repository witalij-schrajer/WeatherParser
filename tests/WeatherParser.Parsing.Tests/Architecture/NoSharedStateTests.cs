using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Export;

namespace WeatherParser.Parsing.Tests.Architecture;

[TestClass]
public sealed class NoSharedStateTests
{
    private static readonly Assembly Library = typeof(ServiceCollectionExtensions).Assembly;
    private static readonly HashSet<Type> AllowedTypes = new()
    {
        typeof(string),
        typeof(bool),
        typeof(char),
        typeof(byte),
        typeof(sbyte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(decimal),
        typeof(TimeSpan),
        typeof(DateTime),
        typeof(Regex)
    };

    private static ServiceProvider BuildValidated() => new ServiceCollection().AddWeatherParsing().AddTestConsumerServices().BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    [TestMethod]
    public void TwoContainers_ProduceIndependentInstances()
    {
        using var first = BuildValidated();
        using var second = BuildValidated();

        var serviceTypes = new ServiceCollection().AddWeatherParsing().Select(d => d.ServiceType).Where(t => t != typeof(IXmlExportSerializer));

        foreach (var type in serviceTypes)
        {
            var firstInstance = first.GetRequiredService(type);
            var secondInstance = second.GetRequiredService(type);
            firstInstance.Should().NotBeSameAs(secondInstance, type.Name);
            
            // Within one container, two resolutions should be the same instance
            var firstInstance2 = first.GetRequiredService(type);
            firstInstance.Should().BeSameAs(firstInstance2, type.Name);
        }
    }

    [TestMethod]
    public async Task TwoContainers_DoNotShareSerializerCache()
    {
        using var first = BuildValidated();
        using var second = BuildValidated();

        var serializer1 = first.GetRequiredService<IXmlExportSerializer>();
        var serializer2 = second.GetRequiredService<IXmlExportSerializer>();

        var dataset = ObservationTestData.ValidDataset();

        using var stream1 = new MemoryStream();
        using var stream2 = new MemoryStream();

        await serializer1.SerializeAsync(dataset, stream1, CancellationToken.None);
        await serializer2.SerializeAsync(dataset, stream2, CancellationToken.None);

        var fieldInfo = typeof(XmlExportSerializer).GetField("_serializers", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var cache1 = fieldInfo.GetValue(serializer1);
        var cache2 = fieldInfo.GetValue(serializer2);

        cache1.Should().NotBeNull();
        cache2.Should().NotBeNull();
        cache1.Should().NotBeSameAs(cache2);
    }

    [TestMethod]
    public void LibraryTypes_DeclareNoStaticState()
    {
        var violations = new List<string>();

        foreach (var type in Library.GetTypes())
        {
            // Skip compiler infrastructure types
            if (type.IsDefined(typeof(CompilerGeneratedAttribute), false) && type.Name.StartsWith("<", StringComparison.Ordinal))
                continue;

            // Skip Microsoft.CodeCoverage static instrumentation types
            if (IsCoverageInstrumentation(type))
                continue;

            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            foreach (var field in fields)
            {
                // Skip constants and enum values
                if (field.IsLiteral || (type.IsEnum && field.FieldType.IsEnum))
                    continue;

                // Check if it's an init-only field with allowed type
                if (field.IsInitOnly && (field.FieldType.IsEnum || AllowedTypes.Contains(field.FieldType)))
                    continue;

                violations.Add($"{type.FullName}.{field.Name} : {field.FieldType.Name}");
            }
        }

        violations.Should().BeEmpty();
    }

    /// <summary>
    /// Types that Microsoft.CodeCoverage static instrumentation injects while tests collect coverage;
    /// test tooling, not library code (contract C-109 amendment).
    /// </summary>
    private static bool IsCoverageInstrumentation(Type type) => type.Namespace is { } ns && (string.Equals(ns, "Microsoft.CodeCoverage.Instrumentation", StringComparison.Ordinal) || ns.StartsWith("Microsoft.CodeCoverage.Instrumentation.", StringComparison.Ordinal));
}
