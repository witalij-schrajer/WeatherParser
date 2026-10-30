namespace WeatherParser.Cli.Lifetime;

/// <summary>
/// Ends the process with Environment.Exit (CL-18).
/// </summary>
public sealed class ProcessTerminator : IProcessTerminator
{
    /// <inheritdoc/>
    public void Terminate(int exitCode)
    {
        Environment.Exit(exitCode);
    }
}
