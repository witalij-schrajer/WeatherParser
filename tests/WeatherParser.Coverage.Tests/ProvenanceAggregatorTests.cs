using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Coverage;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class ProvenanceAggregatorTests
{
    private MockRepository _mockRepository = null!;
    private ProvenanceAggregator _provenanceAggregator = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRepository = new MockRepository(MockBehavior.Strict);
        _provenanceAggregator = new ProvenanceAggregator();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockRepository.VerifyAll();
    }

    [TestMethod]
    [TestCategory("T-P1-17")]
    [TestCategory("T-P1-24")]
    public void Aggregate_ExcludedAssembly_ExpectedResult()
    {
        // Arrange
        var report = new CoverageReport(
            new List<ModuleCoverage>
            {
                new ModuleCoverage("WeatherParser.Parsing.Tests.dll", "ID1", "path1", new BlockCounters(57, 33)),
                new ModuleCoverage("WeatherParser.Parsing.dll", "ID2", "path2", new BlockCounters(10, 0)),
            },
            "native-xml");

        var exclusions = new List<string> { "*.Tests.dll" };

        // Act
        var result = _provenanceAggregator.Aggregate(report, exclusions);

        // Assert
        Assert.AreEqual(10L, result.Covered);
        Assert.AreEqual(0L, result.NotCovered);
    }

    [TestMethod]
    [TestCategory("T-P1-17")]
    [TestCategory("T-P1-24")]
    public void Aggregate_TwoProductionModules_ExpectedResult()
    {
        // Arrange
        var report = new CoverageReport(
            new List<ModuleCoverage>
            {
                new ModuleCoverage("WeatherParser.Parsing.dll", "ID1", "path1", new BlockCounters(10, 0)),
                new ModuleCoverage("WeatherParser.Parsing.Tests.dll", "ID2", "path2", new BlockCounters(57, 33)),
                new ModuleCoverage("WeatherParser.Another.dll", "ID3", "path3", new BlockCounters(30, 60)),
            },
            "native-xml");

        var exclusions = new List<string> { "*.Tests.dll" };

        // Act
        var result = _provenanceAggregator.Aggregate(report, exclusions);

        // Assert
        Assert.AreEqual(40L, result.Covered);
        Assert.AreEqual(60L, result.NotCovered);
    }

    [TestMethod]
    [TestCategory("T-P1-17")]
    [TestCategory("T-P1-24")]
    public void Aggregate_SameNameDifferentModuleId_Throws()
    {
        // Arrange
        var report = new CoverageReport(
            new List<ModuleCoverage>
            {
                new ModuleCoverage("WeatherParser.Parsing.dll", "ID1", "path1", new BlockCounters(10, 0)),
                new ModuleCoverage("WeatherParser.Parsing.dll", "ID2", "path2", new BlockCounters(30, 60)),
            },
            "native-xml");

        var exclusions = new List<string> { "*.Tests.dll" };

        // Act & Assert
        Assert.ThrowsExactly<CoverageInputException>(() => _provenanceAggregator.Aggregate(report, exclusions));
    }

    [TestMethod]
    [TestCategory("T-P1-17")]
    [TestCategory("T-P1-24")]
    public void Aggregate_SameModuleIdDifferentName_Throws()
    {
        // Arrange
        var report = new CoverageReport(
            new List<ModuleCoverage>
            {
                new ModuleCoverage("WeatherParser.Parsing.dll", "ID1", "path1", new BlockCounters(10, 0)),
                new ModuleCoverage("WeatherParser.Another.dll", "ID1", "path2", new BlockCounters(30, 60)),
            },
            "native-xml");

        var exclusions = new List<string> { "*.Tests.dll" };

        // Act & Assert
        Assert.ThrowsExactly<CoverageInputException>(() => _provenanceAggregator.Aggregate(report, exclusions));
    }

    [TestMethod]
    [TestCategory("T-P1-17")]
    [TestCategory("T-P1-24")]
    public void Aggregate_ExclusionMatchingIsCaseInsensitive_ExpectedResult()
    {
        // Arrange
        var report = new CoverageReport(
            new List<ModuleCoverage>
            {
                new ModuleCoverage("WeatherParser.Parsing.Tests.DLL", "ID1", "path1", new BlockCounters(57, 33)),
                new ModuleCoverage("WeatherParser.Parsing.dll", "ID2", "path2", new BlockCounters(10, 0)),
            },
            "native-xml");

        var exclusions = new List<string> { "*.Tests.DLL" };

        // Act
        var result = _provenanceAggregator.Aggregate(report, exclusions);

        // Assert
        Assert.AreEqual(10L, result.Covered);
        Assert.AreEqual(0L, result.NotCovered);
    }

    [TestMethod]
    [TestCategory("T-P1-17")]
    [TestCategory("T-P1-24")]
    public void Aggregate_AllExcluded_Throws()
    {
        // Arrange
        var report = new CoverageReport(
            new List<ModuleCoverage>
            {
                new ModuleCoverage("WeatherParser.Parsing.Tests.dll", "ID1", "path1", new BlockCounters(57, 33)),
                new ModuleCoverage("WeatherParser.Another.Tests.dll", "ID2", "path2", new BlockCounters(10, 0)),
            },
            "native-xml");

        var exclusions = new List<string> { "*.Tests.dll" };

        // Act & Assert
        Assert.ThrowsExactly<CoverageInputException>(() => _provenanceAggregator.Aggregate(report, exclusions));
    }

    [TestMethod]
    [TestCategory("T-P1-17")]
    [TestCategory("T-P1-24")]
    public void Aggregate_NullReport_Throws()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => _provenanceAggregator.Aggregate(null!, new List<string>()));
    }

    [TestMethod]
    [TestCategory("T-P1-17")]
    [TestCategory("T-P1-24")]
    public void Aggregate_NullExclusions_Throws()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() => _provenanceAggregator.Aggregate(new CoverageReport(new List<ModuleCoverage>(), "native-xml"), null!));
    }
}
