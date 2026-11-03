using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Observations.Dwd;

/// <summary>
/// The reason why a DWD observation reading was omitted.
/// </summary>
public enum DwdMappingOmissionReason
{
    /// <summary>
    /// The newest non-missing direction row has a mean wind speed of 0 in the same row (contract OC-06).
    /// </summary>
    CalmWind,

    /// <summary>
    /// The newest non-missing direction row has a direction of 0, which the DWD data never use for north (contract OC-06).
    /// </summary>
    ZeroDirection
}
