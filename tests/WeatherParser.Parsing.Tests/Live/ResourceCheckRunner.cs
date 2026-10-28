using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests.Forecasts.Mosmix;

namespace WeatherParser.Parsing.Tests.Live;

internal static class ResourceCheckRunner
{
    public const string Variable = "WEATHERPARSER_RESOURCE_CHECK";
    public const string DirectoryVariable = "WEATHERPARSER_RESOURCE_CHECK_DIR";
    public const string GenerateMode = "generate";
    public const string MeasureMode = "measure";
    public const string FileName = "mosmix-s-synthetic.kmz";
    public const string ResultFileName = "resource-check.txt";
    public const string MosmixId = "10637";
    public const int RecordedPlacemarks = 5648;   // the all-stations file of 2026-10-26 (P7.02): 5648 placemarks, 40 elements each, 240 hourly steps
    public const int RecordedElements = 40;
    public const int RecordedSteps = 240;
    public static readonly TimeSpan DeadlineThreshold = TimeSpan.FromMinutes(5);   // MF-05: the MOSMIX_S parse deadline and request bound
    public const long PeakWorkingSetThresholdBytes = 256L * 1024 * 1024;            // MF-07 readiness rule: absolute process peak
    public const long StartPeakLimitBytes = 128L * 1024 * 1024;                     // MF-07 readiness rule: initial process peak

    public static string? Mode(Func<string, string?> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        var mode = environment(Variable);
        return string.Equals(mode, GenerateMode, StringComparison.Ordinal) || string.Equals(mode, MeasureMode, StringComparison.Ordinal) ? mode : null;
    }

    public static string? Directory(Func<string, string?> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return environment(DirectoryVariable);
    }

    public static async Task<string> GenerateAsync(string directory, int placemarks, int elements, int steps, CancellationToken cancellationToken)
    {
        directory = OutputDirectoryGuard.EnsureAllowed(directory);
        System.IO.Directory.CreateDirectory(directory);

        var bytes = MosmixForecastKmzBuilder.LargeAllStationsKmz(placemarks, placemarks / 2, MosmixId, steps, elements - 1, includeTemperature: true);
        var path = Path.Combine(directory, FileName);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);

        await using var stream = File.OpenRead(path);
        var hash = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken));

        var countsPath = Path.Combine(directory, FileName + ".counts");
        await File.WriteAllTextAsync(countsPath, $"placemarks={placemarks};elements={elements};steps={steps};sha256={hash}", cancellationToken);

        return path;
    }

    public static async Task<ResourceCheckResult> MeasureAsync(string directory, TimeProvider clock, CancellationToken cancellationToken, long? startPeakLimitBytes = null)
    {
        directory = OutputDirectoryGuard.EnsureAllowed(directory);
        var path = Path.Combine(directory, FileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The resource-check file is missing: {path}. Run the generate mode first.", path);
        }

        // Isolation rule: the measurement is only meaningful in a process whose peak working set has not been raised by other tests.
        long startPeak;
        using (var current = Process.GetCurrentProcess())
        {
            startPeak = current.PeakWorkingSet64;
        }

        var limit = startPeakLimitBytes ?? StartPeakLimitBytes;
        if (startPeak > limit)
        {
            throw new InvalidOperationException($"The resource check refuses to measure: the process peak working set is already {startPeak} bytes, above the isolation limit of {limit} bytes; run the measure mode alone in a fresh test process (dotnet test --filter FullyQualifiedName~ResourceCheck_Measure_OnlyWhenExplicitlyEnabled).");
        }

        var countsPath = Path.Combine(directory, FileName + ".counts");
        var countsText = await File.ReadAllTextAsync(countsPath, cancellationToken);
        var parts = countsText.Split(';');
        var placemarks = int.Parse(parts[0].Split('=')[1], CultureInfo.InvariantCulture);
        var elements = int.Parse(parts[1].Split('=')[1], CultureInfo.InvariantCulture);
        var steps = int.Parse(parts[2].Split('=')[1], CultureInfo.InvariantCulture);

        if (parts.Length < 4)
        {
            throw new InvalidOperationException("The resource-check companion file lacks the sha256 field; run the generate mode again.");
        }

        var expected = parts[3].Split('=')[1];
        await using var hashStream = File.OpenRead(path);
        var actual = Convert.ToHexStringLower(await SHA256.HashDataAsync(hashStream, cancellationToken));
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The resource-check file {path} does not match its companion (expected sha256 {expected}, actual {actual}); run the generate mode again.");
        }

        var services = new ServiceCollection().AddWeatherParsing();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(new HttpClient(new FileHandler(path), disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan });
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(LiveCheckSettings.Dwd(directory).Cdc);
        services.AddSingleton(LiveCheckSettings.Dwd(directory).Mosmix);
        services.AddSingleton(new DwdObservationOptions());
        services.AddSingleton(new OsthessenWetterOptions());
        services.AddSingleton(new OsthessenWetterEndpoints { PageUri = new Uri("https://osthessen.example/messwerte/") });
        services.AddSingleton(new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://resource-check.invalid/single_stations/") });
        services.AddSingleton(new MosmixLForecastOptions { MaxRunAge = TimeSpan.FromHours(240) });
        services.AddSingleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://resource-check.invalid/MOSMIX_S_LATEST_240.kmz") });
        services.AddSingleton(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = TimeSpan.FromHours(240) });

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

        var before = Process.GetCurrentProcess().WorkingSet64;
        var stopwatch = Stopwatch.StartNew();
        var result = await provider.GetRequiredService<IMosmixSParser>().ParseAsync(MosmixId, cancellationToken);
        stopwatch.Stop();

        using (var current = Process.GetCurrentProcess())
        {
            var peak = current.PeakWorkingSet64;

            return new ResourceCheckResult(
                new FileInfo(path).Length,
                placemarks,
                elements,
                steps,
                MosmixId,
                result.Run.Forecasts.Count,
                result.Freshness,
                stopwatch.Elapsed,
                before,
                peak,
                startPeak);
        }
    }

    public static async Task WriteAsync(ResourceCheckResult result, string directory, CancellationToken cancellationToken)
    {
        directory = OutputDirectoryGuard.EnsureAllowed(directory);
        var path = Path.Combine(directory, ResultFileName);
        await File.WriteAllTextAsync(path, $"""
            file_bytes={result.FileBytes}
            placemarks={result.Placemarks}
            elements={result.Elements}
            steps={result.Steps}
            mosmix_id={result.MosmixId}
            parse_steps={result.ParseSteps}
            freshness={result.Freshness}
            duration_ms={result.Duration.TotalMilliseconds:F0}
            working_set_before_bytes={result.WorkingSetBeforeBytes}
            start_peak_working_set_bytes={result.StartPeakWorkingSetBytes}
            peak_working_set_bytes={result.PeakWorkingSetBytes}
            deadline_threshold_ms=300000
            peak_threshold_bytes=268435456
            within_deadline={result.WithinDeadline.ToString().ToLowerInvariant()}
            within_peak={result.WithinPeak.ToString().ToLowerInvariant()}
            """, cancellationToken);
    }

    private sealed class FileHandler(string path) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
            };
            return Task.FromResult(response);
        }
    }
}
