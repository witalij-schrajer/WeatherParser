# P6.10 – Integrated Coverage and Platform Verification: Record

**Status (2026-10-26): S0 to S4 executed on candidate `4373399`; outcome in section 7; after the PR #9 review S1 to S3 repeated on the new candidate `12e7205`: READY (section 8).** Plan: [P6_10_INTEGRATED_VERIFICATION_PLAN.md](../development/p6/P6_10_INTEGRATED_VERIFICATION_PLAN.md). Step evidence of S0 is in [P6_VERIFICATION.md](P6_VERIFICATION.md) section 10.

## 1. Candidate, target and authorizations

- **Candidate**: `4373399b278e65097452fe18d7029fee5461bdb1` (P6.10 S0, head of `features/develop/P6`, pushed to draft pull request #9).
- **Integration target**: `8d8079a4238514f7c9bcfc3846829e2408938ae3` (`develop`, merge of pull request #8; the P6 branch point and the P6.01 baseline).
- **User authorizations (2026-10-26, chat)**: "You have my go-ahed for run at nano pi r76s at p6.10" (S3) and, separately, "I authorize you to run  live check against OsthessenWetter page." (S4).
- **New-candidate rule** (plan section 3): a production change after S1 makes a new candidate and repeats S1 to S3; documentation-only changes do not.

## 2. S1 Windows

- Command: `pwsh ./scripts/ci-local.ps1 -Commit 4373399b278e65097452fe18d7029fee5461bdb1 -TargetRef 8d8079a4238514f7c9bcfc3846829e2408938ae3 -RunSettings coverage.runsettings`.
- Exit 0; 2026-10-26T01:36:59Z to 01:38:06Z; run directory `artifacts/ci/4373399-20261026T013659Z` (gitignored); `commits.json`: commit `4373399…`, targetCommit `8d8079a…`, mode two-pass, exitCode 0.
- Environment: Windows x64 (OS 10.0.26200), SDK 10.0.402, runtime Microsoft.NETCore.App 10.0.13, RID win-x64.
- Tests (current pass): Parsing 2298 total, 2295 passed, 0 failed (the three live tests not executed), Coverage 99/99, Coverage.Runner 56/56, SchemaGeneration 46/46, package consumer 41/41; target pass Parsing 1937 passed of 1939 (two live tests not executed).
- Gate: passed with all five rules true, 8475/8478 above 7491/7494; `gate-result.json` SHA-256 `4a879cdf92c492d27bb9ed2a8465bff4a8cafe925a4f52f41fd1e0e0d4b41edb`.
- Package: packed SHA-512 `i+99jtTx3Xphxv5Cq7rRyA/J/Ax6h7R2czjZVvj0yU4ysCsawzrTqIk0KMmhmn9g4Rd0MjqsZOeJA4QYesWj/A==` (`package-hash.txt`), 8 entries.

## 3. S2 Hosted CI

- Run 40255069498, event `pull_request` on the push of the candidate (created 2026-10-26T01:36:47Z), head `4373399b278e65097452fe18d7029fee5461bdb1`: conclusion success; jobs `windows-x64`, `ubuntu-x64` and `debian13-arm64` (container) all success.
- Artifacts downloaded with `gh run download 40255069498` to `artifacts/ci/hosted-40255069498` (gitignored): every `commits.json` names commit `4373399` and targetCommit `8d8079a`, mode two-pass, exitCode 0; every `gate-result.json` passed with 8475/8478; tests per job: Parsing 2295 of 2298 (the three live tests not executed), Coverage 99/99, Coverage.Runner 56/56, SchemaGeneration 46/46, package consumer 41/41.
- The hosted `debian13-arm64` job is a container and not target evidence (inherited limitation); S3 is the target evidence.

## 4. S3 NanoPi R76S (Debian 13 ARM64)

- **Bundle**: `git bundle create weatherparser.bundle features/develop/P6 p610-target` (p610-target a temporary local branch at `8d8079a`, deleted after bundling); heads `4373399` refs/heads/features/develop/P6 and `8d8079a` refs/heads/p610-target; `git bundle verify` reported a complete history; 1738407 B, SHA-256 `24b9976ac8e563cb7abc89ddb20cf08fddea8eed78e4aa1a3a7f6d7ef1f4734d`, equal on both sides after scp.
- **Target and procedure**: `ssh R76S`: aarch64, Debian GNU/Linux 13 (trixie); working directory `~/weatherparser-p610`; CONTRIBUTING.md section 7: clone the bundle, detach at `origin/features/develop/P6`, `ci-commit` and `ci-target` branches, `bash scripts/prepare-debian-arm64.sh > env.sh`, `. ./env.sh`, `pwsh ./scripts/ci-local.ps1 -Commit ci-commit -TargetRef ci-target -RunSettings coverage.runsettings`.
- **Environment**: prepare exited 0 (SDK 10.0.402 and PowerShell 7.6.7 already installed, ICU `libicuuc.so.76`); runtime Microsoft.NETCore.App 10.0.13; RID linux-arm64, OS version 13.
- **Run**: ci-local exited 0, run `4373399-20261026T013737Z`, 01:37:36Z to 01:43:38Z; the run directory was copied back with scp to `artifacts/ci/arm64-4373399-20261026T013737Z` (gitignored); the working directory was removed afterwards (the older directories weatherparser-p211 and weatherparser-p211b were left untouched).
- **Results**: `commits.json` commit `4373399`, targetCommit `8d8079a`, exitCode 0, two-pass; Parsing 2295 of 2298 (three live tests not executed), Coverage 99, Coverage.Runner 56, SchemaGeneration 46, package consumer 41/41; gate passed with all five rules, 8475/8478 above 7491/7494; `gate-result.json` SHA-256 `995f7def9b964d2fc65a9f460e982f3c03ff7ba0a66f53f198a5093b118710e4`; packed SHA-512 `K7IvcWLKSyWeZPgWz4gA4eSGUcewSj/JeGEaw/7fbqN4YuDiPn5Orbgovhoi2Z/Cw8ULE7R+fbZotz5F4DEROA==`.
- **Time-zone data (T-P6-06)**: the `Europe/Berlin` midnight, gap, overlap, zone and delayed-response tests (10 results) passed on Debian with its system time-zone data.

## 5. S4 Live check

- **Conditions**: source conditions of contract OW-14 (no licence, the imprint forbids reproduction without the author's consent); OW-15: one request, explicitly enabled, authorized by the user, the page never written to the repository and only a value-free summary kept outside it (P6_VERIFICATION.md section 10).
- **Run**: `WEATHERPARSER_LIVE_CHECK=1`, `WEATHERPARSER_LIVE_CHECK_MODE=osthessen`, `dotnet test tests/WeatherParser.Parsing.Tests -c Release --no-build --filter FullyQualifiedName~LiveCheck_Osthessen_OnlyWhenExplicitlyEnabled` on the candidate build, Windows x64; 2026-10-26T01:37:42Z to 01:37:53Z; the test passed (including `EnsureUsable`); the summary was written to a temporary directory outside the repository and is not committed.
- **Request**: 1 of a budget of 1: HTTP 200, no `Content-Length`, 106052 bytes read, `https://osthessen-wetter.de/messwerte/`.
- **Result**: pattern `*`, no error; 20 overview stations matched; 16 datasets (Körnbach, Steinbach, Roßbach, Schlitz, Bad Salzschlirf, Gotthards, Müs-Hohhütte, Müs, Bimbach, Blankenau, Künzell, Johannesberg, Seiferts, Wüstensachsen, Wasserkuppe, Rotes Moor), each with temperature and the 10-minute mean wind (the wind table joined for every one of them) and all but Wasserkuppe with humidity; 4 station failures `OSTHESSEN-STATION-OFFLINE` (Arzell, Langenbieber, Kleinlüder, Petersberg); notices: 4 × `OSTHESSEN-STATION-OFFLINE`, 1 × `OBSERVATION-READING-OMITTED` (the Wasserkuppe humidity cell was empty); no `OSTHESSEN-FORMAT-CHANGED`, no ID collision, no wind-table or wind-row notice, no unreadable value, no unresolved time; schema events 0 and JSON/XML equivalent for every dataset.
- **Revision**: the page matched the documented revision of the contract (no drift); the measurement times (01:25Z to 01:31Z) were inferred after the end of daylight-saving time on 2026-10-25 and lie within minutes before the retrieval.
- **Coordinates**: the run used placeholder coordinates from the test-only `PlaceholderMetadataLookup`; they are not station locations and were not kept.
- Not repeated (OW-15); a further live request needs a new authorization.

## 6. Comparison

- The 2540 test results of the current pass (all projects; 2537 passed, 3 not executed: the live tests) are identical on Windows and the NanoPi, compared by project, test name and outcome with multiplicity preserved after normalizing the decimal separator of data-row names: 0 differences.
- Block counters identical (8475/8478 against 7491/7494); the `gate-result.json` files have identical content (the Windows file differs only by CRLF line endings: equal SHA-256 `995f7def…` after normalization).
- The package hashes differ because each platform packs its own build (C-116 holds within each run).
- Hosted CI (S2) agrees on all three jobs and is recorded separately.

## 7. Outcome

**READY** for candidate `4373399`: S1 (Windows), S2 (hosted CI) and S3 (NanoPi R76S) passed on the same candidate with identical outcomes, and the authorized live check S4 passed with one request; no open defect. The candidate goes to P6.11.

## 8. Rerun on candidate `12e7205` after the PR #9 review (2026-10-26)

- **Trigger**: the peer code review of `0012d16` on PR #9 (review 5648301660) reported two production findings; their fixes ([P6_VERIFICATION.md](P6_VERIFICATION.md) section 11, A10) make `12e7205385d1ad6ca1eb45cb4fcb1f7b50699554` the new candidate. Under the new-candidate rule (section 1) S1 to S3 were repeated against the unchanged target `8d8079a`; S4 was not repeated (OW-15).
- **S1 Windows**: `pwsh ./scripts/ci-local.ps1 -Commit 12e7205385d1ad6ca1eb45cb4fcb1f7b50699554 -TargetRef 8d8079a4238514f7c9bcfc3846829e2408938ae3 -RunSettings coverage.runsettings`: exit 0, 07:15:54Z to 07:16:58Z, run `artifacts/ci/12e7205-20261026T071554Z`; Parsing 2301 of 2304 (three live tests not executed), Coverage 99, Coverage.Runner 56, SchemaGeneration 46, package consumer 41/41; gate passed, 8486/8489 above 7491/7494; packed SHA-512 `vLtVCLhIIhxoROmyp7qs9T5YEDM9B49ba5me9Y/UMxE4CuUi7tfB+XdsSj2dlX/4F4dJD8Bu0cE/pmvvMAmY7A==`.
- **S2 Hosted CI**: run 40284717931 (`pull_request`, created 07:15:45Z) on `12e7205`: `windows-x64`, `ubuntu-x64`, `debian13-arm64` success; every `commits.json` names `12e7205` and `8d8079a`, exitCode 0; every gate passed with 8486/8489; the same test counts as S1.
- **S3 NanoPi R76S**: bundle with heads `12e7205` and `8d8079a`, 1758754 B, SHA-256 `889703b5782a2e8adc60384e10f56360455773a15adc526747e00b780aee809a` on both sides; the procedure of section 4; ci-local exit 0, run `12e7205-20261026T071609Z`, 07:16:09Z to 07:22:07Z, copied back to `artifacts/ci/arm64-12e7205-20261026T071609Z`, device directory removed; the same test counts as S1; gate passed, 8486/8489; `gate-result.json` SHA-256 `1fed15ceb1d941444281f0839bc7588f8e952c281f7b23eb3a7b130bb9e52dc4` (equal to the Windows file after line-ending normalization); packed SHA-512 `MFH2ypeOsIOr3qW+coaH4mb0ffNv2T9gy6qbEe07Xmvmm3Gt4SLW8t8iKaWLGJ3AmoEM5ALXiu8oGQLZRxqwkQ==`; the `Europe/Berlin` time-zone tests (10 results) passed.
- **Comparison**: 2546 test results (all projects; the six new tests included) identical on Windows and the NanoPi with multiplicity preserved: 0 differences.
- **Outcome**: **READY** for candidate `12e7205`; S4 stays the snapshot of section 5 (on `4373399`; the fixes do not change the parsing of a page with distinct station names or the deadline default).

