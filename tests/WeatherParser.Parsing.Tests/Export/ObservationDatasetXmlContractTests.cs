using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

/// <summary>
/// Tests for the XML serialization contract of <see cref="ObservationDataset"/>.
/// </summary>
[TestClass]
public sealed class ObservationDatasetXmlContractTests
{
    private static readonly XNamespace Ns = "urn:weatherparser:export:v1";
    private IXmlExportSerializer _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new XmlExportSerializer();
    }

    [TestMethod]
    public async Task SerializeAsync_MissingTemperature_OmitsTemperatureElement()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Measurements.TemperatureC = null;
        using var stream = await SerializeToStream(dataset);
        var doc = XDocument.Load(stream);

        var root = doc.Root!;
        root.Name.LocalName.Should().Be("observation_dataset");
        root.Name.NamespaceName.Should().Be(Ns.NamespaceName);

        var measurements = root.Element(Ns + "measurements")!;
        measurements.Should().NotBeNull();

        var tempElement = measurements.Element(Ns + "temperature_c");
        tempElement.Should().BeNull();

        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        doc.Descendants().Attributes(xsi + "nil").Should().BeEmpty();
    }

    [TestMethod]
    public async Task SerializeAsync_ZeroTemperature_WritesZeroValue()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Measurements.TemperatureC = ObservationTestData.Value(0.0m, ObservationTestData.MeasuredAtUtc);
        using var stream = await SerializeToStream(dataset);
        var doc = XDocument.Load(stream);

        var valueElement = doc.Root!
            .Element(Ns + "measurements")!
            .Element(Ns + "temperature_c")!
            .Element(Ns + "value")!;

        decimal.Parse(valueElement.Value, System.Globalization.CultureInfo.InvariantCulture).Should().Be(0m);
    }

    [TestMethod]
    public async Task SerializeAsync_NonZeroTemperature_WritesValue()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Measurements.TemperatureC = ObservationTestData.Value(-3.5m, ObservationTestData.MeasuredAtUtc);
        using var stream = await SerializeToStream(dataset);
        var doc = XDocument.Load(stream);

        var valueElement = doc.Root!
            .Element(Ns + "measurements")!
            .Element(Ns + "temperature_c")!
            .Element(Ns + "value")!;

        decimal.Parse(valueElement.Value, System.Globalization.CultureInfo.InvariantCulture).Should().Be(-3.5m);
    }

    private async Task<MemoryStream> SerializeToStream(ObservationDataset dataset)
    {
        var stream = new MemoryStream();
        await _subject.SerializeAsync(dataset, stream, default);
        stream.Position = 0;
        return stream;
    }
}
