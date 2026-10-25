using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Selection;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Parsing;

/// <summary>
/// Provides <see cref="IServiceCollection"/> registration for the WeatherParser.Parsing
/// endpoint validation services and the JSON and XML export serializers.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services required to validate and map parser endpoint definitions,
    /// as well as the JSON and XML export serializers and the list entry ordering service.
    /// The source-time normalization services are also registered.
    /// The quantity services (output rounding, pressure conversion, validation, period-rate and wind-speed conversion) are also registered.
    /// The observation dataset builder is also registered.
    /// The forecast run builder is also registered.
    /// The coordinate parser is also registered.
    /// The search-parameter factory is also registered.
    /// The distance calculator is also registered.
    /// The search-area filter is also registered.
    /// The CDC station-list reader is also registered.
    /// The CDC evidence reader is also registered.
    /// The CDC availability deriver is also registered.
    /// The MOSMIX station-catalog reader is also registered.
    /// The MOSMIX catalog endpoints validator is also registered.
    /// The MOSMIX_L evidence reader is also registered.
    /// The MOSMIX_S evidence cache is also registered.
    /// The MOSMIX availability resolver is also registered.
    /// The catalog downloader is also registered; the consumer registers the <see cref="HttpClient"/> and the <see cref="TimeProvider"/>.
    /// The CDC endpoints validator and the CDC and MOSMIX catalog sources are also registered; the consumer registers <see cref="CdcCatalogEndpoints"/> and <see cref="WeatherParser.Parsing.Evidence.MosmixS.MosmixCatalogEndpoints"/>.
    /// The MOSMIX_S evidence refresher is also registered; its timing is controlled externally (no scheduler).
    /// The DWD station resolver is also registered.
    /// The DWD observation mapper is also registered.
    /// The DWD observation source is also registered; the consumer registers <see cref="DwdObservationOptions"/>.
    /// The DWD observation parser is also registered.
    /// The CDC request composer is also registered.
    /// </summary>
    /// <param name="services">The service collection to register the services with.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddWeatherParsing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IEndpointUriValidator, EndpointUriValidator>();
        services.AddSingleton<IParserEndpointValidator, ParserEndpointValidator>();
        services.AddSingleton<IJsonExportSerializer, JsonExportSerializer>();
        services.AddSingleton<IXmlExportSerializer, XmlExportSerializer>();
        services.AddSingleton<IListEntryOrdering, ListEntryOrdering>();
        services.AddSingleton<ITimeZoneResolver, TimeZoneResolver>();
        services.AddSingleton<ISourceTimeNormalizer, SourceTimeNormalizer>();
        services.AddSingleton<IOutputRounding, OutputRounding>();
        services.AddSingleton<IPressureConverter, PressureConverter>();
        services.AddSingleton<IQuantityValidator, QuantityValidator>();
        services.AddSingleton<IPeriodRateConverter, PeriodRateConverter>();
        services.AddSingleton<IWindSpeedConverter, WindSpeedConverter>();
        services.AddSingleton<IObservationDatasetBuilder, ObservationDatasetBuilder>();
        services.AddSingleton<IForecastRunBuilder, ForecastRunBuilder>();
        services.AddSingleton<ICoordinateParser, CoordinateParser>();
        services.AddSingleton<ISearchParameterFactory, SearchParameterFactory>();
        services.AddSingleton<IDistanceCalculator, DistanceCalculator>();
        services.AddSingleton<ISearchAreaFilter, SearchAreaFilter>();
        services.AddSingleton<ICdcStationCatalogReader, CdcStationCatalogReader>();
        services.AddSingleton<ICdcNowEvidenceReader, CdcNowEvidenceReader>();
        services.AddSingleton<ICdcAvailabilityDeriver, CdcAvailabilityDeriver>();
        services.AddSingleton<IMosmixLocationCatalogReader, MosmixLocationCatalogReader>();
        services.AddSingleton<IMosmixCatalogEndpointsValidator, MosmixCatalogEndpointsValidator>();
        services.AddSingleton<IMosmixLEvidenceReader, MosmixLEvidenceReader>();
        services.AddSingleton<IMosmixEvidenceCache, FileSystemMosmixEvidenceCache>();
        services.AddSingleton<IMosmixAvailabilityResolver, MosmixAvailabilityResolver>();
        services.AddSingleton<ICatalogDownloader, CatalogDownloader>();
        services.AddSingleton<ICdcCatalogEndpointsValidator, CdcCatalogEndpointsValidator>();
        services.AddSingleton<ICdcRequestComposer, CdcRequestComposer>();
        services.AddSingleton<ICdcCatalogSource, CdcCatalogSource>();
        services.AddSingleton<IMosmixCatalogSource, MosmixCatalogSource>();
        services.AddSingleton<IMosmixSEvidenceRefresher, MosmixSEvidenceRefresher>();
        services.AddSingleton<ICdcStationSelector, CdcStationSelector>();
        services.AddSingleton<IMosmixLocationSelector, MosmixLocationSelector>();
        services.AddSingleton<IDwdStationResolver, DwdStationResolver>();
        services.AddSingleton<IDwdObservationMapper, DwdObservationMapper>();
        services.AddSingleton<IDwdObservationSource, DwdObservationSource>();
        services.AddSingleton<IDwdObservationParser, DwdObservationParser>();
        services.AddSingleton<IStationNamePatternMatcher, StationNamePatternMatcher>();
        services.AddSingleton<IOsthessenWetterPageReader, OsthessenWetterPageReader>();
        services.AddSingleton<IOsthessenWetterStationIdentity, OsthessenWetterStationIdentity>();
        services.AddSingleton<IOsthessenWetterStationMetadataLookup, OsthessenWetterStationMetadataLookup>();
        services.AddSingleton<IOsthessenWetterValueReader, OsthessenWetterValueReader>();
        services.AddSingleton<IOsthessenWetterTimeReader, OsthessenWetterTimeReader>();
        services.AddSingleton<IOsthessenWetterObservationMapper, OsthessenWetterObservationMapper>();
        services.AddSingleton<IOsthessenWetterPageSource, OsthessenWetterPageSource>();

        return services;
    }
}
