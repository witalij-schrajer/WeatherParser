namespace WeatherParser.Parsing.Exceptions;

/// <summary>
/// Raised when a catalog, list or evidence file cannot be retrieved within the SC-09 bounds.
/// </summary>
public sealed class CatalogRetrievalException : Exception
{
    /// <summary>
    /// Error identifier for retrieval failures.
    /// </summary>
    public const string RetrievalFailedErrorId = "CATALOG-RETRIEVAL-FAILED";

    /// <summary>
    /// Error identifier for timeouts.
    /// </summary>
    public const string TimeoutErrorId = "CATALOG-TIMEOUT";

    /// <summary>
    /// Error identifier for files too large.
    /// </summary>
    public const string TooLargeErrorId = "CATALOG-TOO-LARGE";

    /// <summary>
    /// Gets the error identifier for this exception.
    /// </summary>
    public string ErrorId { get; }

    /// <summary>
    /// Gets the endpoint name.
    /// </summary>
    public string EndpointName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogRetrievalException"/> class.
    /// </summary>
    /// <param name="errorId">The error identifier.</param>
    /// <param name="endpointName">The endpoint name.</param>
    /// <param name="message">The message that describes the error.</param>
    public CatalogRetrievalException(string errorId, string endpointName, string message) : base(message)
    {
        if (errorId != RetrievalFailedErrorId &&
            errorId != TimeoutErrorId &&
            errorId != TooLargeErrorId)
        {
            throw new ArgumentException("Unknown error ID.", nameof(errorId));
        }

        if (endpointName is null)
        {
            throw new ArgumentNullException(nameof(endpointName));
        }

        ErrorId = errorId;
        EndpointName = endpointName;
    }
}
