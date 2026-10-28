namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Validated, immutable snapshot of an MQTT topic mapping (contract MQ-09).
/// </summary>
public sealed class ValidatedMqttTopicMapping
{
    /// <summary>
    /// Gets the template.
    /// </summary>
    public MqttTopicTemplate Template { get; }

    /// <summary>
    /// Gets the quality of service.
    /// </summary>
    public MqttQualityOfService QualityOfService { get; }

    /// <summary>
    /// Gets the retain flag.
    /// </summary>
    public bool Retain { get; }

    internal ValidatedMqttTopicMapping(MqttTopicTemplate template, MqttQualityOfService qualityOfService, bool retain)
    {
        Template = template;
        QualityOfService = qualityOfService;
        Retain = retain;
    }
}
