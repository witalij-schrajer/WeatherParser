namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Looks up the consumer-supplied metadata of an overview station (contract OW-04).
/// </summary>
public interface IOsthessenWetterStationMetadataLookup
{
    /// <summary>
    /// Returns a copy of the metadata whose name equals the given overview name after Unicode normalization form C (ordinal comparison), or null when there is none.
    /// </summary>
    /// <param name="name">The overview station name.</param>
    /// <exception cref="ArgumentNullException">When name is null.</exception>
    OsthessenWetterStationMetadata? Find(string name);
}
