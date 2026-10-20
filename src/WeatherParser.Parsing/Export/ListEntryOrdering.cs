
namespace WeatherParser.Parsing.Export;

/// <summary>
/// Orders list entries by distance_km ascending, then by ID with ordinal comparison.
/// </summary>
public sealed class ListEntryOrdering : IListEntryOrdering
{
    /// <inheritdoc />
    public IReadOnlyList<CdcStationListEntry> Order(IEnumerable<CdcStationListEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return entries.OrderBy(e => e.DistanceKm).ThenBy(e => e.StationId, StringComparer.Ordinal).ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<MosmixLocationListEntry> Order(IEnumerable<MosmixLocationListEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return entries.OrderBy(e => e.DistanceKm).ThenBy(e => e.MosmixId, StringComparer.Ordinal).ToList();
    }
}
