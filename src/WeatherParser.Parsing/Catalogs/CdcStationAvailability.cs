namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// An active station with its available REQ-052 measurement names in ordinal order.
/// </summary>
/// <param name="Station">The candidate station.</param>
/// <param name="AvailableMeasurements">The available measurements in ordinal order.</param>
public sealed record CdcStationAvailability(CdcCandidateStation Station, IReadOnlyList<string> AvailableMeasurements);
