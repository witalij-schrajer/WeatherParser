using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using WeatherParser.Parsing.Observations.Osthessen;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Forecasts;
using System.Diagnostics;

namespace WeatherParser.Parsing.Tests.Live;

internal static class LiveCheckRunner
{
    public const string EnableVariable = "WEATHERPARSER_LIVE_CHECK";
    public const string ModeVariable = "WEATHERPARSER_LIVE_CHECK_MODE";
    public const string ObservationMode = "observation";
    public const string OsthessenMode = "osthessen";
    public const string MosmixLMode = "mosmix-l";
    public const string MosmixSMode = "mosmix-s";

    public static bool IsEnabled(Func<string, string?> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return string.Equals(environment(EnableVariable), "1", StringComparison.Ordinal);
    }

    public static bool IsObservationMode(Func<string, string?> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return IsEnabled(environment) && string.Equals(environment(ModeVariable), ObservationMode, StringComparison.Ordinal);
    }

    public static bool IsOsthessenMode(Func<string, string?> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return IsEnabled(environment) && string.Equals(environment(ModeVariable), OsthessenMode, StringComparison.Ordinal);
    }

    public static bool IsMosmixLMode(Func<string, string?> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return IsEnabled(environment) && string.Equals(environment(ModeVariable), MosmixLMode, StringComparison.Ordinal);
    }

    public static bool IsMosmixSMode(Func<string, string?> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return IsEnabled(environment) && string.Equals(environment(ModeVariable), MosmixSMode, StringComparison.Ordinal);
    }

    public static bool IsSelectionMode(Func<string, string?> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return IsEnabled(environment) && !IsObservationMode(environment) && !IsOsthessenMode(environment) && !IsMosmixLMode(environment) && !IsMosmixSMode(environment);
    }

    public static async Task<LiveCheckResult?> RunIfEnabledAsync(Func<string, string?> environment, LiveCheckSettings settings, HttpMessageHandler transport, TimeProvider clock, CancellationToken cancellationToken)
    {
        if (!IsEnabled(environment))
        {
            return null;
        }

        return await RunAsync(settings, transport, clock, cancellationToken);
    }

    public static async Task<LiveObservationResult?> RunObservationIfEnabledAsync(Func<string, string?> environment, LiveCheckSettings settings, HttpMessageHandler transport, TimeProvider clock, CancellationToken cancellationToken)
    {
        if (!IsObservationMode(environment))
        {
            return null;
        }

        return await RunObservationAsync(settings, transport, clock, cancellationToken);
    }

    public static async Task<LiveMosmixResult?> RunMosmixIfEnabledAsync(Func<string, string?> environment, LiveCheckSettings settings, HttpMessageHandler transport, TimeProvider clock, CancellationToken cancellationToken)
    {
        if (!IsMosmixLMode(environment) && !IsMosmixSMode(environment))
        {
            return null;
        }

        var product = IsMosmixLMode(environment) ? MosmixProduct.MosmixL : MosmixProduct.MosmixS;
        return await RunMosmixAsync(product, settings, transport, clock, cancellationToken);
    }

    public static async Task<LiveMosmixResult> RunMosmixAsync(MosmixProduct product, LiveCheckSettings settings, HttpMessageHandler transport, TimeProvider clock, CancellationToken cancellationToken)
    {
        var budget = new RequestBudgetHandler(LiveCheckSettings.MosmixRequestBudget, transport);
        var lLogger = new CapturingLogger<MosmixLParser>();
        var sLogger = new CapturingLogger<MosmixSParser>();

        var services = new ServiceCollection().AddWeatherParsing();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILogger<MosmixLParser>>(lLogger);
        services.AddSingleton<ILogger<MosmixSParser>>(sLogger);
        services.AddSingleton(new HttpClient(budget, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan });
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(settings.Cdc);
        services.AddSingleton(settings.Mosmix);
        services.AddSingleton(new DwdObservationOptions());
        services.AddSingleton(new OsthessenWetterOptions { Stations = { new OsthessenWetterStationMetadata { Name = "Teststation", Latitude = 50.5m, Longitude = 9.7m } } });
        services.AddSingleton(new OsthessenWetterEndpoints { PageUri = new Uri("https://osthessen.example/messwerte/") });
        services.AddSingleton(new MosmixLForecastEndpoints { SingleStationsDirectory = settings.MosmixLSingleStationsDirectory });
        services.AddSingleton(new MosmixSForecastEndpoints { AllStationsUri = settings.MosmixSAllStationsUri });
        services.AddSingleton(new MosmixLForecastOptions { MaxRunAge = settings.MosmixLMaxRunAge });
        services.AddSingleton(new MosmixSForecastOptions { MaxRunAge = settings.MosmixSMaxRunAge });

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Func<CancellationToken, Task<MosmixParseResult>> parse = product == MosmixProduct.MosmixS ? ct => provider.GetRequiredService<IMosmixSParser>().ParseAsync(settings.MosmixId, ct) : ct => provider.GetRequiredService<IMosmixLParser>().ParseAsync(settings.MosmixId, ct);
        var json = provider.GetRequiredService<IJsonExportSerializer>();
        var xml = provider.GetRequiredService<IXmlExportSerializer>();

        MosmixParseResult? parsed = null;
        string? errorId = null;
        IReadOnlyList<OmittedForecastValue> omitted = Array.Empty<OmittedForecastValue>();
        IReadOnlyList<string> notExported = Array.Empty<string>();
        IReadOnlyList<string> absent = Array.Empty<string>();
        ForecastRun? run = null;
        MosmixRunFreshness? freshness = null;
        TimeSpan? runAge = null;

        var stopwatch = Stopwatch.StartNew();
        try
        {
            parsed = await parse(cancellationToken);
        }
        catch (MosmixForecastException exception)
        {
            errorId = exception.ErrorId;
            omitted = exception.OmittedValues;
            notExported = exception.NotExportedElements;
        }
        stopwatch.Stop();

        if (parsed is not null)
        {
            run = parsed.Run;
            freshness = parsed.Freshness;
            runAge = parsed.RunAge;
            absent = parsed.AbsentElements;
            omitted = parsed.OmittedValues;
            notExported = parsed.NotExportedElements;
        }

        byte[]? jsonBytes = null;
        byte[]? xmlBytes = null;

        IReadOnlyList<string> schemaEvents = Array.Empty<string>();

        if (run is not null)
        {
            using var jsonStream = new MemoryStream();
            await json.SerializeAsync(run, jsonStream, cancellationToken);
            jsonBytes = jsonStream.ToArray();

            using var xmlStream = new MemoryStream();
            await xml.SerializeAsync(run, xmlStream, cancellationToken);
            xmlBytes = xmlStream.ToArray();

            schemaEvents = ExportSchemaValidation.Validate(xmlBytes);
        }

        var peak = Process.GetCurrentProcess().PeakWorkingSet64;

        return new LiveMosmixResult(
            product,
            settings.MosmixId,
            run,
            freshness,
            runAge,
            omitted,
            notExported,
            absent,
            errorId,
            jsonBytes,
            xmlBytes,
            schemaEvents,
            budget.Requests,
            product == MosmixProduct.MosmixS ? sLogger.Entries : lLogger.Entries,
            stopwatch.Elapsed,
            peak);
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
        services.AddSingleton(new OsthessenWetterOptions { Stations = { new OsthessenWetterStationMetadata { Name = "Teststation", Latitude = 50.5m, Longitude = 9.7m } } });
        services.AddSingleton(new OsthessenWetterEndpoints { PageUri = new Uri("https://osthessen.example/messwerte/") });
        services.AddSingleton(new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://mosmix.example/single_stations/") });
        services.AddSingleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://mosmix.example/MOSMIX_S_LATEST_240.kmz") });
        services.AddSingleton(new MosmixLForecastOptions { MaxRunAge = TimeSpan.FromHours(8) });
        services.AddSingleton(new MosmixSForecastOptions { MaxRunAge = TimeSpan.FromHours(2) });

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

    public static async Task<LiveObservationResult> RunObservationAsync(LiveCheckSettings settings, HttpMessageHandler transport, TimeProvider clock, CancellationToken cancellationToken)
    {
        var budget = new RequestBudgetHandler(LiveCheckSettings.ObservationRequestBudget, transport);
        var parserLogger = new CapturingLogger<DwdObservationParser>();

        var services = new ServiceCollection().AddWeatherParsing();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILogger<DwdObservationParser>>(parserLogger);
        services.AddSingleton(new HttpClient(budget, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan });
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(settings.Cdc);
        services.AddSingleton(settings.Mosmix);
        services.AddSingleton(new DwdObservationOptions());
        services.AddSingleton(new OsthessenWetterOptions { Stations = { new OsthessenWetterStationMetadata { Name = "Teststation", Latitude = 50.5m, Longitude = 9.7m } } });
        services.AddSingleton(new OsthessenWetterEndpoints { PageUri = new Uri("https://osthessen.example/messwerte/") });
        services.AddSingleton(new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://mosmix.example/single_stations/") });
        services.AddSingleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://mosmix.example/MOSMIX_S_LATEST_240.kmz") });
        services.AddSingleton(new MosmixLForecastOptions { MaxRunAge = TimeSpan.FromHours(8) });
        services.AddSingleton(new MosmixSForecastOptions { MaxRunAge = TimeSpan.FromHours(2) });

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var parser = provider.GetRequiredService<IDwdObservationParser>();
        var json = provider.GetRequiredService<IJsonExportSerializer>();
        var xml = provider.GetRequiredService<IXmlExportSerializer>();

        var dataset = await parser.ParseAsync(settings.ObservationStationId, cancellationToken);

        using var jsonStream = new MemoryStream();
        await json.SerializeAsync(dataset, jsonStream, cancellationToken);
        var jsonBytes = jsonStream.ToArray();

        using var xmlStream = new MemoryStream();
        await xml.SerializeAsync(dataset, xmlStream, cancellationToken);
        var xmlBytes = xmlStream.ToArray();

        var events = ExportSchemaValidation.Validate(xmlBytes);

        return new LiveObservationResult(dataset, jsonBytes, xmlBytes, events, budget.Requests, parserLogger.Entries.ToList());
    }

    public static async Task<LiveOsthessenResult?> RunOsthessenIfEnabledAsync(Func<string, string?> environment, LiveCheckSettings settings, HttpMessageHandler transport, TimeProvider clock, CancellationToken cancellationToken)
    {
        if (!IsOsthessenMode(environment))
        {
            return null;
        }

        return await RunOsthessenAsync(settings, transport, clock, cancellationToken);
    }

    public static async Task<LiveOsthessenResult> RunOsthessenAsync(LiveCheckSettings settings, HttpMessageHandler transport, TimeProvider clock, CancellationToken cancellationToken)
    {
        var budget = new RequestBudgetHandler(LiveCheckSettings.OsthessenRequestBudget, transport);
        var parserLogger = new CapturingLogger<OsthessenWetterParser>();

        var services = new ServiceCollection().AddWeatherParsing();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILogger<OsthessenWetterParser>>(parserLogger);
        services.AddSingleton(new HttpClient(budget, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan });
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(settings.Cdc);
        services.AddSingleton(settings.Mosmix);
        services.AddSingleton(new DwdObservationOptions());
        services.AddSingleton(new OsthessenWetterOptions());
        services.AddSingleton(new OsthessenWetterEndpoints { PageUri = settings.OsthessenPageUri });
        // The real page has no consumer metadata here: placeholder coordinates let every online station build a dataset.
        services.Replace(ServiceDescriptor.Singleton<IOsthessenWetterStationMetadataLookup, PlaceholderMetadataLookup>());
        services.AddSingleton(new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://mosmix.example/single_stations/") });
        services.AddSingleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://mosmix.example/MOSMIX_S_LATEST_240.kmz") });
        services.AddSingleton(new MosmixLForecastOptions { MaxRunAge = TimeSpan.FromHours(8) });
        services.AddSingleton(new MosmixSForecastOptions { MaxRunAge = TimeSpan.FromHours(2) });

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var parser = provider.GetRequiredService<IOsthessenWetterParser>();
        var json = provider.GetRequiredService<IJsonExportSerializer>();
        var xml = provider.GetRequiredService<IXmlExportSerializer>();

        IReadOnlyList<ObservationDataset> datasets;
        IReadOnlyList<OsthessenWetterStationFailure> failures;
        string? errorId = null;
        try
        {
            var parsed = await parser.ParseAsync(settings.OsthessenPattern, cancellationToken);
            datasets = parsed.Datasets;
            failures = parsed.StationFailures;
        }
        catch (OsthessenWetterException exception) when (exception.ErrorId == OsthessenWetterException.NoUsableStationErrorId)
        {
            datasets = Array.Empty<ObservationDataset>();
            failures = exception.StationFailures;
            errorId = exception.ErrorId;
        }

        var jsonDocuments = new List<byte[]>();
        var xmlDocuments = new List<byte[]>();
        var events = new List<string>();
        foreach (var dataset in datasets)
        {
            using var jsonStream = new MemoryStream();
            await json.SerializeAsync(dataset, jsonStream, cancellationToken);
            jsonDocuments.Add(jsonStream.ToArray());

            using var xmlStream = new MemoryStream();
            await xml.SerializeAsync(dataset, xmlStream, cancellationToken);
            var xmlBytes = xmlStream.ToArray();
            xmlDocuments.Add(xmlBytes);
            events.AddRange(ExportSchemaValidation.Validate(xmlBytes));
        }

        return new LiveOsthessenResult(datasets, failures, errorId, jsonDocuments, xmlDocuments, events, budget.Requests, parserLogger.Entries.ToList());
    }

    public static void EnsureUsable(LiveOsthessenResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.ErrorId is not null || result.Datasets.Count == 0)
        {
            throw new InvalidOperationException($"The OsthessenWetter live check produced no usable dataset (error {result.ErrorId ?? "none"}, failures {result.StationFailures.Count}).");
        }
    }

    public static SocketsHttpHandler CreateTransport()
    {
        var transport = new SocketsHttpHandler();
        transport.AllowAutoRedirect = false;
        return transport;
    }
}
