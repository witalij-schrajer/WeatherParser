# P8.09 – Integrated Coverage and Platform Verification: Implementation Plan

**Status: planned on 2026-10-28; accepted by the user on 2026-10-28 with the phase plan; complete on 2026-10-29 with outcome READY ([record](P8_09_INTEGRATED_VERIFICATION.md)).** Refines the P8.09 row of section 5 and section 7 of [PHASE_P8_PLAN.md](../../PHASE_P8_PLAN.md).

## 1. Goal and Scope
Goal: verify the P8 candidate as a whole on Windows, in hosted CI and on the NanoPi R76S, and decide READY or NOT_READY.
In scope: candidate commit; `ci-local.ps1` on Windows against the integration target; the hosted CI run of the candidate push; the NanoPi R76S run of `ci-local.ps1` on the exact candidate SHA (with the user's authorization), including the package consumer tests of P8.07; the record `docs/p8/P8_09_INTEGRATED_VERIFICATION.md`; status passages.
Out of scope with owner: live checks (MQ-14: none in P8); acceptance → P8.10.

## 2. Preconditions and Inputs
- P8.03 to P8.08 complete and committed.
- Tools: `scripts/ci-local.ps1`; the device procedure of CONTRIBUTING.md section 7 (an empty `artifacts/local-nuget` directory before building outside `ci-local.ps1`; the device script checks out the candidate SHA, not the branch tip).
- Required user authorization: the NanoPi R76S run on the candidate; a new candidate after review fixes needs a new authorization.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Candidate | The head after P8.08 / a later head | The head after P8.08; any later code change makes a new candidate and repeats S1 to S3 | Orchestrator, recorded | S1 |
| Live checks | None (MQ-14) / one per source | None: P8 adds no retrieval | User (MQ-14) | — |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p8/P8_09_INTEGRATED_VERIFICATION.md` | Create | Candidate, runs, counters, platform outcomes, READY/NOT_READY |
| `PHASE_P8_PLAN.md`, status passages | Change | Status |
| `docs/p8/P8_VERIFICATION.md` | Change | P8.09 section |

## 5. Procedure
### S1 Windows
- `pwsh ./scripts/ci-local.ps1 -Commit <candidate> -TargetRef <integration-target> -RunSettings coverage.runsettings`; exit code, run directory, block counters (strict improvement against the remeasured baseline, at least 80 %), test counts, package identity.

### S2 Hosted CI
- The CI run of the candidate push (`gh run list`, `gh run view`); job outcomes per platform; the hosted `debian13-arm64` container recorded as not target evidence.

### S3 NanoPi R76S (authorized)
- `ci-local.ps1` on the device on the exact candidate SHA; exit code, counters, test counts, runtime and OS versions; evidence directory outside the repository.

### S4 Record
- READY when S1 to S3 pass; otherwise NOT_READY with the failing check; Codex five-angle review of the record until 0 findings; commit and push on the user's instruction.

## 6. Tests and Test IDs
| Step-local case | T-P8 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 to S3 | T-P8-16 | REQ-017, REQ-084, REQ-085, REQ-086 | Coverage gate and target-platform behavior |

## 7. Evidence to Record
- Run directories, exit codes, counters, hosted run IDs, device versions, Codex rounds

## 8. Risks and Limitations
- (a) A review fix after S3 makes a new candidate; S1 to S3 are repeated and the NanoPi rerun needs a new authorization (P7 precedent)

## 9. Acceptance Criteria
- [ ] Windows, hosted CI and NanoPi R76S outcomes recorded separately
- [ ] Coverage gate passed on the candidate
- [ ] READY or NOT_READY recorded; Codex 0 findings or reported
