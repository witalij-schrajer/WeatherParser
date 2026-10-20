using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Export;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Tests.Schemas;

[TestClass]
public sealed class RealExportValidationTests
{
    private IXmlExportSerializer _xml = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _xml = new XmlExportSerializer();
    }

    [TestMethod]
    [DynamicData(nameof(AllRootsTestData.Roots), typeof(AllRootsTestData))]
    public async Task RealExports_OfAllFiveRoots_ValidateAgainstSchema(string rootName)
    {
        var bytes = await AllRootsTestData.XmlBytesAsync(AllRootsTestData.Build(rootName), _xml);
        ExportSchemaValidation.Validate(bytes).Should().BeEmpty();
    }

    [TestMethod]
    public async Task RealExport_WithEmptyCollectionsAndOptionalObjects_ValidatesAgainstSchema()
    {
        var document = new ExportDocument
        {
            Observations = { },
            ForecastRuns = { },
            Errors = { }
        };
        var bytes = await AllRootsTestData.XmlBytesAsync(document, _xml);
        ExportSchemaValidation.Validate(bytes).Should().BeEmpty();

        var run = ForecastTestData.ValidRun();
        bytes = await AllRootsTestData.XmlBytesAsync(run, _xml);
        ExportSchemaValidation.Validate(bytes).Should().BeEmpty();

        var dataset = ObservationTestData.ValidDataset();
        dataset.Measurements.HumidityPercent = null;
        bytes = await AllRootsTestData.XmlBytesAsync(dataset, _xml);
        ExportSchemaValidation.Validate(bytes).Should().BeEmpty();
    }

    [TestMethod]
    public async Task RealExport_WithPressureAndPeriodMembers_ValidatesAgainstSchema()
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
        dataset.Measurements.TemperatureC!.Period = new MeasurementPeriod
        {
            StartUtc = ObservationTestData.MeasuredAtUtc.AddHours(-1),
            EndUtc = ObservationTestData.MeasuredAtUtc
        };
        dataset.Measurements.TemperatureC.Quality = "QN_9:3";

        var bytes = await AllRootsTestData.XmlBytesAsync(dataset, _xml);
        ExportSchemaValidation.Validate(bytes).Should().BeEmpty();
    }
}
