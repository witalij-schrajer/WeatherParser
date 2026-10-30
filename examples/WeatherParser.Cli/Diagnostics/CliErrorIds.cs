using System;
using System.Collections.Generic;
using System.Linq;

namespace WeatherParser.Cli.Diagnostics;

/// <summary>
/// Provides constants for CLI error IDs.
/// </summary>
public static class CliErrorIds
{
    /// <summary>
    /// CLI-ARGUMENT-INVALID
    /// </summary>
    public const string ArgumentInvalid = "CLI-ARGUMENT-INVALID";

    /// <summary>
    /// CLI-COMMAND-MISSING
    /// </summary>
    public const string CommandMissing = "CLI-COMMAND-MISSING";

    /// <summary>
    /// CLI-COMMAND-CONFLICT
    /// </summary>
    public const string CommandConflict = "CLI-COMMAND-CONFLICT";

    /// <summary>
    /// CLI-CONFIG-MISSING
    /// </summary>
    public const string ConfigMissing = "CLI-CONFIG-MISSING";

    /// <summary>
    /// CLI-CONFIG-INVALID
    /// </summary>
    public const string ConfigInvalid = "CLI-CONFIG-INVALID";

    /// <summary>
    /// CLI-CREDENTIALS-INVALID
    /// </summary>
    public const string CredentialsInvalid = "CLI-CREDENTIALS-INVALID";

    /// <summary>
    /// CLI-DWD-STATION-MISSING
    /// </summary>
    public const string DwdStationMissing = "CLI-DWD-STATION-MISSING";

    /// <summary>
    /// CLI-OSTHESSEN-PUBLICATION-NOT-PERMITTED
    /// </summary>
    public const string OsthessenPublicationNotPermitted = "CLI-OSTHESSEN-PUBLICATION-NOT-PERMITTED";

    /// <summary>
    /// CLI-OUTPUT-EXISTS
    /// </summary>
    public const string OutputExists = "CLI-OUTPUT-EXISTS";

    /// <summary>
    /// CLI-OUTPUT-FAILED
    /// </summary>
    public const string OutputFailed = "CLI-OUTPUT-FAILED";

    /// <summary>
    /// CLI-MQTT-CONNECT-FAILED
    /// </summary>
    public const string MqttConnectFailed = "CLI-MQTT-CONNECT-FAILED";

    /// <summary>
    /// CLI-MQTT-PUBLISH-FAILED
    /// </summary>
    public const string MqttPublishFailed = "CLI-MQTT-PUBLISH-FAILED";

    /// <summary>
    /// CLI-MQTT-PUBLISH-TIMEOUT
    /// </summary>
    public const string MqttPublishTimeout = "CLI-MQTT-PUBLISH-TIMEOUT";

    /// <summary>
    /// CLI-MQTT-MESSAGE-REFUSED
    /// </summary>
    public const string MqttMessageRefused = "CLI-MQTT-MESSAGE-REFUSED";

    /// <summary>
    /// CLI-CANCELLED
    /// </summary>
    public const string Cancelled = "CLI-CANCELLED";

    /// <summary>
    /// CLI-DEADLINE-EXCEEDED
    /// </summary>
    public const string DeadlineExceeded = "CLI-DEADLINE-EXCEEDED";

    /// <summary>
    /// CLI-UNEXPECTED
    /// </summary>
    public const string Unexpected = "CLI-UNEXPECTED";

    /// <summary>
    /// CLI-PUBLICATION-SUMMARY
    /// </summary>
    public const string PublicationSummary = "CLI-PUBLICATION-SUMMARY";

    /// <summary>
    /// Gets all CLI error IDs.
    /// </summary>
    public static IReadOnlyList<string> All { get; } = new[]
    {
        ArgumentInvalid,
        CommandMissing,
        CommandConflict,
        ConfigMissing,
        ConfigInvalid,
        CredentialsInvalid,
        DwdStationMissing,
        OsthessenPublicationNotPermitted,
        OutputExists,
        OutputFailed,
        MqttConnectFailed,
        MqttPublishFailed,
        MqttPublishTimeout,
        MqttMessageRefused,
        Cancelled,
        DeadlineExceeded,
        Unexpected,
        PublicationSummary
    };
}
