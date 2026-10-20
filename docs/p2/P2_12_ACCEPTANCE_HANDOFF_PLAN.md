# P2.12 – Acceptance and P4 Handoff: Implementation Plan

**Status: plan only (2026-10-20); nothing is implemented or verified yet.** This plan refines the P2.12 row of section 5 and section 10 of [PHASE_P2_PLAN.md](../../PHASE_P2_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief and corrected by qwen in one review round (no-TDD statement, section 6 sentence, after-acceptance artifact rows); the orchestrating agent split the section 1 scope lines into lists and restored path backticks after qwen repeatedly left them as prose.

## 1. Goal and Scope
Goal: turn the P2.11 evidence into an auditable acceptance package — the requirement/test evidence matrix, the contract and version decisions, limitations and open items, the orchestrator's audit decision and the user's explicit acceptance decision — and hand P4 (and the later provider phases) the accepted contracts and their responsibilities; mark P2 complete only after the user accepts.

In scope:
- acceptance record
- evidence matrix
- P4 handoff document
- status updates after acceptance
- the phase plan's section 10 checklist

Out of scope (owner):
- new evidence, reruns or fixes → P2.11 or the owning step
- P4 coordinate and catalog work → P4
- release, package publication and hardware acceptance → P9

## 2. Preconditions and Inputs
- P2.11 outcome READY for one final candidate (or NOT_READY items with an explicit user disposition for each)
- `docs/p2/P2_VERIFICATION.md` and `docs/p2/P2_EXPORT_CONTRACT.md` complete
- The final candidate SHA is known

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Acceptance record location | Proposed `docs/p2/P2_12_ACCEPTANCE.md` (as in P1) / a section of `docs/p2/P2_VERIFICATION.md` | Separate record, matching P1 practice | Orchestrator, recorded | A5 |
| User acceptance | accept / accept with listed dispositions / reject with required rework | — (only the user decides) | User | A7 |
| Handling of NOT_READY items | each needs an explicit user disposition (accept as limitation with owner, or rework) / silent carry-over | Explicit disposition per item; silent carry-over is not allowed | User | A5, A7 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p2/P2_12_ACCEPTANCE.md` | Create | Acceptance record with audit decision, candidate SHA, gate counters, test counts, package and schema hashes, platform results, authorship, requirement/test evidence matrix, contract decisions, limitations, and user's decision |
| `docs/p2/P4_HANDOFF.md` | Create | P4 handoff document with accepted foundation, list contracts, ordering service, P4 responsibilities, downstream responsibilities for P5–P7, P8, working rules, and open items |
| `docs/p2/P2_VERIFICATION.md` | Update | Add link to the acceptance record |
| `AGENTS.md` | Update (after acceptance only) | Phase-status line |
| `README.md` | Update (after acceptance only) | Status summary; package check |
| `DEVELOPMENT_PLAN.md` | Update (after acceptance only) | P2 status and P4 handoff link |
| `PHASE_P2_PLAN.md` | Update (after acceptance only) | Status, section 10 checklist |

## 5. Procedure
P2.12 changes no product code, so it has no Red/Green/Refactor slices; each step below has Action, Roles, Check and an outcome rule.

### A1 – Preconditions
**Action:** Confirm the P2.11 outcome, the final candidate SHA and that `docs/p2/P2_VERIFICATION.md` and `docs/p2/P2_EXPORT_CONTRACT.md` reference that candidate.  
**Roles:** Orchestrator  
**Check:** Document review and `git rev-parse <candidate>`  
**Outcome rule:** Missing READY or mismatched candidate → stop; return to P2.11.

### A2 – Requirement and test evidence matrix
**Action:** Build the matrix T-P2-01 to T-P2-28 → requirement IDs → test names → Red/characterization evidence → run artifact → commit; mark each row PASSED, OPEN (with blocker and owner) or FAILED.  
**Roles:** Qwen drafts the table from the orchestrator's extracted data; the orchestrator verifies every row against the records and test sources.  
**Check:** Every row has real evidence or an explicit status.  
**Outcome rule:** Any FAILED row → audit decision NOT_READY.

### A3 – Contract and version decisions
**Action:** Summarize the accepted public contract (roots, entities, field mappings, `schema_version`, XML namespace, omission, time, numeric and error rules, schema files and delivery) with links to `docs/p2/P2_EXPORT_CONTRACT.md` and the schema files; list user decisions with dates.  
**Roles:** Qwen drafts; orchestrator verifies.  
**Check:** Links resolve; values match the contract record.  
**Outcome rule:** An undecided OPEN contract item → NOT_READY unless the user disposes of it.

### A4 – Limitations and open items
**Action:** List inherited P1 limitations, P2 limitations (for example coarse XML cancellation, characterization-only evidence, hosted-versus-target evidence), open decisions and their owners.  
**Roles:** Qwen drafts; orchestrator verifies.  
**Check:** Each item has an owner phase.  
**Outcome rule:** None (informational), but missing owners block A5.

### A5 – Audit decision
**Action:** Write `docs/p2/P2_12_ACCEPTANCE.md` with the audit decision ACCEPTED or NOT_READY for the evidence, the candidate, gate counters, test counts, package and schema hashes, platform results and authorship.  
**Roles:** The orchestrator writes the decision and verifies every value; qwen may draft prose.  
**Check:** Document review against `docs/p2/P2_VERIFICATION.md`.  
**Outcome rule:** NOT_READY → list required rework and stop.

### A6 – P4 handoff
**Action:** Write `docs/p2/P4_HANDOFF.md`: accepted foundation (candidate SHA, package version, block counters as the next integration target context), accepted list contracts and the ordering service for P4 (`CdcStationList`, `MosmixLocationList`, `SearchParameters`, availability vocabulary), P4 responsibilities (coordinate parsing, geodesy, inclusive filters, availability derivation, exclusion of locations without altitude, empty-result log notice), downstream responsibilities for P5–P7 (source units, quality codes, time extraction into the P2.04 service, forecast-only quantities as additive contract changes), P8 (standalone dataset and run roots as MQTT payloads), working rules and open items.  
**Roles:** Qwen drafts from the orchestrator's facts; the orchestrator verifies.  
**Check:** Every responsibility traces to REQUIREMENTS.md and the phase plan.  
**Outcome rule:** None.

### A7 – User acceptance
**Action:** Present the acceptance record and handoff to the user; record the user's decision with date and wording in `docs/p2/P2_12_ACCEPTANCE.md`.  
**Roles:** User decides; orchestrator records.  
**Check:** The recorded wording.  
**Outcome rule:** No acceptance → P2 stays not complete.

### A8 – Status updates after acceptance
**Action:** Only after A7 acceptance, update the AGENTS.md phase-status line, README status summary, `DEVELOPMENT_PLAN.md`, `PHASE_P2_PLAN.md` status and its section 10 checklist; add the P4 handoff link; commit only when the user requests it.  
**Roles:** Qwen drafts the text changes; the orchestrator applies and verifies consistency.  
**Check:** The AGENTS.md consistency table rows for "Phase status" and `README.md` (package check after a README change).  
**Outcome rule:** Inconsistency between documents → fix before reporting completion.

## 6. Tests and Test IDs
| Step-local case | T-P2 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| A2 | T-P2-01 to T-P2-28 | All requirement IDs of the phase plan matrix | Every row mapped to real tests and run evidence, or marked OPEN/FAILED with owner; no new tests |

P2.12 adds no tests; step A2 produces the full T-P2-01 to T-P2-28 evidence matrix in `docs/p2/P2_12_ACCEPTANCE.md`.

## 7. Evidence to Record
- The acceptance record holds the matrix, decisions, limitations, the audit decision and the user's decision with date and wording
- Authorship per document part is recorded

## 8. Risks and Limitations
- Treating the audit decision as user acceptance — kept as two separate recorded decisions
- Status documents updated before acceptance — A8 runs only after A7
- Stale links after later commits — the record names the candidate and the document revision inspected
- A README status change alters the packed package — the package check of CONTRIBUTING.md section 4 is required

## 9. Acceptance Criteria
- [ ] Matrix complete for T-P2-01 to T-P2-28  
- [ ] Contract decisions and limitations recorded with owners  
- [ ] Audit decision recorded  
- [ ] P4 handoff written  
- [ ] User decision recorded with date and wording  
- [ ] Status documents consistent after acceptance  
- [ ] Nothing pushed or published without separate authorization
