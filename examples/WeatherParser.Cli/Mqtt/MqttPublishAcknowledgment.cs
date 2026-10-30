namespace WeatherParser.Cli.Mqtt;

/// <summary>
/// The broker's acknowledgment of one QoS-1 publication; a failure reason code (MQTT 5.0) is final (CL-14).
/// </summary>
public sealed record MqttPublishAcknowledgment(
    bool IsSuccess,
    string ReasonCode);
