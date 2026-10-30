namespace WeatherParser.Cli.Lifetime;

using System;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Cli.Arguments;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Reads the library deadline of the selected command from the registered library options: the CDC selection deadline, the MOSMIX selection deadline plus the MOSMIX_S refresh deadline with --RefreshMosmixSEvidence, the observation deadline, the OsthessenWetter page deadline or the MOSMIX parse deadline of the product (CL-14).
/// </summary>
public sealed class LibraryDeadlineResolver : ILibraryDeadlineResolver
{
    /// <inheritdoc/>
    public TimeSpan Resolve(IServiceProvider services, CommandRequest request)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(request);
        return request switch
        {
            SelectDwdRequest => services.GetRequiredService<CdcCatalogEndpoints>().SelectionDeadline,
            SelectMosmixRequest selectMosmix => MosmixSelection(services.GetRequiredService<MosmixCatalogEndpoints>(), selectMosmix.RefreshMosmixSEvidence),
            ParseDwdRequest => services.GetRequiredService<DwdObservationOptions>().ObservationDeadline,
            ParseOstHessenRequest => services.GetRequiredService<OsthessenWetterOptions>().PageDeadline,
            ParseMosmixRequest { Product: MosmixProduct.MosmixS } => services.GetRequiredService<MosmixSForecastOptions>().ParseDeadline,
            _ => services.GetRequiredService<MosmixLForecastOptions>().ParseDeadline,
        };
    }

    // The evidence refresh runs before the selection, so both deadlines bound the command (CL-14).
    private static TimeSpan MosmixSelection(MosmixCatalogEndpoints catalog, bool refresh) =>
        refresh ? catalog.SelectionDeadline + catalog.MosmixSRefreshDeadline : catalog.SelectionDeadline;
}
