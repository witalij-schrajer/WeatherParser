using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using WeatherParser.Parsing.Forecasts.Mosmix;

namespace WeatherParser.Parsing.Tests.Forecasts.Mosmix;

[TestClass]
public class MosmixForecastDocumentReaderStructureTests
{
    private IMosmixForecastDocumentReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new MosmixForecastDocumentReader();
    }

    private async Task<MosmixForecastDocument> Read(string kml, MosmixForecastReaderLimits? limits = null)
    {
        var reader = limits is null ? _subject : new MosmixForecastDocumentReader(limits);
        return await reader.ReadSingleLocationAsync(new MemoryStream(MosmixForecastKmzBuilder.Kmz(kml)), "10637", CancellationToken.None);
    }

    private async Task ShouldFail(string kml, string errorId, MosmixForecastReaderLimits? limits = null)
    {
        await FluentActions.Awaiting(() => Read(kml, limits))
            .Should()
            .ThrowAsync<MosmixForecastFormatException>()
            .Where(e => e.ErrorId == errorId);
    }

    private static string WithProductDefinition(string productDefinition) => MosmixForecastKmzBuilder.Document(productDefinition, MosmixForecastKmzBuilder.Placemark("10637"));

    private static string WithForecasts(params string[] forecasts) => MosmixForecastKmzBuilder.Single(forecasts: forecasts);

    private static string Steps(params string[] instants)
    {
        var sb = new StringBuilder();
        foreach (var instant in instants)
        {
            sb.Append($"          <dwd:TimeStep>{instant}</dwd:TimeStep>\n");
        }
        return sb.ToString();
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_ValidFile_DocumentWithTimelineLocationSeries()
    {
        var doc = await Read(MosmixForecastKmzBuilder.Single());

        doc.MosmixId.Should().Be("10637");
        doc.IssuedAtUtc.Should().Be(new DateTime(2026, 10, 26, 15, 0, 0, DateTimeKind.Utc));
        doc.Timeline.Should().Equal(
            new DateTime(2026, 10, 26, 16, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 26, 17, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 26, 18, 0, 0, DateTimeKind.Utc));
        doc.LocationName.Should().Be("BEISPIELSTADT");
        doc.Latitude.Should().Be(50.5m);
        doc.Longitude.Should().Be(9.6m);
        doc.AltitudeM.Should().Be(250.0m);

        doc.Series.Should().HaveCount(2);
        doc.Series[0].ElementName.Should().Be("TTT");
        doc.Series[0].Values.Should().Equal("281.15", "281.05", null);
        doc.Series[1].ElementName.Should().Be("FF");
        doc.Series[1].Values.Should().Equal("3.10", null, "2.60");
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_WrongDwdNamespace_FailsFormatInvalid()
    {
        var kml = MosmixForecastKmzBuilder.Single().Replace(MosmixForecastKmzBuilder.DwdNs, "https://example.invalid/dwd.xsd");
        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_WrongKmlNamespace_FailsFormatInvalid()
    {
        var kml = MosmixForecastKmzBuilder.Single().Replace(MosmixForecastKmzBuilder.KmlNs, "http://example.invalid/kml");
        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_ProductIdNotMosmix_FailsFormatInvalid()
    {
        var kml = WithProductDefinition(MosmixForecastKmzBuilder.ProductDefinition(productId: "ICON_MOS"));

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_IssueTimeMissing_FailsIssueTimeMissing()
    {
        var kml = WithProductDefinition(MosmixForecastKmzBuilder.ProductDefinition(issueTime: null));

        await ShouldFail(kml, MosmixForecastErrorIds.IssueTimeMissing);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_IssueTimeTwice_FailsFormatInvalid()
    {
        var kml = WithProductDefinition(MosmixForecastKmzBuilder.ProductDefinition(
            extra: "<dwd:IssueTime>2026-10-26T15:00:00.000Z</dwd:IssueTime>\n"));

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [DataRow("2026-10-26T15:00:00.500Z")]
    [DataRow("2026-10-26 15:00:00Z")]
    [DataRow("2026-13-26T15:00:00.000Z")]
    [DataRow("2026-10-26T15:00:00.000+01:00")]
    [DataRow("")]
    [TestMethod]
    public async Task ReadSingleLocationAsync_IssueTimeInvalid_FailsFormatInvalid(string issueTime)
    {
        var kml = WithProductDefinition(MosmixForecastKmzBuilder.ProductDefinition(issueTime: issueTime));

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_IssueTimeWithoutFraction_WholeSecond()
    {
        var doc = await Read(WithProductDefinition(MosmixForecastKmzBuilder.ProductDefinition(issueTime: "2026-10-26T15:00:00Z")));

        doc.IssuedAtUtc.Should().Be(new DateTime(2026, 10, 26, 15, 0, 0, DateTimeKind.Utc));
    }

    [DataRow("")]
    [DataRow("2026-10-26T17:00:00.000Z|2026-10-26T16:00:00.000Z")]
    [DataRow("2026-10-26T16:00:00.000Z|2026-10-26T16:00:00.000Z")]
    [DataRow("2026-10-26T16:00:00.000+01:00")]
    [DataRow("2026-10-26T16:00:00.500Z")]
    [TestMethod]
    public async Task ReadSingleLocationAsync_TimelineInvalid_FailsFormatInvalid(string timeline)
    {
        var steps = timeline.Split('|', StringSplitOptions.RemoveEmptyEntries);
        var placemark = steps.Length == 0
            ? MosmixForecastKmzBuilder.Placemark("10637")
            : MosmixForecastKmzBuilder.Placemark("10637", forecasts: MosmixForecastKmzBuilder.Forecast("TTT", string.Join(" ", Enumerable.Repeat("1.0", steps.Length))));
        var kml = MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(timeStepsXml: Steps(steps)), placemark);
        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_PlacemarkOfAnotherLocation_FailsLocationMismatch()
    {
        var kml = MosmixForecastKmzBuilder.Document(
            MosmixForecastKmzBuilder.ProductDefinition(),
            MosmixForecastKmzBuilder.Placemark("10638"));

        await ShouldFail(kml, MosmixForecastErrorIds.LocationMismatch);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_TrimmedNameMatches()
    {
        var kml = MosmixForecastKmzBuilder.Document(
            MosmixForecastKmzBuilder.ProductDefinition(),
            MosmixForecastKmzBuilder.Placemark("  10637 "));

        var doc = await Read(kml);

        doc.MosmixId.Should().Be("10637");
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_TwoPlacemarks_FailsFormatInvalid()
    {
        var kml = MosmixForecastKmzBuilder.Document(
            MosmixForecastKmzBuilder.ProductDefinition(),
            MosmixForecastKmzBuilder.Placemark("10637"),
            MosmixForecastKmzBuilder.Placemark("10638"));

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_NoPlacemark_FailsFormatInvalid()
    {
        var kml = MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition());

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_DuplicateElementName_FailsFormatInvalid()
    {
        var kml = WithForecasts(
            MosmixForecastKmzBuilder.Forecast("TTT", "1.0 2.0 3.0"),
            MosmixForecastKmzBuilder.Forecast("TTT", "4.0 5.0 6.0"));

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [DataRow("1.0 2.0")]
    [DataRow("1.0 2.0 3.0 4.0")]
    [TestMethod]
    public async Task ReadSingleLocationAsync_TokenCountDiffers_FailsFormatInvalid(string tokens)
    {
        var kml = WithForecasts(MosmixForecastKmzBuilder.Forecast("TTT", tokens), MosmixForecastKmzBuilder.Forecast("FF", "1.0 2.0 3.0"));
        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [DataRow("1e5")]
    [DataRow("+1.0")]
    [DataRow("1,000")]
    [DataRow("NaN")]
    [DataRow("1234567890")]
    [DataRow("1.1234567")]
    [DataRow("--")]
    [DataRow("1.")]
    [DataRow(".5")]
    [TestMethod]
    public async Task ReadSingleLocationAsync_TokenOutsideGrammar_FailsFormatInvalid(string token)
    {
        var kml = WithForecasts(
            MosmixForecastKmzBuilder.Forecast("TTT", $"{token} 2.0 3.0"));

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_TokenGrammarBoundaries_Accepted()
    {
        var doc = await Read(WithForecasts(
            MosmixForecastKmzBuilder.Forecast("TTT", "123456789 -0.123456 0")));

        doc.Series[0].Values.Should().Equal("123456789", "-0.123456", "0");
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_DashToken_NullAtItsIndex()
    {
        var doc = await Read(WithForecasts(
            MosmixForecastKmzBuilder.Forecast("TTT", "- 1.0 -"),
            MosmixForecastKmzBuilder.Forecast("FF", "1.0 - 2.0")));

        doc.Series[0].Values.Should().Equal(null, "1.0", null);
        doc.Series[1].Values.Should().Equal("1.0", null, "2.0");
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_TimeStepsAtBound_Accepted()
    {
        var limits = MosmixForecastReaderLimits.Contract with { MaxTimeSteps = 5 };
        var kml = MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(timeSteps: 5), MosmixForecastKmzBuilder.Placemark("10637", forecasts: MosmixForecastKmzBuilder.Forecast("TTT", string.Join(" ", Enumerable.Repeat("1.0", 5)))));
        var doc = await Read(kml, limits);
        doc.Timeline.Should().HaveCount(5);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_TimeStepsOneAboveBound_FailsFormatInvalid()
    {
        var limits = MosmixForecastReaderLimits.Contract with { MaxTimeSteps = 5 };
        var kml = MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(timeSteps: 6), MosmixForecastKmzBuilder.Placemark("10637", forecasts: MosmixForecastKmzBuilder.Forecast("TTT", string.Join(" ", Enumerable.Repeat("1.0", 6)))));
        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid, limits);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_ElementsAtBound_Accepted()
    {
        var limits = MosmixForecastReaderLimits.Contract with { MaxForecastsPerPlacemark = 3 };
        var kml = WithForecasts(
            MosmixForecastKmzBuilder.Forecast("A1", "1.0 2.0 3.0"),
            MosmixForecastKmzBuilder.Forecast("A2", "1.0 2.0 3.0"),
            MosmixForecastKmzBuilder.Forecast("A3", "1.0 2.0 3.0"));

        await Read(kml, limits);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_ElementsOneAboveBound_FailsFormatInvalid()
    {
        var limits = MosmixForecastReaderLimits.Contract with { MaxForecastsPerPlacemark = 3 };
        var kml = WithForecasts(
            MosmixForecastKmzBuilder.Forecast("A1", "1.0 2.0 3.0"),
            MosmixForecastKmzBuilder.Forecast("A2", "1.0 2.0 3.0"),
            MosmixForecastKmzBuilder.Forecast("A3", "1.0 2.0 3.0"),
            MosmixForecastKmzBuilder.Forecast("A4", "1.0 2.0 3.0"));

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid, limits);
    }

    [DataRow("T T")]
    [DataRow("T-T")]
    [DataRow("ABCDEFGHIJKLMNOPQ")]
    [DataRow("")]
    [TestMethod]
    public async Task ReadSingleLocationAsync_ElementNameOutsideGrammar_FailsFormatInvalid(string elementName)
    {
        var kml = WithForecasts(
            MosmixForecastKmzBuilder.Forecast(elementName, "1.0 2.0 3.0"));

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_ElementNameMissing_FailsFormatInvalid()
    {
        var kml = WithForecasts(
            "<dwd:Forecast><dwd:value>1.0 2.0 3.0</dwd:value></dwd:Forecast>");

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_ValueMissing_FailsFormatInvalid()
    {
        var kml = WithForecasts(
            "<dwd:Forecast dwd:elementName=\"TTT\"></dwd:Forecast>");

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_Coordinates_DecimalDegreesAndAltitude()
    {
        var doc = await Read(MosmixForecastKmzBuilder.Single(coordinates: "-8.4,70.56,-12.5"));

        doc.Longitude.Should().Be(-8.4m);
        doc.Latitude.Should().Be(70.56m);
        doc.AltitudeM.Should().Be(-12.5m);
    }

    [DataRow("-180,-90,0")]
    [DataRow("180,90,0")]
    [TestMethod]
    public async Task ReadSingleLocationAsync_CoordinatesAtLimits_Accepted(string coordinates)
    {
        var doc = await Read(MosmixForecastKmzBuilder.Single(coordinates: coordinates));

        var parts = coordinates.Split(',');
        doc.Longitude.Should().Be(decimal.Parse(parts[0], CultureInfo.InvariantCulture));
        doc.Latitude.Should().Be(decimal.Parse(parts[1], CultureInfo.InvariantCulture));
        doc.AltitudeM.Should().Be(decimal.Parse(parts[2], CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_CoordinatesWithoutAltitude_FailsFormatInvalid()
    {
        await ShouldFail(MosmixForecastKmzBuilder.Single(coordinates: "9.6,50.5"), MosmixForecastErrorIds.FormatInvalid);
    }

    [DataRow("180.000001,50,0")]
    [DataRow("-180.000001,50,0")]
    [DataRow("9.6,90.000001,0")]
    [DataRow("9.6,-90.000001,0")]
    [TestMethod]
    public async Task ReadSingleLocationAsync_CoordinatesJustOutsideLimits_FailsFormatInvalid(string coordinates)
    {
        var kml = MosmixForecastKmzBuilder.Single(coordinates: coordinates);

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [DataRow("abc")]
    [DataRow("9.6")]
    [DataRow("9,6,50,5")]
    [DataRow("9.6;50.5;1")]
    [DataRow("9.6,50.5,1,2")]
    [DataRow("")]
    [TestMethod]
    public async Task ReadSingleLocationAsync_CoordinatesMissingOrUnparsable_FailsFormatInvalid(string coordinates)
    {
        var kml = MosmixForecastKmzBuilder.Single(coordinates: coordinates);

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_NoPoint_FailsFormatInvalid()
    {
        var kml = MosmixForecastKmzBuilder.Document(
            MosmixForecastKmzBuilder.ProductDefinition(),
            MosmixForecastKmzBuilder.Placemark("10637", coordinates: null));

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_LocationName_KeptExactly()
    {
        var doc = await Read(MosmixForecastKmzBuilder.Single(description: "Bad Hersfeld (Kl\u00E4ranlage),  Nord"));

        doc.LocationName.Should().Be("Bad Hersfeld (Kl\u00E4ranlage),  Nord");
    }

    [DataRow(0)]
    [DataRow(257)]
    [TestMethod]
    public async Task ReadSingleLocationAsync_LocationNameEmptyOrTooLong_FailsFormatInvalid(int length)
    {
        var name = new string('A', length);
        var kml = MosmixForecastKmzBuilder.Single(description: name);

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_LocationName256Characters_Accepted()
    {
        var name = new string('A', 256);
        var doc = await Read(MosmixForecastKmzBuilder.Single(description: name));

        doc.LocationName.Should().Be(name);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_LocationNameWhitespaceOnly_FailsFormatInvalid()
    {
        await ShouldFail(MosmixForecastKmzBuilder.Single(description: "   "), MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_OptionalSchemaElementsAbsent_Accepted()
    {
        var kml = WithProductDefinition("<dwd:ProductDefinition>" +
                                        "<dwd:ProductID>MOSMIX</dwd:ProductID>\n" +
                                        "<dwd:IssueTime>2026-10-26T15:00:00.000Z</dwd:IssueTime>\n" +
                                        "<dwd:ForecastTimeSteps>\n" +
                                        MosmixForecastKmzBuilder.TimeSteps(3) +
                                        "</dwd:ForecastTimeSteps>\n" +
                                        "</dwd:ProductDefinition>");

        var doc = await Read(kml);
        doc.IssuedAtUtc.Should().Be(new DateTime(2026, 10, 26, 15, 0, 0, DateTimeKind.Utc));
    }

    [DataRow(256)]
    [TestMethod]
    public async Task ReadSingleLocationAsync_OptionalDwdText_BoundedTo256(int length)
    {
        var kml = WithProductDefinition(MosmixForecastKmzBuilder.ProductDefinition(
            extra: $"<dwd:MetElementDefinition>{new string('a', length)}</dwd:MetElementDefinition>"));

        await Read(kml);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_OptionalDwdTextOver256_FailsFormatInvalid()
    {
        var kml = WithProductDefinition(MosmixForecastKmzBuilder.ProductDefinition(
            extra: $"<dwd:MetElementDefinition>{new string('a', 257)}</dwd:MetElementDefinition>"));

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_DefaultUndefSignNotDash_FailsFormatInvalid()
    {
        var kml = MosmixForecastKmzBuilder.Single().Replace("<dwd:DefaultUndefSign>-</dwd:DefaultUndefSign>", "<dwd:DefaultUndefSign>*</dwd:DefaultUndefSign>");
        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_ReferencedModelText_BoundedTo256()
    {
        var kml = MosmixForecastKmzBuilder.Single().Replace("<dwd:ReferencedModel>", "<dwd:ReferencedModel>" + new string('a', 256));
        kml.Should().Contain(new string('a', 256));
        (await Read(kml)).MosmixId.Should().Be("10637");
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_ReferencedModelTextOver256_FailsFormatInvalid()
    {
        var kml = MosmixForecastKmzBuilder.Single().Replace("<dwd:ReferencedModel>", "<dwd:ReferencedModel>" + new string('a', 257));
        kml.Should().Contain(new string('a', 257));
        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_ModelTextOver256_FailsFormatInvalid()
    {
        var kml = MosmixForecastKmzBuilder.Single().Replace("dwd:referenceTime=\"2026-10-26T12:00:00Z\"/>", "dwd:referenceTime=\"2026-10-26T12:00:00Z\">" + new string('a', 257) + "</dwd:Model>");
        kml.Should().Contain(new string('a', 257));
        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_TwoReferencedModels_FailsFormatInvalid()
    {
        var kml = MosmixForecastKmzBuilder.Single().Replace("</dwd:ReferencedModel>", "</dwd:ReferencedModel><dwd:ReferencedModel/>");
        kml.Should().Contain("</dwd:ReferencedModel><dwd:ReferencedModel/>");
        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_PlacemarkBeforeProductDefinition_FailsFormatInvalid()
    {
        var kml = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\" standalone=\"yes\"?>\n" +
                  "<kml:kml xmlns:dwd=\"https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd\" xmlns:kml=\"http://www.opengis.net/kml/2.2\">\n" +
                  "  <kml:Document>\n" +
                  "    <kml:Placemark>\n" +
                  "      <kml:name>10637</kml:name>\n" +
                  "      <kml:description>BEISPIELSTADT</kml:description>\n" +
                  "      <kml:ExtendedData>\n" +
                  "        <dwd:Forecast dwd:elementName=\"TTT\"><dwd:value>281.15 281.05 -</dwd:value></dwd:Forecast>\n" +
                  "      </kml:ExtendedData>\n" +
                  "      <kml:Point>\n" +
                  "        <kml:coordinates>9.6,50.5,250.0</kml:coordinates>\n" +
                  "      </kml:Point>\n" +
                  "    </kml:Placemark>\n" +
                  "    <kml:ExtendedData>\n" +
                  "      <dwd:ProductDefinition>\n" +
                  "        <dwd:Issuer>Example issuer</dwd:Issuer>\n" +
                  "        <dwd:ProductID>MOSMIX</dwd:ProductID>\n" +
                  "        <dwd:GeneratingProcess>Example process</dwd:GeneratingProcess>\n" +
                  "        <dwd:IssueTime>2026-10-26T15:00:00.000Z</dwd:IssueTime>\n" +
                  "        <dwd:ReferencedModel><dwd:Model dwd:name=\"ICON\" dwd:referenceTime=\"2026-10-26T12:00:00Z\"/></dwd:ReferencedModel>\n" +
                  "        <dwd:ForecastTimeSteps>\n" +
                  "          <dwd:TimeStep>2026-10-26T16:00:00.000Z</dwd:TimeStep>\n" +
                  "          <dwd:TimeStep>2026-10-26T17:00:00.000Z</dwd:TimeStep>\n" +
                  "          <dwd:TimeStep>2026-10-26T18:00:00.000Z</dwd:TimeStep>\n" +
                  "        </dwd:ForecastTimeSteps>\n" +
                  "        <dwd:FormatCfg><dwd:DefaultUndefSign>-</dwd:DefaultUndefSign></dwd:FormatCfg>\n" +
                  "      </dwd:ProductDefinition>\n" +
                  "    </kml:ExtendedData>\n" +
                  "  </kml:Document>\n" +
                  "</kml:kml>\n";

        await ShouldFail(kml, MosmixForecastErrorIds.FormatInvalid);
    }
}
