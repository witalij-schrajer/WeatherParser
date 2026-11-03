namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Reads one CDC <c>now</c> station list in the SC-02 format (ISO-8859-1, header and dash line, fixed character positions) from a caller-supplied stream that stays open.
/// </summary>
public interface ICdcStationCatalogReader
{
    /// <summary>
    /// Reads one CDC <c>now</c> station list in the SC-02 format (ISO-8859-1, header and dash line, fixed character positions) from a caller-supplied stream that stays open.
    /// </summary>
    /// <param name="catalog">The stream to read the catalog from.</param>
    /// <param name="endpointName">The endpoint name used in diagnostics.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The catalog with records and diagnostics.</returns>
    /// <exception cref="WeatherParser.Parsing.Exceptions.CatalogFormatException">
    /// Thrown when the stream is empty, a line is longer than 2048 characters,
    /// or there is a header or dash-line mismatch.
    /// </exception>
    Task<CdcStationCatalog> ReadAsync(Stream catalog, string endpointName, CancellationToken cancellationToken);
}
