using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Verifies the members of the restored WeatherParser.Parsing package and the reproducibility settings visible in it (RL-01 A, RL-03 A).
/// </summary>
[TestClass]
public sealed class PackageContentsTests
{
    /// <summary>
    /// Tests that the package contains exactly the expected members.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-02")]
    public void Package_Contains_ExactlyExpectedMembers()
    {
        var names = PackageArtifacts.EntryNames();
        var psmdcpEntries = names.Where(name => Regex.IsMatch(name, "^package/services/metadata/core-properties/[^/]+\\.psmdcp$")).ToList();
        psmdcpEntries.Count.Should().Be(1);

        var expected = new[]
        {
            "LICENSE",
            "README.md",
            "WeatherParser.Parsing.nuspec",
            "[Content_Types].xml",
            "_rels/.rels",
            "lib/net10.0/WeatherParser.Parsing.dll",
            "lib/net10.0/WeatherParser.Parsing.xml",
            "schemas/export/v1/weatherparser-export-v1.xsd"
        }.OrderBy(name => name, StringComparer.Ordinal).ToList();

        var others = names.Where(name => !psmdcpEntries.Contains(name)).ToList();
        others.Should().Equal(expected);
    }

    /// <summary>
    /// Tests that the XML documentation covers every exported type.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-02")]
    public void Package_XmlDocumentation_CoversEveryExportedType()
    {
        var xmlBytes = PackageArtifacts.ReadEntry("lib/net10.0/WeatherParser.Parsing.xml");
        var xmlContent = Encoding.UTF8.GetString(xmlBytes);
        var doc = XDocument.Parse(xmlContent);

        var memberElements = doc.Descendants("member").ToList();
        var summaries = memberElements.ToDictionary(
            e => e.Attribute("name")!.Value,
            e => e.Element("summary")?.Value.Trim() ?? "");

        var exportedTypes = typeof(ParserEndpointDefinition).Assembly.GetExportedTypes();
        var missingIds = new List<string>();

        foreach (var type in exportedTypes)
        {
            var id = "T:" + type.FullName!.Replace('+', '.');
            if (!summaries.TryGetValue(id, out var summary) || string.IsNullOrEmpty(summary))
            {
                missingIds.Add(id);
            }
        }

        missingIds.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the README.md file is byte-identical to the repository version.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-02")]
    public void Package_Readme_ByteIdenticalToRepository()
    {
        var readmeBytes = PackageArtifacts.ReadEntry("README.md");
        var repoReadmePath = Path.Combine(PackageArtifacts.RepositoryRoot, "README.md");
        var repoReadmeBytes = File.ReadAllBytes(repoReadmePath);
        readmeBytes.Should().Equal(repoReadmeBytes);
    }

    /// <summary>
    /// Tests that text members use LF line endings.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-02")]
    public void Package_TextMembers_UseLfLineEndings()
    {
        var readmeBytes = PackageArtifacts.ReadEntry("README.md");
        var licenseBytes = PackageArtifacts.ReadEntry("LICENSE");

        readmeBytes.Should().NotContain((byte)'\r');
        licenseBytes.Should().NotContain((byte)'\r');
    }

    /// <summary>
    /// Tests that the .nuspec file contains no absolute path.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-02")]
    public void Package_Nuspec_ContainsNoAbsolutePath()
    {
        var nuspecBytes = PackageArtifacts.ReadEntry("WeatherParser.Parsing.nuspec");
        var nuspecContent = Encoding.UTF8.GetString(nuspecBytes);

        Regex.IsMatch(nuspecContent, @"[A-Za-z]:\\").Should().BeFalse();
        nuspecContent.Should().NotContain(PackageArtifacts.RepositoryRoot);
    }

    /// <summary>
    /// Tests that the assembly's debug path is mapped.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-03")]
    public void Package_Assembly_DebugPathIsMapped()
    {
        var dllBytes = PackageArtifacts.ReadEntry("lib/net10.0/WeatherParser.Parsing.dll");
        using var peReader = new PEReader(new MemoryStream(dllBytes));
        var codeViewEntry = peReader.ReadDebugDirectory().Single(e => e.Type == DebugDirectoryEntryType.CodeView);
        var codeViewData = peReader.ReadCodeViewDebugDirectoryData(codeViewEntry);
        codeViewData.Path.Should().StartWith("/_/");
        Regex.IsMatch(codeViewData.Path, @"[A-Za-z]:\\").Should().BeFalse();
    }

    /// <summary>
    /// Tests that the assembly has no absolute build path.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P9-03")]
    public void Package_Assembly_HasNoAbsoluteBuildPath()
    {
        var dllBytes = PackageArtifacts.ReadEntry("lib/net10.0/WeatherParser.Parsing.dll");
        var dllContent = Encoding.UTF8.GetString(dllBytes);

        dllContent.Should().NotContain(PackageArtifacts.RepositoryRoot.Replace('\\', '/'));
        dllContent.Should().NotContain(PackageArtifacts.RepositoryRoot);
    }
}
