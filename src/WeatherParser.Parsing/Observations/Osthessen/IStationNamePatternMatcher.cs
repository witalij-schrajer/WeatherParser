using System;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Validates station name patterns and matches them against station names with case-insensitive glob semantics (REQ-040, contract OW-09).
/// </summary>
public interface IStationNamePatternMatcher
{
    /// <summary>
    /// Validates a pattern before any request is made.
    /// </summary>
    /// <param name="pattern">The pattern to validate.</param>
    /// <exception cref="OsthessenWetterException">
    /// OSTHESSEN-PATTERN-INVALID when the pattern is null, empty, whitespace only or longer than 128 characters.
    /// </exception>
    StationNamePattern Validate(string? pattern);

    /// <summary>
    /// Returns true when the whole name matches the pattern: * matches any number of characters, ? exactly one Unicode scalar, every other character itself, case-insensitively.
    /// </summary>
    /// <param name="pattern">The pattern to match against.</param>
    /// <param name="name">The name to match.</param>
    /// <exception cref="ArgumentNullException">
    /// When pattern or name is null.
    /// </exception>
    bool IsMatch(StationNamePattern pattern, string name);
}
