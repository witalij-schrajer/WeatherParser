namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// A notice found while reading the page; logged by the parser.
/// </summary>
/// <param name="NoticeId">One of the OsthessenWetterNoticeIds values.</param>
/// <param name="StationName">The station the notice refers to, or null for the whole page.</param>
public sealed record OsthessenWetterPageDiagnostic(string NoticeId, string? StationName);
