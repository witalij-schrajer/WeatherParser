namespace WeatherParser.Parsing.Entities;

/// <summary>
/// Document envelope for the MOSMIX location list produced by <c>--SelectMOSMIX</c>, per REQ-101.
/// Entries are sorted ascending by distance, then by MOSMIX id. All envelope fields are
/// inherited from <see cref="StationListDocumentBase{TEntry}"/>.
/// </summary>
[Serializable]
public sealed class MosmixStationListDocument : StationListDocumentBase<MosmixStationListEntry>;
