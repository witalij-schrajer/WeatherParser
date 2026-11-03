namespace WeatherParser.Parsing.Exceptions;

/// <summary>
/// Raised when a station list, catalog or evidence file violates its format contract (SC-10, SC-17, SC-19, SC-20).
/// Messages never contain input values.
/// </summary>
public sealed class CatalogFormatException : Exception
{
    /// <summary>
    /// Error identifier for invalid catalog formats.
    /// </summary>
    public const string CatalogFormatInvalidErrorId = "CATALOG-FORMAT-INVALID";

    /// <summary>
    /// Error identifier for invalid CDC evidence.
    /// </summary>
    public const string CdcEvidenceInvalidErrorId = "CDC-EVIDENCE-INVALID";

    /// <summary>
    /// Error identifier for invalid MOSMIX evidence.
    /// </summary>
    public const string MosmixEvidenceInvalidErrorId = "MOSMIX-EVIDENCE-INVALID";

    /// <summary>
    /// Gets the error identifier for this exception.
    /// </summary>
    public string ErrorId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogFormatException"/> class.
    /// </summary>
    /// <param name="errorId">The error identifier.</param>
    /// <param name="message">The message that describes the error.</param>
    public CatalogFormatException(string errorId, string message) : base(message)
    {
        if (errorId != CatalogFormatInvalidErrorId &&
            errorId != CdcEvidenceInvalidErrorId &&
            errorId != MosmixEvidenceInvalidErrorId)
        {
            throw new ArgumentException("Unknown error ID.", nameof(errorId));
        }

        ErrorId = errorId;
    }
}
