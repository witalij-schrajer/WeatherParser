using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeatherParser.Cli.Configuration;

/// <summary>Loads the configuration file of the invocation (CL-04, CL-05).</summary>
public sealed class ConfigurationLoader : IConfigurationLoader
{
    private const string EnvironmentVariable = "WEATHERPARSER_CONFIG";

    private static readonly JsonSerializerOptions Options = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private readonly IEnvironmentReader _environment;

    /// <summary>Initializes a new instance of the <see cref="ConfigurationLoader"/> class.</summary>
    /// <param name="environment">The environment reader to use.</param>
    public ConfigurationLoader(IEnvironmentReader environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        _environment = environment;
    }

    /// <inheritdoc />
    public CliConfiguration Load(string? configPath)
    {
        var path = configPath ?? _environment.Get(EnvironmentVariable);
        if (string.IsNullOrEmpty(path))
        {
            throw new CliException("CLI-CONFIG-MISSING", "No configuration file given; use --Config <path> or the environment variable WEATHERPARSER_CONFIG.");
        }

        if (!File.Exists(path))
        {
            throw new CliException("CLI-CONFIG-MISSING", $"The configuration file '{path}' does not exist.");
        }

        CliConfiguration? configuration;
        try
        {
            configuration = JsonSerializer.Deserialize<CliConfiguration>(File.ReadAllText(path), Options);
        }
        catch (JsonException exception)
        {
            throw new CliException("CLI-CONFIG-INVALID", $"The configuration file '{path}' is invalid: {exception.Message}");
        }

        return configuration ?? throw new CliException("CLI-CONFIG-INVALID", $"The configuration file '{path}' is invalid: it contains no configuration object.");
    }
}
