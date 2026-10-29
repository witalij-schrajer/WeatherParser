# P8.09 – Integrated Verification

**Status (2026-10-29): complete, outcome READY on candidate `290a0eb` (Windows, hosted CI and the NanoPi R76S, section 5) and, after the test-only PR #14 review fixes, READY again on candidate `ce166eb` (section 6).** Plan: [P8_09_INTEGRATED_VERIFICATION_PLAN.md](P8_09_INTEGRATED_VERIFICATION_PLAN.md).

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

## 6. Rerun on candidate `ce166eb` (after the PR #14 review fixes)

- Candidate: `ce166ebdcd82a6d5d92c5a926d9eb02ef0b17221`, the head after review round 4 ([P8.10 record](P8_10_ACCEPTANCE.md) section 8); the changes since `290a0eb` touch tests and documentation only, the library source is unchanged; integration target unchanged `8ed51c725333168d6e072369343c4eb449f8356c`.
- S1 Windows: `pwsh ./scripts/ci-local.ps1 -Commit ce166ebdcd82a6d5d92c5a926d9eb02ef0b17221 -TargetRef 8ed51c725333168d6e072369343c4eb449f8356c -RunSettings coverage.runsettings`: exit 0, 2026-10-29T07:55:05Z to 07:56:27Z, run directory artifacts/ci/ce166eb-20261029T075505Z (gitignored); Parsing 3213 of 3220 passed, 0 failed (seven opt-in checks not executed), SchemaGeneration 46, Coverage 99, Coverage.Runner 56, package consumer 61; gate passed with 11102/11103 above the baseline 10248/10249 (gate-result.json SHA-256 `196b05e14e2f473b1745d7521d19bfbf1541fdf26d00f34d9ed4c21651f9f59b`, identical to the `290a0eb` run); packed SHA-512 `oZTYauX4W38QswKYjuH2oePNnlwDxUiLlrnJ2rKICHnLvazGCJ/8v7ylZJkWZA+xpGsh6vLtEKF/EZkfvlkkmQ==`.
- S2 hosted CI: run 40838662090 (`pull_request`, head `ce166eb`, created 2026-10-29T07:56:29Z): `changes`, `windows-x64`, `ubuntu-x64` and `debian13-arm64` success (the container job is not target evidence).
- S3 NanoPi R76S: user authorization (2026-10-29, chat, answering "Do you accept P8 and the complete library on candidate ce166eb?"): "Accept, rerun NanoPi". Same procedure as section 4 (Git bundle of `features/develop/P8` and the target, hash checked on the device, detached checkout at exactly the candidate, `scripts/prepare-debian-arm64.sh`, `ci-local.ps1 -Commit ci-commit -TargetRef ci-target`); device Linux 6.1.141 aarch64, Debian 13.3, .NET SDK 10.0.402. Result: `ci_local_exit=0`, 2026-10-29T12:48:59Z to 12:57:50Z (run 8 min 48 s), run directory copied to artifacts/ci/arm64-ce166ebdcd82a6d5d92c5a926d9eb02ef0b17221-p809/ce166eb-20261029T124900Z (gitignored); Parsing 3213 of 3220 passed, 0 failed, SchemaGeneration 46, Coverage 99, Coverage.Runner 56, package consumer 61 (including the P8.07 concurrency tests); gate passed with 11102/11103 above 10248/10249 (gate-result.json SHA-256 `65784afdd47d2d613e51377b4a4ba40c7b33610843106a4d7ac4866f0db50648`, identical to the `290a0eb` NanoPi run); packed SHA-512 `U4AH2jKakfI6PfydKKYFQIRqJ6tnFneqSbZ3KKau4/eEsGTIE3vcs6iA24YEr92ahNgxfDvpCrHK936cXoM2nQ==` (not byte-reproducible, inherited).
- Outcome: **READY** on candidate `ce166eb` on Windows, hosted CI and the NanoPi R76S, with the same test outcomes and block counters as on `290a0eb`; no defect open.
