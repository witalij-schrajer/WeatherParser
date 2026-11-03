using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public class ObservationReadBackTests
{
    [TestMethod]
    public async Task Json_ReadBack_RestoresTimesPeriodAndAltitude()
    {
        // Arrange
        var dataset = ObservationTestData.ValidDataset();
        dataset.Measurements.TemperatureC!.Period = new MeasurementPeriod { StartUtc = new DateTime(2026, 7, 15, 11, 50, 0, DateTimeKind.Utc), EndUtc = ObservationTestData.MeasuredAtUtc };
        dataset.Location.AltitudeM = 44m;

        using var stream = new MemoryStream();
        var serializer = new JsonExportSerializer();
        await serializer.SerializeAsync(dataset, stream, CancellationToken.None);
        stream.Position = 0;

        // Act
        var copy = JsonSerializer.Deserialize<ObservationDataset>(stream.ToArray())!;

        // Assert
        copy.MeasuredAtUtc.Should().Be(ObservationTestData.MeasuredAtUtc);
        copy.Measurements.TemperatureC!.Period!.StartUtc.Should().Be(new DateTime(2026, 7, 15, 11, 50, 0, DateTimeKind.Utc));
        copy.Measurements.TemperatureC!.Period!.EndUtc.Should().Be(ObservationTestData.MeasuredAtUtc);
        copy.Location.AltitudeM.Should().Be(44m);
    }

    [TestMethod]
    public async Task Xml_ReadBack_RestoresTimesPeriodAndAltitude()
    {
        // Arrange
        var dataset = ObservationTestData.ValidDataset();
        dataset.Measurements.TemperatureC!.Period = new MeasurementPeriod { StartUtc = new DateTime(2026, 7, 15, 11, 50, 0, DateTimeKind.Utc), EndUtc = ObservationTestData.MeasuredAtUtc };
        dataset.Location.AltitudeM = 44m;

        using var stream = new MemoryStream();
        var serializer = new XmlExportSerializer();
        await serializer.SerializeAsync(dataset, stream, CancellationToken.None);
        stream.Position = 0;

        // Act
        var copy = (ObservationDataset)new XmlSerializer(typeof(ObservationDataset)).Deserialize(new MemoryStream(stream.ToArray()))!;

        // Assert
        copy.MeasuredAtUtc.Should().Be(ObservationTestData.MeasuredAtUtc);
        copy.Measurements.TemperatureC!.Period!.StartUtc.Should().Be(new DateTime(2026, 7, 15, 11, 50, 0, DateTimeKind.Utc));
        copy.Measurements.TemperatureC!.Period!.EndUtc.Should().Be(ObservationTestData.MeasuredAtUtc);
        copy.Location.AltitudeM.Should().Be(44m);
    }

    [TestMethod]
    public void AltitudeMSpecified_SetFalse_ClearsAltitude()
    {
        // Arrange
        var location = new ObservationLocation { AltitudeM = 44m };
        location.AltitudeMSpecified = false;

        // Act & Assert
        location.AltitudeM.Should().BeNull();
        location.AltitudeMSpecified.Should().BeFalse();

        // Act & Assert
        var withAltitude = new ObservationLocation { AltitudeM = 12m };
        withAltitude.AltitudeMSpecified = true;
        withAltitude.AltitudeM.Should().Be(12m);
    }
}
