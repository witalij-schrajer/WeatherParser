using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Tests.Schemas;
using FluentAssertions;

namespace WeatherParser.Parsing.Tests.Selection;

[TestClass]
public sealed class SelectionExportContractTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 21, 12, 0, 0, 500, TimeSpan.Zero);
    private static readonly XNamespace Ns = ExportNamespaces.V1;

    [TestMethod]
    public async Task CdcSelection_SerializesToEquivalentJsonAndXml()
    {
        using var fixture = new SelectionPipelineFixture(Start);
        fixture.ArrangeCdc();

        var culture = CultureInfo.CurrentCulture;
        var uiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

        try
        {
            var cdcResult = await fixture.SelectCdcAsync(SelectionPipelineFixture.Parameters());
            var jsonBytes = await fixture.JsonAsync(cdcResult);
            var xmlBytes = await fixture.XmlAsync(cdcResult);

            var json = Json(jsonBytes);
            json.GetProperty("source").GetString().Should().Be("dwd:cdc-10min-now");
            json.GetProperty("retrieved_at").GetString().Should().Be("2026-10-21T12:00:00");
            var stations = json.GetProperty("stations");
            stations.GetArrayLength().Should().Be(1);
            var station = stations[0];
            station.GetProperty("station_id").GetString().Should().Be("00044");
            station.GetProperty("name").GetString().Should().Be(SelectionPipelineFixture.CdcName);
            station.GetProperty("latitude").GetDecimal().Should().Be(50.1234m);
            station.GetProperty("altitude_m").GetDecimal().Should().Be(123m);
            var measurements = station.GetProperty("available_measurements");
            measurements.EnumerateArray().Select(e => e.GetString()!).ToArray().Should().Equal(
                "humidity_percent",
                "pressure_hpa",
                "pressure_mmhg",
                "temperature_c");

            var jsonText = Encoding.UTF8.GetString(jsonBytes);
            jsonText.Should().Contain("50.1234");
            jsonText.Should().NotContain("50,1234");

            var xml = Xml(xmlBytes);
            xml.Name.LocalName.Should().Be("cdc_station_list");
            xml.Element(Ns + "retrieved_at")!.Value.Should().Be("2026-10-21T12:00:00");
            var stationElement = xml.Element(Ns + "stations")!.Elements(Ns + "station").Single();
            stationElement.Element(Ns + "station_id")!.Value.Should().Be("00044");
            stationElement.Element(Ns + "name")!.Value.Should().Be(SelectionPipelineFixture.CdcName);
            stationElement.Element(Ns + "latitude")!.Value.Should().Be("50.1234");
            stationElement.Element(Ns + "altitude_m")!.Value.Should().Be("123");
            var measurementElements = stationElement.Element(Ns + "available_measurements")!.Elements(Ns + "measurement");
            measurementElements.Select(e => e.Value).ToArray().Should().Equal(
                "humidity_percent",
                "pressure_hpa",
                "pressure_mmhg",
                "temperature_c");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
        }
    }

    [TestMethod]
    public async Task MosmixSelection_SerializesToEquivalentJsonAndXml_WithEvidenceTimestampsPresentAndOmitted()
    {
        using var fixture = new SelectionPipelineFixture(Start);
        await fixture.ArrangeMosmixAsync();

        fixture.Handler.OnRequest = uri =>
        {
            if (uri.Contains("MOSMIX_L_LATEST_X123A"))
            {
                fixture.Clock.Advance(TimeSpan.FromSeconds(5));
            }
        };

        var mosmixResult = await fixture.SelectMosmixAsync(SelectionPipelineFixture.Parameters());
        var jsonBytes = await fixture.JsonAsync(mosmixResult);
        var xmlBytes = await fixture.XmlAsync(mosmixResult);

        var json = Json(jsonBytes);
        json.GetProperty("source").GetString().Should().Be("dwd:mosmix-stationskatalog");
        json.GetProperty("retrieved_at").GetString().Should().Be("2026-10-21T12:00:00");

        var locations = json.GetProperty("locations");
        locations.GetArrayLength().Should().Be(2);

        var loc0 = locations[0];
        loc0.GetProperty("mosmix_id").GetString().Should().Be("X123A");

        var products0 = loc0.GetProperty("products");
        products0.GetArrayLength().Should().Be(2);

        var prodS = products0[0];
        prodS.GetProperty("product").GetString().Should().Be("MOSMIX_S");
        prodS.GetProperty("availability").GetString().Should().Be("available");
        var elementsS = prodS.GetProperty("available_forecast_elements");
        Strings(elementsS).Should().Equal("FF", "TTT");
        prodS.GetProperty("evidence_retrieved_at").GetString().Should().Be("2026-10-21T10:00:00");
        prodS.GetProperty("evidence_issued_at").GetString().Should().Be("2026-10-21T03:00:00");

        var prodL = products0[1];
        prodL.GetProperty("product").GetString().Should().Be("MOSMIX_L");
        prodL.GetProperty("availability").GetString().Should().Be("available");
        var elementsL = prodL.GetProperty("available_forecast_elements");
        Strings(elementsL).Should().Equal("FF", "TTT");
        prodL.GetProperty("evidence_retrieved_at").GetString().Should().Be("2026-10-21T12:00:05");
        prodL.GetProperty("evidence_issued_at").GetString().Should().Be("2026-10-21T03:00:00");

        var loc1 = locations[1];
        loc1.GetProperty("mosmix_id").GetString().Should().Be("01234");

        var products1 = loc1.GetProperty("products");
        products1.GetArrayLength().Should().Be(2);

        var prodS1 = products1[0];
        prodS1.GetProperty("product").GetString().Should().Be("MOSMIX_S");
        prodS1.GetProperty("availability").GetString().Should().Be("unavailable");
        prodS1.GetProperty("available_forecast_elements").GetArrayLength().Should().Be(0);
        prodS1.GetProperty("evidence_retrieved_at").GetString().Should().Be("2026-10-21T10:00:00");
        prodS1.GetProperty("evidence_issued_at").GetString().Should().Be("2026-10-21T03:00:00");

        var prodL1 = products1[1];
        prodL1.GetProperty("product").GetString().Should().Be("MOSMIX_L");
        prodL1.GetProperty("availability").GetString().Should().Be("unavailable");
        prodL1.GetProperty("available_forecast_elements").GetArrayLength().Should().Be(0);
        prodL1.TryGetProperty("evidence_retrieved_at", out _).Should().BeFalse();
        prodL1.TryGetProperty("evidence_issued_at", out _).Should().BeFalse();

        var xml = Xml(xmlBytes);
        var locElements = xml.Element(Ns + "locations")!.Elements(Ns + "location").ToArray();
        locElements.Length.Should().Be(2);

        var loc0Element = locElements[0];
        loc0Element.Element(Ns + "mosmix_id")!.Value.Should().Be("X123A");

        var products0Elements = loc0Element.Element(Ns + "products")!.Elements(Ns + "product_availability").ToArray();
        products0Elements.Length.Should().Be(2);

        var prodSElement = products0Elements[0];
        prodSElement.Element(Ns + "product")!.Value.Should().Be("MOSMIX_S");
        prodSElement.Element(Ns + "availability")!.Value.Should().Be("available");
        var elementsSElement = prodSElement.Element(Ns + "available_forecast_elements")!.Elements(Ns + "element").Select(e => e.Value).ToArray();
        elementsSElement.Should().Equal("FF", "TTT");
        prodSElement.Element(Ns + "evidence_retrieved_at")!.Value.Should().Be("2026-10-21T10:00:00");
        prodSElement.Element(Ns + "evidence_issued_at")!.Value.Should().Be("2026-10-21T03:00:00");

        var prodLElement = products0Elements[1];
        prodLElement.Element(Ns + "product")!.Value.Should().Be("MOSMIX_L");
        prodLElement.Element(Ns + "availability")!.Value.Should().Be("available");
        var elementsLElement = prodLElement.Element(Ns + "available_forecast_elements")!.Elements(Ns + "element").Select(e => e.Value).ToArray();
        elementsLElement.Should().Equal("FF", "TTT");
        prodLElement.Element(Ns + "evidence_retrieved_at")!.Value.Should().Be("2026-10-21T12:00:05");
        prodLElement.Element(Ns + "evidence_issued_at")!.Value.Should().Be("2026-10-21T03:00:00");

        var loc1Element = locElements[1];
        loc1Element.Element(Ns + "mosmix_id")!.Value.Should().Be("01234");

        var products1Elements = loc1Element.Element(Ns + "products")!.Elements(Ns + "product_availability").ToArray();
        products1Elements.Length.Should().Be(2);

        var prodS1Element = products1Elements[0];
        prodS1Element.Element(Ns + "product")!.Value.Should().Be("MOSMIX_S");
        prodS1Element.Element(Ns + "availability")!.Value.Should().Be("unavailable");
        var elementsS1Element = prodS1Element.Element(Ns + "available_forecast_elements")!.Elements(Ns + "element").ToArray();
        elementsS1Element.Should().BeEmpty();
        prodS1Element.Element(Ns + "evidence_retrieved_at")!.Value.Should().Be("2026-10-21T10:00:00");
        prodS1Element.Element(Ns + "evidence_issued_at")!.Value.Should().Be("2026-10-21T03:00:00");

        var prodL1Element = products1Elements[1];
        prodL1Element.Element(Ns + "product")!.Value.Should().Be("MOSMIX_L");
        prodL1Element.Element(Ns + "availability")!.Value.Should().Be("unavailable");
        var elementsL1Element = prodL1Element.Element(Ns + "available_forecast_elements")!.Elements(Ns + "element").ToArray();
        elementsL1Element.Should().BeEmpty();
        prodL1Element.Element(Ns + "evidence_retrieved_at").Should().BeNull();
        prodL1Element.Element(Ns + "evidence_issued_at").Should().BeNull();
    }

    [TestMethod]
    public async Task Selections_XmlValidatesAgainstV1Schema()
    {
        using var fixture = new SelectionPipelineFixture(Start);
        fixture.ArrangeCdc();
        await fixture.ArrangeMosmixAsync();

        var cdcResult = await fixture.SelectCdcAsync(SelectionPipelineFixture.Parameters());
        var mosmixResult = await fixture.SelectMosmixAsync(SelectionPipelineFixture.Parameters());

        var cdcXmlBytes = await fixture.XmlAsync(cdcResult);
        var mosmixXmlBytes = await fixture.XmlAsync(mosmixResult);

        ExportSchemaValidation.Validate(cdcXmlBytes).Should().BeEmpty();
        ExportSchemaValidation.Validate(mosmixXmlBytes).Should().BeEmpty();

        TimestampSemantics.Violations(cdcXmlBytes).Should().BeEmpty();
        TimestampSemantics.Violations(mosmixXmlBytes).Should().BeEmpty();
    }

    [TestMethod]
    public async Task EmptySelections_SerializeAsValidEmptyLists()
    {
        using var fixture = new SelectionPipelineFixture(Start);
        fixture.ArrangeCdc();
        await fixture.ArrangeMosmixAsync();

        var cdcResult = await fixture.SelectCdcAsync(SelectionPipelineFixture.Parameters(-50m));
        var mosmixResult = await fixture.SelectMosmixAsync(SelectionPipelineFixture.Parameters(-50m));

        var jsonBytesCdc = await fixture.JsonAsync(cdcResult);
        var jsonBytesMosmix = await fixture.JsonAsync(mosmixResult);

        var jsonCdc = Json(jsonBytesCdc);
        var jsonMosmix = Json(jsonBytesMosmix);

        jsonCdc.GetProperty("stations").GetArrayLength().Should().Be(0);
        jsonMosmix.GetProperty("locations").GetArrayLength().Should().Be(0);

        var xmlBytesCdc = await fixture.XmlAsync(cdcResult);
        var xmlBytesMosmix = await fixture.XmlAsync(mosmixResult);

        ExportSchemaValidation.Validate(xmlBytesCdc).Should().BeEmpty();
        ExportSchemaValidation.Validate(xmlBytesMosmix).Should().BeEmpty();

        fixture.CdcLogger.Count("SELECTION-EMPTY").Should().Be(1);
        fixture.MosmixLogger.Count("SELECTION-EMPTY").Should().Be(1);
    }

    [TestMethod]
    public async Task Selections_PreserveLeadingZeroIdsUmlautsAndEscaping()
    {
        using var fixture = new SelectionPipelineFixture(Start);
        fixture.ArrangeCdc();
        await fixture.ArrangeMosmixAsync();

        var cdcResult = await fixture.SelectCdcAsync(SelectionPipelineFixture.Parameters());
        var mosmixResult = await fixture.SelectMosmixAsync(SelectionPipelineFixture.Parameters());

        var jsonBytesCdc = await fixture.JsonAsync(cdcResult);
        var jsonBytesMosmix = await fixture.JsonAsync(mosmixResult);

        var jsonCdc = Json(jsonBytesCdc);
        var jsonMosmix = Json(jsonBytesMosmix);

        var station = jsonCdc.GetProperty("stations")[0];
        station.GetProperty("station_id").ValueKind.Should().Be(JsonValueKind.String);
        station.GetProperty("station_id").GetString().Should().Be("00044");
        station.GetProperty("name").GetString().Should().Be(SelectionPipelineFixture.CdcName);

        var locations = jsonMosmix.GetProperty("locations");
        locations[1].GetProperty("mosmix_id").ValueKind.Should().Be(JsonValueKind.String);
        locations[1].GetProperty("mosmix_id").GetString().Should().Be("01234");
        locations[0].GetProperty("name").GetString().Should().Be(SelectionPipelineFixture.MosmixName);

        var xmlBytesCdc = await fixture.XmlAsync(cdcResult);
        var xmlBytesMosmix = await fixture.XmlAsync(mosmixResult);

        var xmlCdc = Xml(xmlBytesCdc);
        var xmlMosmix = Xml(xmlBytesMosmix);

        var xmlCdcText = Encoding.UTF8.GetString(xmlBytesCdc);
        var xmlMosmixText = Encoding.UTF8.GetString(xmlBytesMosmix);

        xmlCdcText.Should().Contain("&amp;");
        xmlCdcText.Should().Contain("&lt;");
        xmlMosmixText.Should().Contain("&amp;");
        xmlMosmixText.Should().Contain("&lt;");

        xmlCdc.Element(Ns + "stations")!.Elements(Ns + "station").Single().Element(Ns + "name")!.Value.Should().Be(SelectionPipelineFixture.CdcName);
        xmlMosmix.Element(Ns + "locations")!.Elements(Ns + "location").First().Element(Ns + "name")!.Value.Should().Be(SelectionPipelineFixture.MosmixName);
    }

    [TestMethod]
    public async Task Selections_DeterministicDocuments()
    {
        using var fixture = new SelectionPipelineFixture(Start);
        fixture.ArrangeCdc();
        await fixture.ArrangeMosmixAsync();

        var cdcResult1 = await fixture.SelectCdcAsync(SelectionPipelineFixture.Parameters());
        var mosmixResult1 = await fixture.SelectMosmixAsync(SelectionPipelineFixture.Parameters());

        var jsonBytesCdc1 = await fixture.JsonAsync(cdcResult1);
        var xmlBytesCdc1 = await fixture.XmlAsync(cdcResult1);

        var cdcResult2 = await fixture.SelectCdcAsync(SelectionPipelineFixture.Parameters());
        var mosmixResult2 = await fixture.SelectMosmixAsync(SelectionPipelineFixture.Parameters());

        var jsonBytesCdc2 = await fixture.JsonAsync(cdcResult2);
        var xmlBytesCdc2 = await fixture.XmlAsync(cdcResult2);

        jsonBytesCdc1.Should().Equal(jsonBytesCdc2);
        xmlBytesCdc1.Should().Equal(xmlBytesCdc2);

        var jsonBytesMosmix1 = await fixture.JsonAsync(mosmixResult1);
        var xmlBytesMosmix1 = await fixture.XmlAsync(mosmixResult1);

        var jsonBytesMosmix2 = await fixture.JsonAsync(mosmixResult2);
        var xmlBytesMosmix2 = await fixture.XmlAsync(mosmixResult2);

        jsonBytesMosmix1.Should().Equal(jsonBytesMosmix2);
        xmlBytesMosmix1.Should().Equal(xmlBytesMosmix2);
    }

    [TestMethod]
    public async Task Selections_CdcActivityDate_FromReferenceInstantUtcDate()
    {
        using var fixture1 = new SelectionPipelineFixture(new DateTimeOffset(2026, 10, 21, 23, 30, 0, TimeSpan.Zero));
        fixture1.ArrangeCdc(toDate: "20261020", messDatum: "202610212320");
        var result1 = await fixture1.SelectCdcAsync(SelectionPipelineFixture.Parameters());
        result1.Stations.Should().ContainSingle();

        using var fixture2 = new SelectionPipelineFixture(new DateTimeOffset(2026, 10, 21, 23, 30, 0, TimeSpan.Zero));
        fixture2.ArrangeCdc(toDate: "20261019", messDatum: "202610212320");
        var result2 = await fixture2.SelectCdcAsync(SelectionPipelineFixture.Parameters());
        result2.Stations.Should().BeEmpty();
    }

    private static JsonElement Json(byte[] bytes) => JsonDocument.Parse(bytes).RootElement;

    private static XElement Xml(byte[] bytes) => XDocument.Load(new MemoryStream(bytes)).Root!;

    private static string[] Strings(JsonElement array) => array.EnumerateArray().Select(e => e.GetString()!).ToArray();
}
