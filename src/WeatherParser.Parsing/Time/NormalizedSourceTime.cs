namespace WeatherParser.Parsing.Time;

/// <summary>
/// Represents a normalized source time with UTC instant, quality, and optional source time zone.
/// </summary>
public sealed record NormalizedSourceTime(DateTime UtcInstant, TimestampQuality Quality, string? SourceTimeZone);
