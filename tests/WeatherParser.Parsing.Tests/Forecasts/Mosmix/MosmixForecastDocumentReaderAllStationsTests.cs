using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Forecasts.Mosmix;

namespace WeatherParser.Parsing.Tests.Forecasts.Mosmix;

[TestClass]
public class MosmixForecastDocumentReaderAllStationsTests
{
    private IMosmixForecastDocumentReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new MosmixForecastDocumentReader();
    }

    private async Task<MosmixForecastDocument> Read(string kml, string id = "10637", MosmixForecastReaderLimits? limits = null)
    {
        var reader = limits is null ? _subject : new MosmixForecastDocumentReader(limits);
        var kmz = MosmixForecastKmzBuilder.Kmz(kml, CompressionLevel.Optimal, MosmixForecastKmzBuilder.AllStationsMemberName);
        return await reader.ReadAllStationsAsync(new MemoryStream(kmz), id, CancellationToken.None);
    }

    private async Task ShouldFail(string kml, string errorId, string id = "10637", MosmixForecastReaderLimits? limits = null)
    {
        (await FluentActions.Invoking(() => Read(kml, id, limits))
            .Should().ThrowAsync<MosmixForecastFormatException>())
            .Where(e => e.ErrorId == errorId);
    }

    private static string Selected() => MosmixForecastKmzBuilder.Placemark("10637", "SELECTED", "8.6,50.05,111.0", MosmixForecastKmzBuilder.Forecast("TTT", "1.5 - 2.5"));

    private static string Other(string id) => MosmixForecastKmzBuilder.Placemark(id, "OTHER", "1.0,2.0,3.0", MosmixForecastKmzBuilder.Forecast("TTT", "9.0 9.0 9.0"));

    private static string Doc(params string[] placemarks) => MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(), placemarks);

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    public async Task ReadAllStationsAsync_PlacemarkFirstMiddleLast_Found(int position)
    {
        var others = new Queue<string>(new[] { Other("A111"), Other("B222") });
        var placemarks = new string[3];
        for (var i = 0; i < 3; i++)
        {
            placemarks[i] = i == position ? Selected() : others.Dequeue();
        }
        var kml = Doc(placemarks);

        var result = await Read(kml);

        result.MosmixId.Should().Be("10637");
        result.LocationName.Should().Be("SELECTED");
        result.Latitude.Should().Be(50.05m);
        result.Longitude.Should().Be(8.6m);
        result.AltitudeM.Should().Be(111.0m);
        result.Series.Should().ContainSingle()
            .Which.ElementName.Should().Be("TTT");
        result.Series[0].Values.Should().Equal("1.5", null, "2.5");
    }

    [TestMethod]
    public async Task ReadAllStationsAsync_PlacemarkAbsent_FailsIdUnknown()
    {
        var kml = Doc(Other("A111"), Other("B222"));

        await ShouldFail(kml, MosmixForecastErrorIds.IdUnknown);
    }

    [TestMethod]
    [DataRow("before")]
    [DataRow("after")]
    [DataRow("selected")]
    public async Task ReadAllStationsAsync_DuplicateId_FailsFormatInvalid(string scenario)
    {
        var selected = Selected();
        var other1 = Other("A111");
        var other2 = Other("A111");

        var kml = scenario switch
        {
            "before" => Doc(other1, other2, selected),
            "after" => Doc(selected, other1, other2),
            "selected" => Doc(selected, other1, selected),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadAllStationsAsync_TruncatedArchive_FailsFormatInvalid()
    {
        var kml = Doc(Selected(), Other("A111"));
        var kmz = MosmixForecastKmzBuilder.Kmz(kml, CompressionLevel.Optimal, MosmixForecastKmzBuilder.AllStationsMemberName);
        var truncated = new byte[kmz.Length - 40];
        Array.Copy(kmz, 0, truncated, 0, truncated.Length);

        await FluentActions.Awaiting(() => _subject.ReadAllStationsAsync(new MemoryStream(truncated), "10637", CancellationToken.None))
            .Should().ThrowAsync<MosmixForecastFormatException>().Where(e => e.ErrorId == MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    [DataRow("elementName")]
    [DataRow("token")]
    [DataRow("tokenCount")]
    [DataRow("name")]
    [DataRow("duplicateElement")]
    public async Task ReadAllStationsAsync_OtherPlacemarkViolatesBounds_FailsFormatInvalid(string scenario)
    {
        var selected = Selected();
        var other = scenario switch
        {
            "elementName" => MosmixForecastKmzBuilder.Placemark("B222", "OTHER", "1.0,2.0,3.0", MosmixForecastKmzBuilder.Forecast("ABCDEFGHIJKLMNOPQ", "1.0 1.0 1.0")),
            "token" => MosmixForecastKmzBuilder.Placemark("B222", "OTHER", "1.0,2.0,3.0", MosmixForecastKmzBuilder.Forecast("TTT", "1e5 1.0 1.0")),
            "tokenCount" => MosmixForecastKmzBuilder.Placemark("B222", "OTHER", "1.0,2.0,3.0", MosmixForecastKmzBuilder.Forecast("TTT", "1.0 1.0")),
            "name" => MosmixForecastKmzBuilder.Placemark("ab12", "OTHER", "1.0,2.0,3.0", MosmixForecastKmzBuilder.Forecast("TTT", "1.0 1.0 1.0")),
            "duplicateElement" => MosmixForecastKmzBuilder.Placemark("B222", "OTHER", "1.0,2.0,3.0", MosmixForecastKmzBuilder.Forecast("TTT", "1.0 1.0 1.0"), MosmixForecastKmzBuilder.Forecast("TTT", "1.0 1.0 1.0")),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

        var kml = Doc(selected, other);

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadAllStationsAsync_OtherPlacemarkWithoutPointOrDescriptionText_Accepted()
    {
        var kml = Doc(Selected(), MosmixForecastKmzBuilder.Placemark("B222", "", null));

        var result = await Read(kml);

        result.MosmixId.Should().Be("10637");
    }

    [TestMethod]
    public async Task ReadAllStationsAsync_PlacemarksAtBound_Accepted()
    {
        var limits = MosmixForecastReaderLimits.Contract with { MaxPlacemarks = 3 };
        var kml = Doc(Selected(), Other("A111"), Other("B222"));

        var result = await Read(kml, limits: limits);

        result.MosmixId.Should().Be("10637");
    }

    [TestMethod]
    public async Task ReadAllStationsAsync_PlacemarksOneAboveBound_FailsFormatInvalid()
    {
        var limits = MosmixForecastReaderLimits.Contract with { MaxPlacemarks = 3 };
        var kml = Doc(Selected(), Other("A111"), Other("B222"), Other("C333"));

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid, limits: limits);
    }

    [TestMethod]
    [DataRow("coordinates", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("description", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("issueTimeMissing", MosmixForecastErrorIds.IssueTimeMissing)]
    [DataRow("fractionalIssueTime", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("whitespaceDescription", MosmixForecastErrorIds.FormatInvalid)]
    public async Task ReadAllStationsAsync_RulesOfSingleFormApplyToSelected(string scenario, string errorId)
    {
        var kml = scenario switch
        {
            "coordinates" => Doc(MosmixForecastKmzBuilder.Placemark("10637", "SELECTED", "abc", MosmixForecastKmzBuilder.Forecast("TTT", "1.5 - 2.5"))),
            "description" => Doc(MosmixForecastKmzBuilder.Placemark("10637", "", "8.6,50.05,111.0", MosmixForecastKmzBuilder.Forecast("TTT", "1.5 - 2.5"))),
            "issueTimeMissing" => MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(issueTime: null), Selected()),
            "fractionalIssueTime" => MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(issueTime: "2026-10-26T15:00:00.500Z"), Selected()),
            "whitespaceDescription" => Doc(MosmixForecastKmzBuilder.Placemark("10637", "   ", "8.6,50.05,111.0", MosmixForecastKmzBuilder.Forecast("TTT", "1.5 - 2.5"))),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

        await ShouldFail(kml, errorId);
    }

    [TestMethod]
    [DoNotParallelize]
    [Timeout(120000)]
    public async Task ReadAllStationsAsync_OtherPlacemarksNotRetained()
    {
        var kmz = MosmixForecastKmzBuilder.LargeAllStationsKmz(1500, 750, "10637", 240, 40);
        using var archive = new ZipArchive(new MemoryStream(kmz));
        var memberLength = archive.Entries[0].Length;
        memberLength.Should().BeGreaterThan(80 * 1024 * 1024);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var baseline = GC.GetTotalMemory(true);

        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stop = new CancellationTokenSource();
        long maxSample = 0;
        var samples = 0;
        var sampler = Task.Run(() =>
        {
            started.SetResult();
            while (!stop.IsCancellationRequested)
            {
                var current = GC.GetTotalMemory(true);
                if (current > maxSample)
                {
                    maxSample = current;
                }

                samples++;
                Thread.Sleep(20);
            }
        });
        await started.Task;
        MosmixForecastDocument result;
        try
        {
            result = await _subject.ReadAllStationsAsync(new MemoryStream(kmz), "10637", CancellationToken.None);
        }
        finally
        {
            stop.Cancel();
            await sampler;
        }

        result.MosmixId.Should().Be("10637");
        result.Timeline.Should().HaveCount(240);
        result.Series.Should().HaveCount(40);
        samples.Should().BeGreaterThanOrEqualTo(5, "the heap must be sampled while the member is consumed");
        (maxSample - baseline).Should().BeLessThan(memberLength / 4);
    }
}
