# P2.11 – Integrated Coverage and Platform Verification: Implementation Plan

**Status: plan only (2026-10-20); nothing is implemented or verified yet.** This plan refines the P2.11 row of section 5 and section 8 of [PHASE_P2_PLAN.md](../../PHASE_P2_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief and corrected by qwen in one review round (scope lists, no-TDD statement, artifact rows, evidence destination); the orchestrating agent restored two pairs of path backticks.

## 1. Goal and Scope
Goal: verify one committed P2 candidate end to end — the full two-pass local CI pipeline with the native block-coverage gate against the PR base, the hosted workflow, and the actual Debian 13 ARM64 target (NanoPi R76S) for P2-specific behavior — and audit that every T-P2-01 to T-P2-28 row has real test, TDD and run evidence; produce READY or NOT_READY for P2.12.

In scope:
- candidate and target identification
- local two-pass run
- hosted workflow run on the PR
- target-platform run and P2-specific platform checks
- TDD/evidence audit
- offline check
- the evidence record
- NOT_READY handling

Out of scope (owner):
- new features or contract changes → back to the owning step P2.02–P2.10
- fixes for defects found here → the owning step, test-first, followed by a new candidate
- acceptance decision and P4 handoff → P2.12
- release and hardware acceptance, REQ-084 → P9

## 2. Preconditions and Inputs
- Prerequisite steps: P2.03–P2.10 complete.
- Existing repository facts used:
  - Local pipeline: `pwsh ./scripts/ci-local.ps1 -Commit <candidate-sha> -TargetRef <PR-base-sha> -RunSettings coverage.runsettings`
  - Hosted workflow `.github/workflows/ci.yml` runs on Windows x64, Ubuntu x64 and Debian 13 ARM64 (`debian:trixie` container on a GitHub-hosted ARM64 runner)
  - Target procedure: CONTRIBUTING.md section 7 (target preparation with `scripts/prepare-debian-arm64.sh`, transfer with a git bundle, run command, `-CollectOnly`/`-ResumeFrom` fallback)
- Required user authorization:
  - Authorize NanoPi R76S access, setup and transfer
  - Decide offline proof method

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Target-platform access for this run | authorize NanoPi R76S access, setup and transfer / record an OPEN blocker and ask for an acceptance disposition | Authorize: T-P2-28 needs actual target evidence | User | V4 |
| Offline proof method | run the standard test projects on the target with outbound network blocked for the test process / review that no test uses HTTP clients or sockets plus a successful run without configured endpoints | OPEN – user decides what is practical on the target; Recommended the network-blocked run where the user authorizes it | User | V5 |
| Target locale for culture checks | `de_DE.UTF-8` / `C.UTF-8` / both | Both, if the locales are installed; record which were available | Orchestrator, recorded | V4 |
| Candidate rerun policy after a defect | fix in the owning step and supply a new candidate; repeat V1–V6 / patch evidence only | Fix and new candidate; earlier runs stay recorded as failed | Orchestrator, recorded | V7 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p2/P2_VERIFICATION.md` | Change | P2.11 section with V1–V8 results and the READY/NOT_READY outcome |
| `docs/COMPATIBILITY.md` | Change | Verified Windows, hosted and target results for the candidate, kept separate from requirements |
| `artifacts/ci/<commit>-<UTC timestamp>/` | Create (local, gitignored) | Run directories of V2 and V4; key artifact hashes recorded in `docs/p2/P2_VERIFICATION.md` |

## 5. Procedure
P2.11 changes no product code, so it has no Red/Green/Refactor slices; each verification step below has Action, Roles, Check and an outcome rule.

### V1 – Identify candidate and target
- **Action**: Record the full candidate SHA, the PR number or deliberate target, the full PR-base SHA, the script revision, SDK/runtime versions and the run-settings SHA-256; confirm the candidate diff is not documentation-only.
- **Roles**: Orchestrator.
- **Check**: `git rev-parse <candidate>`, `git rev-parse <PR-base>`, `git diff --stat <PR-base> <candidate>`.
- **Outcome rule**: Missing or ambiguous identities → NOT_READY.

### V2 – Local two-pass run on Windows
- **Action**: Run `pwsh ./scripts/ci-local.ps1 -Commit <candidate-sha> -TargetRef <PR-base-sha> -RunSettings coverage.runsettings`; record exit code, test counts per project, build warnings, package entries and hashes, current/target block counters, all gate verdicts and the run directory.
- **Roles**: Orchestrator runs and records; qwen not applicable (run results).
- **Check**: The script exit code and `gate-result.json`.
- **Outcome rule**: Non-zero exit or any failed gate rule → NOT_READY.

### V3 – Hosted workflow
- **Action**: Record the hosted run of the PR (run ID/URL, three job results) without treating it as target evidence.
- **Roles**: Orchestrator.
- **Check**: The run's job results.
- **Outcome rule**: A failed hosted job is investigated and recorded; it blocks READY until explained or fixed.

### V4 – Actual target run and P2-specific checks
- **Action**: With the user's authorization, run the CONTRIBUTING.md section 7 procedure for the same candidate and target on the NanoPi R76S; additionally record P2-specific results: both serializers' outputs, `Europe/Berlin` resolution (tzdata version) and the defined failure without time-zone data if a safe test exists, culture checks under the available locales, byte-identical schema generation compared with Windows (SHA-256), and the package consumer on the target.
- **Roles**: Orchestrator runs, collects and records; qwen not applicable.
- **Check**: Script exit code, gate result, per-check outputs.
- **Outcome rule**: Without authorization or with a failed check → record an OPEN blocker or failure and NOT_READY unless the user gives an explicit acceptance disposition.

### V5 – Offline behavior
- **Action**: Apply the offline proof method decided in section 3; confirm no weather endpoint, HTTP client or broker is used by the standard tests.
- **Roles**: Orchestrator.
- **Check**: Per the decided method.
- **Outcome rule**: Any network dependence → NOT_READY.

### V6 – TDD and evidence audit
- **Action**: For every T-P2-01 to T-P2-28 row, map the test names, the recorded Red (or characterization) evidence, the run artifact and the commit; confirm `[TestInitialize]`/`[TestCleanup]` with `MockRepository.VerifyAll()` in test classes that use mocks; confirm qwen/orchestrator authorship is recorded per step.
- **Roles**: Qwen may draft the mapping table from the orchestrator's extracted data; the orchestrator verifies every row against the records and test sources.
- **Check**: Test source review and `docs/p2/P2_VERIFICATION.md`.
- **Outcome rule**: A row without real evidence → NOT_READY.

### V7 – Defect handling
- **Action**: For each defect found, record it, return it to the owning step for a regression test first and a fix by qwen, and repeat V1–V6 for the new candidate.
- **Roles**: Orchestrator writes the defect brief; qwen generates the regression test and fix; orchestrator verifies.
- **Check**: The new candidate's runs.
- **Outcome rule**: Open defects → NOT_READY.

### V8 – Record and outcome
- **Action**: Write the P2.11 section of `docs/p2/P2_VERIFICATION.md` with V1–V7 results and the READY/NOT_READY outcome for P2.12.
- **Roles**: Qwen drafts prose from recorded facts; the orchestrator verifies every value.
- **Check**: Document review.
- **Outcome rule**: READY only if V1–V7 have no open failure and every mandatory platform check has evidence or an explicit user disposition.

## 6. Tests and Test IDs
| Step-local case | T-P2 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| V6 | T-P2-26 | REQ-080–083, REQ-085 | Red→Green→Refactor evidence, test setup/cleanup rules and offline behavior for every T-P2 row |
| V5 | T-P2-26 | REQ-083 | Standard tests need no weather HTTP, production sources or brokers |
| V1, V2 | T-P2-27 | REQ-086 | Native block minimum and strict improvement against the remeasured PR base; identities, settings and exclusions comparable; no bootstrap or docs-only bypass |
| V3 | T-P2-27 | REQ-086 | Hosted workflow outcome recorded separately from local and target evidence |
| V4 | T-P2-28 | REQ-005, REQ-017, REQ-084 | Actual P2 serializer, time-zone, schema and package behavior on the Debian 13 ARM64 target, separate from Windows and hosted evidence; missing checks reported explicitly |

## 7. Evidence to Record
- All entries go to the P2.11 section of `docs/p2/P2_VERIFICATION.md`; verified platform results also go to `docs/COMPATIBILITY.md`.
- Identities: full candidate SHA, PR number or deliberate target, full PR-base SHA, script revision, SDK/runtime versions, run-settings SHA-256.
- Commands: `git rev-parse`, `git diff --stat`, `scripts/ci-local.ps1`.
- Exit codes, test counts per project, build warnings, package entries and hashes, current/target block counters, all gate verdicts, run directory.
- Hosted run ID and job results.
- Target host details: OS, SDK/runtime, tzdata, locales.
- P2-specific target outputs: serializers' outputs, `Europe/Berlin` resolution, culture checks, schema generation SHA-256, package consumer on target.
- Offline proof method applied and confirmation of no network dependence.
- T-P2 audit table mapping test names, Red evidence, run artifacts, commits, `[TestInitialize]`/`[TestCleanup]` verification, qwen/orchestrator authorship.
- Defects found, returned to owning step, fixed, and rerun results.
- Authorship and final outcome (READY or NOT_READY).

## 8. Risks and Limitations
- Target access may not be authorized in time — OPEN blocker plus explicit disposition, never relabeled hosted or Windows evidence.
- Coverage strict improvement may fail for an otherwise correct candidate — resolve with meaningful tests in the owning step, never with exclusions or metric padding.
- The target time-zone data or locales may be missing — record precisely and treat as a platform finding.
- Retained run directories are local and gitignored — hashes of key artifacts are recorded in the document.
- A rerun after a fix changes the candidate SHA — all evidence must reference the final candidate.

## 9. Acceptance Criteria
- [ ] Candidate and PR-base identities recorded.
- [ ] Local two-pass gate passed with all five rules.
- [ ] Hosted results recorded.
- [ ] Target run and P2-specific checks recorded or an OPEN blocker with explicit user disposition.
- [ ] Offline proof recorded.
- [ ] Every T-P2 row mapped to real evidence.
- [ ] `docs/COMPATIBILITY.md` updated.
- [ ] READY/NOT_READY stated.
