using WeatherParser.Cli.Arguments;

namespace WeatherParser.Cli.Diagnostics;

/// <summary>
/// Provides an interface for classifying failures.
/// </summary>
public interface IFailureClassifier
{
    /// <summary>
    /// Classifies a failure based on the command and error ID.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="errorId">The error ID.</param>
    /// <returns>The failure kind.</returns>
    FailureKind Classify(CliCommand command, string errorId);
}
