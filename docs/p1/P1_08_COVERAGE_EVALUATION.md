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
- **Library coverage decision (2026-10-19):** The user decided to remove the unused, untested export-entity scaffolding (`src/WeatherParser.Parsing/Entities`, 19 files) instead of excluding it or bringing P2 serializer tests forward; P2 reintroduces each entity test-first. Re-measured with Microsoft.CodeCoverage: `WeatherParser.Parsing.dll` 57/63 blocks = 90.48% (previously 57/90 = 63.33%). Remaining uncovered blocks: `EndpointUriValidator.IsValid` 12/16 and the `ParserEndpointValidator` constructor 3/5. This measurement was converted with the global `dotnet-coverage` 18.12.0 tool and has not yet been run through an executable gate runner.
- **Package consumer:** Repacking showed that the committed consumer lock file still pinned the P1.04 package, which predates the DI dependency added in P1.06; the consumer had been restoring a stale cached package. The cache entry was removed and the lock file now pins the current package (packed SHA-512 equals the restored hash).

### P1.08 closure results (2026-10-19, Windows x64)

Implemented according to the [closure plan](P1_08B_COVERAGE_GATE_COMPLETION_PLAN.md). Not yet committed at the time of writing; acceptance is pending review.

- **Pinned tool (closure decision):** `dotnet-coverage` 18.11.0 is published on nuget.org and pinned in `.config/dotnet-tools.json`, matching `Microsoft.CodeCoverage` 18.11.0. The SDK 10 default created the manifest at the repository root; it was moved to `.config/` as planned. Its license is the proprietary Microsoft Software License Terms – Microsoft .NET Library (developer tool, not redistributed; see `DEPENDENCIES.md`).
- **Counter identity:** converting the P1.07 raw report with the pinned 18.11.0 gives exactly the earlier counters: `WeatherParser.Parsing.dll` 57/33, `WeatherParser.Parsing.Tests.dll` 189/4.
- **Runner:** `tools/WeatherParser.Coverage.Runner` with the commands `evaluate` and `write-baseline`, the JSON contract and exit codes 0/1/2 from the closure plan. An explicitly supplied `--baseline` file that does not exist is an input error (exit 2); omitting `--baseline` without `--bootstrap` fails the `Baseline` rule (exit 1).
- **Fixtures:** all 20 fixtures in `tests/WeatherParser.Coverage.Tests/Fixtures` are loaded by at least one test; none was deleted. New: `report-passing.xml`, `baseline-passing.json`, `baseline-exact-100.json`.
- **Tests:** 149 passing offline (`WeatherParser.Coverage.Tests` 85, `WeatherParser.Coverage.Runner.Tests` 54, `WeatherParser.Parsing.Tests` 8, `WeatherParser.Package.Tests` 2); 0 build warnings. The end-to-end tests cover every row of the closure plan's section 6, including the exact P1.09 bootstrap invocation.
- **Self-coverage (pinned tool, each module measured by its own test project):** `WeatherParser.Coverage.dll` 827/896 = 92.30%; `WeatherParser.Coverage.Runner.dll` 378/426 = 88.73%. Both are above 80%; these tool modules are excluded from the product gate.
- **Gate on real data:**
  - The stored P1.07 report through the runner exits 1 at "57/90 (63.33%)".
  - A fresh report of the current library through the exact bootstrap invocation exits 0 with 57/63 = 90.48%.
- **How the code was produced:**
  - The local qwen3-coder:30b model generated the runner, its tests and the fixture-based reader tests in six batches plus repair rounds.
  - **Written by Claude:** `CommandLineParser` (three qwen attempts failed to compile or violated the contract) and `report-passing.xml` (qwen used child elements instead of the native attributes).
  - **Corrected by Claude:** wrong assertions in qwen's tests (three baseline-reader tests expecting exceptions the reader must not throw, output-channel assertions contradicting the runner contract, a parity-based argument filter that removed nothing), and small compile errors.
- **Deviation from TDD:** tests and implementation were generated together per batch, so a failing (Red) test before each implementation was not demonstrated per behaviour. The behaviour is covered by the tests above; the Red step is not evidenced.
- **Not yet proven:** no non-bootstrap gate run exists yet. The closure's own changes count as a code change, so a later two-pass run against a target with the same 57/63 will fail the `Comparison` rule unless coverage strictly improves (see the P1.10 plan, decision h). Running the gate inside a CI sequence is P1.09.

### Correction: documentation-only classification (2026-10-19, after b6100e2)

- **Defect:** `PullRequestClassifier` at `b6100e2` granted the documentation-only bypass too broadly. Any `.txt` or `.markdown` file counted as documentation, and so did any path containing `/docs/` or `/documentation/`, whatever its extension (for example `src/WeatherParser.Parsing/docs/Foo.cs`). This violated the binding rule (PHASE_P1_PLAN.md section 5, T-P1-23): a pull request is documentation-only only when every changed path is a Markdown documentation file. The defect came from the model-generated code and was missed in review.
- **Red:** the new regression test `Classify_NonMarkdownPathEvenInDocumentationFolder_ReturnsCode` (T-P1-23) failed for all five cases before the fix: `notes.txt`, `docs/notes.txt`, `src/WeatherParser.Parsing/docs/Foo.cs`, `tools/documentation/Build.ps1`, and `README.md` together with a `.txt` fixture. Each was classified as DocumentationOnly instead of Code.
- **Green:** the classifier now accepts only paths ending in `.md` (case-insensitive); the folder rule, `.txt` and `.markdown` were removed. All 154 tests pass offline with 0 build warnings (`WeatherParser.Coverage.Tests` 90, `WeatherParser.Coverage.Runner.Tests` 54, `WeatherParser.Parsing.Tests` 8, `WeatherParser.Package.Tests` 2).
- **Effect:** a commit changing a lock file, a `.txt` file or any non-Markdown file in a documentation folder is now a code change and must strictly improve coverage below a 100% baseline. The README and consumer-lock commit of the P1.11 plan is therefore correctly classified as code.

### P1.08 acceptance (2026-10-19)

**Current status: COMPLETE AND ACCEPTED by the user.** The user explicitly stated: "I accept this deviation" and instructed that P1.08 be marked complete. This acceptance supersedes earlier pending/not-yet-accepted status statements above; those statements and original evidence remain preserved as historical text.

- **Accepted deviation:** the original P1.08 implementation and tests were generated together, so a failing Red test before each implementation behavior was not evidenced. This exception applies only to that P1.08 history. It does not invent retrospective Red evidence, relax future TDD requirements, waive coverage rules or accept unrelated deviations.
- **Implementation references:** `b6100e2` contains the closure implementation and pinned tooling; `a562f63` contains the test-first classifier correction.
- **Acceptance basis:** the recorded closure results and correction report: 154 passing offline tests after correction, zero recorded build warnings, all 20 fixtures referenced, pinned conversion with unchanged P1.07 counters, and the recorded tool self-coverage. These are the existing verification results, not a new test-suite execution.
- **Review smoke checks:** the existing runner returned exit 0 for the synthetic 80/100 report and exit 1 for the historical 57/90 report, as required. A fresh Release build attempt aborted with MSB5021 and no source error reported; the full suite was not rerun successfully during this review. No new clean-build/test success is claimed.
- **Remaining scope:** local CI execution and the first accepted integrated baseline belong to P1.09. Debian/ARM64 evidence remains P1.10, documentation handover remains P1.11, and overall phase acceptance remains P1.12. Completion of P1.08 does not close those checks or authorize publication.
