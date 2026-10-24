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
    Refresh
}
