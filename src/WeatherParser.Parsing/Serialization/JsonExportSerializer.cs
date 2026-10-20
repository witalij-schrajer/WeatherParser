using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace WeatherParser.Parsing.Serialization;

/// <summary>
/// Serializes export roots to JSON format.
/// </summary>
public sealed class JsonExportSerializer : IJsonExportSerializer
{
    private readonly JsonSerializerOptions _options = new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

    /// <inheritdoc/>
    /// <exception cref="OperationCanceledException">The token was canceled before or during the copy to the destination.</exception>
    /// <exception cref="ExportSerializationException">The root could not be serialized as a JSON document; nothing was written to the destination.</exception>
    public async Task SerializeAsync<TRoot>(TRoot root, Stream destination, CancellationToken cancellationToken)
        where TRoot : class
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(destination);

        using var buffer = new MemoryStream();
        try
        {
            await JsonSerializer.SerializeAsync(buffer, root, _options, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ExportSerializationException("The export root could not be serialized as a JSON document.", exception);
        }

        buffer.Position = 0;
        await buffer.CopyToAsync(destination, cancellationToken);
    }
}
