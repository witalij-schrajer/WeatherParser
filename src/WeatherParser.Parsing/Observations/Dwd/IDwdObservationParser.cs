using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations;

namespace WeatherParser.Parsing.Observations.Dwd;

/// <summary>
/// Parses the DWD 10-minute now observations of one explicitly given CDC station into an observation dataset (contract OC-01 to OC-11).
/// </summary>
public interface IDwdObservationParser
{
    /// <summary>
    /// Validates the ID before any request, reads the station lists, resolves the station, reads its archives, maps the rows and builds the dataset; notices and omissions are logged with stable IDs.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The observation dataset.</returns>
    /// <exception cref="DwdStationException">
    /// STATION-ID-INVALID, STATION-UNKNOWN or STATION-METADATA-CONFLICT.
    /// </exception>
    /// <exception cref="ObservationLimitException">
    /// OBSERVATION-DEADLINE-EXCEEDED.
    /// </exception>
    /// <exception cref="CatalogRetrievalException">
    /// Retrieval failures.
    /// </exception>
    /// <exception cref="CatalogFormatException">
    /// Format failures.
    /// </exception>
    /// <exception cref="ObservationDatasetRejectedException">
    /// No usable reading (C-60).
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// The caller cancelled.
    /// </exception>
    Task<ObservationDataset> ParseAsync(string stationId, CancellationToken cancellationToken);
}
