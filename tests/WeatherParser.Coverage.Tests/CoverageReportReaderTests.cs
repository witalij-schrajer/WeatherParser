using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class CoverageReportReaderTests
{
    private Mock<IFileReader> _mockFileReader = null!;
    private CoverageReportReader _coverageReportReader = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockFileReader = new MockRepository(MockBehavior.Strict).Create<IFileReader>();
        _coverageReportReader = new CoverageReportReader(_mockFileReader.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockFileReader.VerifyAll();
    }

    [TestMethod]
    [TestCategory("T-P1-13")]
    public async Task ReadAsync_MissingReport_ThrowsCoverageInputException()
    {
        // Arrange
        var path = "report.xml";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _coverageReportReader.ReadAsync(path, CancellationToken.None));
        exception.Message.Should().Contain("was not found");
    }

    [TestMethod]
    [TestCategory("T-P1-13")]
    public async Task ReadAsync_MalformedXml_ThrowsCoverageInputException()
    {
        // Arrange
        var path = "report.xml";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync(path, CancellationToken.None)).Returns(Task.FromResult("malformed xml"));

        // Act & Assert
        var exception = await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _coverageReportReader.ReadAsync(path, CancellationToken.None));
        exception.Message.Should().Contain("not well-formed XML");
    }

    [TestMethod]
    [TestCategory("T-P1-11")]
    public async Task ReadAsync_CoberturaFormat_ThrowsCoverageInputException()
    {
        // Arrange
        var path = "report.xml";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(true);
        var coberturaXml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "report-cobertura-branch-free.xml"));
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync(path, CancellationToken.None)).Returns(Task.FromResult(coberturaXml));

        // Act & Assert
        var exception = await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _coverageReportReader.ReadAsync(path, CancellationToken.None));
        exception.Message.Should().Contain("Cobertura reports are not accepted");
    }

    [TestMethod]
    [TestCategory("T-P1-11")]
    public async Task ReadAsync_ModuleWithoutBlockCounters_ThrowsCoverageInputException()
    {
        // Arrange
        var path = "report.xml";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(true);
        var xml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "report-no-block-counters.xml"));
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync(path, CancellationToken.None)).Returns(Task.FromResult(xml));

        // Act & Assert
        var exception = await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _coverageReportReader.ReadAsync(path, CancellationToken.None));
        exception.Message.Should().Contain("missing required block counters");
        exception.Message.Should().Contain("WeatherParser.Parsing.dll");
    }

    [TestMethod]
    [TestCategory("T-P1-24")]
    public async Task ReadAsync_ValidReport_ReturnsModulesWithCorrectCounters()
    {
        // Arrange
        var path = "report.xml";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(true);
        var xml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "report-valid.xml"));
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync(path, CancellationToken.None)).Returns(Task.FromResult(xml));

        // Act
        var result = await _coverageReportReader.ReadAsync(path, CancellationToken.None);

        // Assert
        result.Format.Should().Be("native-xml");
        result.Modules.Should().HaveCount(2);
        result.Modules[0].Name.Should().Be("WeatherParser.Parsing.Tests.dll");
        result.Modules[0].ModuleId.Should().Be("29AAB4ACC418694D9B42DCFA19D90BDAF1CD70F6");
        result.Modules[0].Path.Should().Be("WeatherParser.Parsing.Tests.dll");
        result.Modules[0].Blocks.Covered.Should().Be(189);
        result.Modules[0].Blocks.NotCovered.Should().Be(4);

        result.Modules[1].Name.Should().Be("WeatherParser.Parsing.dll");
        result.Modules[1].ModuleId.Should().Be("693E8A261D622D4ABDE874DDC2B54C2FB2F52FC0");
        result.Modules[1].Path.Should().Be("WeatherParser.Parsing.dll");
        result.Modules[1].Blocks.Covered.Should().Be(57);
        result.Modules[1].Blocks.NotCovered.Should().Be(33);
    }

    [TestMethod]
    [TestCategory("T-P1-24")]
    public async Task ReadAsync_DuplicateModuleReport_ReturnsBothModules()
    {
        // Arrange
        var path = "report.xml";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(true);
        var xml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "report-duplicate-module.xml"));
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync(path, CancellationToken.None)).Returns(Task.FromResult(xml));

        // Act
        var result = await _coverageReportReader.ReadAsync(path, CancellationToken.None);

        // Assert
        result.Format.Should().Be("native-xml");
        result.Modules.Should().HaveCount(2);
        result.Modules[0].Name.Should().Be("WeatherParser.Parsing.dll");
        result.Modules[0].ModuleId.Should().Be("693E8A261D622D4ABDE874DDC2B54C2FB2F52FC0");
        result.Modules[0].Blocks.Covered.Should().Be(57);
        result.Modules[0].Blocks.NotCovered.Should().Be(33);

        result.Modules[1].Name.Should().Be("WeatherParser.Parsing.dll");
        result.Modules[1].ModuleId.Should().Be("693E8A261D622D4ABDE874DDC2B54C2FB2F52FC0");
        result.Modules[1].Blocks.Covered.Should().Be(100);
        result.Modules[1].Blocks.NotCovered.Should().Be(0);
    }

    [TestMethod]
    [TestCategory("T-P1-11")]
    public async Task ReadAsync_ModuleWithNegativeBlockCounters_ThrowsCoverageInputException()
    {
        // Arrange
        var path = "report.xml";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync(path, CancellationToken.None))
            .Returns(Task.FromResult(
                """
                <?xml version="1.0" encoding="utf-8"?>
                <results>
                  <modules>
                    <module name="WeatherParser.Parsing.dll" path="WeatherParser.Parsing.dll" blocks_covered="-5" blocks_not_covered="33">
                    </module>
                  </modules>
                </results>
                """));

        // Act & Assert
        var exception = await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _coverageReportReader.ReadAsync(path, CancellationToken.None));
        exception.Message.Should().Contain("invalid block counter");
        exception.Message.Should().Contain("WeatherParser.Parsing.dll");
    }

    [TestMethod]
    [TestCategory("T-P1-11")]
    public async Task ReadAsync_ModuleWithInvalidBlockCounters_ThrowsCoverageInputException()
    {
        // Arrange
        var path = "report.xml";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync(path, CancellationToken.None))
            .Returns(Task.FromResult(
                """
                <?xml version="1.0" encoding="utf-8"?>
                <results>
                  <modules>
                    <module name="WeatherParser.Parsing.dll" path="WeatherParser.Parsing.dll" blocks_covered="abc" blocks_not_covered="33">
                    </module>
                  </modules>
                </results>
                """));

        // Act & Assert
        var exception = await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _coverageReportReader.ReadAsync(path, CancellationToken.None));
        exception.Message.Should().Contain("invalid block counter values");
        exception.Message.Should().Contain("WeatherParser.Parsing.dll");
    }

    [TestMethod]
    [TestCategory("T-P1-11")]
    public async Task ReadAsync_ModuleWithZeroDenominator_ReturnsZeroCounters()
    {
        // Arrange
        var path = "report.xml";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(true);
        var xml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "report-zero-denominator.xml"));
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync(path, CancellationToken.None)).Returns(Task.FromResult(xml));

        // Act
        var result = await _coverageReportReader.ReadAsync(path, CancellationToken.None);

        // Assert
        result.Modules.Should().HaveCount(1);
        result.Modules[0].Blocks.Covered.Should().Be(0);
        result.Modules[0].Blocks.NotCovered.Should().Be(0);
    }

    [TestMethod]
    [TestCategory("T-P1-24")]
    public async Task ReadAsync_Rounded100Report_ReturnsExactCounters()
    {
        // Arrange
        var path = "report.xml";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(true);
        var xml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "report-rounded-100.xml"));
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync(path, CancellationToken.None)).Returns(Task.FromResult(xml));

        // Act
        var result = await _coverageReportReader.ReadAsync(path, CancellationToken.None);

        // Assert
        result.Modules.Should().HaveCount(1);
        result.Modules[0].Blocks.Covered.Should().Be(99999);
        result.Modules[0].Blocks.NotCovered.Should().Be(1);
    }

    [TestMethod]
    [TestCategory("T-P1-11")]
    public async Task ReadAsync_MalformedReport_ThrowsCoverageInputException()
    {
        // Arrange
        var path = "report.xml";
        _mockFileReader.Setup(fr => fr.Exists(path)).Returns(true);
        var xml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "report-malformed.xml"));
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync(path, CancellationToken.None)).Returns(Task.FromResult(xml));

        // Act & Assert
        var exception = await Assert.ThrowsExactlyAsync<CoverageInputException>(() => _coverageReportReader.ReadAsync(path, CancellationToken.None));
        exception.Message.Should().Contain("not well-formed XML");
    }
}
