# P6.01 – Foundation Check

**Status (2026-10-25): in progress. S1 and S2 recorded; S3 (branch, status passages, seed commit, draft pull request), S4 (baseline run) and S5 (plan reconciliation and review) follow.** Plan: [P6_01_FOUNDATION_CHECK_PLAN.md](P6_01_FOUNDATION_CHECK_PLAN.md).

User instruction (2026-10-25, chat): "I have merged the PR. start the p6 implementation. Executes step by step, commit and push changes after each step".

## 1. Records recheck (S1)

- P5 accepted by the user on 2026-10-25 on candidate `fd461c8` ([P5.11 acceptance record](../p5/P5_11_ACCEPTANCE.md) section 6); pull request #7 merged into `develop` on 2026-10-25T08:24:59Z, merge commit `29fd5533449898f1b14513b11c65a0afa1cf049a` (section 8).
- Pull request #8 (P5 post-merge records and the P6 plans) merged by the user on 2026-10-25T09:35:24Z, merge commit `8d8079a4238514f7c9bcfc3846829e2408938ae3`, head `7e07ab84e62c1acd5bf04892021abc468b29780e` (read with `gh pr view 8` after `git fetch`).
- `git merge-base --is-ancestor 29fd553 origin/develop`: true. `origin/develop` = `8d8079a`.
- Commits in `29fd553..8d8079a`, all documentation-only (`git diff --name-only 29fd553 8d8079a` lists only Markdown files):

| Commit | Subject | Classification |
| --- | --- | --- |
| `7384230` | P5.11 A11: post-merge checkpoint, P5 complete | Documentation-only |
| `75b2667` | P5.11 A11: record ci-local run on checkpoint 7384230 | Documentation-only |
| `80dc47b` | P6: phase plan draft and planning check of the OsthessenWetter page | Documentation-only |
| `a77ee93` | P6 plan: Codex review fixes (rounds 4, 1, 1, 0 findings) | Documentation-only |
| `608497d` | P6: step plans P6.01-P6.11 and planning gate (PR #8 review) | Documentation-only |
| `6945234` | P6 plans: asynchronous page-body reading and S1 registration (PR #8 review) | Documentation-only |
| `7e07ab8` | Update docs/p6/P6_06_RETRIEVAL_BOUNDARY_PLAN.md (the user, on GitHub: the reviewer's suggestion that `Read_HttpClientTimeout_ThrowsPageUnavailable` covers the timeout before the response headers and the stalled-body case is `Read_BodyStalled_DownloaderRequestTimeout_ThrowsPageUnavailable`) | Documentation-only |
| `8d8079a` | Merge pull request #8 | Merge |

- Planning gate (PHASE_P6_PLAN.md section 5), first part: the eleven step plans are written and Codex-reviewed to 0 findings, and the pull-request review of `6945234` ended with APPROVE WITH CONDITIONS; its one condition is applied in `7e07ab8`. The second part (reconciliation with the approved contract) belongs to P6.02.

## 2. Extraction (S2)

| Field | Value | Source |
| --- | --- | --- |
| Integration target | `8d8079a4238514f7c9bcfc3846829e2408938ae3` (`develop` head at branch creation; documentation-only successor of the P5 merge `29fd553`) | `git rev-parse origin/develop`, section 1 |
| Package identity | `WeatherParser.Parsing` `0.1.0-initial`, unpublished | P6 handoff section 1 |
| SDK / runtime | SDK 10.0.402 (`global.json`, `rollForward` `disable`), runtime 10.0.13 | `dotnet --version`, `dotnet --list-runtimes` |
| Coverage tools | Microsoft.CodeCoverage 18.11.0 (`Directory.Packages.props`), dotnet-coverage 18.11.0 (`.config/dotnet-tools.json`) | Files |
| Contracts | Export contract v1 C-01 to C-119 (frozen; additive optional members only, C-03, C-105); selection contract SC-01 to SC-23; observation contract OC-01 to OC-14 | P6 handoff section 1 |
| Reused building blocks | `ISourceTimeNormalizer.InferDate` and `NormalizeComplete`, `IObservationDatasetBuilder`, the real serializers and the v1 schema, the P5.07 parser pattern, `ICatalogDownloader` | P6 handoff sections 2 and 4 |
| P5 coverage at acceptance | 7491/7494 native blocks of `WeatherParser.Parsing.dll` | P6 handoff section 1 |
| Baseline for P6 | Remeasured in S4 | Section 4 |

SDK servicing check (2026-10-25): the official release metadata https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json lists `latest-release` 10.0.13 of 2026-10-13, `latest-sdk` 10.0.402 and `latest-runtime` 10.0.13. The pinned SDK is current; no update is needed.

## 3. Inherited limitations (S2)

Every open item of [P5_11_ACCEPTANCE.md](../p5/P5_11_ACCEPTANCE.md) section 4 and [P6_HANDOFF.md](../p5/P6_HANDOFF.md) section 7:

| Inherited item | P6 disposition | Check |
| --- | --- | --- |
| Builders and root validation implement the same rules separately (P2) | Guarded again in P6 | P6.08 equivalence tests through the real serializers |
| Coarse XML cancellation (C-10, P2) | Carried forward, accepted limitation | Restated in the P6.11 limitations |
| Hosted `debian13-arm64` is a container; target evidence only from the NanoPi R76S; release hardware acceptance | Kept separate; release acceptance P9 | P6.10 records NanoPi evidence apart from hosted CI |
| Packed package not byte-reproducible | Carried forward, P9 | Restated in the P6.11 limitations |
| DI under load (P4) | Carried forward, P8 and P3 | Restated in the P6.11 limitations |
| OsthessenWetter source conditions and attribution (REQ-092) | Clarified as far as possible in P6 | P6.02 (facts, OW-14), P6.09 (README), P6.11 (status) |
| `RWS_10` time stamp not corrected (OC-05) | DWD only; unchanged by P6 | Carried forward, restated in P6.11 |
| Calm-wind assumption (OC-06) | DWD only; unchanged by P6 | Carried forward |
| `now` quality control not completed (OC-07) | DWD only; consumers | Carried forward |
| No freshness filter (OC-04) | Same rule for OsthessenWetter (REQ-043, OW-12) | P6.05 and P6.07 old-display tests; README in P6.09 |
| The P5 live parse is a single snapshot; pressure path synthetic only | Recorded limitation | Carried forward |
| Scheduling recommendation (OC-12) | External orchestration; P6 adds its own recommendation (OW-14) | P6.09 README |
| Example CLI `--ParseDWD`, configuration, exit codes | P3 (with `--ParseOstHessen`) | Carried forward |
| MQTT payloads and topics for observations | P8 (with the OsthessenWetter datasets) | Carried forward |
| Characterization evidence over existing behavior | Recorded; the P6 plans use characterization only where behavior exists (P6.08, P6.09) | P6 verification record |
| Publication, package ID, feed, release | P9 | Carried forward |
| MOSMIX element verification, freshness per product, full parsing | P7 | Carried forward |
| Example-CLI exposure of the MOSMIX_S refresh (SC-22) | User, P3 | Carried forward |
| Selection live check single snapshot (SC-16) | Recorded limitation | Carried forward |
| Forecast-only quantities not in the export contract | P7 | Carried forward |
| `de_DE.UTF-8` locale not installed on the target; culture checks use ICU | Recorded platform fact; P6 culture tests (`de-DE`, `tr-TR`) rely on ICU | P6.10 NanoPi run |
| P1 limitations | Unchanged | Carried forward |

No item is without a disposition.

## 4. Baseline run (S4)

Pending.

## 5. Plan adjustments and outcome (S5)

Pending.
