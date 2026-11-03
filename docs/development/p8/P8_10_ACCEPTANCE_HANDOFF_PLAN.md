# P8.10 – Acceptance of P8 and of the Complete Library, P3 Handoff: Implementation Plan

**Status: planned on 2026-10-28; accepted by the user on 2026-10-28 with the phase plan; complete on 2026-10-29: audit ACCEPTED, P8 accepted by the user on candidate `ce166eb` ([record](../../p8/P8_10_ACCEPTANCE.md)); pull request #14 merged into `develop` on 2026-10-29 (merge commit `21b7210`).** Refines the P8.10 row of section 5 and section 9 of [PHASE_P8_PLAN.md](../PHASE_P8_PLAN.md).

## 1. Goal and Scope
Goal: audit the P8 evidence, summarize the acceptance of the complete library across P2 and P4 to P8 as the gate for P3, obtain the user's acceptance, update the status documents, write the P3 handoff and bring the pull request through review to the user's merge.
In scope: `docs/p8/P8_10_ACCEPTANCE.md` with A1 review identity, A2 evidence matrix T-P8-01 to T-P8-17, A3 contract summary (MQ-01 to MQ-15), A4 limitations and open items with owners, A5 audit decision, a library acceptance summary (one row per library capability with the accepting phase, acceptance record and candidate), A7 user decision, A8 status updates, A9 to A11 ready for review, review fixes and merge; `docs/p8/P3_HANDOFF.md`; status passages.
Out of scope with owner: P3 itself; publication → P9.

## 2. Preconditions and Inputs
- P8.09 READY.
- Inputs: `docs/p8/P8_VERIFICATION.md`, `docs/p8/P8_09_INTEGRATED_VERIFICATION.md`, the acceptance records of P2 and P4 to P7, the P3 responsibilities of DEVELOPMENT_PLAN.md and REQ-060 to REQ-073.
- Required user decisions: acceptance of P8 and of the complete library; the instruction to mark the pull request ready for review (which also starts the automatic Codex review); the merge.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Library acceptance form | One user acceptance covering P8 and the complete library / separate acceptances | One acceptance with two recorded parts, so P3 can start on a single instruction | User | A7 |
| Open OW-14 | Carried with owner the user / closed | Carried unless the user records the clarification; the P3 handoff forbids publishing OsthessenWetter values until then | User | A4 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p8/P8_10_ACCEPTANCE.md` | Create | Acceptance record |
| `docs/p8/P3_HANDOFF.md` | Create | Foundation, what P8 provides, P3 responsibilities (CLI, configuration, transport with QoS-1 acknowledgment, TLS, credentials, exit codes, partial failures, the five commands), reuse obligations, working rules, open items |
| `README.md`, `AGENTS.md`, `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md`, `PHASE_P8_PLAN.md` | Change | Status passages at acceptance and after the merge |

## 5. Procedure
### S1 Evidence audit (A1 to A5)
- Every T-P8 row traced to tests and records; every limitation with an owner; audit ACCEPTED or NOT_ACCEPTED; Codex five-angle review until 0 findings.

### S2 User acceptance and status (A7, A8)
- The user's wording and date recorded verbatim; status passages switched to "accepted; complete only once merged"; package check; `ci-local.ps1` on the committed revision.

### S3 Ready for review, review fixes, merge (A9 to A11)
- On the user's instruction the pull request is marked ready for review; the automatic Codex review and any user review findings are verified and fixed (code test-first through qwen, documentation by the orchestrating agent); a code fix makes a new candidate and repeats P8.09 S1 to S3; the user merges; the post-merge records follow in a separate documentation pull request, as after P4 to P7.

## 6. Tests and Test IDs
| Step-local case | T-P8 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P8-01 to T-P8-17 | all P8 rows | Evidence audit |

## 7. Evidence to Record
- Audit decision, user wording and date, review rounds and fixes, merge commit

## 8. Risks and Limitations
- (a) Review fixes after acceptance can require a new candidate and a new NanoPi authorization

## 9. Acceptance Criteria
- [x] Evidence audit ACCEPTED; user acceptance of P8 and of the complete library recorded
- [x] Status documents consistent; P3 handoff written
- [x] Pull request reviewed, findings resolved, merged by the user
