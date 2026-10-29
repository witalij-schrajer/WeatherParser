using System;
using System.Collections.Generic;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;

namespace WeatherParser.Cli.Configuration;

public sealed class CliConfiguration
{
    public DwdSection? Dwd { get; init; }
    public OsthessenWetterSection? OsthessenWetter { get; init; }
    public MosmixSection? Mosmix { get; init; }
    public MqttSection? Mqtt { get; init; }
    public LimitsSection? Limits { get; init; }
    public HttpSection? Http { get; init; }
}

public sealed class DwdSection
{
    public string? ReferenceStationId { get; init; }
    public CdcCatalogEndpoints? Catalog { get; init; }
    public DwdObservationOptions? Observation { get; init; }
}

public sealed class OsthessenWetterSection
{
    public Uri? PageUri { get; init; }
    public TimeSpan? PageDeadline { get; init; }
    public List<OsthessenWetterStationMetadata>? Stations { get; init; }
    public bool PublicationPermitted { get; init; }
}

public sealed class MosmixSection
{
    public MosmixCatalogEndpoints? Catalog { get; init; }
    public MosmixLSection? L { get; init; }
    public MosmixSSection? S { get; init; }
}

public sealed class MosmixLSection
{
    public Uri? SingleStationsDirectory { get; init; }
    public TimeSpan? ParseDeadline { get; init; }
    public TimeSpan? MaxRunAge { get; init; }
}

public sealed class MosmixSSection
{
    public Uri? AllStationsUri { get; init; }
    public TimeSpan? ParseDeadline { get; init; }
    public TimeSpan? MaxRunAge { get; init; }
}

public sealed class MqttSection
{
    public BrokerSection? Broker { get; init; }
    public TopicsSection? Topics { get; init; }
    public int? MaxPayloadBytes { get; init; }
}

public sealed class BrokerSection
{
    public string? Host { get; init; }
    public int? Port { get; init; }
    public string? ProtocolVersion { get; init; }
    public string? ClientId { get; init; }
    public TimeSpan? KeepAlive { get; init; }
    public string? CredentialsFile { get; init; }
    public TlsSection? Tls { get; init; }
    public bool AllowUnencrypted { get; init; }
}

public sealed class TlsSection
{
    public string? CaFile { get; init; }
}

public sealed class TopicsSection
{
    public TopicSection? Observation { get; init; }
    public TopicSection? Forecast { get; init; }
    public TopicSection? CdcStationList { get; init; }
    public TopicSection? MosmixLocationList { get; init; }
}

public sealed class TopicSection
{
    public string? TopicTemplate { get; init; }
}

public sealed class LimitsSection
{
    public TimeSpan? ConnectTimeout { get; init; }
    public TimeSpan? PublishTimeout { get; init; }
    public int? MaxPublishAttempts { get; init; }
    public TimeSpan? RetryDelay { get; init; }
    public TimeSpan? CommandDeadline { get; init; }
    public TimeSpan? ShutdownGracePeriod { get; init; }
}

public sealed class HttpSection
{
    public string? UserAgent { get; init; }
}
