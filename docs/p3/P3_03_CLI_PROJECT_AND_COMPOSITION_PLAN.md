# P3.03 – CLI Project, Package Consumption and Composition Root: Implementation Plan

**Status: planned on 2026-10-29; accepted by the user on 2026-10-29 with the phase plan ("Accept the plans"); not started: P3 starts only on a separate instruction, and the CL rows are decided in P3.02.** Refines the P3.03 row of section 5 and the "Package boundary" and "Single command" rows of section 4 of [PHASE_P3_PLAN.md](../../PHASE_P3_PLAN.md).

## 1. Goal and Scope
Goal: an executable example CLI `WeatherParser` that consumes the same-run package `WeatherParser.Parsing` through a versioned `PackageReference`, builds and validates one DI container per process, runs exactly one command handler and exits, plus its test project, its process-test harness and its integration into the solution, `ci-local.ps1`, hosted CI and the coverage gate of the approved CL-19 option.
In scope: `examples/WeatherParser.Cli` and `tests/WeatherParser.Cli.Tests` (CL-01); the `PackageReference` and its lock-file handling; `WeatherParser.slnx` and, per CL-01 and CL-19, `WeatherParser.Ci.slnf` or a separate consumer stage; `Main` and the composition root (CL-07) with a command-dispatch seam (Proposed: `ICommandRunner`) and a placeholder command set that reports "not implemented" until P3.07; the help output (T-P3-04, the texts completed in P3.04); the process-test harness that runs the built CLI with a timeout, a temporary working directory and captured stdout, stderr and exit code; `ci-local.ps1`, `.github/workflows/ci.yml` and the coverage runner changes the approved CL-19 option needs; CONTRIBUTING.md, DEPENDENCIES.md and README project list per the consistency table.
Out of scope with owner: the argument grammar → P3.04; configuration → P3.05; logging and exit codes → P3.06 (P3.03 uses `0` for help and a provisional `1` for every other path, replaced in P3.06); commands → P3.07.

## 2. Preconditions and Inputs
- P3.02 approved CL-01, CL-07 and CL-19 (and, for option C, the recorded REQ-086 amendment).
- Existing: the package-consumer stage of `scripts/ci-local.ps1` (pack into the run directory, local feed, locked restore with the one-line `contentHash` rewrite of the consumer lock, build and test of `tests/WeatherParser.Package.Tests`), `tests/WeatherParser.Package.Tests` as the model for package consumption, `Directory.Packages.props` (`WeatherParser.Parsing` `0.1.0-initial`).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Process-test target | Run the CLI through `dotnet <dll>` / through the app host | `dotnet <path>/WeatherParser.dll` from the build output, so the same harness works on Windows and Debian without a platform-specific app host; one app-host smoke test per platform in P3.11 | Orchestrator, recorded | S3 |
| Lock-file handling of the CLI projects | As the package consumer (committed lock with the `WeatherParser.Parsing` `contentHash` rewritten in a run) / no lock file | As the package consumer: committed lock, `--locked-mode`, the one-line rewrite verified by `ci-local.ps1`, never committed back | Orchestrator, recorded (CL-01) | S1, S4 |
| Coverage collection | Per the approved CL-19 option | The CLI tests run with the same `coverage.runsettings`; option A merges the CLI report into the gate input, option C evaluates a second gate; the runner change is test-first | User (CL-19) | S4 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `examples/WeatherParser.Cli/WeatherParser.Cli.csproj`, `Program.cs`, `CompositionRoot.cs`, `ICommandRunner.cs` (Proposed) | Create | Executable `net10.0`, assembly name `WeatherParser`, `IsPackable` false, `PackageReference` to `WeatherParser.Parsing` |
| `examples/WeatherParser.Cli/packages.lock.json` | Create | Locked restore against the local feed |
| `tests/WeatherParser.Cli.Tests/…` | Create | MSTest project referencing the CLI project; the process-test harness (Proposed: `CliProcess`); first tests |
| `WeatherParser.slnx`, `WeatherParser.Ci.slnf` | Change | Per CL-01 and CL-19 |
| `scripts/ci-local.ps1`, `.github/workflows/ci.yml`, `tools/WeatherParser.Coverage*` (only for the CL-19 option that needs it) | Change | Build and test the CLI after the pack step; coverage per CL-19 |
| `CONTRIBUTING.md` (sections 2, 3, 5, 6), `DEPENDENCIES.md`, `README.md` (project list) | Change | Consistency table |
| `docs/p3/P3_VERIFICATION.md` | Change | P3.03 section |

## 5. Procedure
Every slice: qwen3-coder generates tests first, then code, from briefs with the real declarations (AGENTS.md "qwen invocation"); the orchestrating agent reviews each diff, runs the builds and tests, records Red and Green; Codex reviews the step.

### S1 Project and package reference
**Red**: `CliProject_ReferencesLibraryOnlyAsVersionedPackage` (reads the project file: exactly one `PackageReference` `WeatherParser.Parsing` with a version from `Directory.Packages.props`, no `ProjectReference` to `src/`, no `Compile` item outside the CLI directory), `CliProject_AssemblyNameIsWeatherParser`, `CliProject_IsNotPackable`.
**Green**: the two projects, the lock files, the solution entries.

### S2 Composition root
**Red**: `CompositionRoot_BuildsValidatedContainer` (with a minimal test configuration of every consumer registration: `ValidateOnBuild` and `ValidateScopes` succeed), `CompositionRoot_RegistersLibraryThroughAddWeatherParsing` (the library serializers and parsers resolve), `CompositionRoot_MissingConsumerRegistration_FailsAtBuild`, `CompositionRoot_TwoBuilds_IndependentContainers`, `Program_RunsExactlyOneCommandAndDisposesProvider` (a strict mock of `ICommandRunner` is called once; the provider is disposed afterwards).
**Green**: `Main`, the composition root and the dispatch seam.

### S3 Process harness and help
**Red**: `Process_Help_PrintsUsageToStdout_ExitCodeZero`, `Process_Help_WritesNothingToStderr`, `Process_NoArguments_PrintsUsageToStderr_NonZero` (provisional `1`, fixed in P3.06), `Process_ExitsWithoutWaiting` (the process ends within the harness timeout without input; stdin closed), `Harness_KillsProcessOnTimeout` (a harness self-test with a deliberately sleeping child).
**Green**: the help text skeleton, the harness.

### S4 CI and coverage integration
**Red**: for `ci-local.ps1` the existing script tests of the consumer stage (or new ones in the same style) expecting the CLI build and test steps and their counts in the run summary; for the CL-19 option the coverage-runner tests (option A: the merged report counts both assemblies; option C: a second gate result with its own rules); run before the change to show the failure.
**Green**: the script, workflow and runner changes; a full `ci-local.ps1` run on the step commit.

**Refactor**: with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P3 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P3-01 | REQ-012, REQ-013, C-112 | Package reference only |
| S2 | T-P3-01 | REQ-016, REQ-070, REQ-071, C-107, C-108 | Validated container, one command |
| S3 | T-P3-04, T-P3-18 | REQ-006, REQ-010 | Help, termination |
| S4 | T-P3-20 | REQ-086 | CI and coverage integration |

## 7. Evidence to Record
- Red names and failures, counts, the `ci-local.ps1` run directory and counters (library gate and the CL-19 result), hosted CI run, authorship (qwen: code, tests, script and runner changes; orchestrating agent: documentation), Codex rounds

## 8. Risks and Limitations
- (a) The CLI build depends on the pack step, so a plain `dotnet build WeatherParser.slnx` without a packed feed fails for the CLI projects; CONTRIBUTING.md states the order, as for the package consumer
- (b) Process tests are slower than unit tests; each sets a timeout, and the count is kept to the behaviors that need a real process (channels, exit codes, signals, files)

## 9. Acceptance Criteria
- [ ] Every listed test green with recorded Red
- [ ] The CLI builds and runs against the same-run package on Windows and in hosted CI
- [ ] `ci-local.ps1` exit 0 with the CL-19 gate result recorded; consistency-table documents updated; Codex 0 findings or reported
