namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Message types for MQTT messages (contract MQ-03).
/// </summary>
public enum MqttMessageType
{
    /// <summary>
    /// An observation message.
    /// </summary>
    Observation = 0,

    /// <summary>
    /// A forecast message.
    /// </summary>
    Forecast = 1,

    /// <summary>
    /// A CDC station list message.
    /// </summary>
    CdcStationList = 2,

    /// <summary>
    /// A MOSMIX location list message.
    /// </summary>
    MosmixLocationList = 3
}
