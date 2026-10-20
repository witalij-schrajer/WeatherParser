using FluentAssertions;

namespace WeatherParser.SchemaGeneration.Tests;

[TestClass]
public sealed class GoldenFileTests
{
    private const string RegenerateVariable = "WEATHERPARSER_REGENERATE_SCHEMAS";

    /// <summary>
    /// Tests that the generated schema matches the committed schema file.
    /// </summary>
    [TestMethod]
    public void GeneratedSchema_MatchesCommittedFile()
    {
        var path = Path.Combine(RepositoryRoot.Find(), "schemas", "export", "v1", "weatherparser-export-v1.xsd");
        IExportSchemaGenerator generator = new ExportSchemaGenerator(new ExportSchemaRefiner());
        byte[] generated = generator.Generate();

        if (Environment.GetEnvironmentVariable(RegenerateVariable) == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, generated);
            Assert.Inconclusive("Regenerated " + path + "; review the difference before committing it.");
        }
        else
        {
            File.Exists(path).Should().BeTrue("the committed schema " + path + " must exist; create it with " + RegenerateVariable + "=1");
            File.ReadAllBytes(path).Should().Equal(generated, "the committed schema must equal a fresh generation; regenerate with " + RegenerateVariable + "=1 and review the difference");
        }
    }
}
