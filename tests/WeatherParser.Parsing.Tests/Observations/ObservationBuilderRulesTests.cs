using FluentAssertions;
using Moq;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Observations;

[TestClass]
public sealed class ObservationBuilderRulesTests
{
    private MockRepository _repository = null!;
    private IObservationDatasetBuilder _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _repository = new MockRepository(MockBehavior.Strict);
        _subject = new ObservationDatasetBuilder(new QuantityValidator(), new OutputRounding(), new PressureConverter(new OutputRounding()), new PeriodRateConverter(), new WindSpeedConverter());
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _repository.VerifyAll();
    }

    private static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);
    private static NormalizedSourceTime SourceTime(int hour, int minute) => new(Utc(hour, minute), TimestampQuality.Source, null);
    private static ObservationLocation Location() => new() { StationId = "00044", Name = "Großenkneten", Latitude = 52.9336m, Longitude = 8.2370m };
    private static ObservationInput Input(params ObservationReading[] readings) => new() { Provider = "dwd", Location = Location(), RetrievedAtUtc = Utc(12, 5), Readings = readings };
    private static ObservationReading Reading(QuantityKind kind, double? value, NormalizedSourceTime? time) => new() { Kind = kind, Value = value, Time = time };
    private static ObservationReading Temperature() => Reading(QuantityKind.Temperature, 20.3, SourceTime(12, 0));

    [TestMethod]
    [DataRow("DWD")]
    [DataRow("dwd\n")]
    [DataRow("")]
    [DataRow("dw:d")]
    [DataRow("1dwd")]
    public void Build_InvalidProvider_ThrowsArgumentException(string provider)
    {
        var input = new ObservationInput { Provider = provider, Location = Location(), RetrievedAtUtc = Utc(12, 5), Readings = new[] { Temperature() } };

        FluentActions.Invoking(() => _subject.Build(input)).Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Build_MissingLocation_ThrowsArgumentException()
    {
        var input = new ObservationInput { Provider = "dwd", RetrievedAtUtc = Utc(12, 5), Readings = new[] { Temperature() } };

        FluentActions.Invoking(() => _subject.Build(input)).Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Build_MissingKind_ThrowsArgumentException()
    {
        var input = Input(new ObservationReading { Value = 20.3, Time = SourceTime(12, 0) });

        FluentActions.Invoking(() => _subject.Build(input)).Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Build_UndefinedKind_ThrowsArgumentException()
    {
        var input = Input(Reading((QuantityKind)99, 20.3, SourceTime(12, 0)));

        FluentActions.Invoking(() => _subject.Build(input)).Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Build_PressureWithoutUnitOrReference_ThrowsArgumentException()
    {
        var withoutUnit = Input(new ObservationReading { Kind = QuantityKind.Pressure, Value = 1013, Time = SourceTime(12, 0), PressureReference = PressureReference.SeaLevel });
        var withoutReference = Input(new ObservationReading { Kind = QuantityKind.Pressure, Value = 1013, Time = SourceTime(12, 0), PressureUnit = PressureUnit.Hectopascal });

        FluentActions.Invoking(() => _subject.Build(withoutUnit)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => _subject.Build(withoutReference)).Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Build_MeanWindWithoutUnit_ThrowsArgumentException()
    {
        var input = Input(Temperature(), Reading(QuantityKind.MeanWindSpeed, 3, SourceTime(12, 0)));

        FluentActions.Invoking(() => _subject.Build(input)).Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Build_UndefinedInputEnum_ThrowsArgumentException()
    {
        var pressure = Input(new ObservationReading { Kind = QuantityKind.Pressure, Value = 1013, Time = SourceTime(12, 0), PressureUnit = (PressureUnit)9, PressureReference = PressureReference.SeaLevel });
        var reference = Input(new ObservationReading { Kind = QuantityKind.Pressure, Value = 1013, Time = SourceTime(12, 0), PressureUnit = PressureUnit.Hectopascal, PressureReference = (PressureReference)9 });
        var wind = Input(Temperature(), new ObservationReading { Kind = QuantityKind.MeanWindSpeed, Value = 3, Time = SourceTime(12, 0), WindSpeedUnit = (WindSpeedUnit)9 });

        FluentActions.Invoking(() => _subject.Build(pressure)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => _subject.Build(reference)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => _subject.Build(wind)).Should().Throw<ArgumentException>();
    }

    [TestMethod]
    [DataRow(QuantityKind.Irradiance, QuantityKind.RadiationEnergy)]
    [DataRow(QuantityKind.RadiationEnergy, QuantityKind.Irradiance)]
    [DataRow(QuantityKind.PrecipitationRate, QuantityKind.PrecipitationAccumulation)]
    [DataRow(QuantityKind.PrecipitationAccumulation, QuantityKind.PrecipitationRate)]
    [DataRow(QuantityKind.Temperature, QuantityKind.Temperature)]
    public void Build_DuplicateTargetField_ThrowsArgumentException(QuantityKind first, QuantityKind second)
    {
        var input = Input(Reading(first, 1, SourceTime(12, 0)), Reading(second, 1, SourceTime(12, 0)));

        FluentActions.Invoking(() => _subject.Build(input)).Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Build_TwoGusts_AreNotDuplicates()
    {
        var input = Input(Temperature(), Reading(QuantityKind.WindGust, 40, SourceTime(12, 0)), Reading(QuantityKind.WindGust, 45, SourceTime(12, 0)));

        var result = _subject.Build(input);

        result.OmittedReadings.Should().HaveCount(2).And.OnlyContain(r => r == new OmittedReading(QuantityKind.WindGust, OmissionReason.NotExportable));
    }

    [TestMethod]
    [DataRow("LocalKind")]
    [DataRow("SubSecond")]
    [DataRow("UndefinedQuality")]
    [DataRow("EstimatedWithoutBerlin")]
    [DataRow("OtherTimeZone")]
    public void Build_InconsistentNormalizedTime_ThrowsArgumentException(string scenario)
    {
        var time = scenario switch
        {
            "LocalKind" => new NormalizedSourceTime(new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Local), TimestampQuality.Source, null),
            "SubSecond" => new NormalizedSourceTime(Utc(12, 0).AddMilliseconds(1), TimestampQuality.Source, null),
            "UndefinedQuality" => new NormalizedSourceTime(Utc(12, 0), (TimestampQuality)9, null),
            "EstimatedWithoutBerlin" => new NormalizedSourceTime(Utc(12, 0), TimestampQuality.EstimatedDate, null),
            "OtherTimeZone" => new NormalizedSourceTime(Utc(12, 0), TimestampQuality.Source, "Europe/Paris"),
            _ => throw new ArgumentException($"Unknown scenario {scenario}.", nameof(scenario))
        };
        var input = Input(Reading(QuantityKind.Temperature, 20.3, time));

        FluentActions.Invoking(() => _subject.Build(input)).Should().Throw<ArgumentException>();
    }

    [TestMethod]
    [DataRow("GustWithNaN", OmissionReason.NotExportable)]
    [DataRow("AccumulationMissingValueNoPeriod", OmissionReason.Missing)]
    [DataRow("HumidityNaNNoTime", OmissionReason.NonFinite)]
    [DataRow("HumidityAboveRangeNoTime", OmissionReason.OutOfRange)]
    [DataRow("AccumulationNoTimeNoPeriod", OmissionReason.MissingTime)]
    [DataRow("AccumulationNoPeriod", OmissionReason.MissingPeriod)]
    public void Build_OmissionReason_FollowsPrecedence(string scenario, OmissionReason expected)
    {
        var reading = scenario switch
        {
            "GustWithNaN" => Reading(QuantityKind.WindGust, double.NaN, SourceTime(12, 0)),
            "AccumulationMissingValueNoPeriod" => Reading(QuantityKind.PrecipitationAccumulation, null, null),
            "HumidityNaNNoTime" => Reading(QuantityKind.RelativeHumidity, double.NaN, null),
            "HumidityAboveRangeNoTime" => Reading(QuantityKind.RelativeHumidity, 101, null),
            "AccumulationNoTimeNoPeriod" => Reading(QuantityKind.PrecipitationAccumulation, 0.5, null),
            "AccumulationNoPeriod" => Reading(QuantityKind.PrecipitationAccumulation, 0.5, SourceTime(12, 0)),
            _ => throw new ArgumentException($"Unknown scenario {scenario}.", nameof(scenario))
        };

        var result = _subject.Build(Input(Temperature(), reading));

        result.OmittedReadings.Should().ContainSingle().Which.Reason.Should().Be(expected);
    }

    [TestMethod]
    public void Build_UnrepresentableWindSpeed_IsOmittedAndOthersExported()
    {
        var wind = new ObservationReading { Kind = QuantityKind.MeanWindSpeed, Value = 3e28, Time = SourceTime(12, 0), WindSpeedUnit = WindSpeedUnit.MetresPerSecond };

        var result = _subject.Build(Input(Temperature(), wind));

        result.Dataset.Measurements.WindSpeedKmh.Should().BeNull();
        result.Dataset.Measurements.TemperatureC.Should().NotBeNull();
        result.OmittedReadings.Should().ContainSingle().Which.Should().Be(new OmittedReading(QuantityKind.MeanWindSpeed, OmissionReason.Unrepresentable));
    }

    [TestMethod]
    public async Task Build_UnrepresentableTemperatureScale_IsOmittedAndDatasetStillSerializes()
    {
        var input = Input(Reading(QuantityKind.RelativeHumidity, 55, SourceTime(12, 0)), Reading(QuantityKind.Temperature, 7e28, SourceTime(12, 0)));

        var result = _subject.Build(input);

        result.Dataset.Measurements.TemperatureC.Should().BeNull();
        result.OmittedReadings.Should().ContainSingle().Which.Should().Be(new OmittedReading(QuantityKind.Temperature, OmissionReason.Unrepresentable));
        using var json = new MemoryStream();
        using var xml = new MemoryStream();
        await new JsonExportSerializer().SerializeAsync(result.Dataset, json, CancellationToken.None);
        await new XmlExportSerializer().SerializeAsync(result.Dataset, xml, CancellationToken.None);
        json.Length.Should().BeGreaterThan(0);
        xml.Length.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void Build_Pressure_UsesConverterOnceAndKeepsReference()
    {
        var validator = _repository.Create<IQuantityValidator>();
        var rounding = _repository.Create<IOutputRounding>();
        var pressure = _repository.Create<IPressureConverter>();
        var period = _repository.Create<IPeriodRateConverter>();
        var wind = _repository.Create<IWindSpeedConverter>();
        validator.Setup(v => v.Validate(QuantityKind.Pressure, 1000.5)).Returns(ValidatedQuantity.Valid(1000.5m));
        pressure.Setup(p => p.FromHpa(1000.5m, PressureReference.SeaLevel)).Returns(new PressureValues(1001m, 750m, PressureReference.SeaLevel, PressureUnit.Hectopascal));
        var subject = new ObservationDatasetBuilder(validator.Object, rounding.Object, pressure.Object, period.Object, wind.Object);
        var reading = new ObservationReading { Kind = QuantityKind.Pressure, Value = 1000.5, Time = SourceTime(12, 0), PressureUnit = PressureUnit.Hectopascal, PressureReference = PressureReference.SeaLevel };

        var result = subject.Build(Input(reading));

        result.Dataset.Measurements.PressureHpa!.Value.Should().Be(1001m);
        result.Dataset.Measurements.PressureMmHg!.Value.Should().Be(750m);
        result.Dataset.Measurements.PressureHpa.PressureReference.Should().Be(PressureReference.SeaLevel);
        result.Dataset.Measurements.PressureMmHg.PressureReference.Should().Be(PressureReference.SeaLevel);
        pressure.Verify(p => p.FromHpa(It.IsAny<decimal>(), It.IsAny<PressureReference>()), Times.Once());
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void Constructor_NullDependency_ThrowsArgumentNullException(int nullIndex)
    {
        var rounding = new OutputRounding();

        FluentActions.Invoking(() => new ObservationDatasetBuilder(
                nullIndex == 0 ? null! : new QuantityValidator(),
                nullIndex == 1 ? null! : rounding,
                nullIndex == 2 ? null! : new PressureConverter(rounding),
                nullIndex == 3 ? null! : new PeriodRateConverter(),
                nullIndex == 4 ? null! : new WindSpeedConverter()))
            .Should().Throw<ArgumentNullException>();
    }
}
