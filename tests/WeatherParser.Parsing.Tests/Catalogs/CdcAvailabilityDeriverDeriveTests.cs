using FluentAssertions;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class CdcAvailabilityDeriverDeriveTests
{
    // Window [01:30, 04:40] UTC, closed (SC-03).
    private static readonly DateTimeOffset Reference = new(2026, 10, 21, 4, 30, 0, TimeSpan.Zero);

    private ICdcAvailabilityDeriver _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new CdcAvailabilityDeriver(new QuantityValidator());
    }

    [TestMethod]
    public void Derive_MapsApprovedProductsToReq052Names()
    {
        var candidates = Candidates(Station("00001", "ff", "rr", "sd", "tu"));
        var evidence = Map(
            ("00001", "ff", Evidence("00001", "ff", Row(4, 0, ("FF_10", "3.4"), ("DD_10", "270")))),
            ("00001", "rr", Evidence("00001", "rr", Row(4, 0, ("RWS_10", "0.5")))),
            ("00001", "sd", Evidence("00001", "sd", Row(4, 0, ("GS_10", "12.5")))),
            ("00001", "tu", Evidence("00001", "tu", Row(4, 0, ("PP_10", "1001.2"), ("TT_10", "12.3"), ("RF_10", "85.0")))));

        var result = _subject.Derive(candidates, evidence, Reference);

        result.Stations.Should().ContainSingle().Which.AvailableMeasurements.Should().Equal(
            "humidity_percent", "pressure_hpa", "pressure_mmhg", "rain_rate_mm_h",
            "solar_irradiance_w_m2", "temperature_c", "wind_direction_deg", "wind_speed_kmh");
    }

    [TestMethod]
    public void Derive_ValueInTimeWindow_BothSidesOfEachBound()
    {
        DeriveTemperatureAt(1, 20).Stations.Should().BeEmpty(because: "01:20 lies before reference - 3 h");
        DeriveTemperatureAt(1, 30).Stations.Should().ContainSingle(because: "01:30 is the lower bound")
            .Which.AvailableMeasurements.Should().Equal("temperature_c");
        DeriveTemperatureAt(4, 40).Stations.Should().ContainSingle(because: "04:40 is the upper bound")
            .Which.AvailableMeasurements.Should().Equal("temperature_c");
        DeriveTemperatureAt(4, 50).Stations.Should().BeEmpty(because: "04:50 lies after reference + 10 min");
    }

    [TestMethod]
    public void Derive_MissingMarkerSpellings_AreNotUsable()
    {
        foreach (var token in new[] { "-999", "-999.0", "-0999" })
        {
            var result = _subject.Derive(
                Candidates(Station("00001", "tu")),
                Map(("00001", "tu", Evidence("00001", "tu", Row(4, 0, ("TT_10", token))))),
                Reference);

            result.Stations.Should().BeEmpty(because: token);
        }
    }

    [TestMethod]
    public void Derive_ValidatorRejections()
    {
        var humidity = _subject.Derive(
            Candidates(Station("00001", "tu")),
            Map(("00001", "tu", Evidence("00001", "tu", Row(4, 0, ("RF_10", "101"), ("TT_10", "12.3"))))),
            Reference);
        humidity.Stations.Should().ContainSingle().Which.AvailableMeasurements.Should().Equal("temperature_c");

        var wind = _subject.Derive(
            Candidates(Station("00002", "ff")),
            Map(("00002", "ff", Evidence("00002", "ff", Row(4, 0, ("FF_10", "-1"), ("DD_10", "90"))))),
            Reference);
        wind.Stations.Should().ContainSingle().Which.AvailableMeasurements.Should().Equal("wind_direction_deg");
    }

    [TestMethod]
    public void Derive_GenuineZerosUsable()
    {
        var result = _subject.Derive(
            Candidates(Station("00001", "rr", "tu")),
            Map(
                ("00001", "rr", Evidence("00001", "rr", Row(4, 0, ("RWS_10", "0.0")))),
                ("00001", "tu", Evidence("00001", "tu", Row(4, 0, ("TT_10", "0"))))),
            Reference);

        result.Stations.Should().ContainSingle().Which.AvailableMeasurements.Should().Equal("rain_rate_mm_h", "temperature_c");
    }

    [TestMethod]
    public void Derive_404ProductContributesNothing()
    {
        var result = _subject.Derive(
            Candidates(Station("00001", "ff", "tu")),
            Map(
                ("00001", "ff", null),
                ("00001", "tu", Evidence("00001", "tu", Row(4, 0, ("TT_10", "12.3"))))),
            Reference);

        result.Stations.Should().ContainSingle().Which.AvailableMeasurements.Should().Equal("temperature_c");
    }

    [TestMethod]
    public void Derive_StationWithNoAvailableQuantity_IsOmitted()
    {
        var result = _subject.Derive(
            Candidates(Station("00001", "tu"), Station("00002", "tu")),
            Map(
                ("00001", "tu", Evidence("00001", "tu", Row(4, 0, ("TT_10", "12.3")))),
                ("00002", "tu", Evidence("00002", "tu", Row(4, 0, ("TT_10", "-999"))))),
            Reference);

        result.Stations.Should().ContainSingle().Which.Station.StationId.Should().Be("00001");
    }

    [TestMethod]
    public void Derive_IsDeterministicForIdenticalInput()
    {
        var candidates = Candidates(Station("00002", "tu"), Station("00001", "ff", "tu"));
        var evidence = Map(
            ("00002", "tu", Evidence("00002", "tu", Row(4, 0, ("TT_10", "12.3"), ("RF_10", "80"))))
            , ("00001", "ff", Evidence("00001", "ff", Row(4, 0, ("FF_10", "3.4"), ("DD_10", "270"))))
            , ("00001", "tu", Evidence("00001", "tu", Row(4, 0, ("PP_10", "1001.2")))));

        var first = _subject.Derive(candidates, evidence, Reference);
        var second = _subject.Derive(candidates, evidence, Reference);

        first.Stations.Select(s => s.Station.StationId).Should().Equal("00002", "00001");
        second.Stations.Select(s => s.Station.StationId).Should().Equal("00002", "00001");
        for (var i = 0; i < first.Stations.Count; i++)
        {
            second.Stations[i].AvailableMeasurements.Should().Equal(first.Stations[i].AvailableMeasurements);
        }
    }

    [TestMethod]
    public void Derive_NonUtcReference_ThrowsArgumentException()
    {
        var act = () => _subject.Derive(
            Candidates(Station("00001", "tu")),
            Map(("00001", "tu", Evidence("00001", "tu", Row(4, 0, ("TT_10", "12.3"))))),
            new DateTimeOffset(2026, 10, 21, 6, 30, 0, TimeSpan.FromHours(2)));

        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Derive_MissingEvidenceKey_ThrowsArgumentException()
    {
        var act = () => _subject.Derive(Candidates(Station("00001", "tu")), Map(), Reference);

        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Derive_ExcludedStationDiagnosticsAreReturned()
    {
        var diagnostic = new CatalogDiagnostic(CatalogDiagnostic.StationConflictId, "ff-list", 7, "00009", "conflict");
        var candidates = new CdcCandidates([Station("00001", "tu")], [diagnostic]);

        var result = _subject.Derive(
            candidates,
            Map(("00001", "tu", Evidence("00001", "tu", Row(4, 0, ("TT_10", "12.3"))))),
            Reference);

        result.Diagnostics.Should().Equal(diagnostic);
    }

    [TestMethod]
    public void Derive_UsesOnlyTheProductsOwnColumns()
    {
        var result = _subject.Derive(
            Candidates(Station("00001", "tu")),
            Map(("00001", "tu", Evidence("00001", "tu", Row(4, 0, ("TT_10", "12.3"), ("FF_10", "3.4"))))),
            Reference);

        result.Stations.Should().ContainSingle().Which.AvailableMeasurements.Should().Equal("temperature_c");
    }

    [TestMethod]
    public void Derive_UnknownProductCode_ThrowsArgumentException()
    {
        var act = () => _subject.Derive(
            Candidates(Station("00001", "tx")),
            Map(("00001", "tx", Evidence("00001", "tx", Row(4, 0, ("TT_10", "12.3"))))),
            Reference);

        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Derive_NonNumericToken_IsNotUsable()
    {
        var result = _subject.Derive(
            Candidates(Station("00001", "tu")),
            Map(("00001", "tu", Evidence("00001", "tu", Row(4, 0, ("TT_10", "abc"))))),
            Reference);

        result.Stations.Should().BeEmpty();
    }

    private CdcAvailabilityResult DeriveTemperatureAt(int hour, int minute) => _subject.Derive(
        Candidates(Station("00001", "tu")),
        Map(("00001", "tu", Evidence("00001", "tu", Row(hour, minute, ("TT_10", "12.3"))))),
        Reference);

    private static CdcCandidateStation Station(string id, params string[] products) =>
        new(id, 100m, 50.1m, 9.1m, "Musterhausen", "Hessen", products);

    private static CdcCandidates Candidates(params CdcCandidateStation[] stations) => new(stations, []);

    private static CdcNowEvidenceRow Row(int hour, int minute, params (string Column, string Value)[] values) =>
        new(new DateTime(2026, 10, 21, hour, minute, 0, DateTimeKind.Utc), values.ToDictionary(v => v.Column, v => v.Value, StringComparer.Ordinal));

    private static CdcNowEvidence Evidence(string id, string product, params CdcNowEvidenceRow[] rows) => new(id, product, rows);

    private static Dictionary<CdcEvidenceKey, CdcNowEvidence?> Map(params (string Id, string Product, CdcNowEvidence? Evidence)[] entries) =>
        entries.ToDictionary(e => new CdcEvidenceKey(e.Id, e.Product), e => e.Evidence);
}
