using System;

namespace WeatherParser.Parsing.Evidence.MosmixL;

/// <summary>
/// Represents one successfully retrieved and validated MOSMIX_L single-station file.
/// </summary>
/// <param name="RetrievedAtUtc">
/// The completion time of that location's file request (UTC, whole seconds, C-117).
/// </param>
/// <param name="Evidence">
/// The validated file content (SC-20).
/// </param>
public sealed record MosmixLRetrieval(
    DateTime RetrievedAtUtc,
    MosmixLEvidence Evidence);
