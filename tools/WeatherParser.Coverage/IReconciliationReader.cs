namespace WeatherParser.Coverage;

/// <summary>
/// Reads the recorded baseline reconciliations (<c>coverage-reconciliation.json</c>).
/// </summary>
public interface IReconciliationReader
{
    /// <summary>
    /// Reads the recorded baseline reconciliations.
    /// </summary>
    /// <param name="path">The path to the reconciliation file.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The entries; an empty list when the path is null, empty or the file does not exist.</returns>
    /// <exception cref="CoverageInputException">Thrown when the file is not valid JSON or an entry is invalid.</exception>
    Task<IReadOnlyList<ReconciliationEntry>> ReadAsync(string? path, CancellationToken cancellationToken);
}
