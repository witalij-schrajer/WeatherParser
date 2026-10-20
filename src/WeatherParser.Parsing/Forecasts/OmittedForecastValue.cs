using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Forecasts;

/// <summary>
/// Represents a forecast value that was omitted during the build process.
/// </summary>
public sealed record OmittedForecastValue(DateTime ValidAtUtc, QuantityKind Kind, OmissionReason Reason);
