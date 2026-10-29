# P8.09 – Integrated Verification

**Status (2026-10-29): complete, outcome READY on candidate `290a0eb` (Windows, hosted CI and the NanoPi R76S, section 5).** Plan: [P8_09_INTEGRATED_VERIFICATION_PLAN.md](P8_09_INTEGRATED_VERIFICATION_PLAN.md).

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

- User authorization (2026-10-29, chat, in answer to "Do you authorize the NanoPi R76S run of ci-local.ps1 on candidate 290a0eb?"): "Authorize".
- Procedure (CONTRIBUTING.md section 7, script outside the repository): a Git bundle of `features/develop/P8` and the integration target (2 255 964 bytes, SHA-256 `73e9be9f1c2fb53dddf209b65920055aa41626fe00e5781e438ce7aadef076b7`, identical on the device), cloned on the device, checked out detached at exactly `290a0eb7975beaebc90e543f10d507822e7ccab7` with the target `8ed51c725333168d6e072369343c4eb449f8356c`, `scripts/prepare-debian-arm64.sh`, then `pwsh ./scripts/ci-local.ps1 -Commit ci-commit -TargetRef ci-target -RunSettings coverage.runsettings`; the device script was copied as a file and run with `bash`.
- Device: NanoPi R76S, Linux 6.1.141 aarch64, Debian 13.3, .NET SDK 10.0.402, runtime 10.0.13.
- Result: `ci_local_exit=0`, 2026-10-29T07:02:41Z to 07:11:43Z (run 8 min 59 s), run directory copied to artifacts/ci/arm64-290a0eb7975beaebc90e543f10d507822e7ccab7-p809/290a0eb-20261029T070243Z (gitignored); Parsing 3213 of 3220 passed, 0 failed (seven opt-in checks not executed), SchemaGeneration 46, Coverage 99, Coverage.Runner 56, package consumer 61; gate passed with 11102/11103 above the baseline 10248/10249 (gate-result.json SHA-256 `65784afdd47d2d613e51377b4a4ba40c7b33610843106a4d7ac4866f0db50648`; the file differs from the Windows run, the counters are identical); packed SHA-512 `ByY4ng39+4OWbm3XCeNdRwYER2vuV4CcPiXYYcmlbwmMNGXA1YM63fuIn4/APfWmtb+iwBm2zmpajrSpaaiQdg==` (the package is not byte-reproducible across builds, an inherited limitation).
- The P8.07 concurrency tests (overlap of at least two calls proven by the consumer handler's gate) passed on the target as part of the package-consumer run.

## 5. Outcome

**READY** on candidate `290a0eb`: S1 (Windows), S2 (hosted CI, run 40800979162) and S3 (NanoPi R76S) passed with identical test outcomes and the same block counters 11102/11103; no defect open. The platform outcomes stay separate: the hosted `debian13-arm64` container is not target evidence; the NanoPi run is. No live check was due (MQ-14).
