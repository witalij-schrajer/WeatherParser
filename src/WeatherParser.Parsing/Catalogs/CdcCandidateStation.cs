namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// A station of the union of the lists that has at least one candidate product.
/// </summary>
/// <param name="StationId">The station ID.</param>
/// <param name="AltitudeM">The altitude in meters, or null if not available.</param>
/// <param name="Latitude">The latitude.</param>
/// <param name="Longitude">The longitude.</param>
/// <param name="Name">The station name.</param>
/// <param name="FederalState">The federal state.</param>
/// <param name="CandidateProducts">The candidate products in ordinal order.</param>
public sealed record CdcCandidateStation(
    string StationId,
    decimal? AltitudeM,
    decimal Latitude,
    decimal Longitude,
    string Name,
    string FederalState,
    IReadOnlyList<string> CandidateProducts);
