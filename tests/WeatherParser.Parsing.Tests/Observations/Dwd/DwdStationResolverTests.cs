using System;
using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Tests.Observations.Dwd;

[TestClass]
public sealed class DwdStationResolverTests
{
    private DwdStationResolver _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new DwdStationResolver();
    }

    private static IReadOnlyList<CdcProductList> NoLists() => Array.Empty<CdcProductList>();

    private static void ShouldBeIdInvalid(Action act)
    {
        act.Should().Throw<DwdStationException>().Which.ErrorId.Should().Be(DwdStationException.StationIdInvalidErrorId);
    }

    private sealed class ThrowingLists : IReadOnlyList<CdcProductList>
    {
        public CdcProductList this[int index] => throw new InvalidOperationException("The lists must not be touched.");
        public int Count => throw new InvalidOperationException("The lists must not be touched.");
        public IEnumerator<CdcProductList> GetEnumerator() => throw new InvalidOperationException("The lists must not be touched.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static CdcStationRecord Record(string id, string name = "Station", decimal latitude = 50.1234m, decimal longitude = 8.5678m, decimal? altitude = 100m, int line = 3) => new(id, new DateOnly(2000, 1, 1), new DateOnly(2026, 10, 24), altitude, latitude, longitude, name, "Hessen", line);

    private static CdcProductList List(string productCode, params CdcStationRecord[] records) => new(productCode, productCode + "-list", new CdcStationCatalog(records, Array.Empty<CatalogDiagnostic>()));

    [TestMethod]
    public void ValidateId_FiveAsciiDigits_ReturnsId()
    {
        _subject.ValidateId("00044").Should().Be("00044");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("A1234")]
    [DataRow("\u0660\u0660\u0660\u0664\u0664")]
    [DataRow("00044\n")]
    [DataRow("0044")]
    [DataRow("000444")]
    [DataRow(" 00044")]
    [DataRow("00044 ")]
    public void ValidateId_InvalidInputs_ThrowStationIdInvalid(string id)
    {
        ShouldBeIdInvalid(() => _subject.ValidateId(id));
    }

    [TestMethod]
    public void ValidateId_Null_ThrowsStationIdInvalid()
    {
        ShouldBeIdInvalid(() => _subject.ValidateId(null!));
    }

    [TestMethod]
    public void ValidateId_Invalid_MessageDoesNotEchoInput()
    {
        FluentActions.Invoking(() => _subject.ValidateId("A1234"))
            .Should().Throw<DwdStationException>()
            .Which.Message.Should().NotContain("A1234");
    }

    [TestMethod]
    public void Resolve_FiveAsciiDigits_Accepted()
    {
        // the grammar accepts the ID, so with no lists the failure is STATION-UNKNOWN, not STATION-ID-INVALID
        FluentActions.Invoking(() => _subject.Resolve("00044", NoLists()))
            .Should().Throw<DwdStationException>()
            .Which.ErrorId.Should().Be(DwdStationException.StationUnknownErrorId);
    }

    [TestMethod]
    public void Resolve_EmptyId_ThrowsStationIdInvalid()
    {
        ShouldBeIdInvalid(() => _subject.Resolve("", NoLists()));
    }

    [TestMethod]
    public void Resolve_WhitespaceId_ThrowsStationIdInvalid()
    {
        ShouldBeIdInvalid(() => _subject.Resolve("     ", NoLists()));
    }

    [TestMethod]
    public void Resolve_NonDigitId_ThrowsStationIdInvalid()
    {
        ShouldBeIdInvalid(() => _subject.Resolve("A1234", NoLists()));
    }

    [TestMethod]
    public void Resolve_UnicodeDigits_ThrowsStationIdInvalid()
    {
        ShouldBeIdInvalid(() => _subject.Resolve("\u0660\u0660\u0660\u0664\u0664", NoLists()));
    }

    [TestMethod]
    public void Resolve_TrailingNewline_ThrowsStationIdInvalid()
    {
        ShouldBeIdInvalid(() => _subject.Resolve("00044\n", NoLists()));
    }

    [TestMethod]
    public void Resolve_FourDigits_ThrowsStationIdInvalid()
    {
        ShouldBeIdInvalid(() => _subject.Resolve("0044", NoLists()));
    }

    [TestMethod]
    public void Resolve_SixDigits_ThrowsStationIdInvalid()
    {
        ShouldBeIdInvalid(() => _subject.Resolve("000444", NoLists()));
    }

    [TestMethod]
    public void Resolve_NullId_ThrowsStationIdInvalid()
    {
        ShouldBeIdInvalid(() => _subject.Resolve(null!, NoLists()));
    }

    [TestMethod]
    public void Resolve_NullLists_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => _subject.Resolve("00044", null!))
            .Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Resolve_InvalidId_DoesNotTouchLists()
    {
        ShouldBeIdInvalid(() => _subject.Resolve("A1234", new ThrowingLists()));
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesDwdStationResolver()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IDwdStationResolver>().Should().BeOfType<DwdStationResolver>();
    }

    [TestMethod]
    public void Resolve_UnknownId_ThrowsStationUnknown()
    {
        var lists = new[] { List("tu", Record("00044")) };
        FluentActions.Invoking(() => _subject.Resolve("00091", lists))
            .Should().Throw<DwdStationException>()
            .Which.ErrorId.Should().Be(DwdStationException.StationUnknownErrorId);
    }

    [TestMethod]
    public void Resolve_StationInOneList_ReturnsLocationFromThatList()
    {
        var lists = new[]
        {
            List("tu", Record("00091", "Other", 51m, 9m, 200m)),
            List("ff", Record("00044", "Grossenkneten", 52.9336m, 8.2370m, 44m))
        };
        var result = _subject.Resolve("00044", lists);
        result.StationId.Should().Be("00044");
        result.Location.StationId.Should().Be("00044");
        result.Location.Name.Should().Be("Grossenkneten");
        result.Location.Latitude.Should().Be(52.9336m);
        result.Location.Longitude.Should().Be(8.2370m);
        result.Location.AltitudeM.Should().Be(44m);
        result.ProductCodes.Should().Equal("ff");
    }

    [TestMethod]
    public void Resolve_KeepsLeadingZeros()
    {
        var lists = new[] { List("tu", Record("00044")) };
        var result = _subject.Resolve("00044", lists);
        result.StationId.Should().Be("00044");
        result.Location.StationId.Should().Be("00044");
    }

    [TestMethod]
    public void Resolve_PreservesUmlautsInName()
    {
        var lists = new[] { List("tu", Record("00044", "W\u00fcrzburg")) };
        var result = _subject.Resolve("00044", lists);
        result.Location.Name.Should().Be("W\u00fcrzburg");
    }

    [TestMethod]
    public void Resolve_MissingAltitude_IsNullNeverZero()
    {
        var lists = new[] { List("tu", Record("00044", altitude: null)) };
        var result = _subject.Resolve("00044", lists);
        result.Location.AltitudeM.Should().BeNull();
    }

    [TestMethod]
    public void Resolve_ZeroAltitude_IsZero()
    {
        var lists = new[] { List("tu", Record("00044", altitude: 0m)) };
        var result = _subject.Resolve("00044", lists);
        result.Location.AltitudeM.Should().Be(0m);
    }

    [TestMethod]
    public void Resolve_ProductCodes_InProductOrder()
    {
        var lists = new[]
        {
            List("tu", Record("00044")),
            List("rr", Record("00044"))
        };
        var result = _subject.Resolve("00044", lists);
        result.ProductCodes.Should().Equal("tu", "rr");
    }

    [TestMethod]
    public void Resolve_ShuffledLists_ProductOrderCanonical()
    {
        var lists = new[]
        {
            List("rr", Record("00044")),
            List("sd", Record("00044")),
            List("tu", Record("00044"))
        };
        var result = _subject.Resolve("00044", lists);
        result.ProductCodes.Should().Equal("tu", "rr", "sd");
    }

    [TestMethod]
    public void Resolve_StationInAllFourLists_ReturnsFourProducts()
    {
        var lists = new[]
        {
            List("sd", Record("00044")),
            List("ff", Record("00044")),
            List("tu", Record("00044")),
            List("rr", Record("00044"))
        };
        var result = _subject.Resolve("00044", lists);
        result.ProductCodes.Should().Equal("tu", "ff", "rr", "sd");
    }

    [TestMethod]
    public void Resolve_CoordinatesExactDecimal()
    {
        var lists = new[] { List("tu", Record("00044", latitude: 47.26891m, longitude: 14.95061m)) };
        var result = _subject.Resolve("00044", lists);
        result.Location.Latitude.Should().Be(47.26891m);
        result.Location.Longitude.Should().Be(14.95061m);
    }

    [TestMethod]
    public void Resolve_StationAbsentFromSomeLists_OnlyContainingProducts()
    {
        var lists = new[]
        {
            List("tu", Record("00044")),
            List("ff", Record("00091")),
            List("sd", Record("00044"))
        };
        var result = _subject.Resolve("00044", lists);
        result.ProductCodes.Should().Equal("tu", "sd");
    }

    [TestMethod]
    public void Resolve_UnknownProductCode_ThrowsArgumentException()
    {
        var lists = new[]
        {
            List("tu", Record("00044")),
            List("tx", Record("00044"))
        };
        FluentActions.Invoking(() => _subject.Resolve("00044", lists))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Resolve_DuplicateProductCode_ThrowsArgumentException()
    {
        var lists = new[]
        {
            List("tu", Record("00044")),
            List("tu", Record("00044"))
        };
        FluentActions.Invoking(() => _subject.Resolve("00044", lists))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Resolve_IdenticalRecordsInSeveralLists_NoConflict()
    {
        var lists = new[]
        {
            List("tu", Record("00044")),
            List("ff", Record("00044")),
            List("sd", Record("00044"))
        };
        var result = _subject.Resolve("00044", lists);
        result.ProductCodes.Should().Equal("tu", "ff", "sd");
        result.Location.Name.Should().Be("Station");
    }

    [DataRow("name")]
    [DataRow("latitude")]
    [DataRow("longitude")]
    [DataRow("altitude")]
    [DataRow("altitudeMissing")]
    [TestMethod]
    public void Resolve_ConflictingRecords_ThrowsStationMetadataConflict(string field)
    {
        var lists = new[]
        {
            List("tu", Record("00044", line: 7)),
            List("rr", field switch
            {
                "name" => Record("00044", name: "Other"),
                "latitude" => Record("00044", latitude: 50.1235m),
                "longitude" => Record("00044", longitude: 8.5679m),
                "altitude" => Record("00044", altitude: 101m),
                _ => Record("00044", altitude: null)
            })
        };

        var ex = FluentActions.Invoking(() => _subject.Resolve("00044", lists)).Should().Throw<DwdStationException>().Which;
        ex.ErrorId.Should().Be(DwdStationException.StationMetadataConflictErrorId);
        ex.Diagnostic.Should().NotBeNull();
        ex.Diagnostic!.Id.Should().Be(CatalogDiagnostic.StationConflictId);
        ex.Diagnostic.EndpointName.Should().Be("tu-list");
        ex.Diagnostic.StationId.Should().Be("00044");
        ex.Diagnostic.LineNumber.Should().Be(7);
        ex.Diagnostic.Reason.Should().Contain("tu-list").And.Contain("rr-list");
        ex.Message.Should().Contain("00044").And.NotContain("Other");
    }

    [TestMethod]
    public void Resolve_ConflictingRecordsShuffledLists_SameFailure()
    {
        var lists = new[]
        {
            List("rr", Record("00044", altitude: 101m)),
            List("tu", Record("00044", line: 7))
        };

        var ex = FluentActions.Invoking(() => _subject.Resolve("00044", lists)).Should().Throw<DwdStationException>().Which;
        ex.ErrorId.Should().Be(DwdStationException.StationMetadataConflictErrorId);
        ex.Diagnostic.Should().NotBeNull();
        ex.Diagnostic!.Id.Should().Be(CatalogDiagnostic.StationConflictId);
        ex.Diagnostic.EndpointName.Should().Be("tu-list");
        ex.Diagnostic.StationId.Should().Be("00044");
        ex.Diagnostic.LineNumber.Should().Be(7);
        ex.Diagnostic.Reason.Should().Contain("tu-list").And.Contain("rr-list");
    }

    [TestMethod]
    public void Resolve_ConflictInLaterList_NamesBothLists()
    {
        var lists = new[]
        {
            List("tu", Record("00044")),
            List("ff", Record("00044")),
            List("sd", Record("00044", name: "Other"))
        };

        var ex = FluentActions.Invoking(() => _subject.Resolve("00044", lists)).Should().Throw<DwdStationException>().Which;
        ex.ErrorId.Should().Be(DwdStationException.StationMetadataConflictErrorId);
        ex.Diagnostic.Should().NotBeNull();
        ex.Diagnostic!.Id.Should().Be(CatalogDiagnostic.StationConflictId);
        ex.Diagnostic.EndpointName.Should().Be("tu-list");
        ex.Diagnostic.Reason.Should().Contain("tu-list").And.Contain("sd-list");
    }
}
