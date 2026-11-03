namespace WeatherParser.Parsing.Observations;

/// <summary>
/// Builds an observation dataset from input data.
/// </summary>
/// <remarks>
/// Validates, converts, rounds, omits with diagnostics, and rejects a dataset without usable readings.
/// </remarks>
public interface IObservationDatasetBuilder
{
    /// <summary>
    /// Builds an observation dataset from the provided input.
    /// </summary>
    /// <param name="input">The input data to build the dataset from.</param>
    /// <returns>The result of building the dataset, including any omitted readings.</returns>
    ObservationBuildResult Build(ObservationInput input);
}
