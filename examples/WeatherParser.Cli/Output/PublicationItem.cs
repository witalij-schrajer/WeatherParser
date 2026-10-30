namespace WeatherParser.Cli.Output;

using WeatherParser.Parsing.Mqtt;

/// <summary>
/// One queued MQTT message with the source of the export root it was serialized from, for diagnostics (CL-08, CL-15).
/// </summary>
/// <param name="Message">The message, published unchanged.</param>
/// <param name="Source">The source of the export root, for example <c>dwd:00044</c>.</param>
public sealed record PublicationItem(MqttMessage Message, string Source);
