namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Validates the consumer's MQTT topic options completely, without broker access, and returns an immutable snapshot (contract MQ-05, MQ-08, MQ-09, MQ-11).
/// </summary>
public sealed class MqttTopicOptionsValidator : IMqttTopicOptionsValidator
{
    private readonly IMqttTopicTemplateParser _templateParser;

    /// <summary>
    /// Initializes a new instance of the <see cref="MqttTopicOptionsValidator"/> class.
    /// </summary>
    /// <param name="templateParser">The template parser to use.</param>
    public MqttTopicOptionsValidator(IMqttTopicTemplateParser templateParser)
    {
        ArgumentNullException.ThrowIfNull(templateParser);
        _templateParser = templateParser;
    }

    /// <inheritdoc/>
    public ValidatedMqttTopicConfiguration Validate(MqttTopicOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Mappings is null)
        {
            throw Fail(null, "The MQTT topic options have no mapping dictionary.");
        }

        if (options.MaxPayloadBytes is < 1 or > MqttProtocolLimits.MaxPayloadBytes)
        {
            throw Fail(null, $"The MQTT option MaxPayloadBytes must be between 1 and {MqttProtocolLimits.MaxPayloadBytes}.");
        }

        var mappings = options.Mappings.ToArray();
        ValidatedMqttTopicMapping? observation = null;
        ValidatedMqttTopicMapping? forecast = null;
        ValidatedMqttTopicMapping? cdcStationList = null;
        ValidatedMqttTopicMapping? mosmixLocationList = null;

        foreach (var entry in mappings)
        {
            if (!Enum.IsDefined(entry.Key))
            {
                throw Fail(null, "The MQTT topic options contain an unknown message type.");
            }

            if (entry.Value is null)
            {
                throw Fail(entry.Key, $"The MQTT mapping for message type {entry.Key} is missing.");
            }

            if (string.IsNullOrEmpty(entry.Value.TopicTemplate))
            {
                throw Fail(entry.Key, $"The MQTT mapping for message type {entry.Key} has no topic template.");
            }

            if (entry.Value.QualityOfService.HasValue && !Enum.IsDefined(entry.Value.QualityOfService.Value))
            {
                throw Fail(entry.Key, $"The MQTT mapping for message type {entry.Key} has an undefined quality of service.");
            }

            var template = _templateParser.Parse(entry.Value.TopicTemplate, entry.Key);
            CheckDistinguishingPlaceholders(template);
            var qos = entry.Value.QualityOfService ?? MqttQualityOfService.AtLeastOnce;
            var retain = entry.Value.Retain ?? DefaultRetain(entry.Key);
            var mapping = new ValidatedMqttTopicMapping(template, qos, retain);

            switch (entry.Key)
            {
                case MqttMessageType.Observation:
                    observation = mapping;
                    break;
                case MqttMessageType.Forecast:
                    forecast = mapping;
                    break;
                case MqttMessageType.CdcStationList:
                    cdcStationList = mapping;
                    break;
                default:
                    mosmixLocationList = mapping;
                    break;
            }
        }

        var validatedMappings = new[] { observation, forecast, cdcStationList, mosmixLocationList }.OfType<ValidatedMqttTopicMapping>().ToArray();

        for (int i = 0; i < validatedMappings.Length; i++)
        {
            for (int j = i + 1; j < validatedMappings.Length; j++)
            {
                var a = validatedMappings[i];
                var b = validatedMappings[j];
                var prefixA = LiteralPrefix(a.Template);
                var prefixB = LiteralPrefix(b.Template);
                if (prefixA.StartsWith(prefixB, StringComparison.Ordinal) || prefixB.StartsWith(prefixA, StringComparison.Ordinal))
                {
                    throw Fail(null, $"The MQTT topic templates of message types {a.Template.MessageType} and {b.Template.MessageType} can resolve to the same topic: their literal prefixes must differ.");
                }
            }
        }

        return new ValidatedMqttTopicConfiguration(observation, forecast, cdcStationList, mosmixLocationList, options.MaxPayloadBytes);
    }

    private static void CheckDistinguishingPlaceholders(MqttTopicTemplate template)
    {
        if (template.MessageType == MqttMessageType.Observation)
        {
            if (!IsWholeLevel(template, "source") && 
                !(IsWholeLevel(template, "provider") && IsWholeLevel(template, "station_id")))
            {
                throw Fail(MqttMessageType.Observation, "The MQTT topic template for message type Observation must contain {source}, or {provider} and {station_id}, each as a whole topic level.");
            }
        }
        else if (template.MessageType == MqttMessageType.Forecast)
        {
            if (!IsWholeLevel(template, "source") && 
                !(IsWholeLevel(template, "product") && IsWholeLevel(template, "mosmix_id")))
            {
                throw Fail(MqttMessageType.Forecast, "The MQTT topic template for message type Forecast must contain {source}, or {product} and {mosmix_id}, each as a whole topic level.");
            }
        }
    }

    private static bool IsWholeLevel(MqttTopicTemplate template, string placeholderName)
    {
        var segments = template.Segments;
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].IsPlaceholder && segments[i].Text == placeholderName)
            {
                bool beforeOk = i == 0 || (!segments[i - 1].IsPlaceholder && segments[i - 1].Text.EndsWith('/'));
                bool afterOk = i == segments.Count - 1 || (!segments[i + 1].IsPlaceholder && segments[i + 1].Text.StartsWith('/'));
                if (beforeOk && afterOk)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static string LiteralPrefix(MqttTopicTemplate template) => template.Segments[0].IsPlaceholder ? string.Empty : template.Segments[0].Text;

    private static bool DefaultRetain(MqttMessageType messageType) => messageType is MqttMessageType.Observation or MqttMessageType.Forecast;

    private static MqttMessageException Fail(MqttMessageType? messageType, string message) => new(MqttMessageErrorIds.OptionsInvalid, messageType, message);
}
