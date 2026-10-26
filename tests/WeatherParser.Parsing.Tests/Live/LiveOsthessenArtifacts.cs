using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Parsing.Tests.Live;

internal static class LiveOsthessenArtifacts
{
    public static async Task WriteAsync(LiveOsthessenResult result, LiveCheckSettings settings, string outputDirectory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(outputDirectory);

        var summary = new StringBuilder();
        summary.AppendLine($"budget={LiveCheckSettings.OsthessenRequestBudget}");
        summary.AppendLine($"requests={result.Requests.Count}");
        foreach (var req in result.Requests)
        {
            var contentLength = req.ContentLength?.ToString(CultureInfo.InvariantCulture) ?? "-";
            summary.AppendLine($"{(int)req.StatusCode} {contentLength} {req.BytesRead} {req.Uri}");
        }
        summary.AppendLine($"pattern={settings.OsthessenPattern}");
        summary.AppendLine($"error={result.ErrorId ?? "none"}");
        summary.AppendLine($"matched={result.Datasets.Count + result.StationFailures.Count}");
        summary.AppendLine($"datasets={result.Datasets.Count}");
        for (var i = 0; i < result.Datasets.Count; i++)
        {
            using var json = JsonDocument.Parse(result.Json[i]);
            var measurements = string.Join(",", json.RootElement.GetProperty("measurements").EnumerateObject().Select(p => p.Name));
            summary.AppendLine($"dataset {result.Datasets[i].Source} {result.Datasets[i].MeasuredAtWire} {measurements} {Escape(result.Datasets[i].Location.Name)}");
        }
        summary.AppendLine($"failures={result.StationFailures.Count}");
        foreach (var failure in result.StationFailures)
        {
            summary.AppendLine($"failure {failure.ErrorId} {Escape(failure.StationName)}");
        }
        summary.AppendLine($"schema_events={result.SchemaEvents.Count}");
        foreach (var notice in result.Notices)
        {
            summary.AppendLine($"notice {notice.Level} {notice.EventName}");
        }

        var summaryPath = Path.Combine(outputDirectory, "summary.txt");
        await File.WriteAllTextAsync(summaryPath, summary.ToString(), cancellationToken);
    }

    private static string Escape(string text)
    {
        // Station names come from the page: one ASCII line, no markup and no line breaks, so a name cannot forge summary lines.
        var builder = new StringBuilder();
        foreach (var c in text)
        {
            if (c < (char)0x20 || c > (char)0x7E || c == '<' || c == '>' || c == '&' || c == '\\')
            {
                builder.Append("\\u" + ((int)c).ToString("X4", CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }
}
