using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ObservationRequiredMemberGuardTests
{
    private IJsonExportSerializer _jsonSubject = null!;
    private IXmlExportSerializer _xmlSubject = null!;
    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";

    [TestInitialize]
    public void TestInitialize()
    {
        _jsonSubject = new JsonExportSerializer();
        _xmlSubject = new XmlExportSerializer();
    }

    private static ObservationDataset BuildDataset(string? missing)
    {
        var dataset = new ObservationDataset();
        if (missing != "Source") dataset.Source = "dwd:00044";
        var location = new ObservationLocation();
        if (missing != "Location") dataset.Location = location;
        if (missing != "MeasuredAtUtc") dataset.MeasuredAtUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        if (missing != "RetrievedAtUtc") dataset.RetrievedAtUtc = new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc);
        if (missing != "TimestampQuality") dataset.TimestampQuality = TimestampQuality.Source;

        if (missing != "Location.StationId")
            location.StationId = "00044";
        if (missing != "Location.Name")
            location.Name = "Großenkneten";
        if (missing != "Location.Latitude")
            location.Latitude = 52.9336m;
        if (missing != "Location.Longitude")
            location.Longitude = 8.2370m;
        if (missing != "Location.AltitudeM")
            location.AltitudeM = 44m;

        var temp = new MeasurementValue();
        if (missing != "Temperature.Value")
            temp.Value = 20.3m;
        if (missing != "Temperature.MeasuredAtUtc")
            temp.MeasuredAtUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        if (missing != "Temperature.TimestampQuality")
            temp.TimestampQuality = TimestampQuality.Source;
        dataset.Measurements.TemperatureC = temp;

        var pressureHpa = new PressureMeasurementValue();
        if (missing != "PressureHpa.Value")
            pressureHpa.Value = 1013m;
        if (missing != "PressureHpa.MeasuredAtUtc")
            pressureHpa.MeasuredAtUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        if (missing != "PressureHpa.TimestampQuality")
            pressureHpa.TimestampQuality = TimestampQuality.Source;
        if (missing != "PressureHpa.PressureReference")
            pressureHpa.PressureReference = PressureReference.SeaLevel;
        dataset.Measurements.PressureHpa = pressureHpa;

        var pressureMmHg = new PressureMeasurementValue();
        if (missing != "PressureMmHg.Value")
            pressureMmHg.Value = 760m;
        if (missing != "PressureMmHg.MeasuredAtUtc")
            pressureMmHg.MeasuredAtUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        if (missing != "PressureMmHg.TimestampQuality")
            pressureMmHg.TimestampQuality = TimestampQuality.Source;
        if (missing != "PressureMmHg.PressureReference")
            pressureMmHg.PressureReference = PressureReference.SeaLevel;
        dataset.Measurements.PressureMmHg = pressureMmHg;

        var rainRate = new MeasurementValue { Period = new MeasurementPeriod() };
        if (missing != "RainRateMmH.Value")
            rainRate.Value = 3m;
        if (missing != "RainRateMmH.MeasuredAtUtc")
            rainRate.MeasuredAtUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        if (missing != "RainRateMmH.TimestampQuality")
            rainRate.TimestampQuality = TimestampQuality.Source;
        if (missing != "RainRateMmH.Period.StartUtc")
            rainRate.Period.StartUtc = new DateTime(2026, 7, 15, 11, 50, 0, DateTimeKind.Utc);
        if (missing != "RainRateMmH.Period.EndUtc")
            rainRate.Period.EndUtc = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        dataset.Measurements.RainRateMmH = rainRate;

        return dataset;
    }

    [TestMethod]
    [DataRow("Source")]
    [DataRow("Location")]
    [DataRow("MeasuredAtUtc")]
    [DataRow("RetrievedAtUtc")]
    [DataRow("TimestampQuality")]
    [DataRow("Location.StationId")]
    [DataRow("Location.Name")]
    [DataRow("Location.Latitude")]
    [DataRow("Location.Longitude")]
    [DataRow("Temperature.Value")]
    [DataRow("Temperature.MeasuredAtUtc")]
    [DataRow("Temperature.TimestampQuality")]
    [DataRow("PressureHpa.Value")]
    [DataRow("PressureHpa.MeasuredAtUtc")]
    [DataRow("PressureHpa.TimestampQuality")]
    [DataRow("PressureHpa.PressureReference")]
    [DataRow("PressureMmHg.Value")]
    [DataRow("PressureMmHg.MeasuredAtUtc")]
    [DataRow("PressureMmHg.TimestampQuality")]
    [DataRow("PressureMmHg.PressureReference")]
    [DataRow("RainRateMmH.Value")]
    [DataRow("RainRateMmH.MeasuredAtUtc")]
    [DataRow("RainRateMmH.TimestampQuality")]
    [DataRow("RainRateMmH.Period.StartUtc")]
    [DataRow("RainRateMmH.Period.EndUtc")]
    public async Task SerializeAsync_Json_UnassignedRequiredMember_FailsAndWritesNothing(string missing)
    {
        var dataset = BuildDataset(missing);
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _jsonSubject.SerializeAsync(dataset, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    [TestMethod]
    [DataRow("Source")]
    [DataRow("Location")]
    [DataRow("MeasuredAtUtc")]
    [DataRow("RetrievedAtUtc")]
    [DataRow("TimestampQuality")]
    [DataRow("Location.StationId")]
    [DataRow("Location.Name")]
    [DataRow("Location.Latitude")]
    [DataRow("Location.Longitude")]
    [DataRow("Temperature.Value")]
    [DataRow("Temperature.MeasuredAtUtc")]
    [DataRow("Temperature.TimestampQuality")]
    [DataRow("PressureHpa.Value")]
    [DataRow("PressureHpa.MeasuredAtUtc")]
    [DataRow("PressureHpa.TimestampQuality")]
    [DataRow("PressureHpa.PressureReference")]
    [DataRow("PressureMmHg.Value")]
    [DataRow("PressureMmHg.MeasuredAtUtc")]
    [DataRow("PressureMmHg.TimestampQuality")]
    [DataRow("PressureMmHg.PressureReference")]
    [DataRow("RainRateMmH.Value")]
    [DataRow("RainRateMmH.MeasuredAtUtc")]
    [DataRow("RainRateMmH.TimestampQuality")]
    [DataRow("RainRateMmH.Period.StartUtc")]
    [DataRow("RainRateMmH.Period.EndUtc")]
    public async Task SerializeAsync_Xml_UnassignedRequiredMember_FailsAndWritesNothing(string missing)
    {
        var dataset = BuildDataset(missing);
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _xmlSubject.SerializeAsync(dataset, stream, CancellationToken.None))
            .Should().ThrowAsync<ExportSerializationException>();
        stream.Length.Should().Be(0);
    }

    [TestMethod]
    public async Task SerializeAsync_Json_CompleteGraph_Succeeds()
    {
        var dataset = BuildDataset(null);
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _jsonSubject.SerializeAsync(dataset, stream, CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_CompleteGraph_Succeeds()
    {
        var dataset = BuildDataset(null);
        using var stream = new MemoryStream();
        await FluentActions.Awaiting(() => _xmlSubject.SerializeAsync(dataset, stream, CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task SerializeAsync_Json_AssignedZeroValuedEnums_AreWritten()
    {
        var dataset = BuildDataset(null);
        dataset.Measurements.PressureHpa!.PressureReference = PressureReference.Sensor;
        dataset.Measurements.PressureMmHg!.PressureReference = PressureReference.Sensor;
        dataset.TimestampQuality = TimestampQuality.Source;

        using var stream = new MemoryStream();
        await _jsonSubject.SerializeAsync(dataset, stream, CancellationToken.None);

        stream.Position = 0;
        using var doc = JsonDocument.Parse(stream);
        doc.RootElement.GetProperty("measurements").GetProperty("pressure_hpa").GetProperty("pressure_reference").GetString().Should().Be("sensor");
        doc.RootElement.GetProperty("measurements").GetProperty("temperature_c").GetProperty("timestamp_quality").GetString().Should().Be("source");
    }

    [TestMethod]
    public async Task SerializeAsync_Xml_AssignedZeroValuedEnums_AreWritten()
    {
        var dataset = BuildDataset(null);
        dataset.Measurements.PressureHpa!.PressureReference = PressureReference.Sensor;
        dataset.Measurements.PressureMmHg!.PressureReference = PressureReference.Sensor;
        dataset.TimestampQuality = TimestampQuality.Source;

        using var stream = new MemoryStream();
        await _xmlSubject.SerializeAsync(dataset, stream, CancellationToken.None);

        stream.Position = 0;
        var doc = XDocument.Load(stream);
        var measurements = doc.Root!.Element(Ns + "measurements")!;
        measurements.Element(Ns + "pressure_hpa")!.Element(Ns + "pressure_reference")!.Value.Should().Be("sensor");
        measurements.Element(Ns + "temperature_c")!.Element(Ns + "timestamp_quality")!.Value.Should().Be("source");
    }
}
