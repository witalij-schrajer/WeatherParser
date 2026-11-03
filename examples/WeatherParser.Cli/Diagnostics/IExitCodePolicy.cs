namespace WeatherParser.Cli.Diagnostics;

/// <summary>
/// Defines an exit code policy for mapping errors and outcomes to exit codes.
/// </summary>
public interface IExitCodePolicy
{
    /// <summary>
    /// Gets the exit code for a given error ID.
    /// </summary>
    /// <param name="errorId">The error ID.</param>
    /// <returns>The exit code.</returns>
    int ForError(string errorId);

    /// <summary>
    /// Gets the exit code for a command outcome.
    /// </summary>
    /// <param name="outcome">The command outcome.</param>
    /// <returns>The exit code.</returns>
    int ForOutcome(CommandOutcome outcome);

    /// <summary>
    /// Maps a failure detected after retrieval began: cancellation 4, every other failure 1 (CL-11, CL-12).
    /// </summary>
    /// <param name="errorId">The error ID.</param>
    /// <returns>The exit code.</returns>
    int ForRuntimeError(string errorId);
}
