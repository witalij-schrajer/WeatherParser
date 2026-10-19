namespace WeatherParser.Coverage.Runner;

/// <summary>
/// Main runner application entry point.
/// </summary>
public interface IRunnerApplication
{
    /// <summary>
    /// Asynchronously runs the application with the specified command-line arguments.
    /// </summary>
    /// <param name="args">The command-line arguments to process.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The exit code of the application.</returns>
    Task<int> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken);
}
