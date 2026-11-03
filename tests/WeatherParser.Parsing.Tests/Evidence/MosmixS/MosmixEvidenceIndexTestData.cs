using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using FluentAssertions;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Evidence.MosmixS;

internal static class MosmixEvidenceIndexTestData
{
    public const string SourceUri = "https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz";
    public static readonly DateTime DownloadedAt = new(2026, 10, 21, 4, 12, 7, DateTimeKind.Utc);
    public static readonly DateTime IssuedAt = new(2026, 10, 21, 3, 0, 0, DateTimeKind.Utc);
    public const string FileSha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    public static string KeyOf(string uri) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(uri)));

    public static string FileName(string uri) => "mosmix_s_evidence_" + KeyOf(uri)[..16] + ".json";

    public static MosmixCatalogEndpoints Options(string directory) => new()
    {
        CatalogUri = new Uri("https://example.org/mosmix/stations.cfg"),
        MosmixLSingleStationsDirectory = new Uri("https://example.org/mosmix/single_stations/"),
        MosmixSAllStationsUri = new Uri(SourceUri),
        EvidenceCacheDirectory = directory,
        MosmixSEvidenceMaxAge = TimeSpan.FromHours(6)
    };

    public static IMosmixEvidenceCache Cache(string directory) => new FileSystemMosmixEvidenceCache(
        Options(directory),
        new MosmixCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator())));

    public static MosmixSEvidenceIndex Index(IReadOnlyDictionary<string, IReadOnlyList<string>>? locations = null) => new(
        SourceUri,
        DownloadedAt,
        IssuedAt,
        FileSha256,
        39321600,
        locations ?? new Dictionary<string, IReadOnlyList<string>>
        {
            ["X123A"] = new[] { "FF", "TTT" },
            ["01234"] = new[] { "TTT" }
        });

    public static Dictionary<string, IReadOnlyList<string>> ManyLocations(int count, int namesPerLocation = 1)
    {
        var locations = new Dictionary<string, IReadOnlyList<string>>();
        for (int i = 0; i < count; i++)
        {
            var id = i.ToString("D5");
            var names = new List<string>();
            for (int j = 0; j < namesPerLocation; j++)
            {
                names.Add("E" + j.ToString("D3"));
            }
            locations[id] = names;
        }
        return locations;
    }

    public static JsonObject ValidJson()
    {
        var json = new JsonObject
        {
            ["format_version"] = 1,
            ["source_key"] = KeyOf(SourceUri),
            ["source_uri"] = SourceUri,
            ["downloaded_at"] = "2026-10-21T04:12:07",
            ["issued_at"] = "2026-10-21T03:00:00",
            ["file_sha256"] = FileSha256,
            ["file_size"] = 39321600
        };
        var locations = new JsonObject();
        locations["X123A"] = new JsonArray("FF", "TTT");
        locations["01234"] = new JsonArray("TTT");
        json["locations"] = locations;
        return json;
    }

    public static string IndexPath(string directory) => Path.Combine(directory, FileName(SourceUri));

    public static void WriteRaw(string directory, string json) => File.WriteAllText(IndexPath(directory), json, new UTF8Encoding(false));
}
