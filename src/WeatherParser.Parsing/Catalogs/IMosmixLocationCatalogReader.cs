namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Reads the MOSMIX station catalog in the SC-04 format (ISO-8859-1, LF, header tokens ID ICAO NAME LAT LON ELEV, dash line) from a caller-owned stream that stays open.
/// </summary>
public interface IMosmixLocationCatalogReader
{
    /// <summary>
    /// Reads the MOSMIX station catalog in the SC-04 format (ISO-8859-1, LF, header tokens ID ICAO NAME LAT LON ELEV, dash line) from a caller-owned stream that stays open.
    /// </summary>
    /// <param name="catalog">The stream to read the catalog from.</param>
    /// <param name="endpointName">The endpoint name used in diagnostics.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The catalog with records and diagnostics.</returns>
    /// <exception cref="CatalogFormatException">
    /// Thrown when the stream is empty, a line is longer than 2048 characters,
    /// or there is a header or dash-line mismatch.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the stream or endpoint name is null.
    /// </exception>
    Task<MosmixLocationCatalog> ReadAsync(Stream catalog, string endpointName, CancellationToken cancellationToken);
}
