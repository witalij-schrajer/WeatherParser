# P4.11 – Integrated Coverage and Platform Verification: Implementation Plan

**Status: executed (2026-10-23): S0 to S5 on candidate `98122e5`, outcome READY (record [P4_11_INTEGRATED_VERIFICATION.md](P4_11_INTEGRATED_VERIFICATION.md)); evidence-checkpoint commit and its CI pending.** Original planning note: plan only on 2026-10-21. **Reconciled with the approved selection contract (SC-15, SC-18, SC-22) on 2026-10-21 (P4.02 S6).** Refines the P4.11 row of section 5 and section 7 of [PHASE_P4_PLAN.md](../../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 4, 1, 2, 2, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0). S6 record (2026-10-21): qwen reconciled this plan from the orchestrating agent's change list and fixed the confirmed review findings section by section; the orchestrating agent edited these parts itself after failed qwen attempts, for regressions or for recorded design decisions: removal of invented S5 content (fabricated per-ID results). Codex five-angle review rounds: 2, 2, 2, 2, 2, 0 findings; cross-plan five-angle review of all P4 plans: rounds 4, 3, 1, 1, 0 findings.

## 1. Goal and Scope
Goal: verify one committed P4 candidate as a whole — Windows local CI with the native coverage gate against the then-current integration target, hosted CI of the pushed candidate, the same candidate on the NanoPi R76S (Debian 13 ARM64) with the owner's recorded authorization, and an optional, explicitly enabled live catalog check — and conclude READY or NOT_READY for P4.12.

In scope:
- Candidate identification (SHA, PR, integration target SHA)
- Windows `ci-local.ps1` run
- Hosted CI run IDs and job outcomes
- NanoPi run with authorization
- Comparison of Windows and ARM64 outcomes (test counts, block counters, gate results, package hashes where comparable)
- Optional live check
- T-P4 coverage audit (every T-P4 row mapped to passing tests and recorded evidence)
- Defects and their dispositions
- Outcome READY/NOT_READY
- Records

Out of scope with owner:
- Acceptance decision and status updates → P4.12
- New features
- P5 observation retrieval
- Release hardware acceptance → P9 (REQ-084)

## 2. Preconditions and Inputs
- P4.03–P4.10 complete with their Codex reviews
- The candidate committed and pushed on the user's request
- The SC source register (for the optional live check)

## 3. Decisions Required

| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| NanoPi run authorization | run now / later / skip with a named blocker | Ask the user for the recorded authorization of this run (access, transfer, tool installation if any) | User | S3 |
| Live catalog check | run once, explicitly enabled / skip | Optional and explicitly enabled through the P4.11 S0 runner; skip recorded as an open limitation, not a pass (SC-15) | User (P4.02, approved 2026-10-21) | S4 |
| Candidate for verification | the head commit of the draft PR after P4.10 / a later commit | The implementation candidate is the head commit after P4.10 with all step records; a later change to production code, tests, build, CI, package or schema files creates a new implementation candidate and restarts S1–S4; documentation-only evidence checkpoints (P4.11 records, `docs/COMPATIBILITY.md`) do not invalidate the evidence, are committed and pushed on the user's request, verified by their hosted CI run under the normal full-PR gate against the recorded integration target (the whole P4 diff is classified as code, so strict improvement applies; the evidence-only delta itself does not invalidate the implementation evidence) and linked to the implementation-candidate evidence | Orchestrator, recorded | S1 |

## 4. Artifacts

| Path | Action | Content |
|---|---|---|
| `docs/p4/P4_11_INTEGRATED_VERIFICATION.md` | Create | Record: candidate, integration target, Windows run, hosted CI, NanoPi run, comparison, live check, T-P4 audit, defects, outcome, authorship |
| `docs/p4/P4_VERIFICATION.md` | Append | P4.11 section |
| `docs/COMPATIBILITY.md` | Change | Add the P4 platform evidence (separating requested support, vendor support and project evidence, REQ-017) |
| `tests/WeatherParser.Parsing.Tests/Live/` | Create (conditional, S0) | Live-check runner guarded by an explicit runtime opt-in (live execution skipped in default runs) and its mocked offline tests (part of the standard runs); test code only; no production code changes in this step |

## 5. Procedure

### S0 Live-check runner preparation (conditional)
Only if the user wants the optional live check (S4): before S1, the runner is built test-first. 

**Red**: offline tests with a mocked `HttpMessageHandler` for:
- the runtime opt-in guard (without the explicit environment variable the runner returns before any network access),
- the request limit (one MOSMIX_S refresh, one CDC selection, one MOSMIX_L selection, no retries),
- timeout and size limits (30 s per request, 64 MiB for MOSMIX_S, 1 MiB per station file, 8 MiB for list/catalog responses),
- cancellation (caller token cancellation, selection deadline SC-18, refresh deadline SC-22),
- the complete sequence including refresh and two selections,
- boundary cases: exactly permitted response sizes, maximum request counts, deadline limits,
- HTTP 404 for CDC evidence files (must contribute nothing, SC-03),
- HTTP 404 for MOSMIX_L files (must produce `unavailable`, SC-05),
- **missing index test**: assert zero HTTP requests before `EvidenceCacheException` `MOSMIX-S-EVIDENCE-MISSING` (SC-22 requirement), **scoped to isolated MOSMIX selection with empty cache**,
- **over-size index test**: assert `EvidenceCacheException` `MOSMIX-S-EVIDENCE-MISSING` when index exceeds 8 MiB,
- **invalid index test**: assert `EvidenceCacheException` `MOSMIX-S-EVIDENCE-MISSING` for malformed JSON or invalid structure,
- **stale index test**: assert successful use, preserved evidence timestamps and exactly one warning `MOSMIX-S-EVIDENCE-STALE` when `MosmixSEvidenceMaxAge` is exceeded (SC-22 requirement),
- **refresh deadline test**: assert `EvidenceCacheException` `MOSMIX-S-REFRESH-DEADLINE-EXCEEDED` when refresh exceeds `MosmixSRefreshDeadline`,
- **selection deadline test**: assert `SelectionLimitException` `SELECTION-DEADLINE-EXCEEDED` when selection exceeds `SelectionDeadline`,
- **max candidates test**: assert `SelectionLimitException` `SELECTION-TOO-MANY-CANDIDATES` when candidate count exceeds `MaxEvidenceCandidates`,
- **invalid evidence candidates test**: assert `SelectionLimitException` `SELECTION-LIMIT-INVALID` for invalid `MaxEvidenceCandidates` or `SelectionDeadline`.

Expected failures:
- `SelectionLimitException` `SELECTION-LIMIT-INVALID` for invalid `MaxEvidenceCandidates` or `SelectionDeadline`,
- `SelectionLimitException` `SELECTION-TOO-MANY-CANDIDATES` when candidate count exceeds `MaxEvidenceCandidates`,
- `SelectionLimitException` `SELECTION-DEADLINE-EXCEEDED` when selection deadline is exceeded,
- `EvidenceCacheException` `MOSMIX-S-EVIDENCE-MISSING` for missing or invalid index,
- `EvidenceCacheException` `MOSMIX-S-REFRESH-DEADLINE-EXCEEDED` when refresh deadline is exceeded,
- `CatalogRetrievalException` `CATALOG-TIMEOUT` on request timeout,
- `CatalogRetrievalException` `CATALOG-RETRIEVAL-FAILED` on non-success status (except HTTP 404 for CDC and MOSMIX_L files),
- `CatalogRetrievalException` `CATALOG-TOO-LARGE` on response size exceeding limits,
- `CatalogFormatException` on format validation failure.

**Green**: qwen generates the minimal runner in `tests/WeatherParser.Parsing.Tests/Live/` (only the live execution is guarded and skipped in default runs; a test category may be added for convenience but is never the only protection).

**Refactor**: with tests green. Then the orchestrating agent reviews the diff and runs build, tests and the pre-commit coverage on the working tree (CONTRIBUTING.md section 5 with untracked files included); Codex reviews it read-only; the user requests the commit; S1–S4 then run on that candidate.

**Roles**: qwen generates tests and code; the orchestrating agent writes briefs, runs Red, builds, tests and coverage, reviews and records authorship.

**Check**: restore, build and test commands (including offline test coverage) must pass before proceeding.
Without this slice S4 is skipped and recorded as an open limitation.

### S1 Candidate and Windows run
**No Red**

Record the candidate SHA, the PR number and the integration target SHA (`git rev-parse` of the PR base); run `pwsh ./scripts/ci-local.ps1 -Commit <candidate-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`; record exit code, run directory, test counts per project, native covered/total blocks for candidate and target, gate rules (minimum, comparability, strict improvement), gate-result file SHA-256, package identity, packed and restored SHA-512, entries and consumer-lock comparison (C-116).

**Roles**: The orchestrating agent runs and extracts every value; qwen drafts the record prose from the extracted values.

**Check**: 
- `git rev-parse <candidate-sha>`
- `git rev-parse <integration-target-sha>`  
- `pwsh ./scripts/ci-local.ps1 -Commit <candidate-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`
- Record all outputs

### S2 Hosted CI
**No Red**

Record the run ID of the candidate push and the outcome of each job (Windows x64, Ubuntu x64, Debian 13 ARM64 container); download the run artifacts, verify that `commits.json` names the recorded candidate and integration-target SHAs (a run on another PR base needs a matching rerun), and record the C-116 package evidence (identity, packed and restored SHA-512, entries, consumer-lock comparison) with artifact references; a failing job or mismatching SHA pair is a defect (S5).

**Roles**: As S1.

**Check**:
- Record hosted CI run IDs
- Record job outcomes

### S3 NanoPi R76S
**No Red**

With the user's recorded authorization, run CONTRIBUTING.md section 7 for the candidate and the integration target; copy the run directory back; compare with Windows (test counts, block counters, gate result, geodesy test results within tolerance); without authorization record a named blocker; the outcome stays NOT_READY while T-P4-17 evidence is missing — a user acknowledgment of the blocker never counts as a pass; only an explicitly approved, recorded amendment of the phase acceptance criteria changes what READY requires, and the missing check stays listed as open (REQ-084).

**Roles**: As S1.

**Check**:
- Run NanoPi procedure per CONTRIBUTING.md section 7
- Record run directory and comparison data
- Record authorization or blocker

### S4 Optional live check
**No Red**

Only if the user enables it and REQ-092 conditions permit; a separately invoked, explicitly enabled test or procedure (never part of `dotnet test` defaults) performs one CDC and one MOSMIX selection against the recorded endpoints; record date, endpoints by name, response sizes, result counts, schema validation of both documents, and any format deviation versus the SC check (hash or structure); qwen drafts it, the orchestrating agent reviews it and Codex reviews it read-only; a deviation is a finding for the owning step, not a silent adaptation.

Before the user is asked to enable it, a reviewed live-check procedure exists: the exact invocation of a committed, explicitly enabled runner (never in default test runs), the search inputs, the endpoint names mapped to the SC source register, the approved retrieval limits (timeout, response size, no retries), and a total request limit equal to the endpoints one CDC and one MOSMIX selection need, plus one MOSMIX_S evidence refresh; qwen drafts it, the orchestrating agent reviews it and Codex reviews it read-only.

The live check performs one MOSMIX_S evidence refresh into a temporary cache directory, one CDC selection and one MOSMIX selection with a small search radius; the request limit is the request count of exactly these operations: four station lists plus the per-station `now` files of at most `MaxEvidenceCandidates` candidates (the check uses a radius that keeps the candidate count small and records it), one MOSMIX catalog, the MOSMIX_L files of the in-area locations, and one MOSMIX_S all-stations file; timeouts and size bounds of SC-09; no retries; the refresh deadline of SC-22. Record the candidate counts, the MOSMIX_S index `downloaded_at` and `issued_at`, and the evidence timestamps in both documents. Respect the SC-16 guidance (one run, not repeated).

**Roles**: As S1; qwen may draft the live-check procedure text from the orchestrator's brief.

**Check**:
- Run live check if enabled
- Record all outputs

### S5 T-P4 audit, defects and outcome
**No Red**

Map every T-P4-01 to T-P4-20 row to its tests and evidence (step records, run IDs); list defects with owner and disposition; a defect is fixed test-first by qwen in the owning step's scope, then S1–S4 restart on the new candidate; outcome READY only when every mandatory row passes on Windows and the NanoPi (or an explicitly approved, recorded amendment of the phase acceptance criteria changes what READY requires (missing evidence stays listed as open and is never reported as passed)), coverage gate passed, hosted CI green and no defect open; otherwise NOT_READY with the missing items. Then Codex reviews the uncommitted records read-only; qwen fixes confirmed findings; checkpoint commit of the records when the user requests it. Records are committed as a documentation-only evidence checkpoint on the user's request, pushed to the draft pull request, and its hosted CI run is recorded; changes to files in the AGENTS.md consistency table update the dependent documents in the same change.

**Roles**: The orchestrating agent runs and extracts every value; qwen drafts the record prose from the extracted values.

**Check**:
- Audit all T-P4 rows
- List defects with owner and disposition
- Record outcome
- Codex review of records

If a defect is discovered after live verification (SC-16), restart mandatory offline/platform verification, but explicitly defer or skip another live execution according to SC-16 and record its disposition.

## 6. Tests and Test IDs

| Step-local case | T-P4 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P4-16 | REQ-086 | Native block coverage ≥ 80 % and strict improvement against the remeasured integration target (exact-100 % rule), unrounded counters, comparable settings |
| S1, S3 | T-P4-14 (package evidence) | REQ-013 | Packed and restored hashes, entries and consumer-lock comparison from committed runs (C-116) |
| S3 | T-P4-17 | REQ-017, REQ-084 | All P4 tests and the gate on the NanoPi R76S, separate from Windows; geodesy within tolerance |
| S4 | T-P4-18 | REQ-083, REQ-092 | Explicitly enabled live check, separate from standard tests, within source conditions (SC-15, SC-18, SC-09, SC-22) |
| S5 | T-P4-01 to T-P4-20 | REQ-085 and all P4 rows | Audit of every row with evidence |

## 7. Evidence to Record
- Every command with exit code, run directories, run IDs, SHAs, counters, gate results and hashes
- NanoPi authorization wording and date
- Live-check data (if run)
- T-P4 audit table
- Defects and dispositions
- Codex review rounds
- Authorship (orchestrator runs and extraction, qwen drafts)

## 8. Risks and Limitations
- (a) The integration target moves during verification — S1 uses the PR base at run time and records it; a later base change requires a rerun
- (b) ARM64 floating-point differences — tolerance from the SC row, differences recorded
- (c) The hosted ARM64 container mistaken for target evidence — kept separate
- (d) The live source changed since the P4.02 check — recorded as a finding, owning step decides
- (e) Missing NanoPi authorization — named blocker, no claim of ARM64 success

## 9. Acceptance Criteria
- [ ] Candidate and integration target recorded
- [ ] Windows gate passed with recorded counters
- [ ] Hosted CI green for the candidate with verified `commits.json` SHA pair and recorded C-116 package evidence
- [ ] NanoPi run recorded with authorization; without it the outcome is NOT_READY (a blocker acknowledgment is not a pass)
- [ ] Live check run within conditions or recorded as skipped (SC-15)
- [ ] Every T-P4 row audited
- [ ] No open defect
- [ ] Outcome recorded
- [ ] Codex review of the records with 0 open findings, or a recorded Codex unavailability or failure reported to the user (AGENTS.md)
- [ ] No production code changed in this step
