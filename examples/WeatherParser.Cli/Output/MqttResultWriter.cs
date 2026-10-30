namespace WeatherParser.Cli.Output;

using System.Text;
using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Arguments;
using WeatherParser.Parsing.Mqtt;

/// <summary>
/// Serializes one export root with the library's MQTT serializer and queues the message for publication after the command (CL-09, CL-13, MQ-03).
/// </summary>
public sealed class MqttResultWriter : IResultWriter
{
    private readonly IMqttMessageSerializer _serializer;
    private readonly IPublicationQueue _queue;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttResultWriter"/> class.
    /// </summary>
    /// <param name="serializer">The MQTT message serializer.</param>
    /// <param name="queue">The publication queue.</param>
    public MqttResultWriter(IMqttMessageSerializer serializer, IPublicationQueue queue)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
    }

    /// <inheritdoc/>
    public async Task WriteAsync<TRoot>(TRoot root, CommandContext context, CancellationToken cancellationToken) where TRoot : class
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(context);

        var message = await _serializer.SerializeAsync(root, cancellationToken).ConfigureAwait(false);
        _queue.Add(message);
    }
}
