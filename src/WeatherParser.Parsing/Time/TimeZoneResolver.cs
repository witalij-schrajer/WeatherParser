namespace WeatherParser.Parsing.Time;

/// <summary>
/// A time zone resolver that uses system time zones.
/// </summary>
public sealed class TimeZoneResolver : ITimeZoneResolver
{
    /// <summary>
    /// Resolves the specified time zone identifier to a <see cref="TimeZoneInfo"/>.
    /// </summary>
    /// <param name="zoneId">The time zone identifier.</param>
    /// <returns>The resolved <see cref="TimeZoneInfo"/>.</returns>
    public TimeZoneInfo Resolve(string zoneId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
        return TimeZoneInfo.FindSystemTimeZoneById(zoneId);
    }
}
