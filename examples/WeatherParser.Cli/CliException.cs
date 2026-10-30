using System;

namespace WeatherParser.Cli;

/// <summary>
/// A failure of the example CLI with its stable error ID (CL-10).
/// </summary>
public sealed class CliException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CliException"/> class.
    /// </summary>
    /// <param name="errorId">The stable error ID for this exception.</param>
    /// <param name="message">The message that describes the error.</param>
    public CliException(string errorId, string message) : base(message) => ErrorId = errorId;

    /// <summary>
    /// Gets the stable error ID for this exception.
    /// </summary>
    public string ErrorId { get; }
}
