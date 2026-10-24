# P4.11 – Integrated Coverage and Platform Verification: Record

**Status (2026-10-24): complete. S0 to S5 executed on candidate `98122e5` (outcome READY, section 8). The A10 review fixes made `b6bc47f` the new implementation candidate; S1 to S3 were rerun on it with outcome READY, S4 not repeated (section 9). Evidence checkpoint `807d29a`, hosted CI run 40026481905 green.**

Plan: [P4_11_INTEGRATED_VERIFICATION_PLAN.md](P4_11_INTEGRATED_VERIFICATION_PLAN.md).

## 1. Decisions

- **User decisions (2026-10-23, in chat)**:
  - The NanoPi R76S run (S3) is authorized: SSH access as `ssh R76S`, transfer of a git bundle of the candidate and of `develop`, the `ci-local.ps1` run in the home directory, copying the run directory back, cleanup of the working directory; tool installation only if `prepare-debian-arm64.sh` finds something missing, then the orchestrating agent stops and asks.
  - The optional live check is enabled: build the runner test-first (S0), commit it, then one live run (S4).
- **Orchestrator decisions (recorded)**:
  - Opt-in through the environment variable `WEATHERPARSER_LIVE_CHECK` with the exact value `1`; without it `LiveCheckRunner.RunIfEnabledAsync` returns before any other work and the live test `LiveCheck_DwdSources_OnlyWhenExplicitlyEnabled` (`[TestCategory("Live")]`) ends with `Assert.Inconclusive`, so default runs report it as skipped and make no network access.
  - Request budget = request count of one MOSMIX_S refresh, one MOSMIX selection and one CDC selection with `MaxEvidenceCandidates` 10: 1 + 1 + 10 + 4 + 40 = 56, enforced by `RequestBudgetHandler` (refuses the request beyond the budget, records URI, status, declared `Content-Length` and bytes actually read).
  - The live transport is `LiveCheckRunner.CreateTransport()`, a `SocketsHttpHandler` with automatic redirects disabled, so every HTTP request passes the budget and a redirect fails as `CATALOG-RETRIEVAL-FAILED`.
  - The runner resolves every service before the first request.
  - Sequence: MOSMIX_S refresh into a temporary cache directory, MOSMIX selection, CDC selection, both documents serialized to JSON and XML and validated against the v1 schema.
  - Endpoints from the SC source register (`LiveCheckSettings.Dwd`).
  - Search area 50.55, 9.68 (Fulda), 280 m, 15 km, 300 m altitude difference.

## 2. S0 Live-check runner

- **Scope**: Test code only, no production code; live runs only with the explicit opt-in.
- **Design**: The runner is implemented in `LiveCheckRunner.cs`, with test scaffolding in `LiveCheckRunnerTests.cs` and `LiveCheckRunnerFailureTests.cs`.
- **Coverage split and output**: the runner tests cover the opt-in guard, the request budget, the complete sequence, missing, oversized, invalid and stale indexes, refresh and selection deadlines, the request timeout, too many candidates, invalid limits, a server error without retry, a too large and a malformed catalog and caller cancellation; the library bounds at their exact limits (1 MiB, 8 MiB, 64 MiB) stay covered by P4.08. The live test writes the four documents and `summary.txt` (budget, requests with status, declared length and bytes read, counts, index timestamps, schema events, notices) to a temporary directory outside the repository and deletes its cache directory.
- **Red**: 26 failures on the runner stub (`NotImplementedException`), among them the live test; `RequestBudgetHandler_RefusesTheRequestBeyondTheBudget` passed on its first run because the handler was part of the scaffold.
- **Green**: 1607 Parsing tests pass, the live test is skipped; three test defects fixed by qwen (issue time of the MOSMIX_S fixture is 2026-10-23T03:00:00, the stale-index assertion targets the MOSMIX_S item, four HTTP 404 responses instead of five — two of them brief errors of the orchestrating agent).
- **Codex review**: Round 1, 3 findings, all confirmed and fixed test-first: response sizes only from `Content-Length` (now bytes read are counted; Red `RequestBudgetHandler_ResponseWithoutContentLength_CountsBytesRead`), three fixture URLs with wrong file names (corrected, exact evidence URLs asserted), automatic redirects bypassing the budget (`CreateTransport` disables them; `CreateTransport_DisablesAutomaticRedirects` passed on its first run because qwen implemented the method instead of the briefed stub). Round 2: 0 findings.
- **Checks**: `dotnet build WeatherParser.Ci.slnf -c Release --no-incremental` 0 warnings; Parsing 1609 pass and 1 skipped (live test), SchemaGeneration 46, Coverage 99, Coverage.Runner 56; pre-commit coverage gate before the round-1 fixes 6484/6487 above 3035/3037, exit 0 (test code is excluded from the block metric).
- **Authorship**: qwen3-coder:30b generated the runner, the tests and this record text from the orchestrating agent's briefs; the orchestrating agent wrote the briefs, ran every build, test, gate and review, reviewed each diff and added three missing `using` directives and fixed one typo (`EvidenceCacheCacheException`) by hand.

## 3. Candidate and integration target

- Candidate (implementation candidate containing S0): `98122e560370e9de781bd4e0e3ebe0aca270e3d8` (`98122e5`), draft PR #5, branch features/develop/P4.
- Integration target: PR base develop = `b7da8288d8746bc6e034222c0467a8fcebba55b3` (`gh pr view 5` baseRefOid and `git rev-parse origin/develop`, 2026-10-23). Local `develop` (74bf625) differs and was not used.

## 4. S1 Windows local CI

- `pwsh ./scripts/ci-local.ps1 -Commit 98122e560370e9de781bd4e0e3ebe0aca270e3d8 -TargetRef b7da8288d8746bc6e034222c0467a8fcebba55b3 -RunSettings coverage.runsettings`, exit 0, 2026-10-23T20:59:49Z to 21:00:42Z (53 s), run directory artifacts/ci/98122e5-20261023T205949Z (gitignored); commits.json: commit `98122e5...`, targetCommit `b7da828...`, mode two-pass, exitCode 0.
- Tests: Parsing 1610 total, 1609 passed, 0 failed (1 skipped: the live test); Coverage.Runner 56/56; Coverage 99/99; SchemaGeneration 46/46; Package consumer 31/31; target pass Parsing 880/880.
- Gate: passed; Comparability, Measurement 6484/6487, Minimum (>= 80 %), Baseline 3035/3037, Comparison (6484/6487 = 99.95 % exceeds 3035/3037 = 99.93 %) all true; gate-result.json SHA-256 `79ed1e5bf910f26c28e3757502ce95047a3131946d7da8bd19275a259220b5fb`.
- C-116: WeatherParser.Parsing 0.1.0-initial; packed SHA-512 `7P7sYMqKWtEJXRIUZUYeASFJAZFhtd1rnrwgzOqSrmY/3kb9j+tYGx0b3txYLpnDgbuWHeoWw1S+BGbZoz21VA==` (package-hash.txt, recomputed equal); entries _rels/.rels, WeatherParser.Parsing.nuspec, LICENSE, README.md, lib/net10.0/WeatherParser.Parsing.dll, schemas/export/v1/weatherparser-export-v1.xsd, [Content_Types].xml, package/services/metadata/core-properties/nuget.psmdcp; consumer lock: only the WeatherParser.Parsing contentHash line rewritten to the packed hash, locked restore succeeded, 31/31.

## 5. S2 Hosted CI

- Run 40023445539 on `98122e560370e9de781bd4e0e3ebe0aca270e3d8` (pull_request): completed success; jobs ubuntu-x64, windows-x64, debian13-arm64 success.
- Artifacts downloaded with `gh run download 40023445539` (ci-windows-x64 id 12532245700, 1663381 B; ci-ubuntu-x64 id 12531452964, 1617662 B; ci-debian13-arm64 id 12530809186, 1614416 B); each commits.json: commit `98122e5...`, targetCommit `b7da828...`, exitCode 0, mode two-pass (SHA pair verified).
- Each job: Parsing 1610/1609 passed (live test skipped), Coverage.Runner 56, Coverage 99, SchemaGeneration 46, Package 31/31, target Parsing 880/880; gate passed with 6484/6487 (3 not covered); consumer lock: contentHash line rewritten (one line); run durations 01:10 (arm64), 01:38 (ubuntu), 02:11 (windows).
- C-116 per job (packed SHA-512 = recomputed from the artifact's nupkg; entries the expected 8): debian13-arm64 `U0xMLKODMABR9tsdP6mO+Kc8yFvsqhuH0lXqY6aQiu1kepsF7eCT4F6Xt0QGlsDjLnD0ymej6XqSoh4yoQK9yA==`; ubuntu-x64 `C+xIuFjKo+V/5JlPSOCuLSi0cBNFJaWNmeVwbXphnwX3m6jcfYlE9i4GknMQpkWmTzsEw/QGiniDpHbZEe/GLA==`; windows-x64 `geB3EFHxS/WoVJLf/j+i4SBm3sUs4VeLSHVxH2Sv0RDIhAz4wCuuubazy6rgIN/G8tz5ET0flAixOCwxlx2zvQ==`. The hashes differ between jobs and from the local run because each job packs its own build (the package is not byte-reproducible across builds); within each run the restored hash equals the packed hash.

## 6. S3 NanoPi R76S (Debian 13 ARM64)

- Bundle: `git bundle create weatherparser.bundle features/develop/P4 p411-target` (p411-target = temporary local branch at b7da828, deleted after bundling, because local develop 74bf625 differs from the PR base); heads `98122e5` refs/heads/features/develop/P4 and `b7da828` refs/heads/p411-target; 1232656 B, SHA-256 `e5d106971d0dab451f1a43cef78537d82fd5204dd31bc78679ddabdb16313ea2`, equal on both sides after scp.
- Target: `ssh R76S`, aarch64, Debian GNU/Linux 13 (trixie); working directory ~/weatherparser-p411; script = CONTRIBUTING.md section 7 (clone bundle, detach origin/features/develop/P4, ci-commit and ci-target branches, `bash scripts/prepare-debian-arm64.sh > env.sh`, `. ./env.sh`, `pwsh ./scripts/ci-local.ps1 -Commit ci-commit -TargetRef ci-target -RunSettings coverage.runsettings`); ci-commit=`98122e5...`, ci-target=`b7da828...`; prepare exit 0: .NET SDK 10.0.402 and PowerShell 7.6.7 already installed (nothing installed), runtime Microsoft.NETCore.App 10.0.13, ICU libicuuc.so.76; ci-local exit 0, run `98122e5-20261023T210033Z`, 4 min 25 s; copied back with scp to artifacts/ci/arm64-98122e5-20261023T210033Z (gitignored); working directory removed afterwards (older directories weatherparser-p211 and weatherparser-p211b left untouched).
- dotnet info: OS Platform Linux, OS Version 13, RID linux-arm64, Architecture arm64.
- Results: commits.json commit `98122e5`, targetCommit `b7da828`, exitCode 0, two-pass; Parsing 1610 total / 1609 passed / 0 failed (live test not executed), Coverage.Runner 56, Coverage 99, SchemaGeneration 46, Package 31/31, target Parsing 880/880; gate passed, Comparability/Measurement/Minimum/Baseline/Comparison true, 6484/6487 above 3035/3037; gate-result.json SHA-256 `fc76812305d2055440f77956f27f305b80bcb648b2346aa30ea62c039c471b26`; packed SHA-512 `hKlt6WlOi/PQwjoQtGl6TxYF8zU+8ZcCkLsAHmvtwSmKau6ml2/WWOrzWx+pCpF7qutWuqzhwuglNFZe9H5ttw==`; consumer lock contentHash line rewritten (one line).
- Comparison with Windows S1: same 1610 Parsing test results; the 40 differing TRX test names differ only in the decimal separator of data-row display names (German Windows culture "0,1" vs "0.1"); after normalizing it 0 differences; every result Passed except the live test (NotExecuted) on both; 20 geodesy-related results (names matching Distance, SearchArea, Geodes...) all passed on ARM64 within their test tolerances; identical block counters 6484/6487 and baseline 3035/3037. gate-result.json and package hashes differ between platforms (separate builds and environment fields), as expected.

## 7. S4 Live check

- Command: `$env:WEATHERPARSER_LIVE_CHECK='1'; dotnet test tests/WeatherParser.Parsing.Tests -c Release --no-build --filter "FullyQualifiedName~LiveCheck_DwdSources_OnlyWhenExplicitlyEnabled"` (variable removed afterwards), working tree clean at `98122e5`, build 0 warnings; started 2026-10-23T21:01:15Z, test duration 14 s, passed; output C:\Users\<user>\AppData\Local\Temp\wp-live-check-20261023T210116Z (outside the repository).
- Requests 14 of budget 56; all HTTP 200, no redirect; declared Content-Length equal to bytes read for every request:
  MOSMIX_S all-stations `37470861`; MOSMIX catalog `299502`; MOSMIX_L `10536` `18239`, `N1012` `17814`, `L585` `18112`, `L684` `18135`, `L582` `18157`; CDC lists tu `466155`, ff `277779`, rr `1377975`, sd `66357`; now files nieder_01526 `710`, TU_01526 `1522`, nieder_03911 `689`.
- Station lists and catalog sizes equal the source-register sizes of 2026-10-21 (466155, 277779, 1377975, 66357, 299502).
- MOSMIX_S index downloaded_at `2026-10-23T21:01:23`, issued_at `2026-10-23T20:00:00`.
- CDC: retrieved_at `2026-10-23T21:01:29`, 2 stations: 01526 Fulda-Horas 2.66 km (humidity_percent, rain_rate_mm_h, temperature_c), 03911 Petersberg-Marbach 8.16 km (rain_rate_mm_h); CDC candidates 2.
- MOSMIX: retrieved_at `2026-10-23T21:01:28`, 5 locations (candidates 5): L585 FULDA 1.87 km, 10536 FULDA 2.13 km, L582 PETERSBERG-MARBACH 7.86 km, L684 THALAUBACH 13.90 km, N1012 KALBACH 14.86 km; each MOSMIX_S available (40 elements, evidence_retrieved_at `21:01:23`, issued `20:00:00`) and MOSMIX_L available (110 elements, evidence_retrieved_at `21:01:29`, issued `15:00:00`).
- Schema events: 0 (CDC), 0 (MOSMIX). Documents: cdc.json `672 B`, cdc.xml `1234 B`, mosmix.json `8005 B`, mosmix.xml `22000 B`.
- Notices: one CATALOG-RECORD-SKIPPED (catalog line 1530, station 37531, minutes above 59) = the GORI record documented in the contract section 3.2; no other notice; no format deviation found.
- one run, not repeated (SC-16)

## 8. S5 T-P4 audit, defects and outcome

Intro sentence: "Every row is mapped to its passing tests and recorded evidence. Platform evidence for every row: the S1 Windows run, the S2 hosted run 40023445539 and the S3 NanoPi run on `98122e5`, each with 1609 Parsing tests passed (the live test not executed), 31 package-consumer tests passed and the gate passed with 6484/6487 blocks. Step evidence is in [P4_VERIFICATION.md](P4_VERIFICATION.md)."

| T-P4 ID | Owning steps | Tests | Step evidence | Result |
|---------|--------------|-------|---------------|--------|
| T-P4-01 | P4.03 | `CoordinateParserTests` | section 3, `a3b79d7`, CI 39737759157 | Passed |
| T-P4-02 | P4.03 | `CoordinateParserDmsTests` | section 3, `a3b79d7`, CI 39737759157 | Passed |
| T-P4-03 | P4.03, P4.09 | `SearchParameterFactoryTests`; parameter tests of `CdcStationSelectorLimitTests` and `MosmixLocationSelectorLimitTests` | sections 3 and 9 | Passed |
| T-P4-04 | P4.04 | `DistanceCalculatorTests` (converging Vincenty reference pairs within 1 mm, spherical calculations and the near-antipodal fallback within 0.6 %, SC-06; also passed on the NanoPi, S3) | section 4, `abe6d64`, CI 39810906820 | Passed |
| T-P4-05 | P4.04 | `SearchAreaFilterTests`, `SearchAreaFilterCompositionTests` | section 4 | Passed |
| T-P4-06 | P4.05 | `CdcStationCatalogReaderTests`, `CdcStationCatalogReaderMalformedTests`, `CdcStationCatalogReaderBoundaryTests` | section 5, `0195b4f`, CI 39828777889 | Passed |
| T-P4-07 | P4.06 | `CdcAvailabilityDeriverSelectCandidatesTests`, `CdcAvailabilityDeriverDeriveTests`, `CdcNowEvidenceReaderTests` | section 6, `643475f`, CI 39840675493 | Passed |
| T-P4-08 | P4.07 | `MosmixLocationCatalogReaderTests`, `...LineTests`, `...MalformedTests`, `...NegativeBoundsTests`, `...NormalizationTests` | section 7, `0f29842`, CI 39905420541 | Passed |
| T-P4-09 | P4.07, P4.09 | `MosmixAvailabilityResolverTests`, the five `MosmixLEvidenceReader*Tests`, the four `FileSystemMosmixEvidenceCache*Tests`, `MosmixLocationListContractTests` (explicit `unknown`), `MosmixLocationSelectorTests` | sections 7 and 9 | Passed |
| T-P4-10 | P4.07, P4.08 | `MosmixCatalogEndpointsValidatorTests`, `CdcCatalogEndpointsValidatorTests`, `CatalogSourceConstructionTests`, `CatalogServiceArgumentTests`, the six `CatalogDownloader*Tests` | sections 7 and 8, `97ced0b`, CI 39982260348 | Passed |
| T-P4-11 | P4.09 | `CdcStationSelectorTests`, `CdcStationSelectorLimitTests`, `ComposedCdcPipelineTests` | section 9, `f22bc7a`, CI 40010632025 | Passed |
| T-P4-12 | P4.09 | `MosmixLocationSelectorTests`, `MosmixLocationSelectorLimitTests`, `ComposedMosmixPipelineTests` | section 9 | Passed |
| T-P4-13 | P4.09, P4.10 | `SelectionExportContractTests`; package side `PackageSelectionEquivalenceTests` | sections 9 and 10 | Passed |
| T-P4-14 | P4.10, P4.11 | `SelectionDependencyInjectionTests`, `SelectionDependencyInjectionRoutingTests`, `PackageSelectionTests`, `PackageMosmixEvidenceTests`, `PackageSelectionEquivalenceTests`; C-116 package evidence from the committed runs | section 10 (`2f85c2f`, CI 40019053309, `ci-local.ps1` on `2f85c2f`); this record sections 4 to 6 | Passed |
| T-P4-15 | P4.03 to P4.10, P4.11 S0 | Red, Green and Refactor recorded per slice, including first-run characterization tests recorded as existing behaviour | sections 3 to 10; this record section 2 | Passed |
| T-P4-16 | P4.11 | Coverage gate: 6484/6487 (99.95 %) against the integration-target baseline 3035/3037 (99.93 %) remeasured in the same run; minimum 80 %; strict improvement (baseline below 100 %); unrounded native counters; comparable collection settings | this record sections 4 to 6 | Passed |
| T-P4-17 | P4.11 | All P4 tests and the gate on the NanoPi R76S, separate from Windows; identical results after decimal-separator normalization of display names | this record section 6 | Passed |
| T-P4-18 | P4.11 | `LiveCheck_DwdSources_OnlyWhenExplicitlyEnabled` with the offline `LiveCheckRunnerTests` and `LiveCheckRunnerFailureTests` | this record sections 2 and 7 | Passed (one run) |
| T-P4-19 | P4.07 to P4.10 | the four `FileSystemMosmixEvidenceCache*Tests`, the six `MosmixSEvidenceRefresher*Tests`, the index tests of `MosmixLocationSelectorTests` and `MosmixLocationSelectorLimitTests`, `ComposedMosmixPipelineTests`, `PackageMosmixEvidenceTests`, the index cases of `LiveCheckRunnerTests` | sections 7 to 10 | Passed |
| T-P4-20 | P4.07, P4.10 | `MosmixEvidenceTimestampTests`, `MosmixEvidenceTimestampWireTests`, `SchemaRefinementTests` (schema generator), `PackageEvidenceTimestampTests`, `PackageMosmixEvidenceTests` | sections 7 and 10 | Passed |

### Defects

- No open defect. No production defect was found in P4.11; production code is unchanged since P4.09.
- The three Codex findings of S0 concerned the new test-only live-check runner and were fixed test-first before the candidate was committed (section 2).

### Observations (not defects)

- The local `develop` branch (`74bf625`) differs from the PR base `b7da828`; the PR base was used throughout.
- The packed package is not byte-reproducible across builds: each run's packed SHA-512 differs, while within every run the restored hash equals the packed hash (C-116).
- The live MOSMIX catalog still contains the GORI record with a minute part of 60, skipped with one `CATALOG-RECORD-SKIPPED` notice as the contract documents (section 3.2 of the selection contract).

### Outcome

- **READY** for P4.12: every mandatory T-P4 row passes on Windows and on the NanoPi R76S, the coverage gate passed, hosted CI is green for the candidate and no defect is open; the optional live check ran once within the SC-09, SC-16 and SC-22 conditions.
- Open limitations (not passes): release hardware acceptance stays with P9 (REQ-084); the live check is a single snapshot of 2026-10-23 and is not repeated (SC-16); the hosted `debian13-arm64` container is not counted as target evidence; DI under load, CLI, MQTT and live retrieval in normal operation are later phases.
- The P4 acceptance decision belongs to the user in P4.12.

### Authorship and review

- Authorship of this evidence checkpoint (sections 3 to 8 of this record, the P4 section of `docs/COMPATIBILITY.md`): the orchestrating agent ran every command, extracted every value and wrote the facts lists and the T-P4 mapping; qwen3-coder:30b drafted the prose of sections 3 to 8 and the COMPATIBILITY.md section from them (a second qwen request added the missing Limits bullet); the orchestrating agent reviewed every value against the run directories and artifacts and wrote the status lines, section 11 of `P4_VERIFICATION.md` and the review-driven corrections (tolerance wording, this statement) itself.
- Codex records review: round 1, 2 findings (P3), both confirmed and fixed: the geodesy tolerance stated too broadly as 1 mm (now 1 mm for converging Vincenty pairs and 0.6 % for spherical calculations and the near-antipodal fallback), and the missing authorship statement of this checkpoint. Round 2: 0 findings.
- Evidence checkpoint: the records were committed as `807d29a` on the user's request (2026-10-23) and pushed to draft pull request #5 (documentation only; the implementation evidence of candidate `98122e5` stays valid, plan section 3); hosted CI run 40026481905 on `807d29a` succeeded on all three jobs (ubuntu-x64, windows-x64, debian13-arm64) under the full-PR gate, the coverage gate included; on debian13-arm64 the Parsing tests passed 1609 of 1610 (live test not executed), SchemaGeneration 46/46, Coverage 99/99, Coverage.Runner 56/56 and the package consumer 31/31. P4.11 is complete.

## 9. Rerun on candidate b6bc47f (A10 review round 1, 2026-10-24)

### Reason
- The review of PR #5 (A10, P4.12 plan) raised three comments; their test-first fixes (record: `P4_12_ACCEPTANCE.md` section 8.1) changed production code in `MosmixSEvidenceRefresher`, `MosmixLocationSelector` and `FileSystemMosmixEvidenceCache`, so commit `b6bc47f89147c05bf04e06cf0f87c1497bc651f3` (`b6bc47f`) is a new implementation candidate and the A10 rule requires S1 to S4 again (plan section 5, last paragraph: offline and platform verification restart, another live execution is skipped under SC-16).
- Integration target unchanged: PR base `b7da8288d8746bc6e034222c0467a8fcebba55b3` (`gh pr view 5` baseRefOid and `git rev-parse origin/develop` after `git fetch`, 2026-10-24).

### S1 Windows local CI
- `pwsh ./scripts/ci-local.ps1 -Commit b6bc47f89147c05bf04e06cf0f87c1497bc651f3 -TargetRef b7da8288d8746bc6e034222c0467a8fcebba55b3 -RunSettings coverage.runsettings`, exit 0, 2026-10-24T03:15:37Z to 03:16:35Z (run 56 s), run directory artifacts/ci/b6bc47f-20261024T031537Z (gitignored); commits.json: commit `b6bc47f...`, targetCommit `b7da828...`, mode two-pass, exitCode 0; build warnings 0.
- Tests: Parsing 1663 total, 1662 passed, 0 failed (1 not executed: the live test); Coverage.Runner 56/56; Coverage 99/99; SchemaGeneration 46/46; Package consumer 31/31; target pass Parsing 880/880.
- Gate: passed; Comparability, Measurement 6811/6814, Minimum (>= 80 %), Baseline 3035/3037, Comparison (6811/6814 = 99.96 % exceeds 3035/3037 = 99.93 %) all true; the three uncovered blocks are the known ones (`ParserEndpointValidator` constructor 2, `FileSystemMosmixEvidenceCache.DeleteTemporaryFile` 1); gate-result.json SHA-256 `2c995e7e6352f7cc37f9c45398100c0b11db5171eb270196c7d147a23f55e881`.
- C-116: WeatherParser.Parsing 0.1.0-initial; packed SHA-512 `dCCn3uSuMMvrF1eT6hAudmi8isancaeohI9aDhsT4fj6Ya5RBz28cdDG11LRLuXDYCoOt/YezbVFvFD6plAsTQ==` (package-hash.txt, recomputed equal); the same 8 entries as section 4; consumer lock: only the WeatherParser.Parsing contentHash line rewritten (one line), locked restore succeeded, 31/31.

### S2 Hosted CI
- Run 40041773136 on `b6bc47f89147c05bf04e06cf0f87c1497bc651f3` (pull_request, created 2026-10-23T23:49:16Z): completed success; jobs ubuntu-x64, windows-x64, debian13-arm64 success.
- Artifacts downloaded with `gh run download 40041773136` (ci-windows-x64 id 12539692071, 1697248 B; ci-ubuntu-x64 id 12539573017, 1651920 B; ci-debian13-arm64 id 12539143975, 1650214 B); each commits.json: commit `b6bc47f...`, targetCommit `b7da828...`, exitCode 0, mode two-pass (SHA pair verified).
- Each job: Parsing 1662 of 1663 passed (live test not executed), Coverage.Runner 56, Coverage 99, SchemaGeneration 46, Package 31/31, target Parsing 880/880; gate passed with 6811/6814, all five rules true; consumer lock: contentHash line rewritten (one line); run durations 01:15 (arm64), 01:49 (ubuntu), 02:24 (windows).
- C-116 per job (packed SHA-512 = recomputed from the artifact's nupkg; 8 entries): debian13-arm64 `N3sR1unaXl3k8+hozHaLIBwNMAkfbcVJFd7fJHo1N4KGCqKe72QEdXxoulqJ2b0JalUKVQW81DmFEQtVia5pzw==`; ubuntu-x64 `6npTRl/d8Nbsk0vlkYiVfPpH4NK5QAGVHVPfeE5taf7u4vzjgb7cf1vYGzq3Vg8baEsQmu8IHdTcaagWBVageQ==`; windows-x64 `Ty3aCkjnVvCoQs1tzoUDsNxauD3JP5COmXq8PG18B036gKW1P3OTkJmupl9d0Xtty96jWFZJmhqN16mqvFOcmg==`; as in section 5 the hashes differ between builds.

### S3 NanoPi R76S (Debian 13 ARM64)
- Authorization: the user's instruction of 2026-10-24 to rerun P4.11 S1 to S4 on `b6bc47f`, under the terms recorded in section 1.
- Bundle: `git bundle create weatherparser.bundle features/develop/P4 p411-target` (p411-target = temporary local branch at b7da828, deleted after bundling); heads `b6bc47f` refs/heads/features/develop/P4 and `b7da828` refs/heads/p411-target; 1269277 B, SHA-256 `948ac2bea057586b4594269ed8b7f35f57c1782eabc9e3e13e74416df4ea96d5`, equal on both sides after scp.
- Target: `ssh R76S`, aarch64, Debian GNU/Linux 13 (trixie); working directory ~/weatherparser-p411r; same script as section 6; ci-commit=`b6bc47f...`, ci-target=`b7da828...`; prepare exit 0: .NET SDK 10.0.402 and PowerShell 7.6.7 already installed (nothing installed), runtime Microsoft.NETCore.App 10.0.13, ICU libicuuc.so.76; ci-local exit 0, run `b6bc47f-20261024T031752Z`, 4 min 44 s (2026-10-24T03:17:49Z to 03:22:37Z); copied back with scp to artifacts/ci/arm64-b6bc47f-20261024T031752Z (gitignored); working directory removed afterwards (weatherparser-p211 and weatherparser-p211b left untouched).
- dotnet info: OS Platform Linux, OS Version 13, RID linux-arm64, Architecture arm64.
- Results: commits.json commit `b6bc47f`, targetCommit `b7da828`, exitCode 0, two-pass; Parsing 1663 total / 1662 passed / 0 failed (live test not executed), Coverage.Runner 56, Coverage 99, SchemaGeneration 46, Package 31/31, target Parsing 880/880; build warnings 0; gate passed, all five rules true, 6811/6814 above 3035/3037; gate-result.json SHA-256 `43b22ca5f5762ff5df597adebdb66ffaaf29a0e49ac3016b59a3e9d555fbd5d6`; packed SHA-512 `ccLueUXv0KDJf0rBASUzSnWEgGfGi/pE3Bes2eDeFUvUeYF2Hpq3+ddgHyVWINK8EPgy/A9gK5cRYXPQHr7KMQ==` (recomputed equal, 8 entries); consumer lock contentHash line rewritten (one line).
- Comparison with the S1 rerun: 1663 Parsing results on both; 40 differing TRX entries differ only in the decimal separator of data-row display names; after normalizing it 0 differences; every result Passed except the live test (NotExecuted) on both; 20 geodesy-related results (names matching Distance, SearchArea, Geodes) all passed on ARM64 within their test tolerances; identical block counters 6811/6814 and baseline 3035/3037.

### S4 Live check
- Not repeated (SC-16: one run, not repeated; plan section 5, last paragraph). The single live run of section 7 was on candidate `98122e5`; the refresher it exercised changed with `b6bc47f`. This is recorded as an open limitation, not as a live pass of `b6bc47f`.

### Offline check of the new MOSMIX_S parser against a stored real file
- Input: the real MOSMIX_S all-stations file stored during the P4.02 source-fact check (stored as `S_latest.kmz`, downloaded 2026-10-21, 37475070 B, SHA-256 `3195d0074e10f14409d2aa2a69c658384e48cbabb1bf237acfa3589a72f410a2`, kept outside the repository).
- Method: a throwaway console probe outside the repository composed `AddWeatherParsing` with an `HttpClient` whose handler served the stored file for every request, ran one `IMosmixSEvidenceRefresher.RefreshAsync` into a temporary cache directory and read the index back through `IMosmixEvidenceCache`; run once with the library built from `98122e5` and once with the library built from `b6bc47f`.
- Result, identical for both: issued_at `2026-10-21T03:00:00`, 5648 locations, 205719 element entries, at most 40 elements per location, SHA-256 of the canonical index listing (locations in ordinal order with their element names) `2675461f543391c9b98a7cfac31df18820789a177c8e00d079ea6fab96e7d9d2`; refresh 5.4 s (`98122e5`) and 5.6 s (`b6bc47f`); cumulative managed allocation during the refresh 6610 MiB (`98122e5`) and 3540 MiB (`b6bc47f`) (cumulative, not peak memory).

### Outcome
- **READY** for the P4.12 refresh: every mandatory T-P4 row passes on Windows and on the NanoPi R76S for `b6bc47f`, the coverage gate passed, hosted CI run 40041773136 is green with the verified SHA pair and no defect is open; the live-check row T-P4-18 rests on the single run of section 7 on `98122e5` (SC-16), supplemented by the offline check above.

### Authorship
- The orchestrating agent ran every command, extracted every value and checked them against the run directories and artifacts; qwen3-coder:30b drafted this section from the agent's facts list (a first request that rewrote the whole record hit the output limit and was discarded); the orchestrating agent appended the section, wrote the status line and corrected the input file name of the offline check.
- Codex five-angle review of the records refresh (this section, the P4.12 acceptance record, the P5 handoff, COMPATIBILITY.md, P4_VERIFICATION.md section 11): round 1, 0 findings.
