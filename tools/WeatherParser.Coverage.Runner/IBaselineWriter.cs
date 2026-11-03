namespace WeatherParser.Coverage.Runner;

/// <summary>
/// Writes baseline metadata from a coverage report.
/// </summary>
public interface IBaselineWriter
{
    /// <summary>
    /// Asynchronously writes baseline metadata for the specified command.
    /// </summary>
    /// <param name="command">The write-baseline command containing input and output paths.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    Task WriteAsync(WriteBaselineCommand command, CancellationToken cancellationToken);
}
