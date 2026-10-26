namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// A matched station that yields no dataset (contract OW-08).
/// </summary>
/// <param name="StationName">The overview station name as shown on the page.</param>
/// <param name="ErrorId">OSTHESSEN-STATION-OFFLINE, OSTHESSEN-STATION-METADATA-MISSING or OSTHESSEN-STATION-NO-READING.</param>
/// <param name="Reason">An English explanation without page text or values.</param>
public sealed record OsthessenWetterStationFailure(string StationName, string ErrorId, string Reason);
