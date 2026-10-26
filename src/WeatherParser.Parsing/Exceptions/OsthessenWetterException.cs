using System;
using System.Collections.Generic;
using System.Linq;
using WeatherParser.Parsing.Observations.Osthessen;

namespace WeatherParser.Parsing.Exceptions;

/// <summary>
/// Raised when an OsthessenWetter parse fails with a defined error (contract OW-13).
/// </summary>
public sealed class OsthessenWetterException : Exception
{
    /// <summary>
    /// Error identifier for an invalid station name pattern (contract OW-09).
    /// </summary>
    public const string PatternInvalidErrorId = "OSTHESSEN-PATTERN-INVALID";

    /// <summary>
    /// Error identifier for a pattern that matches no station (contract OW-08).
    /// </summary>
    public const string PatternNoMatchErrorId = "OSTHESSEN-PATTERN-NO-MATCH";

    /// <summary>
    /// Error identifier for a page that cannot be retrieved (contract OW-10).
    /// </summary>
    public const string PageUnavailableErrorId = "OSTHESSEN-PAGE-UNAVAILABLE";

    /// <summary>
    /// Error identifier for an expired page deadline (contract OW-10).
    /// </summary>
    public const string DeadlineExceededErrorId = "OSTHESSEN-DEADLINE-EXCEEDED";

    /// <summary>
    /// Error identifier for a page that deviates from the supported revision (contract OW-01, OW-11).
    /// </summary>
    public const string FormatChangedErrorId = "OSTHESSEN-FORMAT-CHANGED";

    /// <summary>
    /// Error identifier for two station names with the same station ID (contract OW-03).
    /// </summary>
    public const string StationIdCollisionErrorId = "OSTHESSEN-STATION-ID-COLLISION";

    /// <summary>
    /// Error identifier for matched stations of which none yields a dataset (contract OW-08).
    /// </summary>
    public const string NoUsableStationErrorId = "OSTHESSEN-NO-USABLE-STATION";

    /// <summary>
    /// Error identifier for invalid consumer endpoints or options (contract OW-04, OW-10).
    /// </summary>
    public const string OptionsInvalidErrorId = "OSTHESSEN-OPTIONS-INVALID";

    /// <summary>
    /// Gets the error identifier for this exception.
    /// </summary>
    public string ErrorId { get; }

    /// <summary>
    /// Gets the station failures of the parse; empty unless the error is OSTHESSEN-NO-USABLE-STATION (contract OW-08).
    /// </summary>
    public IReadOnlyList<OsthessenWetterStationFailure> StationFailures { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OsthessenWetterException"/> class.
    /// </summary>
    /// <param name="errorId">The error identifier.</param>
    /// <param name="message">The message that describes the error.</param>
    public OsthessenWetterException(string errorId, string message) : base(message)
    {
        if (errorId != PatternInvalidErrorId &&
            errorId != PatternNoMatchErrorId &&
            errorId != PageUnavailableErrorId &&
            errorId != DeadlineExceededErrorId &&
            errorId != FormatChangedErrorId &&
            errorId != StationIdCollisionErrorId &&
            errorId != NoUsableStationErrorId &&
            errorId != OptionsInvalidErrorId)
        {
            throw new ArgumentException("Unknown error ID.", nameof(errorId));
        }

        ErrorId = errorId;
        StationFailures = Array.Empty<OsthessenWetterStationFailure>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OsthessenWetterException"/> class.
    /// </summary>
    /// <param name="errorId">The error identifier.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="stationFailures">The station failures; copied.</param>
    /// <exception cref="ArgumentNullException">When stationFailures is null.</exception>
    public OsthessenWetterException(string errorId, string message, IReadOnlyList<OsthessenWetterStationFailure> stationFailures) : this(errorId, message)
    {
        ArgumentNullException.ThrowIfNull(stationFailures);
        StationFailures = stationFailures.ToArray();
    }
}
