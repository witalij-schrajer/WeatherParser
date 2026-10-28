# P8.01 – Foundation Check

**Status (2026-10-28): complete, outcome READY (section 5).** Plan: [P8_01_FOUNDATION_CHECK_PLAN.md](P8_01_FOUNDATION_CHECK_PLAN.md).

User instruction (2026-10-28, chat): "P8 implementation".

## 1. Records recheck and commit classification (S1)

- P7 accepted by the user on 2026-10-28 on candidate `2f1115b` ([P7.12 acceptance record](../p7/P7_12_ACCEPTANCE.md) section 6); pull request #11 merged into `develop` on 2026-10-28, merge commit `20b2ef0a62e8a2a85c7f1a28b7428cb45b8dbeec` ([P8 handoff](../p7/P8_HANDOFF.md) section 1).
- Pull request #12 (P7 post-merge records, the P8 plans and the AGENTS.md subsection "Plan generation") merged by the user on 2026-10-28T22:02:19Z, merge commit `8ed51c725333168d6e072369343c4eb449f8356c`, head `a6ae93ef7ae86abc96fd205a7c546d529ecf4e6d` (read with `gh pr view 12` after `git fetch`). Plan risk (a) did not occur: the P8 plans are on `develop`.
- `git merge-base --is-ancestor 20b2ef0 origin/develop`: true. `origin/develop` = `8ed51c7`.
- `git diff 20b2ef0 origin/develop -- src/` is empty: no production code of the library changed since the P7 merge. Files changed in `20b2ef0..8ed51c7` (`git diff --name-status`): `.github/review/prompt-documentation.md`, `.github/review/prompt-implementation.md`, `.github/workflows/review.yml` (added), `.github/workflows/ci.yml` (changed), `tests/WeatherParser.Parsing.Tests/ParserEndpointValidatorTests.cs` (changed), and Markdown files only otherwise (AGENTS.md, CONTRIBUTING.md, DEPENDENCIES.md, DEVELOPMENT_PLAN.md, PHASE_P1_PLAN.md, PHASE_P7_PLAN.md, PHASE_P8_PLAN.md, README.md, `docs/ci/CODEX_REVIEW_WORKFLOW_VERIFICATION.md`, `docs/p7/*.md`, `docs/p8/*.md`).

| Commit | Subject | Classification |
| --- | --- | --- |
| `717b3f5` | P7.12 A11: post-merge checkpoint, P7 complete | Documentation (README.md changed: counts as code for the CI classification, packed readme) |
| `28a1858` | P7.12: name the post-merge documentation pull request #12 in the acceptance record | Documentation |
| `230205c` | CI: automatic Codex review when a pull request is marked ready for review | CI configuration and documentation |
| `b4e09ae` | Tests: cover the null guard of the ParserEndpointValidator constructor | Tests |
| `5944184` | Codex review workflow: pin Codex 0.165.0, add the verification record | CI configuration and documentation |
| `06369f0` | CI: run the three CI jobs only when code changed | CI configuration and documentation |
| `e7ab744` | CI verification record: hosted run 40760414865 on 06369f0 (code push, all jobs passed) | Documentation |
| `d8878f4` | CI verification record: documentation-only push skipped the CI jobs (run 40760983367) | Documentation |
| `ff40dc9` | Merge pull request #13 | Merge |
| `43866f6` | P8 planning: phase plan and step plans P8.01 to P8.10 | Documentation |
| `f35e2c0` | Merge remote-tracking branch 'origin/develop' into features/develop/P7-post-merge | Merge |
| `51a321f`, `0c79fb5`, `90aa095`, `b6a734e`, `d52cc64`, `92cbe58`, `52a0725` | PR #12 review rounds 1 to 6 and the plan acceptance | Documentation |
| `f8f3c04` | AGENTS.md: plan generation default approach (user decision dated 2026-10-28) | Documentation |
| `615248f`, `527db11`, `2bafe93`, `a6ae93e` | PR #12 review rounds 9 to 11 and the acceptance of the round-9 plan changes | Documentation |
| `8ed51c7` | Merge pull request #12 | Merge |

- Effect on the contracts: none. The only non-documentation changes are the CI configuration (review workflow, "CI only for code changes") and one added test of the existing constructor guard; no export, selection, observation, OsthessenWetter or forecast contract row changed. The handoff assumption "`20b2ef0` or its documentation-only successor" is adjusted: the integration target is `8ed51c7`, a successor with CI-configuration and test changes but no library change (section 5).
- Planning gate (PHASE_P8_PLAN.md section 5), first part: the ten step plans are written, reviewed by the automatic Codex review of pull request #12 to 0 findings (rounds 6, 7, 8, 10 and 11) and accepted by the user on 2026-10-28, including the round-9 plan change. The second part (reconciliation of P8.03 to P8.10 with the approved contract) belongs to P8.02 S5.

## 2. Extraction (S2)

| Field | Value | Source |
| --- | --- | --- |
| Integration target | `8ed51c725333168d6e072369343c4eb449f8356c` (`develop` head at branch creation; successor of the P7 merge `20b2ef0` without library change, section 1) | `git rev-parse origin/develop` |
| Package identity | `WeatherParser.Parsing` `0.1.0-initial`, unpublished; contents `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE`, `schemas/export/v1/weatherparser-export-v1.xsd` | P8 handoff section 1, package check (section 4) |
| SDK / runtime | SDK 10.0.402 (`global.json`, `rollForward` `disable`); only the SDK is pinned | `global.json`, `dotnet --list-sdks` |
| Coverage tools | Microsoft.CodeCoverage and dotnet-coverage as pinned in `Directory.Packages.props` and `.config/dotnet-tools.json` (unchanged since P7) | Files, `git diff 20b2ef0 8ed51c7 --stat` |
| Export contract | v1, C-01 to C-127 (frozen; additive optional members only, C-03, C-105); MQTT payload roots C-01, `source` grammar C-50 and C-68, supported roots C-88, registration C-107 to C-109; schema SHA-256 `6288f1a7dad2909477f9e104621bd8fd04706e09e115742832efba8e2628a0d2` | [P2 export contract](../p2/P2_EXPORT_CONTRACT.md), P8 handoff section 1 |
| Other contracts | Selection SC-01 to SC-23; observation OC-01 to OC-14; OsthessenWetter OW-01 to OW-16; forecast MF-01 to MF-18 | P8 handoff section 1 |
| Services P8 reuses | `IJsonExportSerializer`, `IXmlExportSerializer`, the five export roots (`ExportDocument`, `ObservationDataset`, `ForecastRun`, `CdcStationList`, `MosmixLocationList`), `AddWeatherParsing`, the package-consumer helpers `TestConsumerServices` and `ConsumerSelectionSupport` | P8 handoff sections 2 and 4, repository code |
| CI rules in force | Since pull request #13: the three CI jobs run only when code changed (Markdown and `docs/` only skip them, root `README.md` counts as code); the automatic Codex review runs when a pull request is marked ready for review | AGENTS.md, CONTRIBUTING.md section 6 |
| P7 coverage at acceptance | 10246/10249 native blocks of `WeatherParser.Parsing.dll` on `2f1115b` | P8 handoff section 1 |
| Baseline for P8 | Remeasured in S4 | Section 4 |

SDK servicing check (2026-10-28): the official release metadata https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json lists `latest-release` 10.0.14 of 2026-10-25, `latest-sdk` 10.0.403, support phase `active`; the pinned SDK 10.0.402 belongs to 10.0.13 of 2026-10-13. An update is a separate user decision; P8 continues on the pinned SDK, which keeps the baseline comparable.

## 3. Inherited limitations (S2)

Every open item of [P7_12_ACCEPTANCE.md](../p7/P7_12_ACCEPTANCE.md) section 4, [P8_HANDOFF.md](../p7/P8_HANDOFF.md) section 7 and every item inherited with owner P8 from P2 and P4 to P6 (the table of PHASE_P8_PLAN.md section 1 is the minimum):

| Inherited item | Source | P8 disposition | Check |
| --- | --- | --- | --- |
| MQTT payloads and topics (all roots) | P2.12 section 6, P4.12, P5.11, P6.11, P7.12 section 4 | Done in P8 | P8.03 to P8.06 |
| Product-specific forecast sources for topics and the complete retained run (C-68, REQ-062) | P7.12 section 4 | Done in P8 | MQ-05, P8.04, P8.06 |
| MQTT duplicate-handling rule of REQ-062 (C-55) | P2 export contract C-55 | Decided and documented in P8 | MQ-15, P8.06, P8.08 |
| OsthessenWetter redistribution restriction (OW-14) | P6.11 section 4 | Owner the user; P8 serializes synthetic datasets only and publishes nothing | MQ-10, P8.06, P8.08 (README) |
| DI under load | P4.12, P5.11 (owners P8, P3) | Done in P8 | MQ-12, P8.07 |
| Registration consequence of the options types for every consumer | P7.12 section 4 | Integrated consumer and README in P8 | P8.07, P8.08, MQ-09 |
| Schema compatibility note (C-105, C-121) | P7.12 section 4 | README in P8, carried to P9 | P8.08 |
| Builders and root validation implement the same rules separately | P2, inherited | Carried forward; P8 adds no change | P8.10 limitations |
| Coarse XML cancellation (C-10) | P2, inherited | Carried forward, accepted limitation | P8.10 limitations |
| Hosted `debian13-arm64` is a container; target evidence only from the NanoPi R76S | P5.11 to P7.12 | Kept separate; release hardware acceptance P9 | P8.09 |
| Packed package not byte-reproducible | P5.11 to P7.12 | Carried forward, P9 | P8.10 limitations |
| MOSMIX_S evidence-refresh CLI exposure (SC-22) | P4, P7.12 | P3 (unchanged) | Carried forward |
| P7.06 TDD deviation | P7.12 section 4 | Disposed by the user's P7 acceptance; stays documented | None in P8 |
| MOSMIX_S one download per parse, first interval period before `issued_at` (MF-13), CRC-32 only on forecast paths, elements not exported in v1, `pressure_reference` `unknown` for MOSMIX, single-snapshot live parses and resource measurement | P7.12 section 4 | Recorded decisions and limitations; unchanged by P8 | Carried forward |
| OsthessenWetter revision, stale displays, not exported quantities, consumer coordinates, single live snapshot (REQ-043, REQ-045, OW-04, OW-06, OW-15) | P6.11 section 4 | Recorded; unchanged by P8 | Carried forward |
| DWD observation limitations (`RWS_10` nominal period OC-05, calm wind OC-06, `QN` unchanged OC-07, no freshness filter OC-04, single P5 live snapshot and synthetic pressure path OC-13) | P5.11 section 4 | Recorded; unchanged by P8 | Carried forward |
| Selection live check single snapshot (SC-16) | P4.12 | Recorded | Carried forward |
| Example CLI, configuration, exit codes, CLI cancellation and timeout bounds, broker transport | P3 | P3 | Carried forward |
| Publication, package ID, feed, release | P9 | P9 | Carried forward |
| P1 limitations | P1.12 | Unchanged | Carried forward |

No item is without a disposition.

## 4. Branch, seed commit and baseline run (S3, S4)

- Branch `features/develop/P8` from `origin/develop` `8ed51c7`; status passages "P8 started" in AGENTS.md, README.md, DEVELOPMENT_PLAN.md and PHASE_P1_PLAN.md.
- README package check (CONTRIBUTING.md section 4) on the seed working tree (LF bytes, identical to the committed blob of the seed commit): README.md (SHA-256 `5A8EC9B869FF304861BC4A8C8F46D31AC9391F712FDE1F3AFDB3B00A421F1BCB`), LICENSE and the export schema byte-identical in the package; packed SHA-512 `uWJObFon4fM1v7B3VCZiS7q7CJaC/Vk3VUntRUorv7x8+aOQjYwBly3ePfYFQv3kayLS9yD8QJntHcGxc/vf+w==` equals the restored hash; package consumer 47/47; the consumer lock was not committed. The committed-revision check is the `ci-local.ps1` run of S4.
- Seed commit `5525ea11073c9cb5d347bbf889119fd592d635ca` (status passages and the S1 to S3 record), pushed; draft pull request #14 into `develop` opened; Auto-fix on.
- `pwsh ./scripts/ci-local.ps1 -Commit 5525ea11073c9cb5d347bbf889119fd592d635ca -TargetRef 8ed51c725333168d6e072369343c4eb449f8356c -RunSettings coverage.runsettings`: exit 0, 2026-10-28T22:06:07Z to 22:07:26Z, run directory artifacts/ci/5525ea1-20261028T220607Z (gitignored); commits.json: commit `5525ea1...`, targetCommit `8ed51c7...`, two-pass.
- Tests: Parsing 2947 of 2954 passed, 0 failed (the seven opt-in live and resource checks not executed), SchemaGeneration 46, Coverage 99, Coverage.Runner 56, package consumer 47.
- Gate passed: 10248/10249 (99.99 %) at or above the baseline 10248/10249 of `8ed51c7` (the candidate changes Markdown files only); gate-result.json SHA-256 `79b88132aead3f0704190e94548ba313491fd7fa9b4d7a6e677569d537ea5ab9`; packed SHA-512 `ERtQChZQsl8OFFChz06Z/9vna3MHBHgYgTgQSy9PuBeAPQoMtGkRQ4WRQMnSikp4TcBeywUJqQEMdLP9WUfRCg==`.
- Hosted CI for the seed commit `5525ea1`: run 40777302720 (`pull_request`, created 2026-10-28T22:06:02Z; the jobs run because the commit changes `README.md`).
- Baseline for the P8 code steps: **10248/10249** native blocks for target `8ed51c7`. It is two covered blocks above the P7 gate 10246/10249 on `2f1115b`, with the same total: the test added by pull request #13 (`b4e09ae`, the null guard of the `ParserEndpointValidator` constructor) covers two blocks of unchanged production code. The baseline is valid while the pull-request base is `8ed51c7` and `coverage.runsettings` is unchanged; every P8 code step must strictly improve the unrounded ratio (one uncovered block remains).

## 5. Plan adjustments and outcome (S5)

- `PHASE_P8_PLAN.md` section 1 receives the "P8.01 extraction" table; status line updated.
- Adjustment: the integration target is `8ed51c7` (the merge of pull request #12) instead of "`20b2ef0` or its documentation-only successor": the successor also contains CI-configuration and test changes (section 1) but no library change, so the contracts and the handoff foundation stand; the baseline is the remeasured 10248/10249 instead of the P7 gate 10246/10249.
- Recorded for the user (not a plan adjustment): SDK 10.0.403 / runtime 10.0.14 is available since 2026-10-25; P8 stays on the pinned 10.0.402 unless the user decides an update (it would change `global.json`, `DEPENDENCIES.md` and CONTRIBUTING.md section 1 and require a new baseline).
- Outcome: **READY**. Every field is resolved, the baseline run passed, no library code changed since `20b2ef0`, and every inherited limitation has a disposition.
