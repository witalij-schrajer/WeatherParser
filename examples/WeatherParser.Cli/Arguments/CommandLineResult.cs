namespace WeatherParser.Cli.Arguments;

/// <summary>
/// Represents the result of parsing command-line arguments.
/// </summary>
public abstract record CommandLineResult;

/// <summary>
/// Represents a usage error encountered during command-line parsing.
/// </summary>
/// <param name="ErrorId">The identifier of the error.</param>
/// <param name="Message">The message describing the error.</param>
public sealed record UsageError(string ErrorId, string Message) : CommandLineResult;

/// <summary>
/// Represents a help request encountered during command-line parsing.
/// </summary>
/// <param name="Command">The command for which help is requested, or null for general help.</param>
public sealed record HelpRequest(CliCommand? Command) : CommandLineResult;

/// <summary>
/// Represents a successfully parsed command-line.
/// </summary>
/// <param name="Command">The command that was parsed.</param>
/// <param name="Values">The values of options, keyed by their canonical names.</param>
/// <param name="Flags">The set of flags that were specified.</param>
public sealed record ParsedCommandLine(CliCommand Command, IReadOnlyDictionary<string, string> Values, IReadOnlySet<string> Flags) : CommandLineResult;
