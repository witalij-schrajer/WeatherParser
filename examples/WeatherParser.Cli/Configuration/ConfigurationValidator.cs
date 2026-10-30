using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Cli.Arguments;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Selection;

namespace WeatherParser.Cli.Configuration;

/// <summary>
/// Validates the CLI configuration and command request against expected values and service resolution.
/// </summary>
public sealed class ConfigurationValidator : IConfigurationValidator
{
    private const string ConfigInvalid = "CLI-CONFIG-INVALID";
    private static readonly Regex UserAgentPattern = new(@"\A[\x20-\x7E]{1,200}\z", RegexOptions.CultureInvariant);
    private static readonly Regex ClientIdPattern = new(@"\A[0-9A-Za-z]{1,23}\z", RegexOptions.CultureInvariant);

    private readonly IReferenceStationResolver _stationResolver;
    private readonly ICredentialProvider _credentialProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationValidator"/> class.
    /// </summary>
    /// <param name="stationResolver">The reference station resolver to use.</param>
    /// <param name="credentialProvider">The credential provider to use.</param>
    public ConfigurationValidator(IReferenceStationResolver stationResolver, ICredentialProvider credentialProvider)
    {
        ArgumentNullException.ThrowIfNull(stationResolver);
        ArgumentNullException.ThrowIfNull(credentialProvider);
        _stationResolver = stationResolver;
        _credentialProvider = credentialProvider;
    }

    /// <inheritdoc />
    /// <summary>
    /// Validates the specified service provider, configuration and command request.
    /// </summary>
    /// <param name="services">The service provider to validate.</param>
    /// <param name="configuration">The CLI configuration to validate.</param>
    /// <param name="request">The command request to validate.</param>
    /// <returns>The resolved station and credentials.</returns>
    public ValidatedSettings Validate(IServiceProvider services, CliConfiguration configuration, CommandRequest request)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(request);

        var limits = configuration.Limits;
        CheckRange("Limits.ConnectTimeout", limits?.ConnectTimeout, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2));
        CheckRange("Limits.PublishTimeout", limits?.PublishTimeout, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(2));
        CheckRange("Limits.RetryDelay", limits?.RetryDelay, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        CheckRange("Limits.CommandDeadline", limits?.CommandDeadline, TimeSpan.FromMinutes(1), TimeSpan.FromHours(1));
        CheckRange("Limits.ShutdownGracePeriod", limits?.ShutdownGracePeriod, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));
        CheckRange("Limits.MaxPublishAttempts", limits?.MaxPublishAttempts, 1, 5);

        if (configuration.Http?.UserAgent is { } userAgent && !UserAgentPattern.IsMatch(userAgent))
        {
            throw Invalid("The setting 'Http.UserAgent' must have 1 to 200 printable ASCII characters.");
        }

        var mqtt = request.Common.Output == OutputFormat.Mqtt;
        BrokerCredentials? credentials = null;
        if (mqtt)
        {
            ValidateBroker(configuration.Mqtt?.Broker);
            credentials = _credentialProvider.GetCredentials(configuration.Mqtt!.Broker);
        }

        string? stationId = null;
        if (request is ParseDwdRequest parseDwd)
        {
            stationId = _stationResolver.Resolve(parseDwd.StationId, configuration);
        }

        try
        {
            ResolveCommandServices(services, request);
            if (mqtt)
            {
                CheckTopic(services, request);
            }
        }
        catch (Exception exception) when (LibraryErrorId(exception) is { } errorId)
        {
            throw Invalid($"{errorId}: {exception.Message}");
        }

        return new ValidatedSettings(stationId, credentials);
    }

    /// <summary>
    /// Validates the specified broker section.
    /// </summary>
    /// <param name="broker">The broker section to validate.</param>
    private static void ValidateBroker(BrokerSection? broker)
    {
        if (string.IsNullOrEmpty(broker?.Host))
        {
            throw Invalid("The MQTT output needs the setting 'Mqtt.Broker.Host'.");
        }

        CheckRange("Mqtt.Broker.Port", broker.Port, 1, 65535);
        CheckRange("Mqtt.Broker.KeepAlive", broker.KeepAlive, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

        if (broker.ProtocolVersion is { } version && version != "5.0" && version != "3.1.1")
        {
            throw Invalid($"The setting 'Mqtt.Broker.ProtocolVersion' is '{version}'; allowed are 5.0 and 3.1.1.");
        }

        if (broker.ClientId is { } clientId && !ClientIdPattern.IsMatch(clientId))
        {
            throw Invalid("The setting 'Mqtt.Broker.ClientId' must have 1 to 23 letters and digits.");
        }
    }

    /// <summary>
    /// Resolves the command services for the specified service provider and command request.
    /// </summary>
    /// <param name="services">The service provider to resolve services from.</param>
    /// <param name="request">The command request to resolve services for.</param>
    private static void ResolveCommandServices(IServiceProvider services, CommandRequest request)
    {
        // Creating the services runs the library's own option and endpoint validation (CL-05).
        _ = request switch
        {
            SelectDwdRequest => services.GetRequiredService<ICdcStationSelector>(),
            SelectMosmixRequest select => ResolveMosmixSelection(services, select.RefreshMosmixSEvidence),
            ParseDwdRequest => services.GetRequiredService<IDwdObservationParser>(),
            ParseOstHessenRequest => services.GetRequiredService<IOsthessenWetterParser>(),
            ParseMosmixRequest { Product: MosmixProduct.MosmixS } => services.GetRequiredService<IMosmixSParser>(),
            _ => (object)services.GetRequiredService<IMosmixLParser>(),
        };
    }

    /// <summary>
    /// Resolves the MOSMIX selection for the specified service provider and refresh flag.
    /// </summary>
    /// <param name="services">The service provider to resolve services from.</param>
    /// <param name="refresh">A value indicating whether to refresh the evidence.</param>
    /// <returns>The resolved MOSMIX selection object.</returns>
    private static object ResolveMosmixSelection(IServiceProvider services, bool refresh)
    {
        var selector = services.GetRequiredService<IMosmixLocationSelector>();
        return refresh ? services.GetRequiredService<IMosmixSEvidenceRefresher>() : selector;
    }

    /// <summary>
    /// Checks the MQTT topic for the specified service provider and command request.
    /// </summary>
    /// <param name="services">The service provider to validate.</param>
    /// <param name="request">The command request to validate.</param>
    private static void CheckTopic(IServiceProvider services, CommandRequest request)
    {
        var messageType = request switch
        {
            SelectDwdRequest => MqttMessageType.CdcStationList,
            SelectMosmixRequest => MqttMessageType.MosmixLocationList,
            ParseMosmixRequest => MqttMessageType.Forecast,
            _ => MqttMessageType.Observation,
        };

        var topics = services.GetRequiredService<IMqttTopicOptionsValidator>().Validate(services.GetRequiredService<MqttTopicOptions>());
        if (topics.GetMapping(messageType) is null)
        {
            throw Invalid($"MQTT-MAPPING-MISSING: No MQTT topic is configured for message type {messageType} (setting 'Mqtt.Topics').");
        }
    }

    /// <summary>
    /// Checks the range of a value against specified minimum and maximum values.
    /// </summary>
    /// <typeparam name="T">The type of the value to check.</typeparam>
    /// <param name="name">The name of the setting being checked.</param>
    /// <param name="value">The value to check.</param>
    /// <param name="min">The minimum allowed value.</param>
    /// <param name="max">The maximum allowed value.</param>
    private static void CheckRange<T>(string name, T? value, T min, T max)
        where T : struct, IComparable<T>
    {
        if (value is { } actual && (actual.CompareTo(min) < 0 || actual.CompareTo(max) > 0))
        {
            throw Invalid($"The setting '{name}' is {actual}; allowed are {min} to {max}.");
        }
    }

    /// <summary>
    /// Gets the library error ID from the specified exception.
    /// </summary>
    /// <param name="exception">The exception to get the error ID from.</param>
    /// <returns>The library error ID, or null if not found.</returns>
    private static string? LibraryErrorId(Exception exception) => exception switch
    {
        ParserEndpointValidationException endpoint => endpoint.ErrorId,
        SelectionLimitException selection => selection.ErrorId,
        EvidenceCacheException cache => cache.ErrorId,
        ObservationLimitException observation => observation.ErrorId,
        OsthessenWetterException osthessen => osthessen.ErrorId,
        MosmixForecastException mosmix => mosmix.ErrorId,
        MqttMessageException mqtt => mqtt.ErrorId,
        _ => null,
    };

    /// <summary>
    /// Creates a new invalid configuration exception with the specified message.
    /// </summary>
    /// <param name="message">The message for the exception.</param>
    /// <returns>A new <see cref="CliException"/> instance.</returns>
    private static CliException Invalid(string message) => new(ConfigInvalid, message);
}
