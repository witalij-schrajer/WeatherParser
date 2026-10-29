# P3.11 – Integrated Verification: Implementation Plan

**Status: planned on 2026-10-29; under review, not yet accepted.** Refines the P3.11 row of section 5 and section 7 of [PHASE_P3_PLAN.md](../../PHASE_P3_PLAN.md).

## 1. Goal and Scope
Goal: verify the P3 candidate on Windows, hosted CI and the NanoPi R76S, run the broker integration test against the authorized test broker, and run one authorized live check per command, with READY or NOT_READY.
In scope: S1 `ci-local.ps1` on Windows (library gate and the CL-19 result); S2 hosted CI of the candidate; S3 the NanoPi R76S run of `ci-local.ps1` on the exact candidate (CONTRIBUTING.md section 7) plus a run of the built CLI on the device (help, one JSON command against the fake source, the signal tests of P3.09); S4 the broker integration test of P3.08 against the authorized test broker, from Windows and from the NanoPi if the user authorizes both; S5 live checks: one run per command in JSON mode against the real sources with the configuration the user authorizes, `--ParseOstHessen` only in JSON mode without redistribution (OW-14), and one MQTT run of a DWD or MOSMIX command against the test broker; the record `docs/p3/P3_11_INTEGRATED_VERIFICATION.md`.
Out of scope with owner: the acceptance → P3.12; release artifacts → P9.

## 2. Preconditions and Inputs
- P3.03 to P3.10 complete; candidate = the head after P3.10 (a later code change makes a new candidate and repeats S1 to S5 as far as affected, with new authorizations for S3 to S5).
- Required user authorizations, each recorded verbatim: S3 (NanoPi run on the named candidate), S4 (the test broker, its location and whether credentials are used; no production broker), S5 (each live command with its source and frequency; DWD retrieval within the source conditions of the README).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Live-check configuration | A configuration file outside the repository / a committed file | Outside the repository (a scratch directory), never committed; the record names the endpoints by their public source hosts and the IDs used, never local paths or credentials | Orchestrator, recorded | S5 |
| Evidence of a live run | The exit code, the document's root, item counts, `retrieved_at` and source IDs / the full document | Exit code, root, counts, sources and timestamps; the document itself is not committed (source conditions; OW-14 for OsthessenWetter values) | Orchestrator, recorded (REQ-092) | S5 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p3/P3_11_INTEGRATED_VERIFICATION.md` | Create | S1 to S5 with commands, run directories, counters, platforms, authorizations, outcome |
| `docs/p3/P3_VERIFICATION.md`, `PHASE_P3_PLAN.md` | Change | P3.11 section; status line |

## 5. Procedure
No slice changes behavior, so there is no Red step; a defect found here is fixed test-first in the step it belongs to, with a new candidate.

### S1 Windows
- `pwsh ./scripts/ci-local.ps1 -Commit <candidate-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`; exit code, counts, both gate results per CL-19, packed SHA-512.

### S2 Hosted CI
- The CI run of the candidate push: every job and its conclusion; the hosted `debian13-arm64` container is not target evidence.

### S3 NanoPi R76S
- With authorization: Git bundle, hash check on the device, detached checkout at the candidate, `scripts/prepare-debian-arm64.sh`, `ci-local.ps1`; then the built CLI on the device: `--Help`, one command in JSON mode against the fake source, the Linux signal tests; device, OS, SDK and runtime versions.

### S4 Test broker
- With authorization: the opt-in integration tests of P3.08 S4 against the named test broker; broker product and version; results.

### S5 Live checks
- With authorization, one run per command in JSON mode (`--SelectDWD`, `--ParseDWD`, `--ParseOstHessen`, `--SelectMOSMIX` (MOSMIX_L availability; MOSMIX_S per CL-17), `--ParseMOSMIX --Product MOSMIX_L` and `--Product MOSMIX_S`), and one MQTT run against the test broker; exit code, root, counts, sources and timestamps per run.

### S6 Outcome
- READY when S1 to S3 pass with identical test outcomes and gate counters, S4 and S5 pass as authorized and no defect is open; otherwise NOT_READY with the open items; Codex review of the record.

## 6. Tests and Test IDs
| Step-local case | T-P3 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 to S3 | T-P3-20, T-P3-01, T-P3-18 | REQ-017, REQ-084, REQ-086 | Platforms, package consumption, process behavior |
| S4 | T-P3-19, T-P3-14 | REQ-062, REQ-063, REQ-083 | Real broker behavior |
| S5 | T-P3-19, T-P3-10, T-P3-11 | REQ-084, REQ-092 | Live runs, recorded separately |

## 7. Evidence to Record
- Run directories, exit codes, counters, hashes, CI run IDs, device data, broker data, live-run summaries, the authorizations verbatim, Codex rounds

## 8. Risks and Limitations
- (a) Live sources change; a live failure caused by the source is recorded as such and does not block READY when the offline behavior is verified and the user accepts the recorded cause
- (b) Without an authorized test broker S4 stays open and P3 cannot be accepted with broker evidence; the record says so

## 9. Acceptance Criteria
- [ ] S1 to S3 passed with recorded evidence; S4 and S5 passed as authorized or explicitly open
- [ ] Outcome READY or NOT_READY recorded; Codex 0 findings or reported
