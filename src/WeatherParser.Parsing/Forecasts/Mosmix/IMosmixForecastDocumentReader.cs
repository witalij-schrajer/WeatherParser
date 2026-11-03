namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Reads MOSMIX forecast documents.
/// </summary>
public interface IMosmixForecastDocumentReader
{
    /// <summary>
    /// Reads a MOSMIX_L per-location KMZ whose single placemark must be the requested location.
    /// </summary>
    /// <param name="kmz">The KMZ stream.</param>
    /// <param name="mosmixId">The MOSMIX ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="MosmixForecastFormatException">The file is invalid.</exception>
    Task<MosmixForecastDocument> ReadSingleLocationAsync(Stream kmz, string mosmixId, CancellationToken cancellationToken);

    /// <summary>
    /// Reads a MOSMIX_S all-stations KMZ to its end and returns the requested placemark.
    /// </summary>
    /// <param name="kmz">The KMZ stream.</param>
    /// <param name="mosmixId">The MOSMIX ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="MosmixForecastFormatException">The file is invalid.</exception>
    Task<MosmixForecastDocument> ReadAllStationsAsync(Stream kmz, string mosmixId, CancellationToken cancellationToken);
}
