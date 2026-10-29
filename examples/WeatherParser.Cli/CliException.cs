using System;

namespace WeatherParser.Cli;

/// <summary>
/// A failure of the example CLI with its stable error ID (CL-10).
/// </summary>
public sealed class CliException : Exception
{
    public CliException(string errorId, string message) : base(message) => ErrorId = errorId;
    public string ErrorId { get; }
}
