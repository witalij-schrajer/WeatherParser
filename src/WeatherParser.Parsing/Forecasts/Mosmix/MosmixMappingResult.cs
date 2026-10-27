namespace WeatherParser.Parsing.Forecasts.Mosmix;

using WeatherParser.Parsing.Forecasts;

/// <summary>
/// Represents the result of mapping a MOSMIX forecast document.
/// </summary>
public sealed class MosmixMappingResult
{
    /// <summary>
    /// Gets or sets the input for building a forecast run.
    /// </summary>
    public required ForecastRunInput Input { get; init; }

    /// <summary>
    /// Gets or sets the names of elements that are missing from the document but expected by the mapping table.
    /// </summary>
    public required IReadOnlyList<string> AbsentElements { get; init; }

    /// <summary>
    /// Gets or sets the names of document series that are not mapped to a forecast element.
    /// </summary>
    public required IReadOnlyList<string> NotExportedElements { get; init; }
}
