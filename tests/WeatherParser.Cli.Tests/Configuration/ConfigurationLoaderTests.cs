using FluentAssertions;
using Moq;
using WeatherParser.Cli.Configuration;

namespace WeatherParser.Cli.Tests.Configuration;

[TestClass]
[TestCategory("T-P3-05")]
/// <summary>
/// Verifies loading of the configuration file: location, precedence and strict parsing (CL-04, CL-05, T-P3-05).
/// </summary>
public sealed class ConfigurationLoaderTests
{
    private MockRepository _repository = null!;
    private Mock<IEnvironmentReader> _environment = null!;
    private ConfigurationLoader _subject = null!;
    private string _directory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _repository = new(MockBehavior.Strict);
        _environment = _repository.Create<IEnvironmentReader>();
        _subject = new(_environment.Object);
        _directory = Path.Combine(Path.GetTempPath(), "weatherparser-config-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _repository.VerifyAll();
        Directory.Delete(_directory, true);
    }

    private string WriteFile(string name, string content)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, content, new System.Text.UTF8Encoding(false));
        return path;
    }

    private CliException LoadError(string? path)
    {
        Action act = () => _subject.Load(path);
        return act.Should().Throw<CliException>().Which;
    }

    [TestMethod]
    /// <summary>
    /// Loads configuration from a specified file path.
    /// </summary>
    public void Load_ConfigOptionPath_Used()
    {
        var path = WriteFile("config.json", """{"Dwd":{"ReferenceStationId":"00044"}}""");
        _subject.Load(path).Dwd!.ReferenceStationId.Should().Be("00044");
    }

    [TestMethod]
    /// <summary>
    /// Uses the environment variable path when no option is provided.
    /// </summary>
    public void Load_EnvironmentVariablePath_UsedWhenNoOption()
    {
        var path = WriteFile("config.json", """{"Dwd":{"ReferenceStationId":"00044"}}""");
        _environment.Setup(e => e.Get("WEATHERPARSER_CONFIG")).Returns(path);
        _subject.Load(null).Dwd!.ReferenceStationId.Should().Be("00044");
    }

    [TestMethod]
    /// <summary>
    /// Ensures that the option path overrides the environment variable.
    /// </summary>
    public void Load_OptionOverridesEnvironmentVariable()
    {
        var optionPath = WriteFile("option.json", """{"Dwd":{"ReferenceStationId":"00044"}}""");
        var envPath = WriteFile("env.json", """{"Dwd":{"ReferenceStationId":"00045"}}""");
        _subject.Load(optionPath).Dwd!.ReferenceStationId.Should().Be("00044");
    }

    [TestMethod]
    /// <summary>
    /// Throws an exception when no configuration path is provided and environment variable is missing.
    /// </summary>
    public void Load_NoPath_ConfigurationMissing()
    {
        _environment.Setup(e => e.Get("WEATHERPARSER_CONFIG")).Returns((string?)null);
        var ex = LoadError(null);
        ex.ErrorId.Should().Be("CLI-CONFIG-MISSING");
    }

    [TestMethod]
    /// <summary>
    /// Throws an exception when the environment variable is empty.
    /// </summary>
    public void Load_EmptyEnvironmentVariable_ConfigurationMissing()
    {
        _environment.Setup(e => e.Get("WEATHERPARSER_CONFIG")).Returns("");
        var ex = LoadError(null);
        ex.ErrorId.Should().Be("CLI-CONFIG-MISSING");
    }

    [TestMethod]
    /// <summary>
    /// Throws an exception when the configuration file is missing.
    /// </summary>
    public void Load_FileMissing_ConfigurationMissing()
    {
        var path = Path.Combine(_directory, "missing.json");
        var ex = LoadError(path);
        ex.ErrorId.Should().Be("CLI-CONFIG-MISSING");
    }

    [TestMethod]
    /// <summary>
    /// Throws an exception when the JSON is malformed.
    /// </summary>
    public void Load_MalformedJson_ConfigurationInvalid()
    {
        var path = WriteFile("config.json", "{");
        var ex = LoadError(path);
        ex.ErrorId.Should().Be("CLI-CONFIG-INVALID");
    }

    [TestMethod]
    /// <summary>
    /// Throws an exception when the JSON is a null literal.
    /// </summary>
    public void Load_NullLiteral_ConfigurationInvalid()
    {
        var path = WriteFile("config.json", "null");
        var ex = LoadError(path);
        ex.ErrorId.Should().Be("CLI-CONFIG-INVALID");
    }

    [DataRow("{\"Dwdd\":{}}")]
    [DataRow("{\"Mqtt\":{\"Broker\":{\"Pasword\":\"x\"}}}")] // typo
    [DataRow("{\"Dwd\":{\"Catalog\":{\"Foo\":1}}}")]
    [DataRow("{\"Mqtt\":{\"Topics\":{\"Observation\":{\"TopicTemplate\":\"a/b\",\"QualityOfService\":0}}}}")]
    [DataRow("{\"Mqtt\":{\"Topics\":{\"CdcStationList\":{\"TopicTemplate\":\"a/b\",\"Retain\":true}}}}")]
    [DataRow("{\"dwd\":{}}")]
    [TestMethod]
    /// <summary>
    /// Throws an exception when the configuration contains unknown members.
    /// </summary>
    public void Load_UnknownMember_ConfigurationInvalid(string json)
    {
        var path = WriteFile("config.json", json);
        var ex = LoadError(path);
        ex.ErrorId.Should().Be("CLI-CONFIG-INVALID");
    }

    [DataRow("{\"Dwd\":{} /* c */}")]
    [DataRow("{\"Dwd\":{},}")]
    [TestMethod]
    /// <summary>
    /// Throws an exception when the configuration contains comments or trailing commas.
    /// </summary>
    public void Load_CommentsAndTrailingCommas_ConfigurationInvalid(string json)
    {
        var path = WriteFile("config.json", json);
        var ex = LoadError(path);
        ex.ErrorId.Should().Be("CLI-CONFIG-INVALID");
    }

    [TestMethod]
    /// <summary>
    /// Ensures that configuration is not implicitly searched in the current directory.
    /// </summary>
    public void Load_NoImplicitSearch()
    {
        WriteFile("weatherparser.json", """{"Dwd":{"ReferenceStationId":"00044"}}""");
        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_directory);
            _environment.Setup(e => e.Get("WEATHERPARSER_CONFIG")).Returns((string?)null);
            var ex = LoadError(null);
            ex.ErrorId.Should().Be("CLI-CONFIG-MISSING");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [TestMethod]
    /// <summary>
    /// Parses durations, URIs and library types correctly.
    /// </summary>
    public void Load_DurationsUrisAndLibraryTypes_Parsed()
    {
        var json = """
            {"Limits":{"CommandDeadline":"00:05:00","MaxPublishAttempts":2},"Mosmix":{"L":{"SingleStationsDirectory":"https://example.invalid/l/","MaxRunAge":"08:00:00"},"Catalog":{"MaxEvidenceCandidates":7}},"OsthessenWetter":{"Stations":[{"Name":"Test","Latitude":50.5,"Longitude":9.7,"AltitudeM":300}]}}
            """;
        var path = WriteFile("config.json", json);
        var config = _subject.Load(path);
        config.Limits!.CommandDeadline.Should().Be(TimeSpan.FromMinutes(5));
        config.Limits.MaxPublishAttempts.Should().Be(2);
        config.Mosmix!.L!.SingleStationsDirectory.Should().Be(new Uri("https://example.invalid/l/"));
        config.Mosmix.L.MaxRunAge.Should().Be(TimeSpan.FromHours(8));
        config.Mosmix.Catalog!.MaxEvidenceCandidates.Should().Be(7);
        config.OsthessenWetter!.Stations![0].Latitude.Should().Be(50.5m);
    }

    [TestMethod]
    /// <summary>
    /// Throws an exception when the configuration file is locked.
    /// </summary>
    [TestCategory("T-P3-05")]
    public void Load_FileLocked_ConfigurationInvalid()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("Mandatory file locks exist only on Windows.");
        }

        var path = WriteFile("config.json", """{"Dwd":{"ReferenceStationId":"00044"}}""");
        using var lockStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var ex = LoadError(path);
        ex.ErrorId.Should().Be("CLI-CONFIG-INVALID");
        ex.Message.Should().Contain($"The configuration file '{path}' cannot be read.");
    }
}
