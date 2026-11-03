namespace WeatherParser.Parsing.Catalogs;

/// <summary>
/// Specifies the kind of deadline for a catalog request.
/// </summary>
public enum CatalogDeadlineKind
{
    /// <summary>
    /// Selection deadline SC-18.
    /// </summary>
    Selection,

    /// <summary>
    /// Refresh deadline SC-22.
    /// </summary>
    Refresh,

    /// <summary>
    /// The DWD observation deadline (contract OC-09); its expiry raises OBSERVATION-DEADLINE-EXCEEDED.
    /// </summary>
    Observation,

    /// <summary>
    /// The OsthessenWetter page deadline (contract OW-10); its expiry raises OSTHESSEN-DEADLINE-EXCEEDED.
    /// </summary>
    OsthessenWetter,

    /// <summary>
    /// The parse deadline of the MOSMIX_L forecast parser (contract MF-05).
    /// </summary>
    MosmixL,

    /// <summary>
    /// The parse deadline of the MOSMIX_S forecast parser (contract MF-05).
    /// </summary>
    MosmixS
}
