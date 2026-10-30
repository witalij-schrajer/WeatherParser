namespace WeatherParser.Cli.Lifetime;

/// <summary>
/// Ends the process at once; the seam over Environment.Exit (CL-18).
/// </summary>
public interface IProcessTerminator
{
    /// <summary>
    /// Ends the process immediately with the exit code.
    /// </summary>
    /// <param name="exitCode">The exit code.</param>
    void Terminate(int exitCode);
}
