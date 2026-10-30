using FluentAssertions;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;

namespace WeatherParser.Cli.Tests.Configuration;

/// <summary>
/// Verifies that the committed example configuration loads and passes the configuration-validation phase of every command in JSON mode, and contains no credential (CL-04 to CL-06, T-P3-05).
/// </summary>
[TestClass]
public sealed class ExampleConfigurationTests
{
    /// <summary>
    /// Verifies the committed example configuration.
    /// </summary>
    [TestMethod]
    [TestCategory("T-P3-05")]
    public void Load_ExampleConfiguration_IsValid()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(directory.FullName, "WeatherParser.slnx")))
        {
            directory = directory.Parent!;
        }

        var examplePath = Path.Combine(directory.FullName, "examples", "weatherparser.example.json");
        var configuration = new ConfigurationLoader(new EnvironmentReader()).Load(examplePath);

        // The committed placeholder is a Linux path; the library requires a fully qualified path on the test platform.
        configuration.Mosmix!.Catalog!.EvidenceCacheDirectory = Path.GetTempPath();
        var validator = new ConfigurationValidator(new ReferenceStationResolver(), new CredentialProvider(new EnvironmentReader(), new FileModeReader()));

        foreach (var command in Enum.GetValues<CliCommand>())
        {
            var request = TestConfigurations.Request(command, OutputFormat.Json);
            using var provider = TestConfigurations.Build(configuration, request);
            FluentActions.Invoking(() => validator.Validate(provider, configuration, request)).Should().NotThrow();
        }

        var text = File.ReadAllText(examplePath);
        text.Should().NotContain("\"Password\"");
        text.Should().NotContain("\"Username\"");
    }
}
