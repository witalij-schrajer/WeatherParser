namespace WeatherParser.Parsing.Time;

/// <summary>
/// Normalizes source times into UTC instants with quality and optional source time zone.
/// </summary>
public interface ISourceTimeNormalizer
{
    /// <summary>
    /// Normalizes a complete source timestamp with an offset to a UTC instant.
    /// </summary>
    /// <param name="sourceTime">The source timestamp with offset.</param>
    /// <returns>The normalized time in UTC.</returns>
    NormalizedSourceTime NormalizeComplete(DateTimeOffset? sourceTime);

    /// <summary>
    /// Normalizes a local time into a UTC instant.
    /// </summary>
    /// <param name="localTime">The local time, assumed to be in Europe/Berlin.</param>
    /// <returns>The normalized time in UTC.</returns>
    NormalizedSourceTime NormalizeCompleteLocal(DateTime? localTime);

    /// <summary>
    /// Infers the date from a displayed time and the retrieval instant.
    /// </summary>
    /// <param name="displayedTime">The displayed time, e.g., from a weather report.</param>
    /// <param name="retrievalUtc">The UTC time when data was retrieved.</param>
    /// <returns>The inferred date in UTC.</returns>
    NormalizedSourceTime InferDate(TimeOnly? displayedTime, DateTime retrievalUtc);
}
