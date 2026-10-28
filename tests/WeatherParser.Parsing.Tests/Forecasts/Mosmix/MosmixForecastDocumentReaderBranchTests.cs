using System;
using System.IO;
using System.Text;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Forecasts.Mosmix;

namespace WeatherParser.Parsing.Tests.Forecasts.Mosmix;

[TestClass]
public class MosmixForecastDocumentReaderBranchTests
{
    private IMosmixForecastDocumentReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new MosmixForecastDocumentReader();
    }

    private const string Pd = "<dwd:ProductDefinition><dwd:ProductID>MOSMIX</dwd:ProductID><dwd:IssueTime>2026-10-26T15:00:00.000Z</dwd:IssueTime><dwd:ForecastTimeSteps><dwd:TimeStep>2026-10-26T16:00:00.000Z</dwd:TimeStep><dwd:TimeStep>2026-10-26T17:00:00.000Z</dwd:TimeStep><dwd:TimeStep>2026-10-26T18:00:00.000Z</dwd:TimeStep></dwd:ForecastTimeSteps></dwd:ProductDefinition>";
    private const string Root = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\" standalone=\"yes\"?>\n<kml:kml xmlns:dwd=\"https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd\" xmlns:kml=\"http://www.opengis.net/kml/2.2\">";

    private static string Replace(string a, string b)
    {
        var kml = MosmixForecastKmzBuilder.Single();
        if (!kml.Contains(a))
            Assert.Fail($"Base document does not contain '{a}'");
        return kml.Replace(a, b);
    }

    private static string Kml(string scenario)
    {
        return scenario switch
        {
            "secondDocument" => Replace("</kml:Document>", "</kml:Document><kml:Document/>"),
            "noDocument" => Root + "<kml:Style/></kml:kml>",
            "emptyDocument" => Root + "<kml:Document/></kml:kml>",
            "documentWithoutProductDefinition" => Root + "<kml:Document><kml:ExtendedData/></kml:Document></kml:kml>",
            "twoProductDefinitions" => Replace("</dwd:ProductDefinition>", "</dwd:ProductDefinition><dwd:ProductDefinition/>"),
            "emptyProductDefinition" => MosmixForecastKmzBuilder.Document("<dwd:ProductDefinition/>", MosmixForecastKmzBuilder.Placemark("10637")),
            "twoProductIds" => Replace("<dwd:ProductID>MOSMIX</dwd:ProductID>", "<dwd:ProductID>MOSMIX</dwd:ProductID><dwd:ProductID>MOSMIX</dwd:ProductID>"),
            "noProductId" => Replace("<dwd:ProductID>MOSMIX</dwd:ProductID>", ""),
            "twoForecastTimeSteps" => Replace("<dwd:FormatCfg>", "<dwd:ForecastTimeSteps/><dwd:FormatCfg>"),
            "noForecastTimeSteps" => MosmixForecastKmzBuilder.Document(Pd.Replace("<dwd:ForecastTimeSteps><dwd:TimeStep>2026-10-26T16:00:00.000Z</dwd:TimeStep><dwd:TimeStep>2026-10-26T17:00:00.000Z</dwd:TimeStep><dwd:TimeStep>2026-10-26T18:00:00.000Z</dwd:TimeStep></dwd:ForecastTimeSteps>", ""), MosmixForecastKmzBuilder.Placemark("10637")),
            "emptyForecastTimeSteps" => MosmixForecastKmzBuilder.Document(Pd.Replace("<dwd:ForecastTimeSteps><dwd:TimeStep>2026-10-26T16:00:00.000Z</dwd:TimeStep><dwd:TimeStep>2026-10-26T17:00:00.000Z</dwd:TimeStep><dwd:TimeStep>2026-10-26T18:00:00.000Z</dwd:TimeStep></dwd:ForecastTimeSteps>", "<dwd:ForecastTimeSteps/>"), MosmixForecastKmzBuilder.Placemark("10637")),
            "twoIssuers" => Replace("<dwd:Issuer>Example issuer</dwd:Issuer>", "<dwd:Issuer>A</dwd:Issuer><dwd:Issuer>B</dwd:Issuer>"),
            "twoGeneratingProcesses" => Replace("<dwd:GeneratingProcess>Example process</dwd:GeneratingProcess>", "<dwd:GeneratingProcess>A</dwd:GeneratingProcess><dwd:GeneratingProcess>B</dwd:GeneratingProcess>"),
            "twoMetElementDefinitions" => Replace("<dwd:FormatCfg>", "<dwd:MetElementDefinition>a</dwd:MetElementDefinition><dwd:MetElementDefinition>b</dwd:MetElementDefinition><dwd:FormatCfg>"),
            "twoFormatCfgs" => Replace("</dwd:FormatCfg>", "</dwd:FormatCfg><dwd:FormatCfg/>"),
            "twoUndefSigns" => Replace("<dwd:DefaultUndefSign>-</dwd:DefaultUndefSign>", "<dwd:DefaultUndefSign>-</dwd:DefaultUndefSign><dwd:DefaultUndefSign>-</dwd:DefaultUndefSign>"),
            "undefSignStar" => Replace("<dwd:DefaultUndefSign>-</dwd:DefaultUndefSign>", "<dwd:DefaultUndefSign>*</dwd:DefaultUndefSign>"),
            "emptyPlacemark" => MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(), "<kml:Placemark/>"),
            "twoNames" => Replace("<kml:name>10637</kml:name>", "<kml:name>10637</kml:name><kml:name>10637</kml:name>"),
            "twoDescriptions" => Replace("</kml:description>", "</kml:description><kml:description>X</kml:description>"),
            "twoPlacemarkExtendedData" => Replace("<kml:Point>", "<kml:ExtendedData/><kml:Point>"),
            "twoPoints" => Replace("</kml:Point>", "</kml:Point><kml:Point><kml:coordinates>1,2,3</kml:coordinates></kml:Point>"),
            "emptyPoint" => MosmixForecastKmzBuilder.Single(coordinates: null!).Replace("</kml:Placemark>", "<kml:Point/></kml:Placemark>"),
            "pointWithoutCoordinates" => Replace("<kml:coordinates>9.6,50.5,250.0</kml:coordinates>", ""),
            "twoCoordinates" => Replace("</kml:coordinates>", "</kml:coordinates><kml:coordinates>1,2,3</kml:coordinates>"),
            "emptyForecast" => Replace("<dwd:Forecast dwd:elementName=\"FF\"><dwd:value>3.10 - 2.60</dwd:value></dwd:Forecast>", "<dwd:Forecast dwd:elementName=\"FF\"/>"),
            "forecastWithoutValue" => Replace("<dwd:value>3.10 - 2.60</dwd:value>", "<dwd:Foo/>"),
            "twoValues" => Replace("<dwd:value>3.10 - 2.60</dwd:value>", "<dwd:value>3.10 - 2.60</dwd:value><dwd:value>3.10 - 2.60</dwd:value>"),
            "emptyValue" => Replace("<dwd:value>3.10 - 2.60</dwd:value>", "<dwd:value/>"),
            "elementInValue" => Replace("281.15 281.05 -", "281.15 <x/>281.05 -"),
            "overlongToken" => Replace("281.15 281.05 -", "123456789.12345678 281.05 -"),
            "elementInName" => Replace("<kml:name>10637</kml:name>", "<kml:name>10637<x/></kml:name>"),
            "overlongName" => Replace("<kml:name>10637</kml:name>", "<kml:name>" + new string('A', 257) + "</kml:name>"),
            "coordinatesNotNumeric" => Replace("9.6,50.5,250.0", "a,b,c"),
            "altitudeNotNumeric" => Replace("9.6,50.5,250.0", "9.6,50.5,abc"),
            "emptyName" => Replace("<kml:name>10637</kml:name>", "<kml:name/>"),
            "nameWithInnerSpaces" => Replace("<kml:name>10637</kml:name>", "<kml:name> 10637   X </kml:name>"),
            "foreignRootChild" => Replace("<kml:Document>", "<kml:Style/><kml:Document>"),
            "foreignDwdChild" => Replace("<dwd:Issuer>", "<kml:Style/><dwd:Foo>x</dwd:Foo><dwd:Issuer>"),
            "emptyFormatCfg" => Replace("<dwd:FormatCfg><dwd:DefaultUndefSign>-</dwd:DefaultUndefSign></dwd:FormatCfg>", "<dwd:FormatCfg/>"),
            "foreignFormatCfgChild" => Replace("<dwd:DefaultUndefSign>", "<dwd:Foo/><dwd:DefaultUndefSign>"),
            "foreignTimeStepChild" => Replace("<dwd:ForecastTimeSteps>", "<dwd:ForecastTimeSteps><dwd:Foo/>"),
            "cdataInContainer" => Replace("<kml:Document>", "<kml:Document><![CDATA[x]]>"),
            "foreignPlacemarkChild" => Replace("<kml:name>", "<kml:styleUrl>#s</kml:styleUrl><kml:name>"),
            "foreignPointChild" => Replace("<kml:coordinates>", "<kml:altitudeMode>absolute</kml:altitudeMode><kml:coordinates>"),
            "foreignForecastSibling" => Replace("<dwd:Forecast dwd:elementName=\"TTT\">", "<kml:Data name=\"x\"/><dwd:Forecast dwd:elementName=\"TTT\">"),
            "foreignForecastChild" => Replace("<dwd:value>281.15", "<dwd:Foo/><dwd:value>281.15"),
            "extraWhitespaceTokens" => Replace("281.15 281.05 -", "  281.15   281.05 -  "),
            "emptyPlacemarkExtendedData" => MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(), "<kml:Placemark><kml:name>10637</kml:name><kml:description>X</kml:description><kml:ExtendedData/><kml:Point><kml:coordinates>9.6,50.5,250.0</kml:coordinates></kml:Point></kml:Placemark>"),
            "noPlacemarkExtendedData" => MosmixForecastKmzBuilder.Document(MosmixForecastKmzBuilder.ProductDefinition(), "<kml:Placemark><kml:name>10637</kml:name><kml:description>X</kml:description><kml:Point><kml:coordinates>9.6,50.5,250.0</kml:coordinates></kml:Point></kml:Placemark>"),
            "emptyReferencedModel" => Replace("<dwd:ReferencedModel><dwd:Model dwd:name=\"ICON\" dwd:referenceTime=\"2026-10-26T12:00:00Z\"/></dwd:ReferencedModel>", "<dwd:ReferencedModel/>"),
            "foreignReferencedModelChild" => Replace("<dwd:ReferencedModel>", "<dwd:ReferencedModel><kml:Style/>"),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
    }

    private Task<MosmixForecastDocument> Read(string kml) => _subject.ReadSingleLocationAsync(new MemoryStream(MosmixForecastKmzBuilder.Kmz(kml)), "10637", CancellationToken.None);

    [TestMethod]
    [DataRow("secondDocument", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("noDocument", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("emptyDocument", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("documentWithoutProductDefinition", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("twoProductDefinitions", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("emptyProductDefinition", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("twoProductIds", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("noProductId", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("twoForecastTimeSteps", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("noForecastTimeSteps", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("emptyForecastTimeSteps", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("twoIssuers", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("twoGeneratingProcesses", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("twoMetElementDefinitions", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("twoFormatCfgs", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("twoUndefSigns", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("undefSignStar", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("emptyPlacemark", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("twoNames", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("twoDescriptions", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("twoPlacemarkExtendedData", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("twoPoints", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("emptyPoint", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("pointWithoutCoordinates", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("twoCoordinates", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("emptyForecast", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("forecastWithoutValue", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("twoValues", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("emptyValue", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("elementInValue", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("overlongToken", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("elementInName", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("overlongName", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("coordinatesNotNumeric", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("altitudeNotNumeric", MosmixForecastErrorIds.FormatInvalid)]
    [DataRow("emptyName", MosmixForecastErrorIds.LocationMismatch)]
    [DataRow("nameWithInnerSpaces", MosmixForecastErrorIds.LocationMismatch)]
    public async Task ReadSingleLocationAsync_MalformedStructure_Fails(string scenario, string errorId)
    {
        await FluentActions.Awaiting(() => Read(Kml(scenario))).Should().ThrowAsync<MosmixForecastFormatException>().Where(e => e.ErrorId == errorId);
    }

    [TestMethod]
    [DataRow("foreignRootChild")]
    [DataRow("foreignDwdChild")]
    [DataRow("emptyFormatCfg")]
    [DataRow("foreignFormatCfgChild")]
    [DataRow("foreignTimeStepChild")]
    [DataRow("cdataInContainer")]
    [DataRow("foreignPlacemarkChild")]
    [DataRow("foreignPointChild")]
    [DataRow("foreignForecastSibling")]
    [DataRow("foreignForecastChild")]
    [DataRow("extraWhitespaceTokens")]
    [DataRow("emptyPlacemarkExtendedData")]
    [DataRow("noPlacemarkExtendedData")]
    [DataRow("emptyReferencedModel")]
    [DataRow("foreignReferencedModelChild")]
    public async Task ReadSingleLocationAsync_IgnoredOrOptionalStructure_Accepted(string scenario)
    {
        var doc = await Read(Kml(scenario));
        doc.MosmixId.Should().Be("10637");
        if (scenario == "extraWhitespaceTokens")
        {
            doc.Series.Single(s => s.ElementName == "TTT").Values.Should().Equal("281.15", "281.05", null);
        }
        else if (scenario == "emptyPlacemarkExtendedData" || scenario == "noPlacemarkExtendedData")
        {
            doc.Series.Should().BeEmpty();
        }
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_CorruptCentralDirectory_FailsFormatInvalid()
    {
        var valid = MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single());
        var bytes = new byte[valid.Length];
        Array.Copy(valid, bytes, valid.Length);
        // Find central directory signature
        for (int i = 0; i < bytes.Length - 4; i++)
        {
            if (bytes[i] == 0x50 && bytes[i + 1] == 0x4B && bytes[i + 2] == 0x01 && bytes[i + 3] == 0x02)
            {
                bytes[i + 3] = 0x03; // Corrupt it
                break;
            }
        }

        await FluentActions.Awaiting(() => _subject.ReadSingleLocationAsync(new MemoryStream(bytes), "10637", CancellationToken.None))
            .Should().ThrowAsync<MosmixForecastFormatException>()
            .Where(e => e.ErrorId == MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public async Task ReadSingleLocationAsync_CorruptDeflateData_FailsFormatInvalid()
    {
        var valid = MosmixForecastKmzBuilder.Kmz(MosmixForecastKmzBuilder.Single());
        var bytes = new byte[valid.Length];
        Array.Copy(valid, bytes, valid.Length);
        // Find local header
        int localHeaderOffset = 0;
        for (int i = 0; i < bytes.Length - 4; i++)
        {
            if (bytes[i] == 0x50 && bytes[i + 1] == 0x4B && bytes[i + 2] == 0x03 && bytes[i + 3] == 0x04)
            {
                localHeaderOffset = i;
                break;
            }
        }

        int memberNameLength = Encoding.ASCII.GetByteCount(MosmixForecastKmzBuilder.SingleMemberName);
        int compressedDataStart = localHeaderOffset + 30 + memberNameLength;

        for (int i = 0; i < 8; i++)
        {
            bytes[compressedDataStart + 20 + i] = 0xFF;
        }

        await FluentActions.Awaiting(() => _subject.ReadSingleLocationAsync(new MemoryStream(bytes), "10637", CancellationToken.None))
            .Should().ThrowAsync<MosmixForecastFormatException>()
            .Where(e => e.ErrorId == MosmixForecastErrorIds.FormatInvalid);
    }

    [TestMethod]
    public void Constructor_NullLimits_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new MosmixForecastDocumentReader(null!)).Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void FormatException_NullErrorId_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new MosmixForecastFormatException(null!, "m")).Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void FormatException_KeepsIdAndMessage()
    {
        var ex = new MosmixForecastFormatException("X", "m");
        ex.ErrorId.Should().Be("X");
        ex.Message.Should().Be("m");
    }
}
