using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Forecasts.Mosmix;

/// <summary>
/// The per-product configuration of one parser, built once by the public parser and passed with every parse (no behavior switch: the product is a label and the exception Product only).
/// </summary>
public sealed class MosmixParseRequest
{
    /// <summary>
    /// Gets or sets the product.
    /// </summary>
    public required MosmixProduct Product { get; init; }

    /// <summary>
    /// Gets or sets the parse deadline.
    /// </summary>
    public required TimeSpan ParseDeadline { get; init; }

    /// <summary>
    /// Gets or sets the maximum run age.
    /// </summary>
    public required TimeSpan MaxRunAge { get; init; }

    /// <summary>
    /// Gets or sets the sanitized endpoint.
    /// </summary>
    public required string SanitizedEndpoint { get; init; }

    /// <summary>
    /// Gets or sets the retrieval function.
    /// </summary>
    /// <param name="mosmixId">The MOSMIX ID.</param>
    /// <param name="read">The read function.</param>
    /// <param name="deadline">The deadline cancellation token.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The retrieved document.</returns>
    public required Func<string, Func<Stream, CancellationToken, Task<MosmixForecastDocument>>, CancellationToken, CancellationToken, Task<MosmixForecastDocument>> Retrieve { get; init; }

    /// <summary>
    /// Gets or sets the read function.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="mosmixId">The MOSMIX ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed document.</returns>
    public required Func<Stream, string, CancellationToken, Task<MosmixForecastDocument>> Read { get; init; }

    /// <summary>
    /// Gets or sets the logger.
    /// </summary>
    public required ILogger Logger { get; init; }
}
