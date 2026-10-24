namespace WeatherParser.Parsing.Exceptions;

/// <summary>
/// Raised when a selection limit of SC-18 is invalid or exceeded.
/// </summary>
public sealed class SelectionLimitException : Exception
{
    /// <summary>
    /// Error identifier for too many candidates.
    /// </summary>
    public const string TooManyCandidatesErrorId = "SELECTION-TOO-MANY-CANDIDATES";

    /// <summary>
    /// Error identifier for deadline exceeded.
    /// </summary>
    public const string DeadlineExceededErrorId = "SELECTION-DEADLINE-EXCEEDED";

    /// <summary>
    /// Error identifier for invalid limit.
    /// </summary>
    public const string LimitInvalidErrorId = "SELECTION-LIMIT-INVALID";

    /// <summary>
    /// Gets the error identifier for this exception.
    /// </summary>
    public string ErrorId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectionLimitException"/> class.
    /// </summary>
    /// <param name="errorId">The error identifier.</param>
    /// <param name="message">The message that describes the error.</param>
    public SelectionLimitException(string errorId, string message) : base(message)
    {
        if (errorId != TooManyCandidatesErrorId &&
            errorId != DeadlineExceededErrorId &&
            errorId != LimitInvalidErrorId)
        {
            throw new ArgumentException("Unknown error ID.", nameof(errorId));
        }

        ErrorId = errorId;
    }
}
