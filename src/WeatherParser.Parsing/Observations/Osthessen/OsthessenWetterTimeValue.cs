using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// The time read from one table row; Time is set only when Status is Resolved.
/// </summary>
/// <param name="Status">The outcome of reading the time.</param>
/// <param name="Time">The normalized time, or null if Status is not Resolved.</param>
public sealed record OsthessenWetterTimeValue(OsthessenWetterTimeStatus Status, NormalizedSourceTime? Time);
