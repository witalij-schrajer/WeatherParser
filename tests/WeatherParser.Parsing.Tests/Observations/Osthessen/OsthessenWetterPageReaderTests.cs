using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Observations.Osthessen;

[TestClass]
public sealed class OsthessenWetterPageReaderTests
{
    private OsthessenWetterPageReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new OsthessenWetterPageReader();
    }

    [TestMethod]
    public async Task ReadAsync_NullStream_ThrowsArgumentNullException()
    {
        await FluentActions.Awaiting(() => _subject.ReadAsync(null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    public async Task ReadAsync_CancelledToken_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await FluentActions.Awaiting(() => _subject.ReadAsync(OsthessenWetterHtml.ToStream(OsthessenWetterHtml.DefaultPage()), cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task ReadAsync_UsesOnlyAsynchronousReads()
    {
        var stream = new OsthessenWetterHtml.AsyncOnlyStream(Encoding.UTF8.GetBytes(OsthessenWetterHtml.DefaultPage()));
        var result = await _subject.ReadAsync(stream, CancellationToken.None);
        result.Stations.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_PendingReadCancelled_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        var task = _subject.ReadAsync(new OsthessenWetterHtml.PendingStream(), cts.Token);
        task.IsCompleted.Should().BeFalse();
        cts.Cancel();

        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task ReadAsync_ExternalResourcesAndScript_NotLoadedOrRun()
    {
        var result = await _subject.ReadAsync(OsthessenWetterHtml.ToStream(OsthessenWetterHtml.DefaultPage()), CancellationToken.None);
        result.Stations.Should().NotContain(s => s.Name == "Injected");
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesOsthessenWetterPageReader()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IOsthessenWetterPageReader>().Should().BeOfType<OsthessenWetterPageReader>();
    }

    private async Task<OsthessenWetterPage> Read(string html) => await _subject.ReadAsync(OsthessenWetterHtml.ToStream(html), CancellationToken.None);

    private async Task<OsthessenWetterException> ReadFails(string html) => (await FluentActions.Awaiting(() => Read(html)).Should().ThrowAsync<OsthessenWetterException>()).Which;

    private static string Overview(params string[] rows) => OsthessenWetterHtml.Table(OsthessenWetterHtml.OverviewHeader(), rows);

    private static string Wind(params string[] rows) => OsthessenWetterHtml.Table(OsthessenWetterHtml.WindHeader(), rows);

    [TestMethod]
    public async Task ReadAsync_ValidPage_ReturnsOverviewRowsInPageOrder()
    {
        var result = await Read(OsthessenWetterHtml.DefaultPage());
        result.Stations.Select(s => s.Name).Should().Equal("Ro\u00dfbach", "M\u00fcs-Hohh\u00fctte", "Langenbieber");
    }

    [TestMethod]
    public async Task ReadAsync_Row_StatusNameTimeAndCells()
    {
        var result = await Read(OsthessenWetterHtml.DefaultPage());
        var station = result.Stations[0];
        station.Status.Should().Be(OsthessenWetterStationStatus.Online);
        station.OverviewTimeText.Should().Be("09:31 Uhr");
        station.TemperatureText.Should().Be("10.3\u00b0C");
        station.HumidityText.Should().Be("93%");
    }

    [TestMethod]
    public async Task ReadAsync_OfflineRow_StatusOfflineAndEmptyCells()
    {
        var result = await Read(OsthessenWetterHtml.DefaultPage());
        var station = result.Stations[2];
        station.Status.Should().Be(OsthessenWetterStationStatus.Offline);
        station.OverviewTimeText.Should().Be("");
        station.TemperatureText.Should().Be("");
        station.HumidityText.Should().Be("");
    }

    [TestMethod]
    public async Task ReadAsync_ZeroHumidity_KeptAsText()
    {
        var result = await Read(OsthessenWetterHtml.DefaultPage());
        result.Stations[1].HumidityText.Should().Be("0%");
    }

    [TestMethod]
    public async Task ReadAsync_TemperatureCell_PrimaryValueOnly()
    {
        var result = await Read(OsthessenWetterHtml.DefaultPage());
        result.Stations[1].TemperatureText.Should().Be("-4.2\u00b0C");
    }

    [TestMethod]
    public async Task ReadAsync_TemperatureMissingChangePresent_TemperatureEmpty()
    {
        var result = await Read(OsthessenWetterHtml.Page(Overview(OsthessenWetterHtml.OverviewRow("Bimbach", temperature: "", change: "+1")), null));
        result.Stations[0].TemperatureText.Should().Be("");
    }

    [TestMethod]
    public async Task ReadAsync_TrendAndOtherCells_NotRead()
    {
        var result = await Read(OsthessenWetterHtml.DefaultPage());
        foreach (var station in result.Stations)
        {
            station.TemperatureText.Should().NotContain("\u00b0C/h");
            station.HumidityText.Should().NotContain("km/h");
            station.HumidityText.Should().NotContain("mm");
            station.OverviewTimeText.Should().NotContain("11.1");
        }
    }

    [TestMethod]
    public async Task ReadAsync_UnclosedAndStrayTags_Tolerated()
    {
        var result = await Read(OsthessenWetterHtml.DefaultPage());
        result.Stations.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task ReadAsync_OverviewTableMissing_ThrowsFormatChanged()
    {
        var ex = await ReadFails(OsthessenWetterHtml.Page(null, Wind(OsthessenWetterHtml.WindRow("Rossbach"))));
        ex.ErrorId.Should().Be(OsthessenWetterException.FormatChangedErrorId);
    }

    [TestMethod]
    public async Task ReadAsync_OverviewHeaderChanged_ThrowsFormatChanged()
    {
        var header = OsthessenWetterHtml.OverviewHeader().Replace("Luft-<br>feuchte", "Feuchte SECRET");
        var ex = await ReadFails(OsthessenWetterHtml.Page(OsthessenWetterHtml.Table(header, OsthessenWetterHtml.OverviewRow("Bimbach")), null));
        ex.ErrorId.Should().Be(OsthessenWetterException.FormatChangedErrorId);
        ex.Message.Should().NotContain("SECRET");
    }

    [TestMethod]
    public async Task ReadAsync_RowWithMissingCell_ThrowsFormatChanged()
    {
        var row = OsthessenWetterHtml.OverviewRow("Bimbach").Replace(" <td class='tg-iohx'>93<font size='-2'>%</font></td>", "");
        var ex = await ReadFails(OsthessenWetterHtml.Page(Overview(row), null));
        ex.ErrorId.Should().Be(OsthessenWetterException.FormatChangedErrorId);
    }

    [TestMethod]
    public async Task ReadAsync_RowWithExtraCell_ThrowsFormatChanged()
    {
        var row = OsthessenWetterHtml.OverviewRow("Bimbach").Replace(" </tr>", "<td>x</td> </tr>");
        var ex = await ReadFails(OsthessenWetterHtml.Page(Overview(row), null));
        ex.ErrorId.Should().Be(OsthessenWetterException.FormatChangedErrorId);
    }

    [TestMethod]
    public async Task ReadAsync_UnknownStatusTitle_ThrowsFormatChanged()
    {
        var row = OsthessenWetterHtml.OverviewRow("Bimbach", status: "Station maintenance SECRET");
        var ex = await ReadFails(OsthessenWetterHtml.Page(Overview(row), null));
        ex.ErrorId.Should().Be(OsthessenWetterException.FormatChangedErrorId);
        ex.Message.Should().NotContain("SECRET");
    }

    [TestMethod]
    public async Task ReadAsync_MissingStatus_ThrowsFormatChanged()
    {
        var row = OsthessenWetterHtml.OverviewRow("Bimbach").Replace(" title='Station online'", "");
        var ex = await ReadFails(OsthessenWetterHtml.Page(Overview(row), null));
        ex.ErrorId.Should().Be(OsthessenWetterException.FormatChangedErrorId);
    }

    [TestMethod]
    public async Task ReadAsync_MissingName_ThrowsFormatChanged()
    {
        var row = OsthessenWetterHtml.OverviewRow("").Replace("<font size='2em'></font>", "");
        var ex = await ReadFails(OsthessenWetterHtml.Page(Overview(row), null));
        ex.ErrorId.Should().Be(OsthessenWetterException.FormatChangedErrorId);
    }

    [TestMethod]
    public async Task ReadAsync_DuplicateOverviewName_BothReturned()
    {
        var result = await Read(OsthessenWetterHtml.Page(Overview(OsthessenWetterHtml.OverviewRow("Bimbach"), OsthessenWetterHtml.OverviewRow("Bimbach")), null));
        result.Stations.Should().HaveCount(2);
        result.Stations.Select(s => s.Name).Should().Equal("Bimbach", "Bimbach");
    }

    [TestMethod]
    public async Task ReadAsync_NoStationRows_EmptyStations()
    {
        var result = await Read(OsthessenWetterHtml.Page(Overview(), null));
        result.Stations.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_OtherSections_NeverRead()
    {
        var result = await Read(OsthessenWetterHtml.DefaultPage());
        foreach (var station in result.Stations)
        {
            station.Name.Should().NotContain("db.example.invalid");
            station.Name.Should().NotContain("web000");
            station.Name.Should().NotContain("Injected");
            station.OverviewTimeText.Should().NotContain("db.example.invalid");
            station.OverviewTimeText.Should().NotContain("web000");
            station.OverviewTimeText.Should().NotContain("Injected");
            station.TemperatureText.Should().NotContain("db.example.invalid");
            station.TemperatureText.Should().NotContain("web000");
            station.TemperatureText.Should().NotContain("Injected");
            station.HumidityText.Should().NotContain("db.example.invalid");
            station.HumidityText.Should().NotContain("web000");
            station.HumidityText.Should().NotContain("Injected");
        }
        result.Diagnostics.Select(d => d.StationName ?? "").Should().NotContain(n => n.Contains("db.example.invalid") || n.Contains("web000") || n.Contains("Injected"));
    }

    [TestMethod]
    public async Task ReadAsync_WindRowsJoinedByKey_DifferentOrder()
    {
        var page = await Read(OsthessenWetterHtml.DefaultPage());

        Station(page, "Ro\u00dfbach").WindMeanText.Should().Be("12.5km/h");
        Station(page, "Ro\u00dfbach").WindTimeText.Should().Be("09:31 Uhr");
        Station(page, "M\u00fcs-Hohh\u00fctte").WindMeanText.Should().Be("0.0km/h");
        Station(page, "M\u00fcs-Hohh\u00fctte").WindTimeText.Should().Be("09:30 Uhr");
        Station(page, "Langenbieber").WindMeanText.Should().Be("");
        Station(page, "Langenbieber").WindTimeText.Should().Be("");
        page.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_JoinKey_CaseInsensitive()
    {
        var page = await Read(OsthessenWetterHtml.Page(
            OsthessenWetterHtml.Table(OsthessenWetterHtml.OverviewHeader(), OsthessenWetterHtml.OverviewRow("Gotthards")),
            OsthessenWetterHtml.Table(OsthessenWetterHtml.WindHeader(), OsthessenWetterHtml.WindRow("GOTTHARDS", mean10: "7.0"))));

        Station(page, "Gotthards").WindMeanText.Should().Be("7.0km/h");
        page.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_WindRowMissing_DiagnosticUnmatched()
    {
        var page = await Read(OsthessenWetterHtml.Page(
            OsthessenWetterHtml.Table(OsthessenWetterHtml.OverviewHeader(),
                OsthessenWetterHtml.OverviewRow("Bimbach"),
                OsthessenWetterHtml.OverviewRow("Ro&szligbach")),
            OsthessenWetterHtml.Table(OsthessenWetterHtml.WindHeader(), OsthessenWetterHtml.WindRow("Rossbach"))));

        Station(page, "Bimbach").WindMeanText.Should().BeNull();
        Station(page, "Bimbach").WindTimeText.Should().BeNull();
        page.Diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindRowUnmatched, "Bimbach"));
        Station(page, "Ro\u00dfbach").WindMeanText.Should().Be("23.1km/h");
    }

    [TestMethod]
    public async Task ReadAsync_TwoWindCandidates_DiagnosticUnmatched()
    {
        var page = await Read(OsthessenWetterHtml.Page(
            OsthessenWetterHtml.Table(OsthessenWetterHtml.OverviewHeader(), OsthessenWetterHtml.OverviewRow("Ro&szligbach")),
            OsthessenWetterHtml.Table(OsthessenWetterHtml.WindHeader(),
                OsthessenWetterHtml.WindRow("Rossbach"),
                OsthessenWetterHtml.WindRow("Ro&szlig;bach"))));

        Station(page, "Ro\u00dfbach").WindMeanText.Should().BeNull();
        Station(page, "Ro\u00dfbach").WindTimeText.Should().BeNull();
        page.Diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindRowUnmatched, "Ro\u00dfbach"));
    }

    [TestMethod]
    public async Task ReadAsync_WindTableMissing_DiagnosticAndNoWindCells()
    {
        var page = await Read(OsthessenWetterHtml.Page(
            OsthessenWetterHtml.Table(OsthessenWetterHtml.OverviewHeader(),
                OsthessenWetterHtml.OverviewRow("Bimbach"),
                OsthessenWetterHtml.OverviewRow("Schlitz")),
            null));

        foreach (var station in page.Stations)
        {
            station.WindMeanText.Should().BeNull();
            station.WindTimeText.Should().BeNull();
        }
        page.Diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindTableMissing, null));
    }

    [TestMethod]
    public async Task ReadAsync_WindHeaderChanged_DiagnosticAndNoWindCells()
    {
        var windTable = OsthessenWetterHtml.Table(
            OsthessenWetterHtml.WindHeader().Replace("60 Min.", "30 Min."),
            OsthessenWetterHtml.WindRow("Bimbach"));
        var page = await Read(OsthessenWetterHtml.Page(
            OsthessenWetterHtml.Table(OsthessenWetterHtml.OverviewHeader(), OsthessenWetterHtml.OverviewRow("Bimbach")),
            windTable));

        foreach (var station in page.Stations)
        {
            station.WindMeanText.Should().BeNull();
            station.WindTimeText.Should().BeNull();
        }
        page.Diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindTableMissing, null));
    }

    [TestMethod]
    public async Task ReadAsync_WindRowWithMissingCell_RowIgnoredStationUnmatched()
    {
        var windRow = OsthessenWetterHtml.WindRow("Bimbach").Replace("<td class='tg-iohx'>30.6<font size='-2'>km/h</font></td> ", "");
        var page = await Read(OsthessenWetterHtml.Page(
            OsthessenWetterHtml.Table(OsthessenWetterHtml.OverviewHeader(), OsthessenWetterHtml.OverviewRow("Bimbach")),
            OsthessenWetterHtml.Table(OsthessenWetterHtml.WindHeader(), windRow)));

        Station(page, "Bimbach").WindMeanText.Should().BeNull();
        Station(page, "Bimbach").WindTimeText.Should().BeNull();
        page.Diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindRowUnmatched, "Bimbach"));
    }

    [TestMethod]
    public async Task ReadAsync_WindRowWithoutName_RowIgnoredStationUnmatched()
    {
        var overviewRow = OsthessenWetterHtml.OverviewRow("Bimbach");
        var windRow = OsthessenWetterHtml.WindRow("Bimbach").Replace("<font size='2em'>Bimbach</font>", "");
        var page = await Read(OsthessenWetterHtml.Page(Overview(overviewRow), Wind(windRow)));
        page.Stations[0].WindMeanText.Should().BeNull();
        page.Stations[0].WindTimeText.Should().BeNull();
        page.Diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindRowUnmatched, "Bimbach"));
    }

    [TestMethod]
    public async Task ReadAsync_WindRowUnknownStatus_RowIgnoredStationUnmatched()
    {
        var page = await Read(OsthessenWetterHtml.Page(
            OsthessenWetterHtml.Table(OsthessenWetterHtml.OverviewHeader(), OsthessenWetterHtml.OverviewRow("Bimbach")),
            OsthessenWetterHtml.Table(OsthessenWetterHtml.WindHeader(), OsthessenWetterHtml.WindRow("Bimbach", status: "Station maintenance"))));

        Station(page, "Bimbach").WindMeanText.Should().BeNull();
        Station(page, "Bimbach").WindTimeText.Should().BeNull();
        page.Diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindRowUnmatched, "Bimbach"));
    }

    [TestMethod]
    public async Task ReadAsync_WindRowMissingStatus_RowIgnoredStationUnmatched()
    {
        var windRow = OsthessenWetterHtml.WindRow("Bimbach").Replace(" title='Station online'", "");
        var page = await Read(OsthessenWetterHtml.Page(
            OsthessenWetterHtml.Table(OsthessenWetterHtml.OverviewHeader(), OsthessenWetterHtml.OverviewRow("Bimbach")),
            OsthessenWetterHtml.Table(OsthessenWetterHtml.WindHeader(), windRow)));

        Station(page, "Bimbach").WindMeanText.Should().BeNull();
        Station(page, "Bimbach").WindTimeText.Should().BeNull();
        page.Diagnostics.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindRowUnmatched, "Bimbach"));
    }

    [TestMethod]
    public async Task ReadAsync_OneDeviatingWindRow_OnlyThatStationLosesWind()
    {
        var page = await Read(OsthessenWetterHtml.Page(
            OsthessenWetterHtml.Table(OsthessenWetterHtml.OverviewHeader(),
                OsthessenWetterHtml.OverviewRow("Bimbach"),
                OsthessenWetterHtml.OverviewRow("Schlitz")),
            OsthessenWetterHtml.Table(OsthessenWetterHtml.WindHeader(),
                OsthessenWetterHtml.WindRow("Bimbach"),
                OsthessenWetterHtml.WindRow("Schlitz", status: "Station maintenance"))));

        Station(page, "Bimbach").WindMeanText.Should().Be("23.1km/h");
        Station(page, "Bimbach").WindTimeText.Should().Be("09:31 Uhr");
        Station(page, "Schlitz").WindMeanText.Should().BeNull();
        Station(page, "Schlitz").WindTimeText.Should().BeNull();
        page.Diagnostics.Should().ContainSingle().Which.Should().BeEquivalentTo(new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindRowUnmatched, "Schlitz"));
    }

    [TestMethod]
    public async Task ReadAsync_OfflineWindRow_IsValidStructure()
    {
        var page = await Read(OsthessenWetterHtml.Page(
            OsthessenWetterHtml.Table(OsthessenWetterHtml.OverviewHeader(), OsthessenWetterHtml.OverviewRow("Bimbach", status: "Station offline", time: "", temperature: "", change: "", humidity: "")),
            OsthessenWetterHtml.Table(OsthessenWetterHtml.WindHeader(), OsthessenWetterHtml.WindRow("Bimbach", status: "Station offline", time: "", mean10: ""))));

        Station(page, "Bimbach").WindMeanText.Should().Be("");
        Station(page, "Bimbach").WindTimeText.Should().Be("");
        page.Diagnostics.Should().BeEmpty();
    }

    private static OsthessenWetterStationRow Station(OsthessenWetterPage page, string name) => page.Stations.Should().ContainSingle(s => s.Name == name).Subject;
}
