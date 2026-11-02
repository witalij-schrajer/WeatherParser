namespace MyWeatherApp;

/// <summary>
/// Writes the current DWD observations of one station as one JSON document.
/// </summary>
public interface IDwdJsonExporter
{
    /// <summary>
    /// Parses the observations of the station and writes one complete JSON document to <paramref name="destination"/>.
    /// </summary>
    /// <param name="stationId">The five-digit CDC station ID, kept as a string with leading zeros, for example 00044.</param>
    /// <param name="destination">The stream that receives the JSON document; the caller owns and disposes it.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    Task ExportAsync(string stationId, Stream destination, CancellationToken cancellationToken);
}
