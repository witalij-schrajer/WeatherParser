using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Observations.Dwd;

/// <summary>
/// A station resolved against the CDC station lists: the validated five-digit ID, its location and the product codes of the lists that contain it, in the order tu, ff, rr, sd.
/// </summary>
/// <param name="StationId">The validated five-digit station ID.</param>
/// <param name="Location">The location of the station.</param>
/// <param name="ProductCodes">The product codes of the lists that contain it, in the order tu, ff, rr, sd.</param>
public sealed record DwdResolvedStation(string StationId, ObservationLocation Location, IReadOnlyList<string> ProductCodes);
