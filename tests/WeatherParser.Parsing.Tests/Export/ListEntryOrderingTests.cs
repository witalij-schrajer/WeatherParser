using FluentAssertions;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ListEntryOrderingTests
{
    private IListEntryOrdering _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new ListEntryOrdering();
    }

    [TestMethod]
    public void Order_CdcEntries_SortsByDistanceThenOrdinalId()
    {
        var input = new[]
        {
            Cdc("44", 2.0m),
            Cdc("00100", 2.0m),
            Cdc("00044", 2.0m),
            Cdc("99999", 1.0m)
        };

        var result = _subject.Order(input);

        result.Select(e => e.StationId).Should().Equal("99999", "00044", "00100", "44");
    }

    [TestMethod]
    public void Order_MosmixEntries_SortsByDistanceThenOrdinalId()
    {
        var input = new[]
        {
            Mosmix("X123A", 3.5m),
            Mosmix("01234", 3.5m),
            Mosmix("P0001", 0.5m),
            Mosmix("x123a", 3.5m)
        };

        var result = _subject.Order(input);

        result.Select(e => e.MosmixId).Should().Equal("P0001", "01234", "X123A", "x123a");
    }

    [TestMethod]
    public void Order_IdenticalInput_ProducesIdenticalOrder()
    {
        var input = new[]
        {
            Cdc("44", 2.0m),
            Cdc("00100", 2.0m)
        };

        var result1 = _subject.Order(input);
        var result2 = _subject.Order(input);

        result1.Select(e => e.StationId).Should().Equal(result2.Select(e => e.StationId));
    }

    [TestMethod]
    public void Order_EmptyCdcInput_ReturnsEmpty()
    {
        var result = _subject.Order(Enumerable.Empty<CdcStationListEntry>());

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void Order_EmptyMosmixInput_ReturnsEmpty()
    {
        var result = _subject.Order(Enumerable.Empty<MosmixLocationListEntry>());

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void Order_NullCdcInput_ThrowsArgumentNullException()
    {
        IEnumerable<CdcStationListEntry> nullEntries = null!;

        Action act = () => _subject.Order(nullEntries);

        act.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Order_NullMosmixInput_ThrowsArgumentNullException()
    {
        IEnumerable<MosmixLocationListEntry> nullEntries = null!;

        Action act = () => _subject.Order(nullEntries);

        act.Should().Throw<ArgumentNullException>();
    }

    private static CdcStationListEntry Cdc(string id, decimal distanceKm)
    {
        return new CdcStationListEntry { StationId = id, DistanceKm = distanceKm };
    }

    private static MosmixLocationListEntry Mosmix(string id, decimal distanceKm)
    {
        return new MosmixLocationListEntry { MosmixId = id, DistanceKm = distanceKm };
    }
}
