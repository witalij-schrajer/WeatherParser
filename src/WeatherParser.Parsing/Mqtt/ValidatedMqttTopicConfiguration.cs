namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Validated, immutable snapshot of MQTT topic configuration (contract MQ-09).
/// </summary>
public sealed class ValidatedMqttTopicConfiguration
{
    private readonly ValidatedMqttTopicMapping? _observation;
    private readonly ValidatedMqttTopicMapping? _forecast;
    private readonly ValidatedMqttTopicMapping? _cdcStationList;
    private readonly ValidatedMqttTopicMapping? _mosmixLocationList;
    private readonly int? _maxPayloadBytes;

    internal ValidatedMqttTopicConfiguration(
        ValidatedMqttTopicMapping? observation,
        ValidatedMqttTopicMapping? forecast,
        ValidatedMqttTopicMapping? cdcStationList,
        ValidatedMqttTopicMapping? mosmixLocationList,
        int? maxPayloadBytes)
    {
        _observation = observation;
        _forecast = forecast;
        _cdcStationList = cdcStationList;
        _mosmixLocationList = mosmixLocationList;
        _maxPayloadBytes = maxPayloadBytes;
    }

    /// <summary>
    /// Gets the maximum payload size in bytes.
    /// </summary>
    public int? MaxPayloadBytes => _maxPayloadBytes;

    /// <summary>
    /// Returns the mapping of the message type, or null when the consumer configured none,
    /// or when the message type is undefined.
    /// </summary>
    public ValidatedMqttTopicMapping? GetMapping(MqttMessageType messageType)
        => messageType switch
        {
            MqttMessageType.Observation => _observation,
            MqttMessageType.Forecast => _forecast,
            MqttMessageType.CdcStationList => _cdcStationList,
            MqttMessageType.MosmixLocationList => _mosmixLocationList,
            _ => null
        };
}
