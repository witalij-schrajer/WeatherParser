namespace WeatherParser.Coverage;

/// <summary>
/// Orchestrates the coverage evaluation process.
/// </summary>
public interface ICoverageGate
{
    /// <summary>
    /// Asynchronously evaluates coverage based on the provided request.
    /// </summary>
    /// <param name="request">The coverage gate evaluation request.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous evaluation operation. The value of the TResult parameter contains the coverage evaluation result.</returns>
    Task<CoverageResult> EvaluateAsync(CoverageGateRequest request, CancellationToken cancellationToken);
}
