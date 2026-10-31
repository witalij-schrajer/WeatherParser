using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Verifies the public API inventory of the package against the checked-in listing (RL-04 A, T-P9-04).
/// </summary>
[TestClass]
public sealed class PublicApiInventoryTests
{
    private static readonly string FixtureAssembly = typeof(ApiFixture.ApiSample).Assembly.Location;
    private const string FixtureNamespace = "WeatherParser.Package.Tests.ApiFixture";

    /// <summary>
    /// Gets the baseline listing for the fixture.
    /// </summary>
    private static IReadOnlyList<string> FixtureBaseline()
    {
        return new[]
        {
            "F:WeatherParser.Package.Tests.ApiFixture.ApiColor.Green : WeatherParser.Package.Tests.ApiFixture.ApiColor",
            "F:WeatherParser.Package.Tests.ApiFixture.ApiColor.Red : WeatherParser.Package.Tests.ApiFixture.ApiColor",
            "F:WeatherParser.Package.Tests.ApiFixture.ApiColor.value__ : int",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiSample..ctor() : void",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Describe(string, int) : string",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiSample.get_Count() : int",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiSample.set_Count(int) : void",
            "T:WeatherParser.Package.Tests.ApiFixture.ApiColor",
            "T:WeatherParser.Package.Tests.ApiFixture.ApiSample"
        };
    }

    /// <summary>
    /// Tests that the fixture's API listing matches the known baseline.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void PublicApiInventory_Fixture_MatchesKnownListing()
    {
        var actual = PublicApiListing.Generate(FixtureAssembly, FixtureNamespace);
        actual.Should().Equal(FixtureBaseline());
    }

    /// <summary>
    /// Tests that adding a member is detected.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void PublicApiInventory_AddedMember_Detected()
    {
        var baseline = FixtureBaseline();
        var expected = baseline.Where(l => !l.Contains("Describe(string, int)")).ToList();
        var actual = PublicApiListing.Generate(FixtureAssembly, FixtureNamespace);
        var (added, removed) = PublicApiListing.Compare(expected, actual);
        added.Should().ContainSingle().Which.Should().Be("M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Describe(string, int) : string");
        removed.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that removing a member is detected.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void PublicApiInventory_RemovedMember_Detected()
    {
        var baseline = FixtureBaseline();
        var expected = baseline.Concat(new[] { "M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Reset() : void" }).ToList();
        var actual = PublicApiListing.Generate(FixtureAssembly, FixtureNamespace);
        var (added, removed) = PublicApiListing.Compare(expected, actual);
        added.Should().BeEmpty();
        removed.Should().ContainSingle().Which.Should().Be("M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Reset() : void");
    }

    /// <summary>
    /// Tests that changing a method signature is detected.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void PublicApiInventory_ChangedSignature_Detected()
    {
        var baseline = FixtureBaseline();
        var expected = baseline.Select(l => l.Contains("Describe(string, int)") ? "M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Describe(string) : string" : l).ToList();
        var actual = PublicApiListing.Generate(FixtureAssembly, FixtureNamespace);
        var (added, removed) = PublicApiListing.Compare(expected, actual);
        added.Should().ContainSingle().Which.Should().Be("M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Describe(string, int) : string");
        removed.Should().ContainSingle().Which.Should().Be("M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Describe(string) : string");
    }

    /// <summary>
    /// Tests that the packed assembly's API matches the checked-in listing.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void PublicApi_PackedAssembly_EqualsCheckedInListing()
    {
        var assemblyPath = Path.Combine(PackageArtifacts.PackageRoot, "lib", "net10.0", "WeatherParser.Parsing.dll");
        var actual = PublicApiListing.Generate(assemblyPath);
        var (added, removed) = PublicApiListing.Compare(PublicApiListing.ReadListing(PublicApiListing.ListingPath), actual);

        if (added.Any() || removed.Any())
        {
            var outputPath = Path.Combine(AppContext.BaseDirectory, "PublicApi.actual.txt");
            File.WriteAllText(outputPath, string.Join("\n", actual));
        }

        added.Should().BeEmpty("public members missing from PublicApi.txt (the complete generated listing is in PublicApi.actual.txt of the test output)");
        removed.Should().BeEmpty("members listed in PublicApi.txt but absent from the packed assembly");
    }
}
