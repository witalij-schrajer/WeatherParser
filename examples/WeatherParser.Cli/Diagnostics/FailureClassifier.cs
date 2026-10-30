using System;
using WeatherParser.Cli.Arguments;

namespace WeatherParser.Cli.Diagnostics;

/// <summary>
/// Provides a failure classifier.
/// </summary>
public class FailureClassifier : IFailureClassifier
{
    private static readonly HashSet<string> OsthessenCommandLevel = new(StringComparer.Ordinal)
    {
        "OSTHESSEN-PAGE-UNAVAILABLE", "OSTHESSEN-PATTERN-NO-MATCH", "OSTHESSEN-FORMAT-CHANGED",
        "OSTHESSEN-STATION-ID-COLLISION", "OSTHESSEN-DEADLINE-EXCEEDED", "OSTHESSEN-OPTIONS-INVALID",
    };

    /// <inheritdoc/>
    public FailureKind Classify(CliCommand command, string errorId) => command switch
    {
        CliCommand.SelectDwd or CliCommand.SelectMosmix => FailureKind.CommandLevel,
        CliCommand.ParseOstHessen when OsthessenCommandLevel.Contains(errorId) => FailureKind.CommandLevel,
        _ => FailureKind.Item,
    };
}
