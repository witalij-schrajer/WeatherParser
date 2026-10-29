namespace WeatherParser.Cli;

/// <summary>
/// Runs the one command of a CLI invocation (REQ-006).
/// </summary>
public interface ICommandRunner
{
    /// <summary>
    /// Runs exactly one command and returns the process exit code.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The process exit code.</returns>
    Task<int> RunAsync(string[] args, CancellationToken cancellationToken);
}
