namespace WeatherParser.Coverage;

/// <summary>
/// Checks if the baseline metadata is compatible with the current evaluation context.
/// </summary>
public interface IBaselineComparabilityChecker
{
    /// <summary>
    /// Checks if the provided baseline metadata is comparable to the expected values.
    /// </summary>
    /// <param name="baseline">The baseline metadata to check.</param>
    /// <param name="expectedCommitId">Expected commit ID for comparison.</param>
    /// <param name="toolName">Expected tool name for comparison.</param>
    /// <param name="toolVersion">Expected tool version for comparison.</param>
    /// <param name="format">Expected format for comparison.</param>
    /// <param name="exclusions">Expected exclusions for comparison.</param>
    /// <returns>A rule result indicating whether the baseline is comparable and any diagnostics if not.</returns>
    RuleResult Check(
        BaselineMetadata baseline,
        string expectedCommitId,
        string toolName,
        string toolVersion,
        string format,
        IReadOnlyList<string> exclusions);
}
