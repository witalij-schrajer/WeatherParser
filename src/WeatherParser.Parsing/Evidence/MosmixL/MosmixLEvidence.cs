using System;
using System.Collections.Generic;

namespace WeatherParser.Parsing.Evidence.MosmixL;

/// <summary>
/// The validated content of one MOSMIX_L single-station file (SC-20):
/// the requested MOSMIX ID;
/// the file's <c>dwd:IssueTime</c> as a whole-second UTC value (<see cref="DateTimeKind.Utc"/>);
/// the names of the elements whose series contain at least one numeric value,
/// in ordinal order (empty when every series is missing;
/// the resolver then reports <c>unavailable</c>, SC-05).
/// </summary>
/// <param name="MosmixId">The requested MOSMIX ID.</param>
/// <param name="IssuedAtUtc">The file's <c>dwd:IssueTime</c> as a whole-second UTC value (<see cref="DateTimeKind.Utc"/>).</param>
/// <param name="AvailableElements">The names of the elements whose series contain at least one numeric value, in ordinal order.</param>
public sealed record MosmixLEvidence(
    string MosmixId,
    DateTime IssuedAtUtc,
    IReadOnlyList<string> AvailableElements);
