using System;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Stable notice and station-failure identifiers of the OsthessenWetter parser (contract OW-13).
/// </summary>
public static class OsthessenWetterNoticeIds
{
    /// <summary>
    /// The wind table was not found or its header changed; no wind readings (contract OW-11).
    /// </summary>
    public const string WindTableMissing = "OSTHESSEN-WIND-TABLE-MISSING";

    /// <summary>
    /// No or more than one wind row has the station's join key; no wind readings for that station (contract OW-05).
    /// </summary>
    public const string WindRowUnmatched = "OSTHESSEN-WIND-ROW-UNMATCHED";

    /// <summary>
    /// A value cell does not match the documented number grammar or unit; that reading is omitted (contract OW-11).
    /// </summary>
    public const string ValueUnreadable = "OSTHESSEN-VALUE-UNREADABLE";

    /// <summary>
    /// A displayed time falls into a DST gap or overlap; the readings of that table row are omitted (contract OW-07).
    /// </summary>
    public const string TimeUnresolved = "OSTHESSEN-TIME-UNRESOLVED";

    /// <summary>
    /// The page marks the station offline; station failure (contract OW-12).
    /// </summary>
    public const string StationOffline = "OSTHESSEN-STATION-OFFLINE";

    /// <summary>
    /// No consumer-supplied station metadata for a matched station; station failure (contract OW-04).
    /// </summary>
    public const string StationMetadataMissing = "OSTHESSEN-STATION-METADATA-MISSING";

    /// <summary>
    /// A matched station yields no usable reading; station failure (contract OW-08).
    /// </summary>
    public const string StationNoReading = "OSTHESSEN-STATION-NO-READING";
}
