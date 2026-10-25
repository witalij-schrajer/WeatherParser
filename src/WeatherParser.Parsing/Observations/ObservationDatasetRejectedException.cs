namespace WeatherParser.Parsing.Observations;

/// <summary>
/// Represents an exception thrown when an observation dataset cannot be built.
/// </summary>
public sealed class ObservationDatasetRejectedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObservationDatasetRejectedException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ObservationDatasetRejectedException(string message) : base(message)
    {
        OmittedReadings = Array.Empty<OmittedReading>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservationDatasetRejectedException"/> class with the readings the builder omitted (contract C-60, OC-10).
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="omittedReadings">The omitted readings with their reasons.</param>
    public ObservationDatasetRejectedException(string message, IReadOnlyList<OmittedReading> omittedReadings) : base(message)
    {
        OmittedReadings = omittedReadings ?? throw new ArgumentNullException(nameof(omittedReadings));
    }

    /// <summary>
    /// Gets the readings the builder omitted with their reasons, so that the caller can log every omission before the rejection propagates (contract OC-10); empty when not supplied.
    /// </summary>
    public IReadOnlyList<OmittedReading> OmittedReadings { get; }
}
