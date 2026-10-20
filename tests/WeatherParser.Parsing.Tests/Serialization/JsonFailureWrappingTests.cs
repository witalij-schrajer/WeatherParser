using System.Text.Json;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

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

    public sealed class UnsupportedRoot
    {
        public Type Kind { get; set; } = typeof(string);
    }

    [TestMethod]
    public async Task SerializeAsync_UnsupportedPropertyType_ThrowsExportSerializationExceptionAndWritesNothing()
    {
        var dataset = new UnsupportedRoot();
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _jsonSubject!.SerializeAsync(dataset, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_PreCancelledToken_StillThrowsOperationCanceledException()
    {
        var dataset = new ObservationDataset
        {
            RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc),
            Source = "s"
        };
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _jsonSubject!.SerializeAsync(dataset, stream, new CancellationToken(canceled: true)))
            .Should().ThrowAsync<OperationCanceledException>();
    }
}
