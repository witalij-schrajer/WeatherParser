using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace WeatherParser.Coverage.Tests;

[TestClass]
public sealed class ReconciliationReaderTests
{
    private Mock<IFileReader> _mockFileReader = null!;
    private MockRepository _mockRepository = null!;
    private ReconciliationReader _reader = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mockRepository = new MockRepository(MockBehavior.Strict);
        _mockFileReader = _mockRepository.Create<IFileReader>();
        _reader = new ReconciliationReader(_mockFileReader.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mockRepository.VerifyAll();
    }

    private void SetupFile(string json)
    {
        _mockFileReader.Setup(fr => fr.Exists("reconciliation.json")).Returns(true);
        _mockFileReader.Setup(fr => fr.ReadAllTextAsync("reconciliation.json", It.IsAny<CancellationToken>())).Returns(Task.FromResult(json));
    }

    private static string Entry(
        string gate = "library",
        string targetCommit = "ed7fc7a03db2f5554c490451f918da9fc6828c84",
        string pullRequest = "18",
        string? headBranch = "features/develop/P9",
        string approvedOn = "2026-10-31",
        string record = "docs/p9/P9_RELEASE_CONTRACT.md#rl-20")
    {
        var headBranchJson = headBranch is null ? string.Empty : $"\"headBranch\": \"{headBranch}\",";
        return $$"""
            {
              "entries": [
                {
                  "gate": "{{gate}}",
                  "targetCommit": "{{targetCommit}}",
                  "pullRequest": {{pullRequest}},
                  "headRepository": "witalij-schrajer/WeatherParser",
                  {{headBranchJson}}
                  "approvedOn": "{{approvedOn}}",
                  "record": "{{record}}"
                }
              ]
            }
            """;
    }

    /// <summary>
    /// ReadAsync with a null path returns an empty list.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task ReadAsync_NullPath_ReturnsEmpty()
    {
        var result = await _reader.ReadAsync(null, default);
        result.Should().BeEmpty();
    }

    /// <summary>
    /// ReadAsync with an empty path returns an empty list.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task ReadAsync_EmptyPath_ReturnsEmpty()
    {
        var result = await _reader.ReadAsync(string.Empty, default);
        result.Should().BeEmpty();
    }

    /// <summary>
    /// ReadAsync with a missing file returns an empty list.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task ReadAsync_MissingFile_ReturnsEmpty()
    {
        _mockFileReader.Setup(fr => fr.Exists("reconciliation.json")).Returns(false);

        var result = await _reader.ReadAsync("reconciliation.json", default);
        result.Should().BeEmpty();
    }

    /// <summary>
    /// ReadAsync with a valid file returns the entries.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task ReadAsync_ValidFile_ReturnsEntries()
    {
        var json = $$"""
            {
              "entries": [
                {
                  "gate": "library",
                  "targetCommit": "ed7fc7a03db2f5554c490451f918da9fc6828c84",
                  "pullRequest": 18,
                  "headRepository": "witalij-schrajer/WeatherParser",
                  "headBranch": "features/develop/P9",
                  "approvedOn": "2026-10-31",
                  "record": "docs/p9/P9_RELEASE_CONTRACT.md#rl-20"
                },
                {
                  "gate": "cli",
                  "targetCommit": "ed7fc7a03db2f5554c490451f918da9fc6828c84",
                  "pullRequest": 18,
                  "headRepository": "witalij-schrajer/WeatherParser",
                  "headBranch": "features/develop/P9",
                  "approvedOn": "2026-10-31",
                  "record": "docs/p9/P9_RELEASE_CONTRACT.md#rl-20"
                }
              ]
            }
            """;

        SetupFile(json);

        var result = await _reader.ReadAsync("reconciliation.json", default);

        result.Should().HaveCount(2);
        result[0].Should().BeEquivalentTo(new ReconciliationEntry(
            "library",
            "ed7fc7a03db2f5554c490451f918da9fc6828c84",
            18,
            "witalij-schrajer/WeatherParser",
            "features/develop/P9",
            "2026-10-31",
            "docs/p9/P9_RELEASE_CONTRACT.md#rl-20"));
        result[1].Should().BeEquivalentTo(new ReconciliationEntry(
            "cli",
            "ed7fc7a03db2f5554c490451f918da9fc6828c84",
            18,
            "witalij-schrajer/WeatherParser",
            "features/develop/P9",
            "2026-10-31",
            "docs/p9/P9_RELEASE_CONTRACT.md#rl-20"));
    }

    /// <summary>
    /// ReadAsync with an empty entries array returns an empty list.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task ReadAsync_EmptyEntries_ReturnsEmpty()
    {
        var json = """{"entries": []}""";

        SetupFile(json);

        var result = await _reader.ReadAsync("reconciliation.json", default);
        result.Should().BeEmpty();
    }

    /// <summary>
    /// ReadAsync with invalid JSON throws CoverageInputException.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task ReadAsync_InvalidJson_Throws()
    {
        SetupFile("{ not json");

        Func<Task<IReadOnlyList<ReconciliationEntry>>> act = () => _reader.ReadAsync("reconciliation.json", default);
        await act.Should().ThrowAsync<CoverageInputException>().WithMessage("*reconciliation.json*");
    }

    /// <summary>
    /// ReadAsync with missing entries throws CoverageInputException.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task ReadAsync_EntriesMissing_Throws()
    {
        SetupFile("{}");

        Func<Task<IReadOnlyList<ReconciliationEntry>>> act = () => _reader.ReadAsync("reconciliation.json", default);
        await act.Should().ThrowAsync<CoverageInputException>().WithMessage("*reconciliation.json*");
    }

    /// <summary>
    /// ReadAsync with an entry missing headBranch throws CoverageInputException.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task ReadAsync_EntryWithoutHeadBranch_Throws()
    {
        var json = Entry(headBranch: null);
        SetupFile(json);

        Func<Task<IReadOnlyList<ReconciliationEntry>>> act = () => _reader.ReadAsync("reconciliation.json", default);
        await act.Should().ThrowAsync<CoverageInputException>().WithMessage("*reconciliation.json*");
    }

    /// <summary>
    /// ReadAsync with an unknown gate throws CoverageInputException.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task ReadAsync_UnknownGate_Throws()
    {
        var json = Entry(gate: "parsing");
        SetupFile(json);

        Func<Task<IReadOnlyList<ReconciliationEntry>>> act = () => _reader.ReadAsync("reconciliation.json", default);
        await act.Should().ThrowAsync<CoverageInputException>().WithMessage("*reconciliation.json*");
    }

    /// <summary>
    /// ReadAsync with a short targetCommit throws CoverageInputException.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task ReadAsync_ShortTargetCommit_Throws()
    {
        var json = Entry(targetCommit: "ed7fc7a03db2f5554c490451f918da9fc6828c8");
        SetupFile(json);

        Func<Task<IReadOnlyList<ReconciliationEntry>>> act = () => _reader.ReadAsync("reconciliation.json", default);
        await act.Should().ThrowAsync<CoverageInputException>().WithMessage("*reconciliation.json*");
    }

    /// <summary>
    /// ReadAsync with a non-hex targetCommit throws CoverageInputException.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task ReadAsync_NonHexTargetCommit_Throws()
    {
        var json = Entry(targetCommit: "zd7fc7a03db2f5554c490451f918da9fc6828c84");
        SetupFile(json);

        Func<Task<IReadOnlyList<ReconciliationEntry>>> act = () => _reader.ReadAsync("reconciliation.json", default);
        await act.Should().ThrowAsync<CoverageInputException>().WithMessage("*reconciliation.json*");
    }

    /// <summary>
    /// ReadAsync with a non-positive pullRequest throws CoverageInputException.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task ReadAsync_NonPositivePullRequest_Throws()
    {
        var json = Entry(pullRequest: "0");
        SetupFile(json);

        Func<Task<IReadOnlyList<ReconciliationEntry>>> act = () => _reader.ReadAsync("reconciliation.json", default);
        await act.Should().ThrowAsync<CoverageInputException>().WithMessage("*reconciliation.json*");
    }

    /// <summary>
    /// ReadAsync with an invalid approval date throws CoverageInputException.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task ReadAsync_InvalidApprovalDate_Throws()
    {
        var json = Entry(approvedOn: "31.10.2026");
        SetupFile(json);

        Func<Task<IReadOnlyList<ReconciliationEntry>>> act = () => _reader.ReadAsync("reconciliation.json", default);
        await act.Should().ThrowAsync<CoverageInputException>().WithMessage("*reconciliation.json*");
    }

    /// <summary>
    /// ReadAsync with an empty record throws CoverageInputException.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task ReadAsync_EmptyRecord_Throws()
    {
        var json = Entry(record: " ");
        SetupFile(json);

        Func<Task<IReadOnlyList<ReconciliationEntry>>> act = () => _reader.ReadAsync("reconciliation.json", default);
        await act.Should().ThrowAsync<CoverageInputException>().WithMessage("*reconciliation.json*");
    }

    /// <summary>
    /// ReadAsync with a null entry throws CoverageInputException.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-07")]
    public async Task ReadAsync_NullEntry_Throws()
    {
        SetupFile("{\"entries\": [null]}");

        Func<Task<IReadOnlyList<ReconciliationEntry>>> act = () => _reader.ReadAsync("reconciliation.json", default);
        await act.Should().ThrowAsync<CoverageInputException>().WithMessage("*reconciliation.json*entry 0*");
    }
}
