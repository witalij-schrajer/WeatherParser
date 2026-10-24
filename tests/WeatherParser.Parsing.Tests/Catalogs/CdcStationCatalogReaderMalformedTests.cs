using System.Globalization;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class CdcStationCatalogReaderMalformedTests
{
    private const string Endpoint = "tu-list";
    private ICdcStationCatalogReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new CdcStationCatalogReader();
    }

    [TestMethod]
    public async Task ReadAsync_TabSeparatedHeader_Accepted()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001")
        };

        using var stream = CdcStationListBuilder.Stream(lines, header: CdcStationListBuilder.Header.Replace(' ', '\t'));
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().ContainSingle();
        result.Records[0].StationId.Should().Be("00001");
    }

    [TestMethod]
    public async Task ReadAsync_AltitudeWithLeadingZeros_Accepted()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001", altitude: "0000123"),
            CdcStationListBuilder.Line("00002", altitude: "-0000500")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(2);
        result.Records[0].AltitudeM.Should().Be(123m);
        result.Records[1].AltitudeM.Should().Be(-500m);
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_CancelledAfterPartialRead_ThrowsOperationCanceledAndLeavesStreamOpen()
    {
        var lines = Enumerable.Range(1, 10).Select(i => CdcStationListBuilder.Line(i.ToString("D5"))).ToArray();
        using var source = new CancellationTokenSource();
        using var stream = new CancelOnSecondReadStream(
            CdcStationListBuilder.Stream(lines).ToArray(),
            source);

        Func<Task> act = () => _subject.ReadAsync(stream, Endpoint, source.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
        stream.ReadCount.Should().BeGreaterThanOrEqualTo(2);
        stream.CanRead.Should().BeTrue();
    }

    [TestMethod]
    public async Task ReadAsync_MissingField_SkippedWithDiagnostic()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002").Substring(0, 30), // ends inside altitude field
            CdcStationListBuilder.Line("00003")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(2);
        result.Records[0].StationId.Should().Be("00001");
        result.Records[1].StationId.Should().Be("00003");

        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 4)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
    }

    [TestMethod]
    public async Task ReadAsync_NonNumericCoordinate_SkippedWithDiagnostic()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002", latitude: "ab.cdef"),
            CdcStationListBuilder.Line("00003")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(2);
        result.Records[0].StationId.Should().Be("00001");
        result.Records[1].StationId.Should().Be("00003");

        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 4)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
    }

    [TestMethod]
    public async Task ReadAsync_OutOfRangeCoordinate_SkippedWithDiagnostic()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002", longitude: "181.0000"),
            CdcStationListBuilder.Line("00003")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(2);
        result.Records[0].StationId.Should().Be("00001");
        result.Records[1].StationId.Should().Be("00003");

        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 4)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
    }

    [TestMethod]
    public async Task ReadAsync_EmptyId_SkippedWithDiagnostic()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("     "), // empty ID
            CdcStationListBuilder.Line("00003")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(2);
        result.Records[0].StationId.Should().Be("00001");
        result.Records[1].StationId.Should().Be("00003");

        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 4)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics.Single(d => d.LineNumber == 4).StationId.Should().BeNull();
    }

    [TestMethod]
    public async Task ReadAsync_DuplicateId_ExcludesAllRecordsWithDiagnostic()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002"),
            CdcStationListBuilder.Line("00001", name: "Zweitort")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().ContainSingle();
        result.Records[0].StationId.Should().Be("00002");

        result.Diagnostics.Should().HaveCount(2);
        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 3)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 5)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);

        var diagnostic1 = result.Diagnostics.Single(d => d.LineNumber == 3);
        diagnostic1.StationId.Should().Be("00001");
        diagnostic1.EndpointName.Should().Be(Endpoint);

        var diagnostic2 = result.Diagnostics.Single(d => d.LineNumber == 5);
        diagnostic2.StationId.Should().Be("00001");
        diagnostic2.EndpointName.Should().Be(Endpoint);
    }

    [TestMethod]
    public async Task ReadAsync_ValidInvalidValidRecords_SkippedWithDiagnostic()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            "", // blank line
            CdcStationListBuilder.Line("00002", altitude: "x"),
            CdcStationListBuilder.Line("00003")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(2);
        result.Records[0].StationId.Should().Be("00001");
        result.Records[1].StationId.Should().Be("00003");

        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 5)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
    }

    [TestMethod]
    public async Task ReadAsync_AllRecordsInvalid_IsFormatFailure()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001", name: ""),
            CdcStationListBuilder.Line("00002", name: "")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        Func<Task> act = () => _subject.ReadAsync(stream, Endpoint, CancellationToken.None);
        (await act.Should().ThrowAsync<CatalogFormatException>()).Which.ErrorId.Should().Be(CatalogFormatException.CatalogFormatInvalidErrorId);
    }

    [TestMethod]
    public async Task ReadAsync_EmptyStream_IsFormatFailure()
    {
        using var stream = new MemoryStream();
        Func<Task> act = () => _subject.ReadAsync(stream, Endpoint, CancellationToken.None);
        (await act.Should().ThrowAsync<CatalogFormatException>()).Which.ErrorId.Should().Be(CatalogFormatException.CatalogFormatInvalidErrorId);

        // Stream with only header line and CRLF
        using var stream2 = CdcStationListBuilder.Raw(CdcStationListBuilder.Header + "\r\n");
        Func<Task> act2 = () => _subject.ReadAsync(stream2, Endpoint, CancellationToken.None);
        (await act2.Should().ThrowAsync<CatalogFormatException>()).Which.ErrorId.Should().Be(CatalogFormatException.CatalogFormatInvalidErrorId);

        // Header plus dash line and no data line
        using var stream3 = CdcStationListBuilder.Stream(new string[0], lineEnding: "\r\n");
        Func<Task> act3 = () => _subject.ReadAsync(stream3, Endpoint, CancellationToken.None);
        (await act3.Should().ThrowAsync<CatalogFormatException>()).Which.ErrorId.Should().Be(CatalogFormatException.CatalogFormatInvalidErrorId);
    }

    [TestMethod]
    public async Task ReadAsync_WrongEncodingFixture_IsDetected()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001", name: "W\u00FCrzburg-Testfeld")
        };

        using var stream = CdcStationListBuilder.Stream(lines, encoding: Encoding.UTF8);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        // The record is read but the name is not as expected due to wrong encoding
        result.Records[0].Name.Should().NotBe("W\u00FCrzburg-Testfeld");
    }

    [TestMethod]
    public async Task ReadAsync_CancelledBeforeRead_ThrowsOperationCanceled()
    {
        using var stream = CdcStationListBuilder.Stream(new[] { CdcStationListBuilder.Line() });
        Func<Task> act = () => _subject.ReadAsync(stream, Endpoint, new CancellationToken(true));
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task ReadAsync_CancelledDuringRead_ThrowsOperationCanceled()
    {
        using var source = new CancellationTokenSource();
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002")
        };

        using var stream = new CancelOnReadStream(
            CdcStationListBuilder.Stream(lines).ToArray(),
            source);

        Func<Task> act = () => _subject.ReadAsync(stream, Endpoint, source.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task ReadAsync_NonNumericAltitude_SkippedWithDiagnostic()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002", altitude: "abc"),
            CdcStationListBuilder.Line("00003")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(2);
        result.Records[0].StationId.Should().Be("00001");
        result.Records[1].StationId.Should().Be("00003");

        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 4)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
    }

    [TestMethod]
    public async Task ReadAsync_NumericOverflow_SkippedWithDiagnostic()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002", altitude: "9999999"),
            CdcStationListBuilder.Line("00003", latitude: "99999999999.0"),
            CdcStationListBuilder.Line("00004")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(2);
        result.Records[0].StationId.Should().Be("00001");
        result.Records[1].StationId.Should().Be("00004");

        result.Diagnostics.Should().HaveCount(2);
        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 4)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 5)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
    }

    [TestMethod]
    public async Task ReadAsync_DisallowedSeparatorOrGrouping_Rejected()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002", latitude: "50,1234"), // comma as decimal separator
            CdcStationListBuilder.Line("00003", altitude: "1.000"),   // dot as grouping separator
            CdcStationListBuilder.Line("00004", altitude: "1,000")    // comma as grouping separator
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().ContainSingle().Which.StationId.Should().Be("00001");

        result.Diagnostics.Should().HaveCount(3);
        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 4)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 5)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 6)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
    }

    [TestMethod]
    public async Task ReadAsync_LeavesStreamOpenOnFormatFailureAndCancellation()
    {
        // Format failure
        using var stream1 = CdcStationListBuilder.Raw("Wrong" + "\r\n");
        Func<Task> act1 = () => _subject.ReadAsync(stream1, Endpoint, CancellationToken.None);
        await act1.Should().ThrowAsync<CatalogFormatException>();
        stream1.CanRead.Should().BeTrue();

        // Cancellation
        using var source = new CancellationTokenSource();
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001")
        };

        using var stream2 = new CancelOnReadStream(
            CdcStationListBuilder.Stream(lines).ToArray(),
            source);

        Func<Task> act2 = () => _subject.ReadAsync(stream2, Endpoint, source.Token);
        await act2.Should().ThrowAsync<OperationCanceledException>();
        stream2.CanRead.Should().BeTrue();
    }

    [TestMethod]
    public async Task ReadAsync_LineAt2048Characters_Accepted()
    {
        var line = CdcStationListBuilder.Line("00001").PadRight(2048);
        var lines = new[] { line };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().ContainSingle();
        result.Records[0].StationId.Should().Be("00001");
    }

    [TestMethod]
    public async Task ReadAsync_LineOf2049Characters_IsFormatFailure()
    {
        var line = CdcStationListBuilder.Line("00001").PadRight(2049);
        var lines = new[] { line };

        using var stream = CdcStationListBuilder.Stream(lines);
        Func<Task> act = () => _subject.ReadAsync(stream, Endpoint, CancellationToken.None);
        (await act.Should().ThrowAsync<CatalogFormatException>()).Which.ErrorId.Should().Be(CatalogFormatException.CatalogFormatInvalidErrorId);
    }

    [TestMethod]
    public async Task ReadAsync_HeaderOrDashLineLongerThan2048_IsFormatFailure()
    {
        // Header longer than 2048 characters
        using var stream1 = CdcStationListBuilder.Stream(
            new[] { CdcStationListBuilder.Line("00001") },
            header: CdcStationListBuilder.Header.PadRight(2049));
        await ShouldBeFormatFailure(stream1);

        // Dash line longer than 2048 characters
        using var stream2 = CdcStationListBuilder.Stream(
            new[] { CdcStationListBuilder.Line("00001") },
            dashLine: CdcStationListBuilder.DashLine.PadRight(2049));
        await ShouldBeFormatFailure(stream2);
    }

    [TestMethod]
    public async Task ReadAsync_DiagnosticsOrderedByLineNumber()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002", name: ""),
            CdcStationListBuilder.Line("00001", name: "Zweitort"),
            CdcStationListBuilder.Line("00003")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().ContainSingle().Which.StationId.Should().Be("00003");

        result.Diagnostics.Select(d => d.LineNumber).Should().Equal(3, 4, 5);
    }

    [TestMethod]
    public async Task ReadAsync_HeaderTokensReordered_IsFormatFailure()
    {
        var header = "Stations_id bis_datum von_datum Stationshoehe geoBreite geoLaenge Stationsname Bundesland Abgabe";
        using var stream = CdcStationListBuilder.Stream(new[] { CdcStationListBuilder.Line() }, header: header);
        Func<Task> act = () => _subject.ReadAsync(stream, Endpoint, CancellationToken.None);
        (await act.Should().ThrowAsync<CatalogFormatException>()).Which.ErrorId.Should().Be(CatalogFormatException.CatalogFormatInvalidErrorId);
    }

    [TestMethod]
    public async Task ReadAsync_InvalidCalendarDate_Skipped()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002", toDate: "20261301"), // Invalid date
            CdcStationListBuilder.Line("00003")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(2);
        result.Records[0].StationId.Should().Be("00001");
        result.Records[1].StationId.Should().Be("00003");

        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 4)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
    }

    [TestMethod]
    public async Task ReadAsync_EmptyAltitudeInMixedFixture_KeptAsNull()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002", altitude: ""),
            CdcStationListBuilder.Line("00003")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(3);
        result.Records[1].AltitudeM.Should().BeNull();
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public async Task ReadAsync_AllRecordsExcludedButValid_IsNotFormatFailure()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00001", name: "Zweitort")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().BeEmpty();
        result.Diagnostics.Should().HaveCount(2);
        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 3)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 4)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
    }

    [TestMethod]
    public async Task ReadAsync_InvalidFromToDateFields_GrammarMismatch_SkippedWithDiagnostic()
    {
        var lines = new[]
        {
            CdcStationListBuilder.Line("00001"),
            CdcStationListBuilder.Line("00002", fromDate: "2026-1-2"), // Invalid date format
            CdcStationListBuilder.Line("00003")
        };

        using var stream = CdcStationListBuilder.Stream(lines);
        var result = await _subject.ReadAsync(stream, Endpoint, CancellationToken.None);

        result.Records.Should().HaveCount(2);
        result.Records[0].StationId.Should().Be("00001");
        result.Records[1].StationId.Should().Be("00003");

        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == 4)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
    }

    private Task<CdcStationCatalog> Read(Stream s) => _subject.ReadAsync(s, Endpoint, CancellationToken.None);

    private static void ShouldBeSkipped(CdcStationCatalog result, int lineNumber)
    {
        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == lineNumber)
            .Which.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics.Single(d => d.LineNumber == lineNumber).EndpointName.Should().Be(Endpoint);
        result.Diagnostics.Single(d => d.LineNumber == lineNumber).Reason.Should().NotBeEmpty();
    }

    private async Task ShouldBeFormatFailure(Stream s)
    {
        (await FluentActions.Awaiting(() => Read(s)).Should().ThrowAsync<CatalogFormatException>())
            .Which.ErrorId.Should().Be("CATALOG-FORMAT-INVALID");
    }

    private sealed class CancelOnReadStream(byte[] bytes, CancellationTokenSource source) : MemoryStream(bytes)
    {
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            source.Cancel();
            return await base.ReadAsync(buffer, cancellationToken);
        }
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
