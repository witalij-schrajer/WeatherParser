namespace WeatherParser.Parsing.Observations;

using WeatherParser.Parsing.Export;

/// <summary>
/// Represents the input for building an observation dataset.
/// </summary>
public sealed class ObservationInput
{
    /// <summary>
    /// Gets or sets the provider of the observation data.
    /// </summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the location of the observation.
    /// </summary>
    public ObservationLocation? Location { get; init; }

    /// <summary>
    /// Gets or sets the UTC time when the data was retrieved.
    /// </summary>
    public DateTime RetrievedAtUtc { get; init; }

    /// <summary>
    /// Gets or sets the list of observation readings.
    /// </summary>
    public IReadOnlyList<ObservationReading> Readings { get; init; } = Array.Empty<ObservationReading>();
}
