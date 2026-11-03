namespace WeatherParser.Parsing.Serialization;

/// <summary>
/// Provides methods for serializing export roots to JSON.
/// </summary>
public interface IJsonExportSerializer
{
    /// <summary>
    /// Serializes one export root as one complete JSON document into the caller-owned destination stream.
    /// </summary>
    /// <param name="root">The root object to serialize.</param>
    /// <param name="destination">The stream to write the serialized data to.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    Task SerializeAsync<TRoot>(TRoot root, Stream destination, CancellationToken cancellationToken) where TRoot : class;
}
