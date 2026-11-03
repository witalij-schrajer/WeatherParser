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
/// Runs scripts/build-release.ps1 with a fake dotnet over a synthetic repository, and checks the release workflow file (RL-02 A, RL-06 A, T-P9-05, T-P9-06).
/// </summary>
[TestClass]
public sealed class BuildReleaseScriptTests
{
    private string _dir = null!;
    private string _commit = null!;
    private string _out = null!;
    private string _log = null!;
    private string _fake = null!;

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
            "    '--version' { Write-Output '10.0.402'; exit 0 }\n" +
            "    'test' { exit [int]($env:FAKE_TEST_EXIT ?? '0') }\n" +
            "    'pack' { $o = Get-Out $args; New-Item -ItemType Directory -Force $o | Out-Null; New-Zip (Join-Path $o 'WeatherParser.Parsing.0.1.0.nupkg') 'lib/net10.0/WeatherParser.Parsing.dll'; New-Zip (Join-Path $o 'WeatherParser.Parsing.0.1.0.snupkg') 'lib/net10.0/WeatherParser.Parsing.pdb'; exit 0 }\n" +
            "    'publish' {\n" +
            "        $o = Get-Out $args; New-Item -ItemType Directory -Force $o | Out-Null; Set-Content -LiteralPath (Join-Path $o 'WeatherParser.dll') -Value 'cli'\n" +
            "        $pkg = Join-Path $PWD 'artifacts/local-nuget/WeatherParser.Parsing.0.1.0.nupkg'\n" +
            "        $hash = if ($env:FAKE_WRONG_HASH -eq '1') { 'wrong' } else { [Convert]::ToBase64String([Security.Cryptography.SHA512]::HashData([IO.File]::ReadAllBytes($pkg))) }\n" +
            "        $cache = Join-Path $env:NUGET_PACKAGES 'weatherparser.parsing/0.1.0'; New-Item -ItemType Directory -Force $cache | Out-Null\n" +
            "        Set-Content -LiteralPath (Join-Path $cache 'weatherparser.parsing.0.1.0.nupkg.sha512') -Value $hash -NoNewline\n" +
            "        $obj = Join-Path $PWD 'examples/WeatherParser.Cli/obj'; New-Item -ItemType Directory -Force $obj | Out-Null\n" +
            "        $assets = @{ version = 3; libraries = @{ \"WeatherParser.Parsing/0.1.0\" = @{ type = 'package'; sha512 = $hash } } } | ConvertTo-Json -Depth 5\n" +
            "        Set-Content -LiteralPath (Join-Path $obj 'project.assets.json') -Value $assets; exit 0 }\n" +
            "    default { exit 0 }\n" +
            "}\n");
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
    }

    /// <summary>
    /// Tests that the script produces package, symbols, sums and evidence.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void BuildRelease_ProducesPackageSymbolsSumsAndEvidence()
    {
        var (exitCode, output) = Run();
        exitCode.Should().Be(0);
        Directory.Exists(_out).Should().BeTrue();
        var nupkg = Path.Combine(_out, "WeatherParser.Parsing.0.1.0.nupkg");
        var snupkg = Path.Combine(_out, "WeatherParser.Parsing.0.1.0.snupkg");
        var zip = Path.Combine(_out, "WeatherParser.Cli.0.1.0.linux-arm64.zip");
        var sums = Path.Combine(_out, "SHA256SUMS");
        var evidence = Path.Combine(_out, "evidence.json");
        File.Exists(nupkg).Should().BeTrue();
        File.Exists(snupkg).Should().BeTrue();
        File.Exists(zip).Should().BeTrue();
        File.Exists(sums).Should().BeTrue();
        File.Exists(evidence).Should().BeTrue();

        var lines = File.ReadAllLines(sums);
        lines.Length.Should().Be(3);
        lines[0].Should().Be($"{Sha256Hex(zip)}  WeatherParser.Cli.0.1.0.linux-arm64.zip");
        lines[1].Should().Be($"{Sha256Hex(nupkg)}  WeatherParser.Parsing.0.1.0.nupkg");
        lines[2].Should().Be($"{Sha256Hex(snupkg)}  WeatherParser.Parsing.0.1.0.snupkg");
    }

    /// <summary>
    /// Tests that the CLI artifact is built against the same-run package.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void BuildRelease_CliAcceptanceArtifact_BuiltAgainstSameRunPackage()
    {
        var (exitCode, output) = Run();
        exitCode.Should().Be(0);
        var zip = Path.Combine(_out, "WeatherParser.Cli.0.1.0.linux-arm64.zip");
        File.Exists(zip).Should().BeTrue();

        using var zipStream = new FileStream(zip, FileMode.Open);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
        var entry = archive.GetEntry("WeatherParser.dll");
        entry.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that the script fails when the resolved package differs.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void BuildRelease_ResolvedPackageDiffers_Fails()
    {
        var (exitCode, output) = Run(environment: ("FAKE_WRONG_HASH", "1"));
        exitCode.Should().Be(5);
        output.Should().Contain("PACKAGE-MISMATCH");
        AssertNoArtifacts();
    }

    /// <summary>
    /// Tests that the script fails when the version does not match.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void BuildRelease_VersionMismatch_Fails()
    {
        var (exitCode, output) = Run("0.2.0");
        exitCode.Should().Be(3);
        output.Should().Contain("VERSION-MISMATCH");
        AssertNoArtifacts();
    }

    /// <summary>
    /// Tests that the script fails when tests fail.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void BuildRelease_TestFailure_ProducesNoArtifacts()
    {
        var (exitCode, output) = Run(environment: ("FAKE_TEST_EXIT", "1"));
        exitCode.Should().Be(4);
        output.Should().Contain("TEST-FAILURE");
        AssertNoArtifacts();
    }

    /// <summary>
    /// Tests that the evidence record contains commit, tree, sdk and other information.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-06")]
    public void BuildRelease_EvidenceRecord_NamesCommitTreeSdkAndRuns()
    {
        var (exitCode, output) = Run(environment: ("GITHUB_RUN_ID", "4242"));
        exitCode.Should().Be(0);
        var evidence = Path.Combine(_out, "evidence.json");
        File.Exists(evidence).Should().BeTrue();

        using var doc = JsonDocument.Parse(File.ReadAllText(evidence));
        doc.RootElement.GetProperty("commit").GetString().Should().Be(_commit);
        var tree = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = Path.Combine(_dir, "repo"),
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        tree.ArgumentList.Add("rev-parse");
        tree.ArgumentList.Add($"{_commit}^{{tree}}");
        using (var process = Process.Start(tree)!)
        {
            process.WaitForExit();
            var treeValue = process.StandardOutput.ReadToEnd().Trim();
            doc.RootElement.GetProperty("tree").GetString().Should().Be(treeValue);
        }
        doc.RootElement.GetProperty("version").GetString().Should().Be("0.1.0");
        doc.RootElement.GetProperty("sdk").GetString().Should().Be("10.0.402");
        doc.RootElement.GetProperty("runtimeIdentifier").GetString().Should().Be("linux-arm64");
        doc.RootElement.GetProperty("dotnetRollForward").GetString().Should().Be("Disable");
        doc.RootElement.GetProperty("runId").GetString().Should().Be("4242");
        var artifacts = doc.RootElement.GetProperty("artifacts");
        artifacts.GetArrayLength().Should().Be(3);
        var names = new[] { "WeatherParser.Cli.0.1.0.linux-arm64.zip", "WeatherParser.Parsing.0.1.0.nupkg", "WeatherParser.Parsing.0.1.0.snupkg" };
        for (var i = 0; i < 3; i++)
        {
            artifacts[i].GetProperty("name").GetString().Should().Be(names[i]);
            var path = Path.Combine(_out, names[i]);
            artifacts[i].GetProperty("sha256").GetString().Should().Be(Sha256Hex(path));
        }
    }

    /// <summary>
    /// Tests that DOTNET_ROLL_FORWARD=Disable is set for every dotnet call.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void BuildRelease_RollForwardDisabled_ForEveryDotnetCall()
    {
        var (exitCode, output) = Run();
        exitCode.Should().Be(0);
        var lines = File.ReadAllLines(_log);
        foreach (var line in lines)
        {
            var command = line.Substring(line.IndexOf('|') + 1);
            if (command.StartsWith("test ", StringComparison.Ordinal))
            {
                line.Should().StartWith("|", "dotnet test runs the tests on the installed runtime, without the compiler-runtime pin");
            }
            else
            {
                line.Should().StartWith("Disable|", "every call that produces an artifact runs with the compiler-runtime pin");
            }
        }

        lines.Should().Contain(line => line.StartsWith("Disable|pack", StringComparison.Ordinal));
        lines.Should().Contain(line => line.StartsWith("Disable|publish", StringComparison.Ordinal));
        lines.Should().Contain(line => line.StartsWith("|test", StringComparison.Ordinal));
    }

    /// <summary>
    /// Tests that the release workflow file has read-only permissions and no secrets.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void ReleaseWorkflow_HasReadOnlyPermissionsAndNoSecrets()
    {
        var workflowPath = Path.Combine(PackageArtifacts.RepositoryRoot, ".github", "workflows", "release-build.yml");
        var content = File.ReadAllText(workflowPath);
        content.Should().Contain("workflow_dispatch:");
        content.Should().Contain("contents: read");
        content.Should().NotContain("secrets.");
        content.Should().NotContain("pull_request");
        content.Should().NotContain("push:");
        content.Should().NotContain(": write");
        content.Should().NotContain("id-token");
    }

    /// <summary>
    /// Tests that the script fails when the output directory is not empty.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-05")]
    public void BuildRelease_OutputDirectoryNotEmpty_ExitTwo()
    {
        Directory.CreateDirectory(_out);
        var xtxt = Path.Combine(_out, "x.txt");
        File.WriteAllText(xtxt, "x");
        var (exitCode, output) = Run();
        exitCode.Should().Be(2);
    }
}
