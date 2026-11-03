using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Serialization;

namespace MyWeatherApp;

/// <summary>
/// Parses the DWD observations of one station and writes them as JSON. The parser and the serializer come from the container through constructor injection.
/// </summary>
public sealed class DwdJsonExporter : IDwdJsonExporter
{
    private readonly IDwdObservationParser _parser;
    private readonly IJsonExportSerializer _serializer;

    public DwdJsonExporter(IDwdObservationParser parser, IJsonExportSerializer serializer)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <inheritdoc />
    public async Task ExportAsync(string stationId, Stream destination, CancellationToken cancellationToken)
    {
        var dataset = await _parser.ParseAsync(stationId, cancellationToken).ConfigureAwait(false);
        await _serializer.SerializeAsync(dataset, destination, cancellationToken).ConfigureAwait(false);
    }
}
