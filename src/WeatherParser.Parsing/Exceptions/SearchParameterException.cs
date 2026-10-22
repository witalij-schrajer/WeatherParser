namespace WeatherParser.Parsing.Exceptions;

/// <summary>
/// Raised when search parameters violate the validation policy (contract SC-11, SC-17).
/// </summary>
public sealed class SearchParameterException : Exception
{
    /// <summary>
    /// Error identifier for invalid search parameters.
    /// </summary>
    public const string SearchParameterInvalidErrorId = "SEARCH-PARAMETER-INVALID";

    /// <summary>
    /// Gets the error identifier for this exception.
    /// </summary>
    public string ErrorId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchParameterException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SearchParameterException(string message) : base(message)
    {
        ErrorId = SearchParameterInvalidErrorId;
    }
}
