using System;
using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Serialization;

[TestClass]
public sealed class ExportSerializerStreamContractTests
{
    private JsonExportSerializer? _jsonSubject;
    private XmlExportSerializer? _xmlSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        _jsonSubject = new JsonExportSerializer();
        _xmlSubject = new XmlExportSerializer();
    }

    [TestMethod]
    public async Task SerializeAsync_Json_LeavesDestinationOpen()
    {
        var dataset = BuildValidDataset();
        using var stream = new MemoryStream();
        await _jsonSubject!.SerializeAsync(dataset, stream, CancellationToken.None);

        stream.CanWrite.Should().BeTrue();
        stream.WriteByte(32);
        stream.Length.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_LeavesDestinationOpen()
    {
        var dataset = BuildValidDataset();
        using var stream = new MemoryStream();
        await _xmlSubject!.SerializeAsync(dataset, stream, CancellationToken.None);

        stream.CanWrite.Should().BeTrue();
        stream.WriteByte(32);
        stream.Length.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Json_WritesUtf8WithoutBom()
    {
        var dataset = BuildValidDataset();
        using var stream = new MemoryStream();
        await _jsonSubject!.SerializeAsync(dataset, stream, CancellationToken.None);

        var bytes = stream.ToArray();
        bytes.Length.Should().BeGreaterThan(3);
        bytes.Take(3).ToArray().Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_WritesUtf8WithoutBom()
    {
        var dataset = BuildValidDataset();
        using var stream = new MemoryStream();
        await _xmlSubject!.SerializeAsync(dataset, stream, CancellationToken.None);

        var bytes = stream.ToArray();
        bytes.Length.Should().BeGreaterThan(3);
        bytes.Take(3).ToArray().Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });
    }

    [TestMethod]
    public async Task SerializeAsync_Json_PreCancelledToken_ThrowsAndWritesNothing()
    {
        var dataset = BuildValidDataset();
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _jsonSubject!.SerializeAsync(dataset, stream, new CancellationToken(canceled: true)))
            .Should().ThrowAsync<OperationCanceledException>();

        stream.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_PreCancelledToken_ThrowsAndWritesNothing()
    {
        var dataset = BuildValidDataset();
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _xmlSubject!.SerializeAsync(dataset, stream, new CancellationToken(canceled: true)))
            .Should().ThrowAsync<OperationCanceledException>();

        stream.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Json_DestinationWriteFails_PropagatesIOException()
    {
        var dataset = BuildValidDataset();
        var failingStream = new FailingWriteStream();
        await FluentActions.Awaiting(() => _jsonSubject!.SerializeAsync(dataset, failingStream, CancellationToken.None))
            .Should().ThrowExactlyAsync<IOException>();
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_DestinationWriteFails_PropagatesIOException()
    {
        var dataset = BuildValidDataset();
        var failingStream = new FailingWriteStream();
        await FluentActions.Awaiting(() => _xmlSubject!.SerializeAsync(dataset, failingStream, CancellationToken.None))
            .Should().ThrowExactlyAsync<IOException>();
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_InvalidCharacter_WritesNothingToDestination()
    {
        var dataset = new ObservationDataset
        {
            RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc),
            Source = "bad\u0001value"
        };
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _xmlSubject!.SerializeAsync(dataset, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();

        stream.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_InvalidCharacterAfterLargeContent_WritesNothingToDestination()
    {
        var dataset = new ObservationDataset
        {
            RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc),
            Source = new string('a', 200_000) + "\u0001"
        };
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _xmlSubject!.SerializeAsync(dataset, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();

        stream.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_DeclaresOnlyTheExportNamespace()
    {
        var dataset = BuildValidDataset();
        using var stream = new MemoryStream();
        await _xmlSubject!.SerializeAsync(dataset, stream, CancellationToken.None);

        stream.Position = 0;
        var doc = XDocument.Load(stream);
        var nsDecls = doc.Root!.Attributes().Where(a => a.IsNamespaceDeclaration).ToList();

        nsDecls.Should().HaveCount(1);
        nsDecls[0].Value.Should().Be("urn:weatherparser:export:v1");
    }

    private static ObservationDataset BuildValidDataset()
    {
        return new ObservationDataset
        {
            RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc),
            Source = "Fulda-Horas"
        };
    }
}
