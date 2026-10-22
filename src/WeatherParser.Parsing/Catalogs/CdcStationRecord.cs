namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// One record of a CDC <c>now</c> station list (SC-02).
/// Internal model, not an export entity.
/// </summary>
/// <param name="StationId">The station ID, kept as a string with leading zeros.</param>
/// <param name="FromDate">The start date for the record.</param>
/// <param name="ToDate">The end date for the record.</param>
/// <param name="AltitudeM">The altitude in meters, or null if not available.</param>
/// <param name="Latitude">The latitude.</param>
/// <param name="Longitude">The longitude.</param>
/// <param name="Name">The station name.</param>
/// <param name="FederalState">The federal state.</param>
/// <param name="LineNumber">The 1-based physical line number.</param>
public sealed record CdcStationRecord(
    string StationId,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal? AltitudeM,
    decimal Latitude,
    decimal Longitude,
    string Name,
    string FederalState,
    int LineNumber);
