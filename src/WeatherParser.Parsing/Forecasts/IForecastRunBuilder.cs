using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Forecasts;

/// <summary>
/// Builds a forecast run from input data.
/// </summary>
/// <remarks>
/// See contract C-75 for the build process.
/// </remarks>
public interface IForecastRunBuilder
{
    /// <summary>
    /// Builds a forecast run from the specified input.
    /// </summary>
    /// <param name="input">The input data to build the forecast run from.</param>
    /// <returns>The result of building the forecast run.</returns>
    /// <exception cref="ForecastRunRejectedException">
    /// Thrown when the input data is invalid or cannot be processed.
    /// </exception>
    ForecastRunBuildResult Build(ForecastRunInput input);
}
