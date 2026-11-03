using WeatherParser.Parsing.Export;
namespace WeatherParser.Parsing.Observations;

/// <summary>
/// Represents the result of building an observation dataset.
/// </summary>
public sealed class ObservationBuildResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObservationBuildResult"/> class.
    /// </summary>
    /// <param name="dataset">The built dataset.</param>
    /// <param name="omittedReadings">The list of omitted readings.</param>
    public ObservationBuildResult(ObservationDataset dataset, IReadOnlyList<OmittedReading> omittedReadings)
    {
        Dataset = dataset;
        OmittedReadings = omittedReadings;
    }

    /// <summary>
    /// Gets the built dataset.
    /// </summary>
    public ObservationDataset Dataset { get; }

    /// <summary>
    /// Gets the list of omitted readings.
    /// </summary>
    public IReadOnlyList<OmittedReading> OmittedReadings { get; }
}
