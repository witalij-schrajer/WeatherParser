namespace WeatherParser.Cli.Mqtt;

/// <summary>
/// The bounded connection and publication times and attempts of CL-14.
/// </summary>
public sealed record MqttPublicationLimits(
    TimeSpan ConnectTimeout,
    TimeSpan PublishTimeout,
    int MaxPublishAttempts,
    TimeSpan RetryDelay)
{
    /// <summary>
    /// The CL-14 defaults.
    /// </summary>
    public static MqttPublicationLimits Default { get; } = new(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), 3, TimeSpan.FromSeconds(1));
}
