using System;

namespace WeatherParser.Parsing.Evidence.MosmixS;

/// <summary>
/// Represents the MOSMIX catalog endpoint options.
/// </summary>
public sealed class MosmixCatalogEndpoints
{
    /// <summary>
    /// Gets or sets the MOSMIX station catalog URI.
    /// </summary>
    public Uri? CatalogUri { get; set; }

    /// <summary>
    /// Gets or sets the MOSMIX_L single stations directory URI.
    /// </summary>
    public Uri? MosmixLSingleStationsDirectory { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of evidence candidates.
    /// </summary>
    public int MaxEvidenceCandidates { get; set; } = 100;

    /// <summary>
    /// Gets or sets the selection deadline.
    /// </summary>
    public TimeSpan SelectionDeadline { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Gets or sets the MOSMIX_S all stations URI.
    /// </summary>
    public Uri? MosmixSAllStationsUri { get; set; }

    /// <summary>
    /// Gets or sets the evidence cache directory path.
    /// </summary>
    public string? EvidenceCacheDirectory { get; set; }

    /// <summary>
    /// Gets or sets the maximum age of MOSMIX_S evidence.
    /// </summary>
    public TimeSpan? MosmixSEvidenceMaxAge { get; set; }

    /// <summary>
    /// Gets or sets the MOSMIX_S refresh deadline.
    /// </summary>
    public TimeSpan MosmixSRefreshDeadline { get; set; } = TimeSpan.FromMinutes(10);
}
