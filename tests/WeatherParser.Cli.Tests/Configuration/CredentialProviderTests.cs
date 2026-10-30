using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli;
using FluentAssertions;

namespace WeatherParser.Cli.Tests.Configuration;

[TestClass]
[TestCategory("T-P3-07")]
public sealed class CredentialProviderTests
{
    private const string Marker = "s3cr3t-Marker-7f2a";

    private readonly MockRepository _mocks = new(MockBehavior.Strict);
    private readonly Mock<IEnvironmentReader> _environment;
    private readonly Mock<IFileModeReader> _fileMode;
    private readonly string _tempDirectory;

    public CredentialProviderTests()
    {
        _environment = _mocks.Create<IEnvironmentReader>();
        _fileMode = _mocks.Create<IFileModeReader>();
        _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            _mocks.VerifyAll();
        }
        finally
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
    }

    private void SetEnvironment(string? username, string? password)
    {
        _environment.Setup(e => e.Get("WEATHERPARSER_MQTT_USERNAME")).Returns(username);
        _environment.Setup(e => e.Get("WEATHERPARSER_MQTT_PASSWORD")).Returns(password);
    }

    private CliException Error(BrokerSection? broker)
    {
        var ex = FluentActions.Invoking(() => new CredentialProvider(_environment.Object, _fileMode.Object).GetCredentials(broker)).Should().Throw<CliException>().Which;
        ex.ErrorId.Should().Be("CLI-CREDENTIALS-INVALID");
        ex.Message.Should().NotContain(Marker);
        ex.Message.Should().NotContain("mqtt-user");
        return ex;
    }

    /// <summary>
    /// Verifies that credentials can be retrieved from environment variables.
    /// </summary>
    [TestMethod]
    public void Credentials_FromEnvironmentVariables()
    {
        SetEnvironment("mqtt-user", Marker);
        var broker = new BrokerSection { Host = "h" };
        var credentials = new CredentialProvider(_environment.Object, _fileMode.Object).GetCredentials(broker);
        credentials.Should().NotBeNull();
        credentials.Username.Should().Be("mqtt-user");
        credentials.Password.Should().Be(Marker);
    }

    /// <summary>
    /// Verifies that a credential error is thrown when only one environment variable is set.
    /// </summary>
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Credentials_OnlyOneVariable_Rejected(bool first)
    {
        SetEnvironment(first ? "mqtt-user" : null, first ? null : Marker);
        var ex = Error(new BrokerSection { Host = "h" });
        ex.Message.Should().Contain("Set both WEATHERPARSER_MQTT_USERNAME and WEATHERPARSER_MQTT_PASSWORD or neither.");
    }

    /// <summary>
    /// Verifies that a credential error is thrown when both environment variables and a credential file are specified.
    /// </summary>
    [TestMethod]
    public void Credentials_EnvironmentAndFile_Rejected()
    {
        SetEnvironment("mqtt-user", Marker);
        var path = Path.Combine(_tempDirectory, "credentials.json");
        var ex = Error(new BrokerSection { Host = "h", CredentialsFile = path });
        ex.Message.Should().Contain("both as environment variables and as a credential file");
    }

    /// <summary>
    /// Verifies that credentials can be retrieved from a credential file.
    /// </summary>
    [TestMethod]
    public void Credentials_FromCredentialFile()
    {
        SetEnvironment(null, null);
        var path = Path.Combine(_tempDirectory, "credentials.json");
        File.WriteAllText(path, "{\"Username\":\"mqtt-user\",\"Password\":\"" + Marker + "\"}");
        _fileMode.Setup(f => f.HasGroupOrOtherAccess(path)).Returns(false);
        var credentials = new CredentialProvider(_environment.Object, _fileMode.Object).GetCredentials(new BrokerSection { Host = "h", CredentialsFile = path });
        credentials.Should().NotBeNull();
        credentials.Username.Should().Be("mqtt-user");
        credentials.Password.Should().Be(Marker);
    }

    /// <summary>
    /// Verifies that a credential error is thrown when the credential file is missing.
    /// </summary>
    [TestMethod]
    public void Credentials_FileMissing_Rejected()
    {
        SetEnvironment(null, null);
        var path = Path.Combine(_tempDirectory, "missing.json");
        var ex = Error(new BrokerSection { Host = "h", CredentialsFile = path });
        ex.Message.Should().Contain("does not exist");
    }

    /// <summary>
    /// Verifies that a credential error is thrown when the credential file is accessible by others.
    /// </summary>
    [TestMethod]
    public void Credentials_FileAccessibleByOthers_Rejected()
    {
        SetEnvironment(null, null);
        var path = Path.Combine(_tempDirectory, "credentials.json");
        File.WriteAllText(path, "{\"Username\":\"mqtt-user\",\"Password\":\"" + Marker + "\"}");
        _fileMode.Setup(f => f.HasGroupOrOtherAccess(path)).Returns(true);
        var ex = Error(new BrokerSection { Host = "h", CredentialsFile = path });
        ex.Message.Should().Contain("must not be accessible by group or others");
    }

    /// <summary>
    /// Verifies that a credential error is thrown when the credential file has bad content.
    /// </summary>
    [TestMethod]
    [DataRow("{\"Username\":\"mqtt-user\",\"Password\":\"s3cr3t-Marker-7f2a\",\"Extra\":1}")]
    [DataRow("{\"Username\":\"mqtt-user\",\"Password\":\"\"}")]
    [DataRow("{\"Username\":\"mqtt-user\"}")]
    [DataRow("[1]")]
    [DataRow("{")]
    public void Credentials_FileBadContent_Rejected(string content)
    {
        SetEnvironment(null, null);
        var path = Path.Combine(_tempDirectory, "credentials.json");
        File.WriteAllText(path, content);
        _fileMode.Setup(f => f.HasGroupOrOtherAccess(path)).Returns(false);
        var ex = Error(new BrokerSection { Host = "h", CredentialsFile = path });
        ex.Message.Should().Contain("must contain exactly the members Username and Password");
    }

    /// <summary>
    /// Verifies that no credentials are returned when none are configured.
    /// </summary>
    [TestMethod]
    public void Credentials_None_Anonymous()
    {
        SetEnvironment(null, null);
        var credentials = new CredentialProvider(_environment.Object, _fileMode.Object).GetCredentials(null);
        credentials.Should().BeNull();

        credentials = new CredentialProvider(_environment.Object, _fileMode.Object).GetCredentials(new BrokerSection { Host = "h" });
        credentials.Should().BeNull();
    }
}
