# P9.12 – Release on `master` and Publication: Implementation Plan

**Status: planned on 2026-10-31 on the user's instruction ("Generate the step plans for P9"); under review through the automatic Codex review of pull request #17, not yet accepted; P9 has not started.** Refines the P9.12 row of section 5 of [PHASE_P9_PLAN.md](../../PHASE_P9_PLAN.md).

## 1. Goal and Scope
Goal: the accepted release candidate's artifacts released from `master` and, only with the user's authorization, published, with a post-publication consumer check.
In scope: the release pull request `develop` → `master`; the RL-06 promotion check on the `master` merge commit; the annotated tag `v<version>`; the GitHub release with the promoted artifacts, `SHA256SUMS` and release notes naming the candidate and the tagged commit; the feed publication by the user (RL-02); the post-publication consumer check; the next prerelease version on `develop`; the follow-up of switching on package validation with the released version as baseline (RL-04).
Out of scope with owner: production deployment on the user's systems → the user; announcement or other outward communication → the user.

## 2. Preconditions and Inputs
- P9.11 complete: P9 accepted and the phase pull request merged into `develop` by the user.
- Separate authorizations, each recorded verbatim: opening the release pull request, the tag, the GitHub release, the publication; the agent never handles a feed API key (the user pushes, or approves a protected-environment job if RL-02 approved one).
- Inputs: REQ-003, REQ-013; RL-01, RL-02, RL-04 to RL-06, RL-08; the P9.10 evidence record and stored artifacts.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Promotion check failure | New candidate / user override | A new candidate with P9.10 and P9.11 repeated (RL-06); no override is offered | User (RL-06) | S2 |
| Release pull request review | Same loop as phases / none | The automatic review loop to 0 findings (`review.yml` covers pull requests into `master`) before the user merges | Orchestrator, recorded | S1 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p9/P9_12_RELEASE.md` | Create | Release pull request, review rounds, promotion check output, tag, release URL, publication record, consumer check |
| `CHANGELOG.md`, `README.md`, `PHASE_STATUS.md`, `DEVELOPMENT_PLAN.md`, `AGENTS.md` | Change (on `develop`, after the release) | Released version, feed installation instructions verified, next version |
| Project file or `Directory.Build.props` | Change (on `develop`, after the release) | Next prerelease version; package validation baseline per RL-04 |

## 5. Procedure
Documentation by the orchestrating agent; the version and validation changes after the release by qwen test-first.

### S1 Release pull request
Open the pull request `develop` → `master` with the user's authorization; review loop to 0 findings; the user merges.

### S2 Promotion check
Run `check-promotion.ps1` (P9.06) with the candidate commit, the `master` merge commit and the stored artifacts; record the output; with RL-03 A rebuild the candidate commit cleanly and compare byte for byte.

### S3 Tag and GitHub release (authorized)
Create the annotated tag on the `master` merge commit and the GitHub release with the promoted artifacts, `SHA256SUMS` and the release notes; record the URL.

### S4 Publication (by the user)
The user publishes the promoted `.nupkg` (and symbols) to the feed; the agent records the user's confirmation and the feed's package page.

### S5 Post-publication check
A clean consumer outside any checkout restores the published package from the feed by version, verifies the package hash against `SHA256SUMS` (the feed's repository signature aside, decided in P9.02), and passes the package consumer tests; recorded.

### S6 Follow-ups on `develop`
On a post-release branch with its pull request: the next prerelease version; package validation switched on with the released version as baseline (**Red**: `PackageValidation_BreakingChangeAgainstBaseline_Fails` over a fixture; **Green**: the configuration); README and changelog updates; status documents.

## 6. Tests and Test IDs
| Step-local case | T-P9 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S2 | T-P9-05 | REQ-005 | Promotion only for equal inputs and intact checksums |
| S3, S4 | T-P9-06 | REQ-003, REQ-013 | Tag, release and publication only with authorization |
| S5 | T-P9-12 | REQ-013 | The published package consumed without a checkout |
| S6 | T-P9-04 | REQ-016 | Compatibility check active from the released baseline |

## 7. Evidence to Record
- Authorizations verbatim, pull request and review rounds, promotion output, tag and release URL, feed page, consumer check results, follow-up pull request

## 8. Risks and Limitations
- (a) A published version cannot be replaced on nuget.org (only unlisted); a defect after publication requires a new version
- (b) Feed processing delays the post-publication check; it is repeated until the package is available, without a fixed interval imposed on the user

## 9. Acceptance Criteria
- [ ] Release pull request reviewed to 0 findings and merged by the user
- [ ] Promotion check passed; tag and release created with authorization; publication by the user recorded
- [ ] Post-publication consumer check passed; follow-ups on `develop` in their own pull request
