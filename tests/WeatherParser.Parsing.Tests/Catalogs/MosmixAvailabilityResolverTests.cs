using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Export;
using WeatherParser.Parsing.Tests.Schemas;

namespace WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class MosmixAvailabilityResolverTests
{
    private IMosmixAvailabilityResolver _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new MosmixAvailabilityResolver();
    }

    [TestMethod]
    public void Resolve_MissingIndex_ThrowsEvidenceMissing()
    {
        Action act = () => _subject.Resolve("X123A", null, L("X123A", "TTT"));
        act.Should().Throw<EvidenceCacheException>()
            .Which.ErrorId.Should().Be("MOSMIX-S-EVIDENCE-MISSING");
    }

    [TestMethod]
    public void Resolve_ReturnsExactlyOneItemPerProduct()
    {
        var result = _subject.Resolve("X123A", Index(("X123A", new[] { "TTT" })), L("X123A", "TTT"));
        result.Select(p => p.Product).Should().Equal(MosmixProduct.MosmixS, MosmixProduct.MosmixL);
    }

    [TestMethod]
    public void Resolve_AvailableProduct_ListsOnlyVerifiedElements()
    {
        var result = _subject.Resolve("X123A", Index(("X123A", new[] { "TTT", "FF", "DD" })), L("X123A", "TTT", "RR1c"));
        result[0].Availability.Should().Be(AvailabilityState.Available);
        result[0].AvailableForecastElements.Should().Equal("DD", "FF", "TTT");
        result[1].Availability.Should().Be(AvailabilityState.Available);
        result[1].AvailableForecastElements.Should().Equal("RR1c", "TTT");
    }

    [TestMethod]
    public void Resolve_EvidenceTimestamps_Set()
    {
        var result = _subject.Resolve("X123A", Index(("X123A", new[] { "TTT", "FF", "DD" })), L("X123A", "TTT", "RR1c"));
        result[0].EvidenceRetrievedAtUtc.Should().Be(Downloaded);
        result[0].EvidenceIssuedAtUtc.Should().Be(IndexIssued);
        result[1].EvidenceRetrievedAtUtc.Should().Be(Retrieved);
        result[1].EvidenceIssuedAtUtc.Should().Be(LIssued);
    }

    [TestMethod]
    public void Resolve_StaleIndex_UsedWithItsTimestamps()
    {
        var staleIndex = Index(("X123A", new[] { "TTT" })) with { DownloadedAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var result = _subject.Resolve("X123A", staleIndex, L("X123A", "TTT"));
        result[0].Availability.Should().Be(AvailabilityState.Available);
        result[0].EvidenceRetrievedAtUtc.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        result[0].EvidenceIssuedAtUtc.Should().Be(IndexIssued);
    }

    [TestMethod]
    public void Resolve_MosmixLAbsent_IsUnavailable()
    {
        var result = _subject.Resolve("X123A", Index(("X123A", new[] { "TTT" })), null);
        result[1].Availability.Should().Be(AvailabilityState.Unavailable);
        result[1].AvailableForecastElements.Should().BeEmpty();
        result[1].EvidenceRetrievedAtUtc.Should().BeNull();
        result[1].EvidenceIssuedAtUtc.Should().BeNull();
    }

    [TestMethod]
    public void Resolve_NoLocationEvidence_IsUnavailable()
    {
        var result = _subject.Resolve("X123A", Index(("B456", new[] { "TTT" })), L("X123A", "TTT"));
        result[0].Availability.Should().Be(AvailabilityState.Unavailable);
        result[0].AvailableForecastElements.Should().BeEmpty();
        result[0].EvidenceRetrievedAtUtc.Should().Be(Downloaded);
        result[0].EvidenceIssuedAtUtc.Should().Be(IndexIssued);
    }

    [TestMethod]
    public void Resolve_ProductWideElementListWithoutLocationEvidence_IsUnavailable()
    {
        var result = _subject.Resolve("X123A", Index(("B456", new[] { "DD", "FF", "TTT" }), ("C789", new[] { "PPPP" })), L("X123A"));
        result[0].Availability.Should().Be(AvailabilityState.Unavailable);
        result[0].AvailableForecastElements.Should().BeEmpty();
        result[1].Availability.Should().Be(AvailabilityState.Unavailable);
        result[1].AvailableForecastElements.Should().BeEmpty();
    }

    [TestMethod]
    public void Resolve_UnavailableProduct_HasEmptyElements()
    {
        var result = _subject.Resolve("X123A", Index(("X123A", new string[0])), L("X123A"));
        result[0].Availability.Should().Be(AvailabilityState.Unavailable);
        result[0].AvailableForecastElements.Should().BeEmpty();
        result[1].Availability.Should().Be(AvailabilityState.Unavailable);
        result[1].AvailableForecastElements.Should().BeEmpty();
        result[0].EvidenceRetrievedAtUtc.Should().Be(Downloaded);
        result[0].EvidenceIssuedAtUtc.Should().Be(IndexIssued);
        result[1].EvidenceRetrievedAtUtc.Should().Be(Retrieved);
        result[1].EvidenceIssuedAtUtc.Should().Be(LIssued);
    }

    [TestMethod]
    public void Resolve_ProductsAreIndependent()
    {
        // (a)
        var resultA = _subject.Resolve("X123A", Index(("X123A", new[] { "TTT" })), null);
        resultA[0].Availability.Should().Be(AvailabilityState.Available);
        resultA[1].Availability.Should().Be(AvailabilityState.Unavailable);

        // (b)
        var resultB = _subject.Resolve("X123A", Index(("B456", new[] { "TTT" })), L("X123A", "TTT"));
        resultB[0].Availability.Should().Be(AvailabilityState.Unavailable);
        resultB[1].Availability.Should().Be(AvailabilityState.Available);
    }

    [TestMethod]
    public void Resolve_NeverUsesCdcData()
    {
        var result = _subject.Resolve("00044", Index(("00044", new[] { "TTT" })), null);
        result[0].Availability.Should().Be(AvailabilityState.Available);
        result[0].AvailableForecastElements.Should().Equal("TTT");
    }

    [TestMethod]
    public void Resolve_MosmixLEvidenceForAnotherId_ThrowsArgumentException()
    {
        Action act = () => _subject.Resolve("X123A", Index(), L("B456", "TTT"));
        act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("mosmixL");
    }

    [TestMethod]
    public void Resolve_EmptyId_ThrowsArgumentException()
    {
        Action act = () => _subject.Resolve("", null, null!);
        act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("mosmixId");
    }

    [TestMethod]
    public void Resolve_NullId_ThrowsArgumentException()
    {
        Action act = () => _subject.Resolve(null!, null, null);
        act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("mosmixId");
    }

    [TestMethod]
    public void Resolve_IsDeterministicForIdenticalInput()
    {
        var input1 = Index(("X123A", new[] { "TTT" }));
        var input2 = Index(("X123A", new[] { "TTT" }));
        var result1 = _subject.Resolve("X123A", input1, L("X123A", "TTT"));
        var result2 = _subject.Resolve("X123A", input2, L("X123A", "TTT"));
        result1.Select(p => p.Product).Should().Equal(result2.Select(p => p.Product));
        result1.Select(p => p.Availability).Should().Equal(result2.Select(p => p.Availability));
        result1.SelectMany(p => p.AvailableForecastElements).Should().Equal(result2.SelectMany(p => p.AvailableForecastElements));
    }

    [TestMethod]
    public async Task Resolve_ResultPassesP2ListValidation()
    {
        var list = ListTestData.ValidMosmixList();
        list.Locations[0].Products = _subject.Resolve("X123A", Index(("X123A", new[] { "TTT" })), null).ToList();
        var jsonBytes = await AllRootsTestData.JsonBytesAsync(list, new JsonExportSerializer());
        var xmlBytes = await AllRootsTestData.XmlBytesAsync(list, new XmlExportSerializer());
        ExportSchemaValidation.Validate(xmlBytes).Should().BeEmpty();
        jsonBytes.Should().NotBeEmpty();
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesMosmixAvailabilityResolver()
    {
        using var provider = new ServiceCollection()
            .AddWeatherParsing()
            .AddTestConsumerServices()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        var resolver = provider.GetRequiredService<IMosmixAvailabilityResolver>();
        resolver.Should().BeOfType<MosmixAvailabilityResolver>();
    }

    private static readonly DateTime Downloaded = new(2026, 10, 21, 4, 12, 7, DateTimeKind.Utc);
    private static readonly DateTime IndexIssued = new(2026, 10, 21, 3, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Retrieved = new(2026, 10, 21, 4, 30, 15, DateTimeKind.Utc);
    private static readonly DateTime LIssued = new(2026, 10, 21, 3, 0, 0, DateTimeKind.Utc);

    private static MosmixSEvidenceIndex Index(params (string Id, string[] Names)[] locations)
        => new("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz", Downloaded, IndexIssued, "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", 39321600, locations.ToDictionary(l => l.Id, l => (IReadOnlyList<string>)l.Names));

    private static MosmixLRetrieval L(string id, params string[] elements)
        => new(Retrieved, new MosmixLEvidence(id, LIssued, elements));
}
