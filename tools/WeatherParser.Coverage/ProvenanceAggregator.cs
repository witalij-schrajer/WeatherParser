using System;
using System.Collections.Generic;
using System.Linq;

namespace WeatherParser.Coverage;

/// <summary>
/// Aggregates block counters of production modules after applying exclusions.
/// </summary>
public sealed class ProvenanceAggregator : IProvenanceAggregator
{
    /// <inheritdoc/>
    public BlockCounters Aggregate(CoverageReport report, IReadOnlyList<string> exclusions)
    {
        if (report == null)
            throw new ArgumentNullException(nameof(report));
        if (exclusions == null)
            throw new ArgumentNullException(nameof(exclusions));

        var productionModules = new List<ModuleCoverage>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenIds = new HashSet<string>();

        foreach (var module in report.Modules)
        {
            // Skip excluded modules
            if (IsExcluded(module.Name, exclusions))
                continue;

            // Check for duplicate module names (case-insensitive)
            if (seenNames.Contains(module.Name))
            {
                throw new CoverageInputException($"Duplicate production module name '{module.Name}' found in coverage report.");
            }

            // Check for duplicate module IDs
            if (seenIds.Contains(module.ModuleId))
            {
                throw new CoverageInputException($"Duplicate production module ID '{module.ModuleId}' found in coverage report.");
            }

            seenNames.Add(module.Name);
            seenIds.Add(module.ModuleId);
            productionModules.Add(module);
        }

        if (productionModules.Count == 0)
        {
            throw new CoverageInputException("Coverage report contains no production modules after exclusions.");
        }

        var covered = 0L;
        var notCovered = 0L;

        foreach (var module in productionModules)
        {
            covered += module.Blocks.Covered;
            notCovered += module.Blocks.NotCovered;
        }

        return new BlockCounters(covered, notCovered);
    }

    private static bool IsExcluded(string name, IReadOnlyList<string> exclusions)
    {
        foreach (var exclusion in exclusions)
        {
            if (IsWildcardMatch(name, exclusion))
                return true;
        }
        return false;
    }

    private static bool IsWildcardMatch(string name, string pattern)
    {
        // Simple wildcard matching with * support
        var patternParts = pattern.Split('*');
        var index = 0;

        for (var i = 0; i < patternParts.Length; i++)
        {
            var part = patternParts[i];
            if (string.IsNullOrEmpty(part))
            {
                continue;
            }

            var foundIndex = name.IndexOf(part, index, StringComparison.OrdinalIgnoreCase);
            if (foundIndex < 0)
            {
                return false;
            }

            index = foundIndex + part.Length;
        }

        // Ensure the pattern matches the entire string
        if (pattern.EndsWith("*") || patternParts[patternParts.Length - 1] == "")
        {
            // Pattern ends with *, so it's okay if we don't match the full string
            return true;
        }

        // Check that we matched to the end of the name
        return index >= name.Length;
    }
}
