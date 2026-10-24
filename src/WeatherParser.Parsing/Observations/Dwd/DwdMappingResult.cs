using WeatherParser.Parsing.Observations;

namespace WeatherParser.Parsing.Observations.Dwd;

/// <summary>
/// The readings for the dataset builder and the omissions the mapper reports itself.
/// </summary>
/// <param name="Readings">The observation readings to pass to the dataset builder.</param>
/// <param name="Omissions">The omissions the mapper reports itself.</param>
public sealed record DwdMappingResult(IReadOnlyList<ObservationReading> Readings, IReadOnlyList<DwdMappingOmission> Omissions);
