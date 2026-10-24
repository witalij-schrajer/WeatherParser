namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// One syntactically valid record of the MOSMIX station catalog (SC-04).
/// </summary>
/// <param name="MosmixId">The trimmed catalog ID kept as a string (4 or 5 characters A-Z0-9, leading zeros kept, never a CDC station ID).</param>
/// <param name="Name">The trimmed name.</param>
/// <param name="Latitude">The latitude in decimal degrees.</param>
/// <param name="Longitude">The longitude in decimal degrees.</param>
/// <param name="AltitudeM">The altitude in meters, or null if not available (never 0).</param>
/// <param name="LineNumber">The 1-based physical line number.</param>
public sealed record MosmixLocationRecord(
    string MosmixId,
    string Name,
    decimal Latitude,
    decimal Longitude,
    decimal? AltitudeM,
    int LineNumber);
