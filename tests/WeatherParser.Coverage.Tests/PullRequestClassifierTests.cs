using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class PullRequestClassifierTests
{
    private MockRepository _mockRepository = null!;
    private PullRequestClassifier _pullRequestClassifier = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRepository = new MockRepository(MockBehavior.Strict);
        _pullRequestClassifier = new PullRequestClassifier();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockRepository.VerifyAll();
    }

    [TestMethod]
    [TestCategory("T-P1-23")]
    [DataRow("README.md;docs/x.md", PullRequestCategory.DocumentationOnly)]
    [DataRow("README.md;src/WeatherParser.Parsing/WeatherParser.Parsing.csproj", PullRequestCategory.Code)]
    [DataRow("Directory.Packages.props", PullRequestCategory.Code)]
    [DataRow("global.json", PullRequestCategory.Code)]
    [DataRow("coverage.runsettings", PullRequestCategory.Code)]
    [DataRow("coverage-exclusions.json", PullRequestCategory.Code)]
    [DataRow(".github/workflows/ci.yml", PullRequestCategory.Code)]
    [DataRow("src/Foo.cs", PullRequestCategory.Code)]
    [DataRow("DOCS/README.MD", PullRequestCategory.DocumentationOnly)]
    public void Classify_ChangedFiles_ExpectedResult(string changedFiles, PullRequestCategory expectedCategory)
    {
        var paths = changedFiles.Split(';');
        var changes = new ChangedFileList(paths);

        var result = _pullRequestClassifier.Classify(changes);

        result.Should().Be(expectedCategory);
    }

    [TestMethod]
    [TestCategory("T-P1-23")]
    public void Classify_EmptyList_ExpectedResult()
    {
        var changes = new ChangedFileList(new List<string>());

        var result = _pullRequestClassifier.Classify(changes);

        result.Should().Be(PullRequestCategory.Code);
    }

    [TestMethod]
    [TestCategory("T-P1-23")]
    public void Classify_FromFixtureFiles_ExpectedResult()
    {
        var docsOnlyContent = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "changes-docs-only.txt"));
        var changesDocsOnly = new ChangedFileList(docsOnlyContent.Split('\n', System.StringSplitOptions.RemoveEmptyEntries));

        var resultDocsOnly = _pullRequestClassifier.Classify(changesDocsOnly);
        resultDocsOnly.Should().Be(PullRequestCategory.DocumentationOnly);

        var docsAndCsprojContent = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "changes-docs-and-csproj.txt"));
        var changesDocsAndCsproj = new ChangedFileList(docsAndCsprojContent.Split('\n', System.StringSplitOptions.RemoveEmptyEntries));

        var resultDocsAndCsproj = _pullRequestClassifier.Classify(changesDocsAndCsproj);
        resultDocsAndCsproj.Should().Be(PullRequestCategory.Code);

        var coverageConfigContent = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "changes-coverage-config.txt"));
        var changesCoverageConfig = new ChangedFileList(coverageConfigContent.Split('\n', System.StringSplitOptions.RemoveEmptyEntries));

        var resultCoverageConfig = _pullRequestClassifier.Classify(changesCoverageConfig);
        resultCoverageConfig.Should().Be(PullRequestCategory.Code);
    }
}
