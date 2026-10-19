namespace WeatherParser.Coverage;

/// <summary>
/// Reads and parses native Microsoft.CodeCoverage XML reports.
/// </summary>
public interface ICoverageReportReader
{
    /// <summary>
    /// Asynchronously reads and parses a coverage report from the specified file path.
    /// </summary>
    /// <param name="path">The path to the coverage report file.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous read operation. The value of the TResult parameter contains the parsed coverage report.</returns>
    Task<CoverageReport> ReadAsync(string path, CancellationToken cancellationToken);
}
