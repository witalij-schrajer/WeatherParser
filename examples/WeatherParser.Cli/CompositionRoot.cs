using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Cli.Configuration;

namespace WeatherParser.Cli;

/// <summary>
/// Builds the validated container of the process: AddWeatherParsing, the HttpClient of CL-21, the system TimeProvider and the configuration registrations supplied by the caller; ValidateOnBuild and ValidateScopes are on (CL-07).
/// </summary>
public static class CompositionRoot
{
    /// <summary>
    /// Builds the one validated container of the process: AddWeatherParsing, the HttpClient of CL-21, the system TimeProvider and the configuration registrations supplied by the caller; ValidateOnBuild and ValidateScopes are on (CL-07).
    /// </summary>
    /// <param name="registerConfiguration">The action to register consumer services.</param>
    /// <returns>The built service provider.</returns>
    public static ServiceProvider Build(Action<IServiceCollection> registerConfiguration)
    {
        ArgumentNullException.ThrowIfNull(registerConfiguration);

        var services = new ServiceCollection();
        services.AddWeatherParsing();
        // The configuration registrar registers the Http section; without it the default User-Agent is used.
        services.AddSingleton(provider => CreateHttpClient(provider.GetService<HttpSection>()?.UserAgent));
        services.AddSingleton(TimeProvider.System);
        registerConfiguration(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    /// <summary>
    /// Creates the HTTP handler of CL-21: no automatic redirects, no automatic decompression.
    /// </summary>
    /// <returns>The created SocketsHttpHandler.</returns>
    public static SocketsHttpHandler CreateHttpHandler()
    {
        return new SocketsHttpHandler { AllowAutoRedirect = false, AutomaticDecompression = DecompressionMethods.None };
    }

    /// <summary>
    /// Creates the HttpClient of CL-21 over CreateHttpHandler with an infinite client timeout and the User-Agent Http.UserAgent, or WeatherParser/<informational version> without it.
    /// </summary>
    /// <param name="userAgent">The configured Http.UserAgent, or null for the default WeatherParser/<informational version> (CL-21).</param>
    /// <returns>The created HttpClient.</returns>
    public static HttpClient CreateHttpClient(string? userAgent)
    {
        var handler = CreateHttpHandler();
        var client = new HttpClient(handler, disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };

        var version = typeof(CompositionRoot).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];

        if (userAgent is null)
        {
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("WeatherParser", version));
        }
        else
        {
            // The configured value is sent exactly as written; the configuration validator allows only 1 to 200 printable ASCII characters (CL-21).
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
        }

        return client;
    }
}
