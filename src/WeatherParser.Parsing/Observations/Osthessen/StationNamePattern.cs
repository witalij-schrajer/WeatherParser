namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// A station name pattern validated by IStationNamePatternMatcher, in Unicode normalization form C (contract OW-09).
/// </summary>
public sealed class StationNamePattern
{
    /// <summary>
    /// Gets the validated pattern in Unicode normalization form C.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StationNamePattern"/> class.
    /// </summary>
    /// <param name="value">The validated pattern.</param>
    internal StationNamePattern(string value)
    {
        Value = value;
    }
}
