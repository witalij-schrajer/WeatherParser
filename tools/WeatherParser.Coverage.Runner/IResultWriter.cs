namespace WeatherParser.Coverage.Runner;

/// <summary>
/// Writes coverage evaluation results as JSON.
/// </summary>
public interface IResultWriter
{
    /// <summary>
    /// Asynchronously writes the coverage result to the output.
    /// </summary>
    /// <param name="result">The coverage result to write.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    Task WriteAsync(CoverageResult result, CancellationToken cancellationToken);
}
