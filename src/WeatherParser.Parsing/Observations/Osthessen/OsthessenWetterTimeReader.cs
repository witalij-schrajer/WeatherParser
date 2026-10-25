using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Default implementation of <see cref="IOsthessenWetterTimeReader"/>; stateless apart from the injected normalizer.
/// </summary>
public sealed class OsthessenWetterTimeReader : IOsthessenWetterTimeReader
{
    private readonly ISourceTimeNormalizer _normalizer;

    /// <summary>
    /// Initializes a new instance with the source time normalizer.
    /// </summary>
    /// <param name="normalizer">The source time normalizer.</param>
    public OsthessenWetterTimeReader(ISourceTimeNormalizer normalizer)
    {
        _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
    }

    /// <inheritdoc />
    public OsthessenWetterTimeValue Read(string? timeText, DateTime retrievedAtUtc)
    {
        if (retrievedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The retrieval instant must be of kind UTC.", nameof(retrievedAtUtc));

        var time = ParseTime(timeText);
        if (time is null)
            return new OsthessenWetterTimeValue(OsthessenWetterTimeStatus.Missing, null);

        // Truncate the retrieval instant to whole seconds
        var retrieval = new DateTime(retrievedAtUtc.Ticks - (retrievedAtUtc.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);

        try
        {
            var normalized = _normalizer.InferDate(time, retrieval);
            return new OsthessenWetterTimeValue(OsthessenWetterTimeStatus.Resolved, normalized);
        }
        catch (SourceTimeNormalizationException exception) when (exception.InnerException is not TimeZoneNotFoundException and not InvalidTimeZoneException)
        {
            return new OsthessenWetterTimeValue(OsthessenWetterTimeStatus.Unresolved, null);
        }
    }

    private static TimeOnly? ParseTime(string? text)
    {
        if (text is null || text.Length != 9)
            return null;

        // Check for HH:mm Uhr format
        if (!char.IsAsciiDigit(text[0]) || !char.IsAsciiDigit(text[1]) ||
            text[2] != ':' ||
            !char.IsAsciiDigit(text[3]) || !char.IsAsciiDigit(text[4]) ||
            !text.EndsWith(" Uhr", StringComparison.Ordinal))
        {
            return null;
        }

        var hours = (text[0] - '0') * 10 + (text[1] - '0');
        var minutes = (text[3] - '0') * 10 + (text[4] - '0');

        if (hours > 23 || minutes > 59)
            return null;

        return new TimeOnly(hours, minutes);
    }
}
