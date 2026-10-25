namespace WeatherParser.Parsing.Observations.Dwd;

/// <summary>
/// Consumer-registered options of the DWD observation parser (contract OC-09); the station lists and now directories come from CdcCatalogEndpoints.
/// </summary>
public sealed class DwdObservationOptions
{
    /// <summary>
    /// The overall deadline of one parse, greater than zero and at most one hour; default 120 seconds.
    /// </summary>
    public TimeSpan ObservationDeadline { get; set; } = TimeSpan.FromSeconds(120);
}
