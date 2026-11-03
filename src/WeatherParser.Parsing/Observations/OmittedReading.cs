using WeatherParser.Parsing.Quantities;
namespace WeatherParser.Parsing.Observations;

/// <summary>
/// Represents a reading that was omitted from the dataset.
/// </summary>
/// <param name="Kind">The kind of the omitted reading.</param>
/// <param name="Reason">The reason why the reading was omitted.</param>
public sealed record OmittedReading(QuantityKind Kind, OmissionReason Reason);
