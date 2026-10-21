# P4.12 – Acceptance and P5 Handoff: Implementation Plan

**Status: plan only (2026-10-21); nothing is implemented or verified yet.** Refines the P4.12 row of section 5 and section 9 of [PHASE_P4_PLAN.md](../../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 6, 3, 1, 1, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0).

## 1. Goal and Scope
Goal: turn the READY outcome of P4.11 into an explicit, evidence-based acceptance decision by the user, hand P5 the accepted selection foundation, update the status documents consistently, and complete the phase through the pull-request review and the user's merge into `develop`.

In scope:
- A1 preconditions; A2 requirement and test evidence matrix T-P4-01 to T-P4-18; A3 selection-contract summary (SC rows, any additive export decisions); A4 limitations and open items with owners; A5 audit decision in `docs/p4/P4_12_ACCEPTANCE.md`; A6 P5 handoff `docs/p4/P5_HANDOFF.md`; A7 user acceptance; A8 status updates after acceptance; A9 ready for review; A10 review; A11 merge.
- No production code, tests, packages or CI changes (review-finding fixes go through the owning step's test-first procedure).

Out of scope:
- New behavior or fixes outside review findings → owning step or P5; P5 implementation → P5 on a separate instruction; publication → P9.

## 2. Preconditions and Inputs
- P4.11 outcome READY for the final candidate (or NOT_READY with explicitly approved, recorded amendments of the acceptance criteria — then the open items stay listed as open)
- All step records complete
- Final candidate SHA and integration target SHA from P4.11
- `docs/p4/P4_VERIFICATION.md` and `docs/p4/P4_SELECTION_CONTRACT.md` reference the final candidate

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Acceptance | accept P4 / accept with recorded dispositions / do not accept | — (only the user decides; the audit decision is not the acceptance) | User | A7–A8 |
| Ready-for-review timing | after the A8 status commits are pushed / earlier | After A8, so the reviewed head contains the acceptance record and consistent status documents | Orchestrator, recorded | A9 |
| Home of the post-merge records (A9–A11 results) | first commit of the P5 branch (as for P2) / a separate documentation pull request / before the merge as far as known | Ask the user at A11 | User | A11 |
| Merge | the user (or a named reviewer) merges / the orchestrating agent merges on explicit instruction | — (only the user decides; no auto-merge) | User | A11 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p4/P4_12_ACCEPTANCE.md` | Create | Evidence matrix, contract summary, limitations, audit decision, user decision, status-update record, review and merge record |
| `docs/p4/P5_HANDOFF.md` | Create | P5 handoff document with accepted foundation, what P4 provides, what P5 owns, working rules, open items |
| `AGENTS.md` | Change | Phase-status line and dated line |
| `README.md` | Change | Status summary and dated line |
| `DEVELOPMENT_PLAN.md` | Change | Update status note |
| `PHASE_P1_PLAN.md` | Change | Status note |
| `PHASE_P4_PLAN.md` | Change | Status and the section 9 checklist |

## 5. Procedure
**Action A1: Preconditions**
- Confirm P4.11 outcome is READY or NOT_READY with approved amendments; Compare the integration-target SHA recorded by P4.11 with the current pull-request base; on a mismatch rerun the affected P4.11 verification (Windows, NanoPi, audit) against the current base and require an updated READY outcome before A2
- Identify final candidate SHA and integration target SHA
- Verify `docs/p4/P4_VERIFICATION.md` and `docs/p4/P4_SELECTION_CONTRACT.md` reference the final candidate
- Roles: orchestrating agent
- Output: record section 1 of `docs/p4/P4_12_ACCEPTANCE.md`
- Check: final candidate SHA is correct; verification and contract documents reference it

**Action A2: Evidence matrix**
- Map T-P4-01 to T-P4-18 to requirement IDs
- Identify test names for each behavior
- Record Red → Green → Refactor evidence per implementation slice and regression fix; characterization only for identified pre-existing behavior, with its provenance and any approved deviation recorded separately
- Document run artifacts (CI runs, coverage counters)
- Each row PASSED, FAILED or OPEN; every mandatory row must be PASSED unless a specific, recorded user amendment of that acceptance criterion exists (the row then stays visibly OPEN with blocker and owner); a skipped optional live check (T-P4-18) is recorded separately
- Roles: qwen drafts; orchestrating agent verifies
- Output: record section 2 of `docs/p4/P4_12_ACCEPTANCE.md`
- Check: no mandatory row FAILED; every OPEN row cites the recorded user amendment, blocker and owner

**Action A3: Contract summary**
- List approved SC rows with links to `docs/p4/P4_SELECTION_CONTRACT.md`
- Confirm export contract v1 unchanged or record additive decisions under C-03/C-105
- Roles: qwen drafts; orchestrating agent verifies
- Output: record section 3 of `docs/p4/P4_12_ACCEPTANCE.md`
- Check: all SC rows are listed with links; export contract status is correct

**Action A4: Limitations**
- List inherited P2 limitations
- Add P4 limitations (e.g., per-location availability `unknown`, live check skipped or performed, hosted container versus target evidence)
- Identify open decisions with owners
- Roles: qwen drafts; orchestrating agent verifies
- Output: record section 4 of `docs/p4/P4_12_ACCEPTANCE.md`
- Check: all limitations and open items are listed with owners

**Action A5: Audit decision**
- Record ACCEPTED or NOT_READY for the evidence; NOT_READY stops progression: A7 and later actions wait until the gaps are closed through the owning step and P4.11, or amended by the user
- Include candidate SHA, gate counters, test counts, package hash, platform results
- Roles: orchestrating agent
- Output: record section 5 of `docs/p4/P4_12_ACCEPTANCE.md`
- Check: audit decision is recorded with all required details

**Action A6: P5 handoff**
- Document accepted foundation (candidate SHA, package version, block counters)
- List what P4 provides (CDC catalog reader, availability derivation and its product mapping, retrieval boundary and endpoint options, selection services, notice IDs)
- List what P5 owns, as in DEVELOPMENT_PLAN.md P5: station resolution from the explicitly configured station (a missing station fails before any HTTP or MQTT; never the nearest or a substitute station), bounded retrieval with controlled missing-product behavior, CSV/ZIP parsing, verified units, quality, period mappings, DWD missing markers versus real zero, distinct measurement times, normalization through the existing `ISourceTimeNormalizer` and `IObservationDatasetBuilder`, export through the real serializers under the frozen v1 contract and schema, offline versus live evidence; CLI configuration precedence and broker transport stay with P3
- Define working rules and open items
- Roles: qwen drafts; orchestrating agent verifies
- Output: create `docs/p4/P5_HANDOFF.md`
- Check: the handoff links `ISourceTimeNormalizer`, `IObservationDatasetBuilder`, the serializers, `docs/p2/P2_EXPORT_CONTRACT.md`, `docs/p4/P4_SELECTION_CONTRACT.md` and the schema, and lists every P5 obligation above

**Action A7: User acceptance**
- Present the record and the handoff to the user
- Record the user's decision with date and exact wording
- No status change before this decision
- Roles: orchestrating agent
- Output: record section 6 of `docs/p4/P4_12_ACCEPTANCE.md`
- Check: user decision is recorded with date and wording

**Action A8: Status updates after acceptance**
- Qwen drafts the status passages
- Orchestrating agent verifies them
- README package check (CONTRIBUTING.md section 4) on the working tree
- Orchestrating agent runs build, all tests and the pre-commit coverage on the working tree for the full P4 diff (CONTRIBUTING.md section 5, untracked files included); then Codex reviews the uncommitted record, handoff and status changes read-only; qwen fixes confirmed findings; the review run, findings and dispositions are recorded in `docs/p4/P4_VERIFICATION.md`; Each qwen fix is reviewed and the affected checks are repeated before the commit request (build, tests and coverage; the README package check again whenever README.md changed)
- Checkpoint commit when the user requests it
- `ci-local.ps1` on that committed revision
- Push
- Roles: orchestrating agent
- Output: record section 7 of `docs/p4/P4_12_ACCEPTANCE.md`
- Check: package check passes; committed-revision run passes; all status documents are consistent

**Action A9: Ready for review**
- On the user's request `gh pr ready <number>`
- Record the `ready_for_review` CI run of the head commit
- Roles: orchestrating agent
- Output: record section 8 of `docs/p4/P4_12_ACCEPTANCE.md` (or the post-merge home)
- Check: CI run is recorded; PR is marked ready for review

**Action A10: Review**
- Wait for the review
- Check each comment against REQUIREMENTS.md, AGENTS.md and the plans
- Confirmed findings fixed test-first by qwen in the owning scope, verified, committed on request and pushed; a fix that changes production code, tests, build, CI, package or schema files creates a new implementation candidate: P4.11 S1–S4 rerun, A2, A5 and A6 refreshed, and renewed user acceptance if accepted behavior changed; Before the merge, compare the current pull-request base with the base of the recorded verification; if `develop` advanced, rerun the affected P4.11 verification and audit against the new base, refresh the acceptance evidence and handoff, and require an updated READY outcome
- Rejected findings answered with the reason
- Corrections during A8–A11 that materially change the evidence, contract claims, limitations or acceptance criteria (for example results attributed to the wrong platform or candidate, or an omitted mandatory OPEN item) reopen the affected P4.11 verification or audit and the A2–A6 gates, even without a code change; progression stops until READY is restored under the approved criteria, and A7 is repeated whenever the basis of acceptance changes (also after base updates); editorial changes that leave evidence and acceptance basis intact do not reopen the gates
- Record each finding and disposition
- Roles: orchestrating agent
- Output: record review findings and dispositions
- Check: all review comments resolved or answered; review approval recorded; all hosted jobs pass for the final head and base pair

**Action A11: Merge**
- The user (or the named reviewer) merges into `develop`
- Orchestrating agent merges only on explicit instruction
- Post-merge checkpoint on the user's request, in the home the user chose: verify the merged pull request, record the merge SHA, update the P5 handoff integration target to the merged `develop` revision, update all phase-status documents of the AGENTS.md consistency table to "P4 complete" (README package check if README changes); P5 starts only on a separate instruction; For that checkpoint: README package check (CONTRIBUTING.md section 4) when README.md changes, commit on the user's request, then `pwsh ./scripts/ci-local.ps1 -Commit <checkpoint-sha> -TargetRef <its integration-target-sha> -RunSettings coverage.runsettings` on the committed revision; results linked from `docs/p4/P4_12_ACCEPTANCE.md`
- P4 is complete only after A7 and A11
- Roles: orchestrating agent
- Output: record merge commit and post-merge records location
- Check: merge commit is recorded; post-merge records are placed where user decided

## 6. Tests and Test IDs
| Step-local case | T-P4 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| A2 | T-P4-01 to T-P4-18 | All P4 REQ IDs | Every row PASSED with evidence or OPEN with blocker and owner |
| A8 | none | — | Status documents consistent per the AGENTS.md table; README package check |

## 7. Evidence to Record
- `docs/p4/P4_12_ACCEPTANCE.md` with:
  - Evidence matrix (T-P4-01 to T-P4-18)
  - Contract summary (SC rows, export contract status)
  - Limitations and open items
  - Audit decision (ACCEPTED/NOT_READY)
  - User decision (date, wording)
  - Status-update record
  - CI run IDs
  - Review findings and dispositions
  - Merge commit
- `docs/p4/P5_HANDOFF.md` with:
  - Accepted foundation details
  - What P4 provides
  - What P5 owns
  - Working rules
  - Open items

## 8. Risks and Limitations
- (a) Status documents claiming completion before the merge — "complete only after acceptance and merge" wording is checked
- (b) Review findings fixed without tests — test-first rule enforced
- (c) Records lost when the branch is deleted after the merge — user decides the home before A11
- (d) A P4.11 READY on an outdated base — A1 checks the candidate and base again

## 9. Acceptance Criteria
- [ ] Final candidate SHA confirmed and verified in documents
- [ ] Evidence matrix complete with all T-P4 rows having PASSED or OPEN status
- [ ] Contract summary includes all approved SC rows and export contract status
- [ ] Limitations and open items listed with owners
- [ ] Audit decision recorded with required details
- [ ] P5 handoff document created with all sections
- [ ] User decision recorded with date and wording
- [ ] Status documents consistent after acceptance
- [ ] README package check passes
- [ ] Committed-revision `ci-local.ps1` run passes
- [ ] Ready-for-review CI run passes
- [ ] Review findings resolved or rejected
- [ ] Pull request merged into `develop` by the user
- [ ] Post-merge records placed where user decided
- [ ] Codex review of A8 changes with 0 open findings, or a recorded Codex unavailability or failure reported to the user (AGENTS.md)
