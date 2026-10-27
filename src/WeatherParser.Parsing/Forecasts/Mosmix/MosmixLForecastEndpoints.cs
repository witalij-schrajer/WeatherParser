using System;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Represents the endpoints for MOSMIX_L forecast sources (contract MF-04).
/// </summary>
public sealed class MosmixLForecastEndpoints
{
    /// <summary>
    /// Gets or sets the URI of the single stations directory.
    /// The URI must be an absolute HTTP or HTTPS URI without user information,
    /// whose path ends with "/" and which has no query and no fragment (SC-12 directory rules).
    /// </summary>
    public Uri? SingleStationsDirectory { get; set; }
}
