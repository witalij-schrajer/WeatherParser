using System;
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
        _traversedTypes = ExportGraphTraversal.CustomTypesReachableFrom(typeof(ObservationDataset));
    }

    /// <summary>
    /// Tests that all custom types reachable from ObservationDataset are serializable.
    /// </summary>
    [TestMethod]
    public void ExportTypes_ReachableFromObservationDataset_AreSerializable()
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
    /// Tests that all custom types reachable from ObservationDataset have a public parameterless constructor.
    /// </summary>
    [TestMethod]
    public void ExportTypes_ReachableFromObservationDataset_HavePublicParameterlessConstructors()
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
    /// Tests that the traversal finds nested types like ObservationDataset, ObservationMeasurements and MeasurementValue.
    /// </summary>
    [TestMethod]
    public void ExportGraphTraversal_FromObservationDataset_FindsNestedTypes()
    {
        var expectedTypes = new[]
        {
            typeof(ObservationDataset),
            typeof(ObservationMeasurements),
            typeof(MeasurementValue)
        };

        foreach (var expectedType in expectedTypes)
        {
            _traversedTypes.Should().Contain(expectedType);
        }
    }
}
