# P3.11 – Integrated Verification: Implementation Plan

**Status: planned on 2026-10-29; accepted by the user on 2026-10-29 with the phase plan ("Accept the plans"); reconciled on 2026-10-29 with the [P3 CLI contract](../../p3/P3_CLI_CONTRACT.md) approved by the user on 2026-10-29 ("Approve all"), section 10 of this plan; where this plan's earlier text differs, section 10 and the contract win.** Refines the P3.11 row of section 5 and section 7 of [PHASE_P3_PLAN.md](../PHASE_P3_PLAN.md).

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
- App-host smoke test (the P3.03 decision): the built app host `WeatherParser.exe` of the candidate runs `--Help` (expected: usage on stdout, nothing on stderr, exit 0) and one command in JSON mode against the loopback fake source (expected: one complete document on stdout, exit 0); command lines, exit codes and output sizes recorded.
- Real Ctrl+C check (P3.09 section 8): the app host runs a JSON command with `--OutputFile` against the loopback fake source that delays its response, in its own console; a real console Ctrl+C is delivered (pressed by the user or sent to the console's process group with `GenerateConsoleCtrlEvent` by a helper in that console; the method is recorded); expected: exit code 4 per CL-11 (or as approved), the cancellation summary on stderr, no destination file and no temporary file.

### S2 Hosted CI
- The CI run of the candidate push: every job and its conclusion; the hosted `debian13-arm64` container is not target evidence.

### S3 NanoPi R76S
- With authorization: Git bundle, hash check on the device, detached checkout at the candidate, `scripts/prepare-debian-arm64.sh`, `ci-local.ps1`; then the built CLI on the device through its app host `./WeatherParser` (the P3.03 app-host smoke test: `--Help` exit 0 with usage on stdout, one command in JSON mode against the fake source with one complete document and exit 0), the Linux signal tests, and one real SIGINT and one real SIGTERM sent with `kill` to the app host during a delayed JSON command (expected: exit 4, summary on stderr, no destination or temporary file); device, OS, SDK and runtime versions.

### S4 Test broker
- With authorization: the opt-in integration tests of P3.08 S4 repeated on the candidate against the named test broker (P3.08 already ran them once during development); broker product and version; results.

### S5 Live checks
- With authorization, one run per command in JSON mode (`--SelectDWD`, `--ParseDWD`, `--ParseOstHessen`, `--SelectMOSMIX` (MOSMIX_L availability; MOSMIX_S per CL-17), `--ParseMOSMIX --Product MOSMIX_L` and `--Product MOSMIX_S`), and one MQTT run against the test broker; exit code, root, counts, sources and timestamps per run.

### S6 Outcome
- READY when, on each of S1 to S3, every test applicable to that platform passes (none failed; the skipped tests are exactly the documented opt-in and platform-specific ones: for example the Linux-only signal and file-permission tests on Windows and the Windows trigger test on Linux, each named with its reason) and every coverage gate of CL-19 passes on that platform; the tests shared by all platforms are compared separately and must have identical outcomes; differences in counts or gate counters are recorded and explained by the documented platform-specific tests and code paths; the app-host smoke tests (S1 and S3), the real Windows Ctrl+C check (S1) and the real SIGINT and SIGTERM checks on the NanoPi (S3) passed with their expected outcomes; S4 and S5 pass as authorized and no defect is open; otherwise NOT_READY with the open items; Codex review of the record.

## 6. Tests and Test IDs
| Step-local case | T-P3 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 to S3 | T-P3-20, T-P3-01, T-P3-18 | REQ-006, REQ-007, REQ-017, REQ-084, REQ-086 | Platforms, package consumption, app host, real signals, process behavior |
| S4 | T-P3-19, T-P3-14 | REQ-062, REQ-063, REQ-083 | Real broker behavior |
| S5 | T-P3-19, T-P3-10, T-P3-11 | REQ-084, REQ-092 | Live runs, recorded separately |

## 7. Evidence to Record
- Run directories, exit codes, counters, hashes, CI run IDs, device data, broker data, live-run summaries, the authorizations verbatim, Codex rounds

## 8. Risks and Limitations
- (a) Live sources change; a live failure caused by the source is recorded as such and does not block READY when the offline behavior is verified and the user accepts the recorded cause
- (b) Without an authorized test broker S4 stays open and P3 cannot be accepted with broker evidence; the record says so

## 9. Acceptance Criteria
- [ ] S1 to S3 passed with recorded evidence, including the app-host smoke tests and the real Ctrl+C, SIGINT and SIGTERM checks; S4 and S5 passed as authorized or explicitly open
- [ ] Outcome READY or NOT_READY recorded; Codex 0 findings or reported

## 10. Reconciliation with the approved contract (P3.02 S5)

- CL-19 C: READY needs both coverage gates (the library gate with the REQ-086 amendment and the CLI assembly gate) on Windows, hosted CI and the NanoPi R76S.
- CL-20: live checks one per command in JSON mode, one MQTT run against the authorized test broker, `--ParseOstHessen` only in JSON mode; the app-host smoke tests and real signal checks of S1 and S3 are unchanged.
