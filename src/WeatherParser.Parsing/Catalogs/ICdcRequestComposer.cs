namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Composes the CDC request targets shared by the selection and the observation sources from the consumer-supplied endpoints (contract SC-09, SC-12, SC-21); no built-in URL.
/// </summary>
public interface ICdcRequestComposer
{
    /// <summary>
    /// The four station lists in the product order tu, ff, rr, sd.
    /// </summary>
    /// <param name="endpoints">The CDC catalog endpoints.</param>
    /// <returns>The four station lists in the product order tu, ff, rr, sd.</returns>
    IReadOnlyList<CdcListRequestTarget> StationLists(CdcCatalogEndpoints endpoints);

    /// <summary>
    /// The station's now archive of one product; an unknown product code throws ArgumentException.
    /// </summary>
    /// <param name="endpoints">The CDC catalog endpoints.</param>
    /// <param name="stationId">The station ID.</param>
    /// <param name="productCode">The product code.</param>
    /// <returns>The station's now archive of one product.</returns>
    CdcArchiveRequestTarget Archive(CdcCatalogEndpoints endpoints, string stationId, string productCode);
}
