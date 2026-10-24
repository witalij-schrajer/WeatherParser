using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Geography;

/// <summary>
/// Creates validated search parameters (SC-11).
/// </summary>
public sealed class SearchParameterFactory : ISearchParameterFactory
{
    // Accepted catalog altitude range of SC-02 and SC-04 (SC-11 representability check).
    private static readonly decimal LowestCatalogAltitudeM = -500m;
    private static readonly decimal HighestCatalogAltitudeM = 9000m;

    /// <inheritdoc />
    public SearchParameters Create(decimal latitude, decimal longitude, decimal altitudeM, decimal maxDistanceKm, decimal maxAltitudeDifferenceM)
    {
        if (latitude < -90m || latitude > 90m)
        {
            throw new SearchParameterException("Latitude must be between -90 and 90 degrees inclusive (contract SC-11).");
        }

        if (longitude < -180m || longitude > 180m)
        {
            throw new SearchParameterException("Longitude must be between -180 and 180 degrees inclusive (contract SC-11).");
        }

        if (maxDistanceKm < 0m)
        {
            throw new SearchParameterException("Maximum distance must not be negative (contract SC-11).");
        }

        if (maxAltitudeDifferenceM < 0m)
        {
            throw new SearchParameterException("Maximum altitude difference must not be negative (contract SC-11).");
        }

        if (altitudeM > decimal.MaxValue + LowestCatalogAltitudeM || altitudeM < decimal.MinValue + HighestCatalogAltitudeM)
        {
            throw new SearchParameterException("Reference altitude must keep the difference to every catalog altitude from -500 to 9000 m representable (contract SC-11).");
        }

        return new SearchParameters
        {
            Latitude = latitude,
            Longitude = longitude,
            AltitudeM = altitudeM,
            MaxDistanceKm = maxDistanceKm,
            MaxAltitudeDifferenceM = maxAltitudeDifferenceM
        };
    }
}
