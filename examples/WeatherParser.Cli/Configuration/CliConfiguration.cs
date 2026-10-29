using System;
using System.Collections.Generic;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;

namespace WeatherParser.Cli.Configuration;

public sealed class CliConfiguration
{
    public DwdSection? Dwd { get; set; }
    public OsthessenWetterSection? OsthessenWetter { get; set; }
    public MosmixSection? Mosmix { get; set; }
    public MqttSection? Mqtt { get; set; }
    public LimitsSection? Limits { get; set; }
    public HttpSection? Http { get; set; }
}

public sealed class DwdSection
{
    public string? ReferenceStationId { get; set; }
    public CdcCatalogEndpoints? Catalog { get; set; }
    public DwdObservationOptions? Observation { get; set; }
}

public sealed class OsthessenWetterSection
{
    public Uri? PageUri { get; set; }
    public TimeSpan? PageDeadline { get; set; }
    public List<OsthessenWetterStationMetadata>? Stations { get; set; }
    public bool PublicationPermitted { get; set; }
}

public sealed class MosmixSection
{
    public MosmixCatalogEndpoints? Catalog { get; set; }
    public MosmixLSection? L { get; set; }
    public MosmixSSection? S { get; set; }
}

public sealed class MosmixLSection
{
    public Uri? SingleStationsDirectory { get; set; }
    public TimeSpan? ParseDeadline { get; set; }
    public TimeSpan? MaxRunAge { get; set; }
}

public sealed class MosmixSSection
{
    public Uri? AllStationsUri { get; set; }
    public TimeSpan? ParseDeadline { get; set; }
    public TimeSpan? MaxRunAge { get; set; }
}

public sealed class MqttSection
{
    public BrokerSection? Broker { get; set; }
    public TopicsSection? Topics { get; set; }
    public int? MaxPayloadBytes { get; set; }
}

public sealed class BrokerSection
{
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? ProtocolVersion { get; set; }
    public string? ClientId { get; set; }
    public TimeSpan? KeepAlive { get; set; }
    public string? CredentialsFile { get; set; }
    public TlsSection? Tls { get; set; }
    public bool AllowUnencrypted { get; set; }
}

public sealed class TlsSection
{
    public string? CaFile { get; set; }
}

public sealed class TopicsSection
{
    public TopicSection? Observation { get; set; }
    public TopicSection? Forecast { get; set; }
    public TopicSection? CdcStationList { get; set; }
    public TopicSection? MosmixLocationList { get; set; }
}

public sealed class TopicSection
{
    public string? TopicTemplate { get; set; }
}

public sealed class LimitsSection
{
    public TimeSpan? ConnectTimeout { get; set; }
    public TimeSpan? PublishTimeout { get; set; }
    public int? MaxPublishAttempts { get; set; }
    public TimeSpan? RetryDelay { get; set; }
    public TimeSpan? CommandDeadline { get; set; }
    public TimeSpan? ShutdownGracePeriod { get; set; }
}

public sealed class HttpSection
{
    public string? UserAgent { get; set; }
}
