# P7.12 – Acceptance and P8 Handoff: Implementation Plan

**Status: planned (2026-10-26); reconciled on 2026-10-26 with the approved [P7 forecast contract](P7_FORECAST_CONTRACT.md) (P7.02 S6); not started.** Refines the P7.12 row of section 5 and section 9 of [PHASE_P7_PLAN.md](../../PHASE_P7_PLAN.md).

## 1. Goal and Scope
Goal: audit the P7 evidence of the READY candidate, record the limitations with owners, obtain the user's acceptance, update the status documents, write the P8 handoff, take the pull request through review and record the merge done by the user.
In scope: the record `docs/p7/P7_12_ACCEPTANCE.md` (evidence matrix T-P7-01 to T-P7-23, MF decisions with their implementation, export contract changes, limitations, audit decision, acceptance, merge); `docs/p7/P8_HANDOFF.md`; status passages; pull request ready for review on the user's instruction; review fixes test-first; the post-merge checkpoint on the user's instruction.
Out of scope with owner: merging → the user; P8 → a separate instruction of the user.

## 2. Preconditions and Inputs
- P7.11 outcome READY on a fixed candidate.
- Inputs: `docs/p7/P7_VERIFICATION.md`, `docs/p7/P7_11_INTEGRATED_VERIFICATION.md`, `docs/p7/P7_FORECAST_CONTRACT.md`, the P2 export contract with the C decisions of P7.03, the P6.11 acceptance record as the pattern.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Acceptance | Accept on the candidate / accept with amendments / not accept | The user decides on the evidence | User | A7 |
| OPEN rows | Only with a user-approved amendment | As stated | User | A2 |
| Merge | The user merges into `develop`; never auto-merge | As stated | User | A11 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p7/P7_12_ACCEPTANCE.md` | Create | Evidence audit, limitations, decision, acceptance, review, merge |
| `docs/p7/P8_HANDOFF.md` | Create | Foundation, what P7 provides, P8 responsibilities, reuse obligations, open items |
| `PHASE_P7_PLAN.md`, `README.md`, `AGENTS.md`, `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md` | Change | Status passages and section 9 checklist |

## 5. Procedure
No action changes behavior. The orchestrating agent writes all records; Codex reviews A2 to A6 and A8; the user decides A7 and A11.

- **A1**: candidate and base verified against the P7.11 record and `gh pr view`.
- **A2**: every T-P7 row with its evidence; OPEN only with a user-approved amendment; the live row separately.
- **A3**: MF-01 to MF-18 with their implementation; the additive export decisions of P7.03 (MF-09 `run_age_exceeded`, MF-11 `wind_gust_kmh`, `precipitation_amount_mm`, the `IntervalAggregation` members and the builder amendments, MF-02 `OmittedValues` on the rejection) listed with their C numbers and the regenerated schema checksum.
- **A4**: limitations with owners: the MOSMIX_S download per parse and its measured cost; the compressed archive held in memory; the first interval before `issued_at` (MF-13); the CRC-32 check enforced on the forecast paths only, unless the user approved the P4 amendment (P7.04); elements not exported (probabilities, longer totals and gusts, codes, cloud cover, visibility, sunshine; no humidity derived from the dew point, MF-10); `pressure_reference` `unknown` for MOSMIX pressure until a DWD source states the reduction level (MF-10); the registration consequence for consumers; the schema compatibility note (C-105); the inherited items (builder duplication guard, XML cancellation, hosted ARM64 evidence, package reproducibility, DI under load, the MOSMIX_S evidence-refresh CLI exposure, the OsthessenWetter source conditions OW-14); any MF-18 amendment proposal for the user.
- **A5**: audit decision ACCEPTED or NOT ACCEPTED with the gaps.
- **A6**: P8 handoff: the parsers, results, options, IDs, the product-specific sources of C-68 for P8 topics, complete-run retained semantics of REQ-062 as a P8 obligation, and the OW-14 redistribution restriction carried forward.
- **A7**: the user's acceptance wording and date recorded.
- **A8**: status passages ("P7 accepted; complete only once the user merges the pull request"); README package check; `ci-local.ps1` on the committed revision.
- **A9**: pull request marked ready for review on the user's instruction; CI run recorded.
- **A10**: review findings verified and fixed test-first; a production change makes a new candidate and reruns P7.11 S1 to S3.
- **A11**: the user merges; the merge SHA, the P8 handoff integration target and "P7 complete" recorded where the user decides.

## 6. Tests and Test IDs
No new automated test; the audit covers T-P7-01 to T-P7-23.

## 7. Evidence to Record
- Matrix with evidence links; limitations; audit decision; acceptance wording; review findings and fixes; merge SHA; Codex rounds

## 8. Risks and Limitations
- (a) A review finding changes production code — a new candidate and a P7.11 rerun
- (b) The user accepts with amendments — recorded as amendments with their owners

## 9. Acceptance Criteria
- [ ] Every T-P7 row with evidence; limitations with owners
- [ ] Audit decision and the user's acceptance recorded
- [ ] P8 handoff written; status documents consistent
- [ ] Pull request reviewed, findings resolved, merged by the user; post-merge state recorded
