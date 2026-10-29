using System.Text;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Serializes supported export roots into MQTT messages: topic from the consumer's template, payload from the JSON export serializer, QoS and retain from the mapping; no broker access, no logging (contract MQ-02 to MQ-13).
/// </summary>
public sealed class MqttMessageSerializer : IMqttMessageSerializer
{
    private readonly ValidatedMqttTopicConfiguration _configuration;
    private readonly IMqttTopicValidator _topicValidator;
    private readonly IJsonExportSerializer _jsonSerializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttMessageSerializer"/> class.
    /// </summary>
    /// <param name="options">The MQTT topic options.</param>
    /// <param name="optionsValidator">The MQTT topic options validator.</param>
    /// <param name="topicValidator">The MQTT topic validator.</param>
    /// <param name="jsonSerializer">The JSON export serializer.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any argument is null.
    /// </exception>
    public MqttMessageSerializer(
        MqttTopicOptions options,
        IMqttTopicOptionsValidator optionsValidator,
        IMqttTopicValidator topicValidator,
        IJsonExportSerializer jsonSerializer)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(optionsValidator);
        ArgumentNullException.ThrowIfNull(topicValidator);
        ArgumentNullException.ThrowIfNull(jsonSerializer);

        _configuration = optionsValidator.Validate(options);
        _topicValidator = topicValidator;
        _jsonSerializer = jsonSerializer;
    }

    /// <inheritdoc/>
    public async Task<MqttMessage> SerializeAsync<TRoot>(TRoot root, CancellationToken cancellationToken) where TRoot : class
    {
        ArgumentNullException.ThrowIfNull(root);
        cancellationToken.ThrowIfCancellationRequested();

        var messageType = MessageTypeOf(typeof(TRoot)) ?? throw new MqttMessageException(MqttMessageErrorIds.MessageTypeUnsupported, null, $"The type '{typeof(TRoot).Name}' is not an MQTT message type; supported roots are ObservationDataset, ForecastRun, CdcStationList and MosmixLocationList.");

        var mapping = _configuration.GetMapping(messageType) ?? throw new MqttMessageException(MqttMessageErrorIds.MappingMissing, messageType, $"No MQTT topic mapping is configured for message type {messageType}.");

        using var buffer = new MqttPayloadBuffer(PayloadCap(_configuration.MaxPayloadBytes, mapping.Template));
        try
        {
            await _jsonSerializer.SerializeAsync(root, buffer, cancellationToken);
        }
        catch (ExportSerializationException exception)
        {
            throw new MqttMessageException(MqttMessageErrorIds.SerializationFailed, messageType, $"Message type {messageType}: the export root could not be serialized as JSON.", exception);
        }

        var topicBuilder = new StringBuilder();
        foreach (var segment in mapping.Template.Segments)
        {
            if (!segment.IsPlaceholder)
            {
                topicBuilder.Append(segment.Text);
            }
            else
            {
                string value = string.Empty;
                Check(() => { value = ReadValue(root, segment.Text, messageType); _topicValidator.ValidatePlaceholderValue(segment.Text, value); }, messageType);
                topicBuilder.Append(value);
            }
        }

        var topic = topicBuilder.ToString();
        Check(() => _topicValidator.ValidateTopic(topic), messageType);

        if (buffer.Overflowed || buffer.Length > EffectiveBound(_configuration.MaxPayloadBytes, topic))
        {
            throw new MqttMessageException(MqttMessageErrorIds.PayloadTooLarge, messageType, $"Message type {messageType}: the payload exceeds the MQTT payload bound for its topic.");
        }

        return new MqttMessage(topic, buffer.ToArray(), mapping.QualityOfService, mapping.Retain, messageType);
    }

    private static MqttMessageType? MessageTypeOf(Type rootType)
    {
        if (rootType == typeof(ObservationDataset))
        {
            return MqttMessageType.Observation;
        }

        if (rootType == typeof(ForecastRun))
        {
            return MqttMessageType.Forecast;
        }

        if (rootType == typeof(CdcStationList))
        {
            return MqttMessageType.CdcStationList;
        }

        if (rootType == typeof(MosmixLocationList))
        {
            return MqttMessageType.MosmixLocationList;
        }

        return null;
    }

    private static void Check(Action check, MqttMessageType messageType)
    {
        try
        {
            check();
        }
        catch (MqttMessageException exception)
        {
            if (exception.ErrorId != MqttMessageErrorIds.SerializationFailed)
            {
                throw new MqttMessageException(exception.ErrorId, messageType, $"Message type {messageType}: {exception.Message}", exception);
            }
            throw;
        }
    }

    private static string ReadValue(object root, string name, MqttMessageType messageType)
    {
        try
        {
            return PlaceholderValue(root, name);
        }
        catch (InvalidOperationException exception)
        {
            throw new MqttMessageException(MqttMessageErrorIds.SerializationFailed, messageType, $"Message type {messageType}: a required member for the placeholder '{name}' is not set.", exception);
        }
    }

    private static string PlaceholderValue(object root, string name)
    {
        return root switch
        {
            ObservationDataset dataset => name switch
            {
                "source" => dataset.Source,
                "provider" => Provider(dataset.Source),
                _ => dataset.Location.StationId
            },
            ForecastRun run => name switch
            {
                "source" => run.Source,
                "provider" => Provider(run.Source),
                "mosmix_id" => run.MosmixId,
                _ => run.Product == MosmixProduct.MosmixS ? "MOSMIX_S" : "MOSMIX_L"
            },
            CdcStationList list => Provider(list.Source),
            _ => Provider(((MosmixLocationList)root).Source)
        };
    }

    private static string Provider(string source)
    {
        var separator = source.IndexOf(':');
        if (separator <= 0)
        {
            throw new MqttMessageException(MqttMessageErrorIds.PlaceholderValueInvalid, null, "The source has no provider prefix before ':' for the placeholder 'provider'.");
        }

        return source[..separator];
    }

    /// <summary>
    /// The cap of the payload buffer: the largest effective bound any resolved topic of the template can have, the smaller of the configured bound and 268 435 450 minus the template's minimum topic length (literal UTF-8 bytes plus one byte per placeholder).
    /// </summary>
    internal static int PayloadCap(int? maxPayloadBytes, MqttTopicTemplate template)
    {
        var minimumTopicBytes = template.Segments.Sum(segment => segment.IsPlaceholder ? 1 : Encoding.UTF8.GetByteCount(segment.Text));
        return Math.Min(maxPayloadBytes ?? MqttProtocolLimits.MaxPayloadBytes, MqttProtocolLimits.MaxPayloadBytes - minimumTopicBytes);
    }

    /// <summary>
    /// The effective payload bound of a message: the smaller of the configured bound and 268 435 450 minus the UTF-8 bytes of its topic (contract section 4).
    /// </summary>
    internal static long EffectiveBound(int? maxPayloadBytes, string topic) => Math.Min(maxPayloadBytes ?? long.MaxValue, (long)MqttProtocolLimits.MaxPayloadBytes - Encoding.UTF8.GetByteCount(topic));
}
