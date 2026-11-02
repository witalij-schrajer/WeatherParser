# P3.12 – Acceptance of P3 and P9 Handoff: Implementation Plan

**Status: planned on 2026-10-29; accepted by the user on 2026-10-29 with the phase plan ("Accept the plans"); reconciled on 2026-10-29 with the [P3 CLI contract](../../p3/P3_CLI_CONTRACT.md) approved by the user on 2026-10-29 ("Approve all"), section 10 of this plan; where this plan's earlier text differs, section 10 and the contract win.** Refines the P3.12 row of section 5 and section 9 of [PHASE_P3_PLAN.md](../PHASE_P3_PLAN.md).

## 1. Goal and Scope
Goal: an evidence audit of P3, the review loop of the phase pull request to 0 findings, the user's acceptance of P3, the status updates and the P9 handoff.
In scope: the acceptance record `docs/p3/P3_12_ACCEPTANCE.md` (review identity, evidence matrix T-P3-01 to T-P3-20, contract summary, limitations with owners, audit decision, user decision, ready for review, review and merge); the P9 handoff `docs/p3/P9_HANDOFF.md`; status passages; the review loop (AGENTS.md "Phase implementation" step 3); the merge by the user and the post-merge records.
Out of scope with owner: release, publication and deployment → P9.

## 2. Preconditions and Inputs
- P3.11 READY.
- Inputs: the P3 verification record, the P3.11 record, the approved contract, the acceptance records of P2 and P4 to P8, the limitations carried into P3.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Acceptance | Accept / accept with named limitations / reject | The user decides after the 0-finding review round; the wording is recorded verbatim | User | S3 |
| Open authorizations | Accept with S4 or S5 open / require them | The record lists every open S4 or S5 item; acceptance with an open item needs the user's explicit wording for that item | User | S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p3/P3_12_ACCEPTANCE.md` | Create | Sections A1 to A11 as in P8.10 |
| `docs/p3/P9_HANDOFF.md` | Create | Foundation, what the CLI and the library provide, P9 responsibilities (release artifacts and checksums, package ID and feed, deployment model, ARM64 release acceptance, operating guides, source conditions), reuse obligations, open items |
| `README.md`, `AGENTS.md`, `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md`, `PHASE_P3_PLAN.md`, the P3 step plans | Change | Status passages; checklists |
| `docs/p3/P3_VERIFICATION.md` | Change | P3.12 section |

## 5. Procedure
### S1 Audit and drafts
- Evidence matrix: every T-P3 row with its tests, steps and run evidence; limitations with owners (at least: OW-14 with the user; broker behavior beyond the authorized test broker; DI under load beyond one command per process, P9; the inherited library limitations); audit decision ACCEPTED or NOT_ACCEPTED, which is not the user's acceptance; the P9 handoff draft; README package check if the README changes.

### S2 Review loop
- Mark the pull request ready for review; the automatic Codex review runs; draft while fixing; every finding verified and fixed (code test-first through qwen with a new candidate and a P3.11 rerun as far as affected, documentation by the orchestrating agent) or rejected with the reason; every round recorded; repeated until a round reports 0 findings; after the tenth round the remaining findings go to the user with the agent's check of each (AGENTS.md "Phase implementation" step 2).

### S3 User decision and status
- The user is asked for the acceptance only after the 0-finding round; the wording is recorded verbatim; status passages updated ("accepted; complete once merged"); package check and `ci-local.ps1` on the committed revision when the README changes; one more review round on the recording commit; the pull request stays ready for review.

### S4 Merge and post-merge
- The user merges; the merge commit, parents and tree are recorded in a post-merge documentation pull request (as after P4 to P8), status "P3 complete", the P9 handoff in effect; P9 starts only on a separate instruction.

## 6. Tests and Test IDs
| Step-local case | T-P3 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P3-01 to T-P3-20 | all P3 requirements | Evidence audit |

## 7. Evidence to Record
- The audit matrix, review rounds (head, run, finding count, dispositions), the user's acceptance wording, the merge commit, the post-merge checks

## 8. Risks and Limitations
- (a) A review fix that changes code makes a new candidate; target and live reruns then need new authorizations

## 9. Acceptance Criteria
- [ ] Audit recorded; review loop at 0 findings or the user's decision on named findings
- [ ] User acceptance recorded verbatim; status consistent; P9 handoff written
- [ ] Merged by the user; post-merge records in their own pull request

## 10. Reconciliation with the approved contract (P3.02 S5)

- The evidence matrix uses the approved CL values; the limitations include the CL-22 version conflict (SDK and `Microsoft.Extensions.*` update as a separate user decision outside P3), the MQTT 3.1.1 acknowledgment limitation (CL-13), the Windows-only reachability of the library's `DeleteTemporaryFile` block and the REQ-086 amendment of CL-19.
