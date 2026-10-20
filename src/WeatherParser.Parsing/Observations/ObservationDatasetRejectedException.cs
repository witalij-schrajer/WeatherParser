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
    }
}
