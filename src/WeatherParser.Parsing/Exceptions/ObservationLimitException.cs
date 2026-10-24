namespace WeatherParser.Parsing.Exceptions;

/// <summary>
/// Raised when a DWD observation limit of contract OC-09 is invalid or exceeded.
/// </summary>
public sealed class ObservationLimitException : Exception
{
    /// <summary>
    /// Error identifier for deadline exceeded.
    /// </summary>
    public const string DeadlineExceededErrorId = "OBSERVATION-DEADLINE-EXCEEDED";

    /// <summary>
    /// Error identifier for invalid limit.
    /// </summary>
    public const string LimitInvalidErrorId = "OBSERVATION-LIMIT-INVALID";

    /// <summary>
    /// Gets the error identifier for this exception.
    /// </summary>
    public string ErrorId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservationLimitException"/> class.
    /// </summary>
    /// <param name="errorId">The error identifier.</param>
    /// <param name="message">The message that describes the error.</param>
    public ObservationLimitException(string errorId, string message) : base(message)
    {
        if (errorId != DeadlineExceededErrorId &&
            errorId != LimitInvalidErrorId)
        {
            throw new ArgumentException("Unknown error ID.", nameof(errorId));
        }

        ErrorId = errorId;
    }
}
