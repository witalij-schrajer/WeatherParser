using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Forecasts;

/// <summary>
/// Represents the result of building a forecast run.
/// </summary>
public sealed class ForecastRunBuildResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForecastRunBuildResult"/> class.
    /// </summary>
    /// <param name="run">The built forecast run.</param>
    /// <param name="omittedValues">The list of omitted values.</param>
    public ForecastRunBuildResult(ForecastRun run, IReadOnlyList<OmittedForecastValue> omittedValues)
    {
        Run = run;
        OmittedValues = omittedValues;
    }

    /// <summary>
    /// Gets the built forecast run.
    /// </summary>
    public ForecastRun Run { get; }

    /// <summary>
    /// Gets the list of omitted values.
    /// </summary>
    public IReadOnlyList<OmittedForecastValue> OmittedValues { get; }
}
