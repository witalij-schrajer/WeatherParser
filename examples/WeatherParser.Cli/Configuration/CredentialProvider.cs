using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeatherParser.Cli.Configuration;

/// <summary>
/// Provides credentials for accessing an MQTT broker.
/// </summary>
public sealed class CredentialProvider : ICredentialProvider
{
    private const string UsernameVariable = "WEATHERPARSER_MQTT_USERNAME";
    private const string PasswordVariable = "WEATHERPARSER_MQTT_PASSWORD";
    private const string CredentialsInvalid = "CLI-CREDENTIALS-INVALID";
    private static readonly JsonSerializerOptions Options = new() { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

    private readonly IEnvironmentReader _environment;
    private readonly IFileModeReader _fileModeReader;

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialProvider"/> class.
    /// </summary>
    /// <param name="environment">The environment reader to use.</param>
    /// <param name="fileModeReader">The file mode reader to use.</param>
    public CredentialProvider(IEnvironmentReader environment, IFileModeReader fileMode)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(fileMode);
        _environment = environment;
        _fileModeReader = fileMode;
    }

    /// <inheritdoc />
    /// <summary>
    /// Gets the credentials for the specified broker section.
    /// </summary>
    /// <param name="broker">The broker section to get credentials for.</param>
    /// <returns>The broker credentials, or null if not found.</returns>
    public BrokerCredentials? GetCredentials(BrokerSection? broker)
    {
        var username = _environment.Get(UsernameVariable);
        var password = _environment.Get(PasswordVariable);
        var fromEnvironment = !string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password);
        var file = broker?.CredentialsFile;

        if (fromEnvironment && file != null)
        {
            throw new CliException(CredentialsInvalid, "Broker credentials are given both as environment variables and as a credential file; use one source.");
        }

        if (fromEnvironment)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new CliException(CredentialsInvalid, "Set both WEATHERPARSER_MQTT_USERNAME and WEATHERPARSER_MQTT_PASSWORD or neither.");
            }

            return new BrokerCredentials(username, password);
        }

        return file == null ? null : ReadFile(file);
    }

    /// <summary>
    /// Reads the credentials from the specified file.
    /// </summary>
    /// <param name="path">The path to the credential file.</param>
    /// <returns>The broker credentials read from the file.</returns>
    private BrokerCredentials ReadFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new CliException(CredentialsInvalid, "The credential file named by 'Mqtt.Broker.CredentialsFile' does not exist.");
        }

        if (_fileModeReader.HasGroupOrOtherAccess(path))
        {
            throw new CliException(CredentialsInvalid, "The credential file must not be accessible by group or others.");
        }

        CredentialFile? content;
        try
        {
            content = JsonSerializer.Deserialize<CredentialFile>(File.ReadAllText(path), Options);
        }
        catch (JsonException)
        {
            content = null;
        }

        // The exception text is never used: it could quote the file content.
        if (string.IsNullOrEmpty(content?.Username) || string.IsNullOrEmpty(content.Password))
        {
            throw new CliException(CredentialsInvalid, "The credential file must contain exactly the members Username and Password, both non-empty.");
        }

        return new BrokerCredentials(content.Username, content.Password);
    }

    /// <summary>
    /// Represents a credential file.
    /// </summary>
    private sealed class CredentialFile
    {
        /// <summary>
        /// Gets or sets the username from the credential file.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the password from the credential file.
        /// </summary>
        public string? Password { get; set; }
    }
}
