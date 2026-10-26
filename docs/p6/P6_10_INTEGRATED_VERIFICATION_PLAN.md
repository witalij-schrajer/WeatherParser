# P6.10 – Integrated Coverage and Platform Verification: Implementation Plan

**Status: executed on 2026-10-26 on candidate `4373399` ([record](P6_10_INTEGRATED_VERIFICATION.md)): outcome READY.** Refines the P6.10 row of section 5 of [PHASE_P6_PLAN.md](../../PHASE_P6_PLAN.md). Reconciled on 2026-10-25 with the OsthessenWetter contract approved by the user on 2026-10-25 ([contract](P6_OSTHESSEN_CONTRACT.md), option A in every row): OW-15 as planned.

## 1. Goal and Scope
Goal: verify one P6 candidate end to end on Windows, in hosted CI and on the NanoPi R76S, and optionally with one authorized live parse, and record the outcome READY or NOT_READY.
In scope: candidate and integration-target identification; `ci-local.ps1` on Windows; the hosted CI run of the candidate push with artifacts; the NanoPi R76S run (with the user's authorization, procedure of CONTRIBUTING.md section 7 and P5.10); the `Europe/Berlin` time-zone data on Debian 13; the optional live check (OW-15) with its own authorization; record `docs/p6/P6_10_INTEGRATED_VERIFICATION.md`.
Out of scope with owner: acceptance → P6.11; release hardware acceptance → P9.

## 2. Preconditions and Inputs
- P6.03 to P6.09 complete and committed; the candidate is the committed head.
- Required user decisions: authorization of the NanoPi run; separately, authorization of the live check (one request to the real page).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| NanoPi run | Authorize / skip with a named blocker | Authorize (T-P6-17 is mandatory) | User | S3 |
| Live check | Authorize one request / skip and record the limitation | The user's choice; the source conditions (OW-14) may make the user prefer to skip until clarified | User | S4 |
| New-candidate rule | A production change after S1 requires S1 to S3 again on the new candidate / partial rerun | Full S1–S3 rerun, as in P5.10 | Orchestrator, recorded | all |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `tests/WeatherParser.Parsing.Tests/Live/*` | Change | OsthessenWetter mode `WEATHERPARSER_LIVE_CHECK_MODE=osthessen` with a one-request budget (S0, test-first) |
| `docs/p6/P6_10_INTEGRATED_VERIFICATION.md` | Create | Candidate, runs, counters, platform outcomes, live result or skip, outcome |
| `docs/COMPATIBILITY.md` | Change | P6 scope |
| `docs/p6/P6_VERIFICATION.md`, `PHASE_P6_PLAN.md` | Change | Section and status |

## 5. Procedure
### S0 Live-check mode (test-first, before the candidate is fixed)
- **Red**: the runner tests for the new mode (enabled only with both variables, budget one request, the page not written to the repository, artifacts only to the gitignored run directory, the P4 and P5 live tests skipped in this mode). **Green**: qwen implements; the orchestrating agent verifies; Codex reviews.

### S1 Windows
- `pwsh ./scripts/ci-local.ps1 -Commit <candidate-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings`; counters, gate result and SHA-256, package hash, test counts recorded.

### S2 Hosted CI
- The candidate push's run ID and each job outcome; artifacts downloaded; each `commits.json` names the candidate and the target.

### S3 NanoPi R76S (authorized)
- Bundle transfer with SHA-256 on both sides, `prepare-debian-arm64.sh`, `ci-local.ps1`, run directory copied back, device directory removed; outcomes compared with Windows; the time-zone tests (T-P6-06) passing on Debian recorded explicitly.

### S4 Live check (authorized separately)
- One request; the summary (station count, matched names, datasets, failures, notices) recorded; the page itself not committed; never repeated without a new authorization.

### S5 Record and review
- Record written; Codex five-angle review; outcome READY when S1 to S3 passed (and S4 passed or is recorded as skipped), otherwise NOT_READY with open items.

## 6. Tests and Test IDs
| Step-local case | T-P6 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1, S2 | T-P6-16 | REQ-086 | Native block coverage ≥ 80 % and strict improvement against the target |
| S3 | T-P6-17 | REQ-017, REQ-084 | NanoPi outcome separate from Windows and hosted ARM64 |
| S0, S4 | T-P6-18 | REQ-083, REQ-092 | Live check explicitly enabled and authorized, one request |

## 7. Evidence to Record
- Candidate and target SHAs, commands, exit codes, run directories, counters, hashes, CI run ID and job outcomes, NanoPi transfer hashes, live summary or skip reason, authorship, Codex rounds

## 8. Risks and Limitations
- (a) The hosted `debian13-arm64` job is not target evidence (inherited) — kept separate from S3
- (b) The live page differs from the documented revision — recorded as drift; the supported revision stays the documented one; a parser update is a new decision

## 9. Acceptance Criteria
- [ ] S1 to S3 passed on the same candidate; outcomes separate per platform
- [ ] Live check passed or recorded as skipped with its limitation
- [ ] Record complete; Codex 0 findings or reported; outcome recorded
