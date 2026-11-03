# P1.08 closure – Coverage Gate Completion: Implementation Plan

**Status: COMPLETE AND ACCEPTED by the user (2026-10-19), with the documented original Red-step deviation accepted for P1.08 only.** See the [acceptance record](../../p1/P1_08_COVERAGE_EVALUATION.md#p108-acceptance-2026-10-19). The procedure below preserves the intended implementation sequence; it is not a claim that the missing historical Red steps occurred. Drafted with the local qwen3-coder:30b model over three review rounds and finalized by review against [AGENTS.md](../../../AGENTS.md), the [P1 detailed plan](../PHASE_P1_PLAN.md) (sections 4 and 5), the [P1.08 record](../../p1/P1_08_COVERAGE_EVALUATION.md) and the [P1.09 plan](P1_09_LOCAL_CI_PLAN.md), which depends on this package. Covers T-P1-09 to T-P1-18, T-P1-23 and T-P1-24.

## 1. Goal and Scope

Close the open P1.08 items so that the implemented coverage evaluator can be executed non-interactively by the P1.09 CI script, with reproducible tooling and fixture-based evidence.

In scope:
- An executable gate runner `tools/WeatherParser.Coverage.Runner` with the commands `evaluate` and `write-baseline`.
- A test project for the runner, written test-first.
- Reader tests that load the existing synthetic fixtures instead of inline strings.
- `dotnet-coverage` pinned in a local tool manifest.
- The evaluator's own block coverage, measured and recorded.

Out of scope:
- Any change to the gate contract: `ICoverageGate`, `CoverageGateRequest` and `CoverageResult` stay as implemented.
- Git or CI orchestration: changed-file list, baseline pass and artifacts belong to P1.09.
- P2 entities and serializers, P3 example CLI (this runner is a developer tool, not the P3 CLI), P4–P8 parsers, P9 release.

## 2. Preconditions and Inputs

- `tools/WeatherParser.Coverage` and `tests/WeatherParser.Coverage.Tests` (78 passing tests) as committed in `f289026`.
- 17 fixtures in `tests/WeatherParser.Coverage.Tests/Fixtures`; 12 of them are not yet loaded by any test.
- The user decided on 2026-10-19 to remove the unused entity scaffolding (commit `2f8c670`). The library now measures 57/63 = 90.48% blocks. The remaining uncovered blocks are `EndpointUriValidator.IsValid` 12/16 and the `ParserEndpointValidator` constructor 3/5.
- `Microsoft.CodeCoverage` 18.11.0 is pinned centrally. `dotnet-coverage` is currently a global tool, version 18.12.0, with no manifest.
- The baseline JSON format is defined in [P1_08_COVERAGE_EVALUATION.md](../../p1/P1_08_COVERAGE_EVALUATION.md) section 3 and implemented by `BaselineMetadataReader`.

## 3. Decisions Required from the User

**Pinned `dotnet-coverage` version.** Options:
- (A) 18.11.0, aligned with `Microsoft.CodeCoverage`.
- (B) 18.12.0, the currently installed global version, with the mismatch documented.

**Recommended: A, if 18.11.0 is published on nuget.org.** This must be checked at implementation time, not assumed. Either way, the pinned version must reproduce the P1.07 counters (section 6, tool row).

## 4. Artifacts

| Path | New/Changed | Responsibility |
|---|---|---|
| `tools/WeatherParser.Coverage.Runner/WeatherParser.Coverage.Runner.csproj` | New | Non-packable `net10.0` console app; references only `tools/WeatherParser.Coverage`; no third-party packages; `packages.lock.json` |
| `…/Program.cs` | New | Composition root: manual constructor wiring of the real implementations, then `return await app.RunAsync(args, ct)`; no other logic |
| `…/RunnerCommands.cs` | New | Records `EvaluateCommand(ReportPath, BaselinePath?, ChangedFilesPath, ExpectedBaselineCommit?, ToolName, ToolVersion, Exclusions, Bootstrap)` and `WriteBaselineCommand(ReportPath, CommitId, ToolName, ToolVersion, Exclusions, OutputPath)`; record `UsageError(Message)` |
| `…/ICommandLineParser.cs`, `CommandLineParser.cs` | New | Turns `string[]` into one of the three records; no I/O; no dependencies |
| `…/IResultWriter.cs`, `JsonResultWriter.cs` | New | Writes `CoverageResult` as indented camelCase JSON to an injected `TextWriter` (stdout) |
| `…/IFileWriter.cs`, `FileWriter.cs` | New | File-write boundary (`WriteAllTextAsync`), mirroring `IFileReader` |
| `…/IBaselineWriter.cs`, `BaselineWriter.cs` | New | Ctor: `ICoverageReportReader`, `IProvenanceAggregator`, `IFileWriter`; reads the report, aggregates the product counters and writes baseline JSON in the documented format |
| `…/IRunnerApplication.cs`, `RunnerApplication.cs` | New | Ctor: `ICommandLineParser`, `ICoverageGate`, `IBaselineWriter`, `IResultWriter`, `IFileReader`, `TextWriter` (stderr); checks that an explicitly supplied baseline file exists, dispatches commands and maps outcomes to exit codes 0/1/2; English diagnostics to stderr |
| `tests/WeatherParser.Coverage.Runner.Tests/*` | New | MSTest/Moq/FluentAssertions tests with `MockRepository` and `VerifyAll()` in `[TestCleanup]`; references the runner project; `packages.lock.json` |
| `tests/WeatherParser.Coverage.Tests/CoverageReportReaderTests.cs`, `BaselineMetadataReaderTests.cs` | Changed | Load fixture files instead of inline duplicates |
| `tests/WeatherParser.Coverage.Tests/Fixtures/report-passing.xml` | New | Production module 80/20, used to prove the passing path |
| `.config/dotnet-tools.json` | New | Pins `dotnet-coverage` (decision in section 3) |
| `WeatherParser.slnx` | Changed | Adds the runner and runner-test projects |
| `docs/p1/P1_08_COVERAGE_EVALUATION.md`, `PHASE_P1_PLAN.md`, `DEPENDENCIES.md`, `README.md` | Changed | Result record, P1.08 row, local tool entry, runner and conversion commands |

**Runner contract (shared with P1.09; change only together with it):**
- stdout carries JSON `{ "passed": bool, "current": { "covered": n, "notCovered": n } | null, "rules": [ { "ruleId": s, "verdict": bool, "diagnostic": s } ] }`.
- Exit codes:
  - 0: all rules passed.
  - 1: at least one coverage rule failed.
  - 2: usage error, or an input error: a missing or invalid report or changed-file list, an explicitly supplied `--baseline` file that does not exist or is invalid, or a duplicate module.
- Missing baseline, two distinct cases:
  - `--baseline <path>` given but the file does not exist: input error, exit 2. The runner checks this itself before calling the gate, because the gate's `BaselineMetadataReader` deliberately returns null for a missing file.
  - `--baseline` omitted and `--bootstrap` not given: not an input error. The gate runs without a baseline, its `Baseline` rule fails, and the exit code is 1.
  - The evaluator itself is unchanged.
- `--expected-baseline-commit` is required exactly when `--baseline` is given, and is a usage error otherwise (including together with `--bootstrap`).
- `--bootstrap` and `--baseline` are mutually exclusive.
- `--exclude` may be repeated.

## 5. Procedure

Each behaviour follows Red → Green → Refactor. The Red step is a named failing test, run and observed failing before any implementation.

1. **Project setup.** Create both projects, add them to `WeatherParser.slnx`, and run a locked restore (`dotnet restore` to create the lock files, then `dotnet restore --locked-mode`). Write no logic yet.
2. **CommandLineParser** (pure, no mocks):
   - `Parse_EvaluateWithAllOptions_ReturnsEvaluateCommand`
   - `Parse_RepeatedExclude_KeepsAllPatterns`
   - `Parse_MissingRequiredOption_ReturnsUsageError`
   - `Parse_UnknownOption_ReturnsUsageError`
   - `Parse_BaselineWithoutExpectedCommit_ReturnsUsageError`
   - `Parse_BootstrapWithBaseline_ReturnsUsageError`
   - `Parse_ExpectedCommitWithoutBaseline_ReturnsUsageError` (covers `--bootstrap --expected-baseline-commit`)
   - `Parse_WriteBaselineWithAllOptions_ReturnsWriteBaselineCommand`

   Every usage-error message is English.
3. **JsonResultWriter:** `Write_Result_ProducesCamelCaseContract` asserts exactly the property names above, on a `StringWriter`.
4. **BaselineWriter** (mocks for the reader, aggregator and `IFileWriter`):
   - `WriteAsync_WritesDocumentedBaselineFormat`
   - `WriteAsync_InputException_Propagates`
5. **RunnerApplication** (mocks for all dependencies):
   - Exit 0 when `Passed` is true.
   - Exit 1 when a non-`Input` rule failed.
   - Exit 2 for a `UsageError`.
   - Exit 2 when the result contains a failed `Input` rule.
   - Exit 2 when `--baseline` names a non-existing file; the gate is not called.
   - `write-baseline` returns 0, or 2 on `CoverageInputException`.
   - `OperationCanceledException` propagates.
   - stderr receives the diagnostic in every failure case.
6. **Composition root.** Write `Program.cs`. Add one end-to-end test class without mocks, `RunnerEndToEndTests`, which wires the real components the way `Program.cs` does. It needs a shared wiring method, so Program and the tests cannot diverge. It covers the end-to-end rows of section 6.
7. **Fixture-based reader tests.** Replace the inline report and baseline strings with `Path.Combine(AppContext.BaseDirectory, "Fixtures", name)` loads, so every fixture is used by at least one test. Delete a fixture only if it is truly redundant, and list it in the record. The 78 existing tests must stay green.
8. **Tool manifest.** Run `dotnet new tool-manifest`, then `dotnet tool install dotnet-coverage --version <decision>`. Then run `dotnet tool run dotnet-coverage merge <P1.07 raw .coverage> -f xml -o <tmp>/coverage.xml` and compare the module counters with the stored P1.07 conversion: 57/33 and 189/4.
9. **Self-coverage.** Run `dotnet test tests/WeatherParser.Coverage.Tests --collect:"Code Coverage"` and the same for the runner tests, then convert with the pinned tool. Record the block counters of `WeatherParser.Coverage.dll` and `WeatherParser.Coverage.Runner.dll`. The target is at least 80% blocks; report a lower value honestly. These tool modules are excluded from the product gate (`--exclude WeatherParser.Coverage*.dll`), but they are still tested.
10. **Current-library check.** Collect a fresh product coverage report of `tests/WeatherParser.Parsing.Tests` and run `dotnet run --project tools/WeatherParser.Coverage.Runner -- evaluate --report <xml> --changed-files tests/WeatherParser.Coverage.Tests/Fixtures/changes-docs-and-csproj.txt --tool-name Microsoft.CodeCoverage --tool-version 18.11.0 --exclude *.Tests.dll --exclude WeatherParser.Coverage*.dll --bootstrap`. Expect exit 0 with `current` 57/6.

## 6. Tests and Test IDs

| Test ID | Test / evidence | Expected result |
|---|---|---|
| T-P1-09 | End-to-end `evaluate --bootstrap` on `report-real-p1-07.xml` | Exit 1; `current` 57/33; Minimum diagnostic contains "57/90" |
| T-P1-09 | End-to-end `evaluate --bootstrap` on `report-passing.xml` | Exit 0 |
| T-P1-09 | Step 10, fresh report of the current library | Exit 0; 57/63 |
| T-P1-10 | Existing `RuleEvaluatorTests` (equality below 100% fails, increase passes) | Unchanged, pass |
| T-P1-11 | Reader tests with `report-cobertura-branch-free.xml` and `report-no-block-counters.xml`; runner end-to-end | `CoverageInputException`; runner exit 2 |
| T-P1-12 | `report-rounded-100.xml` against an exact-100% baseline | Comparison fails (99999/100000 is not 100%) |
| T-P1-13 | `report-malformed.xml`; non-existing report path | Exit 2 with English diagnostic |
| T-P1-13 | `report-zero-denominator.xml` | Measurement rule fails |
| T-P1-14 | `evaluate` without `--baseline` and without `--bootstrap` (end-to-end) | Baseline rule fails; exit 1 |
| T-P1-14 | `evaluate --baseline <non-existing path> --expected-baseline-commit <sha>` (end-to-end) | Exit 2; diagnostic names the path; no gate result |
| T-P1-14 | The exact P1.09 bootstrap invocation `evaluate --report report-passing.xml --changed-files changes-docs-and-csproj.txt --tool-name Microsoft.CodeCoverage --tool-version 18.11.0 --exclude *.Tests.dll --exclude WeatherParser.Coverage*.dll --bootstrap` (end-to-end) | Exit 0 |
| T-P1-14 | The same invocation plus `--expected-baseline-commit <sha>` | Usage error; exit 2 |
| T-P1-15 | `baseline-stale-commit.json` | Comparability fails; exit 1 |
| T-P1-16 | `baseline-other-tool-version.json`, `baseline-other-exclusions.json` | Comparability fails |
| T-P1-16 | `baseline-malformed.json`, `baseline-missing-counters.json` | Exit 2 |
| T-P1-17 / T-P1-24 | `report-duplicate-module.xml` | Duplicate production module rejected; exit 2 |
| T-P1-18 | Existing `RuleEvaluatorTests` (exact 100%, documentation equality) | Unchanged, pass |
| T-P1-23 | `changes-docs-only.txt`, `changes-docs-and-csproj.txt`, `changes-coverage-config.txt` through the runner | DocumentationOnly only for the docs-only list |
| T-P1-24 | `report-valid.xml`, `baseline-valid.json` | Module ids, names, paths, counters and metadata read exactly |
| Runner | `write-baseline` on `report-real-p1-07.xml`, read back with `BaselineMetadataReader` | Same commit, tool and exclusions; counters 57/33 |
| Runner | Parser rows of step 2 | Usage error exit 2; all exclusions kept |
| Tool | Pinned `dotnet-coverage` conversion of the P1.07 raw report | Counters 57/33 and 189/4 unchanged; any difference recorded as a finding |

## 7. Evidence to Record

Update section 8 of `docs/p1/P1_08_COVERAGE_EVALUATION.md` with:
- Implementation commit.
- Pinned `dotnet-coverage` version and the counter-identity result.
- Runner exit codes and JSON for the real P1.07 report and for the current library.
- Self-coverage counters of both tool modules.
- Test counts per project.
- Every deleted fixture.

Update the P1.08 row in `PHASE_P1_PLAN.md`, the local tool entry in `DEPENDENCIES.md`, and the runner and conversion commands in `README.md`. P1.08 is accepted only after this evidence exists.

## 8. Risks and Limitations

- The pinned `dotnet-coverage` could report counters differently from the P1.07 conversion. The step 8 comparison detects this; a difference is documented, never ignored.
- The runner JSON and exit codes are a contract with P1.09: change them only together with the P1.09 script.
- End-to-end tests depend on `CopyToOutputDirectory` for fixtures. They must not need git, network or a broker.
- Windows x64 evidence only; Linux ARM64 coverage collection is P1.10.

## 9. Acceptance Criteria

- [x] The decision in section 3 is recorded, and `.config/dotnet-tools.json` pins that version.
- [x] `dotnet tool restore` works, and the conversion counter check passes or its difference is documented.
- [x] Every row of section 6 passes.
- [x] Every remaining fixture is loaded by at least one test; deleted fixtures are listed.
- [x] The runner uses exactly the contract in section 4, proven by tests.
- [x] Every doing class in the runner has an interface and constructor injection; `Program.cs` contains wiring only.
- [x] Self-coverage counters of `WeatherParser.Coverage.dll` and `WeatherParser.Coverage.Runner.dll` are recorded.
- [x] Documents named in section 7 are updated; the build has 0 warnings; all tests pass offline.

These checkboxes reference the recorded closure verification accepted by the user, not a fresh build/test run. The acceptance record retains the later aborted-build limitation and the expressly accepted original Red-step deviation.
