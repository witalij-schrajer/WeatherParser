using Microsoft.Extensions.DependencyInjection;

namespace WeatherParser.Parsing.Parsing;

/// <summary>
/// Provides <see cref="IServiceCollection"/> registration for the WeatherParser.Parsing
/// endpoint validation services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services required to validate and map parser endpoint definitions.
    /// </summary>
    /// <param name="services">The service collection to register the services with.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddWeatherParsing(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IEndpointUriValidator, EndpointUriValidator>();
        services.AddSingleton<IParserEndpointValidator, ParserEndpointValidator>();

        return services;
    }
}
