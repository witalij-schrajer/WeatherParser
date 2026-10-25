using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Reads the displayed time of a table row and infers its date through ISourceTimeNormalizer.InferDate (REQ-042, contract OW-07).
/// </summary>
public interface IOsthessenWetterTimeReader
{
    /// <summary>
    /// Reads text of the form HH:mm Uhr (00:00 to 23:59, two ASCII digits each) and infers the date from the retrieval instant truncated to whole seconds.
    /// </summary>
    /// <param name="timeText">The displayed time text.</param>
    /// <param name="retrievedAtUtc">The retrieval instant in UTC.</param>
    /// <exception cref="ArgumentException">When retrievedAtUtc is not of kind UTC.</exception>
    /// <exception cref="SourceTimeNormalizationException">When the Europe/Berlin time zone cannot be resolved.</exception>
    OsthessenWetterTimeValue Read(string? timeText, DateTime retrievedAtUtc);
}
