using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class CdcAvailabilityDeriverSelectCandidatesTests
{
    private static readonly DateTimeOffset Reference = new(2026, 10, 21, 4, 30, 0, TimeSpan.Zero);
    private ICdcAvailabilityDeriver _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new CdcAvailabilityDeriver(new QuantityValidator());
    }

    [TestMethod]
    public void SelectCandidates_UnionOfFourProductLists()
    {
        var lists = new[]
        {
            List("tu", Rec("00001")),
            List("ff", Rec("00001"), Rec("00002")),
            List("rr", Rec("00003")),
            List("sd", Rec("00004"))
        };

        var result = _subject.SelectCandidates(lists, Reference);

        result.Stations.Should().HaveCount(4);
        result.Stations[0].StationId.Should().Be("00001");
        result.Stations[0].CandidateProducts.Should().ContainInOrder("ff", "tu");
        result.Stations[1].StationId.Should().Be("00002");
        result.Stations[1].CandidateProducts.Should().ContainInOrder("ff");
        result.Stations[2].StationId.Should().Be("00003");
        result.Stations[2].CandidateProducts.Should().ContainInOrder("rr");
        result.Stations[3].StationId.Should().Be("00004");
        result.Stations[3].CandidateProducts.Should().ContainInOrder("sd");
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void SelectCandidates_StationWithConflictingAttributes_ExcludedWithConflictDiagnostic()
    {
        var lists = new[]
        {
            List("tu", Rec("00001"), Rec("00002")),
            List("ff", Rec("00001", altitude: 101m)),
        };

        var result = _subject.SelectCandidates(lists, Reference);

        result.Stations.Should().HaveCount(1);
        result.Stations[0].StationId.Should().Be("00002");
        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be(CatalogDiagnostic.StationConflictId);
        diagnostic.StationId.Should().Be("00001");
    }

    [TestMethod]
    public void SelectCandidates_StationWithConflictingLongitude_ExcludedWithConflictDiagnostic()
    {
        var lists = new[]
        {
            List("tu", Rec("00001"), Rec("00002")),
            List("ff", Rec("00001", longitude: 9.2m)),
        };

        var result = _subject.SelectCandidates(lists, Reference);

        result.Stations.Should().ContainSingle().Which.StationId.Should().Be("00002");
        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be(CatalogDiagnostic.StationConflictId);
        diagnostic.StationId.Should().Be("00001");
    }

    [TestMethod]
    public void SelectCandidates_StationWithoutAltitude_OneNoAltitudeDiagnosticPerStation()
    {
        var lists = new[]
        {
            List("tu", Rec("00001", altitude: null)),
            List("ff", Rec("00001")),
        };

        var result = _subject.SelectCandidates(lists, Reference);

        result.Stations.Should().ContainSingle();
        result.Stations[0].StationId.Should().Be("00001");
        result.Stations[0].AltitudeM.Should().BeNull();
        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be(CatalogDiagnostic.StationNoAltitudeId);
        diagnostic.StationId.Should().Be("00001");
        diagnostic.EndpointName.Should().Be("tu-list");
    }

    [TestMethod]
    public void SelectCandidates_StationInOnlyOneList_Kept()
    {
        var lists = new[]
        {
            List("tu", Rec("00001")),
            List("ff", Rec("00002"))
        };

        var result = _subject.SelectCandidates(lists, Reference);

        result.Stations.Should().HaveCount(2);
        result.Stations[0].StationId.Should().Be("00001");
        result.Stations[0].CandidateProducts.Should().ContainInOrder("tu");
        result.Stations[1].StationId.Should().Be("00002");
        result.Stations[1].CandidateProducts.Should().ContainInOrder("ff");
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void SelectCandidates_CandidateProductRule_BothSidesOfEachBound()
    {
        var lists = new[]
        {
            List("tu",
                Rec("00001", from: "20261021", to: "20261021"), // candidate
                Rec("00002", from: "20261022", to: "20261022"), // not a candidate, not malformed
                Rec("00003", to: "20261020"), // candidate
                Rec("00004", to: "20261019"), // not a candidate
                Rec("00005", to: "20261022")  // candidate
            )
        };

        var result = _subject.SelectCandidates(lists, Reference);

        result.Stations.Should().HaveCount(3);
        result.Stations[0].StationId.Should().Be("00001");
        result.Stations[0].CandidateProducts.Should().ContainInOrder("tu");
        result.Stations[1].StationId.Should().Be("00003");
        result.Stations[1].CandidateProducts.Should().ContainInOrder("tu");
        result.Stations[2].StationId.Should().Be("00005");
        result.Stations[2].CandidateProducts.Should().ContainInOrder("tu");
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void SelectCandidates_ToDateBeyondBound_SkippedAsMalformed()
    {
        var lists = new[]
        {
            List("tu", Rec("00001"), Rec("00002", to: "20261023", line: 4))
        };

        var result = _subject.SelectCandidates(lists, Reference);

        result.Stations.Should().ContainSingle();
        result.Stations[0].StationId.Should().Be("00001");
        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        diagnostic.EndpointName.Should().Be("tu-list");
        diagnostic.LineNumber.Should().Be(4);
        diagnostic.StationId.Should().Be("00002");
    }

    [TestMethod]
    public void SelectCandidates_FromDateAfterToDate_SkippedAsMalformed()
    {
        var lists = new[]
        {
            List("tu", Rec("00001"), Rec("00002", from: "20261021", to: "20261020", line: 4))
        };

        var result = _subject.SelectCandidates(lists, Reference);

        result.Stations.Should().ContainSingle();
        result.Stations[0].StationId.Should().Be("00001");
        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be(CatalogDiagnostic.RecordSkippedId);
        diagnostic.LineNumber.Should().Be(4);
    }

    [TestMethod]
    public void SelectCandidates_AllRecordsOfAListMalformed_ThrowsFormatInvalid()
    {
        var lists = new[]
        {
            List("tu", Rec("00001", to: "20261023"))
        };

        Action act = () => _subject.SelectCandidates(lists, Reference);

        act.Should().Throw<CatalogFormatException>()
            .Which.ErrorId.Should().Be(CatalogFormatException.CatalogFormatInvalidErrorId);
    }

    [TestMethod]
    public void SelectCandidates_StationWithoutCandidateProduct_IsOmitted()
    {
        var lists = new[]
        {
            List("tu", Rec("00001", to: "20261001"), Rec("00002"))
        };

        var result = _subject.SelectCandidates(lists, Reference);

        result.Stations.Should().ContainSingle();
        result.Stations[0].StationId.Should().Be("00002");
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void SelectCandidates_AllStationsInactive_EmptyResultWithoutException()
    {
        var lists = new[]
        {
            List("tu", Rec("00001", to: "20261001"))
        };

        var result = _subject.SelectCandidates(lists, Reference);

        result.Stations.Should().BeEmpty();
        result.Diagnostics.Should().BeEmpty();
    }

    [TestMethod]
    public void SelectCandidates_NonUtcReference_ThrowsArgumentException()
    {
        var reference = new DateTimeOffset(2026, 10, 21, 6, 30, 0, TimeSpan.FromHours(2));
        var lists = new[] { List("tu", Rec("00001")) };

        Action act = () => _subject.SelectCandidates(lists, reference);

        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void SelectCandidates_UnknownOrDuplicateProductCode_ThrowsArgumentException()
    {
        Action act1 = () => _subject.SelectCandidates([List("tx", Rec("00001"))], Reference);
        act1.Should().Throw<ArgumentException>();

        Action act2 = () => _subject.SelectCandidates([List("tu", Rec("00001")), List("tu", Rec("00002"))], Reference);
        act2.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void SelectCandidates_IsDeterministicForAnyListOrder()
    {
        var lists1 = new[]
        {
            List("tu", Rec("00001")),
            List("ff", Rec("00001"), Rec("00002")),
            List("rr", Rec("00003")),
            List("sd", Rec("00004"))
        };

        var lists2 = new[]
        {
            List("sd", Rec("00004")),
            List("rr", Rec("00003")),
            List("ff", Rec("00001"), Rec("00002")),
            List("tu", Rec("00001"))
        };

        var result1 = _subject.SelectCandidates(lists1, Reference);
        var result2 = _subject.SelectCandidates(lists2, Reference);

        result1.Stations.Should().HaveCount(4);
        result2.Stations.Should().HaveCount(4);
        for (int i = 0; i < 4; i++)
        {
            result1.Stations[i].StationId.Should().Be(result2.Stations[i].StationId);
            result1.Stations[i].CandidateProducts.Should().ContainInOrder(result2.Stations[i].CandidateProducts);
        }
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesCdcAvailabilityDeriver()
    {
        using var provider = new ServiceCollection()
            .AddWeatherParsing()
            .BuildServiceProvider(true);

        var deriver = provider.GetRequiredService<ICdcAvailabilityDeriver>();
        deriver.Should().BeOfType<CdcAvailabilityDeriver>();
    }

    private static CdcStationRecord Rec(string id, string from = "20000101", string to = "20261021", decimal? altitude = 100m, decimal latitude = 50.1m, decimal longitude = 9.1m, string name = "Musterhausen", int line = 3) => new(id, DateOnly.ParseExact(from, "yyyyMMdd"), DateOnly.ParseExact(to, "yyyyMMdd"), altitude, latitude, longitude, name, "Hessen", line);

    private static CdcProductList List(string code, params CdcStationRecord[] records) => new(code, code + "-list", new CdcStationCatalog(records, []));
}
