# P7 Verification Record

**Status (2026-10-26): P7.01 complete, outcome READY (section 1); P7.02: sources retrieved, contract MF-01 to MF-18 drafted and Codex-reviewed to 0 findings, awaiting the user's approval (section 2).**

- Integration target: `8009f1c413ead6b150f2c71bec8f669e81be5558` (documentation-only successor of the P6 merge `de9d4ba`).
- Branch `features/develop/P7`.

## 1. P7.01 – Foundation check

- Record: [P7_01_FOUNDATION_CHECK.md](P7_01_FOUNDATION_CHECK.md), written by the orchestrating agent.
- S1 and S2 recorded (merge checks, commit classification, extraction, limitation mapping, SDK servicing check).
- S3: branch `features/develop/P7` from `origin/develop` `8009f1c`; status passages "P7 started" in AGENTS.md, README.md, DEVELOPMENT_PLAN.md and PHASE_P1_PLAN.md.
- README package check (CONTRIBUTING.md section 4) on the working tree: README.md (SHA-256 `569409D674B7C9CAFC6344AF93D806C1F86D9EF8B4DBF7743C89C9A6D0C346AA`), LICENSE and the export schema byte-identical in the package; packed SHA-512 `SMSzmKob8OgJa8pTuJJsLJG8X9MciprcpESLrZBsGKFmWqMlltKHdUdC7lyp7H5OLoP77t/EbLrZ0AuqlzCcsQ==` equals the restored hash; package consumer 41/41; the consumer lock was not committed.
- Seed commit `edd74fd0aebbde35eb251cf943f54e8ceec70644`, pushed; draft pull request #11 into `develop` opened; Auto-fix on.
- `pwsh ./scripts/ci-local.ps1 -Commit edd74fd0aebbde35eb251cf943f54e8ceec70644 -TargetRef 8009f1c413ead6b150f2c71bec8f669e81be5558 -RunSettings coverage.runsettings`: exit 0, 2026-10-26T20:26:27Z to 20:27:36Z, run directory artifacts/ci/edd74fd-20261026T202627Z (gitignored), two-pass.
- Tests: Parsing 2301 of 2304 passed (the three opt-in live tests not executed), SchemaGeneration 46, Coverage 99, Coverage.Runner 56, package consumer 41.
- Gate passed: 8486/8489 (99.96 %) at or above the baseline 8486/8489 of `8009f1c`; gate-result.json SHA-256 `d2c55afebf59d4460f1c30fd5d4b0fb842cad7cf50b08a73265442f9e780e0ef`; packed SHA-512 `fTHnwPXsnlLWmnUwWAFEP5y0EsYlOAy46ChRXrSWRc7MEQ0F3cd71HrWY+RfBmRboJoiJh8QCBF0gpctijO7yw==`.
- Hosted CI for `edd74fd`: run 40395632527 (`pull_request`).
- Baseline for the P7 code steps: **8486/8489** native blocks for target `8009f1c`, valid while the pull-request base is `8009f1c` and `coverage.runsettings` is unchanged.
- S5: `PHASE_P7_PLAN.md` section 1 table "P7.01 extraction" and status line; status passages "P7.01 complete" in AGENTS.md, README.md, DEVELOPMENT_PLAN.md and PHASE_P1_PLAN.md.
- Codex five-angle review of P7.01 (`p701/codex_r1.md`, outside the repository): 4 findings, all confirmed and fixed by the orchestrating agent: [P2] the limitation mapping grouped P5 and P6 items and lost OW-06 (60-minute mean and extremes), the synthetic-only pressure evidence of OC-13, the CLI cancellation and timeout bounds and the consumer owners (now one row per item with its owner); [P2] DEVELOPMENT_PLAN.md said "P7 not started" and PHASE_P7_PLAN.md "planning only" (both updated); [P3] the extraction table counted six commits without the merge commit; [P3] the runtime was called pinned (only the SDK is pinned).
- Round 2 (`p701/codex_r2.md`): 0 findings.
- README package check on the S5 working tree (README status passage changed): README.md SHA-256 `A3248120AB39171C4C2174266F5711C067E64CC50A2122436B87F70720EAE1B4` identical in the package with LICENSE and the export schema; packed SHA-512 `ZlF9TPPxdZwpQLqfcu3f0CGBkzEn5rWg9Sjs0RaEgSjIk2tqo51BDlsjURhzSW5ET0e+FRITYsm4AzS7L8jN5g==` equals the restored hash; package consumer 41/41. The `ci-local.ps1` run on this record commit is recorded with the first P7.02 commit.
- P7.01 complete, outcome READY.

## 2. P7.02 – Source facts and MOSMIX forecast contract

- User authorization (2026-10-26, chat): "I authorize the P7.02 S1 retrievals (one GET each, including one MOSMIX_S all-stations file of about 37.5 MB) and the S3 replay probe on the NanoPi R76S. Keep SDK 10.0.402."
- Carried from P7.01: `pwsh ./scripts/ci-local.ps1 -Commit fe52f8673445acc85aa545f38649ec789e6f534e -TargetRef 8009f1c413ead6b150f2c71bec8f669e81be5558 -RunSettings coverage.runsettings` on the P7.01 record commit: exit 0, run directory artifacts/ci/fe52f86-20261026T205004Z (gitignored); Parsing 2301 of 2304 passed (the three opt-in live tests not executed), SchemaGeneration 46, Coverage 99, Coverage.Runner 56, package consumer 41; gate passed with 8486/8489 at or above the baseline (gate-result.json SHA-256 `d2c55afebf59d4460f1c30fd5d4b0fb842cad7cf50b08a73265442f9e780e0ef`); packed SHA-512 `urt+d9o3MJxRyDHOhPT3TWwseuCKhh6dVDYjNwkf4yPdMgCMy1s70eepKuUJNs57b5BRyaU81rZXmMmjY6Iz9Q==`.
- S1: one GET each of 12 DWD sources on 2026-10-26 between 20:33:48Z and 20:34:01Z, all HTTP 200 (source register in the contract section 2); the schema, the element definitions, the catalog and the MOSMIX_L index are byte-identical to the P4 records; the MOSMIX_S file (36316660 B) downloaded in 2.92 s on Windows. Structural facts extracted with a script outside the repository (contract sections 3 to 5); no DWD file committed.
- S2: the .NET runtime source at tag `v10.0.13` (two files, contract section 2) and a synthetic local probe show that `ZipArchiveEntry.Open()` does not verify the entry CRC-32 (contract section 7.1).
- S3: replay probe of the S1 file, three runs each on Windows (2.0 s, peak 94 MB) and the NanoPi R76S (9.0 s, peak 107 MB) with the SHA-256 checked on both sides; no further download; probe files removed from the device afterwards (contract section 7.2).
- S4: contract draft [P7_FORECAST_CONTRACT.md](P7_FORECAST_CONTRACT.md) written by the orchestrating agent; Codex rounds 5, 2 and 0 findings, all confirmed and fixed (contract section 9, verbatim outputs in section 9.1).
- README package check on the working tree: README.md SHA-256 `F45B4E82188741B30742C5C4A738A704432A7C9F74291F32024651C05ECC7163` identical in the package with LICENSE and the export schema; packed SHA-512 `AzIYU9jsdc6sb0KuFPoXO4/i1tKcur127dg7F+I3vIZH8Wv146G7g31q2LsGu9i3MIBASuyjslAYPeNPo/PSrQ==` equals the restored hash; package consumer 41/41.
- PR #11 review of `3582936` (2026-10-26, the user, inline comment on MF-11): 1 finding [P2], confirmed and fixed by the orchestrating agent: partial failure of a `PrecipitationAccumulation` value feeding two outputs now defined (total kept, rate omitted as `Unrepresentable`, regression test in P7.03). Codex round 4: 0 findings.
- S5 (user approval) and S6 (plan reconciliation) follow.
