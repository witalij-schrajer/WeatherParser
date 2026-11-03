using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Observations.Dwd;

/// <summary>
/// A quantity the mapper omits itself because no reading may reach the dataset builder (contract OC-06); the caller logs it as OBSERVATION-READING-OMITTED.
/// </summary>
/// <param name="Kind">The kind of quantity that was omitted.</param>
/// <param name="Reason">The reason for omission.</param>
public sealed record DwdMappingOmission(QuantityKind Kind, DwdMappingOmissionReason Reason);
