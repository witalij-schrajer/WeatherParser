using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Entities;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Verifies that this test project consumes the real built NuGet package rather than
/// a project/source reference, and that the package does not leak non-library concerns.
/// </summary>
[TestClass]
public sealed class PackageConsumptionTests
{
    [TestMethod]
    public void Deps_file_declares_the_library_as_a_restored_package_not_a_project_reference()
    {
        string depsFilePath = Path.Combine(
            AppContext.BaseDirectory,
            $"{Assembly.GetExecutingAssembly().GetName().Name}.deps.json");

        File.Exists(depsFilePath).Should().BeTrue("the deps file must be produced for the test host");

        using JsonDocument deps = JsonDocument.Parse(File.ReadAllText(depsFilePath));
        JsonElement libraries = deps.RootElement.GetProperty("libraries");

        JsonElement packageEntry = libraries.EnumerateObject()
            .Single(property => property.Name.StartsWith("WeatherParser.Parsing/", StringComparison.Ordinal))
            .Value;

        // A project reference would be recorded with "type": "project" and no package hash;
        // only a real PackageReference produces "type": "package" with a nupkg hash path.
        packageEntry.GetProperty("type").GetString().Should().Be("package");
        packageEntry.TryGetProperty("hashPath", out JsonElement hashPath).Should().BeTrue();
        hashPath.GetString().Should().EndWith(".nupkg.sha512");
    }

    [TestMethod]
    public void Package_exposes_only_the_expected_library_assembly()
    {
        Assembly assembly = typeof(GeoLocation).Assembly;

        assembly.GetName().Name.Should().Be("WeatherParser.Parsing");

        // The package must not carry any CLI or broker-transport assembly alongside the library.
        assembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .Should().NotContain(name => name != null &&
                (name.Contains("Cli", StringComparison.OrdinalIgnoreCase) ||
                 name.Contains("Mqtt", StringComparison.OrdinalIgnoreCase)));
    }
}
