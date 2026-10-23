using System;
using System.Collections.Generic;

namespace WeatherParser.Parsing.Evidence.MosmixS;

/// <summary>
/// Represents the MOSMIX_S evidence index of SC-22.
/// </summary>
/// <param name="SourceUri">The sanitized source URI (scheme, host, path; no query, no user info).</param>
/// <param name="DownloadedAtUtc">When the download completed.</param>
/// <param name="IssuedAtUtc">The file's <c>dwd:IssueTime</c> (UTC, whole seconds).</param>
/// <param name="FileSha256">The lowercase hexadecimal SHA-256.</param>
/// <param name="FileSize">The size in bytes of the downloaded file.</param>
/// <param name="Locations">Per placemark ID (<c>^[A-Z0-9]{{4,5}}$</c>) the element names (<c>^[A-Za-z0-9_]{{1,16}}$</c>, at most 200, unique, ordinal order) whose series contain at least one numeric value.</param>
public sealed record MosmixSEvidenceIndex(
    string SourceUri,
    DateTime DownloadedAtUtc,
    DateTime IssuedAtUtc,
    string FileSha256,
    long FileSize,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Locations);
