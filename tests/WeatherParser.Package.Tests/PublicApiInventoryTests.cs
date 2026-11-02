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
    private IPublicApiListing _listing = null!;

    /// <summary>
    /// Initializes the test fixture.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _listing = new PublicApiListing();
    }

    private static readonly string FixtureAssembly = typeof(ApiFixture.ApiSample).Assembly.Location;
    private const string FixtureNamespace = "WeatherParser.Package.Tests.ApiFixture";

    /// <summary>
    /// Gets the baseline listing for the fixture.
    /// </summary>
    private static IReadOnlyList<string> FixtureBaseline()
    {
        return new[]
        {
            "F:WeatherParser.Package.Tests.ApiFixture.ApiColor.Green : WeatherParser.Package.Tests.ApiFixture.ApiColor [public const] = 1",
            "F:WeatherParser.Package.Tests.ApiFixture.ApiColor.Red : WeatherParser.Package.Tests.ApiFixture.ApiColor [public const] = 0",
            "F:WeatherParser.Package.Tests.ApiFixture.ApiColor.value__ : int [public]",
            "F:WeatherParser.Package.Tests.ApiFixture.ApiSample.Limit : int [public const] = 5",
            "F:WeatherParser.Package.Tests.ApiFixture.ApiSample.Tag : string [public static readonly]",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiBase..ctor() : void [protected]",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiBase.Draw() : void [protected abstract]",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiBase.get_Name() : string [public virtual]",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiSample..ctor() : void [public]",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Area() : int [public]",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Describe(string prefix, int times = 1) : string [public]",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Draw() : void [protected override]",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Pick``1<T: class>(!!0 value) : !!0 [public]",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Twice(int x) : int [public static]",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiSample.get_Count() : int [public]",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiSample.get_Label() : string [public]",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiSample.set_Count(int value) : void [public]",
            "M:WeatherParser.Package.Tests.ApiFixture.ApiSample.set_Label(string value) : void modreq(System.Runtime.CompilerServices.IsExternalInit) [public]",
            "M:WeatherParser.Package.Tests.ApiFixture.IApiShape.Area() : int [public abstract]",
            "P:WeatherParser.Package.Tests.ApiFixture.ApiBase.Name : string",
            "P:WeatherParser.Package.Tests.ApiFixture.ApiSample.Count : int",
            "P:WeatherParser.Package.Tests.ApiFixture.ApiSample.Label : string",
            "T:WeatherParser.Package.Tests.ApiFixture.ApiBase (public class abstract) : System.Object",
            "T:WeatherParser.Package.Tests.ApiFixture.ApiColor (public enum) : System.Enum",
            "T:WeatherParser.Package.Tests.ApiFixture.ApiSample (public class sealed) : WeatherParser.Package.Tests.ApiFixture.ApiBase, WeatherParser.Package.Tests.ApiFixture.IApiShape",
            "T:WeatherParser.Package.Tests.ApiFixture.IApiShape (public interface)"
        }.OrderBy(l => l, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Tests that the fixture's API listing matches the known baseline.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void PublicApiInventory_Fixture_MatchesKnownListing()
    {
        var actual = _listing.Generate(FixtureAssembly, FixtureNamespace);
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
        var expected = baseline.Where(l => !l.Contains("Twice(int x)")).ToList();
        var actual = _listing.Generate(FixtureAssembly, FixtureNamespace);
        var (added, removed) = _listing.Compare(expected, actual);
        added.Should().ContainSingle().Which.Should().Be("M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Twice(int x) : int [public static]");
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
        var expected = baseline.Concat(new[] { "M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Reset() : void [public]" }).ToList();
        var actual = _listing.Generate(FixtureAssembly, FixtureNamespace);
        var (added, removed) = _listing.Compare(expected, actual);
        added.Should().BeEmpty();
        removed.Should().ContainSingle().Which.Should().Be("M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Reset() : void [public]");
    }

    /// <summary>
    /// Tests that changing a method signature is detected.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void PublicApiInventory_ChangedSignature_Detected()
    {
        var baseline = FixtureBaseline();
        var expected = baseline.Select(l => l.Contains("Describe(string prefix, int times = 1)") ? "M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Describe(string prefix, int times) : string [public]" : l).ToList();
        var actual = _listing.Generate(FixtureAssembly, FixtureNamespace);
        var (added, removed) = _listing.Compare(expected, actual);
        added.Should().ContainSingle().Which.Should().Be("M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Describe(string prefix, int times = 1) : string [public]");
        removed.Should().ContainSingle().Which.Should().Be("M:WeatherParser.Package.Tests.ApiFixture.ApiSample.Describe(string prefix, int times) : string [public]");
    }

    /// <summary>
    /// Tests that init and set are distinct.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void PublicApiInventory_InitAndSetAreDistinct()
    {
        var actual = _listing.Generate(FixtureAssembly, FixtureNamespace);
        var setCountLine = actual.Single(l => l.Contains("set_Count"));
        var setLabelLine = actual.Single(l => l.Contains("set_Label"));

        setLabelLine.Should().Contain("modreq(");
        setCountLine.Should().NotContain("modreq(");
    }

    /// <summary>
    /// Tests that constraints and constants are listed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void PublicApiInventory_ConstraintAndConstantAreListed()
    {
        var actual = _listing.Generate(FixtureAssembly, FixtureNamespace);
        var pickLine = actual.Single(l => l.Contains("Pick``1<T: class>"));
        var limitLine = actual.Single(l => l.Contains("Limit : int [public const] = 5"));

        pickLine.Should().Contain("T: class");
        limitLine.Should().Contain("= 5");
    }

    /// <summary>
    /// Tests that the packed assembly's API matches the checked-in listing.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void PublicApi_PackedAssembly_EqualsCheckedInListing()
    {
        var assemblyPath = Path.Combine(PackageArtifacts.PackageRoot, "lib", "net10.0", "WeatherParser.Parsing.dll");
        var actual = _listing.Generate(assemblyPath);
        var (added, removed) = _listing.Compare(_listing.ReadListing(_listing.ListingPath), actual);

        if (added.Any() || removed.Any())
        {
            var outputPath = Path.Combine(AppContext.BaseDirectory, "PublicApi.actual.txt");
            File.WriteAllText(outputPath, string.Join("\n", actual));
        }

        added.Should().BeEmpty("public members missing from PublicApi.txt (the complete generated listing is in PublicApi.actual.txt of the test output)");
        removed.Should().BeEmpty("members listed in PublicApi.txt but absent from the packed assembly");
    }
}
