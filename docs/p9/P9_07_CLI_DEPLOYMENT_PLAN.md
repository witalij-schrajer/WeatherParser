# P9.07 – CLI Deployment Model: Implementation Plan

**Status: planned on 2026-10-31 on the user's instruction ("Generate the step plans for P9"); under review through the automatic Codex review of pull request #17, not yet accepted; P9 has not started.** Refines the P9.07 row of section 5 of [PHASE_P9_PLAN.md](../../PHASE_P9_PLAN.md).

## 1. Goal and Scope
Goal: a documented, verified example deployment of the CLI on Debian 13 ARM64 under RL-07 and RL-08: publish, installation layout, service user, configuration and credential file permissions, invocation without a terminal.
In scope: a standalone fixture host for offline target runs (Proposed: `tests/WeatherParser.Cli.FixtureHost`, an executable that serves the synthetic files of a given directory on the loopback interface at a fixed or announced port, logs requests to stderr and exits on SIGINT or SIGTERM; the CLI tests' `LoopbackHttpServer` is an internal in-process helper and cannot serve this purpose); the CLI acceptance artifact of P9.06 (the RL-07 publish for `linux-arm64`, framework-dependent or self-contained as decided); an optional install script (Proposed: `scripts/install-cli-debian.sh`) that creates the layout and permissions without starting or scheduling anything; its tests; the deployment section of the operating guide draft (completed in P9.08); a first verification on the NanoPi R76S in a disposable location with the user's authorization.
Out of scope with owner: cron and n8n examples and log handling → P9.08; the release-candidate acceptance run → P9.10; installing the .NET runtime on the user's systems → the user (documented only).

## 2. Preconditions and Inputs
- P9.06 complete; P9.02 approved RL-07 and RL-08.
- The user's authorization of the NanoPi R76S run for S4 (what may be created where, as which user, whether `sudo` may be used), recorded verbatim; without it the step stops at S4.
- Inputs: REQ-004 to REQ-008, REQ-060, REQ-064; CL-04 (no implicit configuration search), CL-06 (credential file owner and mode, Linux `statx`), CL-11 (exit codes); the README section "Example CLI".

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Install script | Script / documented commands only | A script only if RL-07 approves it: idempotent, `set -eu`, English comments, prints every action, refuses to overwrite an existing configuration or credential file, never writes a secret, never creates a cron entry or a service; otherwise documented commands with the same checks | User (RL-07), orchestrator records | S3 |
| Script tests | Run in the hosted Debian container / only on the NanoPi | Tests in the hosted Debian 13 ARM64 container job (as root there, a disposable file system) plus the authorized NanoPi run; the container is not target evidence | Orchestrator, recorded | S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `tests/WeatherParser.Cli.FixtureHost/…` (Proposed) and its tests | Create | Fixture host and synthetic fixture set (placeholder IDs and values, no copied source file, no OsthessenWetter value) |
| `WeatherParser.slnx`, `WeatherParser.Ci.slnf`, `coverage.runsettings` (or the CLI gate's module filter), `CONTRIBUTING.md` sections 2, 3, 5 and 7, README project list | Change | Per the AGENTS.md consistency table: the new project, its exclusion from the CLI gate (P9 handoff section 5: a new executable in the test graph is excluded), the target procedure |
| `scripts/install-cli-debian.sh` (Proposed, if approved) | Create | Layout, user, permissions |
| Script tests (location decided with the existing script-test practice) | Create | Behavior tests of the script |
| `docs/OPERATIONS.md` (Proposed) | Create | Deployment section (draft) |
| `CONTRIBUTING.md` section 7, `DEPENDENCIES.md` | Change | Per the consistency table if scripts or CI jobs change |
| `docs/p9/P9_VERIFICATION.md` | Change | P9.07 section |

## 5. Procedure
Script and tests by qwen from briefs; documentation by the orchestrating agent.

### S1 Fixture host
**Red**: `FixtureHost_ServesFileFromDirectory_OnLoopbackOnly`, `FixtureHost_UnknownPath_Returns404`, `FixtureHost_PathTraversal_Rejected`, `FixtureHost_AnnouncesPortOnStdout`, `FixtureHost_SigtermOrCtrlC_ExitsZero` (Linux signal case skipped on Windows with the reason, as the existing signal tests do), `Cli_ParseDwd_AgainstFixtureHost_ProducesDocument` (a process test of the built CLI with a configuration pointing at the host).
**Green**: the host (qwen) and the synthetic fixture set; the exclusion from the CLI gate verified by the gate's module list in a `ci-local.ps1` run.

### S2 Publish
The CLI acceptance artifact from the P9.06 release build (the RL-07 publish for `linux-arm64` against the same-run package); record the output layout, size and checksum; a smoke run of `WeatherParser --Help` on the NanoPi is part of S4.

### S3 Install script (if approved)
**Red**: `Install_CreatesLayoutUserAndPermissions` (directories, symlink, service user without login shell, configuration directory mode, credential file absent unless supplied), `Install_ExistingConfiguration_NotOverwritten`, `Install_SecondRun_Idempotent`, `Install_NoCronEntryOrServiceCreated`, `Install_ExampleConfiguration_HasPlaceholdersOnly`.
**Green**: the script.
**Refactor**: with all tests green.

### S4 Target check (authorized)
On the NanoPi R76S in a disposable location: verify the CLI acceptance artifact's checksum and install it per RL-07; start the fixture host; as the service user and without a terminal (`setsid`, stdin from `/dev/null`) run `--Help` (exit 0), a usage error (exit 2), a credential file with mode `0644` and one owned by another user (rejected per CL-06, exit 2), and one `--SelectDWD` against the fixture host in JSON mode (exit 0, document on stdout, logs on stderr); remove the disposable installation afterwards and record it.

### S5 Review
Per-step Codex review; commit and push.

## 6. Tests and Test IDs
| Step-local case | T-P9 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P9-13 (offline evidence class) | REQ-083, REQ-084 | A standalone, loopback-only fixture source for target runs |
| S3 | T-P9-08 | REQ-060, REQ-064 | Layout, user and permissions; nothing overwritten, nothing scheduled |
| S4 | T-P9-08, T-P9-13 | REQ-004, REQ-006, REQ-007 | Noninteractive runs as the service user on the target |
| all | T-P9-16 | REQ-085 | Red evidence |

## 7. Evidence to Record
- Publish command and output, Red names and failures, container run, the authorization verbatim, NanoPi commands, exit codes and channel contents (no address, credential or private path), cleanup, authorship, Codex round

## 8. Risks and Limitations
- (a) A self-contained publish needs a rebuild for every runtime security update; recorded in the guide if chosen
- (b) The CL-06 foreign-owner check needs root to prepare; on the NanoPi it runs only if the authorization covers `sudo`

## 9. Acceptance Criteria
- [ ] Fixture host tested with recorded Red and excluded from the CLI gate; acceptance artifact recorded; script (if approved) tested with recorded Red
- [ ] Authorized target check passed and cleaned up
- [ ] Codex 0 findings or reported
