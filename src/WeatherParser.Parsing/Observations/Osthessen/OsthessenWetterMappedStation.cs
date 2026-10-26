using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// One overview station mapped to its station ID, location and readings (contract OW-03, OW-04, OW-06, OW-07).
/// </summary>
/// <param name="StationId">The station ID slug (OW-03).</param>
/// <param name="Name">The overview station name as shown on the page.</param>
/// <param name="Status">The status shown on the page; an offline station has no readings.</param>
/// <param name="Location">The location from the consumer metadata, or null when the consumer supplied none (OW-04).</param>
/// <param name="Readings">The readings for the dataset builder: missing values and missing times are passed on for the builder to omit.</param>
/// <param name="Notices">The notices for unreadable values and unresolved times.</param>
public sealed record OsthessenWetterMappedStation(
    string StationId,
    string Name,
    OsthessenWetterStationStatus Status,
    ObservationLocation? Location,
    IReadOnlyList<ObservationReading> Readings,
    IReadOnlyList<OsthessenWetterMappingNotice> Notices);
