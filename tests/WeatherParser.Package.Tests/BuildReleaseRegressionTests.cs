using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Regression tests for scripts/build-release.ps1: run IDs, the CLI's resolved package, cleanup and staging (RL-06 A, RL-08 A, T-P9-05).
/// </summary>
[TestClass]
public sealed class BuildReleaseRegressionTests
{
    private string _dir = null!;
    private string _commit = null!;
    private string _out = null!;
    private string _log = null!;
    private string _fake = null!;
    private string _workRoot = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _dir = Path.Combine(Path.GetTempPath(), "wp-release-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        var repo = Path.Combine(_dir, "repo");
        Directory.CreateDirectory(repo);
        var git = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = repo,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        git.ArgumentList.Add("init");
        git.ArgumentList.Add("-q");
        using (var process = Process.Start(git)!)
        {
            process.WaitForExit();
        }
        git.ArgumentList.Clear();
        git.ArgumentList.Add("config");
        git.ArgumentList.Add("user.email");
        git.ArgumentList.Add("test@example.invalid");
        using (var process = Process.Start(git)!)
        {
            process.WaitForExit();
        }
        git.ArgumentList.Clear();
        git.ArgumentList.Add("config");
        git.ArgumentList.Add("user.name");
        git.ArgumentList.Add("Test");
        using (var process = Process.Start(git)!)
        {
            process.WaitForExit();
        }
        git.ArgumentList.Clear();
        git.ArgumentList.Add("config");
        git.ArgumentList.Add("core.autocrlf");
        git.ArgumentList.Add("false");
        using (var process = Process.Start(git)!)
        {
            process.WaitForExit();
        }

        Directory.CreateDirectory(Path.Combine(repo, "schemas", "export", "v1"));
        File.WriteAllText(Path.Combine(repo, "README.md"), "readme");
        File.WriteAllText(Path.Combine(repo, "LICENSE"), "license");
        File.WriteAllText(Path.Combine(repo, "schemas", "export", "v1", "weatherparser-export-v1.xsd"), "<xs:schema />");
        Directory.CreateDirectory(Path.Combine(repo, "src", "WeatherParser.Parsing"));
        File.WriteAllText(
            Path.Combine(repo, "src", "WeatherParser.Parsing", "WeatherParser.Parsing.csproj"),
            """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><Version>0.1.0</Version></PropertyGroup><PropertyGroup><IsPackable>true</IsPackable></PropertyGroup></Project>""");
        Directory.CreateDirectory(Path.Combine(repo, "examples", "WeatherParser.Cli"));
        File.WriteAllText(
            Path.Combine(repo, "examples", "WeatherParser.Cli", "WeatherParser.Cli.csproj"),
            """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><Version>0.1.0</Version></PropertyGroup></Project>""");
        git.ArgumentList.Clear();
        git.ArgumentList.Add("add");
        git.ArgumentList.Add(".");
        using (var process = Process.Start(git)!)
        {
            process.WaitForExit();
        }
        git.ArgumentList.Clear();
        git.ArgumentList.Add("commit");
        git.ArgumentList.Add("-m");
        git.ArgumentList.Add("candidate");
        using (var process = Process.Start(git)!)
        {
            process.WaitForExit();
        }
        git.ArgumentList.Clear();
        git.ArgumentList.Add("rev-parse");
        git.ArgumentList.Add("HEAD");
        using (var process = Process.Start(git)!)
        {
            process.WaitForExit();
            _commit = process.StandardOutput.ReadToEnd().Trim();
        }
        _out = Path.Combine(_dir, "out");
        _log = Path.Combine(_dir, "dotnet.log");
        _fake = Path.Combine(_dir, "fake-dotnet.ps1");
        File.WriteAllText(_fake,
            "$ErrorActionPreference = 'Stop'\n" +
            "Add-Type -AssemblyName System.IO.Compression.FileSystem\n" +
            "Add-Content -LiteralPath $env:FAKE_DOTNET_LOG -Value (\"$env:DOTNET_ROLL_FORWARD|\" + ($args -join ' '))\n" +
            "function Get-Out { for ($i = 0; $i -lt $args[0].Count; $i++) { if ($args[0][$i] -in '-o', '--output') { return $args[0][$i + 1] } }; throw 'no output' }\n" +
            "function New-Zip([string]$Path, [string]$Entry) { $z = [IO.Compression.ZipFile]::Open($Path, 'Create'); $e = $z.CreateEntry($Entry); $w = [IO.StreamWriter]::new($e.Open()); $w.Write($Entry); $w.Dispose(); $z.Dispose() }\n" +
            "switch ($args[0]) {\n" +
            "    '--version' { if ($env:FAKE_VERSION_EMPTY -ne '1') { Write-Output '10.0.402' }; exit 0 }\n" +
            "    'test' { exit 0 }\n" +
            "    'pack' { $o = Get-Out $args; New-Item -ItemType Directory -Force $o | Out-Null; New-Zip (Join-Path $o 'WeatherParser.Parsing.0.1.0.nupkg') 'lib/net10.0/WeatherParser.Parsing.dll'; New-Zip (Join-Path $o 'WeatherParser.Parsing.0.1.0.snupkg') 'lib/net10.0/WeatherParser.Parsing.pdb'; exit 0 }\n" +
            "    'publish' {\n" +
            "        $o = Get-Out $args; New-Item -ItemType Directory -Force $o | Out-Null; Set-Content -LiteralPath (Join-Path $o 'WeatherParser.dll') -Value 'cli'\n" +
            "        $pkg = Join-Path $PWD 'artifacts/local-nuget/WeatherParser.Parsing.0.1.0.nupkg'\n" +
            "        $real = [Convert]::ToBase64String([Security.Cryptography.SHA512]::HashData([IO.File]::ReadAllBytes($pkg)))\n" +
            "        $cache = Join-Path $env:NUGET_PACKAGES 'weatherparser.parsing/0.1.0'; New-Item -ItemType Directory -Force $cache | Out-Null\n" +
            "        Set-Content -LiteralPath (Join-Path $cache 'weatherparser.parsing.0.1.0.nupkg.sha512') -Value $real -NoNewline\n" +
            "        $version = $env:FAKE_ASSETS_VERSION ?? '0.1.0'; $hash = $env:FAKE_ASSETS_HASH ?? $real\n" +
            "        $obj = Join-Path $PWD 'examples/WeatherParser.Cli/obj'; New-Item -ItemType Directory -Force $obj | Out-Null\n" +
            "        $assets = @{ version = 3; libraries = @{ \"WeatherParser.Parsing/$version\" = @{ type = 'package'; sha512 = $hash } } } | ConvertTo-Json -Depth 5\n" +
            "        if ($env:FAKE_BLOCK_EVIDENCE) { New-Item -ItemType Directory -Force $env:FAKE_BLOCK_EVIDENCE | Out-Null; Set-Content -LiteralPath (Join-Path $env:FAKE_BLOCK_EVIDENCE 'evidence.json') -Value 'blocker' }\n" +
            "        Set-Content -LiteralPath (Join-Path $obj 'project.assets.json') -Value $assets; exit 0 }\n" +
            "    default { exit 0 }\n" +
            "}\n");
        _workRoot = Path.Combine(_dir, "work");
        Directory.CreateDirectory(_workRoot);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_dir))
        {
            var dirInfo = new DirectoryInfo(_dir);
            foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }
            Directory.Delete(_dir, true);
        }
    }

    private (int ExitCode, string Output) Run(string version = "0.1.0", params (string Name, string Value)[] environment)
    {
        var scriptPath = Path.Combine(PackageArtifacts.RepositoryRoot, "scripts", "build-release.ps1");
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
        startInfo.ArgumentList.Add("-Commit");
        startInfo.ArgumentList.Add(_commit);
        startInfo.ArgumentList.Add("-Version");
        startInfo.ArgumentList.Add(version);
        startInfo.ArgumentList.Add("-OutputDirectory");
        startInfo.ArgumentList.Add(_out);
        startInfo.ArgumentList.Add("-Dotnet");
        startInfo.ArgumentList.Add(_fake);
        startInfo.ArgumentList.Add("-WorkRoot");
        startInfo.ArgumentList.Add(_workRoot);

        foreach (var (name, value) in environment)
        {
            startInfo.Environment[name] = value;
        }

        startInfo.Environment["FAKE_DOTNET_LOG"] = _log;

        using var process = Process.Start(startInfo)!;
        
        var stderrTask = process.StandardError.ReadToEndAsync();
        var stdout = process.StandardOutput.ReadToEnd();
        var output = stdout + stderrTask.Result;

        if (!process.WaitForExit(180000))
        {
            process.Kill(true);
            Assert.Fail("build-release.ps1 did not finish within 180 s");
        }
        return (process.ExitCode, output);
    }

    private static string Sha256Hex(string path)
    {
        using var sha = SHA256.Create();
        var bytes = File.ReadAllBytes(path);
        var hash = sha.ComputeHash(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    private void AssertNoArtifacts()
    {
        if (!Directory.Exists(_out))
        {
            return;
        }

        Directory.GetFiles(_out, "*.nupkg").Should().BeEmpty();
        Directory.GetFiles(_out, "*.snupkg").Should().BeEmpty();
        Directory.GetFiles(_out, "*.zip").Should().BeEmpty();
        Directory.GetFiles(_out, "SHA256SUMS").Should().BeEmpty();
        Directory.GetFiles(_out, "evidence.json").Should().BeEmpty();
    }

    private void AssertNoArtifactsAndNoWorkFolder()
    {
        AssertNoArtifacts();
        Directory.GetDirectories(_workRoot, "wp-release-work-*").Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a realistic GitHub run ID is written as a JSON string.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void BuildRelease_RealisticRunId_IsWrittenAsString()
    {
        var (exitCode, output) = Run(environment: ("GITHUB_RUN_ID", "41187615685"));
        exitCode.Should().Be(0);
        var evidence = Path.Combine(_out, "evidence.json");
        File.Exists(evidence).Should().BeTrue();

        using var doc = JsonDocument.Parse(File.ReadAllText(evidence));
        doc.RootElement.GetProperty("runId").ValueKind.Should().Be(JsonValueKind.String);
        doc.RootElement.GetProperty("runId").GetString().Should().Be("41187615685");
    }

    /// <summary>
    /// Tests that the CLI fails when it resolves a different package version.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void BuildRelease_CliResolvedOtherVersion_Fails()
    {
        var (exitCode, output) = Run(environment: ("FAKE_ASSETS_VERSION", "0.0.9"));
        exitCode.Should().Be(5);
        output.Should().Contain("PACKAGE-MISMATCH");
        AssertNoArtifactsAndNoWorkFolder();
    }

    /// <summary>
    /// Tests that the CLI fails when it resolves a different package hash.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void BuildRelease_CliResolvedOtherHash_Fails()
    {
        var (exitCode, output) = Run(environment: ("FAKE_ASSETS_HASH", "d3Jvbmc="));
        exitCode.Should().Be(5);
        output.Should().Contain("PACKAGE-MISMATCH");
        AssertNoArtifactsAndNoWorkFolder();
    }

    /// <summary>
    /// Tests that an unexpected error exits with code 6 and cleans up.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void BuildRelease_UnexpectedError_ExitSixAndCleansUp()
    {
        var (exitCode, output) = Run(environment: ("FAKE_VERSION_EMPTY", "1"));
        exitCode.Should().Be(6);
        output.Should().Contain("UNEXPECTED");
        AssertNoArtifactsAndNoWorkFolder();
    }

    /// <summary>
    /// Tests that a successful run removes the work folder.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void BuildRelease_Success_RemovesWorkFolder()
    {
        var (exitCode, output) = Run();
        exitCode.Should().Be(0);
        Directory.GetDirectories(_workRoot, "wp-release-work-*").Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a version mismatch removes the work folder.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void BuildRelease_VersionMismatch_RemovesWorkFolder()
    {
        var (exitCode, output) = Run("0.2.0");
        exitCode.Should().Be(3);
        AssertNoArtifactsAndNoWorkFolder();
    }

    /// <summary>
    /// Tests that a failed transfer into the output folder leaves no artifact of this run.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void BuildRelease_TransferFailure_LeavesNoArtifactOfThisRun()
    {
        var (exitCode, output) = Run(environment: ("FAKE_BLOCK_EVIDENCE", _out));
        exitCode.Should().Be(6);
        output.Should().Contain("UNEXPECTED");

        Directory.GetFiles(_out, "*.nupkg").Should().BeEmpty();
        Directory.GetFiles(_out, "*.snupkg").Should().BeEmpty();
        Directory.GetFiles(_out, "*.zip").Should().BeEmpty();
        Directory.GetFiles(_out, "SHA256SUMS").Should().BeEmpty();

        File.ReadAllText(Path.Combine(_out, "evidence.json")).Trim().Should().Be("blocker");

        Directory.GetDirectories(_workRoot, "wp-release-work-*").Should().BeEmpty();
    }
}
