namespace WeatherParser.Cli.Commands;

using WeatherParser.Cli.Arguments;

/// <summary>
/// The output settings and resolved values of one invocation that a command handler needs (CL-09, CL-12).
/// </summary>
/// <param name="Options">The common options of the command line.</param>
/// <param name="StationId">The resolved DWD station ID for --ParseDWD; otherwise null.</param>
/// <param name="StandardOutput">The standard output writer for JSON and XML documents.</param>
public sealed record CommandContext(CommonOptions Options, string? StationId, TextWriter StandardOutput);
