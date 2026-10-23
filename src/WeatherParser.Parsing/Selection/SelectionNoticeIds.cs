using System;

namespace WeatherParser.Parsing.Selection;

/// <summary>
/// Event names of the notices written by the selectors (contract SC-08, SC-17).
/// </summary>
public static class SelectionNoticeIds
{
    /// <summary>
    /// Information: a valid selection without matches returned an empty list, SC-10.
    /// </summary>
    public const string SelectionEmpty = "SELECTION-EMPTY";

    /// <summary>
    /// Warning: the MOSMIX_S evidence index is older than <see cref="WeatherParser.Parsing.Evidence.MosmixS.MosmixCatalogEndpoints.MosmixSEvidenceMaxAge"/> and is still used, SC-22.
    /// </summary>
    public const string MosmixSEvidenceStale = "MOSMIX-S-EVIDENCE-STALE";
}
