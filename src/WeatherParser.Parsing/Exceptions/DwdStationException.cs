using WeatherParser.Parsing.Catalogs;

namespace WeatherParser.Parsing.Exceptions;

/// <summary>
/// Raised when a DWD station ID is invalid, unknown or has conflicting station-list metadata (contract OC-01, OC-02, OC-10).
/// </summary>
public sealed class DwdStationException : Exception
{
    /// <summary>
    /// Error identifier for an invalid station ID.
    /// </summary>
    public const string StationIdInvalidErrorId = "STATION-ID-INVALID";

    /// <summary>
    /// Error identifier for an unknown station ID.
    /// </summary>
    public const string StationUnknownErrorId = "STATION-UNKNOWN";

    /// <summary>
    /// Error identifier for conflicting station-list metadata.
    /// </summary>
    public const string StationMetadataConflictErrorId = "STATION-METADATA-CONFLICT";

    /// <summary>
    /// Gets the error identifier for this exception.
    /// </summary>
    public string ErrorId { get; }

    /// <summary>
    /// Gets the station-list conflict diagnostic for STATION-METADATA-CONFLICT, which the caller logs; null for the other error IDs.
    /// </summary>
    public CatalogDiagnostic? Diagnostic { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DwdStationException"/> class.
    /// </summary>
    /// <param name="errorId">The error identifier.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="diagnostic">The diagnostic for metadata conflicts, or null.</param>
    public DwdStationException(string errorId, string message, CatalogDiagnostic? diagnostic = null) : base(message)
    {
        if (errorId != StationIdInvalidErrorId &&
            errorId != StationUnknownErrorId &&
            errorId != StationMetadataConflictErrorId)
        {
            throw new ArgumentException("Unknown error ID.", nameof(errorId));
        }

        ErrorId = errorId;
        Diagnostic = diagnostic;
    }
}
