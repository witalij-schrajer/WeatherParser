# P6 Verification Record

**Status (2026-10-25): P6.01 complete, outcome READY.**

- Integration target: `8d8079a4238514f7c9bcfc3846829e2408938ae3` (documentation-only successor of the P5 merge `29fd553`).
- Branch `features/develop/P6`.

## 1. P6.01 – Foundation check

- Record: [P6_01_FOUNDATION_CHECK.md](P6_01_FOUNDATION_CHECK.md), written by the orchestrating agent.
- S1 and S2 recorded (merge checks, commit classification, extraction, limitation mapping, SDK servicing check).
- S3: branch `features/develop/P6` from `origin/develop` `8d8079a`; status passages "P6 started" in AGENTS.md, README.md, DEVELOPMENT_PLAN.md, PHASE_P1_PLAN.md and PHASE_P6_PLAN.md.
- README package check (CONTRIBUTING.md section 4) on the working tree: README.md (SHA-256 `C490BC35A28993E4A34F38449C9E575C6C4A13E80A9C64C34E396856D5778FF4`), LICENSE and the export schema byte-identical in the package; packed SHA-512 `FSz18m/oHC5XWGZp+W8sMISJzhJMyYaNHyZEpWdPzMfEsuvkZxIKVML4rFj1M4DZrVPkPQ9aQqULuhDayZG6/Q==` equals the restored hash; package consumer 36/36; the consumer lock was not committed.
- Seed commit `62441ff2d687b4132b0d3843457caccbcc9d615e` (status passages and the S1/S2 record), pushed; draft pull request #9 into `develop` opened; Auto-fix on.
- `pwsh ./scripts/ci-local.ps1 -Commit 62441ff2d687b4132b0d3843457caccbcc9d615e -TargetRef 8d8079a4238514f7c9bcfc3846829e2408938ae3 -RunSettings coverage.runsettings`: exit 0, 2026-10-25T09:39:24Z to 09:40:28Z, run directory artifacts/ci/62441ff-20261025T093924Z (gitignored); commits.json: commit `62441ff...`, targetCommit `8d8079a...`, two-pass.
- Tests: Parsing 1937 of 1939 passed (the two live tests not executed), SchemaGeneration 46, Coverage 99, Coverage.Runner 56, package consumer 36.
- Gate passed: 7491/7494 (99.96 %) at or above the baseline 7491/7494 of `8d8079a` (documentation-only candidate); gate-result.json SHA-256 `25774d03278c9037bdcb6992d1411a290bcc36ed3b92eb7609eb65e22e9bbc55`.
- Packed SHA-512 `i0JDQXZTNaDFInttJLjnEs4oYlPcSq/Kc2xe0v/qX/VtnR9P0G3+o3ucu5b+p4pYLbGYUYs7jHAEZv83AQwGRA==`.
- Baseline for the P6 code steps: **7491/7494** native blocks for target `8d8079a`, valid while the pull-request base is `8d8079a` and `coverage.runsettings` is unchanged. Every P6 code step must strictly improve the unrounded ratio.
- `PHASE_P6_PLAN.md` section 1 extraction table and status line updated.
- Hosted CI for the seed commit `62441ff`: run 40181539289 (`pull_request`, created 2026-10-25T09:39:21Z, completed 09:42:07Z), ubuntu-x64, windows-x64 and debian13-arm64 all success.
- Codex five-angle review of P6.01 (`p601_codex_r1.md`): 1 finding [P2], confirmed and fixed by the orchestrating agent: the hosted CI run of the seed commit was not recorded (now recorded above).
- Round 2 (`p601_codex_r2.md`): 0 findings.
- README package check on the S5 working tree (README status passage changed): README.md SHA-256 `9E55253F629F00B75BAE8245721E05F0D5CA16405ADA032C2C1668B6BCCB0750` identical in the package with LICENSE and the export schema; packed SHA-512 `TIZS7LW/0aiReShFsQwMO8czbRNDBVMqYHkWtLjJzZH4cYycjHxK05rBAVkqr2jtStU8IXidwMMOK+cwzDFF2g==` equals the restored hash; package consumer 36/36. The `ci-local.ps1` run on this record commit is recorded with the first P6.02 commit.
