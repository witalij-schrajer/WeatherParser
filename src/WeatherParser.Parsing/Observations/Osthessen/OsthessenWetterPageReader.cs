using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Osthessen;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Default implementation of <see cref="IOsthessenWetterPageReader"/> over AngleSharp; stateless.
/// </summary>
public sealed class OsthessenWetterPageReader : IOsthessenWetterPageReader
{
    private const int StatusCell = 0, NameCell = 1, TemperatureCell = 2, HumidityCell = 7;
    private const int WindNameCell = 1, WindMean10Cell = 4;

    /// <inheritdoc />
    public async Task<OsthessenWetterPage> ReadAsync(Stream html, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(html);
        cancellationToken.ThrowIfCancellationRequested();

        // The live HTTP body is read only asynchronously so that cancellation and the deadline can interrupt it (contract OW-02, P6.04).
        using var buffer = new MemoryStream();
        await html.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;

        var parser = new HtmlParser(); // The default configuration has no scripting and no resource loader.
        var document = parser.ParseDocument(buffer);
        cancellationToken.ThrowIfCancellationRequested();

        var overviewTable = FindTable(document, OverviewLabels());

        if (overviewTable is null)
        {
            throw new OsthessenWetterException(OsthessenWetterException.FormatChangedErrorId, "The overview table with the documented header was not found; the page deviates from the supported revision (contract OW-01, OW-11).");
        }

        var stations = new List<OsthessenWetterStationRow>();
        foreach (var tr in overviewTable.QuerySelectorAll("tr").Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cells = tr.Children.Where(child => child.LocalName == "td").ToArray();
            
            if (cells.Length != OverviewLabels().Length)
                throw new OsthessenWetterException(OsthessenWetterException.FormatChangedErrorId, "A station row of the overview table does not have the documented cells; the page deviates from the supported revision (contract OW-01, OW-11).");

            var statusImage = cells[StatusCell].QuerySelector("img")?.GetAttribute("title");
            var status = ParseStatus(statusImage);
            if (status is null)
                throw new OsthessenWetterException(OsthessenWetterException.FormatChangedErrorId, "A station row has no documented status image title; the page deviates from the supported revision (contract OW-01, OW-12).");

            var name = cells[NameCell].QuerySelector("font[size='2em']")?.TextContent.Trim();
            if (string.IsNullOrEmpty(name))
                throw new OsthessenWetterException(OsthessenWetterException.FormatChangedErrorId, "A station row of the overview table has no station name; the page deviates from the supported revision (contract OW-01, OW-11).");

            var overviewTimeText = cells[NameCell].QuerySelector("font[size='1']")?.TextContent.Trim() ?? "";
            
            // The Temp.2m cell holds the temperature in font size 3 and, after a line break, the change in degC/h; only the temperature is read (contract A7).
            var temperatureText = cells[TemperatureCell].QuerySelector("font[size='3']")?.TextContent.Trim() ?? "";
            
            var humidityText = cells[HumidityCell].TextContent.Trim();

            stations.Add(new OsthessenWetterStationRow(
                Name: name,
                Status: status.Value,
                OverviewTimeText: overviewTimeText,
                TemperatureText: temperatureText,
                HumidityText: humidityText,
                WindTimeText: null,
                WindMeanText: null));
        }

        var diagnostics = new List<OsthessenWetterPageDiagnostic>();
        var windTable = FindTable(document, WindLabels());
        if (windTable is null)
        {
            diagnostics.Add(new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindTableMissing, null));
            return new OsthessenWetterPage(stations, diagnostics);
        }

        // Collect candidates for joining
        var windCandidates = new List<(string JoinKey, string WindTimeText, string WindMeanText)>();
        var deviatingKeys = new HashSet<string>(StringComparer.Ordinal);
        
        // A deviating wind row is ignored and its station gets the unmatched notice; the other rows keep their wind values (contract OW-11 amendment of 2026-10-25).
        foreach (var tr in windTable.QuerySelectorAll("tr").Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cells = tr.Children.Where(child => child.LocalName == "td").ToArray();
            
            string? name = null;
            if (cells.Length > WindNameCell)
            {
                var nameElement = cells[WindNameCell].QuerySelector("font[size='2em']");
                name = nameElement?.TextContent.Trim();
            }

            if (cells.Length != WindLabels().Length || string.IsNullOrEmpty(name) || ParseStatus(cells[StatusCell].QuerySelector("img")?.GetAttribute("title")) is null)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    deviatingKeys.Add(JoinKey(name));
                }
                continue;
            }

            var joinKey = JoinKey(name);
            var windTimeText = cells[WindNameCell].QuerySelector("font[size='1']")?.TextContent.Trim() ?? "";
            var windMeanText = cells[WindMean10Cell].TextContent.Trim();

            windCandidates.Add((joinKey, windTimeText, windMeanText));
        }

        // Join candidates to overview rows
        // One lookup instead of a scan per station keeps the join linear in the row count.
        var candidatesByKey = windCandidates.ToLookup(c => c.JoinKey, StringComparer.Ordinal);
        var joinedStations = new List<OsthessenWetterStationRow>();
        foreach (var station in stations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = JoinKey(station.Name);
            // Two candidates already make the key ambiguous (contract OW-05).
            var matchingCandidates = candidatesByKey[key].Take(2).ToList();
            if (matchingCandidates.Count == 1 && !deviatingKeys.Contains(key))
            {
                var candidate = matchingCandidates[0];
                joinedStations.Add(station with
                {
                    WindTimeText = candidate.WindTimeText,
                    WindMeanText = candidate.WindMeanText
                });
            }
            else
            {
                joinedStations.Add(station);
                diagnostics.Add(new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindRowUnmatched, station.Name));
            }
        }

        return new OsthessenWetterPage(joinedStations, diagnostics);
    }

    private static IElement? FindTable(IDocument document, string[] labels)
    {
        return document.QuerySelectorAll("table")
            .Where(table => table.QuerySelector("tr") is { } firstRow && 
                            firstRow.Children
                                .Select(cell => string.Concat(cell.TextContent.Where(c => !char.IsWhiteSpace(c))))
                                .SequenceEqual(labels))
            .FirstOrDefault();
    }

    private static string[] WindLabels() => new[] { "", "WetterstationletzteDaten", "Spitzen-b\u00F6eheute", "Spitzen-b\u00F6everg.10Min.", "Wind\u00D810Min.", "Wind\u00D860Min." };

    private static string JoinKey(string name)
    {
        return name.Normalize(NormalizationForm.FormC).ToLowerInvariant().Replace("\u00df", "ss").TrimEnd().TrimEnd(';').TrimEnd();
    }

    private static string[] OverviewLabels() => new[] { "", "Wetterstation", "Temp.2m\u00C4nderung", "Temp.-Trend", "Tmax/Tmin", "Spitzen-b\u00F6e", "Nieder-schlag", "Luft-feuchte" };

    private static OsthessenWetterStationStatus? ParseStatus(string? title)
    {
        return title switch
        {
            "Station online" => OsthessenWetterStationStatus.Online,
            "Station offline" => OsthessenWetterStationStatus.Offline,
            _ => null
        };
    }
}
