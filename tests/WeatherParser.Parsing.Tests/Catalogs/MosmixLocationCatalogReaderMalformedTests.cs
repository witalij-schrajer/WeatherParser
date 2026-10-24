using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class MosmixLocationCatalogReaderMalformedTests
{
    private const string Endpoint = "mosmix-catalog";
    private MockRepository _mocks = null!;
    private Mock<ILogger<MosmixLocationCatalogReader>> _logger = null!;
    private MosmixLocationCatalogReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _logger = _mocks.Create<ILogger<MosmixLocationCatalogReader>>();
        _subject = new MosmixLocationCatalogReader(_logger.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private Task<MosmixLocationCatalog> Read(params string[] lines) => _subject.ReadAsync(new MemoryStream(MosmixCatalogBuilder.Catalog(lines)), Endpoint, CancellationToken.None);

    private static void ShouldBeSkipped(MosmixLocationCatalog result, int lineNumber)
    {
        result.Diagnostics.Should().ContainSingle(d => d.LineNumber == lineNumber && d.Id == CatalogDiagnostic.RecordSkippedId && d.EndpointName == Endpoint);
    }

    private void ExpectNoAltitudeNotice()
    {
        _logger.Setup(l => l.Log(
            LogLevel.Warning,
            It.Is<EventId>(e => e.Name == "MOSMIX-LOCATION-NO-ALTITUDE"),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains(Endpoint)),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
    }

    private void VerifyNoAltitudeNotices(int count) => _logger.Verify(l => l.Log(
            LogLevel.Warning,
            It.Is<EventId>(e => e.Name == "MOSMIX-LOCATION-NO-ALTITUDE"),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(count));

    private static async Task ShouldBeFormatFailure(Func<Task> act)
    {
        var exception = await act.Should().ThrowAsync<CatalogFormatException>();
        exception.Which.ErrorId.Should().Be("CATALOG-FORMAT-INVALID");
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
            return await base.ReadAsync(buffer.Length > 64 ? buffer[..64] : buffer, cancellationToken);
        }
    }

    private sealed class ThrowingStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new IOException("Simulated read failure.");
    }

    [TestMethod]
    public async Task ReadAsync_MalformedRecord_SkippedWithDiagnostic()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("A001"),
            MosmixCatalogBuilder.Line("A002", latitude: "5O.00"));

        result.Records.Should().ContainSingle();
        result.Records[0].MosmixId.Should().Be("A001");
        ShouldBeSkipped(result, 4);
    }

    [TestMethod]
    public async Task ReadAsync_ValidInvalidValidRecords_SkippedWithDiagnostic()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("A001"),
            MosmixCatalogBuilder.Line("A002", longitude: "9.0"),
            MosmixCatalogBuilder.Line("A003"));

        result.Records.Should().HaveCount(2);
        result.Records[0].MosmixId.Should().Be("A001");
        result.Records[1].MosmixId.Should().Be("A003");
        ShouldBeSkipped(result, 4);
    }

    [TestMethod]
    public async Task ReadAsync_AllRecordsInvalid_IsFormatFailure()
    {
        await ShouldBeFormatFailure(() => Read(
            MosmixCatalogBuilder.Line("A001", latitude: "x"),
            MosmixCatalogBuilder.Line("A002", latitude: "x")));
    }

    [TestMethod]
    public async Task ReadAsync_HeaderAndDashLineWithoutRecords_IsFormatFailure()
    {
        await ShouldBeFormatFailure(() => Read());
    }

    [TestMethod]
    public async Task ReadAsync_DuplicateId_ExcludesAllWithDiagnostic()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("X123A"),
            MosmixCatalogBuilder.Line("A001"),
            MosmixCatalogBuilder.Line("X123A", name: "ANDERSWO"));

        result.Records.Should().ContainSingle();
        result.Records[0].MosmixId.Should().Be("A001");

        result.Diagnostics.Should().HaveCount(2);
        result.Diagnostics[0].Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics[0].LineNumber.Should().Be(3);
        result.Diagnostics[0].StationId.Should().Be("X123A");
        result.Diagnostics[0].EndpointName.Should().Be(Endpoint);
        result.Diagnostics[1].Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics[1].LineNumber.Should().Be(5);
        result.Diagnostics[1].StationId.Should().Be("X123A");
        result.Diagnostics[1].EndpointName.Should().Be(Endpoint);
    }

    [TestMethod]
    public async Task ReadAsync_OnlyDuplicates_EmptyResultWithoutException()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("X123A"),
            MosmixCatalogBuilder.Line("X123A"));

        result.Records.Should().BeEmpty();
        result.Diagnostics.Should().HaveCount(2);
        result.Diagnostics[0].Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics[0].LineNumber.Should().Be(3);
        result.Diagnostics[0].StationId.Should().Be("X123A");
        result.Diagnostics[0].EndpointName.Should().Be(Endpoint);
        result.Diagnostics[1].Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics[1].LineNumber.Should().Be(4);
        result.Diagnostics[1].StationId.Should().Be("X123A");
        result.Diagnostics[1].EndpointName.Should().Be(Endpoint);
    }

    [TestMethod]
    public async Task ReadAsync_DuplicateIdWithoutAltitude_WritesNoAltitudeNotice()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("A001"),
            MosmixCatalogBuilder.Line("X123A", elevation: null),
            MosmixCatalogBuilder.Line("X123A"));

        result.Records.Should().ContainSingle();
        result.Records[0].MosmixId.Should().Be("A001");

        result.Diagnostics.Should().HaveCount(2);
        result.Diagnostics[0].Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics[0].LineNumber.Should().Be(4);
        result.Diagnostics[0].StationId.Should().Be("X123A");
        result.Diagnostics[0].EndpointName.Should().Be(Endpoint);
        result.Diagnostics[1].Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        result.Diagnostics[1].LineNumber.Should().Be(5);
        result.Diagnostics[1].StationId.Should().Be("X123A");
        result.Diagnostics[1].EndpointName.Should().Be(Endpoint);
    }

    [TestMethod]
    public async Task ReadAsync_NumericOverflowOrDisallowedSeparator_SkippedWithDiagnostic()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("A001"),
            MosmixCatalogBuilder.Line("A002", latitude: "50,30"),
            MosmixCatalogBuilder.Line("A003", latitude: "1234.00"),
            MosmixCatalogBuilder.Line("A004", elevation: "123456"),
            MosmixCatalogBuilder.Line("A005", elevation: "1e3"),
            MosmixCatalogBuilder.Line("A006", longitude: "+9.00"));

        result.Records.Should().ContainSingle();
        result.Records[0].MosmixId.Should().Be("A001");

        result.Diagnostics.Should().HaveCount(5);
        for (int i = 0; i < 5; i++)
        {
            result.Diagnostics[i].Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
            result.Diagnostics[i].LineNumber.Should().Be(4 + i);
            result.Diagnostics[i].EndpointName.Should().Be(Endpoint);
        }
    }

    [TestMethod]
    public async Task ReadAsync_EmptyStream_IsFormatFailure()
    {
        await ShouldBeFormatFailure(() => _subject.ReadAsync(new MemoryStream(), Endpoint, CancellationToken.None));
    }

    [TestMethod]
    public async Task ReadAsync_HeaderWithoutDashLine_IsFormatFailure()
    {
        await ShouldBeFormatFailure(() => _subject.ReadAsync(new MemoryStream(MosmixCatalogBuilder.Text(MosmixCatalogBuilder.Header + "\n")), Endpoint, CancellationToken.None));
    }

    [TestMethod]
    public async Task ReadAsync_CancelledBeforeRead_ThrowsOperationCanceledException()
    {
        var token = new CancellationToken(true);
        await FluentActions.Awaiting(() => _subject.ReadAsync(new MemoryStream(MosmixCatalogBuilder.Catalog(MosmixCatalogBuilder.Line())), Endpoint, token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task ReadAsync_CancelledDuringRead_ThrowsOperationCanceledAndLeavesStreamOpen()
    {
        using var source = new CancellationTokenSource();
        var bytes = MosmixCatalogBuilder.Catalog(Enumerable.Range(1, 20).Select(i => MosmixCatalogBuilder.Line("B" + i.ToString("000"))).ToArray());
        using var stream = new CancelOnSecondReadStream(bytes, source);
        await FluentActions.Awaiting(() => _subject.ReadAsync(stream, Endpoint, source.Token))
            .Should().ThrowAsync<OperationCanceledException>();
        stream.ReadCount.Should().BeGreaterThanOrEqualTo(2);
        stream.CanRead.Should().BeTrue();
    }

    [TestMethod]
    public async Task ReadAsync_StreamThrowsDuringRead_PropagatesFailureAndLeavesStreamOpen()
    {
        var stream = new ThrowingStream(MosmixCatalogBuilder.Catalog(MosmixCatalogBuilder.Line()));
        (await FluentActions.Awaiting(() => _subject.ReadAsync(stream, Endpoint, CancellationToken.None))
            .Should().ThrowAsync<IOException>()).WithMessage("Simulated read failure.");
        stream.CanRead.Should().BeTrue();
    }

    [TestMethod]
    public async Task ReadAsync_FormatFailure_LeavesStreamOpen()
    {
        var stream = new MemoryStream(MosmixCatalogBuilder.Text("INVALID HEADER\n" + MosmixCatalogBuilder.DashLine + "\n"));
        (await FluentActions.Awaiting(() => _subject.ReadAsync(stream, Endpoint, CancellationToken.None))
            .Should().ThrowAsync<CatalogFormatException>()).Which.ErrorId.Should().Be("CATALOG-FORMAT-INVALID");
        stream.CanRead.Should().BeTrue();
    }

    [TestMethod]
    public async Task ReadAsync_LineAt2048Characters_Accepted()
    {
        var line = MosmixCatalogBuilder.Line("A001");
        line += new string(' ', 2048 - line.Length);
        var result = await Read(line);
        result.Records.Should().ContainSingle();
        result.Records[0].MosmixId.Should().Be("A001");
    }

    [TestMethod]
    public async Task ReadAsync_LineOf2049Characters_IsFormatFailure()
    {
        var line = MosmixCatalogBuilder.Line("A001");
        line += new string(' ', 2049 - line.Length);
        await ShouldBeFormatFailure(() => Read(line));
    }

    [TestMethod]
    public async Task ReadAsync_HeaderLineOf2049Characters_IsFormatFailure()
    {
        var text = MosmixCatalogBuilder.Text(MosmixCatalogBuilder.Header + new string(' ', 2049 - MosmixCatalogBuilder.Header.Length) + "\n" + MosmixCatalogBuilder.DashLine + "\n" + MosmixCatalogBuilder.Line() + "\n");
        await ShouldBeFormatFailure(() => _subject.ReadAsync(new MemoryStream(text), Endpoint, CancellationToken.None));
    }

    [TestMethod]
    public async Task ReadAsync_FourTokenLine_Skipped()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("A001"),
            MosmixCatalogBuilder.Line("A002") + " 7");

        result.Records.Should().ContainSingle();
        result.Records[0].MosmixId.Should().Be("A001");
        ShouldBeSkipped(result, 4);
    }

    [TestMethod]
    public async Task ReadAsync_ShortLine_Skipped()
    {
        var result = await Read(
            MosmixCatalogBuilder.Line("A001"),
            "A002 ---- KURZ");

        result.Records.Should().ContainSingle();
        result.Records[0].MosmixId.Should().Be("A001");
        ShouldBeSkipped(result, 4);
    }
}
