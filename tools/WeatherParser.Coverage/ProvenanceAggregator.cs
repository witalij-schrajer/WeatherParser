using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

        try
        {
            foreach (var module in productionModules)
            {
                covered = checked(covered + module.Blocks.Covered);
                notCovered = checked(notCovered + module.Blocks.NotCovered);
                _ = checked(covered + notCovered);
            }
        }
        catch (OverflowException ex)
        {
            throw new CoverageInputException("Aggregated block counters exceed the supported range.", ex);
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
        var regex = "^" + Regex.Escape(pattern).Replace(@"\*", ".*") + "$";
        return Regex.IsMatch(name, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
