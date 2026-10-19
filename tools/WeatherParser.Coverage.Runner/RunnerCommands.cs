namespace WeatherParser.Coverage.Runner;

/// <summary>
/// Base record for all runner commands.
/// </summary>
public abstract record RunnerCommand;

/// <summary>
/// Command to evaluate coverage.
/// </summary>
/// <param name="ReportPath">Path to the native XML coverage report.</param>
/// <param name="BaselinePath">Path to the baseline metadata JSON, or null if no baseline exists.</param>
/// <param name="ChangedFilesPath">Path to the changed files list.</param>
/// <param name="ExpectedBaselineCommit">Expected commit ID for baseline comparability check.</param>
/// <param name="ToolName">Tool name used to generate the report.</param>
/// <param name="ToolVersion">Tool version used to generate the report.</param>
/// <param name="Exclusions">List of exclusion patterns for module filtering.</param>
/// <param name="Bootstrap">True if this is the initial bootstrap run with no baseline yet.</param>
public sealed record EvaluateCommand(
    string ReportPath,
    string? BaselinePath,
    string ChangedFilesPath,
    string? ExpectedBaselineCommit,
    string ToolName,
    string ToolVersion,
    IReadOnlyList<string> Exclusions,
    bool Bootstrap) : RunnerCommand;

/// <summary>
/// Command to write a baseline.
/// </summary>
/// <param name="ReportPath">Path to the native XML coverage report.</param>
/// <param name="CommitId">Git commit identifier for the baseline.</param>
/// <param name="ToolName">Tool name used to generate the report.</param>
/// <param name="ToolVersion">Tool version used to generate the report.</param>
/// <param name="Exclusions">List of exclusion patterns for module filtering.</param>
/// <param name="OutputPath">Path to write the baseline metadata JSON.</param>
public sealed record WriteBaselineCommand(
    string ReportPath,
    string CommitId,
    string ToolName,
    string ToolVersion,
    IReadOnlyList<string> Exclusions,
    string OutputPath) : RunnerCommand;

/// <summary>
/// Command representing a usage error.
/// </summary>
/// <param name="Message">The usage error message.</param>
public sealed record UsageError(string Message) : RunnerCommand;
