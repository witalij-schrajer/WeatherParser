namespace WeatherParser.Parsing.Geography;

/// <summary>
/// Calculates WGS84 distances with the Vincenty inverse formula and a spherical fallback (SC-06).
/// </summary>
public sealed class DistanceCalculator : IDistanceCalculator
{
    private const double SemiMajorAxisM = 6378137.0;
    private const double Flattening = 1.0 / 298.257223563;
    private const double SemiMinorAxisM = SemiMajorAxisM * (1.0 - Flattening);
    private const double MeanRadiusM = 6371008.8;
    private const double ConvergenceThresholdRad = 1e-12;
    private const int MaxIterations = 200;

    private static double Square(double value) => value * value;

    /// <inheritdoc />
    public decimal CalculateKm(decimal latitude1, decimal longitude1, decimal latitude2, decimal longitude2)
    {
        if (IsSamePole(latitude1, latitude2))
        {
            return 0m; // same pole, any longitude
        }

        var L = ToRadians(LongitudeDifferenceDegrees(longitude1, longitude2));
        var U1 = Math.Atan((1 - Flattening) * Math.Tan(ToRadians(latitude1)));
        var U2 = Math.Atan((1 - Flattening) * Math.Tan(ToRadians(latitude2)));
        var sinU1 = Math.Sin(U1);
        var cosU1 = Math.Cos(U1);
        var sinU2 = Math.Sin(U2);
        var cosU2 = Math.Cos(U2);

        var lambda = L;
        double lambdaPrevious;

        for (var iteration = 0; iteration < MaxIterations; iteration++)
        {
            var sinLambda = Math.Sin(lambda);
            var cosLambda = Math.Cos(lambda);
            var sinSigma = Math.Sqrt(Square(cosU2 * sinLambda) +
                                     Square(cosU1 * sinU2 - sinU1 * cosU2 * cosLambda));
            if (sinSigma == 0)
            {
                return 0m; // coincident points
            }

            var cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambda;
            var sigma = Math.Atan2(sinSigma, cosSigma);
            var sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
            var cosSqAlpha = 1 - Square(sinAlpha);
            var cos2SigmaM = cosSqAlpha != 0 ? cosSigma - 2 * sinU1 * sinU2 / cosSqAlpha : 0; // equatorial line

            var C = Flattening / 16 * cosSqAlpha * (4 + Flattening * (4 - 3 * cosSqAlpha));
            lambdaPrevious = lambda;
            lambda = L + (1 - C) * Flattening * sinAlpha *
                     (sigma + C * sinSigma *
                      (cos2SigmaM + C * cosSigma * (-1 + 2 * Square(cos2SigmaM))));

            if (Math.Abs(lambda - lambdaPrevious) < ConvergenceThresholdRad)
            {
                var uSq = cosSqAlpha * (Square(SemiMajorAxisM) - Square(SemiMinorAxisM)) / Square(SemiMinorAxisM);
                var A = 1 + uSq / 16384 * (4096 + uSq * (-768 + uSq * (320 - 175 * uSq)));
                var B = uSq / 1024 * (256 + uSq * (-128 + uSq * (74 - 47 * uSq)));
                var deltaSigma = B * sinSigma *
                                 (cos2SigmaM + B / 4 *
                                  (cosSigma * (-1 + 2 * Square(cos2SigmaM)) -
                                   B / 6 * Square(cos2SigmaM) * (-3 + 4 * Square(sinSigma)) *
                                   (-3 + 4 * Square(cos2SigmaM))));

                return (decimal)(SemiMinorAxisM * A * (sigma - deltaSigma) / 1000.0);
            }
        }

        // no convergence: documented fallback (SC-06)
        return SphericalKm(latitude1, longitude1, latitude2, longitude2);
    }

    /// <inheritdoc />
    public decimal SphericalKm(decimal latitude1, decimal longitude1, decimal latitude2, decimal longitude2)
    {
        if (IsSamePole(latitude1, latitude2))
        {
            return 0m; // same pole, any longitude
        }

        var phi1 = ToRadians(latitude1);
        var phi2 = ToRadians(latitude2);
        var deltaPhi = phi2 - phi1;
        var deltaLambda = ToRadians(LongitudeDifferenceDegrees(longitude1, longitude2));

        var h = Square(Math.Sin(deltaPhi / 2)) +
                Math.Cos(phi1) * Math.Cos(phi2) * Square(Math.Sin(deltaLambda / 2));

        return (decimal)(2 * MeanRadiusM * Math.Asin(Math.Sqrt(h)) / 1000.0);
    }

    private static double ToRadians(decimal degrees) => (double)degrees * Math.PI / 180.0;

    private static decimal LongitudeDifferenceDegrees(decimal longitude1, decimal longitude2)
    {
        var difference = longitude2 - longitude1;
        if (difference > 180m)
        {
            difference -= 360m;
        }
        else if (difference < -180m)
        {
            difference += 360m;
        }

        return difference;
    }

    private static bool IsSamePole(decimal latitude1, decimal latitude2) => (latitude1 == 90m && latitude2 == 90m) || (latitude1 == -90m && latitude2 == -90m);
}
