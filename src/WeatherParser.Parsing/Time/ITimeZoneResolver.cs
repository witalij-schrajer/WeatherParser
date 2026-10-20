namespace WeatherParser.Parsing.Time;

/// <summary>
/// Resolves time zone identifiers to <see cref="TimeZoneInfo"/> objects.
/// </summary>
public interface ITimeZoneResolver
{
    /// <summary>
    /// Resolves the specified time zone identifier to a <see cref="TimeZoneInfo"/>.
    /// </summary>
    /// <param name="zoneId">The time zone identifier.</param>
    /// <returns>The resolved <see cref="TimeZoneInfo"/>.</returns>
    TimeZoneInfo Resolve(string zoneId);
}
