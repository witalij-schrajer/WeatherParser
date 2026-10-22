namespace WeatherParser.Parsing.Geography;

/// <summary>
/// Calculates WGS84 distances with the Vincenty inverse formula and a spherical fallback (SC-06).
/// </summary>
public interface IDistanceCalculator
{
    /// <summary>
    /// Calculates the ellipsoidal distance in kilometres on WGS84 with the Vincenty inverse formula (a = 6378137 m, f = 1/298.257223563, iteration until the longitude change is below 1e-12 rad, at most 200 iterations).
    /// For pairs where the iteration does not converge (near-antipodal points) the spherical distance is returned as the documented fallback, never an exception (SC-06).
    /// Inputs are validated decimal degrees; the result is metres / 1000 converted from double to decimal without further rounding (C-26).
    /// </summary>
    /// <param name="latitude1">The first point's latitude in decimal degrees.</param>
    /// <param name="longitude1">The first point's longitude in decimal degrees.</param>
    /// <param name="latitude2">The second point's latitude in decimal degrees.</param>
    /// <param name="longitude2">The second point's longitude in decimal degrees.</param>
    /// <returns>The distance in kilometres.</returns>
    decimal CalculateKm(decimal latitude1, decimal longitude1, decimal latitude2, decimal longitude2);

    /// <summary>
    /// Calculates the great-circle distance in kilometres on a sphere with mean radius 6371.0088 km, computed in <see cref="double"/>.
    /// (SC-06); used by the search-area pre-filter.
    /// </summary>
    /// <param name="latitude1">The first point's latitude in decimal degrees.</param>
    /// <param name="longitude1">The first point's longitude in decimal degrees.</param>
    /// <param name="latitude2">The second point's latitude in decimal degrees.</param>
    /// <param name="longitude2">The second point's longitude in decimal degrees.</param>
    /// <returns>The distance in kilometres.</returns>
    decimal SphericalKm(decimal latitude1, decimal longitude1, decimal latitude2, decimal longitude2);
}
