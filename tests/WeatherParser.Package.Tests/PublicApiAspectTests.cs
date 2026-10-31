using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Verifies that the API inventory records the consumer-visible aspects of properties, nested types, variance and attributes (RL-04 A, T-P9-04).
/// </summary>
[TestClass]
public sealed class PublicApiAspectTests
{
    private IPublicApiListing _listing = null!;
    private const string A = "WeatherParser.Package.Tests.ApiAspects";

    /// <summary>
    /// Initializes the test fixture.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _listing = new PublicApiListing();
    }

    private IReadOnlyList<string> Listing() => _listing.Generate(typeof(ApiAspects.ApiRequired).Assembly.Location, A);

    /// <summary>
    /// Tests that required properties are marked.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void Inventory_RequiredProperty_IsMarked()
    {
        Listing().Should().Contain($"P:{A}.ApiRequired.Key : string @RequiredMember");
        Listing().Should().NotContain($"P:{A}.ApiNotRequired.Key : string @RequiredMember");
        Listing().Should().Contain($"P:{A}.ApiNotRequired.Key : string");
    }

    /// <summary>
    /// Tests that required types are marked.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void Inventory_RequiredType_IsMarked()
    {
        Listing().Should().Contain($"T:{A}.ApiRequired (public class sealed) : System.Object @RequiredMember");
        Listing().Should().NotContain($"T:{A}.ApiNotRequired (public class sealed) : System.Object @RequiredMember");
        Listing().Should().Contain($"T:{A}.ApiNotRequired (public class sealed) : System.Object");
    }

    /// <summary>
    /// Tests that nested type access is listed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void Inventory_NestedTypeAccess_IsListed()
    {
        Listing().Should().Contain($"T:{A}.ApiOuter.ApiProtectedInner (protected class) : System.Object");
        Listing().Should().Contain($"T:{A}.ApiOuter.ApiPublicInner (public class) : System.Object");
    }

    /// <summary>
    /// Tests that generic variance is listed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void Inventory_GenericVariance_IsListed()
    {
        Listing().Should().Contain($"T:{A}.IApiVariant`2<out TResult, in TArg> (public interface)");
    }

    /// <summary>
    /// Tests that obsolete members are listed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void Inventory_Obsolete_IsListed()
    {
        Listing().Should().Contain($"M:{A}.ApiObsolete.Old() : void [public] @Obsolete");
        Listing().Should().Contain($"M:{A}.ApiObsolete.Gone() : void [public] @Obsolete(error)");
        Listing().Should().Contain($"M:{A}.ApiObsolete.Current() : void [public]");
    }

    /// <summary>
    /// Tests that extension methods are listed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void Inventory_ExtensionMethod_IsListed()
    {
        Listing().Should().Contain($"M:{A}.ApiExtensions.Twice(int value) : int [public static] @Extension");
        Listing().Should().Contain($"T:{A}.ApiExtensions (public class static) : System.Object @Extension");
    }

    /// <summary>
    /// Tests that flags enums are listed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void Inventory_FlagsEnum_IsListed()
    {
        Listing().Should().Contain($"T:{A}.ApiFlags (public enum) : System.Enum @Flags");
    }
}
