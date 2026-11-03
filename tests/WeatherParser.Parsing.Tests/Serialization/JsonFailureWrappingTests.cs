using System.Text.Json;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Export;

namespace WeatherParser.Parsing.Tests.Serialization;

[TestClass]
public sealed class JsonFailureWrappingTests
{
    private JsonExportSerializer? _jsonSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        _jsonSubject = new JsonExportSerializer();
    }

    [TestMethod]
    public async Task SerializeAsync_SupportedRootFailingInsideSerializer_WrapsTheFailureAndWritesNothing()
    {
        var list = ListTestData.ValidCdcList();
        list.Stations.Add(new CdcStationListEntry());
        using var stream = new MemoryStream();
        (await FluentActions.Awaiting(() => _jsonSubject!.SerializeAsync(list, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>())
            .WithInnerException<InvalidOperationException>();
        stream.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_PreCancelledToken_StillThrowsOperationCanceledException()
    {
        var dataset = ObservationTestData.ValidDataset();
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _jsonSubject!.SerializeAsync(dataset, stream, new CancellationToken(canceled: true)))
            .Should().ThrowAsync<OperationCanceledException>();
    }
}
