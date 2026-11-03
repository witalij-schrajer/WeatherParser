using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Derives CDC station availability from the station lists and the <c>now</c> evidence (SC-03).
/// </summary>
public interface ICdcAvailabilityDeriver
{
    /// <summary>
    /// Selects candidate stations from the lists.
    /// </summary>
    /// <param name="lists">The product lists.</param>
    /// <param name="referenceTimeUtc">The reference time in UTC with zero offset.</param>
    /// <returns>The candidate stations and diagnostics.</returns>
    /// <exception cref="WeatherParser.Parsing.Exceptions.CatalogFormatException">Thrown when a list has all records malformed.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="referenceTimeUtc"/> does not have zero offset.</exception>
    CdcCandidates SelectCandidates(IReadOnlyList<CdcProductList> lists, DateTimeOffset referenceTimeUtc);

    /// <summary>
    /// Derives the availability of stations.
    /// </summary>
    /// <param name="candidates">The candidate stations.</param>
    /// <param name="evidence">The evidence for each station and product.</param>
    /// <param name="referenceTimeUtc">The reference time in UTC with zero offset.</param>
    /// <returns>The availability result.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="referenceTimeUtc"/> does not have zero offset.</exception>
    CdcAvailabilityResult Derive(CdcCandidates candidates, IReadOnlyDictionary<CdcEvidenceKey, CdcNowEvidence?> evidence, DateTimeOffset referenceTimeUtc);
}
