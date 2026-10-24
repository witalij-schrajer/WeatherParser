# P4.01 – Foundation Check Record

**Status (2026-10-21): executed; outcome READY; Codex review closed with 0 findings (section 11). P4.02 starts on a separate instruction.**

Executes [the P4.01 plan](P4_01_FOUNDATION_CHECK_PLAN.md) and section 1 of [PHASE_P4_PLAN.md](../../PHASE_P4_PLAN.md).

## 1. Authorization

- 2026-10-20, user message: "let the qwen generate the plans for each P4 step. Invoke Codex to review each plan. ..."
- 2026-10-21, user answer: "Yes: commit 1 = P2 completion records, commit 2 = P4 phase plan and step plans, push features/develop/P4 and open the draft PR into develop."
- 2026-10-21, user message: "Go ahead with P4.01: SDK check, ci-local.ps1 baseline run on e5dfbc6 against the integration target, foundation record, Codex review."

## 2. Records rechecked

- `docs/p2/P2_12_ACCEPTANCE.md` sections 8 (audit ACCEPTED), 10 (user acceptance 2026-10-20), 11 (status updates), 12 (A9–A11)
- `docs/p2/P4_HANDOFF.md`
- `docs/p2/P2_VERIFICATION.md`
- `PHASE_P2_PLAN.md` status (P2.12 complete)
- `git merge-base --is-ancestor 74bf625 origin/develop` succeeded
- `origin/develop` = `b7da8288d8746bc6e034222c0467a8fcebba55b3`

## 3. Commits since the P2 merge

- `6793fdc` – AGENTS.md: open a draft PR into develop right after the first push of a phase branch.
- `b7da828` – merge of pull request #4.
- `git diff --stat 74bf625 origin/develop`: AGENTS.md only, 1 insertion. Classification: documentation-only; no product change.

The README package check (CONTRIBUTING.md section 4) for commit 1 had passed on the working tree on 2026-10-20: allowed entries only, `README.md`/`LICENSE`/schema byte-identical, restored hash equal to packed hash, 18 consumer tests passed, consumer-lock refresh discarded.

First hosted CI run of the pull request: 39542565088 on `e5dfbc6` — jobs `ubuntu-x64`, `debian13-arm64` and `windows-x64` succeeded.

## 4. Field-by-field extraction

| Field | Value | Source |
|---|---|---|
| Integration target | `b7da8288d8746bc6e034222c0467a8fcebba55b3` (develop head; P2 merge `74bf625eb4b2fbf49cbce7d1cee42b6302f2e47b`) | `git rev-parse origin/develop`, ancestry check |
| Package identity | `WeatherParser.Parsing` `0.1.0-initial`, unpublished | P4 handoff section 1 |
| SDK / runtime | SDK 10.0.402 (`global.json`, rollForward disable), runtime `Microsoft.NETCore.App` 10.0.13 | `dotnet --version`, `dotnet --list-runtimes`, P2_VERIFICATION.md |
| Coverage tools | Microsoft.CodeCoverage 18.11.0, dotnet-coverage 18.11.0 | DEPENDENCIES.md |
| Export contract | v1 frozen at P2 acceptance; namespace `urn:weatherparser:export:v1`; C-01 to C-116; additive optional members only (C-03, C-105) | `docs/p2/P2_EXPORT_CONTRACT.md` |
| Helpers for P4 | list roots `CdcStationList`, `MosmixLocationList`, `SearchParameters`; `IListEntryOrdering`; `IJsonExportSerializer`, `IXmlExportSerializer`; `AddWeatherParsing`; schema validation settings C-104 | P4 handoff section 2 |
| P2 coverage at acceptance | 3035/3037 on Windows and the NanoPi R76S (candidate `bced7be`) | `docs/p2/P2_12_ACCEPTANCE.md` |
| Remeasured baseline | 3035/3037 for target `b7da828` (S4 run) | run directory below |
| Inherited limitations and owners | Coarse XML cancellation (C-10): accepted limitation, revisit if a later phase needs finer cancellation; characterization evidence in several P2 steps: recorded, no rework planned; builders and root validation implement rules separately (equivalence tests guard divergence): P5–P7; hosted `debian13-arm64` job is a container, target evidence only from the NanoPi R76S: P9 for release and hardware acceptance (REQ-084), P4.11 for P4 target evidence; `de_DE.UTF-8` locale not installed on the target (ICU data used): recorded platform fact; CONTRIBUTING.md section 7 needs local `ci-commit`/`ci-target` branches: fixed procedure (P2.11 defect D1), applied in P4.11 | `docs/p2/P2_12_ACCEPTANCE.md` section 6, P4 handoff section 6 |

## 5. SDK servicing check

Official release metadata https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json checked on 2026-10-21: latest release 10.0.13 (2026-10-13, security release), latest SDK 10.0.402, support phase active. The pinned SDK is current; no update decision needed.

## 6. Branch, commits and draft pull request

Branch `features/develop/P4` from `b7da828`; commit 1 `8276a5b` (P2 completion records); commit 2 `e5dfbc6fd8e0fc06ecd3ba81382345b46aeeb91b` (`PHASE_P4_PLAN.md` and the twelve step plans); both pushed on 2026-10-21; draft pull request #5 into `develop` opened directly afterwards (https://github.com/witalij-schrajer/WeatherParser/pull/5); Auto-fix enabled.

## 7. Baseline run

Baseline run, 2026-10-21: `pwsh ./scripts/ci-local.ps1 -Commit e5dfbc6fd8e0fc06ecd3ba81382345b46aeeb91b -TargetRef b7da8288d8746bc6e034222c0467a8fcebba55b3 -RunSettings coverage.runsettings`, exit 0, mode two-pass, duration 48 s (04:11:05Z–04:11:50Z), run directory `artifacts/ci/e5dfbc6-20261021T041105Z/`. Tests: Parsing 880, Coverage 99, Coverage.Runner 56, SchemaGeneration 43, package consumer 18 (1096 passed, 0 failed). Changed files: 21 Markdown files only (AGENTS.md, DEVELOPMENT_PLAN.md, PHASE_P1_PLAN.md, PHASE_P2_PLAN.md, PHASE_P4_PLAN.md, README.md, three docs/p2 files, twelve docs/p4 plans). Native block counters of `WeatherParser.Parsing.dll`: candidate 3035/3037, target 3035/3037. Gate passed: Comparability, Measurement (3035/3037 valid), Minimum (99.93 % ≥ 80 %), Baseline (3035/3037 valid), Comparison (at or above baseline; documentation-only change). `gate-result.json` SHA-256 `d54cfb2244a2e8280fa460534e69edc856247fc1540f8ea8e18fd3a16ab7ac7d`. Packed package SHA-512 (base64) `I8SFCrvLZnfuOGKZ8gCsKMkBYmNIvkj9Y3FAR1VsM2qJzWVJrRWS4Vexau6JHrhe+lpdhdI45nAu1C65aWvEdw==`; the isolated run rewrote only the consumer lock's `WeatherParser.Parsing` `contentHash` (one line, not committed, C-116). This run is also the committed-revision run required for the README change of commit 1 (CONTRIBUTING.md section 4).

## 8. Findings

None.

## 9. Plan reconciliation

`PHASE_P4_PLAN.md` section 1 receives the extraction table; no other plan change was needed. Outcome: READY.

## 10. Outcome

READY.

## 11. Authorship and review

Round 3: 1 finding, confirmed — the P4.01 plan status line had lost its authorship and review history; qwen restored it. Round 4: 0 findings. P4.01 is complete.
