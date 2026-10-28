using FluentAssertions;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Evidence.MosmixL;
using WeatherParser.Parsing.Tests.Evidence.MosmixS;
using WeatherParser.Parsing.Tests.Parsing;
using WeatherParser.Parsing.Tests.Selection;
using WeatherParser.Parsing.Tests.Export;

namespace WeatherParser.Parsing.Tests.Live;

[TestClass]
public sealed class LiveCheckTests
{
    [TestMethod]
    [TestCategory("Live")]
    [Timeout(900000)]
    public async Task LiveCheck_DwdSources_OnlyWhenExplicitlyEnabled()
    {
        if (!LiveCheckRunner.IsEnabled(Environment.GetEnvironmentVariable))
        {
            Assert.Inconclusive($"Live check skipped: set {LiveCheckRunner.EnableVariable}=1 to run it once against the DWD sources (contract SC-15, SC-16).");
        }

        if (!LiveCheckRunner.IsSelectionMode(Environment.GetEnvironmentVariable))
        {
            Assert.Inconclusive($"Selection live check skipped: {LiveCheckRunner.ModeVariable} selects another live check (contract OC-13, OW-15).");
        }

        var cache = Path.Combine(Path.GetTempPath(), "wp-live-cache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(cache);
        var output = Path.Combine(Path.GetTempPath(), "wp-live-check-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(output);

        try
        {
            var settings = LiveCheckSettings.Dwd(cache);
            using var transport = LiveCheckRunner.CreateTransport();
            var result = await LiveCheckRunner.RunAsync(settings, transport, TimeProvider.System, CancellationToken.None);

            var cdcJsonPath = Path.Combine(output, "cdc.json");
            await File.WriteAllBytesAsync(cdcJsonPath, result.CdcJson);
            var cdcXmlPath = Path.Combine(output, "cdc.xml");
            await File.WriteAllBytesAsync(cdcXmlPath, result.CdcXml);
            var mosmixJsonPath = Path.Combine(output, "mosmix.json");
            await File.WriteAllBytesAsync(mosmixJsonPath, result.MosmixJson);
            var mosmixXmlPath = Path.Combine(output, "mosmix.xml");
            await File.WriteAllBytesAsync(mosmixXmlPath, result.MosmixXml);

            var summary = new StringBuilder();
            summary.AppendLine($"budget={settings.RequestBudget}");
            summary.AppendLine($"requests={result.Requests.Count}");
            foreach (var req in result.Requests)
            {
                var contentLength = req.ContentLength?.ToString(CultureInfo.InvariantCulture) ?? "-";
                summary.AppendLine($"{(int)req.StatusCode} {contentLength} {req.BytesRead} {req.Uri}");
            }
            summary.AppendLine($"cdc_stations={result.CdcList.Stations.Count}");
            summary.AppendLine($"mosmix_locations={result.MosmixList.Locations.Count}");
            summary.AppendLine($"index_downloaded_at={result.Index?.DownloadedAtUtc:yyyy-MM-ddTHH:mm:ss}");
            summary.AppendLine($"index_issued_at={result.Index?.IssuedAtUtc:yyyy-MM-ddTHH:mm:ss}");
            summary.AppendLine($"cdc_schema_events={result.CdcSchemaEvents.Count}");
            summary.AppendLine($"mosmix_schema_events={result.MosmixSchemaEvents.Count}");
            foreach (var notice in result.Notices)
            {
                summary.AppendLine($"notice {notice.Level} {notice.EventName} {notice.Message}");
            }

            var summaryPath = Path.Combine(output, "summary.txt");
            await File.WriteAllTextAsync(summaryPath, summary.ToString());

            Console.WriteLine(output);

            result.Requests.Count.Should().BeLessThanOrEqualTo(settings.RequestBudget);
            result.MosmixSchemaEvents.Should().BeEmpty();
            result.CdcSchemaEvents.Should().BeEmpty();
            result.Index.Should().NotBeNull();
        }
        finally
        {
            if (Directory.Exists(cache))
            {
                Directory.Delete(cache, recursive: true);
            }
        }
    }

    [TestMethod]
    [TestCategory("Live")]
    [Timeout(300000)]
    public async Task LiveCheck_DwdObservation_OnlyWhenExplicitlyEnabled()
    {
        if (!LiveCheckRunner.IsObservationMode(Environment.GetEnvironmentVariable))
        {
            Assert.Inconclusive($"Observation live check skipped: set {LiveCheckRunner.EnableVariable}=1 and {LiveCheckRunner.ModeVariable}={LiveCheckRunner.ObservationMode} to run one DWD observation parse (contract OC-13).");
        }

        var cache = Path.Combine(Path.GetTempPath(), "wp-live-cache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(cache);
        var output = Path.Combine(Path.GetTempPath(), "wp-live-observation-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(output);

        try
        {
            var settings = LiveCheckSettings.Dwd(cache);
            using var transport = LiveCheckRunner.CreateTransport();
            var result = await LiveCheckRunner.RunObservationAsync(settings, transport, TimeProvider.System, CancellationToken.None);

            await LiveObservationArtifacts.WriteAsync(result, settings, output, CancellationToken.None);

            Console.WriteLine(output);

            using var json = JsonDocument.Parse(result.Json);
            result.Requests.Count.Should().BeLessThanOrEqualTo(LiveCheckSettings.ObservationRequestBudget);
            result.SchemaEvents.Should().BeEmpty();
            result.Dataset.Source.Should().Be("dwd:" + settings.ObservationStationId);
            ExportEquivalence.AssertEquivalent(json.RootElement, XDocument.Parse(Encoding.UTF8.GetString(result.Xml)).Root!, "observation_dataset");
        }
        finally
        {
            if (Directory.Exists(cache))
            {
                Directory.Delete(cache, recursive: true);
            }
        }
    }

    [TestMethod]
    [TestCategory("Live")]
    [Timeout(300000)]
    public async Task LiveCheck_Osthessen_OnlyWhenExplicitlyEnabled()
    {
        if (!LiveCheckRunner.IsOsthessenMode(Environment.GetEnvironmentVariable))
        {
            Assert.Inconclusive($"OsthessenWetter live check skipped: set {LiveCheckRunner.EnableVariable}=1 and {LiveCheckRunner.ModeVariable}={LiveCheckRunner.OsthessenMode} to run one request against the OsthessenWetter page (contract OW-15).");
        }

        var cache = Path.Combine(Path.GetTempPath(), "wp-live-cache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(cache);
        var output = Path.Combine(Path.GetTempPath(), "wp-live-osthessen-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(output);

        try
        {
            var settings = LiveCheckSettings.Dwd(cache);
            using var transport = LiveCheckRunner.CreateTransport();
            var result = await LiveCheckRunner.RunOsthessenAsync(settings, transport, TimeProvider.System, CancellationToken.None);

            await LiveOsthessenArtifacts.WriteAsync(result, settings, output, CancellationToken.None);

            Console.WriteLine(output);

            LiveCheckRunner.EnsureUsable(result);

            result.Requests.Count.Should().BeLessThanOrEqualTo(LiveCheckSettings.OsthessenRequestBudget);
            result.SchemaEvents.Should().BeEmpty();
            result.Datasets.Should().OnlyContain(d => d.Source.StartsWith("osthessenwetter:", StringComparison.Ordinal));
            for (var i = 0; i < result.Datasets.Count; i++)
            {
                using var json = JsonDocument.Parse(result.Json[i]);
                ExportEquivalence.AssertEquivalent(json.RootElement, XDocument.Parse(Encoding.UTF8.GetString(result.Xml[i])).Root!, "observation_dataset");
            }
        }
        finally
        {
            if (Directory.Exists(cache))
            {
                Directory.Delete(cache, recursive: true);
            }
        }
    }

    [TestMethod]
    [TestCategory("Live")]
    [Timeout(600000)]
    public async Task LiveCheck_MosmixL_OnlyWhenExplicitlyEnabled()
    {
        if (!LiveCheckRunner.IsMosmixLMode(Environment.GetEnvironmentVariable))
        {
            Assert.Inconclusive($"MosmixL live check skipped: set {LiveCheckRunner.EnableVariable}=1 and {LiveCheckRunner.ModeVariable}={LiveCheckRunner.MosmixLMode} to run one request against the DWD MOSMIX_L (contract OC-13).");
        }

        var cache = Path.Combine(Path.GetTempPath(), "wp-live-cache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(cache);
        var output = Path.Combine(Path.GetTempPath(), "wp-live-mosmix-l-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(output);

        try
        {
            var settings = LiveCheckSettings.Dwd(cache);
            using var transport = LiveCheckRunner.CreateTransport();
            var result = await LiveCheckRunner.RunMosmixAsync(MosmixProduct.MosmixL, settings, transport, TimeProvider.System, CancellationToken.None);

            await LiveMosmixArtifacts.WriteAsync(result, settings, output, CancellationToken.None);
            Console.WriteLine(output);

            result.Requests.Count.Should().BeLessThanOrEqualTo(LiveCheckSettings.MosmixRequestBudget);
            result.ErrorId.Should().BeNull();
            result.Run.Should().NotBeNull();
            result.Run!.Product.Should().Be(MosmixProduct.MosmixL);
            result.SchemaEvents.Should().BeEmpty();

            result.Json.Should().NotBeNull();
            result.Xml.Should().NotBeNull();
            ExportEquivalence.AssertEquivalent(JsonDocument.Parse(result.Json!).RootElement, XDocument.Parse(Encoding.UTF8.GetString(result.Xml!)).Root!, "forecast_run");
        }
        finally
        {
            if (Directory.Exists(cache))
            {
                Directory.Delete(cache, recursive: true);
            }
        }
    }

    [TestMethod]
    [TestCategory("Live")]
    [Timeout(600000)]
    public async Task LiveCheck_MosmixS_OnlyWhenExplicitlyEnabled()
    {
        if (!LiveCheckRunner.IsMosmixSMode(Environment.GetEnvironmentVariable))
        {
            Assert.Inconclusive($"MosmixS live check skipped: set {LiveCheckRunner.EnableVariable}=1 and {LiveCheckRunner.ModeVariable}={LiveCheckRunner.MosmixSMode} to run one request against the DWD MOSMIX_S (contract OC-13).");
        }

        var cache = Path.Combine(Path.GetTempPath(), "wp-live-cache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(cache);
        var output = Path.Combine(Path.GetTempPath(), "wp-live-mosmix-s-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(output);

        try
        {
            var settings = LiveCheckSettings.Dwd(cache);
            using var transport = LiveCheckRunner.CreateTransport();
            var result = await LiveCheckRunner.RunMosmixAsync(MosmixProduct.MosmixS, settings, transport, TimeProvider.System, CancellationToken.None);

            await LiveMosmixArtifacts.WriteAsync(result, settings, output, CancellationToken.None);
            Console.WriteLine(output);

            result.Requests.Count.Should().BeLessThanOrEqualTo(LiveCheckSettings.MosmixRequestBudget);
            result.ErrorId.Should().BeNull();
            result.Run.Should().NotBeNull();
            result.Run!.Product.Should().Be(MosmixProduct.MosmixS);
            result.SchemaEvents.Should().BeEmpty();
            result.Duration.Should().BeLessThanOrEqualTo(ResourceCheckRunner.DeadlineThreshold);

            result.Json.Should().NotBeNull();
            result.Xml.Should().NotBeNull();
            ExportEquivalence.AssertEquivalent(JsonDocument.Parse(result.Json!).RootElement, XDocument.Parse(Encoding.UTF8.GetString(result.Xml!)).Root!, "forecast_run");
        }
        finally
        {
            if (Directory.Exists(cache))
            {
                Directory.Delete(cache, recursive: true);
            }
        }
    }

    [TestMethod]
    [TestCategory("ResourceCheck")]
    [Timeout(1800000)]
    public async Task ResourceCheck_Generate_OnlyWhenExplicitlyEnabled()
    {
        if (ResourceCheckRunner.Mode(Environment.GetEnvironmentVariable) != ResourceCheckRunner.GenerateMode)
        {
            Assert.Inconclusive($"Resource check generate skipped: set {ResourceCheckRunner.Variable}={ResourceCheckRunner.GenerateMode} to run it.");
        }

        var directory = ResourceCheckRunner.Directory(Environment.GetEnvironmentVariable) ?? throw new InvalidOperationException($"{ResourceCheckRunner.DirectoryVariable} must name the run directory.");
        var path = await ResourceCheckRunner.GenerateAsync(directory, ResourceCheckRunner.RecordedPlacemarks, ResourceCheckRunner.RecordedElements, ResourceCheckRunner.RecordedSteps, CancellationToken.None);
        Console.WriteLine(path);
        new FileInfo(path).Length.Should().BeGreaterThan(1_000_000);
    }

    [TestMethod]
    [TestCategory("ResourceCheck")]
    [Timeout(1800000)]
    public async Task ResourceCheck_Measure_OnlyWhenExplicitlyEnabled()
    {
        if (ResourceCheckRunner.Mode(Environment.GetEnvironmentVariable) != ResourceCheckRunner.MeasureMode)
        {
            Assert.Inconclusive($"Resource check measure skipped: set {ResourceCheckRunner.Variable}={ResourceCheckRunner.MeasureMode} to run it; run it alone in a fresh test process: dotnet test --filter FullyQualifiedName~ResourceCheck_Measure_OnlyWhenExplicitlyEnabled");
        }

        var directory = ResourceCheckRunner.Directory(Environment.GetEnvironmentVariable) ?? throw new InvalidOperationException($"{ResourceCheckRunner.DirectoryVariable} must name the run directory.");
        var result = await ResourceCheckRunner.MeasureAsync(directory, TimeProvider.System, CancellationToken.None);
        await ResourceCheckRunner.WriteAsync(result, directory, CancellationToken.None);
        Console.WriteLine($"duration_ms={result.Duration.TotalMilliseconds:F0} peak_working_set_bytes={result.PeakWorkingSetBytes} within_deadline={result.WithinDeadline} within_peak={result.WithinPeak} start_peak_working_set_bytes={result.StartPeakWorkingSetBytes}");

        result.Placemarks.Should().Be(ResourceCheckRunner.RecordedPlacemarks);
        result.Elements.Should().Be(ResourceCheckRunner.RecordedElements);
        result.Steps.Should().Be(ResourceCheckRunner.RecordedSteps);

        result.ParseSteps.Should().Be(result.Steps);
        result.WithinDeadline.Should().BeTrue();
        result.WithinPeak.Should().BeTrue();
    }
}
