namespace WeatherParser.Parsing.Quantities;

/// <summary>
/// Immutable averaging or accumulation period in whole UTC seconds (contract C-44).
/// </summary>
public sealed class AveragingPeriod
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AveragingPeriod"/> class.
    /// </summary>
    /// <param name="startUtc">The start of the averaging period in UTC.</param>
    /// <param name="endUtc">The end of the averaging period in UTC.</param>
    public AveragingPeriod(DateTime startUtc, DateTime endUtc)
    {
        if (startUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The period start must be a UTC instant.", nameof(startUtc));

        if (endUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The period end must be a UTC instant.", nameof(endUtc));

        if (startUtc.Ticks % TimeSpan.TicksPerSecond != 0)
            throw new ArgumentException("The period start must be a whole second; sub-second parts are never truncated.", nameof(startUtc));

        if (endUtc.Ticks % TimeSpan.TicksPerSecond != 0)
            throw new ArgumentException("The period end must be a whole second; sub-second parts are never truncated.", nameof(endUtc));

        if (endUtc <= startUtc)
            throw new ArgumentException("The period end must be after its start.", nameof(endUtc));

        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    /// <summary>
    /// Gets the start of the averaging period in UTC.
    /// </summary>
    public DateTime StartUtc { get; }

    /// <summary>
    /// Gets the end of the averaging period in UTC.
    /// </summary>
    public DateTime EndUtc { get; }

    /// <summary>
    /// Gets the duration of the averaging period.
    /// </summary>
    public TimeSpan Duration => EndUtc - StartUtc;
}
