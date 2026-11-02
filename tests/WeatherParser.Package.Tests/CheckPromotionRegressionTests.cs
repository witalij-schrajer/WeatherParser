using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Regression tests for scripts/check-promotion.ps1: manifest and evidence validation, the complete release set and literal paths (RL-06 A, T-P9-05).
/// </summary>
[TestClass]
public sealed class CheckPromotionRegressionTests
{
    private string _dir = null!;
    private string _candidate = null!;
    private string _artifacts = null!;

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
        _artifacts = Path.Combine(_dir, "artifacts");
        Directory.CreateDirectory(_artifacts);

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

        var nupkgPath = Path.Combine(_artifacts, Nupkg);
        File.WriteAllText(nupkgPath, "package bytes");
        var nupkgHash = ComputeSha256(nupkgPath);

        var snupkgPath = Path.Combine(_artifacts, Snupkg);
        File.WriteAllText(snupkgPath, "symbols bytes");
        var snupkgHash = ComputeSha256(snupkgPath);

        var cliZipPath = Path.Combine(_artifacts, CliZip);
        File.WriteAllText(cliZipPath, "cli bytes");
        var cliZipHash = ComputeSha256(cliZipPath);

        WriteManifest(Nupkg, Snupkg, CliZip);
        WriteEvidence(_candidate, "0.1.0", "linux-arm64", Nupkg, Snupkg, CliZip);
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
        startInfo.ArgumentList.Add(_artifacts);

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

    private void WriteEvidence(string commit, string? version, string? runtimeIdentifier, params string[] names)
    {
        var artifacts = new StringBuilder();
        foreach (var name in names)
        {
            var filePath = Path.Combine(_artifacts, name);
            var hash = ComputeSha256(filePath);
            if (artifacts.Length > 0)
                artifacts.Append(",");
            artifacts.Append($"{{\"name\":\"{name}\",\"sha256\":\"{hash}\"}}");
        }

        var json = "{";
        json += $"\"commit\":\"{commit}\"";
        if (version != null)
            json += $",\"version\":\"{version}\"";
        if (runtimeIdentifier != null)
            json += $",\"runtimeIdentifier\":\"{runtimeIdentifier}\"";
        json += $",\"artifacts\":[{artifacts}]}}";

        File.WriteAllText(Path.Combine(_artifacts, "evidence.json"), json);
    }

    private void WriteManifest(params string[] names)
    {
        var sb = new StringBuilder();
        foreach (var name in names)
        {
            var filePath = Path.Combine(_artifacts, name);
            var hash = ComputeSha256(filePath);
            sb.Append(hash).Append("  ").Append(name).Append('\n');
        }
        File.WriteAllText(Path.Combine(_artifacts, "SHA256SUMS"), sb.ToString());
    }

    /// <summary>
    /// Tests that promotion fails with exit code 2 when SHA256SUMS is empty.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_EmptyManifest_ExitTwo()
    {
        File.WriteAllText(Path.Combine(_artifacts, "SHA256SUMS"), "");
        var (exitCode, output) = Run(_candidate, _candidate);
        exitCode.Should().Be(2);
    }

    /// <summary>
    /// Tests that promotion fails with exit code 2 when SHA256SUMS has a malformed line.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_MalformedManifestLine_ExitTwo()
    {
        File.WriteAllText(Path.Combine(_artifacts, "SHA256SUMS"), "garbage\n");
        var (exitCode, output) = Run(_candidate, _candidate);
        exitCode.Should().Be(2);
    }

    /// <summary>
    /// Tests that promotion fails with exit code 2 when SHA256SUMS has an invalid hash syntax.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_InvalidHashSyntax_ExitTwo()
    {
        File.WriteAllText(Path.Combine(_artifacts, "SHA256SUMS"), "zz  " + Nupkg + "\n");
        var (exitCode, output) = Run(_candidate, _candidate);
        exitCode.Should().Be(2);
    }

    /// <summary>
    /// Tests that promotion fails with exit code 2 when SHA256SUMS has a duplicate entry.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_DuplicateManifestEntry_ExitTwo()
    {
        File.WriteAllText(Path.Combine(_artifacts, "SHA256SUMS"), "1234567890123456789012345678901234567890123456789012345678901234  " + Nupkg + "\n1234567890123456789012345678901234567890123456789012345678901234  " + Nupkg + "\n");
        var (exitCode, output) = Run(_candidate, _candidate);
        exitCode.Should().Be(2);
    }

    /// <summary>
    /// Tests that promotion fails with exit code 5 when SHA256SUMS omits an artifact listed in evidence.json.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_ManifestOmitsEvidenceArtifact_Fails()
    {
        var nupkgHash = ComputeSha256(Path.Combine(_artifacts, Nupkg));
        var snupkgHash = ComputeSha256(Path.Combine(_artifacts, Snupkg));
        var cliZipHash = ComputeSha256(Path.Combine(_artifacts, CliZip));
        var otherHash = new string('a', 64);
        WriteManifest(Nupkg, Snupkg, CliZip);
        File.WriteAllText(Path.Combine(_artifacts, "evidence.json"), $"{{\"commit\":\"{_candidate}\",\"version\":\"0.1.0\",\"runtimeIdentifier\":\"linux-arm64\",\"artifacts\":[{{\"name\":\"{Nupkg}\",\"sha256\":\"{nupkgHash}\"}},{{\"name\":\"{Snupkg}\",\"sha256\":\"{snupkgHash}\"}},{{\"name\":\"{CliZip}\",\"sha256\":\"{cliZipHash}\"}},{{\"name\":\"other.zip\",\"sha256\":\"{otherHash}\"}}]}}");
        var (exitCode, output) = Run(_candidate, _candidate);
        exitCode.Should().Be(5);
        output.Should().Contain("MANIFEST-MISMATCH other.zip");
        output.Should().NotContain("MANIFEST-MISMATCH " + Nupkg);
        output.Should().NotContain("MANIFEST-MISMATCH " + Snupkg);
        output.Should().NotContain("MANIFEST-MISMATCH " + CliZip);
    }

    /// <summary>
    /// Tests that promotion fails with exit code 2 when evidence.json does not contain a commit.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_EvidenceWithoutCommit_ExitTwo()
    {
        File.WriteAllText(Path.Combine(_artifacts, "evidence.json"), "{\"artifacts\":[]}");
        var (exitCode, output) = Run(_candidate, _candidate);
        exitCode.Should().Be(2);
    }

    /// <summary>
    /// Tests that promotion fails with exit code 2 when evidence.json is not valid JSON.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_EvidenceNotJson_ExitTwo()
    {
        File.WriteAllText(Path.Combine(_artifacts, "evidence.json"), "not json");
        var (exitCode, output) = Run(_candidate, _candidate);
        exitCode.Should().Be(2);
    }

    /// <summary>
    /// Tests that promotion passes when artifact folder contains wildcard characters.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_ArtifactFolderWithWildcardCharacters_Passes()
    {
        var oldArtifacts = _artifacts;
        _artifacts = Path.Combine(_dir, "art[1]");
        Directory.Move(oldArtifacts, _artifacts);
        var (exitCode, output) = Run(_candidate, _candidate);
        exitCode.Should().Be(0);
        output.Should().Contain("PROMOTABLE");
    }

    /// <summary>
    /// Tests that promotion passes with the complete release set.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_CompleteReleaseSet_Passes()
    {
        var (exitCode, output) = Run(_candidate, _candidate);
        exitCode.Should().Be(0);
        output.Should().Contain("PROMOTABLE");
    }

    /// <summary>
    /// Tests that promotion fails when the release set is missing symbols.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_ReleaseSetWithoutSymbols_Fails()
    {
        File.Delete(Path.Combine(_artifacts, Snupkg));
        WriteManifest(Nupkg, CliZip);
        WriteEvidence(_candidate, "0.1.0", "linux-arm64", Nupkg, CliZip);
        var (exitCode, output) = Run(_candidate, _candidate);
        exitCode.Should().Be(5);
        output.Should().Contain("INCOMPLETE-SET " + Snupkg);
        output.Should().NotContain("PROMOTABLE");
    }

    /// <summary>
    /// Tests that promotion fails when the release set contains an unexpected artifact.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_ReleaseSetWithExtraArtifact_Fails()
    {
        var extraPath = Path.Combine(_artifacts, "extra.txt");
        File.WriteAllText(extraPath, "extra");
        WriteManifest(Nupkg, Snupkg, CliZip, "extra.txt");
        WriteEvidence(_candidate, "0.1.0", "linux-arm64", Nupkg, Snupkg, CliZip, "extra.txt");
        var (exitCode, output) = Run(_candidate, _candidate);
        exitCode.Should().Be(5);
        output.Should().Contain("UNEXPECTED-ARTIFACT extra.txt");
    }

    /// <summary>
    /// Tests that promotion fails when the CLI archive is for a different runtime.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_CliArchiveOfOtherRuntime_Fails()
    {
        WriteEvidence(_candidate, "0.1.0", "linux-x64", Nupkg, Snupkg, CliZip);
        var (exitCode, output) = Run(_candidate, _candidate);
        exitCode.Should().Be(5);
        output.Should().Contain("INCOMPLETE-SET WeatherParser.Cli.0.1.0.linux-x64.zip");
        output.Should().Contain("UNEXPECTED-ARTIFACT WeatherParser.Cli.0.1.0.linux-arm64.zip");
    }

    /// <summary>
    /// Tests that promotion fails with exit code 2 when evidence.json does not contain a version.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_EvidenceWithoutVersion_ExitTwo()
    {
        WriteEvidence(_candidate, null, "linux-arm64", Nupkg, Snupkg, CliZip);
        var (exitCode, output) = Run(_candidate, _candidate);
        exitCode.Should().Be(2);
    }

    /// <summary>
    /// Tests that promotion fails with exit code 2 when evidence.json does not contain a runtime identifier.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void Promotion_EvidenceWithoutRuntimeIdentifier_ExitTwo()
    {
        WriteEvidence(_candidate, "0.1.0", null, Nupkg, Snupkg, CliZip);
        var (exitCode, output) = Run(_candidate, _candidate);
        exitCode.Should().Be(2);
    }
}
