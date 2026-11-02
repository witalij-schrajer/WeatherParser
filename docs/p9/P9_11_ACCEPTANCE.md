# P9.11 – P9 Acceptance Record

**Status (2026-11-02): evidence audit ACCEPTED on release candidate `41bcffe` (section 5); the automatic review of pull request #18 and the user's acceptance are pending (sections 6 and 7).** Plan: [P9_11_INTEGRATED_VERIFICATION_AND_ACCEPTANCE_PLAN.md](P9_11_INTEGRATED_VERIFICATION_AND_ACCEPTANCE_PLAN.md). Integrated verification: [P9_11_INTEGRATED_VERIFICATION.md](P9_11_INTEGRATED_VERIFICATION.md).

## 1. Review identity

| Item | Value |
|---|---|
| Release candidate | `41bcffee2201888cc2bea9b773025ecdbda04ce8` on `features/develop/P9`: the P9.10 candidate `86e77eb` plus the P9.10 record and the README correction decided by the user ("Fix README, new build"); release build run 41498465182, `.nupkg` `206db3ce…0aee`, `.snupkg` `5acfd0c5…5664`, CLI `linux-arm64` archive `eb925ae9…58e7` |
| Pull request | [#18](https://github.com/witalij-schrajer/WeatherParser/pull/18) into `develop`, draft while implementing |
| Integration target | `ed7fc7a03db2f5554c490451f918da9fc6828c84` (merge base; `develop` is at `78690d0` after the merge of pull request #19, which added only the identical `release-build.yml`) |
| Contract | [P9 release contract](P9_RELEASE_CONTRACT.md) RL-01 to RL-20, approved on 2026-10-31 ("Approve all"); RL-19 A and no nuget.org publication (the user, 2026-10-31); the CL-09 amendment of the [P3 CLI contract](../p3/P3_CLI_CONTRACT.md) ("Fix it now", 2026-10-31) |
| Verification | [P9 verification record](P9_VERIFICATION.md) sections 1 to 11; run records [P9.01](P9_01_FOUNDATION_CHECK.md), [P9.07](P9_07_TARGET_RUN.md), [P9.09](P9_09_BROKER_AND_SHUTDOWN_CHECKS.md), [P9.10](P9_10_ARM64_RELEASE_ACCEPTANCE.md), [P9.11](P9_11_INTEGRATED_VERIFICATION.md) |

## 2. Requirement and test evidence matrix (T-P9-01 – T-P9-16)

Platform evidence for every row: the P9.11 runs on `41bcffe` (Windows and NanoPi R76S `ci-local.ps1` exit 0, hosted CI run 41498427219 with all three jobs passed; library gate 11102/11103, CLI gate 2875/2920 on Windows and 2884/2920 on the NanoPi). "Step" names the section of the verification record or the run record with the evidence.

| T-P9 ID | Requirements | Tests and evidence | Step | Status |
|---|---|---|---|---|
| T-P9-01 | REQ-013, RL-01 | `PackageMetadataTests`; `BuildReleaseScriptTests` (version mismatch fails the release build) | P9.05, P9.06 | PASSED; `Authors` "WeatherParser contributors" and no repository URL (RL-19 A) |
| T-P9-02 | REQ-012, REQ-090, RL-01, RL-03 | `PackageContentsTests` (exact member list, the packed `README.md` byte-identical to the committed file) | P9.05 | PASSED |
| T-P9-03 | REQ-005, RL-03 | `ComparePackagesScriptTests`; two clean packs on Windows and two hosted Linux release builds `CONTENT-IDENTICAL` | P9.01, P9.05, P9.06 | PASSED with the named limitation (entry timestamps of four metadata members; the CLI archive not reproducible) |
| T-P9-04 | REQ-016, RL-04 | `PublicApiInventoryTests`, `PublicApiAspectTests` with Red fixtures for added, removed and changed members and parameter direction | P9.05 | PASSED; nullable annotations not inventoried (section 4) |
| T-P9-05 | RL-06 | `BuildReleaseScriptTests`, `BuildReleaseRegressionTests`, `CheckPromotionScriptTests`, `CheckPromotionRegressionTests`; real promotion checks (P9.06 dry runs; `86e77eb` → `8470bd7` `PROMOTABLE`) | P9.06, P9.11 | PASSED |
| T-P9-06 | REQ-003, REQ-013, RL-02, RL-05 | `release-build.yml` is `workflow_dispatch` only with `contents: read`, no secret, no push, tag or release step (`ReleaseWorkflow_HasReadOnlyPermissionsAndNoSecrets` in `BuildReleaseScriptTests`); pull request builds have no publication rights | P9.06 | PASSED |
| T-P9-07 | REQ-009, REQ-086, RL-09 to RL-11 | Restore, build and all tests after the updates; both gates under the amended REQ-086 (`CoverageGateReconciliationTests`, `GatedInputSetsTests`, `CoverageGateGatedInputsTests`, `RunnerApplicationReconciliationTests` and the related parser and reader tests) on Windows, the NanoPi R76S and hosted CI | P9.03, P9.04, P9.11 | PASSED |
| T-P9-08 | REQ-004, REQ-006, REQ-064, RL-07 | P9.07 without root; P9.10 S3 as the service user per RL-07 A, CL-06 accepted and rejected files; `ProcessCredentialFileTests` | P9.07, P9.10 | PASSED |
| T-P9-09 | REQ-007, REQ-008, RL-12 | P9.10 S5: deployment, credential file, cron line with `logger`, journal, logrotate (`--debug`), n8n command line as a shell command, update and rollback | P9.08, P9.10 | PASSED; n8n itself not run, logrotate as a dry run |
| T-P9-10 | REQ-060, REQ-062, REQ-063, RL-13 | `BrokerIntegrationTests` (5 of 5 with TLS and credentials on Windows and the NanoPi); the CLI against the test broker: success, wrong credential, wrong CA, plaintext port; QoS 1 and retain observed | P9.09, P9.10 | PASSED; one broker product, a self-signed test CA |
| T-P9-11 | REQ-007, RL-14 | RL-14 A: recorded limitation, no check | P9.09 | PASSED as the decided limitation |
| T-P9-12 | REQ-001, REQ-012, REQ-013, REQ-017 | `WeatherParser.Package.Tests` 131/131 against the release `.nupkg` of `86e77eb` without the source on Windows and the NanoPi; on `41bcffe` inside `ci-local.ps1` on both platforms and the artifact equivalence of P9.11 section 4 | P9.10, P9.11 | PASSED |
| T-P9-13 | REQ-004, REQ-005, REQ-084, RL-15 | P9.10 S3: every command offline and live, exit codes 0, 1, 2, 3 and 4, real serializers, UTC and `Europe/Berlin`, separate channels, SIGINT and SIGTERM without a temporary file; `FixtureHostTests` | P9.07, P9.10 | PASSED |
| T-P9-14 | REQ-006, REQ-084, RL-15 | P9.10 S4: 374 cron invocations over 36.5 hours, none missing, 5 reported live item failures, peak memory at most 202,492 KB, clean finish | P9.10 | PASSED; longer than approved (section 4) |
| T-P9-15 | REQ-090, REQ-092, REQ-093, OW-14, RL-16 to RL-18 | README "Release and installation" (corrected in `41bcffe`), `docs/OPERATIONS.md` with verification statuses, `CHANGELOG.md` with known limitations and owners, OW-14 stated; local links checked; `ReadmeCliSectionTests`; English only | P9.08, P9.10, P9.11 | PASSED |
| T-P9-16 | REQ-085, REQ-086 | Red → Green → Refactor recorded per code slice in the verification record; both gates per the amended REQ-086 on every platform; NanoPi results recorded separately | P9.03 to P9.11 | PASSED with the recorded process deviations (section 4) |

## 3. Contract summary

RL-01 to RL-18 and RL-20 are approved with option A and implemented as recorded in the verification record; RL-19 A (the repository stays private) and the deferred nuget.org publication are the user's decisions of 2026-10-31. The CL-09 amendment (`OBSERVATION-NO-USABLE-READING` as a `--ParseDWD` item failure) was approved and implemented test-first in P9.10.

## 4. Limitations and open items

The product limitations with their owners are listed in [CHANGELOG.md](../../CHANGELOG.md) ("Known limitations"); they are part of this acceptance. The items below need the user's decision or acknowledgement:

| Item | Owner |
|---|---|
| Hosted CI for the first candidate `86e77eb` ran on the documentation-only head `8470bd7` instead of the exact candidate (plan S1) | Decided by the user: "Keep 86e77eb, run head" |
| The P9.10 target runs were made on `86e77eb`, not on the release candidate `41bcffe`; the artifacts differ only in the README and in commit-derived identity data ([P9.11 record](P9_11_INTEGRATED_VERIFICATION.md) section 4); the release `.nupkg` of `41bcffe` was not restored from a folder feed without a source checkout | Decided by the user with "Fix README, new build"; presented again with the acceptance |
| P9.10 deviations: the scheduled series ran 36.5 hours instead of the approved 24, with correspondingly more hourly live runs; the setup ran once on `2a084be` and twice on `86e77eb`, so the one-time live runs of the authorization were made three times; the root parts ran as scripts prepared by the orchestrating agent | The user |
| MOSMIX retrieval failures name only `CATALOG-RETRIEVAL-FAILED`, not the HTTP status or timeout (three MOSMIX_S failures in the series could not be explained) | The user (a later change) |
| The example CLI's offline fixture host (`tests/WeatherParser.Cli.FixtureHost/`) is not in the RL-11 A list of CLI gated inputs, so a change to it alone does not restore strict improvement of the CLI gate | The user |
| The public API inventory does not record nullable annotations (the rejected part of a P9.05 review finding) | The user |
| Package ID `WeatherParser.Parsing`, the feed nuget.org and the procedure (a push by the user with a scoped key from their own machine) are decided (RL-01 A, RL-02 A); the publication itself is deferred by the user (2026-10-31, RL-19 A) and needs a separate authorization; `Authors` stays "WeatherParser contributors", to be confirmed before any publication | The user, before any publication |
| Pull request #19 into `develop` (the release workflow file only) was merged by the user with a red CLI gate, because `develop` lacked the P9.04 gate amendment ("I'll merge it as is") | Recorded |
| The `coverage-reconciliation.json` entries for pull request #18 must be removed after the merge (REQ-086 amendment of 2026-10-31) | P9.12 or the post-merge documentation branch |
| Process deviations recorded per step in the verification record: P9.04 hand edits by the orchestrating agent without an AGENTS.md exception and a reconstructed Red; P9.05 a partly invalid Red and fixes by the orchestrating agent after failed qwen briefs; P9.06 the round-1 deviation; P9.07 the station-list fixtures written by the orchestrating agent after two failed qwen briefs, the replaced `--ParseDWD` process test (added in P9.10) and the incident of an unquoted here-document that executed quoted commands (effects removed and recorded); P9.09 the test broker prepared by the orchestrating agent instead of the user | The user |
| The NanoPi runs of `ci-local.ps1` used SDK 10.0.402 with runtime 10.0.13 from `scripts/prepare-debian-arm64.sh`; runtime 10.0.14 was exercised by the installed CLI in P9.10 | Recorded |
| Publication on nuget.org, a GitHub release and tag, the release pull request into `master` | P9.12 (the user decided no publication now) |

## 5. Audit decision

**ACCEPTED** (orchestrating agent's evidence audit, 2026-11-02, on candidate `41bcffe`):
- every T-P9 row is PASSED with tests or recorded run evidence, with the limitations named in its row;
- the integrated verification is READY on `41bcffe` on Windows, hosted CI (the exact candidate, all three jobs) and the NanoPi R76S; the P9.10 target evidence applies through the shown artifact equivalence and the user's decision;
- both coverage gates passed on every platform under the amended REQ-086 with the approved reconciliation entries;
- every RL row is approved and implemented, and every limitation has an owner;
- status documents are updated in the same change as this record (AGENTS.md consistency table).

This audit decision is not the user's acceptance.

## 6. User decision

Pending: the user is asked for the acceptance after the automatic review of pull request #18 reports 0 findings (section 7).

## 7. Ready for review and review rounds

- A9: pull request #18 marked ready for review under AGENTS.md "Phase implementation" step 2 on head `736cdb9`.
- Automatic review round 1 (head `736cdb9b6d10ff79d38b5145fbf6ac1c6e472d79`, [run 41501987104](https://github.com/witalij-schrajer/WeatherParser/actions/runs/41501987104), implementation brief, 85 of 118 changed files are code): 1 finding, rejected after verification: [P2] "the reconciliation entries do not cover the current pull request target", because `develop` moved to `78690d0` and the automatic runs pass `base.sha` as the target. Rejected: GitHub reports `base.sha` `ed7fc7a03db2f5554c490451f918da9fc6828c84` for pull request #18 (`gh api repos/witalij-schrajer/WeatherParser/pulls/18`, 2026-11-02), the automatic run 41498427219 on the candidate passed `CI_TARGET` `ed7fc7a…` to all three jobs, and both gates applied the entries there; `ed7fc7a` is also the merge base of the branch with `develop`. Documentation addition: the [P9.11 record](P9_11_INTEGRATED_VERIFICATION.md) section 1 now states where the target comes from. If GitHub ever reports another base for this pull request, the entries no longer apply and a new entry needs the user's approval (plan section 10).

## 8. Authorship and per-step review

Written by the orchestrating agent on 2026-11-02 from the P9 verification record, the run records and the P9 release contract; not generated by qwen (AGENTS.md). The per-step Codex review rounds of the two P9.11 records are recorded here.
- Per-step Codex review, round 1 (the uncommitted P9.11 records and commit `41bcffe`): 3 findings, all minor, verified and confirmed: [P3] the package ID, feed and procedure were listed as open although RL-01 A and RL-02 A decide them: the item now separates the decided choices from the deferred publication and the `Authors` confirmation; [P3] the NanoPi durations were not traceable: they are the script's own "Run completed in" lines, now quoted; [P3] three status summaries were stale (the `CHANGELOG.md` status named `86e77eb` and its build, the `PHASE_P9_PLAN.md` status still showed the P9.08 hosted run as blocked, the verification record's status line ended at P9.06): reconciled.
- Per-step Codex review, round 2 (the corrected records): 0 findings.
- Automatic review round 2 (head `d15c9cfe1e217e73cb2219b051b7bbbc60f35c06`, [run 41502467467](https://github.com/witalij-schrajer/WeatherParser/actions/runs/41502467467)): 2 findings, both verified and confirmed:
  1. [P2] RL-08 A requires the deployment guide to build the CLI from the tagged source against the released package, but `docs/OPERATIONS.md` only described installing the private acceptance archive. Fixed by the orchestrating agent (documentation): a new subsection "Building the CLI from the tagged source (RL-08 A)" with the checkout of `v<version>`, the released `.nupkg` in the local package source until a publication, a fresh package cache, `dotnet restore` and the framework-dependent `dotnet publish` of the release build, and the check of the resolved package (`project.assets.json` `sha512` against the Base64 SHA-512 of the `.nupkg`); its status in section 11 is Planned for P9.12 (the post-publication check of RL-08 A), the same steps run in every release build.
  2. [P2] `scripts/check-promotion.ps1` did not require the complete release set: an artifact folder whose manifest and evidence agreed on only some artifacts was `PROMOTABLE`. Fixed test-first: Red by qwen in `CheckPromotionRegressionTests` (the fixture now holds the real release set; new tests `Promotion_CompleteReleaseSet_Passes`, `Promotion_ReleaseSetWithoutSymbols_Fails`, `Promotion_ReleaseSetWithExtraArtifact_Fails`, `Promotion_CliArchiveOfOtherRuntime_Fails`, `Promotion_EvidenceWithoutVersion_ExitTwo`, `Promotion_EvidenceWithoutRuntimeIdentifier_ExitTwo`; two corrections by qwen of an evidence fixture it had shortened and then extended) and the fixture of `CheckPromotionScriptTests`; against the unchanged script 5 of 24 failed, exactly the five new negative tests (exit 0 instead of 5 or 2). Green by qwen: `evidence.json` must carry string `version` and `runtimeIdentifier` (exit 2), and `SHA256SUMS` must list exactly `WeatherParser.Parsing.<version>.nupkg`, `WeatherParser.Parsing.<version>.snupkg` and `WeatherParser.Cli.<version>.<runtimeIdentifier>.zip` (`INCOMPLETE-SET <name>`, `UNEXPECTED-ARTIFACT <name>`, exit 5); one correction by qwen to ordinal comparison; after two further briefs that qwen did not follow, the orchestrating agent moved the new block before the `EVIDENCE-COMMIT-MISMATCH` check and removed one empty line (layout only; a recorded deviation). 24 of 24 pass; the real artifacts of `41bcffe` stay `PROMOTABLE`. `CONTRIBUTING.md` section 6 lists the new results.
  - Effect on the candidate: the script and its tests are build inputs (RL-06), so the commit with this fix becomes the new release candidate; the library and the CLI sources are unchanged. Its release build, the artifact comparison and the gate runs on Windows and the NanoPi follow after the review loop.
