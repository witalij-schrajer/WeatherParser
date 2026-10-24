# P5.02 – Source Facts and Observation Contract: Implementation Plan

**Status: plan only (2026-10-24); nothing is implemented or verified yet.** Refines section 3 and the P5.02 row of section 5 of [PHASE_P5_PLAN.md](../../PHASE_P5_PLAN.md).

## 1. Goal and Scope
Goal: verify the DWD 10-minute `now` observation sources against official documentation and the actual published files, record the section 3 decisions of the phase plan as the observation contract `docs/p5/P5_OBSERVATION_CONTRACT.md` (OC-01 to OC-14, Proposed), have Codex review it read-only, obtain the user's approval, and reconcile the step plans P5.03 to P5.11 with the approved contract. No production code.
In scope: source register with URL, check date, size and SHA-256 for the four product descriptions, the four `now/` directory listings and one sample archive per product; verified facts per product (file names, header, columns, units, `QN`, missing marker, time base, row counts, 404 behavior); access conditions, attribution and retrieval-frequency guidance (REQ-092); the decision register; the Codex review loop (up to 10 rounds); user approval; reconciliation of the later step plans.
Out of scope with owner: any production code, fixture or test → P5.03 onwards; CLI decisions → P3; export contract changes → none planned (C-03 if ever needed).

## 2. Preconditions and Inputs
- P5.01 outcome READY; `PHASE_P5_PLAN.md` section 3 (decision rows) and the P4 research of 2026-10-21 (`docs/p4/P4_SELECTION_CONTRACT.md` sections 2, 3.1 and 3.4; `docs/p4/research/P4_02_CDC_TU_NOW_MANIFEST.md`).
- Official sources (URL and date recorded in the register): the four DESCRIPTION PDFs of the CDC 10-minute products (air temperature, wind, precipitation, solar), the four `now/` directory listings, the DWD legal notice; downloads are research only and stay outside the repository except their names, sizes and hashes.
- Required user decisions: the fourteen OC rows (approval wording recorded with date).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| OC-01 Station ID grammar | as in the phase plan row "Station ID grammar" | `\A[0-9]{5}\z`, no padding | User | P5.03 |
| OC-02 Station existence and metadata | as in the phase plan row; conflict handling fail / first list | OPEN (conflict handling); existence and metadata as recommended | User | P5.03 |
| OC-03 Products to retrieve and 404 | as in the phase plan row | As recommended | User | P5.06 |
| OC-04 Value selection and times | as in the phase plan row | As recommended (REQ-043: no freshness filter) | User | P5.05, P5.07 |
| OC-05 Units, references and periods | as in the phase plan row, verified against the descriptions | As recommended after verification | User | P5.05 |
| OC-06 Calm wind | omit `DD_10` when `FF_10` = 0 / export as delivered / other DWD marker | OPEN until the wind description is checked | User | P5.05 |
| OC-07 Quality code | plain `QN` string (the source code unchanged, C-53) | Plain string; a prefixed or translated token is out of scope (C-03) | User | P5.04, P5.05 |
| OC-08 Public API | as in the phase plan row | `IDwdObservationParser.ParseAsync(string stationId, CancellationToken)` returning `ObservationDataset`; notices logged | User | P5.07 |
| OC-09 Options, registration and deadline adaptation | as in the phase plan row | New `DwdObservationOptions`; additive `CatalogDeadlineKind.Observation` | User | P5.06, P5.09 |
| OC-10 Error and notice IDs | as in the phase plan row, including `OBSERVATION-LIMIT-INVALID` and the additive `OmittedReadings` property of `ObservationDatasetRejectedException` (type `IReadOnlyList<OmittedReading>`, empty by default, filled by the builder at the rejection site) | As proposed | User | P5.03–P5.07 |
| OC-11 Request bound | as in the phase plan row | At most eight requests, 4 in parallel, within `ObservationDeadline` | User | P5.06 |
| OC-12 Source conditions and retrieval guidance | as in the phase plan row | At most one parse per station per hour, external scheduling | User | P5.09 |
| OC-13 Live verification | as in the phase plan row | Optional, explicitly enabled, one run | User | P5.10 |
| OC-14 Shared reader extension | additive extension of `ICdcNowEvidenceReader` / second reader | Additive extension (SC-21) | Orchestrator, recorded | P5.04 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p5/P5_OBSERVATION_CONTRACT.md` | Create | Sections: 1 approval (date, wording), 2 source register, 3 verified source facts per product, 4 decision register OC-01 to OC-14 with status, 5 open items with owners, 6 Codex review rounds |
| `docs/p5/research/` | Create if needed | Manifest of downloaded research files (names, sizes, SHA-256); no DWD data copied into the repository beyond that |
| `docs/p5/P5_VERIFICATION.md` | Change | P5.02 section: research, draft, review rounds, approval, reconciliation |
| `docs/p5/P5_03_*_PLAN.md` to `docs/p5/P5_11_*_PLAN.md` | Change | Reconciliation with the approved contract (status note "reconciled with OC-nn on <date>") |
| `PHASE_P5_PLAN.md` | Change | Section 3 "Decided by" and status after approval; status line |

## 5. Procedure
### S1 Source verification
**Red**
- No Red: research.

**Roles**
- the orchestrating agent downloads and checks the sources and records URL, date, size and SHA-256; qwen drafts the facts text from the agent's extraction.

**Check**
- for each product: description PDF (units, `QN` levels, missing value, time base), directory listing (archive name pattern, count), one sample archive (member name, header, columns, row count, `STATIONS_ID` form, `MESS_DATUM` form); 404 behavior for a station without a file; compare with the P4 facts of 2026-10-21 and record every difference.

### S2 Contract draft
**Red**
- No Red: documentation.

**Roles**
- qwen drafts `docs/p5/P5_OBSERVATION_CONTRACT.md` from the agent's brief (phase plan section 3 rows, S1 facts); the orchestrating agent checks every ID, literal and reference.

**Check**
- fourteen OC rows with options, recommendation, decider and status "Proposed"; the not-exported columns listed (`TM5_10`, `TD_10`, `RWS_DAU_10`, `RWS_IND_10`, `DS_10`, `SD_10`, `LS_10`); unit conversion `GS_10` J/cm² × 10000 = J/m² stated with its source; attribution and IP note (REQ-092).

### S3 Codex review loop
**Red**
- No Red: review.

**Roles**
- Codex reviews the draft read-only (five-angle format); the orchestrating agent verifies each finding; qwen fixes confirmed findings; up to 10 rounds; every round's output and finding count recorded in the contract (AGENTS.md "Contract decisions").

**Check**
- rounds and counts recorded; remaining findings after round 10 reported to the user with the agent's check of each.

### S4 User approval
**Red**
- No Red: decision.

**Roles**
- the orchestrating agent presents the contract and the open rows (OC-02 conflict handling, OC-06 calm wind) to the user; the user approves; the approval wording and date are recorded in section 1.

**Check**
- every OC row has status Approved, or a recorded deferral that names its owner and the steps it blocks; the affected Red tests of a blocked step do not start before the approval (phase plan section 3).

### S5 Step-plan reconciliation
**Red**
- No Red: documentation.

**Roles**
- qwen reconciles `docs/p5/P5_03_*` to `P5_11_*` and `PHASE_P5_PLAN.md` section 3 from the agent's change list (targeted edits, one plan per request); the orchestrating agent checks the structure of every plan (nine headings, tables intact) and the diff; Codex reviews the reconciled plans read-only; qwen fixes confirmed findings.

**Check**
- every plan names the approved OC IDs it implements; no plan contradicts the contract; headings complete.

### S6 Record and checkpoint
**Red**
- No Red: records.

**Roles**
- qwen drafts the P5.02 section of `docs/p5/P5_VERIFICATION.md`; the orchestrating agent verifies; checkpoint commit when the user requests it; push; hosted CI run recorded.

**Check**
- on the complete step diff: `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, the Parsing tests and the pre-commit coverage gate under the documentation-only rule (CONTRIBUTING.md section 5); orchestrator review of the complete diff; read-only Codex review of the complete diff with recorded dispositions; then the checkpoint commit on the user's request, push, hosted CI run recorded.

## 6. Tests and Test IDs
| Step-local case | T-P5 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1, S2 | T-P5-18 (preconditions) | REQ-092 | Access conditions, attribution and retrieval guidance recorded before any live retrieval |
| S2 | T-P5-05, T-P5-06 (facts) | REQ-052, REQ-054, REQ-055 | Units, pressure reference and averaging periods verified against the DWD descriptions before implementation |

## 7. Evidence to Record
- Source register (URL, date, size, SHA-256) and the differences to the P4 facts
- Contract rounds and finding counts; approval wording and date
- Reconciliation change list and Codex rounds
- Commit and CI run; authorship

## 8. Risks and Limitations
- (a) A source changed since 2026-10-21 (columns, units, archive names) — recorded as a difference; the contract follows the current source
- (b) DWD calm-wind semantics not documented — OC-06 stays OPEN for the user with the observed sample behavior recorded
- (c) Codex review does not converge within 10 rounds — remaining findings reported with the agent's check; the user decides
- (d) Research downloads: only names, sizes and hashes enter the repository (REQ-083, licence)

## 9. Acceptance Criteria
- [ ] Source register complete; every fact traceable to a source
- [ ] OC-01 to OC-14 drafted with options, recommendation and decider
- [ ] Codex rounds recorded; 0 findings or the remaining findings reported to the user
- [ ] User approval recorded with wording and date
- [ ] Every deferred OC row names its owner and the steps it blocks; no blocked Red test started
- [ ] Step plans P5.03 to P5.11 and `PHASE_P5_PLAN.md` reconciled; structure checks passed
- [ ] Complete step diff passed build, tests, the documentation-only coverage gate and a final Codex review before the commit request; records committed on the user's request; CI run recorded
- [ ] No production code, fixture or test changed
