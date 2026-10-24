using System;

namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Represents the CDC catalog endpoint options.
/// </summary>
public sealed class CdcCatalogEndpoints
{
    /// <summary>
    /// Gets or sets the CDC air temperature station list URI.
    /// </summary>
    public Uri? AirTemperatureStationList { get; set; }

    /// <summary>
    /// Gets or sets the CDC air temperature now directory URI.
    /// </summary>
    public Uri? AirTemperatureNowDirectory { get; set; }

    /// <summary>
    /// Gets or sets the CDC wind station list URI.
    /// </summary>
    public Uri? WindStationList { get; set; }

    /// <summary>
    /// Gets or sets the CDC wind now directory URI.
    /// </summary>
    public Uri? WindNowDirectory { get; set; }

    /// <summary>
    /// Gets or sets the CDC precipitation station list URI.
    /// </summary>
    public Uri? PrecipitationStationList { get; set; }

    /// <summary>
    /// Gets or sets the CDC precipitation now directory URI.
    /// </summary>
    public Uri? PrecipitationNowDirectory { get; set; }

    /// <summary>
    /// Gets or sets the CDC solar station list URI.
    /// </summary>
    public Uri? SolarStationList { get; set; }

    /// <summary>
    /// Gets or sets the CDC solar now directory URI.
    /// </summary>
    public Uri? SolarNowDirectory { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of evidence candidates.
    /// </summary>
    public int MaxEvidenceCandidates { get; set; } = 100;

    /// <summary>
    /// Gets or sets the selection deadline.
    /// </summary>
    public TimeSpan SelectionDeadline { get; set; } = TimeSpan.FromSeconds(120);
}
