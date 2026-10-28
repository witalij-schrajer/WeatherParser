# P8 Verification Record

**Status (2026-10-28): P8.01 complete, outcome READY (section 1); P8.02 in progress: contract draft under the automatic pull-request review (section 2).**

- Integration target: `8ed51c725333168d6e072369343c4eb449f8356c` (merge of pull request #12; successor of the P7 merge `20b2ef0` without library change).
- Branch `features/develop/P8`, draft pull request #14 into `develop`.

## 1. P8.01 – Foundation check

- User instruction (2026-10-28, chat): "P8 implementation".
- Record: [P8_01_FOUNDATION_CHECK.md](P8_01_FOUNDATION_CHECK.md), written by the orchestrating agent; no qwen output in this step (documentation and repository operations only, AGENTS.md).
- S1 and S2 recorded (merge checks, commit classification of `20b2ef0..8ed51c7`, extraction, limitation mapping, SDK servicing check).
- S3: branch `features/develop/P8` from `origin/develop` `8ed51c7`; status passages "P8 started" in AGENTS.md, README.md, DEVELOPMENT_PLAN.md and PHASE_P1_PLAN.md; README package check on the working tree passed (README.md SHA-256 `5A8EC9B869FF304861BC4A8C8F46D31AC9391F712FDE1F3AFDB3B00A421F1BCB`, package consumer 47/47); seed commit `5525ea11073c9cb5d347bbf889119fd592d635ca`, pushed; draft pull request #14 opened; Auto-fix on.
- S4: `pwsh ./scripts/ci-local.ps1 -Commit 5525ea11073c9cb5d347bbf889119fd592d635ca -TargetRef 8ed51c725333168d6e072369343c4eb449f8356c -RunSettings coverage.runsettings`: exit 0, run directory artifacts/ci/5525ea1-20261028T220607Z (gitignored), two-pass; Parsing 2947 of 2954 passed, 0 failed (seven opt-in checks not executed), SchemaGeneration 46, Coverage 99, Coverage.Runner 56, package consumer 47; gate passed with 10248/10249 at or above the baseline 10248/10249 (gate-result.json SHA-256 `79b88132aead3f0704190e94548ba313491fd7fa9b4d7a6e677569d537ea5ab9`).
- Hosted CI for `5525ea1`: run 40777302720 (`pull_request`).
- Baseline for the P8 code steps: **10248/10249** native blocks for target `8ed51c7`, valid while the pull-request base is `8ed51c7` and `coverage.runsettings` is unchanged.
- S5: `PHASE_P8_PLAN.md` section 1 table "P8.01 extraction" and status line; status passages "P8.01 complete" in AGENTS.md, README.md, DEVELOPMENT_PLAN.md and PHASE_P1_PLAN.md.
- Codex five-angle review of P8.01 (`p801/codex_r1.md`, outside the repository; seed commit and S5 working tree): 0 findings (its sandbox could not reach GitHub, so the pull-request and run metadata were not rechecked by it).
- README package check on the S5 working tree (README status passage changed): README.md SHA-256 `265E3E08A76D098CDEE4BEA4EF409A2EADCFBC9C7AABAEDB59F753F03385DD4F` identical in the package with LICENSE and the export schema; packed SHA-512 `xOCZjXlp6HVECSf16g0lQk4LX0MB8rMb0032j2+/xhLzvdFqR0RBFB6HGoAgCiHt96QTJX9SY3aVRaNMqm5tLg==` equals the restored hash; package consumer 47/47. The `ci-local.ps1` run on this record commit is recorded with the first P8.02 commit.
- P8.01 complete, outcome READY.

## 2. P8.02 – Protocol facts and MQTT message contract

- Carried from P8.01: `pwsh ./scripts/ci-local.ps1 -Commit e97af434f91e60f015ab6abc779bbedf7d60e05e -TargetRef 8ed51c725333168d6e072369343c4eb449f8356c -RunSettings coverage.runsettings` on the P8.01 record commit: exit 0, run directory artifacts/ci/e97af43-20261028T221017Z (gitignored); Parsing 2947 of 2954 passed, 0 failed (seven opt-in checks not executed), SchemaGeneration 46, Coverage 99, Coverage.Runner 56, package consumer 47; gate passed with 10248/10249 at or above the baseline (gate-result.json SHA-256 `79b88132aead3f0704190e94548ba313491fd7fa9b4d7a6e677569d537ea5ab9`); packed SHA-512 `uESJQPTws384SMI+sYv+Wm0yHWJ8IuccES/dHGCZwGaojrBvQt2CmmyRF2G5M51yifWcfpOdB36fKObWzKfSow==`.
- User decision (2026-10-28, chat), recorded as AGENTS.md "Phase implementation": "Before my approval on p8.02 mark the PR as ready for review and fix all review issues. After my approval mark the PR as draft and implement the phase to the end. Than mark PR as \"ready for review\" anf fix all review findings. Record this approach as standard implementation approach: you works so far alone, as it possible. Before my approval PR must be set to \"ready for review\", the review issues must be fixed, and after that I will approve decision/Accept phase implementation".
- S1: one GET each of the OASIS MQTT 3.1.1 and 5.0 specifications on 2026-10-28, both HTTP 200 (contract section 2); facts F1 to F16 extracted with section references (contract section 3); not committed.
- S2: contract draft [P8_MQTT_CONTRACT.md](P8_MQTT_CONTRACT.md) MQ-01 to MQ-15, written by the orchestrating agent (no qwen output). Refinements of the planning rows: the `{provider}` failure for a list `source` without `:` (MQ-04), Unicode non-characters rejected (MQ-06, MQ-07, from F4), the template length check (MQ-07), the bound 268 435 450 − T (MQ-11, section 4) and therefore the payload-size check after topic resolution (MQ-13, changing the P8.05 precedence).
- S3: the review loop runs through the automatic Codex review of pull request #14 (contract section 6).
- Review round 1 (automatic review of `3547d6f`, run 40778435386): 2 findings, both confirmed and fixed (contract section 6); status passages "P8.02 in progress" added in AGENTS.md, README.md, DEVELOPMENT_PLAN.md and PHASE_P1_PLAN.md; README package check on the working tree: README.md SHA-256 `E98242F6018E6A47C815EC503B70D2D9BE98EF458E68F87054EF667DE5EF49F6` identical in the package with LICENSE and the export schema, packed SHA-512 `txd07wa3bITA+ru6Uf2H6mAOdiYiU/hIUs4iIp+VC1W5Ne1Sicf6ijAZ68qRaH8oSi0TwSRP/QM1fa1kxepZlg==` equals the restored hash, package consumer 47/47.
