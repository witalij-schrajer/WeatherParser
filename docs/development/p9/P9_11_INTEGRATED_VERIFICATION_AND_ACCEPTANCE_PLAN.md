# P9.11 – Integrated Verification, Acceptance and Handoff: Implementation Plan

**Status: planned on 2026-10-31 on the user's instruction ("Generate the step plans for P9"); reviewed through the automatic Codex review of pull request #17 to 0 findings; accepted by the user on 2026-10-31 ("Accept the plans"); accepting the plans neither starts P9 nor approves any RL row; P9 starts only on a separate instruction.** Refines the P9.11 row of section 5 of [PHASE_P9_PLAN.md](../PHASE_P9_PLAN.md).

## 1. Goal and Scope
Goal: the integrated verification of the release candidate, the evidence audit, the review loop of the phase pull request to 0 findings, the user's acceptance of P9 on the candidate and the merge into `develop` by the user.
In scope: `ci-local.ps1` on the candidate on Windows and the NanoPi R76S, hosted CI; the evidence matrix T-P9-01 to T-P9-16; limitations with owners; the acceptance record; status documents; the release handoff for P9.12.
Out of scope with owner: the release pull request into `master`, tag, GitHub release and publication → P9.12.

## 2. Preconditions and Inputs
- P9.10 READY on the candidate.
- The user's authorization of the NanoPi gate run, recorded verbatim.
- Inputs: every P9 record; REQ-084 to REQ-086; AGENTS.md "Phase implementation" step 2.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Candidate after review fixes | Keep the P9.10 candidate / new candidate | A review fix that changes a build input (RL-06) makes a new candidate: P9.10 and this step's S1 are repeated on it unless the user decides otherwise, recorded verbatim; documentation-only fixes outside the build inputs keep the candidate | User for a skipped repeat; orchestrator otherwise | S3 |
| Acceptance wording | Accept / accept with named open items / reject | Presented with every open item and limitation; the user's answer recorded verbatim | User | S4 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p9/P9_11_INTEGRATED_VERIFICATION.md` | Create | Gate runs per platform, CI runs, outcome |
| `docs/p9/P9_11_ACCEPTANCE.md` | Create | Evidence matrix, limitations with owners, audit decision, review rounds, the acceptance verbatim, merge |
| `PHASE_P9_PLAN.md`, `PHASE_STATUS.md`, `AGENTS.md`, `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md` | Change | Status |

## 5. Procedure
Documentation by the orchestrating agent; review fixes to code through qwen test-first.

### S1 Integrated verification
`pwsh ./scripts/ci-local.ps1 -Commit <candidate-sha> -TargetRef <develop-sha> -RunSettings coverage.runsettings` on Windows and, authorized, on the NanoPi; hosted CI that executed on the exact candidate: the automatic run of the candidate push counts only if its three jobs (Windows x64, Ubuntu x64, Debian 13 ARM64) ran; when they were skipped because the push changed documentation only (CONTRIBUTING.md section 6), a manual run of `ci.yml` (`workflow_dispatch` with `target_ref` set to the recorded `develop` commit) is started while the candidate is still the head of the phase branch, because a manual run tests the branch head, and no further commit is pushed before that run has started; the run's head SHA must equal the candidate and all three jobs must have run and passed; if the branch head has already moved past the candidate without an executed run, P9.10 selects a new candidate (section 3). Marking the pull request ready later tests the newer head, not the candidate, and does not count; both gates per the rule in force; outcome READY or NOT_READY.

### S2 Evidence audit
Map every T-P9 row to its tests and records; list limitations with owners (the open items of P3.12 and P8.10 carried or closed); check status consistency (AGENTS.md table); audit decision.

### S3 Review loop
Mark the pull request ready for review; AGENTS.md "Plan generation" step 3 loop to 0 findings (draft while fixing, every finding verified, rounds recorded in the acceptance record, the recording commit reviewed once more); up to the tenth round, then the user decides how to continue.

### S4 Acceptance, status and merge
Ask the user for the acceptance with the open items; record the answer; update the status documents; the user merges the pull request into `develop`; record the merge commit on a post-merge documentation branch.

## 6. Tests and Test IDs
| Step-local case | T-P9 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P9-07, T-P9-16 | REQ-084, REQ-086 | Gates on every platform |
| S2 | all T-P9 rows | REQ-091 | Every row has passing tests or recorded evidence |
| S2 | T-P9-15 | REQ-090, REQ-093 | Documentation and status consistent |

## 7. Evidence to Record
- Run directories, exit codes, counters, the hosted CI run ID with its event (push or manual), head SHA, comparison target and the outcome of each of the three jobs, audit table, review rounds, acceptance verbatim, merge commit

## 8. Risks and Limitations
- (a) Review fixes after P9.10 can invalidate the target evidence for the candidate (section 3)

## 9. Acceptance Criteria
- [ ] Integrated verification READY on the candidate on Windows, hosted CI (an executed run whose head SHA is the candidate, all three jobs passed) and the NanoPi
- [ ] Review loop at 0 findings; acceptance recorded verbatim; status documents consistent
- [ ] Pull request merged into `develop` by the user and recorded

## 10. Reconciliation with the approved contract

The release contract [P9_RELEASE_CONTRACT.md](../../p9/P9_RELEASE_CONTRACT.md) was approved by the user on 2026-10-31 ("Approve all"; RL-19 open). Where this plan and the contract differ, the contract wins. Changes for this step:
- Both gates are evaluated under the amended REQ-086 with the RL-20 entries for the target `ed7fc7a`; if `develop` moves and the pull request's target changes, the entries no longer apply and a new entry needs the user's approval.
