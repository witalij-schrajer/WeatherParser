namespace WeatherParser.Parsing.Geography;

/// <summary>
/// Result of the search-area filter, not an export entity.
/// </summary>
public sealed record SearchAreaEvaluation(SearchAreaMatch Match, decimal? DistanceKm, decimal? AltitudeDifferenceM);
