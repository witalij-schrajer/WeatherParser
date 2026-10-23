using System;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Resolves the per-product availability of a MOSMIX location from the evidence (SC-05, SC-22, SC-23).
/// </summary>
public interface IMosmixAvailabilityResolver
{
    /// <summary>
    /// Returns exactly one item per product in the order <c>MOSMIX_S</c>, <c>MOSMIX_L</c>.
    /// <c>MOSMIX_S</c> from the cached index: the location's element names (ordinal order) give <c>available</c>;
    /// a location without names or without an entry in the index gives <c>unavailable</c> with no elements;
    /// both carry the index's <c>downloaded_at</c> and <c>issued_at</c> as evidence timestamps,
    /// also when the index is older than <c>MosmixSEvidenceMaxAge</c>
    /// (the resolver writes no log; the single stale notice is written by the selection).
    /// A <c>null</c> index throws <see cref="EvidenceCacheException"/> <c>MOSMIX-S-EVIDENCE-MISSING</c>.
    /// <c>MOSMIX_L</c>: <c>null</c> (HTTP 404) gives <c>unavailable</c> without elements and without evidence timestamps;
    /// a retrieval gives <c>available</c> with the evidence's elements, or <c>unavailable</c> when it has none,
    /// with the retrieval time and the file's issue time.
    /// Products never fill each other; no CDC data is used.
    /// </summary>
    /// <param name="mosmixId">The MOSMIX ID of the location.</param>
    /// <param name="mosmixSIndex">The cached index, or <c>null</c> if unavailable.</param>
    /// <param name="mosmixL">The retrieval result, or <c>null</c> if not retrieved (e.g. HTTP 404).</param>
    /// <returns>The availability information for each product.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="mosmixId"/> is empty.
    /// Thrown when <paramref name="mosmixL"/> is not <c>null</c> and its ID does not match <paramref name="mosmixId"/>.
    /// </exception>
    /// <exception cref="EvidenceCacheException">
    /// Thrown when <paramref name="mosmixSIndex"/> is <c>null</c>.
    /// </exception>
    IReadOnlyList<MosmixProductAvailability> Resolve(string mosmixId, MosmixSEvidenceIndex? mosmixSIndex, MosmixLRetrieval? mosmixL);
}
