# P5.11 – Acceptance and P6 Handoff: Implementation Plan

**Status: plan only (2026-10-24); nothing is implemented or verified yet.** Reconciled with the approved observation contract ([OC rows](P5_OBSERVATION_CONTRACT.md), approved 2026-10-24) on 2026-10-24: the limitations of OC-05 and OC-06 added to A4. Refines the P5.11 row of section 5 and section 9 of [PHASE_P5_PLAN.md](../../PHASE_P5_PLAN.md).

## 1. Goal and Scope
Goal: turn the READY outcome of P5.10 into an explicit, evidence-based acceptance decision by the user, hand P6 the accepted observation foundation, update the status documents consistently, and complete the phase through the pull-request review and the user's merge.
In scope: actions A1 preconditions; A2 requirement and test evidence matrix T-P5-01 to T-P5-19; A3 contract summary (OC rows, export contract unchanged or additive decisions); A4 limitations and open items with owners; A5 audit decision; A6 P6 handoff `docs/p5/P6_HANDOFF.md`; A7 user acceptance; A8 status updates; A9 ready for review; A10 review with the new-candidate rule; A11 merge by the user and the post-merge checkpoint in the home the user chooses.
Out of scope with owner: P6 work → P6 (starts only on the user's instruction); fixes → the owning step; publication → P9.

## 2. Preconditions and Inputs
- P5.10 outcome READY (or NOT_READY with approved, recorded amendments); candidate and integration-target SHAs; `docs/p5/P5_VERIFICATION.md` and `docs/p5/P5_OBSERVATION_CONTRACT.md` reference the final candidate.
- Required user decisions: acceptance (A7), the home of the post-merge records (A11), the merge.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Acceptance | accept P5 / accept with recorded dispositions / do not accept | Only the user decides; the audit decision is not the acceptance | User | A7, A8 |
| Ready-for-review timing | after the A8 status commit is pushed / earlier | After A8, so the reviewed head contains the acceptance record and consistent status documents (the user may do it earlier; then the record states it) | Orchestrator, recorded | A9 |
| Home of the post-merge records | first commit of the P6 branch / a separate documentation pull request / before the merge as far as known | Ask the user at A11 | User | A11 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p5/P5_11_ACCEPTANCE.md` | Create | Review identity, evidence matrix, contract summary, limitations, audit decision, user decision, status-update record, review and merge record, authorship |
| `docs/p5/P6_HANDOFF.md` | Create | Accepted foundation, what P5 provides, P6 responsibilities, reuse obligations (`ISourceTimeNormalizer.InferDate`, `IObservationDatasetBuilder`, the real serializers, the parser pattern of P5.07), later phases, working rules, open items |
| `AGENTS.md`, `README.md`, `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md` | Change | Phase-status passages after acceptance and after the merge |
| `PHASE_P5_PLAN.md` | Change | Status and the section 9 checklist |
| `docs/p5/P5_OBSERVATION_CONTRACT.md`, `docs/p5/P5_VERIFICATION.md` | Change | Verification reference to the accepted candidate; section for this step |

## 5. Procedure
### A1 Preconditions
**Red**
- No Red: records.

**Roles**
- the orchestrating agent confirms the P5.10 outcome, compares the integration-target SHA of P5.10 with the current pull-request base (on a mismatch the affected P5.10 verification is rerun against the current base before A2), identifies the final candidate and verifies that the verification record and the contract reference it.

**Check**
- outcome, SHAs and references recorded in section 1 of the acceptance record.

### A2 Evidence matrix
**Red**
- No Red: records.

**Roles**
- the orchestrating agent maps every T-P5-01 to T-P5-19 row to tests, step records and run IDs with status PASSED or OPEN and names characterization evidence with provenance and writes the prose; Codex reviews the record read-only.

**Check**
- every mandatory row PASSED with tests and evidence; an OPEN mandatory row cites a specific user-approved amendment of the acceptance criteria, the blocker and the owner, otherwise the audit outcome is NOT ACCEPTED and A7 onward do not start; an optional T-P5-18 skip is recorded separately with its limitation; no row reported as passed without evidence.

### A3 Contract summary and A4 limitations
**Red**
- No Red: records.

**Roles**
- the orchestrating agent lists the approved OC rows with links and the export-contract status (unchanged or additive decisions), and every limitation with its owner (including inherited P2 and P4 limitations carried forward item by item, the uncorrected instrument-dependent `RWS_10` time stamp of OC-05 and the calm-wind project assumption of OC-06) and writes the prose.

**Check**
- every OC row listed; every limitation has an owner.

### A5 Audit decision and A6 P6 handoff
**Red**
- No Red: records.

**Roles**
- the orchestrating agent records the audit decision (ACCEPTED or NOT ACCEPTED with the missing items; never the user's acceptance) and writes `docs/p5/P6_HANDOFF.md` (what P5 provides: `IDwdObservationParser` and its services, the observation contract, the extended evidence reader; P6 responsibilities per DEVELOPMENT_PLAN.md; reuse obligations; working rules; open items); the agent checks every cited type name against the code; Codex reviews the record and the handoff read-only; the orchestrating agent fixes confirmed findings.

**Check**
- audit decision recorded; after NOT ACCEPTED the actions A7 to A11 wait until the gaps are closed or the criteria are amended with the user's approval; handoff complete; Codex rounds recorded.

### A7 User acceptance
**Red**
- No Red: decision.

**Roles**
- the orchestrating agent presents the record and the handoff; the user decides; the decision is recorded with date and exact wording; no status change before this decision.

**Check**
- user decision recorded with date and wording.

### A8 Status updates
**Red**
- No Red: documentation.

**Roles**
- the orchestrating agent writes the status passages, runs the README package check (CONTRIBUTING.md section 4), build, all tests and the pre-commit coverage on the working tree for the full P5 diff; Codex reviews the uncommitted changes read-only; the orchestrating agent fixes confirmed findings; checkpoint commit when the user requests it; `ci-local.ps1` on the committed revision; push.

**Check**
- package check passed; committed-revision run passed; all status documents consistent (AGENTS.md table).

### A9 Ready for review
**Red**
- No Red: process.

**Roles**
- on the user's request `gh pr ready <number>`; the orchestrating agent records the `ready_for_review` CI run of the head (or records that the user marked the pull request ready earlier, with the event time).

**Check**
- CI run recorded; pull request ready for review.

### A10 Review
**Red**
- No Red: process.

**Roles**
- review comments are analysed and confirmed findings fixed in the owning scope (code, tests, scripts and data test-first by qwen; documentation by the orchestrating agent), verified, committed on request and pushed; a fix that changes production code, tests, build, CI, package or schema files creates a new implementation candidate: P5.10 S1 to S4 rerun, A2, A5 and A6 refreshed, renewed user acceptance (A7) whenever the acceptance basis changed, that is accepted behavior, evidence, contract claims, limitations or acceptance criteria (editorial-only corrections excepted, as in P4.12 A10); before the merge the current pull-request base is compared with the base of the recorded verification; if `develop` advanced, the affected P5.10 verification and audit are rerun against the new base.

**Check**
- all review comments resolved or answered; review approval recorded; all hosted jobs pass for the final head and base pair.

### A11 Merge
**Red**
- No Red: process.

**Roles**
- the user (or the named reviewer) merges into `develop`; the orchestrating agent merges only on explicit instruction; post-merge checkpoint on the user's request in the home the user chose: verify the merged pull request, record the merge SHA, update the P6 handoff integration target to the merged `develop` revision, update all phase-status documents of the AGENTS.md consistency table to "P5 complete" (README package check if README changes), commit on request, `ci-local.ps1` on the checkpoint revision against its integration target, results linked from the acceptance record; P6 starts only on a separate instruction.

**Check**
- merge commit recorded; post-merge records placed where the user decided; P5 is complete only after A7 and A11.

## 6. Tests and Test IDs
| Step-local case | T-P5 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| A2 | T-P5-01 to T-P5-19 | all P5 rows | Every row mapped to tests and evidence; characterization named with provenance |
| A8 | T-P5-14 (status) | REQ-090 | README and status documents consistent; package check |

## 7. Evidence to Record
- Review identity (candidate, pull request, base, head); evidence matrix; contract summary; limitations with owners
- Audit decision; user decision with date and wording; status updates and checks; ready-for-review and CI runs; review rounds, findings and dispositions; merge SHA; post-merge checkpoint runs
- Codex review rounds of the record and the handoff; authorship

## 8. Risks and Limitations
- (a) `develop` advances before the merge — rerun of the affected verification against the new base (A10)
- (b) A review fix changes production code — new candidate, P5.10 S1 to S4 rerun, refreshed A2, A5 and A6
- (c) The user marks the pull request ready earlier than planned — recorded as such; the review then covers the later commits through `synchronize` runs
- (d) Corrections during A8 to A11 that materially change evidence, contract claims, limitations or acceptance criteria reopen the affected P5.10 audit and A2 to A6, stop progression until the verification outcome satisfies the approved criteria, and repeat A7

## 9. Acceptance Criteria
- [ ] Preconditions recorded; candidate and base verified
- [ ] Evidence matrix complete: every mandatory T-P5 row PASSED, or OPEN only with a user-approved amendment, blocker and owner; a T-P5-18 skip recorded separately
- [ ] Contract summary and limitations complete
- [ ] Audit decision recorded; P6 handoff written and Codex-reviewed
- [ ] User acceptance recorded with date and wording
- [ ] Status documents consistent; package check and committed-revision run passed
- [ ] Pull request ready for review; review findings resolved test-first; new-candidate rule applied where production code changed
- [ ] Merged into `develop` by the user; post-merge checkpoint recorded in the chosen home
- [ ] Codex review of the A8 changes with 0 open findings, or a recorded Codex unavailability or failure reported to the user
