namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// The MOSMIX parse pipeline.
/// </summary>
public interface IMosmixParsePipeline
{
    /// <summary>
    /// Parses the forecast data.
    /// </summary>
    /// <param name="request">The parse request.</param>
    /// <param name="mosmixId">The MOSMIX ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parse result.</returns>
    Task<MosmixParseResult> ParseAsync(MosmixParseRequest request, string mosmixId, CancellationToken cancellationToken);
}
