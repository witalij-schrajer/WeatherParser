# P2.01 – Foundation Check Record

**Status (2026-10-20): executed; outcome READY. P2 implementation is authorized; P2.02 starts on a separate instruction.**

This record executes [the P2.01 plan](P2_01_FOUNDATION_CHECK_PLAN.md) and section 1 of [PHASE_P2_PLAN.md](../../PHASE_P2_PLAN.md).

## 1. Authorization

- 2026-10-20, user message: "execute p2.01 step".
- 2026-10-20, user answer to the orchestrator's question whether this also authorizes P2 implementation from P2.02 onward: selected option "Authorize P2 implementation (Recommended)", whose description read "Record 'execute p2.01 step' plus this answer as the P2 implementation authorization; READY then lets P2.02 start on a separate instruction."
- 2026-10-20, user answer on the sanity-run scope (a P2.01 plan section 3 decision): "Full two-pass run (Recommended)".
- Handling of an available SDK servicing update: not needed (section 5), so no decision was required.

## 2. Accepted records unchanged

`git diff --stat 2cbee61 HEAD -- docs/p1 PHASE_P1_PLAN.md` returned no changes. Inspected acceptance/handoff document revision: `2cbee612df1159c8d1a9415223d3a1db95f979ca`. Records: [P1 acceptance record](../p1/P1_12_ACCEPTANCE.md), [P2 handoff](../p1/P2_HANDOFF.md), [P1 plan](../../PHASE_P1_PLAN.md).

## 3. Commits since the inspected revision

| Commit | Files | Classification |
|--------|-------|----------------|
| `c1d3b43` | `.github/workflows/ci.yml`, `AGENTS.md`, `CONTRIBUTING.md`, `DEPENDENCIES.md` | Hosted CI workflow and its documentation; no product or test code |
| `b4df185` | `.github/workflows/ci.yml`, `CONTRIBUTING.md` | Hosted CI fix and first hosted run record; no product or test code |
| `f433d94` | `DEVELOPMENT_PLAN.md`, `PHASE_P2_PLAN.md` | P2 phase plan |
| `0dc3f4c` | `DEVELOPMENT_PLAN.md`, `PHASE_P2_PLAN.md` | P2 phase-plan review revision |
| `ad43ed6` | `PHASE_P2_PLAN.md`, twelve `docs/p2/P2_*_PLAN.md` files | P2 step plans |

Candidate for this check is `features/develop/P2` HEAD `ad43ed6015f12e6b78fdbf1f2347a8da6a95d878`. Remote `develop` moved from `22b3ae2` to `64e460b475b4a29b9e3ddd331394076e53982d9e` (merge of pull request #2, which brought `c1d3b43` and `b4df185`); `git diff --stat 6ac88e7 origin/develop -- src tests scripts coverage.runsettings global.json Directory.Packages.props` is empty, so the PR base's product, test, script and toolchain files equal the accepted foundation `6ac88e7`.

## 4. Field-by-field revalidation

| Field | Accepted value (source) | Revalidated value (2026-10-20) | Result |
|-------|-------------------------|--------------------------------|--------|
| Accepted implementation | `6ac88e783ce10fa6d676860ba831ee371abd9cdf` (P1.12) | `commits.json` of the retained run: `6ac88e783ce10fa6d676860ba831ee371abd9cdf`, exit code 0 | Match |
| Package SHA-512 (base64) | `ld4iecLFargjHkaan1xitVHa/j5GuV5VanGuoCR2olCRDetWgY8h/dqNEF76Di5fLpnrQLpDqORsZK85oYd8jQ==` | recomputed from the retained `.nupkg` | Match |
| Package SHA-256 | `819f64425820f739f222345f0a9d79caa899dabafedfcf036e72a0ea314022fb` | recomputed | Match |
| Retained gate result | 54/56 against 52/56, all five rules passed; SHA-256 `8c76191964f208952bc97603b185a2b790783864408467310a3217cf08fae269` | `artifacts/ci/6ac88e7-20261019T213031Z/gate-result.json` present, same counters and SHA-256 | Match |
| SDK / runtime | 10.0.402 / 10.0.13 | `dotnet --version` 10.0.402; `global.json` 10.0.402, `rollForward` `disable`, `allowPrerelease` false; runtime Microsoft.NETCore.App 10.0.13 | Match |
| Coverage toolchain | Microsoft.CodeCoverage 18.11.0, dotnet-coverage 18.11.0 | `Directory.Packages.props` 18.11.0; `.config/dotnet-tools.json` 18.11.0 | Match |
| Run settings | SHA-256 `dc6f662e4f6faaa853c33ed5497f182a31527fd126961411a604b683ffcf05d2` | `Get-FileHash coverage.runsettings` same value | Match |
| Exclusions | `*.Tests.dll`, `WeatherParser.Coverage*.dll` | passed by `scripts/ci-local.ps1` to the runner (not in the run-settings file) | Match |
| Package identity | `WeatherParser.Parsing` 0.1.0-initial | csproj `<Version>0.1.0-initial</Version>` | Match |
| Test counts | Parsing 15, coverage checker 99, coverage runner 56, package consumer 2 | identical in the sanity run (section 6) | Match |
| Platform evidence | P1.10 record and compatibility matrix | [P1.10 record](../p1/P1_10_DEBIAN_ARM64.md) and [compatibility matrix](../COMPATIBILITY.md) present and unchanged | Match (P1 scope only; no new ARM64 run in P2.01) |

## 5. SDK servicing check

Source: official release metadata `https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json`, read 2026-10-20. Latest release 10.0.13 (2026-10-13, security release) with runtime 10.0.13 and SDKs 10.0.402 and 10.0.113; latest SDK 10.0.402; support phase active; end of life 2028-11-14. Result: the pinned SDK 10.0.402 is the current servicing SDK; no update needed; `global.json` unchanged.

## 6. Sanity run

Sanity run on `ad43ed6`:

- Working tree: locked restore `dotnet restore WeatherParser.Ci.slnf --locked-mode` exit 0; `dotnet build WeatherParser.Ci.slnf -c Release --no-restore` exit 0 with 0 warnings and 0 errors; `dotnet test` of `tests/WeatherParser.Parsing.Tests` 15 passed, `tests/WeatherParser.Coverage.Tests` 99 passed, `tests/WeatherParser.Coverage.Runner.Tests` 56 passed, 0 failed, 0 skipped.
- First two-pass attempt: `pwsh ./scripts/ci-local.ps1 -Commit ad43ed6015f12e6b78fdbf1f2347a8da6a95d878 -TargetRef 64e460b -RunSettings coverage.runsettings` exit 20 (setup/validation). The current pass completed; the baseline pass failed at `git checkout --detach 64e460b…` with "fatal: unable to read tree", because the script's `git clone --no-local` copies only local branches and tags and `64e460b` was reachable only from the remote-tracking ref `origin/develop`. Run directory `artifacts/ci/ad43ed6-20261020T050856Z` (failed, kept as evidence).
- Remedy: a local branch `develop` tracking `origin/develop` was created (`git branch --track develop origin/develop`, pointing at `64e460b475b4a29b9e3ddd331394076e53982d9e`); no other repository change.
- Second attempt: `pwsh ./scripts/ci-local.ps1 -Commit ad43ed6015f12e6b78fdbf1f2347a8da6a95d878 -TargetRef 64e460b475b4a29b9e3ddd331394076e53982d9e -RunSettings coverage.runsettings` exit 0 in 31 s; run directory `artifacts/ci/ad43ed6-20261020T050951Z` (local, gitignored). Build 0 warnings in both passes. Tests: Parsing 15, coverage checker 99, coverage runner 56, package consumer 2, all passed; target pass Parsing 15 passed. Consumer lock: the one permitted `contentHash` line rewritten. Package `WeatherParser.Parsing.0.1.0-initial.nupkg`, SHA-512 `9oPSIk0P7InYFphM0hB/2vHHeX+iMy2b48gGWRfnl0u1300qRnr5Cd1zdX3xbbBAfW+f4aQ0ZikVDgf71jDxBg==` (equal to `package-hash.txt`), SHA-256 `b5fd96ebd5ab06f18d84a0a0a5ed37bb6f4a26c1084a92b7f92926557a7a0baf`; it differs from the P1 package because the package embeds the commit ID. Collection settings of both passes identical except `commit`. Changed files (14): `DEVELOPMENT_PLAN.md`, `PHASE_P2_PLAN.md` and the twelve step plans — documentation only.
- Gate (`gate-result.json`, SHA-256 `36aa8f6f6aa5bd9f82b58caad8e2e31368b80b270f25f917b07e11fd944e2916`): passed; current 54/56, remeasured target `64e460b` 54/56; Comparability, Measurement, Minimum (96.43% ≥ 80%), Baseline and Comparison passed; Comparison passed as equal ("at or above baseline 54/56") because the diff is documentation-only.

The sanity run is a precondition check, not P2 acceptance evidence; no ARM64 run was part of P2.01.

## 7. Findings

1. The P2 integration target changed from `22b3ae2` (planning time) to `64e460b` (current `develop`); its product code equals `6ac88e7`, and it was remeasured at 54/56. P2.02's first code candidate must strictly exceed 54/56 against the then-current PR base.
2. Documentation gap: CONTRIBUTING.md section 6 does not state that `-TargetRef` must be reachable from a local branch or tag of the repository clone, because the script clones with `git clone --no-local`; a commit reachable only from a remote-tracking ref fails with exit 20. Owner: a documentation fix in a later change (user decides when); P2.01 changed no procedure documents.
3. A docs-only candidate passes by equality; this does not apply to P2.02 code candidates (phase plan section 8).

## 8. Plan reconciliation

`PHASE_P2_PLAN.md` section 1 gains a revalidation paragraph linking this record and noting the new target; section 8's "currently develop at 22b3ae2" becomes `64e460b`; `docs/p2/P2_02_CONTRACT_AND_FIRST_SLICE_PLAN.md` section 2 likewise. The extraction table itself stays unchanged because every field matched.

## 9. Outcome

READY. All preconditions hold: authorization recorded; accepted records unchanged; every field matches; SDK current; sanity run passed with the remeasured target. No file under `docs/p1/` and neither `global.json` nor `Directory.Packages.props` was modified. P2.02 starts on a separate user instruction.

## 10. Authorship

The orchestrating agent recorded the authorization, ran every command, verified every value against its source and made the READY decision (AGENTS.md: recorded evidence, run results and user decisions are not qwen work). qwen3-coder:30b drafted this document's prose from the orchestrator's verified facts and corrected it in one review round; the orchestrator reviewed it and removed two formatting artifacts caused by its own brief (doubled backticks, a stray end marker).
