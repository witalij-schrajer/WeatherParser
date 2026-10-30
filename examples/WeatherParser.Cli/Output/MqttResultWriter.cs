namespace WeatherParser.Cli.Output;

using System.Text;
using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Arguments;
using WeatherParser.Parsing.Mqtt;

/// <summary>
/// Serializes one export root with the library's MQTT serializer and publishes it (CL-09, MQ-03).
/// </summary>
public sealed class MqttResultWriter : IResultWriter
{
    private readonly IMqttMessageSerializer _serializer;
    private readonly IMessagePublisher _publisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttResultWriter"/> class.
    /// </summary>
    /// <param name="serializer">The MQTT message serializer.</param>
    /// <param name="publisher">The message publisher.</param>
    public MqttResultWriter(IMqttMessageSerializer serializer, IMessagePublisher publisher)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    /// <inheritdoc/>
    public async Task WriteAsync<TRoot>(TRoot root, CommandContext context, CancellationToken cancellationToken) where TRoot : class
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(context);

        var message = await _serializer.SerializeAsync(root, cancellationToken).ConfigureAwait(false);
        await _publisher.PublishAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
