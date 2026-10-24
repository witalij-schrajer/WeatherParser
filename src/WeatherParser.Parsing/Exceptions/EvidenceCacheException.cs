namespace WeatherParser.Parsing.Exceptions;

/// <summary>
/// Raised for the MOSMIX_S evidence cache settings, refresh and index failures of SC-22.
/// </summary>
public sealed class EvidenceCacheException : Exception
{
    /// <summary>
    /// Error identifier for invalid settings.
    /// </summary>
    public const string SettingsInvalidErrorId = "EVIDENCE-CACHE-SETTINGS-INVALID";

    /// <summary>
    /// Error identifier for cache failure.
    /// </summary>
    public const string CacheFailedErrorId = "EVIDENCE-CACHE-FAILED";

    /// <summary>
    /// Error identifier for refresh deadline exceeded.
    /// </summary>
    public const string RefreshDeadlineExceededErrorId = "MOSMIX-S-REFRESH-DEADLINE-EXCEEDED";

    /// <summary>
    /// Error identifier for missing evidence.
    /// </summary>
    public const string EvidenceMissingErrorId = "MOSMIX-S-EVIDENCE-MISSING";

    /// <summary>
    /// Gets the error identifier for this exception.
    /// </summary>
    public string ErrorId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvidenceCacheException"/> class.
    /// </summary>
    /// <param name="errorId">The error identifier.</param>
    /// <param name="message">The message that describes the error.</param>
    public EvidenceCacheException(string errorId, string message) : base(message)
    {
        if (errorId != SettingsInvalidErrorId &&
            errorId != CacheFailedErrorId &&
            errorId != RefreshDeadlineExceededErrorId &&
            errorId != EvidenceMissingErrorId)
        {
            throw new ArgumentException("Unknown error ID.", nameof(errorId));
        }

        ErrorId = errorId;
    }
}
