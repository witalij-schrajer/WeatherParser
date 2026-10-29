# P8.09 – Integrated Verification

**Status (2026-10-29): in progress; S1 (Windows) and S2 (hosted CI) passed on candidate `290a0eb`; S3 (NanoPi R76S) waits for the user's authorization; outcome pending.** Plan: [P8_09_INTEGRATED_VERIFICATION_PLAN.md](P8_09_INTEGRATED_VERIFICATION_PLAN.md).

## 1. Candidate

- Candidate: `290a0eb7975beaebc90e543f10d507822e7ccab7` (the head after P8.08, branch `features/develop/P8`, pull request #14); integration target `8ed51c725333168d6e072369343c4eb449f8356c` with the baseline 10248/10249 ([P8.01 record](P8_01_FOUNDATION_CHECK.md)).
- No live check (MQ-14: P8 adds no retrieval).

## 2. S1 – Windows

- `pwsh ./scripts/ci-local.ps1 -Commit 290a0eb7975beaebc90e543f10d507822e7ccab7 -TargetRef 8ed51c725333168d6e072369343c4eb449f8356c -RunSettings coverage.runsettings`: exit 0, 2026-10-29T01:32:10Z to 01:33:25Z, run directory artifacts/ci/290a0eb-20261029T013210Z (gitignored), two-pass.
- Tests: Parsing 3213 of 3220 passed, 0 failed (the seven opt-in live and resource checks not executed), SchemaGeneration 46, Coverage 99, Coverage.Runner 56, package consumer 61 against the same-run package `WeatherParser.Parsing` `0.1.0-initial` (packed SHA-512 `gVvXR/zZkPnWVCkH4gAmpbgucIMLAYnx2GIrEnaVKH8EDz3QkAXoSUQscaQWXuStc8sbnZCIIAIFtVtEBjETzQ==`).
- Gate passed: 11102/11103 native blocks (99.99 %) strictly above the baseline 10248/10249 and above the 80 % minimum; gate-result.json SHA-256 `196b05e14e2f473b1745d7521d19bfbf1541fdf26d00f34d9ed4c21651f9f59b`.

## 3. S2 – Hosted CI

- Run 40800979162 (`pull_request`, head `290a0eb`, created 2026-10-29T01:31:56Z): `changes` success, `windows-x64` success, `ubuntu-x64` success, `debian13-arm64` success. The hosted `debian13-arm64` job runs in a container and is not target evidence.

## 4. S3 – NanoPi R76S

- Pending: the run on the exact candidate SHA needs the user's authorization (AGENTS.md, P8.09 plan section 2).

## 5. Outcome

- Pending S3.
