# P1.08 Implementation Proposal: Test-Driven Coverage Evaluation and Exceptions

**Status:** English translation of the existing German-only qwen proposal. This is a draft, not an accepted implementation or evidence of completed tests. The binding P1 detailed plan and repository rules take precedence. Proposed namespaces, paths, signatures and test assignments below are preserved for review; they are not newly approved contracts. The exception-list syntax is a signature sketch, not compilable C#.

## 1. CoverageEvaluator

### Namespace and file path

- **Namespace:** `WeatherParser.Parsing`
- **File path:** `src/WeatherParser.Parsing/CoverageEvaluator.cs`

### Responsibility

The `CoverageEvaluator` class checks a project's code coverage against the supplied requirements and throws exceptions if the coverage rules are not met.

### Public methods and signatures

```text
public class CoverageEvaluator
{
    public CoverageEvaluator() { }

    public void EvaluateCoverage(CoverageReport report, CoverageBaseline baseline, PullRequestType prType)
        throws CoverageBelowThresholdException,
               CoverageReportInvalidException,
               CoverageBaselineInvalidException;
}
```

### Inputs and model types

- **CoverageReport:** Parsed report counters, including covered/valid lines and covered/valid branches.
- **CoverageBaseline:** Baseline counters.
- **PullRequestType:** Enumeration describing the PR category: regular code PR, baseline below 100%, baseline exactly 100%, and documentation-only PR.

### Proposed exception types

- **CoverageBelowThresholdException:** Coverage is below the defined thresholds.
- **CoverageReportInvalidException:** The report is invalid or malformed.
- **CoverageBaselineInvalidException:** The baseline is invalid or malformed.

## 2. CoverageReport

### Namespace and file path

- **Namespace:** `WeatherParser.Parsing.Models`
- **File path:** `src/WeatherParser.Parsing/Models/CoverageReport.cs`

### Responsibility

The `CoverageReport` class represents the parsed coverage report and its counter values.

### Public methods and signatures

```csharp
public class CoverageReport
{
    public int CoveredLines { get; }
    public int ValidLines { get; }
    public int CoveredBranches { get; }
    public int ValidBranches { get; }

    public CoverageReport(int coveredLines, int validLines, int coveredBranches, int validBranches);
}
```

## 3. CoverageBaseline

### Namespace and file path

- **Namespace:** `WeatherParser.Parsing.Models`
- **File path:** `src/WeatherParser.Parsing/Models/CoverageBaseline.cs`

### Responsibility

The `CoverageBaseline` class represents baseline coverage and its counter values.

### Public methods and signatures

```csharp
public class CoverageBaseline
{
    public int CoveredLines { get; }
    public int ValidLines { get; }
    public int CoveredBranches { get; }
    public int ValidBranches { get; }

    public CoverageBaseline(int coveredLines, int validLines, int coveredBranches, int validBranches);
}
```

## 4. PullRequestType

### Namespace and file path

- **Namespace:** `WeatherParser.Parsing.Enums`
- **File path:** `src/WeatherParser.Parsing/Enums/PullRequestType.cs`

### Responsibility

The `PullRequestType` enumeration defines the different pull request types proposed for evaluation.

### Public signature

```csharp
public enum PullRequestType
{
    NormalCodePR,
    BaselineLessThan100,
    BaselineExactly100,
    DocumentationOnlyPR
}
```

## P1.08 implementation steps

1. **Implement CoverageReport.** Add the listed properties and constructor. Test different values (`T-P1-09`).
2. **Implement CoverageBaseline.** Add the listed properties and constructor. Test different values (`T-P1-10`).
3. **Implement PullRequestType.** Add the proposed enumeration values.
4. **Implement CoverageEvaluator.** Implement `EvaluateCoverage` and handling for `CoverageBelowThresholdException`, `CoverageReportInvalidException` and `CoverageBaselineInvalidException`. Test different scenarios (`T-P1-11`, `T-P1-12`, `T-P1-13`).
5. **Implement CoverageEvaluator unit tests.** Follow Red → Green → Refactor. The original proposal associates these test IDs and cases:
   - `T-P1-14`: `CoverageBelowThresholdException` when coverage is below 80%.
   - `T-P1-15`: `CoverageReportInvalidException` for an invalid report.
   - `T-P1-16`: `CoverageBaselineInvalidException` for an invalid baseline.
   - `T-P1-17`: Correct evaluation for a regular code PR.
   - `T-P1-18`: Correct evaluation for a PR with an exact-100% baseline.
6. **Implement evaluator meta-tests.** Add behavior and regression tests proving correct operation and excluding unexpected behavior.
7. **Refactor and document.** Keep the implementation readable and maintainable; document usage of the class/interface.

## Scope boundaries

- Parsing Cobertura XML files follows as a separate step if it is outside the core scope.
- Test code does not count as production code.
- The coverage checkers themselves require behavior/regression meta-tests.

## Review constraints

The original proposal's test-ID assignments differ from the binding [P1 detailed plan](../../PHASE_P1_PLAN.md). Preserve the binding definitions; reconcile this draft before implementation. The repository's interface/constructor-injection rules also apply to executable evaluator logic. This translation does not approve the proposed production-library placement, waive those rules, relabel block coverage as branch coverage, or declare P1.08 complete.
