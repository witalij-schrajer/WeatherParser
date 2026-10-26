# P7.11 – Integrated Coverage and Platform Verification: Implementation Plan

**Status: planned (2026-10-26); not started. Written before the P7.02 contract; it is reconciled with the approved contract in P7.02 S6 before its first Red test.** Refines the P7.11 row of section 5 of [PHASE_P7_PLAN.md](../../PHASE_P7_PLAN.md).

## 1. Goal and Scope
Goal: verify one fixed candidate of the complete P7 implementation through the integrated coverage gate on Windows, hosted CI and the NanoPi R76S, measure the MOSMIX_S stream duration and peak memory on the target, optionally run one authorized live parse per product, and record READY or NOT_READY.
In scope: the live-check modes of MF-17 and an explicitly enabled offline resource check (test-first, before the candidate is fixed); `ci-local.ps1` on Windows; the hosted CI run of the candidate push; the NanoPi R76S run (with authorization) including the resource check; the optional live parses (with separate authorization); the record `docs/p7/P7_11_INTEGRATED_VERIFICATION.md`.
Out of scope with owner: acceptance → P7.12; release hardware acceptance → P9.

## 2. Preconditions and Inputs
- P7.03 to P7.10 complete and committed; the candidate SHA fixed after S0.
- Existing: `scripts/ci-local.ps1`, `scripts/prepare-debian-arm64.sh`, the live-check runner (`LiveCheckRunner`, `LiveCheckSettings`, `RequestBudgetHandler`) with its P4, P5 and P6 modes, the P6.10 record as the pattern.
- Required user authorization: the NanoPi run (S3) and, separately, each live parse (S4).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Live-check modes | As approved in MF-17 (Proposed `mosmix-l`, `mosmix-s`), budget one request each | As approved; summaries value-free and escaped; artifacts only in the gitignored run directory | User (MF-17) | S0, S4 |
| Resource check (Proposed) | An explicitly enabled offline check (Proposed `WEATHERPARSER_RESOURCE_CHECK`) in two separate test-host invocations: (1) mode `generate` writes a synthetic all-stations KMZ of the recorded full size (placemark, element and step counts from P7.02) into the gitignored run directory and exits; (2) mode `measure` runs alone in a fresh `dotnet test` process (filter to the single check), parses the pre-generated file through the real parser with a fake HTTP handler serving the file from disk, and records the duration and the absolute `Process.PeakWorkingSet64` of that process at the end of the parse, compared with the approved total-memory threshold of MF-07 (an absolute peak for the whole measuring process, so no earlier peak can hide the parse's own use); the current working set before the parse is recorded for information only, never subtracted; nothing else runs in that process / one in-suite test that generates and parses (rejected: fixture generation and earlier tests set the process-lifetime peak) / measurement only with the live file | Two-invocation offline check: repeatable on both platforms without network and scoped to the parse; the live parse adds one real-file measurement when authorized | Orchestrator, recorded; the thresholds are MF-05 and MF-07 values | S0, S1, S3 |
| Readiness rule | READY when S1 to S3 pass and the measured values stay within the approved deadline, request bound and memory threshold; S4 passed or recorded as skipped | As stated; a threshold miss is NOT_READY and goes to the user (MF-05 default adjustment by a recorded decision) | User (MF-05, MF-07) | S5 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `tests/WeatherParser.Parsing.Tests/Live/*` | Change | Live-check modes and their runner tests |
| `tests/WeatherParser.Parsing.Tests/Forecasts/Mosmix/MosmixResourceCheckTests.cs` | Create | Explicitly enabled resource check |
| `docs/p7/P7_11_INTEGRATED_VERIFICATION.md` | Create | Candidate, runs, measurements, outcome |
| `docs/p7/P7_VERIFICATION.md`, `docs/COMPATIBILITY.md` | Change | P7.11 summary; platform evidence |

## 5. Procedure
### S0 Live-check modes and resource check (test-first, before the candidate is fixed)
- **Red**: the runner tests for the new modes (enabled only with the mode and the authorization variable, budget one request, the file never written to the repository, artifacts only to the gitignored run directory, the P4, P5 and P6 live tests skipped in these modes) and for the resource check (skipped unless enabled; `generate` writes the file with the recorded counts into the run directory only; `measure` refuses to run without the pre-generated file and without being the only selected test, records the absolute process peak after the parse and writes the result file into the run directory). **Green**: qwen implements; the orchestrating agent verifies; Codex reviews.

### S1 Windows
- `pwsh ./scripts/ci-local.ps1 -Commit <candidate-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`; counters, gate result and SHA-256, package hash, test counts recorded; the resource check run as its two separate invocations (`generate`, then `measure` in a fresh process) with its duration and absolute process peak working set.

### S2 Hosted CI
- The candidate push's run ID and each job outcome; artifacts downloaded; each `commits.json` names the candidate and the target.

### S3 NanoPi R76S (authorized)
- Bundle transfer with SHA-256 on both sides, `prepare-debian-arm64.sh`, `ci-local.ps1`, the resource check as its two separate invocations with duration and absolute process peak working set, run directory copied back, device directory removed; outcomes compared with Windows; the measured values compared with the approved MOSMIX_S deadline, request bound and memory threshold (T-P7-21).

### S4 Live parses (authorized separately)
- One request per product; the summary (product, ID, issue time, step count, element counts, omissions, freshness) recorded without values; the files not committed; never repeated without a new authorization.

### S5 Record and review
- Record written; Codex five-angle review; outcome READY or NOT_READY with open items.

## 6. Tests and Test IDs
| Step-local case | T-P7 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1, S3 | T-P7-20 | REQ-086 | Coverage gate on the candidate; strict improvement |
| S3 | T-P7-21 | REQ-005, REQ-017, REQ-084 | Target behavior, stream duration and memory |
| S0, S4 | T-P7-22 | REQ-083 | Optional live parses, separately enabled |

## 7. Evidence to Record
- Candidate and target SHAs; commands, exit codes, run directories, counters, gate results and SHA-256, package hashes; CI run IDs and job outcomes; NanoPi transfer hashes; measured durations and peak working sets per platform; live summaries; Codex rounds

## 8. Risks and Limitations
- (a) The hosted `debian13-arm64` job is a container, not target evidence; only the NanoPi R76S counts for T-P7-21
- (b) A synthetic full-size file approximates but does not equal a real file; the live parse, when authorized, adds one real measurement
- (c) A production change after S1 makes a new candidate and repeats S1 to S3

## 9. Acceptance Criteria
- [ ] S0 test-first and reviewed; candidate fixed afterwards
- [ ] Windows, hosted CI and NanoPi runs passed with recorded evidence
- [ ] MOSMIX_S duration and peak memory within the approved values on the target, or NOT_READY reported
- [ ] Live parses passed or recorded as skipped
- [ ] Outcome recorded; Codex 0 findings or reported
