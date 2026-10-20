using System.Globalization;

namespace WeatherParser.Parsing.Time;

/// <summary>
/// A normalizer for source times.
/// </summary>
public sealed class SourceTimeNormalizer : ISourceTimeNormalizer
{
    private readonly ITimeZoneResolver _timeZoneResolver;
    private const string BerlinZoneId = "Europe/Berlin";
    private static readonly TimeSpan MidnightTolerance = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the <see cref="SourceTimeNormalizer"/> class.
    /// </summary>
    /// <param name="timeZoneResolver">The time zone resolver.</param>
    public SourceTimeNormalizer(ITimeZoneResolver timeZoneResolver)
    {
        _timeZoneResolver = timeZoneResolver ?? throw new ArgumentNullException(nameof(timeZoneResolver));
    }

    /// <inheritdoc />
    public NormalizedSourceTime NormalizeComplete(DateTimeOffset? sourceTime)
    {
        if (sourceTime is null)
            throw new SourceTimeNormalizationException("The source time is missing; the retrieval time is never used as a measurement time.");
        
        if (HasFractionalSeconds(sourceTime.Value.Ticks))
            throw new SourceTimeNormalizationException("The source time must not contain fractional seconds.");

        var value = sourceTime.Value;
        return new NormalizedSourceTime(value.UtcDateTime, TimestampQuality.Source, null);
    }

    /// <inheritdoc />
    public NormalizedSourceTime NormalizeCompleteLocal(DateTime? localTime)
    {
        if (localTime is null)
            throw new SourceTimeNormalizationException("The source time is missing; the retrieval time is never used as a measurement time.");
        
        if (localTime.Value.Kind != DateTimeKind.Unspecified)
            throw new SourceTimeNormalizationException("A complete local time must have DateTimeKind.Unspecified; use NormalizeComplete for times with an offset.");
        
        if (HasFractionalSeconds(localTime.Value.Ticks))
            throw new SourceTimeNormalizationException("The source time must not contain fractional seconds.");

        var zone = ResolveBerlin();
        var utc = ConvertBerlinLocalToUtc(localTime.Value, zone);
        return new NormalizedSourceTime(utc, TimestampQuality.Source, BerlinZoneId);
    }

    /// <inheritdoc />
    public NormalizedSourceTime InferDate(TimeOnly? displayedTime, DateTime retrievalUtc)
    {
        if (displayedTime is null)
            throw new SourceTimeNormalizationException("The displayed time is missing; the retrieval time is never used as a measurement time.");
        
        if (retrievalUtc.Kind != DateTimeKind.Utc)
            throw new SourceTimeNormalizationException("The retrieval instant must be UTC (DateTimeKind.Utc).");
        
        if (HasFractionalSeconds(displayedTime.Value.Ticks) || HasFractionalSeconds(retrievalUtc.Ticks))
            throw new SourceTimeNormalizationException("The source time must not contain fractional seconds.");

        var zone = ResolveBerlin();
        var display = displayedTime.Value;
        var localRetrieval = TimeZoneInfo.ConvertTimeFromUtc(retrievalUtc, zone);
        localRetrieval = DateTime.SpecifyKind(localRetrieval, DateTimeKind.Unspecified);
        var retrievalDate = DateOnly.FromDateTime(localRetrieval);
        var candidate = retrievalDate.ToDateTime(display, DateTimeKind.Unspecified);

        // C-31: a gap or overlap on the retrieval date is rejected before the midnight rule can move the reading.
        ConvertBerlinLocalToUtc(candidate, zone);

        // C-30: a display more than the tolerance after the local retrieval time belongs to the previous local date.
        if (candidate - localRetrieval > MidnightTolerance)
        {
            candidate = candidate.AddDays(-1);
        }

        // C-31: the chosen date is checked again, so a rollover cannot hide a gap or overlap.
        var utc = ConvertBerlinLocalToUtc(candidate, zone);
        return new NormalizedSourceTime(utc, TimestampQuality.EstimatedDate, BerlinZoneId);
    }

    private static bool HasFractionalSeconds(long ticks)
    {
        return ticks % TimeSpan.TicksPerSecond != 0;
    }

    private static DateTime ConvertBerlinLocalToUtc(DateTime localTime, TimeZoneInfo zone)
    {
        if (zone.IsInvalidTime(localTime))
        {
            throw new SourceTimeNormalizationException(
                $"The local time {localTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} does not exist in {BerlinZoneId} (daylight saving time gap).");
        }

        if (zone.IsAmbiguousTime(localTime))
        {
            throw new SourceTimeNormalizationException(
                $"The local time {localTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} is ambiguous in {BerlinZoneId} (daylight saving time overlap); an explicit UTC offset is required.");
        }

        return TimeZoneInfo.ConvertTimeToUtc(localTime, zone);
    }

    private TimeZoneInfo ResolveBerlin()
    {
        try
        {
            return _timeZoneResolver.Resolve(BerlinZoneId);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new SourceTimeNormalizationException(
                $"The time zone '{BerlinZoneId}' could not be resolved; install the operating system's time-zone data (ICU on Windows, the tzdata package on Linux).",
                exception);
        }
    }
}
