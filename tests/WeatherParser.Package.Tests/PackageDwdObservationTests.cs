using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations.Dwd;

namespace WeatherParser.Package.Tests;

[TestClass]
public sealed class PackageDwdObservationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly string PackagedSchema = Path.Combine(AppContext.BaseDirectory, "Schemas", "weatherparser-export-v1.xsd");

    [TestMethod]
    public void Package_ResolvesDwdObservationParser()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        var parser = host.Provider.GetRequiredService<IDwdObservationParser>();
        parser.Should().BeOfType<DwdObservationParser>();
    }

    [TestMethod]
    public async Task Package_ParseAsync_SyntheticStation_ProducesSchemaValidJsonAndXml()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.ArrangeCdc();

        var parser = host.Provider.GetRequiredService<IDwdObservationParser>();
        var dataset = await parser.ParseAsync("00044", CancellationToken.None);

        var xml = await host.XmlAsync(dataset);
        Validate(xml).Should().BeEmpty();

        var json = await host.JsonAsync(dataset);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("source").GetString().Should().Be("dwd:00044");
    }

    [TestMethod]
    public async Task Package_ParseAsync_MissingProduct_Continues()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.ArrangeCdc();

        var parser = host.Provider.GetRequiredService<IDwdObservationParser>();
        var dataset = await parser.ParseAsync("00044", CancellationToken.None);

        dataset.Measurements.WindSpeedKmh.Should().BeNull();
        dataset.Measurements.WindDirectionDeg.Should().BeNull();
        dataset.Measurements.RainRateMmH.Should().BeNull();
        dataset.Measurements.SolarIrradianceWm2.Should().BeNull();

        host.Handler.Requests.Count.Should().Be(8);
    }

    [TestMethod]
    public async Task Package_ParseAsync_InvalidId_ThrowsStationIdInvalidWithoutRequest()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);

        var parser = host.Provider.GetRequiredService<IDwdObservationParser>();
        Func<Task> act = () => parser.ParseAsync("A1234", CancellationToken.None);
        await act.Should().ThrowAsync<DwdStationException>()
            .Where(e => e.ErrorId == DwdStationException.StationIdInvalidErrorId);

        host.Handler.Requests.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Package_Result_MatchesExpectedGraphAndJsonXmlEquivalent()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.ArrangeCdc();

        var parser = host.Provider.GetRequiredService<IDwdObservationParser>();
        var dataset = await parser.ParseAsync("00044", CancellationToken.None);

        // Validate XML
        var xml = await host.XmlAsync(dataset);
        Validate(xml).Should().BeEmpty();
        var xmlDocument = XDocument.Parse(Encoding.UTF8.GetString(xml));
        var ns = XNamespace.Get("urn:weatherparser:export:v1");
        dataset.Source.Should().Be("dwd:00044");

        var location = dataset.Location;
        location.StationId.Should().Be("00044");
        location.Name.Should().Be("M\u00fchlheim & <S\u00fcd>");
        location.AltitudeM.Should().Be(123);
        location.Latitude.Should().Be(50.1234m);
        location.Longitude.Should().Be(9.5678m);

        var measurements = dataset.Measurements;
        measurements.TemperatureC!.Value.Should().Be(12.3m);
        measurements.TemperatureC.Quality.Should().Be("3");
        measurements.HumidityPercent!.Value.Should().Be(85m);
        measurements.HumidityPercent.Quality.Should().Be("3");
        measurements.PressureHpa!.Value.Should().Be(1001m);
        measurements.PressureHpa.Quality.Should().Be("3");
        measurements.PressureMmHg!.Value.Should().Be(751m);
        measurements.PressureMmHg.Quality.Should().Be("3");

        // Validate JSON
        var json = await host.JsonAsync(dataset);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("source").GetString().Should().Be("dwd:00044");
        doc.RootElement.GetProperty("retrieved_at").GetString().Should().Be("2026-10-21T12:00:00");
        doc.RootElement.GetProperty("measured_at").GetString().Should().Be("2026-10-21T11:50:00");
        doc.RootElement.GetProperty("timestamp_quality").GetString().Should().Be("source");

        var locationJson = doc.RootElement.GetProperty("location");
        locationJson.GetProperty("station_id").GetString().Should().Be("00044");
        locationJson.GetProperty("name").GetString().Should().Be("M\u00fchlheim & <S\u00fcd>");
        locationJson.GetProperty("altitude_m").GetInt32().Should().Be(123);
        locationJson.GetProperty("latitude").GetDecimal().Should().Be(50.1234m);
        locationJson.GetProperty("longitude").GetDecimal().Should().Be(9.5678m);

        var measurementsJson = doc.RootElement.GetProperty("measurements");
        measurementsJson.GetProperty("temperature_c").GetProperty("value").GetDecimal().Should().Be(12.3m);
        measurementsJson.GetProperty("humidity_percent").GetProperty("value").GetDecimal().Should().Be(85m);
        measurementsJson.GetProperty("pressure_hpa").GetProperty("value").GetDecimal().Should().Be(1001m);
        measurementsJson.GetProperty("pressure_mmhg").GetProperty("value").GetDecimal().Should().Be(751m);

        var quantities = new[] { "temperature_c", "humidity_percent", "pressure_hpa", "pressure_mmhg" };
        foreach (var name in quantities)
        {
            var element = measurementsJson.GetProperty(name);
            element.GetProperty("measured_at").GetString().Should().Be("2026-10-21T11:50:00");
            element.GetProperty("quality").GetString().Should().Be("3");
            if (name is "pressure_hpa" or "pressure_mmhg")
            {
                element.GetProperty("pressure_reference").GetString().Should().Be("sensor");
            }
            element.TryGetProperty("period", out _).Should().BeFalse();
        }

        // Check that absent members are not present
        measurementsJson.TryGetProperty("wind_speed_kmh", out _).Should().BeFalse();
        measurementsJson.TryGetProperty("wind_direction_deg", out _).Should().BeFalse();
        measurementsJson.TryGetProperty("rain_rate_mm_h", out _).Should().BeFalse();
        measurementsJson.TryGetProperty("solar_irradiance_w_m2", out _).Should().BeFalse();
        measurementsJson.TryGetProperty("illuminance_lux", out _).Should().BeFalse();

        // XML assertions
        xmlDocument.Root!.Name.Should().Be(ns + "observation_dataset");
        xmlDocument.Root.Element(ns + "source")!.Value.Should().Be("dwd:00044");
        xmlDocument.Root.Element(ns + "measured_at")!.Value.Should().Be("2026-10-21T11:50:00");
        xmlDocument.Root.Element(ns + "retrieved_at")!.Value.Should().Be("2026-10-21T12:00:00");
        xmlDocument.Root.Element(ns + "timestamp_quality")!.Value.Should().Be("source");

        var locationXml = xmlDocument.Root.Element(ns + "location")!;
        locationXml.Element(ns + "station_id")!.Value.Should().Be("00044");
        locationXml.Element(ns + "name")!.Value.Should().Be("M\u00fchlheim & <S\u00fcd>");
        locationXml.Element(ns + "altitude_m")!.Value.Should().Be("123");
        locationXml.Element(ns + "latitude")!.Value.Should().Be("50.1234");
        locationXml.Element(ns + "longitude")!.Value.Should().Be("9.5678");

        var measurementsXml = xmlDocument.Root.Element(ns + "measurements")!;
        measurementsXml.Element(ns + "temperature_c")!.Element(ns + "value")!.Value.Should().Be("12.3");
        measurementsXml.Element(ns + "humidity_percent")!.Element(ns + "value")!.Value.Should().Be("85");
        measurementsXml.Element(ns + "pressure_hpa")!.Element(ns + "value")!.Value.Should().Be("1001");
        measurementsXml.Element(ns + "pressure_mmhg")!.Element(ns + "value")!.Value.Should().Be("751");

        foreach (var name in quantities)
        {
            var element = measurementsXml.Element(ns + name)!;
            element.Element(ns + "measured_at")!.Value.Should().Be("2026-10-21T11:50:00");
            element.Element(ns + "quality")!.Value.Should().Be("3");
            if (name is "pressure_hpa" or "pressure_mmhg")
            {
                element.Element(ns + "pressure_reference")!.Value.Should().Be("sensor");
            }
            element.Element(ns + "period").Should().BeNull();
        }

        foreach (var name in new[] { "wind_speed_kmh", "wind_direction_deg", "rain_rate_mm_h", "solar_irradiance_w_m2", "illuminance_lux" })
        {
            measurementsXml.Element(ns + name).Should().BeNull();
        }
    }

    private static IReadOnlyList<string> Validate(byte[] document)
    {
        var events = new List<string>();
        var schemas = new XmlSchemaSet { XmlResolver = null };
        schemas.ValidationEventHandler += (_, e) => events.Add(e.Severity + ": " + e.Message);
        using (var reader = XmlReader.Create(PackagedSchema, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
        {
            schemas.Add(null, reader);
        }
        schemas.Compile();
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            ValidationType = ValidationType.Schema,
            ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings,
            Schemas = schemas
        };
        settings.ValidationEventHandler += (_, e) => events.Add(e.Severity + ": " + e.Message);
        using (var documentReader = XmlReader.Create(new MemoryStream(document), settings))
        {
            while (documentReader.Read())
            {
            }
        }
        return events;
    }
}
