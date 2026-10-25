# P6.01 – Foundation Check and Plan Adjustment: Implementation Plan

**Status: plan only (2026-10-25); nothing is executed yet.** Refines section 1 and the P6.01 row of section 5 of [PHASE_P6_PLAN.md](../../PHASE_P6_PLAN.md).

## 1. Goal and Scope
Goal: confirm that P5 is accepted and merged, extract the accepted foundation, map every inherited limitation to a P6 step or a carried-forward owner, remeasure the native block-coverage baseline of the integration target on the first committed revision of the P6 branch, and adjust `PHASE_P6_PLAN.md` where the extraction contradicts it.
In scope: records recheck; extraction table; inherited-limitation mapping; SDK servicing check; P6 branch, "P6 started" status passages and draft pull request (on the user's instruction); baseline run with `ci-local.ps1`; plan reconciliation; record `docs/p6/P6_01_FOUNDATION_CHECK.md` and the P6.01 section of `docs/p6/P6_VERIFICATION.md`; outcome READY or NOT_READY.
Out of scope with owner: source facts, the HTML parser package and the OsthessenWetter contract → P6.02; production code and tests → P6.03 onwards; CLI → P3.

## 2. Preconditions and Inputs
- P5 accepted by the user on 2026-10-25 on candidate `fd461c8`; pull request #7 merged into `develop` on 2026-10-25 (merge commit `29fd5533449898f1b14513b11c65a0afa1cf049a`). The post-merge documentation pull request #8 carries the P5 post-merge records and the P6 plans; its merge state is checked here.
- The phase plan's planning gate (PHASE_P6_PLAN.md section 5) is satisfied: all eleven step plans are written and Codex-reviewed.
- Inputs: `docs/p5/P5_11_ACCEPTANCE.md` (sections 4, 6 and 8), `docs/p5/P6_HANDOFF.md`, `docs/p5/P5_VERIFICATION.md`, `docs/p2/P2_EXPORT_CONTRACT.md`, `docs/p4/P4_SELECTION_CONTRACT.md`, `docs/p5/P5_OBSERVATION_CONTRACT.md`, `global.json`, `DEPENDENCIES.md`, `docs/COMPATIBILITY.md`, official .NET release metadata (URL and check date recorded).
- Required user instruction: creating the branch `features/develop/P6`, committing, pushing and opening the draft pull request.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Integration target | `29fd553` (P5 merge) / the `develop` head at branch creation after pull request #8 | The `develop` head at branch creation (`git rev-parse origin/develop`); it must be `29fd553` or a documentation-only successor; any production change since `29fd553` is reported to the user before P6.02 | Orchestrator, recorded | S3, S4 |
| Baseline expectation | 7491/7494 (P5 end state) / remeasured value | The remeasured value of S4 is binding; a deviation is recorded and explained | Orchestrator, recorded | S4 |
| Inherited limitations | Map each to a P6 step and check / carry forward with owner | Mapping as in S2; any item without a mapping blocks READY | Orchestrator, recorded | S2 |
| Plan adjustments | Adjust `PHASE_P6_PLAN.md` where the extraction contradicts it / defer to P6.02 | Adjust in S5; contract-level changes stay with P6.02 | Orchestrator, recorded; contract changes User (P6.02) | S5 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p6/P6_01_FOUNDATION_CHECK.md` | Create | Records recheck, extraction table, limitation mapping, SDK check, baseline run, adjustments, outcome |
| `docs/p6/P6_VERIFICATION.md` | Create | P6 verification record with the P6.01 section |
| `PHASE_P6_PLAN.md` | Change | Section 1 extraction table and adjustments; status line |
| `README.md`, `AGENTS.md`, `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md` | Change | Phase-status passages "P6 started on <date> on the user's instruction" (README needs the package check of CONTRIBUTING.md section 4) |

## 5. Procedure
No slice changes behavior, so there is no Red step. The orchestrating agent writes every record and status text and runs every command; Codex reviews in S5.

### S1 Records recheck
- Confirm acceptance and merge; `git merge-base --is-ancestor 29fd553 origin/develop`; `git log --oneline 29fd553..origin/develop`, each commit classified as documentation-only or not; pull request #8 state recorded.

### S2 Extraction, limitation mapping and SDK check
- Extraction table: integration target, package identity, SDK and runtime versions, coverage tools, export contract version and decision range, selection and observation contract ranges, helpers P6 reuses (P6 handoff sections 2 to 4), P5 coverage at acceptance.
- Limitation mapping of every open item in `P5_11_ACCEPTANCE.md` section 4 and `P6_HANDOFF.md` section 7, at least:

| Inherited item | P6 disposition | Check |
|---|---|---|
| Builder and root-validation duplication | Guarded again in P6 | P6.08 equivalence tests through the real serializers |
| Coarse XML cancellation (C-10) | Carried forward, owner as recorded in P5 | Restated in the P6.11 limitations |
| Hosted `debian13-arm64` container is not target evidence | Kept separate | P6.10 records NanoPi evidence apart from hosted CI |
| Package not byte-reproducible | Carried forward, owner P9 | Restated in the P6.11 limitations |
| DI under load | Carried forward, owners P8 and P3 | Restated in the P6.11 limitations |
| OsthessenWetter source conditions (REQ-092) | Clarified as far as possible in P6 | P6.02 (facts, OW-14), P6.09 (README), P6.11 (status) |
| Every other open P5 item | Its recorded owner | Restated in the P6.11 limitations |

- SDK servicing status against the official .NET release metadata.

### S3 Branch, status passages, seed commit, push and draft pull request
- Branch `features/develop/P6` from `origin/develop`; README package check; commit 1 = the "P6 started" passages and the P6.01 record draft; push; `gh pr create --draft --base develop`; Auto-fix on; pull-request number and CI run recorded.

### S4 Baseline run on the committed revision
- `pwsh ./scripts/ci-local.ps1 -Commit <commit-1-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`; equal coverage passes for the documentation-only candidate; both block counters, the gate result and its SHA-256, the package hash and the test counts recorded. The counters are the comparison value for the P6 code steps only while the pull-request base and `coverage.runsettings` are unchanged; both are checked before every reuse.

### S5 Plan reconciliation, review and record commit
- `PHASE_P6_PLAN.md` section 1 "P6.01 extraction" table and adjustments; Codex five-angle review of the uncommitted changes; confirmed findings fixed by the orchestrating agent; checkpoint commit when the user requests it; push; CI run recorded.

## 6. Tests and Test IDs
| Step-local case | T-P6 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S4 | T-P6-16 (baseline) | REQ-086 | Native block counters of the integration target remeasured on the committed revision; equal coverage accepted for the documentation-only candidate; unrounded counters |

## 7. Evidence to Record
- Commits since `29fd553` with classification; integration target SHA; extraction table with sources; limitation mapping
- SDK servicing check (URL, date, result)
- Branch, commit SHAs, pull-request number, CI run ID
- `ci-local.ps1` command, exit code, run directory, counters, gate result and SHA-256, package hash
- Plan adjustments; Codex rounds and dispositions; authorship

## 8. Risks and Limitations
- (a) `develop` moved beyond `29fd553` with production changes — reported; P6.02 waits for the user's decision
- (b) A newer SDK servicing release — recorded; an update is a separate user decision
- (c) Baseline differs from 7491/7494 — the remeasured value is binding and the difference explained
- (d) Pull request #8 not merged — the branch is created from the merged state only

## 9. Acceptance Criteria
- [ ] P5 acceptance and merge confirmed; every commit since `29fd553` classified
- [ ] Extraction table complete with sources
- [ ] Every inherited limitation mapped to a P6 step and check or carried forward with its owner
- [ ] SDK servicing status recorded
- [ ] Branch, seed commit, push and draft pull request done on the user's instruction; CI run recorded
- [ ] Baseline run on the committed revision passed; counters recorded
- [ ] `PHASE_P6_PLAN.md` section 1 updated; status passages consistent (AGENTS.md table)
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability reported to the user
- [ ] Outcome recorded (READY or NOT_READY with open items); no production code or test changed
