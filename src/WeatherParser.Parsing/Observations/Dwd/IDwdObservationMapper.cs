using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Observations.Dwd;

/// <summary>
/// Maps the rows of one station's CDC now archives to observation readings (contract OC-04 to OC-07).
/// </summary>
public interface IDwdObservationMapper
{
    /// <summary>
    /// Returns one reading per mapped quantity from the newest usable row of its column, and the omissions the builder cannot see.
    /// </summary>
    /// <param name="evidence">The evidence to map.</param>
    /// <exception cref="ArgumentNullException">
    /// When evidence is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// When a product code is unknown or given twice.
    /// </exception>
    DwdMappingResult Map(IReadOnlyList<CdcNowEvidence> evidence);
}
