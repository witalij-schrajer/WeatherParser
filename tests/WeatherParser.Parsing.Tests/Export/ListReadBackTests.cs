using System.Globalization;
using System.Text.Json;
using System.Xml.Serialization;
using FluentAssertions;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ListReadBackTests
{
    private const string XmlNamespace = "urn:weatherparser:export:v1";

    [TestMethod]
    public void JsonReadBack_CdcList_RestoresMetadataAndStation()
    {
        var json = """
            {"schema_version":"1","source":"dwd-cdc","retrieved_at":"2026-07-15T12:05:00","search_parameters":{"latitude":50.5549,"longitude":9.6738,"altitude_m":250,"max_distance_km":30,"max_altitude_difference_m":200},"stations":[{"station_id":"00044","name":"Fulda-Horas","latitude":50.567,"longitude":9.653,"altitude_m":242,"distance_km":2.1,"altitude_difference_m":-8,"available_measurements":["temperature_c"]}]}

            """;

        var result = JsonSerializer.Deserialize<CdcStationList>(json);

        result.Should().NotBeNull();
        result!.SchemaVersion.Should().Be("1");
        result!.Source.Should().Be("dwd-cdc");
        result!.RetrievedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        result!.RetrievedAtUtc.Should().Be(new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc));
        result!.SearchParameters.MaxDistanceKm.Should().Be(30);
        result!.Stations[0].StationId.Should().Be("00044");
        result!.Stations[0].AltitudeDifferenceM.Should().Be(-8);
        result!.Stations[0].AvailableMeasurements.Should().ContainSingle().Which.Should().Be("temperature_c");
    }

    [TestMethod]
    public void XmlReadBack_CdcList_RestoresMetadataAndStation()
    {
        var xml = $"""
            <cdc_station_list xmlns="{XmlNamespace}">
                <schema_version>1</schema_version>
                <source>dwd-cdc</source>
                <retrieved_at>2026-07-15T12:05:00</retrieved_at>
                <search_parameters>
                    <latitude>50.5549</latitude>
                    <longitude>9.6738</longitude>
                    <altitude_m>250</altitude_m>
                    <max_distance_km>30</max_distance_km>
                    <max_altitude_difference_m>200</max_altitude_difference_m>
                </search_parameters>
                <stations>
                    <station>
                        <station_id>00044</station_id>
                        <name>Fulda-Horas</name>
                        <latitude>50.567</latitude>
                        <longitude>9.653</longitude>
                        <altitude_m>242</altitude_m>
                        <distance_km>2.1</distance_km>
                        <altitude_difference_m>-8</altitude_difference_m>
                        <available_measurements>
                            <measurement>temperature_c</measurement>
                        </available_measurements>
                    </station>
                </stations>
            </cdc_station_list>

            """;

        var serializer = new XmlSerializer(typeof(CdcStationList), XmlNamespace);
        using var reader = new StringReader(xml);
        CdcStationList? result = serializer.Deserialize(reader) as CdcStationList;

        result.Should().NotBeNull();
        result!.SchemaVersion.Should().Be("1");
        result!.Source.Should().Be("dwd-cdc");
        result!.RetrievedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        result!.RetrievedAtUtc.Should().Be(new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc));
        result!.SearchParameters.MaxDistanceKm.Should().Be(30);
        result!.Stations[0].StationId.Should().Be("00044");
        result!.Stations[0].AltitudeDifferenceM.Should().Be(-8);
        result!.Stations[0].AvailableMeasurements.Should().ContainSingle().Which.Should().Be("temperature_c");
    }

    [TestMethod]
    public void JsonReadBack_MosmixList_RestoresMetadataAndProducts()
    {
        var json = """
            {"schema_version":"1","source":"dwd-mosmix","retrieved_at":"2026-07-15T12:05:00","search_parameters":{"latitude":50.5549,"longitude":9.6738,"altitude_m":250,"max_distance_km":30,"max_altitude_difference_m":200},"locations":[{"mosmix_id":"X123A","products":[{"product":"MOSMIX_L","availability":"available","available_forecast_elements":["TTT"]}]}]}

            """;

        var result = JsonSerializer.Deserialize<MosmixLocationList>(json);

        result.Should().NotBeNull();
        result!.SchemaVersion.Should().Be("1");
        result!.Source.Should().Be("dwd-mosmix");
        result!.RetrievedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        result!.RetrievedAtUtc.Should().Be(new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc));
        result!.SearchParameters.MaxDistanceKm.Should().Be(30);
        result!.Locations[0].MosmixId.Should().Be("X123A");
        result!.Locations[0].Products[0].Product.Should().Be(MosmixProduct.MosmixL);
        result!.Locations[0].Products[0].Availability.Should().Be(AvailabilityState.Available);
        result!.Locations[0].Products[0].AvailableForecastElements.Should().Equal("TTT");
    }

    [TestMethod]
    public void XmlReadBack_MosmixList_RestoresMetadataAndProducts()
    {
        var xml = $"""
            <mosmix_location_list xmlns="{XmlNamespace}">
                <schema_version>1</schema_version>
                <source>dwd-mosmix</source>
                <retrieved_at>2026-07-15T12:05:00</retrieved_at>
                <search_parameters>
                    <latitude>50.5549</latitude>
                    <longitude>9.6738</longitude>
                    <altitude_m>250</altitude_m>
                    <max_distance_km>30</max_distance_km>
                    <max_altitude_difference_m>200</max_altitude_difference_m>
                </search_parameters>
                <locations>
                    <location>
                        <mosmix_id>X123A</mosmix_id>
                        <products>
                            <product_availability>
                                <product>MOSMIX_L</product>
                                <availability>available</availability>
                                <available_forecast_elements>
                                    <element>TTT</element>
                                </available_forecast_elements>
                            </product_availability>
                        </products>
                    </location>
                </locations>
            </mosmix_location_list>

            """;

        var serializer = new XmlSerializer(typeof(MosmixLocationList), XmlNamespace);
        using var reader = new StringReader(xml);
        MosmixLocationList? result = serializer.Deserialize(reader) as MosmixLocationList;

        result.Should().NotBeNull();
        result!.SchemaVersion.Should().Be("1");
        result!.Source.Should().Be("dwd-mosmix");
        result!.RetrievedAtUtc.Kind.Should().Be(DateTimeKind.Utc);
        result!.RetrievedAtUtc.Should().Be(new DateTime(2026, 7, 15, 12, 5, 0, DateTimeKind.Utc));
        result!.SearchParameters.MaxDistanceKm.Should().Be(30);
        result!.Locations[0].MosmixId.Should().Be("X123A");
        result!.Locations[0].Products[0].Product.Should().Be(MosmixProduct.MosmixL);
        result!.Locations[0].Products[0].Availability.Should().Be(AvailabilityState.Available);
        result!.Locations[0].Products[0].AvailableForecastElements.Should().Equal("TTT");
    }

    [TestMethod]
    public void SchemaVersion_CdcListOtherValue_ThrowsArgumentException()
    {
        FluentActions.Invoking(() => new CdcStationList().SchemaVersion = "2")
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void SchemaVersion_MosmixListOtherValue_ThrowsArgumentException()
    {
        FluentActions.Invoking(() => new MosmixLocationList().SchemaVersion = "2")
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void SchemaVersion_ApprovedLiteral_IsAccepted()
    {
        var cdcList = new CdcStationList();
        var mosmixList = new MosmixLocationList();

        FluentActions.Invoking(() => cdcList.SchemaVersion = "1")
            .Should().NotThrow();
        FluentActions.Invoking(() => mosmixList.SchemaVersion = "1")
            .Should().NotThrow();

        cdcList.SchemaVersion.Should().Be("1");
        mosmixList.SchemaVersion.Should().Be("1");
    }
}
