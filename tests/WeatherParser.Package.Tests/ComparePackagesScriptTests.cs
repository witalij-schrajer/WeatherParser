using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Runs scripts/compare-packages.ps1 over synthetic packages (RL-03 A, T-P9-03).
/// </summary>
[TestClass]
public sealed class ComparePackagesScriptTests
{
    private string _dir = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _dir = Path.Combine(Path.GetTempPath(), "wp-compare-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    private string Zip(string name, params (string Entry, string Content, DateTimeOffset Time)[] entries)
    {
        var path = Path.Combine(_dir, name);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (entry, content, time) in entries)
        {
            var zipEntry = zip.CreateEntry(entry);
            zipEntry.LastWriteTime = time;
            using var stream = zipEntry.Open();
            var bytes = Encoding.UTF8.GetBytes(content);
            stream.Write(bytes, 0, bytes.Length);
        }
        return path;
    }

    private static readonly DateTimeOffset T1 = new(2026, 10, 31, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T2 = T1.AddHours(1);

    private static (int ExitCode, string Output) Run(params string[] arguments)
    {
        var scriptPath = Path.Combine(PackageArtifacts.RepositoryRoot, "scripts", "compare-packages.ps1");
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

        foreach (var arg in arguments)
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
            Assert.Fail("compare-packages.ps1 did not finish within 60 s");
        }
        return (process.ExitCode, output);
    }

    /// <summary>
    /// Tests that identical packages result in exit code zero.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-03")]
    public void ComparePackages_IdenticalPackages_ExitZero()
    {
        var a = Zip("a.nupkg", ("x.txt", "one", T1));
        var b = Path.Combine(_dir, "b.nupkg");
        File.Copy(a, b);
        var (exitCode, output) = Run("-First", a, "-Second", b);
        exitCode.Should().Be(0);
        output.Should().Contain("IDENTICAL");
    }

    /// <summary>
    /// Tests that packages with only timestamp differences result in exit code three and mention the timestamp difference.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-03")]
    public void ComparePackages_TimestampOnly_ExitThreeAndNamesMember()
    {
        var a = Zip("a.nupkg", ("x.txt", "one", T1), ("y.txt", "two", T1));
        var b = Zip("b.nupkg", ("x.txt", "one", T1), ("y.txt", "two", T2));
        var (exitCode, output) = Run("-First", a, "-Second", b);
        exitCode.Should().Be(3);
        output.Should().Contain("CONTENT-IDENTICAL");
        output.Should().Contain("TIMESTAMP y.txt");
        output.Should().NotContain("TIMESTAMP x.txt");
    }

    /// <summary>
    /// Tests that packages with different content result in non-zero exit code and mention the differing member.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-03")]
    public void ComparePackages_DifferentMember_ExitNonZeroAndNamesMember()
    {
        var a = Zip("a.nupkg", ("x.txt", "one", T1));
        var b = Zip("b.nupkg", ("x.txt", "ONE", T1));
        var (exitCode, output) = Run("-First", a, "-Second", b);
        exitCode.Should().Be(1);
        output.Should().Contain("DIFFERENT x.txt");
    }

    /// <summary>
    /// Tests that packages with missing members result in exit code one and mention the member on only first side.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-03")]
    public void ComparePackages_MissingMember_ExitOneAndNamesSide()
    {
        var a = Zip("a.nupkg", ("x.txt", "one", T1), ("z.txt", "z", T1));
        var b = Zip("b.nupkg", ("x.txt", "one", T1));
        var (exitCode, output) = Run("-First", a, "-Second", b);
        exitCode.Should().Be(1);
        output.Should().Contain("ONLY-FIRST z.txt");
    }

    /// <summary>
    /// Tests that missing files result in exit code two.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-03")]
    public void ComparePackages_MissingFile_ExitTwo()
    {
        var a = Zip("a.nupkg", ("x.txt", "one", T1));
        var b = Path.Combine(_dir, "missing.nupkg");
        var (exitCode, output) = Run("-First", a, "-Second", b);
        exitCode.Should().Be(2);
    }

    /// <summary>
    /// Tests that member names are compared case-sensitively.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-03")]
    public void ComparePackages_NamesDifferOnlyInCase_ExitOneAndNamesBoth()
    {
        var a = Zip("a.nupkg", ("README.md", "one", T1));
        var b = Zip("b.nupkg", ("readme.md", "one", T1));
        var (exitCode, output) = Run("-First", a, "-Second", b);
        exitCode.Should().Be(1);
        output.Should().Contain("ONLY-FIRST README.md").And.Contain("ONLY-SECOND readme.md");
    }
}
