using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// A notice about one quantity of a mapped station: OSTHESSEN-VALUE-UNREADABLE or OSTHESSEN-TIME-UNRESOLVED (contract OW-07, OW-11); it never carries page text.
/// </summary>
/// <param name="NoticeId">One of the OsthessenWetterNoticeIds values.</param>
/// <param name="Kind">The quantity kind of the unreadable value or unresolved time.</param>
public sealed record OsthessenWetterMappingNotice(string NoticeId, QuantityKind Kind);
