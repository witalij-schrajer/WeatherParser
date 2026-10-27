using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// Maps a MOSMIX forecast document to a forecast run input.
/// </summary>
public interface IMosmixElementMapper
{
    /// <summary>
    /// Maps the specified document to a forecast run input.
    /// </summary>
    /// <param name="document">The document to map.</param>
    /// <param name="product">The product of the forecast run.</param>
    /// <param name="retrievedAtUtc">The UTC time when the data was retrieved.</param>
    /// <param name="runAgeExceeded">The freshness marker of the run.</param>
    /// <returns>The mapping result.</returns>
    MosmixMappingResult Map(MosmixForecastDocument document, MosmixProduct product, DateTime retrievedAtUtc, bool? runAgeExceeded);
}
