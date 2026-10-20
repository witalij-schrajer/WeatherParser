using System.IO;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.SchemaGeneration.Tests;

[TestClass]
public sealed class ReproducibilityTests
{
    [TestMethod]
    public void TwoCleanGenerations_ProduceIdenticalBytesAndSha256()
    {
        var parentDir = Path.Combine(Path.GetTempPath(), "weatherparser-p209-" + Guid.NewGuid().ToString("N"));
        var firstDir = Path.Combine(parentDir, "first");
        var secondDir = Path.Combine(parentDir, "second");

        try
        {
            Directory.CreateDirectory(firstDir);
            Directory.CreateDirectory(secondDir);

            var generator1 = new ExportSchemaGenerator(new ExportSchemaRefiner());
            var bytes1 = generator1.Generate();
            var path1 = Path.Combine(firstDir, "weatherparser-export-v1.xsd");
            File.WriteAllBytes(path1, bytes1);

            var generator2 = new ExportSchemaGenerator(new ExportSchemaRefiner());
            var bytes2 = generator2.Generate();
            var path2 = Path.Combine(secondDir, "weatherparser-export-v1.xsd");
            File.WriteAllBytes(path2, bytes2);

            Directory.GetFiles(firstDir).Select(Path.GetFileName).Should().Equal("weatherparser-export-v1.xsd");
            Directory.GetFiles(secondDir).Select(Path.GetFileName).Should().Equal("weatherparser-export-v1.xsd");

            bytes1.Should().Equal(bytes2);

            var sha1 = Convert.ToHexString(SHA256.HashData(bytes1));
            var sha2 = Convert.ToHexString(SHA256.HashData(bytes2));
            sha1.Should().Be(sha2);
        }
        finally
        {
            if (Directory.Exists(parentDir))
                Directory.Delete(parentDir, true);
        }
    }
}
