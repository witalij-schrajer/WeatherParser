using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace WeatherParser.Package.Tests;

/// <summary>Locates the restored package, the same-run package and the repository files for the package metadata and contents tests (P9.05).</summary>
internal static class PackageArtifacts
{
    /// <summary>
    /// Gets the root folder of the restored package.
    /// </summary>
    public static string PackageRoot
    {
        get
        {
            var assembly = typeof(PackageArtifacts).Assembly;
            var attribute = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "WeatherParserPackageRoot");
            if (attribute is null || string.IsNullOrEmpty(attribute.Value))
            {
                throw new InvalidOperationException("The restored package folder is not known (assembly metadata WeatherParserPackageRoot).");
            }
            return attribute.Value;
        }
    }

    /// <summary>
    /// Gets the root directory of the repository.
    /// </summary>
    public static string RepositoryRoot
    {
        get
        {
            var baseDirectory = AppContext.BaseDirectory;
            var current = new DirectoryInfo(baseDirectory);
            while (current != null)
            {
                if (current.GetFiles("WeatherParser.slnx").Length > 0)
                {
                    return current.FullName;
                }
                current = current.Parent;
            }
            throw new InvalidOperationException("The repository root (WeatherParser.slnx) was not found above the test output directory.");
        }
    }

    /// <summary>
    /// Gets the version of the project.
    /// </summary>
    public static string ProjectVersion
    {
        get
        {
            var csprojPath = Path.Combine(RepositoryRoot, "src", "WeatherParser.Parsing", "WeatherParser.Parsing.csproj");
            var doc = XDocument.Load(csprojPath);
            var version = doc.Descendants("Version").Select(e => e.Value.Trim()).FirstOrDefault(v => v.Length > 0);
            return version ?? throw new InvalidOperationException("The library project file has no Version.");
        }
    }

    /// <summary>
    /// Gets the consumer pin for the package.
    /// </summary>
    public static string ConsumerPin
    {
        get
        {
            var propsPath = Path.Combine(RepositoryRoot, "Directory.Packages.props");
            var doc = XDocument.Load(propsPath);
            var packageVersionElement = doc.Descendants("PackageVersion")
                .FirstOrDefault(e => e.Attribute("Include")?.Value == "WeatherParser.Parsing");
            var version = packageVersionElement?.Attribute("Version")?.Value;
            return version ?? throw new InvalidOperationException("Directory.Packages.props has no PackageVersion for WeatherParser.Parsing.");
        }
    }

    /// <summary>
    /// Gets the path to the restored NuGet package file.
    /// </summary>
    public static string RestoredPackagePath
    {
        get
        {
            var nupkgFiles = Directory.GetFiles(PackageRoot, "*.nupkg", SearchOption.TopDirectoryOnly)
                .Where(f => !f.EndsWith(".snupkg"))
                .ToList();
            if (nupkgFiles.Count != 1)
            {
                throw new InvalidOperationException("Expected exactly one NuGet package file (*.nupkg, not *.snupkg) in the package root.");
            }
            return nupkgFiles[0];
        }
    }

    /// <summary>
    /// Gets the path to the same-run NuGet package file.
    /// </summary>
    public static string SameRunPackagePath => Path.Combine(RepositoryRoot, "artifacts", "local-nuget", $"WeatherParser.Parsing.{ProjectVersion}.nupkg");

    /// <summary>
    /// Reads the .nuspec file from the restored package.
    /// </summary>
    public static XDocument ReadNuspec()
    {
        using var zip = ZipFile.OpenRead(RestoredPackagePath);
        var entry = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".nuspec"));
        if (entry is null)
        {
            throw new InvalidOperationException("The .nuspec file was not found in the package.");
        }
        return XDocument.Load(entry.Open());
    }

    /// <summary>
    /// Gets a list of all entry names in the restored package, sorted.
    /// </summary>
    public static IReadOnlyList<string> EntryNames()
    {
        using var zip = ZipFile.OpenRead(RestoredPackagePath);
        return zip.Entries.Select(e => e.FullName).OrderBy(name => name, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Reads the bytes of a specific entry from the restored package.
    /// </summary>
    public static byte[] ReadEntry(string name)
    {
        using var zip = ZipFile.OpenRead(RestoredPackagePath);
        var entry = zip.Entries.FirstOrDefault(e => e.FullName == name);
        if (entry is null)
        {
            throw new InvalidOperationException($"The entry '{name}' was not found in the package.");
        }
        using var stream = entry.Open();
        var bytes = new byte[entry.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }
}
