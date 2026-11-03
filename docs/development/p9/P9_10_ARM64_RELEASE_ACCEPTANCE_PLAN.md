# P9.10 – ARM64 Release Acceptance: Implementation Plan

**Status: planned on 2026-10-31 on the user's instruction ("Generate the step plans for P9"); reviewed through the automatic Codex review of pull request #17 to 0 findings; accepted by the user on 2026-10-31 ("Accept the plans"); accepting the plans neither starts P9 nor approves any RL row; P9 starts only on a separate instruction.** Refines the P9.10 row of section 5 of [PHASE_P9_PLAN.md](../PHASE_P9_PLAN.md).

## 1. Goal and Scope
Goal: evidence that the release candidate's stored, checksummed artifacts (RL-06) work on the documented target, the NanoPi R76S with Debian 13 and `linux-arm64`, as a package for independent consumers and as the deployed example CLI, including repeated scheduled invocations (RL-15).
In scope: the release candidate build (P9.06 procedure) on the candidate commit; transfer of the artifacts to the NanoPi and checksum verification there; the package consumer against the release `.nupkg` from a local folder feed without a source checkout; the CLI acceptance artifact of RL-08 installed per RL-07; every command offline against the P9.07 fixture host; one authorized live run per command in JSON mode (OsthessenWetter JSON only, OW-14); one MQTT run against the P9.09 test broker; SIGINT and SIGTERM during a run; the scheduled series of RL-15; execution of the operating-guide examples (T-P9-09); `docs/COMPATIBILITY.md` rows.
Out of scope with owner: the full `ci-local.ps1` gate run on the candidate → P9.11 (also on the NanoPi); publication → P9.12.

## 2. Preconditions and Inputs
- P9.07 to P9.09 complete; P9.02 approved RL-15 with the series parameters.
- The user's authorizations, each recorded verbatim: the NanoPi runs, the live checks (number of requests per source), the MQTT run, the scheduled series (duration, interval, live share).
- Inputs: REQ-004, REQ-005, REQ-006, REQ-007, REQ-017, REQ-084, REQ-092; OW-14; CL-11, CL-18; the P3.11 record as the method precedent.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Candidate commit | The phase head after P9.09 / after P9.08 documentation fixes | The phase head after P9.09 with all documentation of P9.08 in place, so that later documentation-only changes stay outside the build inputs where possible (RL-06); a build-input change after this step requires a new candidate and a repeat of this step | Orchestrator, recorded | S1 |
| Fake sources on the target | The P9.07 fixture host / other local serving | The P9.07 fixture host with its synthetic fixture set, started outside the installed CLI, recorded as an offline evidence class | Orchestrator, recorded | S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p9/P9_10_ARM64_RELEASE_ACCEPTANCE.md` | Create | Run record per evidence class (offline, live, broker, scheduled series, hardware), outcome READY or NOT_READY |
| `docs/COMPATIBILITY.md` | Change | Release rows with the target evidence |
| `docs/OPERATIONS.md` | Change | Verification status of every example |
| `docs/p9/P9_VERIFICATION.md` | Change | P9.10 section |

## 5. Procedure
No code change is planned; documentation by the orchestrating agent. A defect is reported to the user and fixed only on a recorded decision, followed by a new candidate.

### S1 Candidate build
Run the release build of P9.06 on the candidate commit; record the evidence record and `SHA256SUMS`; store the artifacts as decided (P9.06 section 8).

### S2 Package consumption without source checkout
On Windows and the NanoPi: verify the checksums; restore the release `.nupkg` from a local folder feed in a consumer outside any checkout; run the package consumer tests (DI, parsers with local fixtures, JSON/XML with the packaged schema, MQTT serialization).

### S3 CLI runs
On the NanoPi, the CLI acceptance artifact installed per RL-07 after its checksum check, as the service user, without a terminal: every command offline against the fixture host (exit codes 0, 2, 1 and 3 provoked as in the P3.11 matrix, stdout and stderr separated, UTF-8, UTC timestamps, `Europe/Berlin` handling of the OsthessenWetter fixture); one live run per command in JSON mode within the authorized request count; one MQTT run against the P9.09 test broker; SIGINT and SIGTERM during a run (exit 4, no temporary file, no success line).

### S4 Scheduled series
A cron entry for the service user as authorized; afterwards record per invocation the exit code and stderr, check for overlapping processes, remaining temporary files and open handles, and the peak resident memory per process (from `/usr/bin/time -v` or the cgroup accounting, decided in the record); remove the cron entry and record the removal.

### S5 Guide examples
Execute every operating-guide example on the target (cron line, n8n command line as a shell command, credential file set-up) and mark its verification status.

### S6 Outcome and review
READY when S2 to S5 pass on the candidate's artifacts; otherwise NOT_READY with the reasons; per-step Codex review; commit and push.

## 6. Tests and Test IDs
| Step-local case | T-P9 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S2 | T-P9-12 | REQ-001, REQ-012, REQ-013, REQ-017 | Independent consumption on Windows and the NanoPi |
| S3 | T-P9-13, T-P9-08 | REQ-004, REQ-005, REQ-084 | Target runs per evidence class |
| S4 | T-P9-14 | REQ-006, REQ-084 | Scheduled series bounded and clean |
| S5 | T-P9-09 | REQ-007, REQ-008 | Guide examples executed |

## 7. Evidence to Record
- Candidate commit and tree hash, artifact checksums on both machines, authorizations verbatim, per-run commands, exit codes and channel summaries, live request counts and `retrieved_at` values, broker observations, series statistics, cleanup, Codex round

## 8. Risks and Limitations
- (a) Live sources can be unavailable; a live failure is recorded with its error ID and repeated only within the authorization
- (b) The series measures this hardware and this period; it is not a load test beyond the decided parameters

## 9. Acceptance Criteria
- [ ] Every run on the candidate's checksummed artifacts; evidence classes recorded separately
- [ ] Outcome READY; compatibility matrix and guide statuses updated
- [ ] Codex 0 findings or reported

## 10. Reconciliation with the approved contract

The release contract [P9_RELEASE_CONTRACT.md](../../p9/P9_RELEASE_CONTRACT.md) was approved by the user on 2026-10-31 ("Approve all"; RL-19 open). Where this plan and the contract differ, the contract wins. Changes for this step:
- RL-15 A approved as planned; the target runs on runtime 10.0.14 or newer (RL-09 A).
