using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Osthessen;

namespace WeatherParser.Parsing.Tests.Observations.Osthessen;

[TestClass]
public sealed class OsthessenWetterPageReaderBoundaryTests
{
    private OsthessenWetterPageReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new OsthessenWetterPageReader();
    }

    private async Task<OsthessenWetterPage> Read(string html)
    {
        return await _subject.ReadAsync(OsthessenWetterHtml.ToStream(html), CancellationToken.None);
    }

    private async Task<OsthessenWetterException> ReadFails(string html)
    {
        return (await FluentActions.Awaiting(() => Read(html)).Should().ThrowAsync<OsthessenWetterException>()).Which;
    }

    private static string Overview(params string[] rows)
    {
        return OsthessenWetterHtml.Table(OsthessenWetterHtml.OverviewHeader(), rows);
    }

    private static string Wind(params string[] rows)
    {
        return OsthessenWetterHtml.Table(OsthessenWetterHtml.WindHeader(), rows);
    }

    [TestMethod]
    public async Task ReadAsync_TableWithoutRows_Skipped()
    {
        var page = await Read(OsthessenWetterHtml.Page("<table></table>" + Overview(OsthessenWetterHtml.OverviewRow("Bimbach")), null));

        page.Stations.Should().HaveCount(1);
        page.Stations[0].Name.Should().Be("Bimbach");
    }

    [TestMethod]
    public async Task ReadAsync_StatusCellWithoutImage_ThrowsFormatChanged()
    {
        var row = OsthessenWetterHtml.OverviewRow("Bimbach").Replace("<img src='/uploads/s.jpg' title='Station online' width='18' height='18' border='0'>", "");
        var ex = await ReadFails(OsthessenWetterHtml.Page(Overview(row), null));

        ex.ErrorId.Should().Be(OsthessenWetterException.FormatChangedErrorId);
    }

    [TestMethod]
    public async Task ReadAsync_NameCellWithoutTime_TimeEmpty()
    {
        var row = OsthessenWetterHtml.OverviewRow("Bimbach").Replace("<br><font size='1'>09:31 Uhr</font></td>", "</td>");
        var page = await Read(OsthessenWetterHtml.Page(Overview(row), null));

        page.Stations.Should().HaveCount(1);
        page.Stations[0].Name.Should().Be("Bimbach");
        page.Stations[0].OverviewTimeText.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_TemperatureCellWithoutPrimaryElement_TemperatureEmpty()
    {
        var row = OsthessenWetterHtml.OverviewRow("Bimbach").Replace("<font size='3'><b>10.3<font size='-2'>&deg;C</font></b></font>", "");
        var page = await Read(OsthessenWetterHtml.Page(Overview(row), null));

        page.Stations.Should().HaveCount(1);
        page.Stations[0].TemperatureText.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_WindRowWithoutTime_WindTimeEmpty()
    {
        var overviewRow = OsthessenWetterHtml.OverviewRow("Bimbach");
        var windRow = OsthessenWetterHtml.WindRow("Bimbach").Replace("<br><font size='1'>09:31 Uhr</font></td>", "</td>");
        var page = await Read(OsthessenWetterHtml.Page(Overview(overviewRow), Wind(windRow)));

        page.Stations.Should().HaveCount(1);
        page.Stations[0].WindTimeText.Should().BeEmpty();
        page.Stations[0].WindMeanText.Should().Be("23.1km/h");
        page.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_DuplicateOverviewNames_EachJoinedToTheSameWindRow()
    {
        var overviewRow = OsthessenWetterHtml.OverviewRow("Bimbach");
        var windRow = OsthessenWetterHtml.WindRow("Bimbach", mean10: "4.0");
        var page = await Read(OsthessenWetterHtml.Page(Overview(overviewRow, overviewRow), Wind(windRow)));

        page.Stations.Should().HaveCount(2)
            .And.OnlyContain(s => s.Name == "Bimbach" && s.WindMeanText == "4.0km/h" && s.WindTimeText == "09:31 Uhr");
        page.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_WindStatusCellWithoutImage_RowIgnoredStationUnmatched()
    {
        var overviewRow = OsthessenWetterHtml.OverviewRow("Bimbach");
        var windRow = OsthessenWetterHtml.WindRow("Bimbach").Replace("<img src='/uploads/s.jpg' title='Station online' width='18' height='18' border='0'>", "");
        var page = await Read(OsthessenWetterHtml.Page(Overview(overviewRow), Wind(windRow)));

        page.Stations.Should().HaveCount(1);
        page.Stations[0].WindMeanText.Should().BeNull();
        page.Stations[0].WindTimeText.Should().BeNull();
        page.Diagnostics.Should().ContainSingle().Which.Should().BeEquivalentTo(new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindRowUnmatched, "Bimbach"));
    }

    [TestMethod]
    public async Task ReadAsync_OverviewLabelWithAppendedText_ThrowsFormatChanged()
    {
        var header = OsthessenWetterHtml.OverviewHeader().Replace("feuchte</th>", "feuchteExtra</th>");
        var ex = await ReadFails(OsthessenWetterHtml.Page(OsthessenWetterHtml.Table(header, OsthessenWetterHtml.OverviewRow("Bimbach")), null));
        ex.ErrorId.Should().Be(OsthessenWetterException.FormatChangedErrorId);
    }

    [TestMethod]
    public async Task ReadAsync_OverviewLabelWithPrependedText_ThrowsFormatChanged()
    {
        var header = OsthessenWetterHtml.OverviewHeader().Replace(">Wetterstation<br>", ">XWetterstation<br>");
        var ex = await ReadFails(OsthessenWetterHtml.Page(OsthessenWetterHtml.Table(header, OsthessenWetterHtml.OverviewRow("Bimbach")), null));
        ex.ErrorId.Should().Be(OsthessenWetterException.FormatChangedErrorId);
    }

    [TestMethod]
    public async Task ReadAsync_WindLabelWithAppendedText_WindTableMissing()
    {
        var windHeader = OsthessenWetterHtml.WindHeader().Replace("60 Min.</font>", "60 Min.x</font>");
        var page = await Read(OsthessenWetterHtml.Page(Overview(OsthessenWetterHtml.OverviewRow("Bimbach")), OsthessenWetterHtml.Table(windHeader, OsthessenWetterHtml.WindRow("Bimbach"))));
        page.Stations.Should().ContainSingle();
        page.Stations[0].WindMeanText.Should().BeNull();
        page.Diagnostics.Should().ContainSingle().Which.Should().BeEquivalentTo(new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindTableMissing, null));
    }

    [TestMethod]
    public async Task ReadAsync_DeviatingAndValidWindRowForSameStation_StationUnmatched()
    {
        var page = await Read(OsthessenWetterHtml.Page(Overview(OsthessenWetterHtml.OverviewRow("Bimbach")), Wind(OsthessenWetterHtml.WindRow("Bimbach"), OsthessenWetterHtml.WindRow("Bimbach", status: "Station maintenance"))));

        page.Stations.Should().ContainSingle();
        page.Stations[0].WindMeanText.Should().BeNull();
        page.Stations[0].WindTimeText.Should().BeNull();
        page.Diagnostics.Should().ContainSingle().Which.Should().BeEquivalentTo(new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindRowUnmatched, "Bimbach"));
    }

    [TestMethod]
    public async Task ReadAsync_WindRowWithMissingCell_OtherStationKeepsWind()
    {
        var shortRow = OsthessenWetterHtml.WindRow("Schlitz").Replace("<td class='tg-iohx'>30.6<font size='-2'>km/h</font></td> ", "");
        var page = await Read(OsthessenWetterHtml.Page(Overview(OsthessenWetterHtml.OverviewRow("Bimbach"), OsthessenWetterHtml.OverviewRow("Schlitz")), Wind(OsthessenWetterHtml.WindRow("Bimbach", mean10: "4.0"), shortRow)));

        var bimbach = page.Stations.Single(s => s.Name == "Bimbach");
        bimbach.WindMeanText.Should().Be("4.0km/h");
        bimbach.WindTimeText.Should().Be("09:31 Uhr");

        var schlitz = page.Stations.Single(s => s.Name == "Schlitz");
        schlitz.WindMeanText.Should().BeNull();
        schlitz.WindTimeText.Should().BeNull();

        page.Diagnostics.Should().ContainSingle().Which.Should().BeEquivalentTo(new OsthessenWetterPageDiagnostic(OsthessenWetterNoticeIds.WindRowUnmatched, "Schlitz"));
    }

    [TestMethod]
    public async Task ReadAsync_WindRowWithSingleCell_IgnoredWithoutNotice()
    {
        var page = await Read(OsthessenWetterHtml.Page(Overview(OsthessenWetterHtml.OverviewRow("Bimbach")), Wind("<tr><td>x</td></tr>", OsthessenWetterHtml.WindRow("Bimbach", mean10: "4.0"))));

        page.Stations.Should().ContainSingle();
        page.Stations[0].WindMeanText.Should().Be("4.0km/h");
        page.Diagnostics.Should().BeEmpty();
    }
}
