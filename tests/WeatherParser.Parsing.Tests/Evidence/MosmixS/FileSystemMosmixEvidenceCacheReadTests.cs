using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Evidence.MosmixS;

[TestClass]
public sealed class FileSystemMosmixEvidenceCacheReadTests
{
    private string _directory = null!;
    private IMosmixEvidenceCache _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "wp-mosmix-cache-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _subject = MosmixEvidenceIndexTestData.Cache(_directory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    private void Write(JsonNode json) => MosmixEvidenceIndexTestData.WriteRaw(_directory, json.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));

    private Task<MosmixSEvidenceIndex?> Read() => _subject.ReadAsync(CancellationToken.None);

    [TestMethod]
    public async Task Read_ReturnsIndex_WhenValidIndexPresent()
    {
        Write(MosmixEvidenceIndexTestData.ValidJson());
        var result = await Read();
        result.Should().NotBeNull();
        result!.SourceUri.Should().Be(MosmixEvidenceIndexTestData.SourceUri);
        result.DownloadedAtUtc.Should().Be(MosmixEvidenceIndexTestData.DownloadedAt);
        result.IssuedAtUtc.Should().Be(MosmixEvidenceIndexTestData.IssuedAt);
        result.FileSha256.Should().Be(MosmixEvidenceIndexTestData.FileSha256);
        result.FileSize.Should().Be(39321600);
        result.Locations["X123A"].Should().Equal("FF", "TTT");
        result.Locations["01234"].Should().Equal("TTT");
        result.Locations.Count.Should().Be(2);
    }

    [TestMethod]
    public async Task Read_ReturnsNull_WhenIndexFileMissing()
    {
        var result = await Read();
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Read_ReturnsNull_WhenIndexFileUnreadable()
    {
        Directory.CreateDirectory(MosmixEvidenceIndexTestData.IndexPath(_directory));
        var result = await Read();
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Read_ReturnsNull_WhenIndexJsonInvalid()
    {
        MosmixEvidenceIndexTestData.WriteRaw(_directory, "{ not json");
        var result = await Read();
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Read_ReturnsNull_WhenIndexFormatVersionWrong()
    {
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json["format_version"] = 2;
            Write(json);
            (await Read()).Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json["format_version"] = "1";
            Write(json);
            (await Read()).Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json.Remove("format_version");
            Write(json);
            (await Read()).Should().BeNull();
        }
    }

    [TestMethod]
    public async Task Read_ReturnsNull_WhenIndexSourceKeyMismatch()
    {
        var json = MosmixEvidenceIndexTestData.ValidJson();
        json["source_key"] = MosmixEvidenceIndexTestData.KeyOf("https://example.org/mosmix/OTHER.kmz");
        Write(json);
        var result = await Read();
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Read_ReturnsNull_WhenIndexContentViolatesSC20Rules()
    {
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json.Remove("source_key");
            Write(json);
            (await Read()).Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json.Remove("source_uri");
            Write(json);
            (await Read()).Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json.Remove("downloaded_at");
            Write(json);
            (await Read()).Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json.Remove("issued_at");
            Write(json);
            (await Read()).Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json.Remove("file_sha256");
            Write(json);
            (await Read()).Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json.Remove("file_size");
            Write(json);
            (await Read()).Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json.Remove("locations");
            Write(json);
            (await Read()).Should().BeNull();
        }
    }

    [TestMethod]
    public async Task Read_ReturnsNull_WhenMemberValuesAreInvalid()
    {
        async Task AssertNull(Action<JsonObject> change)
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            change(json);
            Write(json);
            (await Read()).Should().BeNull();
        }

        await AssertNull(j => j["file_sha256"] = MosmixEvidenceIndexTestData.FileSha256.ToUpperInvariant());
        await AssertNull(j => j["file_sha256"] = MosmixEvidenceIndexTestData.FileSha256[..63]);
        await AssertNull(j => j["file_size"] = -1);
        await AssertNull(j => j["file_size"] = 1.5);
        await AssertNull(j => j["source_uri"] = "");
        await AssertNull(j => j["locations"] = new JsonArray());
        await AssertNull(j => j["locations"]!["X123A"] = "TTT");
        await AssertNull(j => j["locations"]!["X123A"] = new JsonArray(1));
    }

    [TestMethod]
    public async Task Read_AcceptsIndexAtSizeLimit8MiB()
    {
        var text = MosmixEvidenceIndexTestData.ValidJson().ToJsonString();
        var padding = new string(' ', 8388608 - Encoding.UTF8.GetByteCount(text));
        MosmixEvidenceIndexTestData.WriteRaw(_directory, text + padding);
        var result = await Read();
        result.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Read_RejectsIndexAboveSizeLimit8MiB()
    {
        var text = MosmixEvidenceIndexTestData.ValidJson().ToJsonString();
        var padding = new string(' ', 8388609 - Encoding.UTF8.GetByteCount(text));
        MosmixEvidenceIndexTestData.WriteRaw(_directory, text + padding);
        var result = await Read();
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Read_AcceptsLocationCountAtLimit20000()
    {
        var locations = MosmixEvidenceIndexTestData.ManyLocations(20000);
        var json = MosmixEvidenceIndexTestData.ValidJson();
        var locs = new JsonObject();
        foreach (var kvp in locations)
        {
            locs[kvp.Key] = new JsonArray(kvp.Value.Select(n => (JsonNode?)JsonValue.Create(n)).ToArray());
        }
        json["locations"] = locs;
        Write(json);
        var result = await Read();
        result.Should().NotBeNull();
        result!.Locations.Count.Should().Be(20000);
    }

    [TestMethod]
    public async Task Read_RejectsLocationCountAboveLimit20000()
    {
        var locations = MosmixEvidenceIndexTestData.ManyLocations(20001);
        var json = MosmixEvidenceIndexTestData.ValidJson();
        var locs = new JsonObject();
        foreach (var kvp in locations)
        {
            locs[kvp.Key] = new JsonArray(kvp.Value.Select(n => (JsonNode?)JsonValue.Create(n)).ToArray());
        }
        json["locations"] = locs;
        Write(json);
        var result = await Read();
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Read_AcceptsElementNameCountAtLimit200()
    {
        var json = MosmixEvidenceIndexTestData.ValidJson();
        var names = new JsonArray();
        for (int i = 0; i < 200; i++)
        {
            names.Add("E" + i.ToString("D3"));
        }
        json["locations"]!["X123A"] = names;
        Write(json);
        var result = await Read();
        result.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Read_RejectsElementNameCountAboveLimit200()
    {
        var json = MosmixEvidenceIndexTestData.ValidJson();
        var names = new JsonArray();
        for (int i = 0; i < 201; i++)
        {
            names.Add("E" + i.ToString("D3"));
        }
        json["locations"]!["X123A"] = names;
        Write(json);
        var result = await Read();
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Read_AcceptsNestingDepthAtLimit8()
    {
        var text = MosmixEvidenceIndexTestData.ValidJson().ToJsonString();
        text = text[..^1] + @",""extra"":[[[[[[[0]]]]]]]}";
        MosmixEvidenceIndexTestData.WriteRaw(_directory, text);
        var result = await Read();
        result.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Read_RejectsNestingDepthAboveLimit8()
    {
        var text = MosmixEvidenceIndexTestData.ValidJson().ToJsonString();
        text = text[..^1] + @",""extra"":[[[[[[[[0]]]]]]]]}";
        MosmixEvidenceIndexTestData.WriteRaw(_directory, text);
        var result = await Read();
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task Read_AcceptsValidTimestamps()
    {
        var json = MosmixEvidenceIndexTestData.ValidJson();
        json["downloaded_at"] = "2026-12-31T23:59:59";
        Write(json);
        var result = await Read();
        result.Should().NotBeNull();
        result!.DownloadedAtUtc.Should().Be(new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc));
    }

    [TestMethod]
    public async Task Read_RejectsInvalidTimestamps()
    {
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json["downloaded_at"] = "2026-10-21T04:12:07Z";
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json["downloaded_at"] = "2026-10-21T04:12:07.5";
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json["downloaded_at"] = "2026-02-30T04:12:07";
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json["downloaded_at"] = "2026-10-21 04:12:07";
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json["downloaded_at"] = "2026-10-21T04:12:07+02:00";
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json["downloaded_at"] = "";
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }

        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json["issued_at"] = "2026-10-21T04:12:07Z";
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json["issued_at"] = "2026-10-21T04:12:07.5";
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json["issued_at"] = "2026-02-30T04:12:07";
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json["issued_at"] = "2026-10-21 04:12:07";
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json["issued_at"] = "2026-10-21T04:12:07+02:00";
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            json["issued_at"] = "";
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
    }

    [TestMethod]
    public async Task Read_AcceptsValidLocationIDs()
    {
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            var locs = new JsonObject
            {
                ["A051"] = new JsonArray("TTT")
            };
            json["locations"] = locs;
            Write(json);
            var result = await Read();
            result.Should().NotBeNull();
            result!.Locations.Count.Should().Be(1);
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            var locs = new JsonObject
            {
                ["01234"] = new JsonArray("TTT")
            };
            json["locations"] = locs;
            Write(json);
            var result = await Read();
            result.Should().NotBeNull();
            result!.Locations.Count.Should().Be(1);
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            var locs = new JsonObject
            {
                ["X123A"] = new JsonArray("TTT")
            };
            json["locations"] = locs;
            Write(json);
            var result = await Read();
            result.Should().NotBeNull();
            result!.Locations.Count.Should().Be(1);
        }
    }

    [TestMethod]
    public async Task Read_RejectsInvalidLocationIDs()
    {
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            var locs = new JsonObject
            {
                ["a051"] = new JsonArray("TTT")
            };
            json["locations"] = locs;
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            var locs = new JsonObject
            {
                ["ABC"] = new JsonArray("TTT")
            };
            json["locations"] = locs;
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            var locs = new JsonObject
            {
                ["ABCDEF"] = new JsonArray("TTT")
            };
            json["locations"] = locs;
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            var locs = new JsonObject
            {
                ["A-12"] = new JsonArray("TTT")
            };
            json["locations"] = locs;
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            var locs = new JsonObject
            {
                [""] = new JsonArray("TTT")
            };
            json["locations"] = locs;
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
    }

    [TestMethod]
    public async Task Read_AcceptsValidElementNames()
    {
        var json = MosmixEvidenceIndexTestData.ValidJson();
        var names = new JsonArray("TTT", "RR1c", "wwM6", "E_TTT", "ABCDEFGHIJKLMNOP");
        json["locations"]!["X123A"] = names;
        Write(json);
        var result = await Read();
        result.Should().NotBeNull();
        result!.Locations["X123A"].Should().Equal("TTT", "RR1c", "wwM6", "E_TTT", "ABCDEFGHIJKLMNOP");
    }

    [TestMethod]
    public async Task Read_RejectsInvalidElementNames()
    {
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            var names = new JsonArray("");
            json["locations"]!["X123A"] = names;
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            var names = new JsonArray("ABCDEFGHIJKLMNOPQ");
            json["locations"]!["X123A"] = names;
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            var names = new JsonArray("T T");
            json["locations"]!["X123A"] = names;
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            var names = new JsonArray("T-T");
            json["locations"]!["X123A"] = names;
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
        {
            var json = MosmixEvidenceIndexTestData.ValidJson();
            var names = new JsonArray("T\u00C4");
            json["locations"]!["X123A"] = names;
            Write(json);
            var result = await Read();
            result.Should().BeNull();
        }
    }

    [TestMethod]
    public async Task Read_ReturnsNull_WhenLocationIdOrElementNameIsDuplicated()
    {
        var text = MosmixEvidenceIndexTestData.ValidJson().ToJsonString();
        text.Should().Contain(@"""locations"":{""X123A"":[""FF"",""TTT""],""01234"":[""TTT""]}");
        MosmixEvidenceIndexTestData.WriteRaw(_directory, text.Replace(@"""locations"":{""X123A"":[""FF"",""TTT""],""01234"":[""TTT""]}", @"""locations"":{""X123A"":[""TTT""],""X123A"":[""FF""]}"));
        (await Read()).Should().BeNull();

        var json = MosmixEvidenceIndexTestData.ValidJson();
        json["locations"]!["X123A"] = new JsonArray("TTT", "TTT");
        Write(json);
        (await Read()).Should().BeNull();
    }

    [TestMethod]
    public async Task Constructor_InvalidOptions_ThrowsSettingsInvalid()
    {
        var options = MosmixEvidenceIndexTestData.Options(_directory);
        options.MosmixSEvidenceMaxAge = null;
        var act = () => new FileSystemMosmixEvidenceCache(options, new MosmixCatalogEndpointsValidator(new ParserEndpointValidator(new EndpointUriValidator())));
        act.Should().Throw<EvidenceCacheException>().Which.ErrorId.Should().Be(EvidenceCacheException.SettingsInvalidErrorId);
    }

    [TestMethod]
    public async Task AddWeatherParsing_ResolvesMosmixEvidenceCache()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var cache = provider.GetRequiredService<IMosmixEvidenceCache>();
        cache.Should().BeOfType<FileSystemMosmixEvidenceCache>();
    }
}
