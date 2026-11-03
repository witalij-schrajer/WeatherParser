using System;
using System.Collections.Generic;
using System.Linq;

namespace WeatherParser.Cli.Diagnostics;

/// <summary>
/// Provides an exit code policy.
/// </summary>
public class ExitCodePolicy : IExitCodePolicy
{
    private static readonly HashSet<string> UsageErrors = new(StringComparer.Ordinal)
    {
        CliErrorIds.ArgumentInvalid, CliErrorIds.CommandMissing, CliErrorIds.CommandConflict, CliErrorIds.ConfigMissing,
        CliErrorIds.ConfigInvalid, CliErrorIds.CredentialsInvalid, CliErrorIds.DwdStationMissing, CliErrorIds.OsthessenPublicationNotPermitted,
        CliErrorIds.OutputExists, CliErrorIds.OutputFailed,
        "COORDINATE-INVALID", "SEARCH-PARAMETER-INVALID", "STATION-ID-INVALID", "MOSMIX-ID-INVALID", "OSTHESSEN-PATTERN-INVALID",
    };

    /// <inheritdoc/>
    public int ForError(string errorId) => errorId == CliErrorIds.Cancelled ? ExitCodes.Cancelled
        : UsageErrors.Contains(errorId) ? ExitCodes.Usage
        : ExitCodes.Failure;

    /// <inheritdoc/>
    public int ForOutcome(CommandOutcome outcome) => outcome.Cancelled ? ExitCodes.Cancelled
        : outcome.Failed == 0 ? ExitCodes.Success
        : outcome.Succeeded == 0 ? ExitCodes.Failure
        : ExitCodes.Partial;

    /// <inheritdoc />
    public int ForRuntimeError(string errorId) => errorId == CliErrorIds.Cancelled ? ExitCodes.Cancelled : ExitCodes.Failure;
}
