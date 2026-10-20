using System.Reflection;
using System.Runtime.Serialization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ExportEntitySerializableTests
{
    private IReadOnlyCollection<Type> _traversedTypes = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _traversedTypes = ExportGraphTraversal.CustomTypesReachableFrom(typeof(ObservationDataset), typeof(CdcStationList), typeof(MosmixLocationList), typeof(ForecastRun));
    }

    /// <summary>
    /// Tests that all custom types reachable from the export roots are serializable.
    /// </summary>
    [TestMethod]
    public void ExportTypes_ReachableFromExportRoots_AreSerializable()
    {
        var violations = new List<Type>();

        foreach (var type in _traversedTypes)
        {
            if (!type.IsDefined(typeof(SerializableAttribute), inherit: false))
            {
                violations.Add(type);
            }
        }

        violations.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that all custom types reachable from the export roots have a public parameterless constructor.
    /// </summary>
    [TestMethod]
    public void ExportTypes_ReachableFromExportRoots_HavePublicParameterlessConstructors()
    {
        var violations = new List<Type>();

        foreach (var type in _traversedTypes)
        {
            if (type.IsClass && !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) == null)
            {
                violations.Add(type);
            }
        }

        violations.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the traversal finds nested types reachable from the export roots.
    /// </summary>
    [TestMethod]
    public void ExportGraphTraversal_FromExportRoots_FindsNestedTypes()
    {
        var expectedTypes = new[]
        {
            typeof(ObservationDataset),
            typeof(CdcStationList),
            typeof(CdcStationListEntry),
            typeof(SearchParameters),
            typeof(MosmixLocationList),
            typeof(MosmixLocationListEntry),
            typeof(MosmixProductAvailability),
            typeof(MosmixProduct),
            typeof(AvailabilityState)
        };

        foreach (var expectedType in expectedTypes)
        {
            _traversedTypes.Should().Contain(expectedType);
        }
    }
}
