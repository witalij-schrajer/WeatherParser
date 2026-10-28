using System;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Represents the endpoints for MOSMIX_S forecast sources (contract MF-04).
/// </summary>
public sealed class MosmixSForecastEndpoints
{
    /// <summary>
    /// Gets or sets the URI of the all stations endpoint.
    /// The URI must be an absolute HTTP or HTTPS URI without user information,
    /// and may contain a query which is used unchanged.
    /// </summary>
    public Uri? AllStationsUri { get; set; }
}
