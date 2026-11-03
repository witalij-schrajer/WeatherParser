using System;
using System.Collections.Generic;
using System.Globalization;

namespace WeatherParser.Coverage;

/// <summary>
/// Evaluates coverage rules against current and baseline block counters.
/// </summary>
public sealed class RuleEvaluator : IRuleEvaluator
{
    /// <inheritdoc/>
    public IReadOnlyList<RuleResult> Evaluate(
        BlockCounters current,
        BlockCounters? baseline,
        PullRequestCategory category,
        bool isInitialBootstrap)
    {
        var results = new List<RuleResult>();

        // 1. "Measurement": current.Total == 0 or negative counters -> fail
        if (current.Covered < 0 || current.NotCovered < 0 || (Int128)current.Covered + current.NotCovered > long.MaxValue || current.Total == 0)
        {
            results.Add(new RuleResult("Measurement", false, $"Block counters {current.Covered} covered and {current.NotCovered} not covered are invalid (zero denominator, negative counter or total out of range) and cannot pass."));
            return results;
        }

        results.Add(new RuleResult("Measurement", true, $"Block counters {current.Covered}/{current.Total} are valid."));

        // 2. "Minimum": passes iff 5*Covered >= 4*Total exactly (80/100 passes, 79/100 fails)
        var minimumPass = 5 * (Int128)current.Covered >= 4 * (Int128)current.Total;
        var minPercent = string.Format(CultureInfo.InvariantCulture, "{0:F2}", current.Covered * 100.0 / current.Total);
        results.Add(new RuleResult("Minimum", minimumPass, $"Block coverage {current.Covered}/{current.Total} ({minPercent}%) is {(minimumPass ? "at or above" : "below")} the required minimum of 80%."));

        // 3. "Baseline": baseline null and isInitialBootstrap -> pass; baseline null and not bootstrap -> fail
        if (baseline == null)
        {
            if (isInitialBootstrap)
            {
                results.Add(new RuleResult("Baseline", true, "Initial bootstrap: no baseline, comparison skipped"));
            }
            else
            {
                results.Add(new RuleResult("Baseline", false, "Baseline is missing after initial bootstrap; a missing baseline is never treated as bootstrap."));
            }

            // No Comparison rule in either case
            return results;
        }

        // Baseline with negative counters or Total == 0 -> failed "Baseline" rule (no Comparison)
        if (baseline.Covered < 0 || baseline.NotCovered < 0 || (Int128)baseline.Covered + baseline.NotCovered > long.MaxValue || baseline.Total == 0)
        {
            results.Add(new RuleResult("Baseline", false, $"Baseline block counters {baseline.Covered} covered and {baseline.NotCovered} not covered are invalid (zero denominator, negative counter or total out of range) and cannot pass."));
            return results;
        }

        results.Add(new RuleResult("Baseline", true, $"Baseline block counters {baseline.Covered}/{baseline.Total} are valid."));

        // 4. "Comparison" (only when baseline present and valid); unrounded ratios compared by Int128 cross-multiplication
        var currentScaled = (Int128)current.Covered * baseline.Total;
        var baselineScaled = (Int128)baseline.Covered * current.Total;
        var baselinePercent = string.Format(CultureInfo.InvariantCulture, "{0:F2}", baseline.Covered * 100.0 / baseline.Total);
        var currentPercent = string.Format(CultureInfo.InvariantCulture, "{0:F2}", current.Covered * 100.0 / current.Total);

        if (baseline.NotCovered == 0)
        {
            // Exact 100% baseline
            if (current.NotCovered == 0)
            {
                results.Add(new RuleResult("Comparison", true, $"Block coverage {current.Covered}/{current.Total} ({currentPercent}%) equals baseline {baseline.Covered}/{baseline.Total} ({baselinePercent}%)."));
            }
            else
            {
                results.Add(new RuleResult("Comparison", false, $"Baseline is exactly 100% ({baseline.Covered}/{baseline.Total}); the PR must also be exactly 100% but has {current.NotCovered} uncovered block(s) ({current.Covered}/{current.Total})."));
            }
        }
        else if (category == PullRequestCategory.DocumentationOnly)
        {
            // Documentation-only PR
            if (currentScaled >= baselineScaled)
            {
                results.Add(new RuleResult("Comparison", true, $"Block coverage {current.Covered}/{current.Total} ({currentPercent}%) is at or above baseline {baseline.Covered}/{baseline.Total} ({baselinePercent}%)."));
            }
            else
            {
                results.Add(new RuleResult("Comparison", false, $"Block coverage {current.Covered}/{current.Total} ({currentPercent}%) is below baseline {baseline.Covered}/{baseline.Total} ({baselinePercent}%)."));
            }
        }
        else if (category == PullRequestCategory.GatedInputsUnchanged)
        {
            // Code PR that leaves every gated input unchanged (REQ-086 amendment)
            if (currentScaled >= baselineScaled)
            {
                results.Add(new RuleResult("Comparison", true, $"Block coverage {current.Covered}/{current.Total} ({currentPercent}%) is at or above baseline {baseline.Covered}/{baseline.Total} ({baselinePercent}%); the gated inputs are unchanged, so equal coverage is accepted."));
            }
            else
            {
                results.Add(new RuleResult("Comparison", false, $"Block coverage {current.Covered}/{current.Total} ({currentPercent}%) is below baseline {baseline.Covered}/{baseline.Total} ({baselinePercent}%)."));
            }
        }
        else if (category == PullRequestCategory.ReleasePullRequest)
        {
            // Release pull request from develop into master (REQ-086 amendment 3)
            if (currentScaled >= baselineScaled)
            {
                results.Add(new RuleResult("Comparison", true, $"Block coverage {current.Covered}/{current.Total} ({currentPercent}%) is at or above baseline {baseline.Covered}/{baseline.Total} ({baselinePercent}%); the release rule applies, so equal coverage is accepted."));
            }
            else
            {
                results.Add(new RuleResult("Comparison", false, $"Block coverage {current.Covered}/{current.Total} ({currentPercent}%) is below baseline {baseline.Covered}/{baseline.Total} ({baselinePercent}%); the release rule never accepts a decrease."));
            }
        }
        else if (category == PullRequestCategory.Reconciled)
        {
            // Code PR covered by a user-approved, recorded baseline reconciliation (REQ-086 amendment 2)
            if (currentScaled >= baselineScaled)
            {
                results.Add(new RuleResult("Comparison", true, $"Block coverage {current.Covered}/{current.Total} ({currentPercent}%) is at or above baseline {baseline.Covered}/{baseline.Total} ({baselinePercent}%); a recorded baseline reconciliation applies, so equal coverage is accepted."));
            }
            else
            {
                results.Add(new RuleResult("Comparison", false, $"Block coverage {current.Covered}/{current.Total} ({currentPercent}%) is below baseline {baseline.Covered}/{baseline.Total} ({baselinePercent}%); a recorded baseline reconciliation never accepts a decrease."));
            }
        }
        else
        {
            // Code PR
            if (currentScaled > baselineScaled)
            {
                results.Add(new RuleResult("Comparison", true, $"Block coverage {current.Covered}/{current.Total} ({currentPercent}%) exceeds baseline {baseline.Covered}/{baseline.Total} ({baselinePercent}%)."));
            }
            else if (currentScaled == baselineScaled)
            {
                results.Add(new RuleResult("Comparison", false, $"Block coverage {current.Covered}/{current.Total} ({currentPercent}%) is equal to baseline {baseline.Covered}/{baseline.Total} ({baselinePercent}%); a code PR must strictly increase block coverage below 100%."));
            }
            else
            {
                results.Add(new RuleResult("Comparison", false, $"Block coverage {current.Covered}/{current.Total} ({currentPercent}%) is lower than baseline {baseline.Covered}/{baseline.Total} ({baselinePercent}%)."));
            }
        }

        return results;
    }
}
