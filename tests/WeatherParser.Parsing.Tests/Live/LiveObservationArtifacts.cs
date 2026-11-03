using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Parsing.Tests.Live;

internal static class LiveObservationArtifacts
{
    public static async Task WriteAsync(LiveObservationResult result, LiveCheckSettings settings, string outputDirectory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(outputDirectory);

        await File.WriteAllBytesAsync(Path.Combine(outputDirectory, "observation.json"), result.Json, cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(outputDirectory, "observation.xml"), result.Xml, cancellationToken);

        var summary = new StringBuilder();
        summary.AppendLine($"budget={LiveCheckSettings.ObservationRequestBudget}");
        summary.AppendLine($"requests={result.Requests.Count}");
        foreach (var req in result.Requests)
        {
            var contentLength = req.ContentLength?.ToString(CultureInfo.InvariantCulture) ?? "-";
            summary.AppendLine($"{(int)req.StatusCode} {contentLength} {req.BytesRead} {req.Uri}");
        }
        summary.AppendLine($"station={settings.ObservationStationId}");
        summary.AppendLine($"source={result.Dataset.Source}");
        summary.AppendLine($"retrieved_at={result.Dataset.RetrievedAtWire}");
        summary.AppendLine($"measured_at={result.Dataset.MeasuredAtWire}");

        using var json = JsonDocument.Parse(result.Json);
        foreach (var measurement in json.RootElement.GetProperty("measurements").EnumerateObject())
        {
            var value = measurement.Value.GetProperty("value").GetRawText();
            var measuredAt = measurement.Value.GetProperty("measured_at").GetString();
            summary.AppendLine($"quantity {measurement.Name} {value} {measuredAt}");
        }

        summary.AppendLine($"schema_events={result.SchemaEvents.Count}");
        foreach (var notice in result.Notices)
        {
            summary.AppendLine($"notice {notice.Level} {notice.EventName} {notice.Message}");
        }

        var summaryPath = Path.Combine(outputDirectory, "summary.txt");
        await File.WriteAllTextAsync(summaryPath, summary.ToString(), cancellationToken);
    }
}
