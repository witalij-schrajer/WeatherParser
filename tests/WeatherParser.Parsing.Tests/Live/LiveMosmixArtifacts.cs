using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;

namespace WeatherParser.Parsing.Tests.Live;

internal static class LiveMosmixArtifacts
{
    public static async Task WriteAsync(LiveMosmixResult result, LiveCheckSettings settings, string outputDirectory, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(outputDirectory);

        outputDirectory = OutputDirectoryGuard.EnsureAllowed(outputDirectory);

        var summary = new StringBuilder();
        summary.AppendLine($"budget={LiveCheckSettings.MosmixRequestBudget}");
        summary.AppendLine($"requests={result.Requests.Count}");
        foreach (var req in result.Requests)
        {
            var contentLength = req.ContentLength?.ToString(CultureInfo.InvariantCulture) ?? "-";
            summary.AppendLine($"{(int)req.StatusCode} {contentLength} {req.BytesRead} {req.Uri}");
        }
        summary.AppendLine($"product={(result.Product == MosmixProduct.MosmixS ? "MOSMIX_S" : "MOSMIX_L")}");
        summary.AppendLine($"id={result.MosmixId}");
        summary.AppendLine($"error={result.ErrorId ?? "none"}");
        if (result.Run is not null)
        {
            summary.AppendLine($"issued_at={result.Run.IssuedAtWire}");
            summary.AppendLine($"retrieved_at={result.Run.RetrievedAtWire}");
            summary.AppendLine($"steps={result.Run.Forecasts.Count}");
            summary.AppendLine($"freshness={(result.Freshness == MosmixRunFreshness.Fresh ? "Fresh" : "Stale")}");
            summary.AppendLine($"run_age={result.RunAge?.ToString("c", CultureInfo.InvariantCulture) ?? "-"}");
            summary.AppendLine($"location={Escape(result.Run.Location.Name)}");
            summary.AppendLine($"not_exported={result.NotExportedElements.Count}: {string.Join(",", result.NotExportedElements)}");
            summary.AppendLine($"absent={result.AbsentElements.Count}: {string.Join(",", result.AbsentElements)}");
            summary.AppendLine($"omitted={result.OmittedValues.Count}");
            var groups = result.OmittedValues.GroupBy(v => (v.Kind, v.Reason));
            foreach (var group in groups)
            {
                summary.AppendLine($"omitted {group.Key.Kind} {group.Key.Reason} {group.Count()}");
            }
        }
        summary.AppendLine($"schema_events={result.SchemaEvents.Count}");
        foreach (var notice in result.Notices)
        {
            summary.AppendLine($"notice {notice.Level} {notice.EventName}");
        }
        summary.AppendLine($"duration_ms={(int)result.Duration.TotalMilliseconds}");
        summary.AppendLine($"peak_working_set_bytes={result.PeakWorkingSetBytes}");

        var summaryPath = Path.Combine(outputDirectory, "summary.txt");
        await File.WriteAllTextAsync(summaryPath, summary.ToString(), cancellationToken);

        if (result.Json is not null)
        {
            var jsonPath = Path.Combine(outputDirectory, "forecast.json");
            await File.WriteAllBytesAsync(jsonPath, result.Json, cancellationToken);
        }

        if (result.Xml is not null)
        {
            var xmlPath = Path.Combine(outputDirectory, "forecast.xml");
            await File.WriteAllBytesAsync(xmlPath, result.Xml, cancellationToken);
        }
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
