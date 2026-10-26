using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Time;
using WeatherParser.Parsing.Tests.Schemas;

namespace WeatherParser.Parsing.Tests.Forecasts;

[TestClass]
public sealed class ForecastRunFreshnessMarkerTests
{
    private IForecastRunBuilder _builder = null!;
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _builder = new ForecastRunBuilder(
            new QuantityValidator(),
            new OutputRounding(),
            new PressureConverter(new OutputRounding()),
            new PeriodRateConverter(),
            new WindSpeedConverter());
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    private static DateTime Utc(int hour, int minute) => new(2026, 7, 15, hour, minute, 0, DateTimeKind.Utc);

    private ForecastRun Build(bool? runAgeExceeded)
    {
        var input = new ForecastRunInput
        {
            Product = MosmixProduct.MosmixL,
            MosmixId = "10637",
            Location = new ForecastLocation { Name = "FRANKFURT/M", Latitude = 50.05m, Longitude = 8.6m },
            IssuedAtUtc = Utc(9, 0),
            RetrievedAtUtc = Utc(10, 12),
            Timeline = new[] { Utc(12, 0), Utc(13, 0) },
            Series = new[]
            {
                new ForecastSeries
                {
                    Kind = QuantityKind.Temperature,
                    Values = new double?[] { 20.0, 21.0 },
                    Product = MosmixProduct.MosmixL,
                    MosmixId = "10637",
                    IssuedAtUtc = Utc(9, 0)
                }
            },
            RunAgeExceeded = runAgeExceeded
        };

        return _builder.Build(input).Run;
    }

    private async Task<string> JsonAsync(ForecastRun run)
    {
        using var stream = new MemoryStream();
        await _json.SerializeAsync(run, stream, CancellationToken.None);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private async Task<byte[]> XmlAsync(ForecastRun run)
    {
        using var stream = new MemoryStream();
        await _xml.SerializeAsync(run, stream, CancellationToken.None);
        return stream.ToArray();
    }

    [TestMethod]
    public void Build_StaleInput_RunCarriesMarker()
    {
        // Arrange / Act
        var run = Build(true);

        // Assert
        run.RunAgeExceeded.Should().BeTrue();
    }

    [TestMethod]
    public async Task Build_FreshInput_MarkerFalse()
    {
        // Arrange / Act
        var run = Build(false);
        var json = await JsonAsync(run);
        var xmlBytes = await XmlAsync(run);
        var xml = System.Text.Encoding.UTF8.GetString(xmlBytes);

        // Assert
        run.RunAgeExceeded.Should().BeFalse();

        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("run_age_exceeded", out _).Should().BeTrue();
        document.RootElement.GetProperty("run_age_exceeded").ValueKind.Should().Be(JsonValueKind.False);

        var xdoc = XDocument.Parse(xml);
        var markerElement = xdoc.Root!.Elements().FirstOrDefault(e => e.Name.LocalName == "run_age_exceeded");
        markerElement.Should().NotBeNull();
        markerElement!.Value.Should().Be("false");
    }

    [TestMethod]
    public async Task Build_UnsetInput_MarkerOmitted()
    {
        // Arrange / Act
        var run = Build(null);
        var json = await JsonAsync(run);
        var xmlBytes = await XmlAsync(run);
        var xml = System.Text.Encoding.UTF8.GetString(xmlBytes);

        // Assert
        run.RunAgeExceeded.Should().BeNull();

        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("run_age_exceeded", out _).Should().BeFalse();

        var xdoc = XDocument.Parse(xml);
        var markerElements = xdoc.Root!.Elements().Where(e => e.Name.LocalName == "run_age_exceeded").ToList();
        markerElements.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Serialize_Json_MemberAfterForecasts()
    {
        // Arrange / Act
        var run = Build(true);
        var json = await JsonAsync(run);

        // Assert
        using var document = JsonDocument.Parse(json);
        var propertyNames = document.RootElement.EnumerateObject().Select(p => p.Name).ToList();
        propertyNames.Should().Equal("schema_version", "dataset_type", "source", "mosmix_id", "location", "product", "issued_at", "retrieved_at", "forecasts", "run_age_exceeded");
    }

    [TestMethod]
    public async Task Serialize_Xml_ElementAfterForecasts()
    {
        // Arrange / Act
        var run = Build(true);
        var xmlBytes = await XmlAsync(run);
        var xml = System.Text.Encoding.UTF8.GetString(xmlBytes);

        // Assert
        var xdoc = XDocument.Parse(xml);
        var elementNames = xdoc.Root!.Elements().Select(e => e.Name.LocalName).ToList();
        elementNames.Should().Equal("schema_version", "dataset_type", "source", "mosmix_id", "location", "product", "issued_at", "retrieved_at", "forecasts", "run_age_exceeded");
    }

    [TestMethod]
    public async Task Serialize_MarkerJsonXmlEquivalent()
    {
        // Arrange / Act
        var run = Build(true);
        var json = await JsonAsync(run);
        var xmlBytes = await XmlAsync(run);
        var xml = System.Text.Encoding.UTF8.GetString(xmlBytes);

        // Assert
        using var document = JsonDocument.Parse(json);
        var jsonValue = document.RootElement.GetProperty("run_age_exceeded").GetBoolean();
        jsonValue.Should().BeTrue();

        var xdoc = XDocument.Parse(xml);
        var markerElement = xdoc.Root!.Elements().First(e => e.Name.LocalName == "run_age_exceeded");
        markerElement.Value.Should().Be("true");
    }

    [TestMethod]
    public async Task Schema_RunWithMarker_Valid()
    {
        // Arrange / Act
        var xmlBytesTrue = await XmlAsync(Build(true));
        var xmlBytesFalse = await XmlAsync(Build(false));

        // Assert
        ExportSchemaValidation.Validate(xmlBytesTrue).Should().BeEmpty();
        ExportSchemaValidation.Validate(xmlBytesFalse).Should().BeEmpty();
    }

    [TestMethod]
    public async Task Schema_MarkerBeforeForecasts_Invalid()
    {
        // Arrange / Act
        var xmlBytes = await XmlAsync(Build(true));
        var xdoc = XDocument.Parse(System.Text.Encoding.UTF8.GetString(xmlBytes));
        var forecastsElement = xdoc.Root!.Elements().First(e => e.Name.LocalName == "forecasts");
        var markerElement = xdoc.Root!.Elements().First(e => e.Name.LocalName == "run_age_exceeded");
        markerElement.Remove();
        forecastsElement.AddBeforeSelf(markerElement);
        var bytes = System.Text.Encoding.UTF8.GetBytes(xdoc.ToString());

        // Assert
        ExportSchemaValidation.Validate(bytes).Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task Deserialize_DocumentWithoutMarker_StillValid()
    {
        // Arrange / Act
        var xmlBytes = await XmlAsync(Build(null));
        var events = ExportSchemaValidation.Validate(xmlBytes);

        // Assert
        events.Should().BeEmpty();

        var serializer = new XmlSerializer(typeof(ForecastRun));
        using var stream = new MemoryStream(xmlBytes);
        using var reader = XmlReader.Create(stream, ExportSchemaValidation.SecureReaderSettings());
        var run = (ForecastRun)serializer.Deserialize(reader);
        run.RunAgeExceeded.Should().BeNull();
    }

    [TestMethod]
    public void RunAgeExceededSpecified_SetFalse_ClearsValue()
    {
        // Arrange
        var run = Build(true);
        run.RunAgeExceededSpecified = false;

        // Act / Assert
        run.RunAgeExceeded.Should().BeNull();
    }

    [TestMethod]
    public void RunAgeExceededSpecified_SetTrue_KeepsValue()
    {
        // Arrange
        var run = Build(false);
        run.RunAgeExceededSpecified = true;

        // Act / Assert
        run.RunAgeExceeded.Should().BeFalse();
    }

    [TestMethod]
    public async Task Deserialize_DocumentWithMarker_ReadsValue()
    {
        // Arrange
        var run = Build(true);
        var xmlBytes = await XmlAsync(run);

        // Act
        var serializer = new XmlSerializer(typeof(ForecastRun));
        using var stream = new MemoryStream(xmlBytes);
        using var reader = XmlReader.Create(stream, ExportSchemaValidation.SecureReaderSettings());
        var restored = (ForecastRun)serializer.Deserialize(reader);

        // Assert
        restored.RunAgeExceeded.Should().BeTrue();
    }
}
