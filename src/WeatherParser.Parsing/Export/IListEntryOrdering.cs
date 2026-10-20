
namespace WeatherParser.Parsing.Export;

/// <summary>
/// Orders entries by distance_km ascending, then by ID with ordinal comparison.
/// </summary>
public interface IListEntryOrdering
{
    /// <summary>
    /// Orders CDC station list entries by distance_km ascending, then by ID with ordinal comparison.
    /// </summary>
    /// <param name="entries">The entries to order.</param>
    /// <returns>The ordered entries.</returns>
    IReadOnlyList<CdcStationListEntry> Order(IEnumerable<CdcStationListEntry> entries);

    /// <summary>
    /// Orders MOSMIX location list entries by distance_km ascending, then by ID with ordinal comparison.
    /// </summary>
    /// <param name="entries">The entries to order.</param>
    /// <returns>The ordered entries.</returns>
    IReadOnlyList<MosmixLocationListEntry> Order(IEnumerable<MosmixLocationListEntry> entries);
}
