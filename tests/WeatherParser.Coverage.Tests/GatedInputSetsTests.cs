using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Coverage;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class GatedInputSetsTests
{
    private static readonly string[] Exclusions = { "*.Tests.dll" };
    private const string Target = "ed7fc7a03db2f5554c490451f918da9fc6828c84";

    private MockRepository _mockRepository = null!;
    private Mock<ICoverageReportReader> _reportReader = null!;
    private Mock<IBaselineMetadataReader> _baselineReader = null!;
    private Mock<IChangedFileListReader> _changesReader = null!;
    private Mock<IReconciliationReader> _reconciliationReader = null!;
    private CoverageGate _gate = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRepository = new(MockBehavior.Strict);
        _reportReader = _mockRepository.Create<ICoverageReportReader>();
        _baselineReader = _mockRepository.Create<IBaselineMetadataReader>();
        _changesReader = _mockRepository.Create<IChangedFileListReader>();
        _reconciliationReader = _mockRepository.Create<IReconciliationReader>();

        _gate = new CoverageGate(
            _reportReader.Object,
            _baselineReader.Object,
            _changesReader.Object,
            new ProvenanceAggregator(),
            new RuleEvaluator(),
            new BaselineComparabilityChecker(),
            new PullRequestClassifier(),
            _reconciliationReader.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockRepository.VerifyAll();
    }

    private void Arrange(long covered, long notCovered, bool withBaseline, params string[] changedPaths)
    {
        _reportReader
            .Setup(r => r.ReadAsync("report.xml", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CoverageReport(
                new[] { new ModuleCoverage("WeatherParser.dll", "id", "path", new BlockCounters(covered, notCovered)) },
                "native-xml"));

        _changesReader
            .Setup(r => r.ReadAsync("changes.txt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChangedFileList(changedPaths));

        if (withBaseline)
        {
            _baselineReader
                .Setup(r => r.ReadAsync("baseline.json", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BaselineMetadata(Target, "Microsoft.CodeCoverage", "18.11.0", "native-xml", Exclusions, new BlockCounters(2872, 45)));
        }
        else
        {
            _baselineReader
                .Setup(r => r.ReadAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((BaselineMetadata?)null);
        }
    }

    private static CoverageGateRequest CliRequest(bool bootstrap = false) => new(
        "report.xml",
        bootstrap ? null : "baseline.json",
        "changes.txt",
        bootstrap ? string.Empty : Target,
        "Microsoft.CodeCoverage",
        "18.11.0",
        Exclusions,
        bootstrap,
        GatedInputs: GatedInputSets.Cli);

    /// <summary>
    /// The library gate lists the amendment inputs for the library.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Library_ListsTheAmendmentInputs()
    {
        GatedInputSets.Library.Should().Equal("src/", "Directory.Build.props", "Directory.Packages.props", "global.json", "NuGet.Config", ".editorconfig", "coverage.runsettings");
    }

    /// <summary>
    /// The CLI gate lists the amendment inputs for the CLI.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void Cli_ListsTheAmendment2Inputs()
    {
        GatedInputSets.Cli.Should().Equal("examples/", "tests/WeatherParser.Cli.Tests/", "tests/WeatherParser.Cli.SignalHost/", "src/", "Directory.Build.props", "Directory.Packages.props", "global.json", "NuGet.Config", ".editorconfig", "coverage.runsettings");
    }

    /// <summary>
    /// The ForGate method returns the library set for a "library" gate name.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void ForGate_Library_ReturnsLibrarySet()
    {
        GatedInputSets.ForGate("library").Should().BeSameAs(GatedInputSets.Library);
    }

    /// <summary>
    /// The ForGate method returns the CLI set for a "cli" gate name.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void ForGate_Cli_ReturnsCliSet()
    {
        GatedInputSets.ForGate("cli").Should().BeSameAs(GatedInputSets.Cli);
    }

    /// <summary>
    /// The ForGate method returns null for an unknown gate name.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public void ForGate_UnknownName_ReturnsNull()
    {
        GatedInputSets.ForGate("parsing").Should().BeNull();
        GatedInputSets.ForGate("Library").Should().BeNull();
    }

    /// <summary>
    /// The CLI gate accepts equal coverage when no CLI input changed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task CliGate_CliInputsUnchanged_EqualCoverageAccepted()
    {
        Arrange(2872, 45, true, "tools/WeatherParser.Coverage/CoverageGate.cs", "docs/p9/P9_VERIFICATION.md");
        var result = await _gate.EvaluateAsync(CliRequest(), CancellationToken.None);
        result.Passed.Should().BeTrue();
    }

    /// <summary>
    /// The CLI gate rejects decreased coverage when no CLI input changed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task CliGate_CliInputsUnchanged_DecreaseRejected()
    {
        Arrange(2871, 46, true, "tools/WeatherParser.Coverage/CoverageGate.cs");
        var result = await _gate.EvaluateAsync(CliRequest(), CancellationToken.None);
        result.Passed.Should().BeFalse();
    }

    /// <summary>
    /// The CLI gate requires strict improvement when an example file changed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task CliGate_ExampleFileChanged_StrictImprovementRequired()
    {
        Arrange(2872, 45, true, "examples/WeatherParser.Cli/Program.cs");
        var result = await _gate.EvaluateAsync(CliRequest(), CancellationToken.None);
        result.Passed.Should().BeFalse();
    }

    /// <summary>
    /// The CLI gate requires strict improvement when a source file changed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task CliGate_SrcChanged_StrictImprovementRequired()
    {
        Arrange(2872, 45, true, "src/WeatherParser.Parsing/WeatherParser.Parsing.csproj");
        var result = await _gate.EvaluateAsync(CliRequest(), CancellationToken.None);
        result.Passed.Should().BeFalse();
    }

    /// <summary>
    /// The CLI gate requires strict improvement when a CLI test file changed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task CliGate_CliTestChanged_StrictImprovementRequired()
    {
        Arrange(2872, 45, true, "tests/WeatherParser.Cli.Tests/ProgramTests.cs");
        var result = await _gate.EvaluateAsync(CliRequest(), CancellationToken.None);
        result.Passed.Should().BeFalse();
    }

    /// <summary>
    /// The CLI gate requires strict improvement when a root build input changed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task CliGate_RootBuildInputChanged_StrictImprovementRequired()
    {
        Arrange(2872, 45, true, "Directory.Packages.props");
        var result = await _gate.EvaluateAsync(CliRequest(), CancellationToken.None);
        result.Passed.Should().BeFalse();
    }

    /// <summary>
    /// The CLI gate accepts improved coverage when an example file changed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task CliGate_ExampleFileChanged_ImprovementAccepted()
    {
        Arrange(2873, 44, true, "examples/WeatherParser.Cli/Program.cs");
        var result = await _gate.EvaluateAsync(CliRequest(), CancellationToken.None);
        result.Passed.Should().BeTrue();
    }

    /// <summary>
    /// The CLI gate accepts equal coverage when only documentation changed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task CliGate_DocumentationOnly_EqualCoverageAccepted()
    {
        Arrange(2872, 45, true, "README.md");
        var result = await _gate.EvaluateAsync(CliRequest(), CancellationToken.None);
        result.Passed.Should().BeTrue();
    }

    /// <summary>
    /// The CLI gate accepts bootstrap with minimum coverage when no CLI assembly is targeted.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task CliGate_Bootstrap_TargetWithoutCliAssembly_MinimumOnly()
    {
        Arrange(2872, 45, false, "examples/WeatherParser.Cli/Program.cs");
        var result = await _gate.EvaluateAsync(CliRequest(bootstrap: true), CancellationToken.None);
        result.Passed.Should().BeTrue();
    }

    /// <summary>
    /// The library gate accepts equal coverage when no gated input changed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task LibraryGate_ExampleChange_GatedInputsUnchanged()
    {
        Arrange(2872, 45, true, "examples/WeatherParser.Cli/Program.cs");
        var request = new CoverageGateRequest(
            "report.xml",
            "baseline.json",
            "changes.txt",
            Target,
            "Microsoft.CodeCoverage",
            "18.11.0",
            Exclusions,
            false,
            GatedInputs: GatedInputSets.Library);
        var result = await _gate.EvaluateAsync(request, CancellationToken.None);
        result.Passed.Should().BeTrue();
    }
}
