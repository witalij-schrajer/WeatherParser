# P5.01 – Foundation Check and Plan Adjustment: Implementation Plan

**Status: plan only (2026-10-24); nothing is implemented or verified yet.** Refines section 1 and the P5.01 row of section 5 of [PHASE_P5_PLAN.md](../PHASE_P5_PLAN.md).

## 1. Goal and Scope
Goal: confirm that P4 is accepted and merged, extract the accepted foundation (integration target, package identity, SDK and tool versions, contracts, inherited limitations), remeasure the native block-coverage baseline of the integration target on the first committed revision of the P5 branch, and adjust `PHASE_P5_PLAN.md` where the extraction contradicts it.
In scope: records recheck; extraction table; SDK servicing check; P5 branch, "P5 started" status passages and draft pull request (on the user's instruction); baseline run with `ci-local.ps1`; plan reconciliation; record `docs/p5/P5_01_FOUNDATION_CHECK.md` and the P5.01 section of `docs/p5/P5_VERIFICATION.md`; outcome READY or NOT_READY.
Out of scope with owner: source-fact verification and the observation contract → P5.02; any production code or test → P5.03 onwards; CLI → P3.

## 2. Preconditions and Inputs
- P4 accepted by the user on 2026-10-24 and pull request #5 merged into `develop` (merge commit `95bcb1e3287cae275323cc3bf202337de1942d88`); the P4 post-merge checkpoint is pull request #6 (documentation only), whose merge state is checked here.
- Inputs: `docs/p4/P4_12_ACCEPTANCE.md` (sections 6 and 8.3), `docs/p4/P5_HANDOFF.md`, `docs/p4/P4_VERIFICATION.md`, `docs/p2/P2_EXPORT_CONTRACT.md`, `docs/p4/P4_SELECTION_CONTRACT.md`, `global.json`, `DEPENDENCIES.md`, `docs/COMPATIBILITY.md`, official .NET release metadata (URL and check date recorded).
- Required user instruction: creating the branch `features/develop/P5`, committing, pushing and opening the draft pull request.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Integration target | `95bcb1e` (P4 merge) / the `develop` head at branch creation when pull request #6 has been merged | The `develop` head at branch creation, recorded with `git rev-parse origin/develop`; it must be `95bcb1e` or a documentation-only successor; any production change since `95bcb1e` is reported to the user before P5.02 | Orchestrator, recorded | S3, S4 |
| Baseline expectation | 6811/6814 (P4 end state) / remeasured value | The remeasured value of S4 is binding; a deviation from 6811/6814 is recorded and explained | Orchestrator, recorded | S4 |
| Plan adjustments | Adjust `PHASE_P5_PLAN.md` where the extraction contradicts it / defer to P5.02 | Adjust in S5; contract-level changes stay with P5.02 | Orchestrator, recorded; contract changes User (P5.02) | S5 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p5/P5_01_FOUNDATION_CHECK.md` | Create | Records recheck, extraction table, SDK check, baseline run, adjustments, outcome |
| `docs/p5/P5_VERIFICATION.md` | Create | P5 verification record with the P5.01 section (commands, values, authorship, Codex rounds) |
| `PHASE_P5_PLAN.md` | Change | Section 1 extraction table and adjustments; status line |
| `README.md`, `AGENTS.md`, `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md` | Change | Phase-status passages: "P5 started on <date> on the user's instruction (linking `docs/p4/P5_HANDOFF.md`)" (README needs the package check of CONTRIBUTING.md section 4) |

## 5. Procedure
### S1 Records recheck
**Red**
- No Red: no behavior change.

**Roles**
- the orchestrating agent writes the record text, confirms acceptance and merge, checks `git merge-base --is-ancestor 95bcb1e origin/develop`, lists `git log --oneline 95bcb1e..origin/develop` and classifies each commit (documentation-only or not).

**Check**
- read `docs/p4/P4_12_ACCEPTANCE.md` sections 6 and 8.3, `docs/p4/P5_HANDOFF.md`, `docs/p4/P4_VERIFICATION.md` section 12 and the `PHASE_P4_PLAN.md` status; confirm the pull request #6 state; record every commit since `95bcb1e`.

### S2 Extraction and SDK check
**Red**
- No Red: no behavior change.

**Roles**
- the orchestrating agent writes the extraction table and fills every value from the named sources and checks the SDK servicing status against the official .NET release metadata (URL and date recorded).

**Check**
- fields integration target, package identity, SDK and runtime versions, coverage tools, export contract version and decision range, selection contract range, helpers P5 reuses (P5 handoff sections 2 to 4), P4 coverage at acceptance, inherited limitations with owners.

### S3 Branch, status passages, seed commit, push and draft pull request
**Red**
- No Red: no behavior change.

**Roles**
- the orchestrating agent writes the status passages, the commit message and the pull-request description and runs the README package check, `git` and `gh` commands on the user's instruction and records the results.

**Check**
- branch `features/develop/P5` from `origin/develop`; README package check (CONTRIBUTING.md section 4); commit 1 = the "P5 started" status passages and the P5.01 record draft; push; `gh pr create --draft --base develop`; Auto-fix on; the pull-request number and the `pull_request` CI run recorded.

### S4 Baseline run on the committed revision
**Red**
- No Red: no behavior change.

**Roles**
- the orchestrating agent runs the command, extracts the counters and writes the record text from them.

**Check**
- `pwsh ./scripts/ci-local.ps1 -Commit <commit-1-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`; the candidate is documentation-only, so equal block coverage passes (CONTRIBUTING.md section 5); record both block counters, the gate result and its SHA-256, the package hash and the test counts; the baseline counters are the comparison value for the P5 code steps only while the pull-request base equals the recorded integration target and the collection settings are unchanged; the orchestrating agent checks both before every reuse and remeasures the target when either changed (also in P5.10).

### S5 Plan reconciliation, review and record commit
**Red**
- No Red: no behavior change.

**Roles**
- the orchestrating agent writes the `PHASE_P5_PLAN.md` section 1 extraction table, the adjustments and the records and verifies every value; Codex reviews the uncommitted changes read-only; the orchestrating agent fixes confirmed findings; checkpoint commit when the user requests it; push; hosted CI run recorded.

**Check**
- `PHASE_P5_PLAN.md` section 1 updated; outcome READY when every field is resolved, the baseline run passed and every commit between the P4 merge `95bcb1e` and the recorded integration target is classified and no production change among them is unexplained, otherwise NOT_READY with the open items.

## 6. Tests and Test IDs
| Step-local case | T-P5 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S4 | T-P5-16 (baseline) | REQ-086 | Native block counters of the integration target remeasured on the committed revision; equal coverage accepted for the documentation-only candidate; unrounded counters recorded |

## 7. Evidence to Record
- Commits since `95bcb1e` with classification; integration target SHA; extraction table with sources
- SDK servicing check (URL, date, result)
- Branch, commit SHAs, pull request number, CI run ID
- `ci-local.ps1` command, exit code, run directory, counters, gate result and SHA-256, package hash
- Plan adjustments; Codex review rounds and dispositions; authorship

## 8. Risks and Limitations
- (a) `develop` moved beyond `95bcb1e` with production changes — recorded and reported; P5.02 does not start before the user decides
- (b) SDK servicing release newer than 10.0.402 — recorded; an update is a separate user decision (AGENTS.md)
- (c) Baseline differs from 6811/6814 — the remeasured value is binding and the difference explained
- (d) Pull request #6 not yet merged — the branch is created from the merged state only; otherwise S3 waits

## 9. Acceptance Criteria
- [ ] P4 acceptance and merge confirmed; every commit since `95bcb1e` classified
- [ ] Extraction table complete with sources
- [ ] SDK servicing status recorded
- [ ] Branch, seed commit, push and draft pull request done on the user's instruction; CI run recorded
- [ ] Baseline run on the committed revision passed; counters recorded
- [ ] `PHASE_P5_PLAN.md` section 1 updated; status passages consistent (AGENTS.md table)
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user
- [ ] Outcome recorded (READY or NOT_READY with open items)
- [ ] No production code or test changed
