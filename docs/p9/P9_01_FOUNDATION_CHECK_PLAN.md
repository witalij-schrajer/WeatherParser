# P9.01 – Foundation Check and Plan Adjustment: Implementation Plan

**Status: planned on 2026-10-31 on the user's instruction ("Generate the step plans for P9"); reviewed through the automatic Codex review of pull request #17 to 0 findings; accepted by the user on 2026-10-31 ("Accept the plans"); accepting the plans neither starts P9 nor approves any RL row. P9 started on 2026-10-31 on the user's instruction ("P9 Execution"); this step is complete with outcome READY ([record](P9_01_FOUNDATION_CHECK.md)).** Refines the P9.01 row of section 5 and section 1 of [PHASE_P9_PLAN.md](../../PHASE_P9_PLAN.md).

## 1. Goal and Scope
Goal: confirm that P3 is complete and merged, fix the integration target, extract every release-relevant fact of the repository, name the causes of package differences between two clean builds, remeasure both coverage gates and adjust the P9 plans before any decision or change.
In scope: the records recheck, the commit classification since `2ba5502`, the extraction table, the two-pack comparison, the servicing and latest-version check of the pinned SDK and packages, the phase branch, the draft pull request, the status passages and the baseline.
Out of scope with owner: every decision (RL rows) → P9.02; any change of build settings, pins or code → P9.03 to P9.06.

## 2. Preconditions and Inputs
- The user's instruction to start P9; this plan and the P9 phase and step plans accepted by the user.
- The P3.12 acceptance record (A11: pull request #16 merged as `2ba5502`), the [P9 handoff](../p3/P9_HANDOFF.md), the P8.10 and P3.12 limitations (section 4 of each).
- Inputs: REQ-004, REQ-005, REQ-013, REQ-017, REQ-086 with its amendment; CONTRIBUTING.md sections 1 to 7.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Integration target | `2ba5502` / the `develop` head at branch creation | The `develop` head at branch creation, with every commit since `2ba5502` classified; a production-code change since then is reported to the user before P9.02 | Orchestrator, recorded | S1 |
| Pack comparison method | Byte comparison only / byte comparison and member-by-member comparison | Both: the byte comparison decides reproducibility; the member comparison (each zip member's name, size, SHA-256, timestamp and the `.nuspec` text) names every cause | Orchestrator, recorded | S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p9/P9_01_FOUNDATION_CHECK.md` | Create | Records recheck, commit classification, extraction table, pack comparison, servicing check, baseline, item mapping, outcome READY or NOT_READY |
| `PHASE_P9_PLAN.md` section 1 | Change | Table "P9.01 extraction"; adjustments where the extraction contradicts the plan |
| `PHASE_STATUS.md`, `AGENTS.md`, `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md` | Change | P9 started (the user's instruction quoted), branch, integration target |
| `docs/p9/P9_VERIFICATION.md` | Create | The verification record with the P9.01 section |

## 5. Procedure
Documentation only: written by the orchestrating agent (AGENTS.md, user decision dated 2026-10-24); no qwen brief, no production code.

### S1 Records recheck and commit classification
Check A11 of the P3.12 record against `git log` and `gh pr view 16`; list every commit of `develop` since `2ba5502` with its classification (documentation, CI configuration, tests, production code).

### S2 Branch, seed commit, draft pull request and status passages
Create `features/develop/P9` from the integration target; the seed commit carries the status passages and the empty P9.01 record skeleton; push; open the draft pull request into `develop` directly afterwards (AGENTS.md "Phase pull requests"); turn on Auto-fix.

### S3 Extraction and pack comparison
Extract the package metadata of `src/WeatherParser.Parsing/WeatherParser.Parsing.csproj`, the `Directory.Build.props` settings, the package contents, the public API surface (a listing of the packed assembly's public types and members, input to RL-04), the pins of `global.json`, `Directory.Packages.props` and `examples/Directory.Packages.props`, the branches and workflows. Pack the integration target twice from two clean clones (Windows, and Linux in the hosted CI job or on the NanoPi R76S if the user authorizes it) and compare byte for byte and member by member; name every difference and its cause (input to RL-03).

### S4 Servicing and latest-version check
From the official .NET release metadata and the package feeds: the latest servicing SDK and runtime of 10.0 with their security status, the latest stable versions of `Microsoft.Extensions.*` and MQTTnet, recorded with URL and date (input to RL-09, RL-10; the pins stay).

### S5 Baseline
Run `pwsh ./scripts/ci-local.ps1 -Commit <seed-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`; record the native block counters of `WeatherParser.Parsing.dll` and `WeatherParser.dll` and every uncovered block.

### S6 Item mapping, plan adjustment, record and review
Map every open item of section 1 of the phase plan to a P9 step or a carried-forward owner; add the "P9.01 extraction" table; write the outcome READY (no unmapped item, baseline measured, no unexplained production change) or NOT_READY with the reason; per-step Codex review read-only in the five-angle format; commit and push.

## 6. Tests and Test IDs
| Step-local case | T-P9 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S3 | T-P9-03 | REQ-005 | Two clean packs compared; differences named |
| S5 | T-P9-16 | REQ-086 | Baseline of both gates measured on the integration target |

No new automated tests: P9.01 changes no code.

## 7. Evidence to Record
- Commit classification, extraction values, both package hashes and every named difference, the servicing facts with URLs and dates, the `ci-local.ps1` run (exit code, run directory, counters), the pull request number, the Codex round

## 8. Risks and Limitations
- (a) The Linux pack comparison needs a Linux environment: the hosted CI job (a container, not target evidence) suffices for the cause analysis; the NanoPi run needs the user's authorization
- (b) A production-code change since `2ba5502` would change the contracts' foundation; it stops the step until the user decides

## 9. Acceptance Criteria
- [ ] Records recheck and commit classification recorded
- [ ] Draft pull request open on `features/develop/P9`; status passages updated
- [ ] Extraction table, pack comparison with named causes, servicing facts and baseline recorded
- [ ] Every open item mapped; outcome READY recorded; Codex 0 findings or reported
