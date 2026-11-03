# P9.06 – Release Artifacts, Checksums and Release Workflow: Implementation Plan

**Status: planned on 2026-10-31 on the user's instruction ("Generate the step plans for P9"); reviewed through the automatic Codex review of pull request #17 to 0 findings; accepted by the user on 2026-10-31 ("Accept the plans"); accepting the plans neither starts P9 nor approves any RL row; P9 starts only on a separate instruction.** Refines the P9.06 row of section 5 of [PHASE_P9_PLAN.md](../PHASE_P9_PLAN.md).

## 1. Goal and Scope
Goal: a release build procedure that builds the release artifacts once from a candidate commit, tests them, checksums them, stores them with an evidence record and checks before promotion that a later commit's build inputs equal the candidate's, without any publication right.
In scope: the release build (Proposed: `scripts/build-release.ps1`, and a `workflow_dispatch` workflow `.github/workflows/release-build.yml` if RL-02 and RL-06 approve one) producing the `.nupkg`, the decided symbols, the private CLI acceptance artifact of RL-08 (the RL-07 publish of the candidate commit against the same-run package, built in every RL-08 option and attached to the release only under option B), `SHA256SUMS` over all of them and the evidence record; the promotion check (Proposed: `scripts/check-promotion.ps1`): tree equality of the build inputs between the candidate and a given commit and checksum equality of the stored artifacts; a check that the release version equals the decided version source; CONTRIBUTING.md section 6 and `DEPENDENCIES.md` per the consistency table.
Out of scope with owner: the tag, the GitHub release and the feed push → P9.12 with authorization; the deployment layout → P9.07. The publish command of the CLI acceptance artifact is the RL-07 command as approved in P9.02; P9.07 verifies it on the target and changes it here only through a recorded fix.

## 2. Preconditions and Inputs
- P9.05 complete; P9.02 approved RL-02, RL-05, RL-06 and RL-08.
- Inputs: REQ-003, REQ-005, REQ-013; DEVELOPMENT_PLAN.md P1 ("executing untrusted PR code must not receive privileged publication rights").

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Build-input definition for promotion | As approved in RL-06 | Every path except Markdown files under `docs/` and Markdown files not packed into the package; the root `README.md` counts as input; the list lives in one place used by the script and its tests | User (RL-06), list recorded by the orchestrator | S2 |
| Workflow permissions | Default token permissions / explicit `contents: read` and no secrets | Explicit `permissions: contents: read`, no repository or environment secret, `workflow_dispatch` only, artifacts uploaded as workflow artifacts; publication rights are not part of this workflow | Orchestrator, recorded (RL-02) | S1 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `scripts/build-release.ps1`, `scripts/check-promotion.ps1` (Proposed) | Create | Release build and promotion check, English comments, exit codes |
| `.github/workflows/release-build.yml` (Proposed, if approved) | Create | Read-only release build workflow with pinned actions |
| `tests/WeatherParser.Coverage.Runner.Tests/…` or a new script test project folder (decided with the existing script-test practice of `ci-local.ps1`) | Create | Tests of both scripts over synthetic repositories and artifacts |
| `CONTRIBUTING.md` section 6, `DEPENDENCIES.md` | Change | Procedure, pinned actions |
| `docs/p9/P9_VERIFICATION.md` | Change | P9.06 section |

## 5. Procedure
Scripts, workflow and tests by qwen from briefs; documentation by the orchestrating agent.

### S1 Release build
**Red**: `BuildRelease_ProducesPackageSymbolsSumsAndEvidence`, `BuildRelease_CliAcceptanceArtifact_BuiltAgainstSameRunPackage` (the published CLI's resolved `WeatherParser.Parsing` equals the same-run `.nupkg` by hash, and the artifact is listed in `SHA256SUMS`), `BuildRelease_VersionMismatch_Fails` (decided version source versus requested release version), `BuildRelease_TestFailure_ProducesNoArtifacts`, `BuildRelease_EvidenceRecord_NamesCommitTreeSdkAndRuns`, `ReleaseWorkflow_HasReadOnlyPermissionsAndNoSecrets` (a static check of the workflow file, if a workflow is approved).
**Green**: the script (and workflow) building once from the given commit in a clean clone, running the release test set, packing, writing `SHA256SUMS` and the evidence record.

### S2 Promotion check
**Red**: `Promotion_SameInputs_DocsOnlyDifference_Passes`, `Promotion_ChangedSourceFile_Fails`, `Promotion_ChangedPackedReadme_Fails`, `Promotion_ChangedBuildProps_Fails`, `Promotion_ModifiedArtifact_ChecksumFails`, `Promotion_CandidateNotAncestorOfTarget_Fails`.
**Green**: the promotion script.
**Refactor**: with all tests green.

### S3 Dry run and review
One dry run of the release build on the current phase head (no publication, artifacts kept only as workflow or local artifacts and recorded as a dry run, not as the release candidate); hosted CI; per-step Codex review; commit and push.

## 6. Tests and Test IDs
| Step-local case | T-P9 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P9-05, T-P9-06, T-P9-01 | REQ-003, REQ-013 | Artifacts, sums, evidence; no publication right; version check |
| S2 | T-P9-05 | REQ-005 | Promotion passes only for equal inputs and intact checksums |
| all | T-P9-16 | REQ-085, REQ-086 | Red evidence; gates |

## 7. Evidence to Record
- Red names and failures, counts, dry-run artifact hashes, workflow run ID (if any), pinned action versions, authorship, Codex round

## 8. Risks and Limitations
- (a) Workflow artifacts expire; the release candidate's artifacts are stored in a location the user decides in P9.11 (for example a local folder outside the repository plus the evidence record), and their checksums are recorded in the repository
- (b) The promotion check compares Git trees; untracked local files never enter a release build because the build runs in a clean clone

## 9. Acceptance Criteria
- [ ] Every listed test green with recorded Red
- [ ] No publication right, secret or feed credential in any script or workflow
- [ ] Dry run recorded; CONTRIBUTING.md and DEPENDENCIES.md updated; Codex 0 findings or reported

## 10. Reconciliation with the approved contract

The release contract [P9_RELEASE_CONTRACT.md](../../p9/P9_RELEASE_CONTRACT.md) was approved by the user on 2026-10-31 ("Approve all"; RL-19 open). Where this plan and the contract differ, the contract wins. Changes for this step:
- RL-06 A: the release build runs as a manual run of a new workflow (Proposed `.github/workflows/release-build.yml`, `workflow_dispatch` only, `contents: read`, no secrets) on the hosted `ubuntu-x64` runner; the artifacts are downloaded, checksummed in `SHA256SUMS` and recorded; RL-06 C (attestations) is not applicable while the repository is private (RL-19).
- RL-02 A: no publication step; the user pushes to nuget.org with their own scoped key.
- Precondition added: the user's RL-19 decision, because the candidate's package metadata depends on it.
- 2026-10-31: RL-19 A decided (repository private); RL-06 C not applicable; the release candidate carries no repository URL.
