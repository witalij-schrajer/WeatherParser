using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Parsing;

[TestClass]
public sealed class CdcNowEvidenceReaderColumnsTests
{
    private ICdcNowEvidenceReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new CdcNowEvidenceReader();
    }

    private static readonly string[] TuValues = { "1001.2", "12.3", "10.1", "85.0", "8.9" };

    private static byte[] TuArchive(params string[] rows) => CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(CdcNowArchiveBuilder.Header("tu"), rows)));

    private Task<CdcNowEvidence> Read(byte[] archive, string productCode = "tu") => _subject.ReadAsync(new MemoryStream(archive), "00044", productCode, CancellationToken.None);

    [TestMethod]
    public async Task ReadAsync_ExposesQualityLevelPerRow()
    {
        var archive = TuArchive(
            CdcNowArchiveBuilder.RowWithQn("44", "202610210000", "3", TuValues),
            CdcNowArchiveBuilder.RowWithQn("44", "202610210010", "2", TuValues));

        var result = await Read(archive);

        result.Rows[0].QualityLevel.Should().Be("3");
        result.Rows[1].QualityLevel.Should().Be("2");
    }

    [TestMethod]
    public async Task ReadAsync_QualityLevelKeptAsString()
    {
        var archive = TuArchive(CdcNowArchiveBuilder.RowWithQn("44", "202610210000", "003", TuValues));

        var result = await Read(archive);

        result.Rows[0].QualityLevel.Should().Be("003");
    }

    [TestMethod]
    [DataRow("A")]
    [DataRow("1234")]
    public async Task ReadAsync_InvalidQualityLevel_StillCdcEvidenceInvalid(string qn)
    {
        Func<Task> act = () => Read(TuArchive(CdcNowArchiveBuilder.RowWithQn("44", "202610210000", qn, TuValues)));

        (await FluentActions.Awaiting(act).Should().ThrowAsync<CatalogFormatException>())
            .Which.ErrorId.Should().Be(CatalogFormatException.CdcEvidenceInvalidErrorId);
    }

    [TestMethod]
    public void CdcNowEvidenceRow_DefaultConstruction_CompilesWithoutQualityLevel()
    {
        var row = new CdcNowEvidenceRow(new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc), new Dictionary<string, string>());

        row.QualityLevel.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_TuArchive_ExposesAllFiveColumns()
    {
        var result = await Read(CdcNowArchiveBuilder.Valid("tu"));

        result.Rows[0].Values.Keys.Should().BeEquivalentTo("PP_10", "TT_10", "TM5_10", "RF_10", "TD_10");
        result.Rows[0].Values["TM5_10"].Should().Be("10.1");
        result.Rows[0].Values["TD_10"].Should().Be("8.9");
    }

    [TestMethod]
    public async Task ReadAsync_FfArchive_ExposesBothColumns()
    {
        var result = await Read(CdcNowArchiveBuilder.Valid("ff"), "ff");

        result.Rows[0].Values.Keys.Should().BeEquivalentTo("FF_10", "DD_10");
        result.Rows[0].Values["FF_10"].Should().Be("3.4");
        result.Rows[0].Values["DD_10"].Should().Be("270");
    }

    [TestMethod]
    public async Task ReadAsync_RrArchive_ExposesThreeColumns()
    {
        var result = await Read(CdcNowArchiveBuilder.Valid("rr"), "rr");

        result.Rows[0].Values.Keys.Should().BeEquivalentTo("RWS_DAU_10", "RWS_10", "RWS_IND_10");
        result.Rows[0].Values["RWS_DAU_10"].Should().Be("10");
        result.Rows[0].Values["RWS_10"].Should().Be("0.0");
        result.Rows[0].Values["RWS_IND_10"].Should().Be("0");
    }

    [TestMethod]
    public async Task ReadAsync_SdArchive_ExposesFourColumns()
    {
        var result = await Read(CdcNowArchiveBuilder.Valid("sd"), "sd");

        result.Rows[0].Values.Keys.Should().BeEquivalentTo("DS_10", "GS_10", "SD_10", "LS_10");
        result.Rows[0].Values["DS_10"].Should().Be("0.0");
        result.Rows[0].Values["GS_10"].Should().Be("12.5");
        result.Rows[0].Values["SD_10"].Should().Be("0.1");
        result.Rows[0].Values["LS_10"].Should().Be("500");
    }

    [TestMethod]
    public async Task ReadAsync_IgnoredColumnWithArbitraryToken_NotValidated()
    {
        var archive = TuArchive(CdcNowArchiveBuilder.RowWithQn("44", "202610210000", "3", "1001.2", "12.3", "abc", "85.0", "8.9"));

        var result = await Read(archive);

        result.Rows[0].Values["TM5_10"].Should().Be("abc");
    }

    [TestMethod]
    public async Task ReadAsync_UsedColumnInvalidToken_StillCdcEvidenceInvalid()
    {
        var archive = TuArchive(CdcNowArchiveBuilder.RowWithQn("44", "202610210000", "3", "1001.2", "abc", "10.1", "85.0", "8.9"));

        Func<Task> act = () => Read(archive);

        (await FluentActions.Awaiting(act).Should().ThrowAsync<CatalogFormatException>())
            .Which.ErrorId.Should().Be(CatalogFormatException.CdcEvidenceInvalidErrorId);
    }

    [TestMethod]
    public async Task ReadAsync_MissingUsedColumn_StillCdcEvidenceInvalid()
    {
        var header = "STATIONS_ID;MESS_DATUM;  QN;PP_10;TT_10;TM5_10;TD_10;eor";
        var row = CdcNowArchiveBuilder.RowWithQn("44", "202610210000", "3", "1001.2", "12.3", "10.1", "8.9");
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, new[] { row })));

        Func<Task> act = () => Read(archive);

        (await FluentActions.Awaiting(act).Should().ThrowAsync<CatalogFormatException>())
            .Which.ErrorId.Should().Be(CatalogFormatException.CdcEvidenceInvalidErrorId);
    }

    [TestMethod]
    public async Task ReadAsync_TokensAreTrimmed()
    {
        var result = await Read(CdcNowArchiveBuilder.Valid("tu"));

        result.Rows[0].Values.Should().OnlyContain(kv => kv.Key == kv.Key.Trim() && kv.Value == kv.Value.Trim());
    }
}
