namespace WeatherParser.Coverage;

/// <summary>
/// All gate inputs. BaselinePath null or a non-existing file means "no baseline".
/// </summary>
/// <param name="ReportPath">Path to the native XML coverage report.</param>
/// <param name="BaselinePath">Path to the baseline metadata JSON, or null if no baseline exists.</param>
/// <param name="ChangedFilesPath">Path to the changed files list.</param>
/// <param name="ExpectedBaselineCommitId">Expected commit ID for baseline comparability check.</param>
/// <param name="ToolName">Tool name used to generate the report.</param>
/// <param name="ToolVersion">Tool version used to generate the report.</param>
/// <param name="Exclusions">List of exclusion patterns for module filtering.</param>
/// <param name="IsInitialBootstrap">True if this is the initial bootstrap run with no baseline yet.</param>
/// <param name="GatedInputs">Gated inputs of the gate (REQ-086 amendment); null or empty when every code change requires strict improvement.</param>
/// <param name="GateName">Gate name (<c>library</c> or <c>cli</c>) used to select reconciliation entries; null when no reconciliation applies.</param>
/// <param name="ReconciliationPath">Path to <c>coverage-reconciliation.json</c>; null or a missing file means no recorded reconciliation.</param>
/// <param name="HeadRepository">Head repository of the pull request as <c>owner/name</c>; without it no reconciliation applies.</param>
/// <param name="HeadBranch">Head branch of the pull request; without it no reconciliation applies.</param>
/// <param name="BaseRepository">Base repository of the pull request as <c>owner/name</c>; without it the release rule does not apply.</param>
/// <param name="BaseBranch">Base branch of the pull request; without it the release rule does not apply.</param>
public sealed record CoverageGateRequest(
    string ReportPath,
    string? BaselinePath,
    string ChangedFilesPath,
    string ExpectedBaselineCommitId,
    string ToolName,
    string ToolVersion,
    IReadOnlyList<string> Exclusions,
    bool IsInitialBootstrap,
    IReadOnlyList<string>? GatedInputs = null,
    string? GateName = null,
    string? ReconciliationPath = null,
    string? HeadRepository = null,
    string? HeadBranch = null,
    string? BaseRepository = null,
    string? BaseBranch = null);
