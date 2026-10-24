using System;
using System.Linq;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Resolves the per-product availability of a MOSMIX location from the evidence (SC-05, SC-22, SC-23).
/// </summary>
public sealed class MosmixAvailabilityResolver : IMosmixAvailabilityResolver
{
    /// <inheritdoc />
    public IReadOnlyList<MosmixProductAvailability> Resolve(string mosmixId, MosmixSEvidenceIndex? mosmixSIndex, MosmixLRetrieval? mosmixL)
    {
        ArgumentException.ThrowIfNullOrEmpty(mosmixId);
        if (mosmixL is not null && !string.Equals(mosmixL.Evidence.MosmixId, mosmixId, StringComparison.Ordinal))
        {
            throw new ArgumentException("The MOSMIX_L evidence belongs to another location.", nameof(mosmixL));
        }
        if (mosmixSIndex is null)
        {
            throw new EvidenceCacheException(EvidenceCacheException.EvidenceMissingErrorId, "No usable MOSMIX_S evidence index is available; run the MOSMIX_S evidence refresh first (contract SC-22).");
        }

        var sNames = mosmixSIndex.Locations.TryGetValue(mosmixId, out var names) ? names : Array.Empty<string>();
        var mosmixS = Item(MosmixProduct.MosmixS, sNames, mosmixSIndex.DownloadedAtUtc, mosmixSIndex.IssuedAtUtc);
        var mosmixLItem = mosmixL is null
            ? Item(MosmixProduct.MosmixL, Array.Empty<string>(), null, null)
            : Item(MosmixProduct.MosmixL, mosmixL.Evidence.AvailableElements, mosmixL.RetrievedAtUtc, mosmixL.Evidence.IssuedAtUtc);
        return new[] { mosmixS, mosmixLItem };
    }

    private static MosmixProductAvailability Item(MosmixProduct product, IReadOnlyList<string> elements, DateTime? retrievedAtUtc, DateTime? issuedAtUtc)
    {
        var ordered = elements.OrderBy(element => element, StringComparer.Ordinal).ToList();
        return new MosmixProductAvailability
        {
            Product = product,
            Availability = ordered.Count > 0 ? AvailabilityState.Available : AvailabilityState.Unavailable,
            AvailableForecastElements = ordered,
            EvidenceRetrievedAtUtc = retrievedAtUtc,
            EvidenceIssuedAtUtc = issuedAtUtc,
        };
    }
}
