namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Represents one GET request of the retrieval boundary.
/// </summary>
public sealed record CatalogRequest(
    string EndpointName,
    Uri Uri,
    long MaxBytes,
    bool NotFoundIsAbsent,
    CancellationToken DeadlineToken,
    CatalogDeadlineKind DeadlineKind)
{
    /// <summary>
    /// The maximum bytes for a list or catalog file, SC-09.
    /// </summary>
    public const long ListOrCatalogMaxBytes = 8388608;

    /// <summary>
    /// The maximum bytes for a per-station file, SC-09.
    /// </summary>
    public const long PerStationFileMaxBytes = 1048576;

    /// <summary>
    /// The maximum bytes for the MOSMIX-S all stations file, SC-09.
    /// </summary>
    public const long MosmixSAllStationsMaxBytes = 67108864;

    /// <summary>The bound of this request including the reader callback; null means the default of 30 seconds (contracts SC-09, MF-05).</summary>
    public TimeSpan? RequestTimeout { get; init; }
}
