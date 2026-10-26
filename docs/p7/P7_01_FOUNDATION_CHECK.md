# P7.01 – Foundation Check

**Status (2026-10-26): in progress; S1 to S3 recorded, the baseline run (S4) and the plan reconciliation (S5) follow.** Plan: [P7_01_FOUNDATION_CHECK_PLAN.md](P7_01_FOUNDATION_CHECK_PLAN.md).

User instruction (2026-10-26, chat): "Start the implementation of the phase P7. Execute step by step. Commit and push the results of every step".

## 1. Records recheck (S1)

- P6 accepted by the user on 2026-10-26 on candidate `12e7205` ([P6.11 acceptance record](../p6/P6_11_ACCEPTANCE.md)); pull request #9 merged into `develop` on 2026-10-26, merge commit `de9d4ba3123dcd8a25b8cb0d8d42724f20748843` ([P7 handoff](../p6/P7_HANDOFF.md) section 1).
- Pull request #10 (P6 post-merge records and the P7 plans) merged by the user on 2026-10-26T20:21:49Z, merge commit `8009f1c413ead6b150f2c71bec8f669e81be5558`, head `85e02ec82e6f58b1a5a0d806cd173e6013e253ce` (read with `gh pr view 10` after `git fetch`).
- `git merge-base --is-ancestor de9d4ba origin/develop`: true. `origin/develop` = `8009f1c`.
- Commits in `de9d4ba..8009f1c`, all documentation-only (`git diff --name-only de9d4ba 8009f1c` lists only Markdown files: AGENTS.md, DEVELOPMENT_PLAN.md, PHASE_P1_PLAN.md, PHASE_P6_PLAN.md, PHASE_P7_PLAN.md, README.md, `docs/p6/*.md`, `docs/p7/*.md`):

| Commit | Subject | Classification |
| --- | --- | --- |
| `d7a677a` | P6.11 A11: post-merge checkpoint, P6 complete | Documentation-only |
| `7801852` | P7: phase plan (planning only, Codex-reviewed to 0 findings) | Documentation-only |
| `7aa4342` | P7 plan: PR #10 review fixes (freshness member position, totals vs rates) | Documentation-only |
| `d4e1b5c` | P7: step plans P7.01-P7.12 (Codex-reviewed to 0 findings) | Documentation-only |
| `bbc50a9` | P7 plans: PR #10 review fixes (CRC scope, single MOSMIX_S download) | Documentation-only |
| `85e02ec` | P7 plans accepted by the user on 2026-10-26 | Documentation-only |
| `8009f1c` | Merge pull request #10 | Merge |

- Planning gate (PHASE_P7_PLAN.md section 5), first part: the twelve step plans are written and Codex-reviewed to 0 findings (five rounds), the two pull-request review rounds on pull request #10 are fixed, and the user accepted the P7 plans on 2026-10-26. The second part (reconciliation of P7.03 to P7.12 with the approved contract) belongs to P7.02 S6.

## 2. Extraction (S2)

| Field | Value | Source |
| --- | --- | --- |
| Integration target | `8009f1c413ead6b150f2c71bec8f669e81be5558` (`develop` head at branch creation; documentation-only successor of the P6 merge `de9d4ba`) | `git rev-parse origin/develop`, section 1 |
| Package identity | `WeatherParser.Parsing` `0.1.0-initial`, unpublished; contents `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE`, `schemas/export/v1/weatherparser-export-v1.xsd` | P7 handoff section 1, package check (section 4) |
| SDK / runtime | SDK 10.0.402 (`global.json`, `rollForward` `disable`), runtime 10.0.13 | `dotnet --version`, `dotnet --list-runtimes` |
| Coverage tools | Microsoft.CodeCoverage 18.11.0 (`Directory.Packages.props`), dotnet-coverage 18.11.0 (`.config/dotnet-tools.json`) | Files |
| Export contract | v1, C-01 to C-119 (frozen; additive optional members only, C-03, C-105); forecast runs C-66 to C-81 | [P2 export contract](../p2/P2_EXPORT_CONTRACT.md) |
| Other contracts | Selection SC-01 to SC-23; observation OC-01 to OC-14; OsthessenWetter OW-01 to OW-16 (OW-11 amended) | P7 handoff section 1 |
| Reused building blocks | `ICatalogDownloader` (fixed 30-second request timeout, SC-09), `IForecastRunBuilder`, `IMosmixLEvidenceReader`, `IMosmixSEvidenceRefresher`, the MOSMIX_L URI composition of SC-12, `MosmixKmzBuilder` in the tests, the real serializers and the v1 schema, the P5.07/P6.07 parser pattern | P7 handoff sections 2 and 4 |
| P6 coverage at acceptance | 8486/8489 native blocks of `WeatherParser.Parsing.dll` on `12e7205` | P7 handoff section 1 |
| Baseline for P7 | Remeasured in S4 | Section 4 |

SDK servicing check (2026-10-26T20:24Z): the official release metadata https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json lists `latest-release` 10.0.14 of 2026-10-25, `latest-sdk` 10.0.403 and `latest-runtime` 10.0.14. The pinned SDK 10.0.402 (runtime 10.0.13) is one servicing release behind. An update is a separate user decision (plan risk (b)); P7 continues on the pinned SDK, which keeps the baseline comparable.

## 3. Inherited limitations (S2)

Every open item of [P6_11_ACCEPTANCE.md](../p6/P6_11_ACCEPTANCE.md) section 4, [P7_HANDOFF.md](../p6/P7_HANDOFF.md) section 7 and every item inherited with owner P7 from P2, P4 and P5 ([P5_11_ACCEPTANCE.md](../p5/P5_11_ACCEPTANCE.md) section 4, [P4 selection contract](../p4/P4_SELECTION_CONTRACT.md) section 5):

| Inherited item | P7 disposition | Check |
| --- | --- | --- |
| MOSMIX element verification and freshness per product (P4) | Done in P7 | P7.02 (MF-01, MF-09, MF-18), P7.05 (T-P7-23), P7.07 and P7.08 (T-P7-13) |
| Full MOSMIX parsing, time alignment and units; SC-20 re-verified against the schema (P4 section 5) | Done in P7 | P7.02 (MF-07), P7.04, P7.05 |
| Forecast-only quantities as additive contract decisions (P2) | Decided in P7 | P7.02 (MF-11), P7.03 |
| Builders and root validation implement the same rules separately (P2) | Guarded again in P7 | P7.03 and P7.09 equivalence tests through the real serializers |
| Coarse XML cancellation (C-10, P2) | Carried forward, accepted limitation | Restated in the P7.12 limitations |
| Hosted `debian13-arm64` is a container; target evidence only from the NanoPi R76S; release hardware acceptance | Kept separate; release acceptance P9 | P7.11 records NanoPi evidence apart from hosted CI |
| Packed package not byte-reproducible | Carried forward, P9 | Restated in the P7.12 limitations |
| DI under load (P4) | Carried forward, P8 and P3 | Restated in the P7.12 limitations |
| OsthessenWetter source conditions (OW-14) | Unchanged, owner the user; P8 and P3 must not redistribute | Restated in the P7.12 limitations and the P8 handoff |
| Only the documented OsthessenWetter page revision is supported (REQ-045) | Unchanged; P6 scope | Carried forward |
| OsthessenWetter stale displays not detected (REQ-043); coordinates consumer-supplied (OW-04); precipitation, gusts and trends not exported (OW-06); registration consequence for consumers without OsthessenWetter; live check single snapshot (OW-15) | Unchanged; P6 scope | Carried forward; the registration consequence also applies to the P7 options and is revisited in P7.10 |
| `RWS_10` time stamp not corrected (OC-05); calm-wind assumption (OC-06); `now` quality control (OC-07); no freshness filter for observations (OC-04); P5 live parse single snapshot | Unchanged; P5 scope | Carried forward |
| Scheduling recommendations (OC-12, OW-14) | External orchestration; P7 adds per-product retrieval guidance (MF-16) | P7.10 README |
| Example CLI `--ParseDWD`, `--ParseOstHessen`, `--ParseMOSMIX --Product`, configuration, exit codes | P3 | Carried forward |
| MQTT payloads and topics (observations and forecasts) | P8 | Carried forward; P7 provides the product-specific `source` (C-68) |
| Characterization evidence over existing behavior | Recorded; the P7 plans separate characterization from Red | P7 verification record |
| Publication, package ID, feed, release | P9 | Carried forward |
| Example-CLI exposure of the MOSMIX_S evidence refresh (SC-22) | Unchanged, owner the user and P3 | Restated in the P7.12 limitations |
| Selection live check single snapshot (SC-16) | Recorded limitation | Carried forward |
| `de_DE.UTF-8` locale not installed on the target; culture checks use ICU | Recorded platform fact | P7.11 NanoPi run |
| P1 limitations | Unchanged | Carried forward |

No item is without a disposition.

## 4. Branch, seed commit and baseline run (S3, S4)

- Branch `features/develop/P7` from `origin/develop` `8009f1c`; status passages "P7 started" in AGENTS.md, README.md, DEVELOPMENT_PLAN.md and PHASE_P1_PLAN.md.
- README package check (CONTRIBUTING.md section 4) on the working tree: README.md (SHA-256 `569409D674B7C9CAFC6344AF93D806C1F86D9EF8B4DBF7743C89C9A6D0C346AA`), LICENSE and the export schema byte-identical in the package; packed SHA-512 equals the restored hash; package consumer 41/41; the consumer lock was not committed.
