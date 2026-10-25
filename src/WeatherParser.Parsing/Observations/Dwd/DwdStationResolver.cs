using System.Text.RegularExpressions;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Observations.Dwd;

/// <summary>
/// Default implementation of <see cref="IDwdStationResolver"/>; stateless and synchronous.
/// </summary>
public sealed class DwdStationResolver : IDwdStationResolver
{
    private static readonly Regex StationIdPattern = new(@"\A[0-9]{5}\z", RegexOptions.CultureInvariant);

    /// <inheritdoc />
    public string ValidateId(string stationId)
    {
        if (stationId is null || !StationIdPattern.IsMatch(stationId))
        {
            throw new DwdStationException(DwdStationException.StationIdInvalidErrorId, "The station ID must be exactly five ASCII digits, for example 00044 (contract OC-01).");
        }

        return stationId;
    }

    /// <inheritdoc />
    public DwdResolvedStation Resolve(string stationId, IReadOnlyList<CdcProductList> lists)
    {
        var id = ValidateId(stationId);
        ArgumentNullException.ThrowIfNull(lists);
        
        var ordered = new CdcProductList?[4];
        foreach (var list in lists)
        {
            var rank = ProductRank(list.ProductCode);
            if (rank < 0)
            {
                throw new ArgumentException("Only the CDC product lists tu, ff, rr and sd are supported (contract OC-02).", nameof(lists));
            }
            
            if (ordered[rank] is not null)
            {
                throw new ArgumentException("Each CDC product list may be supplied only once (contract OC-02).", nameof(lists));
            }
            
            ordered[rank] = list;
        }

        var productCodes = new List<string>();
        CdcStationRecord? first = null;
        CdcProductList? firstList = null;
        
        foreach (var list in ordered)
        {
            if (list is null)
            {
                continue;
            }
            
            var record = list.Catalog.Records.FirstOrDefault(r => string.Equals(r.StationId, id, StringComparison.Ordinal));
            if (record is not null)
            {
                if (first is null)
                {
                    first = record;
                    firstList = list;
                }
                else if (!SameMetadata(first, record))
                {
                    var diagnostic = new CatalogDiagnostic(CatalogDiagnostic.StationConflictId, firstList!.EndpointName, first.LineNumber, id, $"Altitude, coordinates or name of the station differ between the station lists '{firstList.EndpointName}' and '{list.EndpointName}' (contract OC-02).");
                    throw new DwdStationException(DwdStationException.StationMetadataConflictErrorId, $"Station {id} has conflicting metadata in the CDC station lists '{firstList.EndpointName}' and '{list.EndpointName}', so its location cannot be stated (contract OC-02).", diagnostic);
                }
                
                productCodes.Add(list.ProductCode);
            }
        }

        if (first is null)
        {
            throw new DwdStationException(DwdStationException.StationUnknownErrorId, $"Station {id} is not in any CDC station list (contract OC-02).");
        }

        return new DwdResolvedStation(
            id,
            new ObservationLocation
            {
                StationId = id,
                Name = first.Name,
                Latitude = first.Latitude,
                Longitude = first.Longitude,
                AltitudeM = first.AltitudeM
            },
            productCodes);
    }
    
    /// <summary>
    /// The position of a product code in the canonical order tu, ff, rr, sd, or -1 for an unsupported code.
    /// </summary>
    private static int ProductRank(string productCode) => productCode switch
    {
        "tu" => 0,
        "ff" => 1,
        "rr" => 2,
        "sd" => 3,
        _ => -1
    };

    /// <summary>
    /// True when two records of the same station agree in name, coordinates and altitude (an altitude present in one record and absent in the other differs).
    /// </summary>
    private static bool SameMetadata(CdcStationRecord a, CdcStationRecord b) => string.Equals(a.Name, b.Name, StringComparison.Ordinal) && a.Latitude == b.Latitude && a.Longitude == b.Longitude && a.AltitudeM == b.AltitudeM;
}
