using System;
using System.Collections.Generic;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;

namespace WeatherParser.Cli.Configuration;

/// <summary>
/// Represents the configuration for the WeatherParser CLI application.
/// </summary>
/// <remarks>
/// Configuration follows CL-05 for sections and CL-06 for credentials.
/// </remarks>
public sealed class CliConfiguration
{
    /// <summary>
    /// Gets or sets the DWD section configuration.
    /// </summary>
    public DwdSection? Dwd { get; set; }
    
    /// <summary>
    /// Gets or sets the Osthessen Wetter section configuration.
    /// </summary>
    public OsthessenWetterSection? OsthessenWetter { get; set; }
    
    /// <summary>
    /// Gets or sets the MOSMIX section configuration.
    /// </summary>
    public MosmixSection? Mosmix { get; set; }
    
    /// <summary>
    /// Gets or sets the MQTT section configuration.
    /// </summary>
    public MqttSection? Mqtt { get; set; }
    
    /// <summary>
    /// Gets or sets the limits section configuration.
    /// </summary>
    public LimitsSection? Limits { get; set; }
    
    /// <summary>
    /// Gets or sets the HTTP section configuration.
    /// </summary>
    public HttpSection? Http { get; set; }
}

/// <summary>
/// Represents the DWD section of the CLI configuration.
/// </summary>
public sealed class DwdSection
{
    /// <summary>
    /// Gets or sets the reference station ID for DWD observations.
    /// </summary>
    public string? ReferenceStationId { get; set; }
    
    /// <summary>
    /// Gets or sets the catalog endpoints for DWD data.
    /// </summary>
    public CdcCatalogEndpoints? Catalog { get; set; }
    
    /// <summary>
    /// Gets or sets the observation options for DWD data.
    /// </summary>
    public DwdObservationOptions? Observation { get; set; }
}

/// <summary>
/// Represents the Osthessen Wetter section of the CLI configuration.
/// </summary>
public sealed class OsthessenWetterSection
{
    /// <summary>
    /// Gets or sets the URI of the Osthessen Wetter page.
    /// </summary>
    public Uri? PageUri { get; set; }
    
    /// <summary>
    /// Gets or sets the deadline for fetching data from the Osthessen Wetter page.
    /// </summary>
    public TimeSpan? PageDeadline { get; set; }
    
    /// <summary>
    /// Gets or sets the list of station metadata for Osthessen Wetter.
    /// </summary>
    public List<OsthessenWetterStationMetadata>? Stations { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether publication is permitted.
    /// </summary>
    public bool PublicationPermitted { get; set; }
}

/// <summary>
/// Represents the MOSMIX section of the CLI configuration.
/// </summary>
public sealed class MosmixSection
{
    /// <summary>
    /// Gets or sets the catalog endpoints for MOSMIX data.
    /// </summary>
    public MosmixCatalogEndpoints? Catalog { get; set; }
    
    /// <summary>
    /// Gets or sets the L section configuration for MOSMIX.
    /// </summary>
    public MosmixLSection? L { get; set; }
    
    /// <summary>
    /// Gets or sets the S section configuration for MOSMIX.
    /// </summary>
    public MosmixSSection? S { get; set; }
}

/// <summary>
/// Represents the L section of the MOSMIX configuration.
/// </summary>
public sealed class MosmixLSection
{
    /// <summary>
    /// Gets or sets the URI of the directory containing single stations for MOSMIX L data.
    /// </summary>
    public Uri? SingleStationsDirectory { get; set; }
    
    /// <summary>
    /// Gets or sets the deadline for parsing MOSMIX L data.
    /// </summary>
    public TimeSpan? ParseDeadline { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum age of a run for MOSMIX L data.
    /// </summary>
    public TimeSpan? MaxRunAge { get; set; }
}

/// <summary>
/// Represents the S section of the MOSMIX configuration.
/// </summary>
public sealed class MosmixSSection
{
    /// <summary>
    /// Gets or sets the URI for all stations in MOSMIX S data.
    /// </summary>
    public Uri? AllStationsUri { get; set; }
    
    /// <summary>
    /// Gets or sets the deadline for parsing MOSMIX S data.
    /// </summary>
    public TimeSpan? ParseDeadline { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum age of a run for MOSMIX S data.
    /// </summary>
    public TimeSpan? MaxRunAge { get; set; }
}

/// <summary>
/// Represents the MQTT section of the CLI configuration.
/// </summary>
public sealed class MqttSection
{
    /// <summary>
    /// Gets or sets the broker section configuration.
    /// </summary>
    public BrokerSection? Broker { get; set; }
    
    /// <summary>
    /// Gets or sets the topics section configuration.
    /// </summary>
    public TopicsSection? Topics { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum payload size in bytes for MQTT messages.
    /// </summary>
    public int? MaxPayloadBytes { get; set; }
}

/// <summary>
/// Represents the broker section of the MQTT configuration.
/// </summary>
public sealed class BrokerSection
{
    /// <summary>
    /// Gets or sets the host name of the MQTT broker.
    /// </summary>
    public string? Host { get; set; }
    
    /// <summary>
    /// Gets or sets the port number of the MQTT broker.
    /// </summary>
    public int? Port { get; set; }
    
    /// <summary>
    /// Gets or sets the protocol version for the MQTT connection.
    /// </summary>
    public string? ProtocolVersion { get; set; }
    
    /// <summary>
    /// Gets or sets the client ID for the MQTT connection.
    /// </summary>
    public string? ClientId { get; set; }
    
    /// <summary>
    /// Gets or sets the keep-alive interval for the MQTT connection.
    /// </summary>
    public TimeSpan? KeepAlive { get; set; }
    
    /// <summary>
    /// Gets or sets the path to the credentials file for the MQTT connection.
    /// </summary>
    public string? CredentialsFile { get; set; }
    
    /// <summary>
    /// Gets or sets the TLS section configuration.
    /// </summary>
    public TlsSection? Tls { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether unencrypted connections are allowed.
    /// </summary>
    public bool AllowUnencrypted { get; set; }
}

/// <summary>
/// Represents the TLS section of the MQTT configuration.
/// </summary>
public sealed class TlsSection
{
    /// <summary>
    /// Gets or sets the path to the CA file for TLS verification.
    /// </summary>
    public string? CaFile { get; set; }
}

/// <summary>
/// Represents the topics section of the MQTT configuration.
/// </summary>
public sealed class TopicsSection
{
    /// <summary>
    /// Gets or sets the topic configuration for observations.
    /// </summary>
    public TopicSection? Observation { get; set; }
    
    /// <summary>
    /// Gets or sets the topic configuration for forecasts.
    /// </summary>
    public TopicSection? Forecast { get; set; }
    
    /// <summary>
    /// Gets or sets the topic configuration for CDC station lists.
    /// </summary>
    public TopicSection? CdcStationList { get; set; }
    
    /// <summary>
    /// Gets or sets the topic configuration for MOSMIX location lists.
    /// </summary>
    public TopicSection? MosmixLocationList { get; set; }
}

/// <summary>
/// Represents a topic section in the MQTT configuration.
/// </summary>
public sealed class TopicSection
{
    /// <summary>
    /// Gets or sets the topic template used for publishing messages.
    /// </summary>
    public string? TopicTemplate { get; set; }
}

/// <summary>
/// Represents the limits section of the CLI configuration.
/// </summary>
public sealed class LimitsSection
{
    /// <summary>
    /// Gets or sets the connection timeout for network operations.
    /// </summary>
    public TimeSpan? ConnectTimeout { get; set; }
    
    /// <summary>
    /// Gets or sets the publish timeout for MQTT messages.
    /// </summary>
    public TimeSpan? PublishTimeout { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of publish attempts before giving up.
    /// </summary>
    public int? MaxPublishAttempts { get; set; }
    
    /// <summary>
    /// Gets or sets the delay between retry attempts for publishing messages.
    /// </summary>
    public TimeSpan? RetryDelay { get; set; }
    
    /// <summary>
    /// Gets or sets the deadline for command execution.
    /// </summary>
    public TimeSpan? CommandDeadline { get; set; }
    
    /// <summary>
    /// Gets or sets the grace period during application shutdown.
    /// </summary>
    public TimeSpan? ShutdownGracePeriod { get; set; }
}

/// <summary>
/// Represents the HTTP section of the CLI configuration.
/// </summary>
public sealed class HttpSection
{
    /// <summary>
    /// Gets or sets the user agent string for HTTP requests.
    /// </summary>
    public string? UserAgent { get; set; }
}
