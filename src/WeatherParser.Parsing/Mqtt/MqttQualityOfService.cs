namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Quality of service levels for MQTT messages (contract MQ-02).
/// </summary>
public enum MqttQualityOfService
{
    /// <summary>
    /// At most once delivery.
    /// </summary>
    AtMostOnce = 0,

    /// <summary>
    /// At least once delivery.
    /// </summary>
    AtLeastOnce = 1,

    /// <summary>
    /// Exactly once delivery.
    /// </summary>
    ExactlyOnce = 2
}
