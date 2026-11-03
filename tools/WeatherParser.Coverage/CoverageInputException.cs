namespace WeatherParser.Coverage;

/// <summary>
/// Thrown by readers/aggregator for missing, unreadable or invalid inputs; message is an actionable English diagnostic.
/// </summary>
public sealed class CoverageInputException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageInputException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public CoverageInputException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CoverageInputException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public CoverageInputException(string message, Exception inner) : base(message, inner)
    {
    }
}
