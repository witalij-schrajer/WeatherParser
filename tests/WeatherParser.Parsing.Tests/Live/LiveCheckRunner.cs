using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Selection;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Schemas;
using WeatherParser.Parsing.Tests.Selection;
using WeatherParser.Parsing.Observations.Dwd;

namespace WeatherParser.Parsing.Tests.Live;

internal static class LiveCheckRunner
{
    public const string EnableVariable = "WEATHERPARSER_LIVE_CHECK";

    public static bool IsEnabled(Func<string, string?> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return string.Equals(environment(EnableVariable), "1", StringComparison.Ordinal);
    }

    public static async Task<LiveCheckResult?> RunIfEnabledAsync(Func<string, string?> environment, LiveCheckSettings settings, HttpMessageHandler transport, TimeProvider clock, CancellationToken cancellationToken)
    {
        if (!IsEnabled(environment))
        {
            return null;
        }

        return await RunAsync(settings, transport, clock, cancellationToken);
    }

    public static async Task<LiveCheckResult> RunAsync(LiveCheckSettings settings, HttpMessageHandler transport, TimeProvider clock, CancellationToken cancellationToken)
    {
        var budget = new RequestBudgetHandler(settings.RequestBudget, transport);
        var cdcLogger = new CapturingLogger<CdcStationSelector>();
        var mosmixLogger = new CapturingLogger<MosmixLocationSelector>();

        var services = new ServiceCollection().AddWeatherParsing();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILogger<CdcStationSelector>>(cdcLogger);
        services.AddSingleton<ILogger<MosmixLocationSelector>>(mosmixLogger);
        services.AddSingleton(new HttpClient(budget, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan });
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(settings.Cdc);
        services.AddSingleton(settings.Mosmix);
        services.AddSingleton(new DwdObservationOptions());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var refresher = provider.GetRequiredService<IMosmixSEvidenceRefresher>();
        var cache = provider.GetRequiredService<IMosmixEvidenceCache>();
        var mosmixSelector = provider.GetRequiredService<IMosmixLocationSelector>();
        var cdcSelector = provider.GetRequiredService<ICdcStationSelector>();
        var json = provider.GetRequiredService<IJsonExportSerializer>();
        var xml = provider.GetRequiredService<IXmlExportSerializer>();

        if (settings.RefreshFirst)
        {
            await refresher.RefreshAsync(cancellationToken);
        }

        var mosmixList = await mosmixSelector.SelectAsync(settings.Parameters, cancellationToken);
        var cdcList = await cdcSelector.SelectAsync(settings.Parameters, cancellationToken);
        var index = await cache.ReadAsync(cancellationToken);

        using var mosmixJsonStream = new MemoryStream();
        await json.SerializeAsync(mosmixList, mosmixJsonStream, cancellationToken);
        var mosmixJson = mosmixJsonStream.ToArray();

        using var mosmixXmlStream = new MemoryStream();
        await xml.SerializeAsync(mosmixList, mosmixXmlStream, cancellationToken);
        var mosmixXml = mosmixXmlStream.ToArray();

        using var cdcJsonStream = new MemoryStream();
        await json.SerializeAsync(cdcList, cdcJsonStream, cancellationToken);
        var cdcJson = cdcJsonStream.ToArray();

        using var cdcXmlStream = new MemoryStream();
        await xml.SerializeAsync(cdcList, cdcXmlStream, cancellationToken);
        var cdcXml = cdcXmlStream.ToArray();

        var mosmixEvents = ExportSchemaValidation.Validate(mosmixXml);
        var cdcEvents = ExportSchemaValidation.Validate(cdcXml);

        return new LiveCheckResult(
            index,
            mosmixList,
            cdcList,
            mosmixJson,
            mosmixXml,
            cdcJson,
            cdcXml,
            mosmixEvents,
            cdcEvents,
            budget.Requests,
            cdcLogger.Entries.Concat(mosmixLogger.Entries).ToList());
    }

    public static SocketsHttpHandler CreateTransport()
    {
        var transport = new SocketsHttpHandler();
        transport.AllowAutoRedirect = false;
        return transport;
    }
}
