using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Runs scripts/check-promotion.ps1 over synthetic Git repositories and artifact folders (RL-06 A, T-P9-05).
/// </summary>
[TestClass]
public sealed class CheckPromotionScriptTests
{
    private string _dir = null!;
    private string _candidate = null!;

    private const string Nupkg = "WeatherParser.Parsing.0.1.0.nupkg";
    private const string Snupkg = "WeatherParser.Parsing.0.1.0.snupkg";
    private const string CliZip = "WeatherParser.Cli.0.1.0.linux-arm64.zip";

    [TestInitialize]
    public void TestInitialize()
    {
        _dir = Path.Combine(Path.GetTempPath(), "wp-promo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        var repoDir = Path.Combine(_dir, "repo");
        Directory.CreateDirectory(repoDir);
        var artifactsDir = Path.Combine(_dir, "artifacts");
        Directory.CreateDirectory(artifactsDir);

        Git("init", "-q");
        Git("config", "user.email", "test@example.invalid");
        Git("config", "user.name", "Test");
        Git("config", "core.autocrlf", "false");

        Directory.CreateDirectory(Path.Combine(repoDir, "src"));
        Directory.CreateDirectory(Path.Combine(repoDir, "docs"));
        File.WriteAllText(Path.Combine(repoDir, "README.md"), "readme");
        File.WriteAllText(Path.Combine(repoDir, "src", "a.cs"), "class A {}");
        File.WriteAllText(Path.Combine(repoDir, "Directory.Build.props"), "<Project />");
        File.WriteAllText(Path.Combine(repoDir, "docs", "notes.md"), "notes");
        File.WriteAllText(Path.Combine(repoDir, "docs", "data.json"), "{}");

        _candidate = Commit("candidate");

        var nupkgPath = Path.Combine(artifactsDir, Nupkg);
        File.WriteAllText(nupkgPath, "package bytes");
        var nupkgHash = ComputeSha256(nupkgPath);

        var snupkgPath = Path.Combine(artifactsDir, Snupkg);
        File.WriteAllText(snupkgPath, "symbols bytes");
        var snupkgHash = ComputeSha256(snupkgPath);

        var cliZipPath = Path.Combine(artifactsDir, CliZip);
        File.WriteAllText(cliZipPath, "cli bytes");
        var cliZipHash = ComputeSha256(cliZipPath);

        var sb = new StringBuilder();
        sb.Append(nupkgHash).Append("  ").Append(Nupkg).Append('\n');
        sb.Append(snupkgHash).Append("  ").Append(Snupkg).Append('\n');
        sb.Append(cliZipHash).Append("  ").Append(CliZip).Append('\n');
        File.WriteAllText(Path.Combine(artifactsDir, "SHA256SUMS"), sb.ToString());

        var json = "{";
        json += $"\"commit\":\"{_candidate}\"";
        json += ",\"version\":\"0.1.0\"";
        json += ",\"runtimeIdentifier\":\"linux-arm64\"";
        json += $",\"artifacts\":[{{\"name\":\"{Nupkg}\",\"sha256\":\"{nupkgHash}\"}},{{\"name\":\"{Snupkg}\",\"sha256\":\"{snupkgHash}\"}},{{\"name\":\"{CliZip}\",\"sha256\":\"{cliZipHash}\"}}]}}";
        File.WriteAllText(Path.Combine(artifactsDir, "evidence.json"), json);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_dir))
        {
            foreach (var f in Directory.EnumerateFiles(_dir, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(f, FileAttributes.Normal);
            }
            Directory.Delete(_dir, true);
        }
    }

    private string Git(params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = Path.Combine(_dir, "repo"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo)!;
        
        var stderrTask = process.StandardError.ReadToEndAsync();
        var stdout = process.StandardOutput.ReadToEnd();
        var output = stdout + stderrTask.Result;

        if (!process.WaitForExit(60000))
        {
            process.Kill(true);
            Assert.Fail("git did not finish within 60 s");
        }
        process.ExitCode.Should().Be(0, $"git {string.Join(" ", args)} failed with exit code {process.ExitCode}");
        return output.Trim();
    }

    private string Commit(string message, params (string Path, string? Content)[] changes)
    {
        foreach (var (path, content) in changes)
        {
            var fullPath = Path.Combine(_dir, "repo", path);
            if (content is null)
                       {
                File.Delete(fullPath);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, content);
            }
        }

        Git("add", "-A");
        Git("commit", "-q", "-m", message);
        return Git("rev-parse", "HEAD");
    }

    private static string ComputeSha256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private (int ExitCode, string Output) Run(string candidate, string target)
    {
        var scriptPath = Path.Combine(PackageArtifacts.RepositoryRoot, "scripts", "check-promotion.ps1");
        var startInfo = new ProcessStartInfo
        {
            FileName = "pwsh",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);

        startInfo.ArgumentList.Add("-Repository");
        startInfo.ArgumentList.Add(Path.Combine(_dir, "repo"));

        startInfo.ArgumentList.Add("-Candidate");
        startInfo.ArgumentList.Add(candidate);

        startInfo.ArgumentList.Add("-Target");
        startInfo.ArgumentList.Add(target);

        startInfo.ArgumentList.Add("-ArtifactDirectory");
        startInfo.ArgumentList.Add(Path.Combine(_dir, "artifacts"));

        using var process = Process.Start(startInfo)!;
        
        var stderrTask = process.StandardError.ReadToEndAsync();
        var stdout = process.StandardOutput.ReadToEnd();
        var output = stdout + stderrTask.Result;

        if (!process.WaitForExit(60000))
        {
            process.Kill(true);
            Assert.Fail("check-promotion.ps1 did not finish within 60 s");
        }
        return (process.ExitCode, output);
    }

    /// <summary>
    /// Tests that promotion passes when only documentation files are changed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_SameInputs_DocsOnlyDifference_Passes()
    {
        var target = Commit("docs", ("docs/notes.md", "changed"), ("CHANGELOG.md", "log"));
        var (exitCode, output) = Run(_candidate, target);
        exitCode.Should().Be(0);
        output.Should().Contain("PROMOTABLE");
    }

    /// <summary>
    /// Tests that promotion fails when a source file is changed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_ChangedSourceFile_Fails()
    {
        var target = Commit("src", ("src/a.cs", "class A { }"));
        var (exitCode, output) = Run(_candidate, target);
        exitCode.Should().Be(4);
        output.Should().Contain("CHANGED-INPUT src/a.cs");
    }

    /// <summary>
    /// Tests that promotion fails when the packed README.md is changed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_ChangedPackedReadme_Fails()
    {
        var target = Commit("readme", ("README.md", "changed readme"));
        var (exitCode, output) = Run(_candidate, target);
        exitCode.Should().Be(4);
        output.Should().Contain("CHANGED-INPUT README.md");
    }

    /// <summary>
    /// Tests that promotion fails when Directory.Build.props is changed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_ChangedBuildProps_Fails()
    {
        var target = Commit("props", ("Directory.Build.props", "<Project><PropertyGroup><Test>1</Test></PropertyGroup></Project>"));
        var (exitCode, output) = Run(_candidate, target);
        exitCode.Should().Be(4);
        output.Should().Contain("CHANGED-INPUT Directory.Build.props");
    }

    /// <summary>
    /// Tests that promotion fails when a non-Markdown file under docs is changed.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_ChangedNonMarkdownUnderDocs_Fails()
    {
        var target = Commit("data", ("docs/data.json", "{\"changed\":true}"));
        var (exitCode, output) = Run(_candidate, target);
        exitCode.Should().Be(4);
        output.Should().Contain("CHANGED-INPUT docs/data.json");
    }

    /// <summary>
    /// Tests that promotion fails when an artifact's checksum is incorrect.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_ModifiedArtifact_ChecksumFails()
    {
        var target = Commit("docs", ("docs/notes.md", "changed"));
        var pkgPath = Path.Combine(_dir, "artifacts", Nupkg);
        File.WriteAllText(pkgPath, "tampered");
        var (exitCode, output) = Run(_candidate, target);
        exitCode.Should().Be(5);
        output.Should().Contain("CHECKSUM-MISMATCH " + Nupkg);
    }

    /// <summary>
    /// Tests that promotion fails when an artifact is missing.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_MissingArtifact_Fails()
    {
        var target = Commit("docs", ("docs/notes.md", "changed"));
        var pkgPath = Path.Combine(_dir, "artifacts", Nupkg);
        File.Delete(pkgPath);
        var (exitCode, output) = Run(_candidate, target);
        exitCode.Should().Be(5);
        output.Should().Contain("MISSING-ARTIFACT " + Nupkg);
    }

    /// <summary>
    /// Tests that promotion fails when evidence.json names a different commit.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_EvidenceForOtherCommit_Fails()
    {
        var target = Commit("docs", ("docs/notes.md", "changed"));
        var nupkgHash = ComputeSha256(Path.Combine(_dir, "artifacts", Nupkg));
        var snupkgHash = ComputeSha256(Path.Combine(_dir, "artifacts", Snupkg));
        var cliZipHash = ComputeSha256(Path.Combine(_dir, "artifacts", CliZip));
        var evidencePath = Path.Combine(_dir, "artifacts", "evidence.json");
        File.WriteAllText(evidencePath, $"{{\"commit\":\"0000000000000000000000000000000000000000\",\"version\":\"0.1.0\",\"runtimeIdentifier\":\"linux-arm64\",\"artifacts\":[{{\"name\":\"{Nupkg}\",\"sha256\":\"{nupkgHash}\"}},{{\"name\":\"{Snupkg}\",\"sha256\":\"{snupkgHash}\"}},{{\"name\":\"{CliZip}\",\"sha256\":\"{cliZipHash}\"}}]}}");
        var (exitCode, output) = Run(_candidate, target);
        exitCode.Should().Be(5);
        output.Should().Contain("EVIDENCE-COMMIT-MISMATCH");
    }

    /// <summary>
    /// Tests that promotion fails when the candidate is not an ancestor of the target.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_CandidateNotAncestorOfTarget_Fails()
    {
        Git("checkout", "-q", "--orphan", "other");
        Git("rm", "-rq", "--cached", ".");
        var other = Commit("unrelated", ("x.txt", "x"));
        var (exitCode, output) = Run(_candidate, other);
        exitCode.Should().Be(3);
        output.Should().Contain("NOT-AN-ANCESTOR");
    }

    /// <summary>
    /// Tests that promotion fails when an unknown commit is specified.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_UnknownCommit_ExitTwo()
    {
        var (exitCode, output) = Run(_candidate, "1111111111111111111111111111111111111111");
        exitCode.Should().Be(2);
    }
}
