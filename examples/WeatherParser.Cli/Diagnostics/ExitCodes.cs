namespace WeatherParser.Cli.Diagnostics;

/// <summary>
/// Provides constants for exit codes.
/// </summary>
public static class ExitCodes
{
    /// <summary>
    /// Success exit code.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// Failure exit code.
    /// </summary>
    public const int Failure = 1;

    /// <summary>
    /// Usage or configuration error before retrieval exit code.
    /// </summary>
    public const int Usage = 2;

    /// <summary>
    /// Partial failure exit code.
    /// </summary>
    public const int Partial = 3;

    /// <summary>
    /// Cancelled exit code.
    /// </summary>
    public const int Cancelled = 4;
}
