# P1.08 – Coverage Evaluation: Implementation Plan

**Status: implemented, not yet accepted (2026-10-19).** Code was generated with the local qwen3-coder:30b model from this plan and then reviewed and corrected; see [Implementation notes](#8-implementation-notes-2026-10-19). P1.08 acceptance is still pending. Drafted with the local qwen2.5-coder model and reviewed against [AGENTS.md](../../AGENTS.md), [REQUIREMENTS.md](../../REQUIREMENTS.md) (REQ-086) and the [P1 detailed plan](../../PHASE_P1_PLAN.md), which take precedence. Implements the coverage decision dated 2026-10-19: native Microsoft.CodeCoverage block coverage is the only metric. Replaces the earlier draft `P1_08_PLAN.qwen.md`.

## 1. Goal and Root Cause

**Goal:**  
Implement a robust coverage evaluation system for the WeatherParser repository that adheres to the specified binding coverage contract and test IDs. This system will evaluate the code-block coverage from the native Microsoft.CodeCoverage XML report and ensure that the coverage meets the minimum threshold of 80%. The system will also handle exceptions and edge cases, such as missing or stale baselines, and provide meaningful diagnostics.

**Root Cause:**  
Microsoft.CodeCoverage measures no branches; its Cobertura conversion reports a vacuous branch-rate="1" with zero branch entries. Line counts are ambiguous for partially covered lines: the native report gives 50.00% while Cobertura gives 54.8% for the same run. Therefore the user decided on 2026-10-19 to use native block coverage as the only metric. The old draft also placed tooling in the published library, gave interfaces to data types and had overlapping evaluators.

## 2. Architecture

### Types Table

| Type | Namespace | File Path | Interface | Constructor Dependencies | Single Responsibility |
|------|-----------|-----------|-----------|------------------------|----------------------|
| CoverageGate | WeatherParser.Coverage | tools/WeatherParser.Coverage/CoverageGate.cs | ICoverageGate | ICoverageReportReader, IBaselineMetadataReader, IChangedFileListReader, IProvenanceAggregator, IRuleEvaluator, IBaselineComparabilityChecker, IPullRequestClassifier | Orchestrates the coverage evaluation process |
| CoverageReportReader | WeatherParser.Coverage | tools/WeatherParser.Coverage/CoverageReportReader.cs | ICoverageReportReader | IFileReader | Reads and parses the native Microsoft.CodeCoverage XML report |
| BaselineMetadataReader | WeatherParser.Coverage | tools/WeatherParser.Coverage/BaselineMetadataReader.cs | IBaselineMetadataReader | IFileReader | Reads baseline metadata from a JSON file |
| ChangedFileListReader | WeatherParser.Coverage | tools/WeatherParser.Coverage/ChangedFileListReader.cs | IChangedFileListReader | IFileReader | Provides the changed-file list from a text file |
| ProvenanceAggregator | WeatherParser.Coverage | tools/WeatherParser.Coverage/ProvenanceAggregator.cs | IProvenanceAggregator | none | Aggregates block counters of production modules after exclusions and rejects duplicate assemblies (same module identity/version/PDB); no baseline metadata |
| BaselineComparabilityChecker | WeatherParser.Coverage | tools/WeatherParser.Coverage/BaselineComparabilityChecker.cs | IBaselineComparabilityChecker | none | Checks the comparability of the baseline metadata |
| PullRequestClassifier | WeatherParser.Coverage | tools/WeatherParser.Coverage/PullRequestClassifier.cs | IPullRequestClassifier | none | Classifies the PR as DocumentationOnly only if every changed path is a Markdown documentation file; any change to source, projects, packages, build/test/coverage/CI configuration or exclusions makes it Code |
| RuleEvaluator | WeatherParser.Coverage | tools/WeatherParser.Coverage/RuleEvaluator.cs | IRuleEvaluator | none | Evaluates coverage rules and returns structured results with per-check verdicts and diagnostics |
| CoverageResult | WeatherParser.Coverage | tools/WeatherParser.Coverage/CoverageResult.cs | none | none | Immutable record representing the coverage evaluation result |
| RuleResult | WeatherParser.Coverage | tools/WeatherParser.Coverage/RuleResult.cs | none | none | Immutable record representing the result of a single rule evaluation |
| BlockCounters | WeatherParser.Coverage | tools/WeatherParser.Coverage/BlockCounters.cs | none | none | Immutable record representing block counters (Covered, NotCovered) |
| ModuleCoverage | WeatherParser.Coverage | tools/WeatherParser.Coverage/ModuleCoverage.cs | none | none | Immutable record representing module coverage (Name, ModuleId, Version, PdbPath/SourceIdentity, BlockCounters) |
| CoverageReport | WeatherParser.Coverage | tools/WeatherParser.Coverage/CoverageReport.cs | none | none | Immutable record representing the coverage report (modules, tool name/version, format, commit ID) |
| BaselineMetadata | WeatherParser.Coverage | tools/WeatherParser.Coverage/BaselineMetadata.cs | none | none | Immutable record representing baseline metadata (commit ID, tool name/version, format, exclusions, BlockCounters) |
| ChangedFileList | WeatherParser.Coverage | tools/WeatherParser.Coverage/ChangedFileList.cs | none | none | Immutable record representing the changed-file list |
| PullRequestCategory | WeatherParser.Coverage | tools/WeatherParser.Coverage/PullRequestCategory.cs | none | none | Enum representing the category of the pull request (DocumentationOnly, Code) |
| FileReader | WeatherParser.Coverage | tools/WeatherParser.Coverage/FileReader.cs | IFileReader | none | Reads files from the file system |

### Test Classes Table

| Test Class | Namespace | File Path |
|------------|-----------|-----------|
| CoverageGateTests | WeatherParser.Coverage.Tests | tests/WeatherParser.Coverage.Tests/CoverageGateTests.cs |
| CoverageReportReaderTests | WeatherParser.Coverage.Tests | tests/WeatherParser.Coverage.Tests/CoverageReportReaderTests.cs |
| BaselineMetadataReaderTests | WeatherParser.Coverage.Tests | tests/WeatherParser.Coverage.Tests/BaselineMetadataReaderTests.cs |
| ChangedFileListReaderTests | WeatherParser.Coverage.Tests | tests/WeatherParser.Coverage.Tests/ChangedFileListReaderTests.cs |
| ProvenanceAggregatorTests | WeatherParser.Coverage.Tests | tests/WeatherParser.Coverage.Tests/ProvenanceAggregatorTests.cs |
| BaselineComparabilityCheckerTests | WeatherParser.Coverage.Tests | tests/WeatherParser.Coverage.Tests/BaselineComparabilityCheckerTests.cs |
| PullRequestClassifierTests | WeatherParser.Coverage.Tests | tests/WeatherParser.Coverage.Tests/PullRequestClassifierTests.cs |
| RuleEvaluatorTests | WeatherParser.Coverage.Tests | tests/WeatherParser.Coverage.Tests/RuleEvaluatorTests.cs |

### Project setup

- The `tests/WeatherParser.Coverage.Tests` project is an MSTest project configured with `IsPackable=false` and targeting `net10.0`.
- This test project is included in the `WeatherParser.slnx` solution.
- It references only the `tools/WeatherParser.Coverage` project via a `ProjectReference`, ensuring no dependency on `src/WeatherParser.Parsing`.
- `tools/WeatherParser.Coverage` stays non-packable (`IsPackable=false`) and is never referenced by `src/WeatherParser.Parsing`.
- MSTest, Moq, and FluentAssertions versions are managed through `Directory.Packages.props`, ensuring consistent versioning across the solution.
- A `packages.lock.json` file is generated and committed to the repository to lock down the exact versions of the dependencies used in the project.

### Rule Results

The rule results are structured with per-check verdicts and English diagnostics. Each rule evaluation returns a `RuleResult` object containing:

- `Verdict`: A boolean indicating whether the rule passed or failed.
- `Diagnostic`: A string providing a human-readable explanation of the rule's outcome.

## 3. Inputs and Formats

### Native XML Report

- **Format:** Native Microsoft.CodeCoverage XML.
- **Content:** Contains `<module>` elements with `blocks_covered` and `blocks_not_covered` attributes, and `<function>` elements with similar attributes.
- **Example:**
  ```xml
  <module name="WeatherParser.Parsing.dll" blocks_covered="57" blocks_not_covered="33">
    <function name="MethodA" blocks_covered="20" blocks_not_covered="5"/>
    <function name="MethodB" blocks_covered="37" blocks_not_covered="28"/>
  </module>
  ```

### Baseline/Metadata JSON

- **Format:** JSON.
- **Content:** Contains metadata about the baseline, such as commit ID, tool name/version, format, exclusions, and block counters.
- **Example:**
  ```json
  {
    "commitId": "abc123",
    "toolName": "Microsoft.CodeCoverage",
    "toolVersion": "1.0.0",
    "format": "native",
    "exclusions": [
      "*.Tests.dll",
      "*.Tools.dll"
    ],
    "blocksCovered": 57,
    "blocksNotCovered": 33
  }
  ```

### Changed-File List

- **Format:** Plain text list of file paths.
- **Content:** Contains the list of files changed in the current PR, obtained from `git diff --name-only`.
- **Example:**
  ```
  src/WeatherParser.Parsing/Parser.cs
  tests/WeatherParser.Parsing.Tests/ParserTests.cs
  ```

### Exclusion Configuration

- **Format:** JSON.
- **Content:** Contains a list of assembly/module names to exclude from coverage evaluation.
- **Example:**
  ```json
  {
    "exclusions": [
      "*.Tests.dll",
      "*.Tools.dll"
    ]
  }
  ```

## 4. TDD Sequence Mapped to Test IDs

### T-P1-13: Broken/Missing Report Fails

1. **Red:** Write a test that checks if the coverage evaluation fails when the native XML report is broken or missing.
2. **Green:** Implement the `CoverageReportReader` to handle broken or missing reports and return a failure if the report is invalid.
3. **Refactor:** Ensure that the `CoverageReportReader` is robust and can handle different error scenarios.

### T-P1-11: Metric Identity: Line-Rate, Branch-Rate or Other Cobertura-Derived Values Are Never Accepted as or Substituted for Block Counters; Missing Native Block Counters Fail

1. **Red:** Write a test that checks if the coverage evaluation fails when the native XML report is missing block counters.
2. **Green:** Implement the `CoverageReportReader` to validate the presence of block counters in the native XML report and return a failure if they are missing.
3. **Refactor:** Ensure that the `CoverageReportReader` is robust and handles different XML structures gracefully.

### T-P1-24: Report/Package Provenance: Counters, Blocks, PDB, Version Traceable

1. **Red:** Write a test that checks if the coverage evaluation correctly traces the provenance of counters, blocks, PDB, and version.
2. **Green:** Implement the `CoverageReportReader` to capture module identity, version, PDB/source fields and the `ProvenanceAggregator` to aggregate counters across production modules.
3. **Refactor:** Ensure that the `CoverageReportReader` and `ProvenanceAggregator` are well-structured and follow the single responsibility principle.

### T-P1-17: Aggregation: Total Counters, No Duplicate Package Assembly

1. **Red:** Write a test that checks if the coverage evaluation correctly aggregates counters across production modules and avoids double-counting.
2. **Green:** Implement the `ProvenanceAggregator` to aggregate counters across production modules and avoid double-counting.
3. **Refactor:** Ensure that the `ProvenanceAggregator` is well-structured and follows the single responsibility principle.

### T-P1-09: Minimum 80% Passes, Below Fails

1. **Red:** Write tests with BlockCounters 80/100 (exactly 80%, must pass) and 79/100 (must fail). The `RuleEvaluator` receives already aggregated `BlockCounters` and reads no report.
2. **Green:** Implement the `RuleEvaluator` to compare the unrounded ratio `blocks_covered/(blocks_covered+blocks_not_covered)` with 0.8. Return a failure if the coverage is below 80%.
3. **Refactor:** Ensure that the `RuleEvaluator` is well-structured and follows the single responsibility principle.

### T-P1-12: Precision: Rounded 100% Is Not Exact 100%

1. **Red:** Write a test that checks if the coverage evaluation fails when the coverage is rounded to 100% but not exactly 100%.
2. **Green:** Implement the `RuleEvaluator` to correctly handle the precision of coverage percentages and return a failure if the coverage is rounded to 100% but not exactly 100%.
3. **Refactor:** Ensure that the `RuleEvaluator` is precise and avoids rounding errors.

### T-P1-14: Bootstrap: Initially Absent Baseline -> >=80%, No Comparison; Later Missing Baseline Fails

#### Case (a) Initial bootstrap (no baseline yet)

1. **Red:** Write a test that checks if the coverage evaluation passes when the baseline is initially absent and the coverage is at least 80%. Additionally, write a test that checks if the coverage evaluation fails when the baseline is initially absent and the coverage is below 80%.
2. **Green:** Implement the `RuleEvaluator` to handle the absence of a baseline and return a success if the coverage is at least 80%. If the coverage is below 80%, return a failure with an appropriate diagnostic message.
3. **Refactor:** Ensure that the `RuleEvaluator` correctly handles the bootstrap scenario and provides meaningful diagnostics.

#### Case (b) After bootstrap

1. **Red:** Write a test that checks if the coverage evaluation fails with an English diagnostic when the baseline is missing after the initial bootstrap.
2. **Green:** Implement the `RuleEvaluator` to fail with a diagnostic message if the baseline is missing after the initial bootstrap.
3. **Refactor:** Ensure that the `RuleEvaluator` correctly handles the missing baseline scenario and provides a clear diagnostic message. Note that a missing baseline is never treated as bootstrap.

### T-P1-10: Regular Code PR: Equality Below 100% Fails, Real Increase Passes

1. **Red:** Write a test that checks if the coverage evaluation fails when the coverage is equal to the baseline but below 100%. Use unrounded block-counter ratios.
2. **Green:** Implement the `RuleEvaluator` to compare the current coverage with the baseline and return a failure if the coverage is equal to the baseline but below 100%. Use unrounded block-counter ratios.
3. **Refactor:** Ensure that the `RuleEvaluator` correctly handles the comparison logic and provides meaningful diagnostics.

1. **Red:** Write a test that checks if the coverage evaluation fails when the coverage is lower than the baseline. Use unrounded block-counter ratios.
2. **Green:** Implement the `RuleEvaluator` to compare the current coverage with the baseline and return a failure with an English diagnostic when the coverage is lower than the baseline. Use unrounded block-counter ratios.
3. **Refactor:** Share the comparison logic with the equality case to avoid code duplication.

1. **Red:** Write a test that checks if the coverage evaluation passes when the coverage is strictly greater than the baseline and at least 80%. Use unrounded block-counter ratios.
2. **Green:** Implement the `RuleEvaluator` to compare the current coverage with the baseline and return a success if the coverage is strictly greater than the baseline and at least 80%. Use unrounded block-counter ratios.
3. **Refactor:** Ensure that the `RuleEvaluator` correctly handles the comparison logic and provides meaningful diagnostics.

### T-P1-18: Exceptions: Exact 100%->100% and Documentation Equality Pass; Decreases Fail

#### Case (a) Exact-100% baseline

1. **Red:** Write a test that checks if the coverage evaluation passes when the baseline and PR both have 0 blocks not covered.
2. **Green:** Implement the `RuleEvaluator` to handle the exact 100% baseline scenario and return a success if both baseline and PR have 0 blocks not covered.
3. **Refactor:** Ensure that the `RuleEvaluator` correctly handles the exact 100% baseline scenario and provides meaningful diagnostics.
4. **Red:** Write a test that checks if the coverage evaluation fails when the PR has blocks not covered against an exact-100% baseline, even if the ratio rounds to 100% (e.g., 99999/100000).
5. **Green:** Implement the `RuleEvaluator` to handle the scenario where the PR has blocks not covered against an exact-100% baseline and return a failure.
6. **Refactor:** Ensure that the `RuleEvaluator` correctly handles this scenario and provides meaningful diagnostics.

#### Case (b) Documentation-only PR (PullRequestCategory.DocumentationOnly)

1. **Red:** Write a test that checks if the coverage evaluation passes when the PR is documentation-only and the coverage is equal to the baseline.
2. **Green:** Implement the `RuleEvaluator` to handle the documentation-only PR scenario and return a success if the coverage is equal to the baseline.
3. **Refactor:** Ensure that the `RuleEvaluator` correctly handles the documentation-only PR scenario and provides meaningful diagnostics.
4. **Red:** Write a test that checks if the coverage evaluation fails when the PR is documentation-only and the coverage is lower than the baseline.
5. **Green:** Implement the `RuleEvaluator` to handle the scenario where the PR is documentation-only and the coverage is lower than the baseline and return a failure.
6. **Refactor:** Ensure that the `RuleEvaluator` correctly handles this scenario and provides meaningful diagnostics.
7. **Red:** Write a test that checks if the coverage evaluation fails when the PR is documentation-only and the coverage is below 80% even if equal to the baseline.
8. **Green:** Implement the `RuleEvaluator` to handle the scenario where the PR is documentation-only and the coverage is below 80% and return a failure.
9. **Refactor:** Ensure that the `RuleEvaluator` correctly handles this scenario and provides meaningful diagnostics.

### T-P1-15: Commit Identity: Stale/Wrong Baseline Detected

1. **Red:** Write a test that checks if the coverage evaluation fails when the baseline has a stale or wrong commit ID.
2. **Green:** Implement the `BaselineComparabilityChecker` to validate the commit ID of the baseline and return a failure if it is stale or wrong.
3. **Refactor:** Ensure that the `BaselineComparabilityChecker` is robust and can handle different commit ID scenarios.

### T-P1-16: Comparability: Different Tools/Exclusions/Counter Semantics Detected

1. **Red:** Write a test that checks if the coverage evaluation fails when the baseline has different tools, exclusions, or counter semantics.
2. **Green:** Implement the `BaselineComparabilityChecker` to validate the tool name/version, format, and exclusions of the baseline and return a failure if they are different.
3. **Refactor:** Ensure that the `BaselineComparabilityChecker` is robust and can handle different tool configurations.

### T-P1-23: Documentation Classification: Code/Build/Package/CI/Coverage Changes Get No Documentation Bypass

1. **Red:** Write a test that checks if the coverage evaluation correctly classifies files as code or documentation. Use `changes-docs-only.txt` (expect `DocumentationOnly`), `changes-docs-and-csproj.txt`, and `changes-coverage-config.txt` (expect `Code`). Also, test an empty list (expect `Code`).
2. **Green:** Implement the `PullRequestClassifier` to classify files based on the changed-file list and return the correct classification.
3. **Refactor:** Ensure that the `PullRequestClassifier` is well-structured and follows the single responsibility principle. Keep the documentation path rule in one place.

### CoverageGate TDD Step

1. **Red:** Write a test that checks if the coverage evaluation gate orchestrates the components correctly and fails with an English diagnostic when run on the real P1.07 report.
2. **Green:** Implement the `CoverageGate` to orchestrate the components and provide a meaningful English diagnostic when the coverage evaluation fails.
3. **Refactor:** Ensure that the `CoverageGate` is well-structured and follows the single responsibility principle.

## 5. Fixtures

### Native XML Fixtures

- **report-missing.xml:** A missing XML report.
- **report-malformed.xml:** A malformed XML report.
- **report-no-block-counters.xml:** An XML report with missing block counters.
- **report-zero-denominator.xml:** An XML report with a zero denominator.
- **report-cobertura-branch-free.xml:** An XML report with Cobertura-style branch-free data (must be rejected).
- **report-duplicate-module.xml:** An XML report with duplicate modules.
- **report-rounded-100.xml:** An XML report with rounded 100% coverage (e.g., 99999/100000).
- **report-real-p1-07.xml:** A sanitized version of the real P1.07 report with block counters (expected FAIL 57/90).

### Metadata Fixtures

- **baseline-valid.json:** A valid JSON file containing baseline metadata.
- **baseline-stale-commit.json:** A JSON file with a stale commit ID.
- **baseline-other-tool-version.json:** A JSON file with a different tool version.
- **baseline-other-exclusions.json:** A JSON file with different exclusions.

### Changed-File List Fixtures

- **changes-docs-only.txt:** A list of files that contain only documentation changes.
- **changes-docs-and-csproj.txt:** A list of files that contain documentation and csproj changes.
- **changes-coverage-config.txt:** A list of files that contain coverage configuration changes.

## 6. Documentation Updates

- **Replace `docs/p1/P1_08_PLAN.qwen.md` with `docs/p1/P1_08_COVERAGE_EVALUATION.md`.**
- **Update the documentation to reflect the new architecture, inputs, formats, TDD sequence, fixtures, and acceptance criteria.**

## 7. P1.08 Acceptance Criteria

1. **Evaluator Tests Pass Offline:** All tests in `tests/WeatherParser.Coverage.Tests` must pass offline.
2. **All Binding Test IDs Covered:** All binding test IDs (T-P1-09 to T-P1-18, T-P1-23, T-P1-24) must be covered and tested.
3. **Running the Gate on the Real P1.07 Report Reports FAIL (63.33% < 80%):** The coverage evaluation gate must correctly fail when run on the real P1.07 report with a coverage of 63.33%.
4. **English Diagnostic Provided:** The coverage evaluation gate must provide a meaningful English diagnostic when the coverage evaluation fails.

## 8. Implementation Notes (2026-10-19)

- **Additional type:** The native report contains no commit ID or tool version. `CoverageGateRequest` (report path, optional baseline path, changed-file list path, expected baseline commit ID, tool name/version, exclusions, initial-bootstrap flag) therefore carries these inputs into `ICoverageGate.EvaluateAsync`. Invalid inputs raise `CoverageInputException`; the gate reports them as a failed `Input` rule.
- **Rule order:** `RuleEvaluator` always reports `Measurement`, `Minimum` and `Baseline`, plus `Comparison` when a valid baseline exists. Only a failed `Measurement` (zero denominator or negative counters) stops evaluation. Ratios are compared as exact counters by `Int128` cross-multiplication; percentages are rendered for diagnostics only.
- **Provenance limits:** The native XML provides module name, module ID (hash) and path, but no assembly version or PDB path. Duplicate detection therefore uses module name and module ID.
- **Verification:** `dotnet test tests/WeatherParser.Coverage.Tests` ran offline: 78/78 passed. `CoverageGateRealReportTests` evaluates the sanitized real P1.07 report (`Fixtures/report-real-p1-07.xml`) and reports FAIL at 57/90 (63.33%) < 80%. The block coverage of the evaluator itself has not been measured yet.
- **Open items:** Most synthetic fixtures (`report-*.xml` except the real report, `baseline-*.json`) are not yet loaded by tests; the reader tests use equivalent inline content. There is no command-line entry point for running the gate in CI yet.
- **Repository:** `.gitignore` pattern `*.coverage` matched the folder `tools/WeatherParser.Coverage/` case-insensitively; a negation now keeps the tool project tracked.
