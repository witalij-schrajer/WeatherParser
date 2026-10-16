using System.Text.Json.Serialization;

namespace WeatherParser.Parsing.Entities;

/// <summary>
/// An exported error object describing a failure encountered while processing a command,
/// per REQ-074 and REQ-072. Contains no credentials or secret configuration.
/// </summary>
[Serializable]
public sealed class ErrorInfo
{
    /// <summary>Stable, machine-readable error code.</summary>
    [JsonPropertyName("error_code")]
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>English, human-readable error message.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>The command that was being executed, e.g. "ParseDWD".</summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>Source or station affected by the error, when applicable.</summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>UTC timestamp when the error occurred, without offset (REQ-051).</summary>
    [JsonPropertyName("occurred_at")]
    public DateTime OccurredAt { get; set; }
}
