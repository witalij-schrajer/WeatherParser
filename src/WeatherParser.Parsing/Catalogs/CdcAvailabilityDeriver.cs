using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Derives CDC station availability from the station lists and the <c>now</c> evidence (SC-03).
/// </summary>
public sealed class CdcAvailabilityDeriver : ICdcAvailabilityDeriver
{
    private readonly IQuantityValidator _quantityValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CdcAvailabilityDeriver"/> class.
    /// </summary>
    /// <param name="quantityValidator">The quantity validator.</param>
    public CdcAvailabilityDeriver(IQuantityValidator quantityValidator)
    {
        ArgumentNullException.ThrowIfNull(quantityValidator);
        _quantityValidator = quantityValidator;
    }

    /// <inheritdoc />
    public CdcCandidates SelectCandidates(IReadOnlyList<CdcProductList> lists, DateTimeOffset referenceTimeUtc)
    {
        ArgumentNullException.ThrowIfNull(lists);
        if (referenceTimeUtc.Offset != TimeSpan.Zero) throw new ArgumentException("The reference time must be UTC (offset zero).", nameof(referenceTimeUtc));

        var productCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var list in lists)
        {
            if (list.ProductCode is not ("tu" or "ff" or "rr" or "sd") || !productCodes.Add(list.ProductCode))
            {
                throw new ArgumentException("Each list must have a distinct product code tu, ff, rr or sd.", nameof(lists));
            }
        }

        var referenceDate = DateOnly.FromDateTime(referenceTimeUtc.UtcDateTime);
        var diagnostics = new List<CatalogDiagnostic>();
        var valid = new List<(CdcProductList List, CdcStationRecord Record)>();
        foreach (var list in lists)
        {
            var validInList = 0;
            foreach (var record in list.Catalog.Records)
            {
                if (record.FromDate > record.ToDate || record.ToDate > referenceDate.AddDays(1))
                {
                    diagnostics.Add(new CatalogDiagnostic(CatalogDiagnostic.RecordSkippedId, list.EndpointName, record.LineNumber, record.StationId, "List dates are inconsistent or lie after the reference date plus one day (contract SC-03)."));
                    continue;
                }
                validInList++;
                valid.Add((list, record));
            }
            if (list.Catalog.Records.Count > 0 && validInList == 0)
            {
                throw new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "A station list contains no record with valid dates (contract SC-10).");
            }
        }
        var stations = new List<CdcCandidateStation>();
        foreach (var group in valid.GroupBy(v => v.Record.StationId, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var first = group.First().Record;
            var altitudes = group.Select(v => v.Record.AltitudeM).Where(a => a is not null).Distinct().Count();
            var conflict = altitudes > 1 || group.Any(v => v.Record.Latitude != first.Latitude || v.Record.Longitude != first.Longitude || !string.Equals(v.Record.Name, first.Name, StringComparison.Ordinal));
            if (conflict)
            {
                diagnostics.Add(new CatalogDiagnostic(CatalogDiagnostic.StationConflictId, group.First().List.EndpointName, first.LineNumber, group.Key, "Altitude, coordinates or name differ between station lists (contract SC-02)."));
                continue;
            }
            var products = group.Where(v => v.Record.FromDate <= referenceDate && v.Record.ToDate >= referenceDate.AddDays(-1))
                .Select(v => v.List.ProductCode).Distinct().OrderBy(p => p, StringComparer.Ordinal).ToList();
            if (products.Count == 0)
            {
                continue; // inactive station: no candidate product, no notice (SC-03)
            }

            var missingAltitude = group.FirstOrDefault(v => v.Record.AltitudeM is null);
            decimal? altitude = first.AltitudeM;
            if (missingAltitude.Record is not null)
            {
                altitude = null;
                diagnostics.Add(new CatalogDiagnostic(CatalogDiagnostic.StationNoAltitudeId, missingAltitude.List.EndpointName, missingAltitude.Record.LineNumber, group.Key, "The station has no altitude and cannot be filtered (contract SC-02)."));
            }
            stations.Add(new CdcCandidateStation(group.Key, altitude, first.Latitude, first.Longitude, first.Name, first.FederalState, products));
        }
        return new CdcCandidates(stations, diagnostics);
    }

    /// <inheritdoc />
    public CdcAvailabilityResult Derive(CdcCandidates candidates, IReadOnlyDictionary<CdcEvidenceKey, CdcNowEvidence?> evidence, DateTimeOffset referenceTimeUtc)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(evidence);
        if (referenceTimeUtc.Offset != TimeSpan.Zero) { throw new ArgumentException("The reference time must be UTC (offset zero).", nameof(referenceTimeUtc)); }
        var windowStart = referenceTimeUtc.UtcDateTime.AddHours(-3);
        var windowEnd = referenceTimeUtc.UtcDateTime.AddMinutes(10);
        var stations = new List<CdcStationAvailability>();
        foreach (var station in candidates.Stations)
        {
            var names = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var product in station.CandidateProducts)
            {
                if (!evidence.TryGetValue(new CdcEvidenceKey(station.StationId, product), out var productEvidence))
                {
                    throw new ArgumentException("Evidence is missing for a candidate product; pass null for an absent file.", nameof(evidence));
                }
                if (productEvidence is null)
                {
                    continue;   // HTTP 404: the product contributes nothing (SC-03)
                }
                foreach (var (column, kind, measurementNames) in ProductMapping(product))
                {
                    if (productEvidence.Rows.Any(row => row.MeasuredAtUtc >= windowStart && row.MeasuredAtUtc <= windowEnd
                        && row.Values.TryGetValue(column, out var token) && IsUsable(token, kind)))
                    {
                        names.UnionWith(measurementNames);
                    }
                }
            }
            if (names.Count > 0)
            {
                stations.Add(new CdcStationAvailability(station, names.ToList()));
            }
        }
        return new CdcAvailabilityResult(stations, candidates.Diagnostics);
    }

    private static (string Column, QuantityKind Kind, string[] Names)[] ProductMapping(string productCode) => productCode switch
    {
        "tu" => [("PP_10", QuantityKind.Pressure, ["pressure_hpa", "pressure_mmhg"]), ("TT_10", QuantityKind.Temperature, ["temperature_c"]), ("RF_10", QuantityKind.RelativeHumidity, ["humidity_percent"])],
        "ff" => [("FF_10", QuantityKind.MeanWindSpeed, ["wind_speed_kmh"]), ("DD_10", QuantityKind.WindDirection, ["wind_direction_deg"])],
        "rr" => [("RWS_10", QuantityKind.PrecipitationAccumulation, ["rain_rate_mm_h"])],
        "sd" => [("GS_10", QuantityKind.RadiationEnergy, ["solar_irradiance_w_m2"])],
        _ => throw new ArgumentException("Unknown CDC product code.", nameof(productCode))
    };

    private bool IsUsable(string token, QuantityKind kind)
    {
        if (!decimal.TryParse(token, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        // Numerically -999 means missing, whatever the spelling (SC-03).
        if (value == -999m)
        {
            return false;
        }

        return _quantityValidator.Validate(kind, (double)value).IsValid;
    }
}
