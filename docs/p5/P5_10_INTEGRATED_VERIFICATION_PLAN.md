# P5.10 – Integrated Coverage and Platform Verification: Implementation Plan

**Status: plan only (2026-10-24); nothing is implemented or verified yet.** Refines the P5.10 row of section 5 and section 7 of [PHASE_P5_PLAN.md](../../PHASE_P5_PLAN.md).

## 1. Goal and Scope
Goal: verify the committed P5 implementation candidate as a whole on Windows, in hosted CI and on the NanoPi R76S, optionally run one explicitly enabled live parse of one real station, audit every T-P5 row against tests and recorded evidence, and declare READY or NOT_READY for P5.11.
In scope: S0 conditional extension of the live-check runner (OC-13) by one DWD observation parse within a request budget of eight (four lists, at most four archives), test-first and committed before the live run; S1 Windows `ci-local.ps1` on the candidate; S2 hosted CI run of the candidate with artifact verification (SHA pair in `commits.json`, C-116 package evidence per job); S3 NanoPi R76S run with the user's recorded authorization; S4 optional live parse (one run, recorded separately, not repeated: SC-16 pattern); S5 T-P5 audit, defects with owner and disposition, outcome, Codex review of the records, checkpoint commit on request.
Out of scope with owner: acceptance decision and status updates → P5.11; fixes → the owning step (a fix restarts S1 to S4 on the new candidate; another live execution is skipped and its disposition recorded, SC-16 pattern); CLI → P3.

## 2. Preconditions and Inputs
- P5.03 to P5.09 complete and committed; the candidate SHA and the integration-target SHA (PR base) known; OC-13 approved.
- User authorization for the NanoPi run and the user's enabling of the live parse (both recorded with date and wording).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Live parse | enabled (one station, one run) / skipped | Per OC-13; if enabled: the P4 live-check station area (Fulda) with one CDC station chosen from the P4 live result, request budget 8, no retries, SC-09 bounds | User | S0, S4 |
| Live-runner extension | extend `LiveCheckRunner` with a DWD observation parse sequence behind the same opt-in / a second runner | Extend the existing runner with a Proposed observation-only mode: a second entry (`LiveCheckRunner.RunObservationAsync`, Proposed) with its own station ID and a budget of 8, invoked by a separate live test behind the same opt-in plus a mode variable (`WEATHERPARSER_LIVE_CHECK_MODE=observation`, Proposed); the P4 path makes no observation request and the observation path no MOSMIX or selection request; `LiveCheckResult` and the summary gain the observation documents and the dataset times | Orchestrator, recorded | S0 |
| NanoPi authorization | granted / not granted | Without it the outcome is NOT_READY while T-P5-17 evidence is missing; a user acknowledgment never counts as a pass (REQ-084) | User | S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `tests/WeatherParser.Parsing.Tests/Live/LiveCheckRunner.cs`, `LiveCheckSettings.cs`, `LiveCheckResult.cs`, `LiveCheckTests.cs`, `LiveCheckRunnerTests.cs`, `LiveCheckRunnerFailureTests.cs` | Change | Observation parse sequence, budget 8, summary fields (station, products requested, 404s, dataset times) with offline tests for the sequence, budget refusal and failure propagation |
| `docs/p5/P5_10_INTEGRATED_VERIFICATION.md` | Create | Decisions, S0 record, candidate, S1 to S4 runs, T-P5 audit, defects, outcome, authorship |
| `docs/COMPATIBILITY.md` | Change | P5 platform summary (Windows, hosted, NanoPi, live) |
| `docs/p5/P5_VERIFICATION.md` | Change | P5.10 section |

## 5. Procedure
### S0 Live-check runner extension (conditional)
**Red**
- `RunIfEnabledAsync_ObservationSequence_RequestsListsThenArchives`
- `RunIfEnabledAsync_ObservationBudgetEight_RefusesNinthRequest`
- `RunIfEnabledAsync_ObservationMissingProduct_RecordedInSummary`
- `RunIfEnabledAsync_ObservationFailure_PropagatesWithoutRetry`
- `RunIfEnabledAsync_NotEnabled_MakesNoRequest` (regression)
- `RunObservationAsync_MakesNoMosmixOrSelectionRequest`
- `RunAsync_P4Path_MakesNoObservationRequest`
- `RunObservationAsync_WritesObservationJsonXmlAndSummary` (to a temporary directory outside the repository); the P4 runner tests unchanged.
Expected failure: the observation-sequence tests fail with their observable assertions (no observation mode, budget 56, no observation summary fields); `RunIfEnabledAsync_NotEnabled_MakesNoRequest` and the P4 runner tests pass on first run and are recorded as existing behavior; the actual failure of each new case is recorded, not prescribed.

**Green**
Minimal extension of the runner and settings; test code only.

**Refactor**
Remove duplication with all tests green.

**Roles**
qwen generates the tests and the extension; the orchestrating agent reviews, runs build, tests and coverage (test code is excluded from the block metric), Codex reviews the diff; checkpoint commit when the user requests it (the live run needs the committed runner).

**Check**
`dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`; `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build` (live test skipped).

### S1 Candidate and Windows run
**Red**
No Red: verification run.

**Roles**
The orchestrating agent records the candidate SHA, the pull request number and the integration-target SHA (`gh pr view` base and `git rev-parse origin/develop`), runs the command, extracts every value and writes the record prose from them.

**Check**
`pwsh ./scripts/ci-local.ps1 -Commit <candidate-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`; record exit code, run directory, test counts per project, native covered/total blocks for candidate and target, gate rules, gate-result SHA-256, package identity, packed and restored SHA-512, entries and consumer-lock comparison (C-116).

### S2 Hosted CI
**Red**
No Red: verification run.

**Roles**
As in S1.

**Check**
Record the run ID of the candidate push and each job's outcome; download the artifacts; verify that every `commits.json` names the candidate and the integration target (a run on another base needs a matching rerun); record the C-116 package evidence per job; a failing job or mismatching SHA pair is a defect (S5).

### S3 NanoPi R76S
**Red**
No Red: verification run.

**Roles**
As in S1; the user's authorization is recorded with date and wording.

**Check**
CONTRIBUTING.md section 7 for the candidate and the integration target (bundle with SHA-256 compared on both sides, `prepare-debian-arm64.sh`, `ci-local.ps1`); copy the run directory back; compare with Windows (test counts, block counters, gate result); the device's working directory removed afterwards; without authorization record a named blocker and the outcome stays NOT_READY while T-P5-17 evidence is missing.

### S4 Optional live parse
**Red**
No Red: verification run.

**Roles**
As in S1; the orchestrating agent writes the live record from the extracted values; Codex reviews it read-only.

**Check**
Only if enabled by the user and REQ-092 conditions are recorded (P5.02); run the committed runner's observation entry once, with `WEATHERPARSER_LIVE_CHECK=1` and the observation mode variable, filtered to the observation live test, on the committed candidate (build 0 warnings); record date, endpoints by name, request count against the budget of 8, HTTP statuses, declared lengths and bytes read, 404 products, the dataset's `retrieved_at`, per-quantity `measured_at` and the exported quantities, XML validation against the v1 schema, JSON parsing and the JSON ↔ XML equivalence check of P5.08 (no JSON schema exists), and any format deviation versus the contract facts (a deviation is a finding for P5.02, not a silent adaptation); one run, not repeated (SC-16 pattern).

### S5 T-P5 audit, defects and outcome
**Red**
No Red: audit.

**Roles**
The orchestrating agent maps every T-P5-01 to T-P5-19 row to its tests and evidence, lists defects with owner and disposition, records the outcome and writes the record prose; Codex reviews the uncommitted records read-only; the orchestrating agent fixes confirmed findings; checkpoint commit when the user requests it; push; hosted CI run of the checkpoint recorded.

**Check**
Outcome READY only when every mandatory row passes on Windows and the NanoPi (or an explicitly approved, recorded amendment of the phase acceptance criteria changes what READY requires, with missing evidence listed as open and never reported as passed), the coverage gate passed, hosted CI is green and no defect is open; otherwise NOT_READY with the missing items; a defect is fixed test-first by qwen in the owning step and S1 to S4 restart on the new candidate (another live execution skipped, disposition recorded).

## 6. Tests and Test IDs
| Step-local case | T-P5 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P5-16 | REQ-086 | Native block coverage ≥ 80 % and strict improvement against the remeasured integration target; unrounded counters; comparable settings |
| S1, S3 | T-P5-13 (package evidence) | REQ-013 | Packed and restored hashes, entries and consumer-lock comparison from committed runs (C-116) |
| S3 | T-P5-17 | REQ-017, REQ-084 | All P5 tests and the gate on the NanoPi R76S, separate from Windows |
| S0, S4 | T-P5-18 | REQ-083, REQ-092 | Explicitly enabled live parse, separate from standard tests, within the recorded source conditions and the budget |
| S5 | T-P5-01 to T-P5-19 | REQ-085 and all P5 rows | Audit of every row with evidence |

## 7. Evidence to Record
- Every command with exit code, run directories, run IDs, SHAs, counters, gate results and hashes
- NanoPi authorization wording and date; live-parse enabling wording and date; live data (if run)
- T-P5 audit table; defects and dispositions; Codex review rounds; authorship (orchestrating agent: runs, extraction and record text)

## 8. Risks and Limitations
- (a) The integration target moves during verification — S1 uses the PR base at run time and records it; a later base change requires a rerun
- (b) The live source changed since P5.02 — recorded as a finding for P5.02; the owning step decides
- (c) Hosted `debian13-arm64` is a container, not target evidence — kept separate from the NanoPi
- (d) Missing NanoPi authorization — named blocker, no claim of ARM64 success
- (e) A defect after the live run — offline and platform verification restart; the live parse is not repeated (SC-16 pattern)

## 9. Acceptance Criteria
- [ ] Candidate and integration target recorded
- [ ] Windows gate passed with recorded counters
- [ ] Hosted CI green for the candidate with the verified SHA pair and recorded C-116 evidence
- [ ] NanoPi run recorded with authorization; without it NOT_READY
- [ ] Live parse run within conditions or recorded as skipped with its disposition
- [ ] Every T-P5 row audited; no open defect; outcome recorded
- [ ] Codex review of the records with 0 open findings, or a recorded Codex unavailability or failure reported to the user
- [ ] No production code changed in this step (S0 is test code only)
