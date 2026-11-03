namespace WeatherParser.Coverage;

/// <summary>
/// Checks if the baseline metadata is compatible with the current evaluation context.
/// </summary>
public sealed class BaselineComparabilityChecker : IBaselineComparabilityChecker
{
    /// <inheritdoc/>
    public RuleResult Check(
        BaselineMetadata baseline,
        string expectedCommitId,
        string toolName,
        string toolVersion,
        string format,
        IReadOnlyList<string> exclusions)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(exclusions);

        var diagnostics = new List<string>();

        if (baseline.CommitId != expectedCommitId)
        {
            diagnostics.Add($"Baseline commit ID '{baseline.CommitId}' differs from expected '{expectedCommitId}'.");
        }

        if (!string.Equals(baseline.ToolName, toolName, StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add($"Baseline tool name '{baseline.ToolName}' differs from expected '{toolName}'.");
        }

        if (!string.Equals(baseline.ToolVersion, toolVersion, StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add($"Baseline tool version '{baseline.ToolVersion}' differs from expected '{toolVersion}'.");
        }

        if (baseline.Format != format)
        {
            diagnostics.Add($"Baseline format '{baseline.Format}' differs from expected '{format}'.");
        }

        // Compare exclusions as sets (order irrelevant, case insensitive)
        var baselineExclusions = new HashSet<string>(baseline.Exclusions, StringComparer.OrdinalIgnoreCase);
        var expectedExclusions = new HashSet<string>(exclusions, StringComparer.OrdinalIgnoreCase);

        if (!baselineExclusions.SetEquals(expectedExclusions))
        {
            diagnostics.Add("Baseline exclusions differ from expected exclusions.");
        }

        if (diagnostics.Count == 0)
        {
            return new RuleResult("Comparability", true, "Baseline is comparable.");
        }

        return new RuleResult("Comparability", false, string.Join(" ", diagnostics));
    }
}
