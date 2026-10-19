namespace WeatherParser.Coverage;

/// <summary>
/// Baseline metadata JSON (camelCase properties: commitId, toolName, toolVersion, format, exclusions, blocksCovered, blocksNotCovered).
/// </summary>
/// <param name="CommitId">Git commit identifier.</param>
/// <param name="ToolName">Tool name that generated the baseline.</param>
/// <param name="ToolVersion">Tool version that generated the baseline.</param>
/// <param name="Format">Baseline format identifier.</param>
/// <param name="Exclusions">List of exclusion patterns.</param>
/// <param name="Blocks">Block counters for the baseline.</param>
public sealed record BaselineMetadata(
    string CommitId,
    string ToolName,
    string ToolVersion,
    string Format,
    IReadOnlyList<string> Exclusions,
    BlockCounters Blocks);
