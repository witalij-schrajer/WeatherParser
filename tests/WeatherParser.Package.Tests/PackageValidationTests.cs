using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Package validation of the library against the released 1.0.0 (RL-04 A, T-P9-04).
/// </summary>
[TestClass]
public sealed class PackageValidationTests
{
    private string _dir = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _dir = Path.Combine(Path.GetTempPath(), "wp-pv-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        
        // Copy global.json to ensure the pinned SDK is used
        var globalJsonPath = Path.Combine(PackageArtifacts.RepositoryRoot, "global.json");
        File.Copy(globalJsonPath, Path.Combine(_dir, "global.json"));
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    private (int ExitCode, string Output) RunDotnet(params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = _dir
        };

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        startInfo.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.EnvironmentVariables["DOTNET_NOLOGO"] = "1";

        using var process = Process.Start(startInfo)!;
        
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        
        if (!process.WaitForExit(300000))
        {
            process.Kill(true);
            process.WaitForExit();
            Assert.Fail("dotnet did not finish within 300 s");
        }
        
        var output = stdoutTask.Result + stderrTask.Result;
        return (process.ExitCode, output);
    }

    /// <summary>
    /// Tests that the baseline file exists and has the expected SHA-256.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void PackageValidation_BaselineFile_IsTheReleasedPackage()
    {
        var baselinePath = Path.Combine(PackageArtifacts.RepositoryRoot, "eng", "package-validation", "WeatherParser.Parsing.1.0.0.nupkg");
        File.Exists(baselinePath).Should().BeTrue("the baseline file should exist");

        byte[] hash;
        using var stream = File.OpenRead(baselinePath);
        {
            hash = SHA256.HashData(stream);
        }

        var hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        hex.Should().Be("9f81089840063f91b51a58bf58b641d12da9afd16baae0392972d26f047758c6");
    }

    /// <summary>
    /// Tests that the library project enables package validation against the baseline.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void PackageValidation_LibraryProject_EnabledAgainstTheBaseline()
    {
        var csprojPath = Path.Combine(PackageArtifacts.RepositoryRoot, "src", "WeatherParser.Parsing", "WeatherParser.Parsing.csproj");
        var doc = XDocument.Load(csprojPath);
        var enableValidation = doc.Descendants("EnablePackageValidation").Select(e => e.Value.Trim()).FirstOrDefault(v => v.Length > 0);
        enableValidation.Should().Be("true");

        var baselinePath = Path.Combine(PackageArtifacts.RepositoryRoot, "eng", "package-validation", "WeatherParser.Parsing.1.0.0.nupkg");
        var baselineElement = doc.Descendants("PackageValidationBaselinePath").FirstOrDefault();
        baselineElement.Should().NotBeNull();
        var baselineValue = baselineElement.Value.Trim();
        var projectDir = Path.GetDirectoryName(csprojPath) + Path.DirectorySeparatorChar;
        var resolvedBaseline = Path.GetFullPath(baselineValue.Replace("$(MSBuildThisFileDirectory)", projectDir));
        resolvedBaseline.Should().Be(baselinePath);
    }

    /// <summary>
    /// Tests that a breaking change against the baseline causes package validation to fail with CP0002.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void PackageValidation_BreakingChangeAgainstBaseline_Fails()
    {
        // Create project file
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Version>1.0.0</Version>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <IsPackable>true</IsPackable>
  </PropertyGroup>
</Project>";
        File.WriteAllText(Path.Combine(_dir, "Fixture.Validation.csproj"), csprojContent);

        // Create Api.cs with A and B
        var apiContent = @"namespace Fixture.Validation;
public class Api 
{ 
    public int A() => 1; 
    public int B() => 2; 
}";
        File.WriteAllText(Path.Combine(_dir, "Api.cs"), apiContent);

        // Pack version 1.0.0
        var (exitCode1, output1) = RunDotnet("pack", "-c", "Release", "-o", Path.Combine(_dir, "baseline"));
        exitCode1.Should().Be(0, "pack of first version should succeed");

        // Modify Api.cs to remove B()
        apiContent = @"namespace Fixture.Validation;
public class Api 
{ 
    public int A() => 1; 
}";
        File.WriteAllText(Path.Combine(_dir, "Api.cs"), apiContent);

        // Pack version 1.1.0 with validation
        var (exitCode2, output2) = RunDotnet(
            "pack",
            "-c", "Release",
            "-o", Path.Combine(_dir, "next"),
            "-p:Version=1.1.0",
            "-p:EnablePackageValidation=true",
            $"-p:PackageValidationBaselinePath={Path.Combine(_dir, "baseline", "Fixture.Validation.1.0.0.nupkg")}");
        exitCode2.Should().NotBe(0, "breaking change should cause validation to fail");
        output2.Should().Contain("CP0002");
    }

    /// <summary>
    /// Tests that a compatible change against the baseline passes package validation.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-04")]
    public void PackageValidation_CompatibleChangeAgainstBaseline_Passes()
    {
        // Create project file
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Version>1.0.0</Version>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <IsPackable>true</IsPackable>
  </PropertyGroup>
</Project>";
        File.WriteAllText(Path.Combine(_dir, "Fixture.Validation.csproj"), csprojContent);

        // Create Api.cs with A and B
        var apiContent = @"namespace Fixture.Validation;
public class Api 
{ 
    public int A() => 1; 
    public int B() => 2; 
}";
        File.WriteAllText(Path.Combine(_dir, "Api.cs"), apiContent);

        // Pack version 1.0.0
        var (exitCode1, output1) = RunDotnet("pack", "-c", "Release", "-o", Path.Combine(_dir, "baseline"));
        exitCode1.Should().Be(0, "pack of first version should succeed");

        // Modify Api.cs to keep A and B, add C()
        apiContent = @"namespace Fixture.Validation;
public class Api 
{ 
    public int A() => 1; 
    public int B() => 2; 
    public int C() => 3;
}";
        File.WriteAllText(Path.Combine(_dir, "Api.cs"), apiContent);

        // Pack version 1.1.0 with validation
        var (exitCode2, output2) = RunDotnet(
            "pack",
            "-c", "Release",
            "-o", Path.Combine(_dir, "next"),
            "-p:Version=1.1.0",
            "-p:EnablePackageValidation=true",
            $"-p:PackageValidationBaselinePath={Path.Combine(_dir, "baseline", "Fixture.Validation.1.0.0.nupkg")}");
        exitCode2.Should().Be(0, "compatible change should pass validation");
    }
}
