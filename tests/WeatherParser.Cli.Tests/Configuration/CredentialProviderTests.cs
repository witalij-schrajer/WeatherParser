using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli;
using FluentAssertions;
using Microsoft.Win32.SafeHandles;

namespace WeatherParser.Cli.Tests.Configuration;

[TestClass]
[TestCategory("T-P3-07")]
public sealed class CredentialProviderTests
{
    private const string Marker = "s3cr3t-Marker-7f2a";
    private const uint CurrentUser = 1000;
    private const UnixFileMode Private = UnixFileMode.UserRead | UnixFileMode.UserWrite;

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

    private void SetupFileStatus(uint owner, UnixFileMode mode)
    {
        _fileMode.Setup(f => f.Read(It.Is<SafeFileHandle>(h => !h.IsInvalid && !h.IsClosed))).Returns(new UnixFileStatus(owner, mode));
        _fileMode.Setup(f => f.GetEffectiveUserId()).Returns(CurrentUser);
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
        SetupFileStatus(CurrentUser, Private);
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
        SetupFileStatus(CurrentUser, Private | UnixFileMode.GroupRead);
        var ex = Error(new BrokerSection { Host = "h", CredentialsFile = path });
        ex.Message.Should().Be("The credential file must be owned by the current user and must not be accessible by group or others.");
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
        SetupFileStatus(CurrentUser, Private);
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

    /// <summary>
    /// Verifies that a credential error is thrown when both environment variables are empty.
    /// </summary>
    [TestMethod]
    [DataRow("", "")]
    [DataRow("", null)]
    [DataRow(null, "")]
    [DataRow("mqtt-user", "")]
    public void Credentials_EmptyVariables_Rejected(string username, string password)
    {
        SetEnvironment(username, password);
        var ex = Error(new BrokerSection { Host = "h" });
        ex.Message.Should().Contain("Set both WEATHERPARSER_MQTT_USERNAME and WEATHERPARSER_MQTT_PASSWORD or neither.");
    }

    /// <summary>
    /// Verifies that a credential error is thrown when one environment variable is empty and the file does not exist.
    /// </summary>
    [TestMethod]
    public void Credentials_EmptyVariableAndFile_BothSourcesRejected()
    {
        SetEnvironment("", null);
        var path = Path.Combine(_tempDirectory, "missing.json");
        var ex = Error(new BrokerSection { Host = "h", CredentialsFile = path });
        ex.Message.Should().Be("Broker credentials are given both as environment variables and as a credential file; use one source.");
    }

    /// <summary>
    /// Verifies that a credential error is thrown when the credential file is locked.
    /// </summary>
    [TestMethod]
    public void Credentials_FileLocked_Rejected()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Inconclusive("Mandatory file locks exist only on Windows; the access-denied path is covered by Credentials_ModeReaderFails_Rejected.");
        }

        SetEnvironment(null, null);
        var path = Path.Combine(_tempDirectory, "credentials.json");
        File.WriteAllText(path, "{\"Username\":\"mqtt-user\",\"Password\":\"" + Marker + "\"}");
        using var lockStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var ex = Error(new BrokerSection { Host = "h", CredentialsFile = path });
        ex.Message.Should().Contain("The credential file named by 'Mqtt.Broker.CredentialsFile' cannot be read.");
        ex.Message.Should().NotContain("denied");
    }

    /// <summary>
    /// Verifies that a credential error is thrown when the credential file mode check fails.
    /// </summary>
    [TestMethod]
    public void Credentials_ModeReaderFails_Rejected()
    {
        SetEnvironment(null, null);
        var path = Path.Combine(_tempDirectory, "credentials.json");
        File.WriteAllText(path, "{\"Username\":\"mqtt-user\",\"Password\":\"" + Marker + "\"}");
        _fileMode.Setup(f => f.Read(It.IsAny<SafeFileHandle>())).Throws(new UnauthorizedAccessException("denied"));
        var ex = Error(new BrokerSection { Host = "h", CredentialsFile = path });
        ex.Message.Should().Contain("The credential file named by 'Mqtt.Broker.CredentialsFile' cannot be read.");
        ex.Message.Should().NotContain("denied");
    }

    /// <summary>
    /// Verifies that a credential file readable by others is rejected.
    /// </summary>
    [TestMethod]
    public void Credentials_FileReadableByOthers_Rejected()
    {
        SetEnvironment(null, null);
        var path = Path.Combine(_tempDirectory, "credentials.json");
        File.WriteAllText(path, "{\"Username\":\"mqtt-user\",\"Password\":\"" + Marker + "\"}");
        SetupFileStatus(CurrentUser, Private | UnixFileMode.OtherRead);
        var ex = Error(new BrokerSection { Host = "h", CredentialsFile = path });
        ex.Message.Should().Be("The credential file must be owned by the current user and must not be accessible by group or others.");
    }

    /// <summary>
    /// Verifies that a private credential file owned by another user is rejected (CL-06).
    /// </summary>
    [TestMethod]
    public void Credentials_FileOwnedByAnotherUser_Rejected()
    {
        SetEnvironment(null, null);
        var path = Path.Combine(_tempDirectory, "credentials.json");
        File.WriteAllText(path, "{\"Username\":\"mqtt-user\",\"Password\":\"" + Marker + "\"}");
        SetupFileStatus(CurrentUser + 1, Private);
        var ex = Error(new BrokerSection { Host = "h", CredentialsFile = path });
        ex.Message.Should().Be("The credential file must be owned by the current user and must not be accessible by group or others.");
    }

    /// <summary>
    /// Verifies that without a Unix owner and mode (Windows) the credential file is read without a check (CL-06).
    /// </summary>
    [TestMethod]
    public void Credentials_NoUnixStatus_ReadWithoutCheck()
    {
        SetEnvironment(null, null);
        var path = Path.Combine(_tempDirectory, "credentials.json");
        File.WriteAllText(path, "{\"Username\":\"mqtt-user\",\"Password\":\"" + Marker + "\"}");
        _fileMode.Setup(f => f.Read(It.IsAny<SafeFileHandle>())).Returns((UnixFileStatus?)null);
        var credentials = new CredentialProvider(_environment.Object, _fileMode.Object).GetCredentials(new BrokerSection { Host = "h", CredentialsFile = path });
        credentials.Should().NotBeNull();
        credentials!.Password.Should().Be(Marker);
    }

    /// <summary>
    /// Verifies that the checked file is the file that is read: replacing the path after the check does not change the credentials (CL-06).
    /// </summary>
    [TestMethod]
    public void Credentials_FileReplacedAfterCheck_CheckedFileIsRead()
    {
        SetEnvironment(null, null);
        var path = Path.Combine(_tempDirectory, "credentials.json");
        File.WriteAllText(path, "{\"Username\":\"mqtt-user\",\"Password\":\"" + Marker + "\"}");
        var replaced = false;
        _fileMode.Setup(f => f.Read(It.IsAny<SafeFileHandle>())).Callback(() =>
        {
            try
            {
                File.Move(path, path + ".old");
                File.WriteAllText(path, "{\"Username\":\"other-user\",\"Password\":\"other-password\"}");
                replaced = true;
            }
            catch (IOException)
            {
                // Windows: the open handle denies the rename, so the checked file cannot be replaced at all.
            }
        }).Returns(new UnixFileStatus(CurrentUser, Private));
        _fileMode.Setup(f => f.GetEffectiveUserId()).Returns(CurrentUser);

        var credentials = new CredentialProvider(_environment.Object, _fileMode.Object).GetCredentials(new BrokerSection { Host = "h", CredentialsFile = path });

        credentials!.Username.Should().Be("mqtt-user");
        credentials.Password.Should().Be(Marker);
        replaced.Should().Be(!OperatingSystem.IsWindows());
    }
}
