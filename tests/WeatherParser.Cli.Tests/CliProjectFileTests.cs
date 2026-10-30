using System.Xml.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Cli.Tests;

/// <summary>
/// Verifies the CLI project file: package consumption of the library, assembly name and packability (T-P3-01, CL-01).
/// </summary>
[TestClass]
public sealed class CliProjectFileTests
{
    private XDocument _project = null!;
    private string _repositoryRoot = null!;

    /// <summary>
    /// Initializes the test fixture by locating the repository root and loading the CLI project file.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        var currentDirectory = baseDirectory;
        while (currentDirectory != null && !File.Exists(Path.Combine(currentDirectory.FullName, "WeatherParser.slnx")))
        {
            currentDirectory = currentDirectory.Parent;
        }

        if (currentDirectory == null)
        {
            throw new InvalidOperationException("The repository root with WeatherParser.slnx was not found.");
        }

        _repositoryRoot = currentDirectory.FullName;
        var projectPath = Path.Combine(_repositoryRoot, "examples", "WeatherParser.Cli", "WeatherParser.Cli.csproj");
        _project = XDocument.Load(projectPath);
    }

    /// <summary>
    /// Verifies that the CLI project references only the WeatherParser.Parsing library as a versioned package.
    /// </summary>
    [TestMethod]
    public void CliProject_ReferencesLibraryOnlyAsVersionedPackage()
    {
        var packageReference = _project.Descendants("PackageReference")
            .Where(e => (string?)e.Attribute("Include") == "WeatherParser.Parsing")
            .Should().ContainSingle()
            .Which;

        packageReference.Attribute("Version").Should().BeNull();

        var packagesPropsPath = Path.Combine(_repositoryRoot, "Directory.Packages.props");
        var packagesProps = XDocument.Load(packagesPropsPath);
        var packageVersion = packagesProps.Descendants("PackageVersion")
            .Where(p => p.Attribute("Include")?.Value == "WeatherParser.Parsing")
            .Should().ContainSingle()
            .Which;

        ((string?)packageVersion.Attribute("Version")).Should().NotBeNullOrWhiteSpace();

        _project.Descendants("ProjectReference").Should().BeEmpty();

        _project.Descendants("Compile")
            .Select(c => (string?)c.Attribute("Include") ?? string.Empty)
            .Should().OnlyContain(include => !include.Contains("..", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that the CLI project has the correct assembly name and output type.
    /// </summary>
    [TestMethod]
    public void CliProject_AssemblyNameIsWeatherParser()
    {
        var assemblyName = _project.Descendants("AssemblyName")
            .Should().ContainSingle()
            .Which;

        assemblyName.Value.Should().Be("WeatherParser");

        var outputType = _project.Descendants("OutputType")
            .Should().ContainSingle()
            .Which;

        outputType.Value.Should().Be("Exe");
    }

    /// <summary>
    /// Verifies that the CLI project is not packable.
    /// </summary>
    [TestMethod]
    public void CliProject_IsNotPackable()
    {
        var isPackable = _project.Descendants("IsPackable")
            .Should().ContainSingle()
            .Which;

        isPackable.Value.Should().Be("false");
    }
}
