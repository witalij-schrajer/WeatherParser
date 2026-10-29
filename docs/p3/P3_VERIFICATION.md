# P3 Verification Record

**Status (2026-10-29): P3.01 complete, outcome READY (section 1); P3.02 complete: contract CL-01 to CL-22 approved by the user on 2026-10-29, plans reconciled (section 2).**

- Integration target: `e623a834bdb2902cec930258a62a48302f626716` (merge of pull request #15; documentation-only successor of the P8 merge `21b7210`).
- Branch `features/develop/P3`.

## 1. P3.01 – Foundation check

- User instruction (2026-10-29, chat): "P3 Implmentation".
- Record: [P3_01_FOUNDATION_CHECK.md](P3_01_FOUNDATION_CHECK.md), written by the orchestrating agent; no qwen output in this step (documentation and repository operations only, AGENTS.md).
- S1 and S2 recorded (merge checks, commit classification of `21b7210..e623a83`, extraction, item mapping).
- S3: branch `features/develop/P3` from `origin/develop` `e623a83`; status passages "P3 started"; README package check on the working tree passed (README.md SHA-256 `1280DDCF3E9992C913F030DD6B01798AFD771524CDE6E0B34E103B253344D3C3`, package consumer 61/61).
- Seed commit `0c54a0a4c90807a3e9011dbfba3ea03e0694f9e2`, pushed; draft pull request #16 into `develop` opened; Auto-fix on. Hosted CI for `0c54a0a`: run 40949977254 (`pull_request`), completed with conclusion success (jobs changes, windows-x64, ubuntu-x64, debian13-arm64 all success).
- S4: `pwsh ./scripts/ci-local.ps1 -Commit 0c54a0a4c90807a3e9011dbfba3ea03e0694f9e2 -TargetRef e623a834bdb2902cec930258a62a48302f626716 -RunSettings coverage.runsettings`: exit 0, run directory artifacts/ci/0c54a0a-20261029T203244Z (gitignored), two-pass; Parsing 3213 of 3220 passed, 0 failed (seven opt-in checks not executed), SchemaGeneration 46, Coverage 99, Coverage.Runner 56, package consumer 61; gate passed with 11102/11103 at or above the baseline 11102/11103 (gate-result.json SHA-256 `6c3c1112dbe482712cee5205a84b266ea71ed626db9743550c0a9ab183073308`).
- Baseline for the P3 code steps: **11102/11103** native blocks for target `e623a83`; the one uncovered block (`FileSystemMosmixEvidenceCache.DeleteTemporaryFile` `catch`) is reachable by a test without a library change on Windows only (deny-delete access control list, experiment in record section 5), not on Linux.
- S5: `PHASE_P3_PLAN.md` section 1 table "P3.01 extraction" and status line; status passages "P3.01 complete" in AGENTS.md, README.md, DEVELOPMENT_PLAN.md and PHASE_P1_PLAN.md; four plan adjustments for P3.02 (record section 6).
- Codex five-angle review of P3.01 (`p301/codex_r1.md`, outside the repository; seed commit and S5 working tree), round 1: 4 findings, each verified: [P2] the inherited-item table missed the P8.10 row "payload bound limits message acceptance, not peak memory": confirmed, row added together with the other P8.10 and P7.12 rows checked one by one; [P2] the assessment that the `DeleteTemporaryFile` `catch` needs a race or a library change overlooked Windows access control lists: confirmed by an experiment (write succeeds, move and delete fail with `UnauthorizedAccessException`), record section 5 and the plan table corrected (reachable on Windows only, not on Linux); [P3] the verification header still said "in progress": fixed; [P3] the hosted CI conclusion was not recorded: run 40949977254 recorded as success.
- Round 2 (`p301/codex_r2.md`): 2 findings, both confirmed: [P2] the README package check recorded for the seed commit did not cover the README with the "P3.01 complete" passage: rerun on the S5 working tree: README.md SHA-256 `A3C101B6562D2F0071EA9FD5B242A3669DAF958B83A1A13E090F0993BD3303B1` identical in the package with LICENSE and the export schema, packed SHA-512 `xYFiCaGTsJZoTC+C8nklCCA0s1gtfWQaJWc3bZ1c+tAaxKP0IU9wkiNfDsSMR0UiyMhJkq2o/uxYwgyjkJiiHg==` equals the restored hash, package consumer 61/61; the `ci-local.ps1` run on the P3.01 record commit is recorded with the first P3.02 commit; [P3] the header referred to a P3.02 section that did not exist yet: reference removed.
- Round 3 (`p301/codex_r3.md`): 0 findings.
- P3.01 complete, outcome READY.

## 2. P3.02 – Dependency facts and CLI contract

- Carried from P3.01: `pwsh ./scripts/ci-local.ps1 -Commit ba314470a1c752bfff574a6d0e2ad9d14c7464ad -TargetRef e623a834bdb2902cec930258a62a48302f626716 -RunSettings coverage.runsettings` on the P3.01 record commit: exit 0, run directory artifacts/ci/ba31447-20261029T204137Z (gitignored); Parsing 3213 of 3220 passed, 0 failed (seven opt-in checks not executed), SchemaGeneration 46, Coverage 99, Coverage.Runner 56, package consumer 61; gate passed with 11102/11103 at or above the baseline (gate-result.json SHA-256 `6c3c1112dbe482712cee5205a84b266ea71ed626db9743550c0a9ab183073308`); packed SHA-512 `0nD9Gx4cjk7/Rvg2HspJIXIzUeVSwhqQMqcLVxTFo3U4uABDCu1qxEx71TmridUOlTegR/Y8qxWLeb47EFZFwA==`.
- S1: package metadata of MQTTnet, System.CommandLine, Microsoft.Extensions.Logging and Microsoft.Extensions.Logging.Console from nuget.org, the MQTTnet licence file and assembly metadata, the MQTTnet GitHub releases and the System.CommandLine syntax documentation, all read on 2026-10-29 (contract section 2); nothing committed.
- S2: contract draft [P3_CLI_CONTRACT.md](P3_CLI_CONTRACT.md) CL-01 to CL-22, written by the orchestrating agent (no qwen output). Refinements of the planning rows: CL-03 (the parsers' own pre-request validation of station ID, MOSMIX ID and pattern instead of a CLI copy, mapped to exit `2`), CL-09 (OsthessenWetter sources through `IOsthessenWetterStationIdentity.ToStationId`), CL-10 (transport IDs with the `CLI-MQTT-` prefix), CL-13 (MQTT 5.0 by default for PUBACK reason codes), CL-19 (options with the P3.01 coverage facts; C recommended), and two added rows: CL-21 (HTTP client settings) and CL-22 (package versions and the 10.0.13/10.0.14 conflict).
- S3: the review loop runs through the automatic Codex review of pull request #16 (contract section 6).
- Review round 1 (automatic review of `ead7db8`, run 40951985205): 2 findings, both confirmed and fixed (contract section 6).
- Review round 2 (automatic review of `be68ad5`, run 40952365941): 1 finding, confirmed and fixed (contract section 6).
- Review round 3 (automatic review of `0f389f9`, run 40952723405): 1 finding, confirmed and fixed (contract section 6).
- Review round 4 (automatic review of `d6682e5`, run 40953016321): 1 finding, confirmed and fixed (contract section 6).
- Review round 5 (automatic review of `34cc9a8`, run 40953343035): 0 findings; the contract is presented to the user for approval.
- Review of the recording commit `743ae20` (run 40953658797): 0 findings.
- S4: the user approved CL-01 to CL-22 on 2026-10-29 ("Approve all", in answer to "Do you approve CL-01 to CL-22 with the recommended option in every row?"); recorded in contract section 1; the CL-19 option C amendment of REQ-086 written into REQUIREMENTS.md, AGENTS.md "Tests" and DEVELOPMENT_PLAN.md in the same commit; pull request #16 converted to draft.
- S5: PHASE_P3_PLAN.md "Approved contract" paragraph and status line; section 10 "Reconciliation with the approved contract" in each step plan P3.03 to P3.12; status passages "P3.02 approved" in AGENTS.md, README.md, DEVELOPMENT_PLAN.md and PHASE_P1_PLAN.md. P3.02 complete.
- README package check on the S5 working tree: README.md SHA-256 `CB68EE2295AFDD582DB45CFA3FEBAD80D0BD93CEBF71863CDE396797656D988F` identical in the package with LICENSE and the export schema, package consumer 61/61.
