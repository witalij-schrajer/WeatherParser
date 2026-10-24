# P5.01 – Foundation Check Record

**Status (2026-10-24): executed; outcome READY (section 10); Codex review in section 11; checkpoint commit on the user's request. P5.02 starts on a separate instruction.**

Executes [the P5.01 plan](P5_01_FOUNDATION_CHECK_PLAN.md) and section 1 of [PHASE_P5_PLAN.md](../../PHASE_P5_PLAN.md).

## 1. Authorization

- 2026-10-24, user message: "prepare to execution of P5 stage".
- 2026-10-24, user message: "Afte aou are redy, create a new branch for P5 implementation and execute p5.01 step".
- 2026-10-24, user answer to the question whether to wait for the merge of pull request #6 (P5.01 plan risk (d)): "I merge PR #6 first (Recommended)". The user merged pull request #6 at 2026-10-24T08:12:44Z; the branch was created afterwards.

## 2. Records rechecked (S1)

- `docs/p4/P4_12_ACCEPTANCE.md` section 5 (audit ACCEPTED, candidate `b6bc47f`, 6811/6814), section 6 (P4 accepted by the user on 2026-10-24 on candidate `b6bc47f89147c05bf04e06cf0f87c1497bc651f3`) and section 8.3 (pull request #5 merged on 2026-10-24T04:09:56Z, merge commit `95bcb1e3287cae275323cc3bf202337de1942d88`; post-merge checkpoint in pull request #6).
- `docs/p4/P5_HANDOFF.md` sections 1 to 7.
- `docs/p4/P4_VERIFICATION.md` section 12 (P4 complete).
- `PHASE_P4_PLAN.md` status: P4 complete; every item of the section 9 checklist checked.
- Pull request #6 (`gh pr view 6`): state MERGED, merged at 2026-10-24T08:12:44Z, merge commit `1e604dc7b3f17826d19207cbec9a699e40e5a30f`; its last hosted CI run 40076934639 on head `c2cfb97` succeeded on `windows-x64`, `ubuntu-x64` and `debian13-arm64`.
- `git merge-base --is-ancestor 95bcb1e origin/develop`: exit 0.
- `git rev-parse origin/develop` at branch creation: `1e604dc7b3f17826d19207cbec9a699e40e5a30f`.

## 3. Commits since the P4 merge

`git log --oneline 95bcb1e..origin/develop`:

| Commit | Subject | Classification |
|---|---|---|
| `38bd198` | P4.12 A11: post-merge checkpoint, P4 complete | Documentation-only |
| `c10809b` | P4.12 A11: record ci-local run on checkpoint 38bd198 | Documentation-only |
| `9b5a8ca` | P5: phase plan and step plans P5.01-P5.11 (qwen-drafted, Codex-reviewed) | Documentation-only |
| `3bb2b32` | AGENTS: qwen scope limited to code; documentation written by the orchestrating agent | Documentation-only |
| `9da193a` | P5 plans: finite ObservationDeadline range in OC-09 and P5.06 boundary tests | Documentation-only |
| `c2cfb97` | P5: planning complete at 9da193a; completion record in PHASE_P5_PLAN.md | Documentation-only |
| `1e604dc` | Merge pull request #6 from witalij-schrajer/features/develop/P4-post-merge | Merge |

`git diff --name-only 95bcb1e origin/develop`: 22 Markdown files (`AGENTS.md`, `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md`, `PHASE_P4_PLAN.md`, `PHASE_P5_PLAN.md`, `README.md`, five files in `docs/p4/`, the eleven step plans in `docs/p5/`); no source, test, script, project, schema or configuration file. No production change since `95bcb1e`.

## 4. Field-by-field extraction (S2)

| Field | Value | Source |
|---|---|---|
| Integration target | `1e604dc7b3f17826d19207cbec9a699e40e5a30f` (`develop` head at branch creation; documentation-only successor of the P4 merge `95bcb1e3287cae275323cc3bf202337de1942d88`) | `git rev-parse origin/develop`, ancestry check, section 3 |
| Package identity | `WeatherParser.Parsing` `0.1.0-initial`, unpublished | `src/WeatherParser.Parsing/WeatherParser.Parsing.csproj`, P5 handoff section 1 |
| Package contents | `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE`, `schemas/export/v1/weatherparser-export-v1.xsd` | P5 handoff section 1 |
| SDK / runtime | SDK 10.0.402 (`global.json`, `rollForward` disable, no prerelease), runtime `Microsoft.NETCore.App` 10.0.13 | `dotnet --version`, `dotnet --list-runtimes`, `global.json` |
| Test and coverage packages | MSTest 4.5.1, Microsoft.NET.Test.Sdk 18.11.0, Microsoft.CodeCoverage 18.11.0, Moq 4.21.0, FluentAssertions 8.11.0, Microsoft.Extensions.DependencyInjection(.Abstractions) and Logging.Abstractions 10.0.13; local tool dotnet-coverage 18.11.0 | `Directory.Packages.props`, `.config/dotnet-tools.json`, `DEPENDENCIES.md` |
| Export contract | v1 frozen; C-01 to C-119 (C-117 to C-119 added additively in P4 from SC-23); only additive optional members (C-03, C-105); output schema `schemas/export/v1/weatherparser-export-v1.xsd` | `docs/p2/P2_EXPORT_CONTRACT.md` |
| Selection contract | SC-01 to SC-23, approved 2026-10-21 | `docs/p4/P4_SELECTION_CONTRACT.md` |
| Helpers P5 reuses | `ICdcStationCatalogReader` (SC-02, SC-10), `ICdcNowEvidenceReader` (SC-19, reuse obligation SC-21), `ICdcAvailabilityDeriver` (SC-03 product mapping), `ICatalogDownloader` (30 s per request, size bounds, no retries, SC-09), `ICdcCatalogSource`, `CdcCatalogEndpoints` (SC-18 limits, validated at construction, SC-12), `ISourceTimeNormalizer`, `IObservationDatasetBuilder`, `IJsonExportSerializer`, `IXmlExportSerializer`, `AddWeatherParsing` (C-107) | P5 handoff sections 2 to 4 |
| P4 coverage at acceptance | 6811/6814 native blocks of `WeatherParser.Parsing.dll` on Windows and the NanoPi R76S (candidate `b6bc47f`); 6811/6814 on the post-merge checkpoint `38bd198` against `95bcb1e` | `docs/p4/P4_12_ACCEPTANCE.md` sections 5 and 8.3 |
| P4 test counts | Parsing 1662 passed (1663 with the skipped live test), Coverage 99, Coverage.Runner 56, SchemaGeneration 46, package consumer 31 | `docs/p4/P4_12_ACCEPTANCE.md` section 5 |
| Remeasured baseline | 6811/6814 native blocks for candidate `483f6d5` and target `1e604dc` (section 7) | `ci-local.ps1` run `artifacts/ci/483f6d5-20261024T184149Z/` (`commits.json`, `gate-result.json`) |
| Inherited limitations and owners | Full observation parsing, unit conversions and quality codes for the SC-03 quantities: P5; reader reuse (SC-21): P5 builds on `ICdcNowEvidenceReader`, P7 on the MOSMIX readers; MOSMIX element verification and freshness: P7; example-CLI exposure of the MOSMIX_S refresh: user, P3 (SC-22); the P4 live check is a single Windows snapshot of 2026-10-23, not repeated (SC-16); hosted `debian13-arm64` is a container, target evidence only from the NanoPi R76S, release hardware acceptance P9 (REQ-084); package not byte-reproducible across builds (C-116 within one run): P9 if required; characterization evidence in several P4 rows: recorded, no rework; coarse XML cancellation (C-10): accepted, carried forward; builders and root validation implement rules separately (equivalence tests guard divergence): P5 to P7; forecast-only quantities not in the export contract: P7; `de_DE.UTF-8` not installed on the target (ICU data used): recorded platform fact; MQTT, CLI, configuration, exit codes, broker transport: P8, P3; publication: P9; P1 limitations unchanged | `docs/p4/P4_12_ACCEPTANCE.md` section 4, P5 handoff section 7 |

## 5. SDK servicing check (S2)

Official release metadata https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json checked on 2026-10-24: latest release 10.0.13 (2026-10-13, security release), latest SDK 10.0.402, support phase active. The pinned SDK 10.0.402 is current; no update decision is needed (P5.01 plan risk (b) does not apply).

## 6. Branch, seed commit and draft pull request (S3)

Branch `features/develop/P5` created from `origin/develop` `1e604dc` on 2026-10-24.

README package check (CONTRIBUTING.md section 4) on the working tree of the seed commit, 2026-10-24: Release build, `dotnet pack`; package entries only `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE`, `schemas/export/v1/weatherparser-export-v1.xsd` and NuGet metadata; `README.md`, `LICENSE` and the export schema byte-identical to the working tree; the forced lock refresh changed one line (the `WeatherParser.Parsing` `contentHash`); restored hash equal to the packed SHA-512 `MSgca0GBer4F95TxJh28ZLbYvZg3R2qAJYu5WiFY1frFAhabgk/dLaSglc0IelTruiqztRry+duvDkAfffwWOg==`; package consumer 31 of 31 passed; the lock refresh was discarded with `git restore`.

Seed commit `483f6d5800b8fff6cdba1357513065efb0b41ebc` (the "P5 started" status passages in `AGENTS.md`, `README.md`, `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md` and `PHASE_P5_PLAN.md`, this record draft and `docs/p5/P5_VERIFICATION.md`), pushed on 2026-10-24; draft pull request #7 into `develop` opened directly afterwards (https://github.com/witalij-schrajer/WeatherParser/pull/7); Auto-fix enabled. First hosted CI run of the pull request: 40121861342 (`pull_request`) on `483f6d5`; jobs `windows-x64`, `ubuntu-x64` and `debian13-arm64` succeeded.

## 7. Baseline run (S4)

Baseline run, 2026-10-24: `pwsh ./scripts/ci-local.ps1 -Commit 483f6d5800b8fff6cdba1357513065efb0b41ebc -TargetRef 1e604dc7b3f17826d19207cbec9a699e40e5a30f -RunSettings coverage.runsettings`, exit 0, mode two-pass, 18:41:49Z to 18:42:48Z, run directory `artifacts/ci/483f6d5-20261024T184149Z/` (gitignored). `commits.json`: commit `483f6d5...`, targetCommit `1e604dc...`.

- Tests (candidate pass): Parsing 1662 of 1663 passed (the live test is not executed), Coverage 99, Coverage.Runner 56, SchemaGeneration 46, package consumer 31; 0 failed. Target pass: Parsing 1662 of 1663 passed.
- Changed files (`git diff --name-only 1e604dc...483f6d5`): the seven Markdown files of the seed commit only; documentation-only for the coverage gate.
- Native block counters of `WeatherParser.Parsing.dll`: candidate 6811/6814, target 6811/6814; equal to the P4 end state, so P5.01 plan risk (c) does not apply.
- Gate passed, all five rules true: Comparability, Measurement (6811/6814 valid), Minimum (99.96 % ≥ 80 %), Baseline (6811/6814 valid), Comparison (at or above baseline; documentation-only change, equal coverage allowed). `gate-result.json` SHA-256 `1b780e0423f1b781e33a70d4874da4fed0670531e5090a0a6cf8a575c8c1b3f4` (byte-identical to the P4 post-merge checkpoint run, whose counters and rules were the same).
- Packed package SHA-512 (base64) `ylT+bX64+3MR/lYGSFsKcMQosYgBc4d+/KbHYDvb53TGTuWghV+jld4wxq9qjzaDOW7lJEKmmfNteMxrqd686Q==`; the isolated run rewrote only the consumer lock's `WeatherParser.Parsing` `contentHash` (one line, not committed, C-116).
- This run is also the committed-revision run required for the README change of the seed commit (CONTRIBUTING.md section 4).

Baseline reuse rule (P5.01 plan S4): 6811/6814 is the comparison value for the P5 code steps only while the base of pull request #7 equals `1e604dc` and `coverage.runsettings` is unchanged; both are checked before every reuse, and the target is remeasured when either changed (also in P5.10).

## 8. Findings

None.

## 9. Plan reconciliation (S5)

One contradiction with `PHASE_P5_PLAN.md` section 1: it names `95bcb1e` as the integration target, while the recorded target is its documentation-only successor `1e604dc` (the P5.01 plan's recommended option). Section 1 receives the "P5.01 extraction" table with the recorded target and baseline; the step plans P5.02 to P5.11 refer to `<integration-target-sha>` and need no change. No contract-level change (P5.02).

## 10. Outcome

READY: every extraction field is resolved; the baseline run passed; every commit between `95bcb1e` and `1e604dc` is classified as documentation-only or merge, with no production change.

## 11. Authorship and review

The orchestrating agent ran every command and wrote this record and the status passages (documentation is not generated by qwen, AGENTS.md, user decision dated 2026-10-24); qwen generated nothing in P5.01, because the step changes no code, test, script or data.

Codex five-angle review of the P5.01 changes (seed commit `483f6d5` plus the uncommitted record updates against `1e604dc`), outputs kept outside the repository with the orchestrating agent's session files:

- Round 1 (`codex_r1.md`): 2 findings, both [P3] and confirmed by the orchestrating agent: (1) the extraction row "Remeasured baseline" still read "Open (S4)" after the run; filled with the counters and the run directory; (2) the `PHASE_P5_PLAN.md` status line still said "nothing is implemented or verified yet"; now records P5.01 as executed (READY) and that no production code or test exists yet. Codex confirmed the commit classification, the run values and the SDK statement.
- Round 2 (`codex_r2.md`): 0 findings; both round-1 fixes confirmed. Codex could not query GitHub from its sandbox, so the hosted CI result rests on the orchestrating agent's `gh pr checks 7` reading (all three jobs passed).

The review is closed with 0 open findings.
