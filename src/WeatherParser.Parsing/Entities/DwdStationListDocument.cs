namespace WeatherParser.Parsing.Entities;

/// <summary>
/// Document envelope for the DWD CDC station list produced by <c>--SelectDWD</c>, per REQ-023.
/// Entries are sorted ascending by distance, then by station id. All envelope fields are
/// inherited from <see cref="StationListDocumentBase{TEntry}"/>.
/// </summary>
[Serializable]
public sealed class DwdStationListDocument : StationListDocumentBase<DwdStationListEntry>;
