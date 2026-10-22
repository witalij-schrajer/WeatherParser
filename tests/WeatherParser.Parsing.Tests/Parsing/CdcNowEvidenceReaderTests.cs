using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Parsing;

[TestClass]
public sealed class CdcNowEvidenceReaderTests
{
    private ICdcNowEvidenceReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new CdcNowEvidenceReader();
    }

    [TestMethod]
    public async Task ReadAsync_ValidEvidenceFixture_ReturnsRows()
    {
        var archive = CdcNowArchiveBuilder.Valid();
        var result = await Read(archive);
        result.StationId.Should().Be("00044");
        result.ProductCode.Should().Be("tu");
        result.Rows.Should().HaveCount(3);
        result.Rows[0].MeasuredAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        result.Rows[0].Values.Should().ContainKeys("PP_10", "TT_10", "RF_10");
        result.Rows[0].Values["PP_10"].Should().Be("1001.2");
        result.Rows[0].Values["TT_10"].Should().Be("12.3");
        result.Rows[0].Values["RF_10"].Should().Be("85.0");
    }

    [TestMethod]
    public async Task ReadAsync_ValidProductLayouts_Accepted()
    {
        foreach (var code in new[] { "ff", "rr", "sd" })
        {
            var archive = CdcNowArchiveBuilder.Valid(code);
            var result = await Read(archive, productCode: code);
            var keys = code switch
            {
                "ff" => new[] { "FF_10", "DD_10" },
                "rr" => new[] { "RWS_10" },
                "sd" => new[] { "GS_10" },
                _ => throw new InvalidOperationException()
            };
            result.Rows[0].Values.Should().ContainKeys(keys);
        }
    }

    [TestMethod]
    public async Task ReadAsync_StationIdWithLeadingZerosMatchesNumericId()
    {
        var header = CdcNowArchiveBuilder.Header("tu");
        var rows = new[] { CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "10.1", "85.0", "8.9") };
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await Read(archive);
    }

    [TestMethod]
    public async Task ReadAsync_WrongStationIdInRow_IsInvalid()
    {
        var header = CdcNowArchiveBuilder.Header("tu");
        var rows = new[] { CdcNowArchiveBuilder.Row("00045", "202610210000", "1001.2", "12.3", "10.1", "85.0", "8.9") };
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_WrongMemberName_IsInvalid()
    {
        var header = CdcNowArchiveBuilder.Header("tu");
        var rows = new[] { CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "10.1", "85.0", "8.9") };
        var archive = CdcNowArchiveBuilder.Zip(("produkt_zehn_now_tu_20250420_20261021_00045.txt", CdcNowArchiveBuilder.Content(header, rows)));
        await ShouldBeInvalid(archive);

        archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("ff", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await ShouldBeInvalid(archive, productCode: "tu");
    }

    [TestMethod]
    public async Task ReadAsync_TwoMembers_IsInvalid()
    {
        var header = CdcNowArchiveBuilder.Header("tu");
        var rows = new[] { CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "10.1", "85.0", "8.9") };
        var archive = CdcNowArchiveBuilder.Zip(
            (CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)),
            ("extra.txt", Encoding.UTF8.GetBytes("content")));
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_NoMember_IsInvalid()
    {
        var archive = CdcNowArchiveBuilder.Zip();
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_CorruptArchive_IsInvalid()
    {
        var archive = new byte[] { 1, 2, 3, 4, 5 };
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_UncompressedSizeAtLimit_Accepted()
    {
        var name = CdcNowArchiveBuilder.MemberName("tu", "00044");
        var content = CdcNowArchiveBuilder.ContentOfLength(1048576);
        content.Length.Should().Be(1048576);
        var archive = CdcNowArchiveBuilder.Zip((name, content));
        var result = await Read(archive);
        result.Rows.Should().HaveCount(1000);
    }

    [TestMethod]
    public async Task ReadAsync_UncompressedSizeAboveLimit_IsInvalid()
    {
        var name = CdcNowArchiveBuilder.MemberName("tu", "00044");
        var content = CdcNowArchiveBuilder.ContentOfLength(1048577);
        content.Length.Should().Be(1048577);
        var archive = CdcNowArchiveBuilder.Zip((name, content));
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_RowLimitAtTwoThousand_Accepted()
    {
        var archive = CdcNowArchiveBuilder.Valid(rowCount: 2000);
        await Read(archive);
    }

    [TestMethod]
    public async Task ReadAsync_RowLimitAboveTwoThousand_IsInvalid()
    {
        var archive = CdcNowArchiveBuilder.Valid(rowCount: 2001);
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_HeaderMissingProductColumn_IsInvalid()
    {
        var header = "STATIONS_ID;MESS_DATUM;  QN;PP_10;TT_10;TM5_10;TD_10;eor";
        var rows = new[] { "44;202610210000;3;1001.2;12.3;10.1;8.9;eor" };
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_HeaderDuplicateName_IsInvalid()
    {
        var header = "STATIONS_ID;MESS_DATUM;  QN;PP_10;TT_10;TT_10;RF_10;TD_10;eor";
        var rows = new[] { CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "10.1", "85.0", "8.9", "8.9") };
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_HeaderWrongOrder_IsInvalid()
    {
        var header = "MESS_DATUM;STATIONS_ID;  QN;PP_10;TT_10;TM5_10;RF_10;TD_10;eor";
        var rows = new[] { CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "10.1", "85.0", "8.9") };
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_HeaderLastFieldNotEor_IsInvalid()
    {
        var header = "STATIONS_ID;MESS_DATUM;  QN;PP_10;TT_10;TM5_10;RF_10;TD_10;end";
        var rows = new[] { CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "10.1", "85.0", "8.9") };
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_HeaderFieldNamesAreTrimmed()
    {
        var header = " STATIONS_ID ; MESS_DATUM ;  QN ;PP_10;TT_10;TM5_10;RF_10;TD_10; eor";
        var rows = new[] { CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "10.1", "85.0", "8.9") };
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await Read(archive);
    }

    [TestMethod]
    public async Task ReadAsync_RowFieldCountMismatch_IsInvalid()
    {
        var header = CdcNowArchiveBuilder.Header("tu");
        var rows = new[] { CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "10.1", "85.0") };
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_MessDatumInvalid_IsInvalid()
    {
        var header = CdcNowArchiveBuilder.Header("tu");
        var rows = new[] { CdcNowArchiveBuilder.Row("00044", "20261021040", "1001.2", "12.3", "10.1", "85.0", "8.9") };
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await ShouldBeInvalid(archive);

        rows = new[] { CdcNowArchiveBuilder.Row("00044", "202613210400", "1001.2", "12.3", "10.1", "85.0", "8.9") };
        archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_QnGrammar_BothSides()
    {
        var header = CdcNowArchiveBuilder.Header("tu");
        var rows = new[] { "44;202610210000;999;1001.2;12.3;10.1;85.0;8.9;eor" };
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await Read(archive);

        rows = new[] { "44;202610210000;1234;1001.2;12.3;10.1;85.0;8.9;eor" };
        archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_ProductValueGrammar_BothSides()
    {
        var header = CdcNowArchiveBuilder.Header("tu");
        var rows = new[] { CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "10.1", "-123456.123", "8.9") };
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await Read(archive);

        rows = new[] { CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "10.1", "1234567", "8.9") };
        archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await ShouldBeInvalid(archive);

        rows = new[] { CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "10.1", "1.1234", "8.9") };
        archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await ShouldBeInvalid(archive);

        rows = new[] { CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "10.1", "-999", "8.9") };
        archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await Read(archive);
    }

    [TestMethod]
    public async Task ReadAsync_EorLiteral_IsRequired()
    {
        var header = CdcNowArchiveBuilder.Header("tu");
        var rows = new[] { CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "10.1", "85.0", "8.9").Replace(";eor", ";EOR") };
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_DuplicateMessDatum_IsInvalid()
    {
        var header = CdcNowArchiveBuilder.Header("tu");
        var rows = new[]
        {
            CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "10.1", "85.0", "8.9"),
            CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "10.1", "85.0", "8.9")
        };
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_IgnoredColumnsAreNotValidated()
    {
        var header = CdcNowArchiveBuilder.Header("tu");
        var rows = new[] { CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "abc", "85.0", "8.9") };
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows)));
        await Read(archive);
    }

    [TestMethod]
    public async Task ReadAsync_LineEndings_CrlfAndLfAccepted()
    {
        var header = CdcNowArchiveBuilder.Header("tu");
        var rows = new[] { CdcNowArchiveBuilder.Row("00044", "202610210000", "1001.2", "12.3", "10.1", "85.0", "8.9") };
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows, "\r\n")));
        await Read(archive);

        archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, rows, "\n")));
        await Read(archive);
    }

    [TestMethod]
    public async Task ReadAsync_HeaderOnly_ReturnsNoRows()
    {
        var header = CdcNowArchiveBuilder.Header("tu");
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, Enumerable.Empty<string>())));
        var result = await Read(archive);
        result.Rows.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_UnknownProductCode_ThrowsArgumentException()
    {
        var archive = CdcNowArchiveBuilder.Valid();
        Func<Task> act = () => _subject.ReadAsync(new MemoryStream(archive), "00044", "tx", CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task ReadAsync_StationIdNotFiveDigits_ThrowsArgumentException()
    {
        var archive = CdcNowArchiveBuilder.Valid();
        Func<Task> act = () => _subject.ReadAsync(new MemoryStream(archive), "44", "tu", CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task ReadAsync_CancelledBeforeRead_ThrowsOperationCanceled()
    {
        var archive = CdcNowArchiveBuilder.Valid();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        Func<Task> act = () => _subject.ReadAsync(new MemoryStream(archive), "00044", "tu", cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task ReadAsync_StreamThrowsDuringRead_PropagatesFailure()
    {
        var archive = CdcNowArchiveBuilder.Valid();
        var stream = new ThrowingStream();
        Func<Task> act = () => _subject.ReadAsync(stream, "00044", "tu", CancellationToken.None);
        await act.Should().ThrowAsync<IOException>();
    }

    [TestMethod]
    public async Task ReadAsync_LeavesStreamOpenOnSuccessAndFailure()
    {
        using var valid = new MemoryStream(CdcNowArchiveBuilder.Valid());
        await _subject.ReadAsync(valid, "00044", "tu", CancellationToken.None);
        valid.CanRead.Should().BeTrue();

        using var invalid = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        (await FluentActions.Awaiting(() => _subject.ReadAsync(invalid, "00044", "tu", CancellationToken.None)).Should().ThrowAsync<CatalogFormatException>())
            .Which.ErrorId.Should().Be("CDC-EVIDENCE-INVALID");
        invalid.CanRead.Should().BeTrue();
    }

    [TestMethod]
    public async Task ReadAsync_CancelledAfterPartialRead_ThrowsOperationCanceledAndLeavesStreamOpen()
    {
        using var source = new CancellationTokenSource();
        var archive = CdcNowArchiveBuilder.Zip(CompressionLevel.NoCompression, (CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.ContentOfLength(200000)));
        using var stream = new CancelOnSecondReadStream(archive, source);
        await FluentActions.Awaiting(() => _subject.ReadAsync(stream, "00044", "tu", source.Token)).Should().ThrowAsync<OperationCanceledException>();
        stream.ReadCount.Should().BeGreaterThanOrEqualTo(2);
        stream.CanRead.Should().BeTrue();
    }

    [TestMethod]
    public async Task AddWeatherParsing_ResolvesCdcNowEvidenceReader()
    {
        var services = new ServiceCollection();
        services.AddWeatherParsing();
        using var provider = services.BuildServiceProvider(validateScopes: true);
        var reader = provider.GetRequiredService<ICdcNowEvidenceReader>();
        reader.Should().BeOfType<CdcNowEvidenceReader>();
    }

    [TestMethod]
    public async Task ReadAsync_CompressedSizeAtLimit_Accepted()
    {
        var name = CdcNowArchiveBuilder.MemberName("tu", "00044");
        var probe = CdcNowArchiveBuilder.ContentOfLength(200000);
        var overhead = CdcNowArchiveBuilder.Zip(CompressionLevel.NoCompression, (name, probe)).Length - probe.Length;
        var content = CdcNowArchiveBuilder.ContentOfLength(1048576 - overhead);
        var archive = CdcNowArchiveBuilder.Zip(CompressionLevel.NoCompression, (name, content));
        archive.Length.Should().Be(1048576);
        var result = await Read(archive);
        result.Rows.Should().HaveCount(1000);
    }

    [TestMethod]
    public async Task ReadAsync_CompressedSizeAboveLimit_IsInvalid()
    {
        var name = CdcNowArchiveBuilder.MemberName("tu", "00044");
        var probe = CdcNowArchiveBuilder.ContentOfLength(200000);
        var overhead = CdcNowArchiveBuilder.Zip(CompressionLevel.NoCompression, (name, probe)).Length - probe.Length;
        var content = CdcNowArchiveBuilder.ContentOfLength(1048576 - overhead + 1);
        var archive = CdcNowArchiveBuilder.Zip(CompressionLevel.NoCompression, (name, content));
        archive.Length.Should().Be(1048577);
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_EmptyMember_IsInvalid()
    {
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), Array.Empty<byte>()));
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_HeaderWithFewerThanFourFields_IsInvalid()
    {
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content("STATIONS_ID;MESS_DATUM;eor", [])));
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_HeaderSecondFieldNotMessDatum_IsInvalid()
    {
        var header = "STATIONS_ID;QN;MESS_DATUM;PP_10;TT_10;TM5_10;RF_10;TD_10;eor";
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, [])));
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_HeaderThirdFieldNotQn_IsInvalid()
    {
        var header = "STATIONS_ID;MESS_DATUM;PP_10;QN;TT_10;TM5_10;RF_10;TD_10;eor";
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, [])));
        await ShouldBeInvalid(archive);
    }

    [TestMethod]
    public async Task ReadAsync_RowStationIdNotDigits_IsInvalid()
    {
        var header = CdcNowArchiveBuilder.Header("tu");
        var archive = CdcNowArchiveBuilder.Zip((CdcNowArchiveBuilder.MemberName("tu", "00044"), CdcNowArchiveBuilder.Content(header, ["A4;202610210000;3;1001.2;12.3;10.1;85.0;8.9;eor"])));
        await ShouldBeInvalid(archive);
    }

    private Task<CdcNowEvidence> Read(byte[] archive, string stationId = "00044", string productCode = "tu") => _subject.ReadAsync(new MemoryStream(archive), stationId, productCode, CancellationToken.None);

    private async Task ShouldBeInvalid(byte[] archive, string stationId = "00044", string productCode = "tu")
    {
        (await FluentActions.Awaiting(() => Read(archive, stationId, productCode)).Should().ThrowAsync<CatalogFormatException>())
            .Which.ErrorId.Should().Be("CDC-EVIDENCE-INVALID");
    }

    private sealed class ThrowingStream : MemoryStream
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new IOException("boom");
    }

    private sealed class CancelOnSecondReadStream(byte[] bytes, CancellationTokenSource source) : MemoryStream(bytes)
    {
        public int ReadCount { get; private set; }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            if (ReadCount == 2)
            {
                source.Cancel();
            }
            return await base.ReadAsync(buffer, cancellationToken);
        }
    }
}
