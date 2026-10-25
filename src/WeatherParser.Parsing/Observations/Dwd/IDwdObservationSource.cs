using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Observations.Dwd;

/// <summary>
/// Retrieves the four CDC station lists and a station's now archives through the catalog downloader under the observation deadline (contract OC-03, OC-09, OC-11).
/// </summary>
public interface IDwdObservationSource
{
    /// <summary>
    /// Reads the four station lists in the product order tu, ff, rr, sd; a list that cannot be retrieved or read fails.
    /// </summary>
    /// <param name="deadline">The deadline for the operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The station lists.</returns>
    Task<IReadOnlyList<CdcProductList>> ReadStationListsAsync(CancellationToken deadline, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the station's now archive of each given product; HTTP 404 gives a null value for that product (OBSERVATION-PRODUCT-MISSING is logged by the caller); every other failure propagates.
    /// </summary>
    /// <param name="stationId">The station ID.</param>
    /// <param name="productCodes">The product codes.</param>
    /// <param name="deadline">The deadline for the operation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The archives.</returns>
    Task<IReadOnlyDictionary<string, CdcNowEvidence?>> ReadArchivesAsync(string stationId, IReadOnlyList<string> productCodes, CancellationToken deadline, CancellationToken cancellationToken);
}
