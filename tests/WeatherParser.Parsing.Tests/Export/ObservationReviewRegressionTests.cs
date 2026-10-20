using System.Text.Json;
using FluentAssertions;
using Moq;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ObservationReviewRegressionTests
{
    private MockRepository _repository = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _repository = new MockRepository(MockBehavior.Strict);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _repository.VerifyAll();
    }

    [TestMethod]
    public async Task SerializeAsync_NullMeasurements_FailsWithExportSerializationExceptionInBothFormats()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Measurements = null!;

        await AssertSerializeFails<ExportSerializationException>(dataset, stream => new JsonExportSerializer().SerializeAsync(dataset, stream, CancellationToken.None));
        await AssertSerializeFails<ExportSerializationException>(dataset, stream => new XmlExportSerializer().SerializeAsync(dataset, stream, CancellationToken.None));
    }

    [TestMethod]
    public async Task SerializeAsync_InvalidGraphWithCancelledToken_ThrowsOperationCanceledExceptionInBothFormats()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Source = "dwd:99999";

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await AssertSerializeFails<OperationCanceledException>(dataset, stream => new JsonExportSerializer().SerializeAsync(dataset, stream, cancellation.Token));
        await AssertSerializeFails<OperationCanceledException>(dataset, stream => new XmlExportSerializer().SerializeAsync(dataset, stream, cancellation.Token));
    }

    [TestMethod]
    public void Build_DuplicateInput_IsRejectedBeforeAnyServiceCall()
    {
        var validator = _repository.Create<IQuantityValidator>();
        var rounding = _repository.Create<IOutputRounding>();
        var pressureConverter = _repository.Create<IPressureConverter>();
        var periodRateConverter = _repository.Create<IPeriodRateConverter>();
        var windSpeedConverter = _repository.Create<IWindSpeedConverter>();

        var input = new ObservationInput
        {
            Provider = "dwd",
            Location = new ObservationLocation
            {
                StationId = "00044",
                Name = "Großenkneten",
                Latitude = 52.9336m,
                Longitude = 8.2370m
            },
            RetrievedAtUtc = ObservationTestData.RetrievedAtUtc,
            Readings = new[]
            {
                new ObservationReading
                {
                    Kind = QuantityKind.Temperature,
                    Value = 20.3,
                    Time = new NormalizedSourceTime(ObservationTestData.MeasuredAtUtc, TimestampQuality.Source, null)
                },
                new ObservationReading
                {
                    Kind = QuantityKind.Temperature,
                    Value = 21.3,
                    Time = new NormalizedSourceTime(ObservationTestData.MeasuredAtUtc, TimestampQuality.Source, null)
                }
            }
        };

        var builder = new ObservationDatasetBuilder(validator.Object, rounding.Object, pressureConverter.Object, periodRateConverter.Object, windSpeedConverter.Object);
        FluentActions.Invoking(() => builder.Build(input)).Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public async Task SerializeAsync_Json_PressureValueMembers_FollowContractOrder()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Measurements.PressureHpa = new PressureMeasurementValue
        {
            Value = 1013m,
            MeasuredAtUtc = ObservationTestData.MeasuredAtUtc,
            TimestampQuality = TimestampQuality.Source,
            PressureReference = PressureReference.SeaLevel
        };
        dataset.Measurements.PressureMmHg = new PressureMeasurementValue
        {
            Value = 760m,
            MeasuredAtUtc = ObservationTestData.MeasuredAtUtc,
            TimestampQuality = TimestampQuality.Source,
            PressureReference = PressureReference.SeaLevel
        };

        using var stream = new MemoryStream();
        var serializer = new JsonExportSerializer();
        await serializer.SerializeAsync(dataset, stream, CancellationToken.None);

        stream.Position = 0;
        using var document = JsonDocument.Parse(stream);
        var measurements = document.RootElement.GetProperty("measurements");
        var pressureHpa = measurements.GetProperty("pressure_hpa");
        var pressureMmHg = measurements.GetProperty("pressure_mmhg");

        pressureHpa.EnumerateObject().Select(p => p.Name).Should().Equal("value", "measured_at", "timestamp_quality", "pressure_reference");
        pressureMmHg.EnumerateObject().Select(p => p.Name).Should().Equal("value", "measured_at", "timestamp_quality", "pressure_reference");
    }

    private static async Task AssertSerializeFails<TException>(ObservationDataset dataset, Func<Stream, Task> serialize) where TException : Exception
    {
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => serialize(stream)).Should().ThrowAsync<TException>();
        stream.Length.Should().Be(0);
    }
}
