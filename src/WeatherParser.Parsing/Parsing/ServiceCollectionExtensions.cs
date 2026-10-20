using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
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

        return services;
    }
}
