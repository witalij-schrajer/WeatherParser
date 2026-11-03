using System;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Verifies the metadata of the restored WeatherParser.Parsing package against the approved release contract (RL-01 A, RL-19 A).
/// </summary>
[TestClass]
public sealed class PackageMetadataTests
{
    private static XElement Metadata()
    {
        var doc = PackageArtifacts.ReadNuspec();
        var ns = doc.Root!.Name.Namespace;
        return doc.Root.Element(ns + "metadata")!;
    }

    private static string? Value(string name)
    {
        var doc = PackageArtifacts.ReadNuspec();
        var ns = doc.Root!.Name.Namespace;
        return doc.Root.Element(ns + "metadata")?.Element(ns + name)?.Value?.Trim();
    }

    [TestMethod]
    [TestCategory("T-P9-01")]
    /// <summary>
    /// Verifies that the package ID, version, authors and license match the approved release contract.
    /// </summary>
    public void Package_Id_Version_Authors_License_AsDecided()
    {
        Value("id").Should().Be("WeatherParser.Parsing");
        Value("version").Should().Be("1.0.0");
        Value("authors").Should().Be("WeatherParser contributors");

        var metadata = Metadata();
        var ns = metadata.Name.Namespace;
        var license = metadata.Element(ns + "license");
        license.Should().NotBeNull();
        license!.Attribute("type")?.Value.Should().Be("expression");
        license.Value.Should().Be("MIT");
    }

    [TestMethod]
    [TestCategory("T-P9-01")]
    /// <summary>
    /// Verifies that the package repository URL is not present and project URL is null.
    /// </summary>
    public void Package_RepositoryAndProjectUrls_AsDecided()
    {
        Value("projectUrl").Should().BeNull();

        var metadata = Metadata();
        var ns = metadata.Name.Namespace;
        var repository = metadata.Element(ns + "repository");
        repository?.Attribute("url").Should().BeNull();
    }

    [TestMethod]
    [TestCategory("T-P9-01")]
    /// <summary>
    /// Verifies that the package description and tags are present and correct.
    /// </summary>
    public void Package_DescriptionAndTags_Present()
    {
        Value("description").Should().Be("Parsers, normalized output entities, JSON and XML serialization with an XML schema, station selection and a generic MQTT message serializer for DWD observations, OsthessenWetter observations and DWD MOSMIX_S and MOSMIX_L forecasts, with dependency-injection integration (AddWeatherParsing). Timestamps are UTC. The public API may change between 0.x minor versions.");
        Value("tags").Should().Be("weather dwd mosmix forecast observations osthessenwetter mqtt parser");

        Value("description").Should().NotContain("not available yet");
    }

    [TestMethod]
    [TestCategory("T-P9-01")]
    /// <summary>
    /// Verifies that the package release notes link is present and correct.
    /// </summary>
    public void Package_ReleaseNotesLink_Present()
    {
        Value("releaseNotes").Should().Be("Release notes: CHANGELOG.md of the WeatherParser repository, version 1.0.0.");
    }

    [TestMethod]
    [TestCategory("T-P9-01")]
    /// <summary>
    /// Verifies that the package version is consistent across sources.
    /// </summary>
    public void Package_Version_SingleSource()
    {
        Value("version").Should().Be(PackageArtifacts.ProjectVersion);
        Value("version").Should().Be(PackageArtifacts.ConsumerPin);
    }

    [TestMethod]
    [TestCategory("T-P9-01")]
    /// <summary>
    /// Verifies that the restored package file name matches the expected pattern and content is identical.
    /// </summary>
    public void Consumers_RestoreExactSameRunPackage()
    {
        var fileName = Path.GetFileName(PackageArtifacts.RestoredPackagePath);
        string.Equals(fileName, $"weatherparser.parsing.{PackageArtifacts.ProjectVersion}.nupkg", StringComparison.OrdinalIgnoreCase).Should().BeTrue($"the restored package file is {fileName}");

        File.Exists(PackageArtifacts.SameRunPackagePath).Should().BeTrue();

        var restoredBytes = File.ReadAllBytes(PackageArtifacts.RestoredPackagePath);
        var sameRunBytes = File.ReadAllBytes(PackageArtifacts.SameRunPackagePath);
        var restoredHash = SHA512.HashData(restoredBytes);
        var sameRunHash = SHA512.HashData(sameRunBytes);
        restoredHash.Should().Equal(sameRunHash);
    }
}
