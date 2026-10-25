# P5.10 – Integrated Coverage and Platform Verification: Record

**Status (2026-10-25): complete. S0 to S5 executed on candidate `a87c7bb` (outcome READY, section 8). The PR #7 review fixes made `fd461c8` the new implementation candidate; S1 to S3 were rerun on it with outcome READY, S4 not repeated (section 9).**

Plan: [P5_10_INTEGRATED_VERIFICATION_PLAN.md](P5_10_INTEGRATED_VERIFICATION_PLAN.md).

## 1. Decisions

- **User decisions (2026-10-25, in chat)**: "I authorize the P5.10 NanoPi R76S run including the optional live DWD parse." The NanoPi run (S3) follows the P4.11 procedure: SSH access as `ssh R76S`, transfer of a git bundle of the candidate and of the integration target, the `ci-local.ps1` run in the home directory, copying the run directory back and removing the working directory afterwards. The live parse (S4) is enabled for one run.
- **Orchestrator decisions (recorded)**:
  - **Live-runner extension (plan section 3)**: the existing runner gains an observation-only mode. It is selected by `WEATHERPARSER_LIVE_CHECK` with the exact value `1` plus `WEATHERPARSER_LIVE_CHECK_MODE` with the exact value `observation` (`LiveCheckRunner.IsObservationMode`).
  - **Observation entry**: `LiveCheckRunner.RunObservationAsync` resolves only `IDwdObservationParser` and the two serializers, never a MOSMIX or selection service. It parses one station within the request budget `LiveCheckSettings.ObservationRequestBudget` = 8 (four CDC station lists and at most four `now` archives, OC-13). Requests pass through the P4 `RequestBudgetHandler`, which records URI, status, declared length and bytes read; there are no retries.
  - **Mode isolation**: in observation mode the P4 selection live test ends as inconclusive, so the P4 path makes no request.
  - **Station**: `01526` (Fulda-Horas), the nearest CDC station of the P4 live result ([P4.11 record](../p4/P4_11_INTEGRATED_VERIFICATION.md) section 7).
  - **Endpoints**: the P4 source register `LiveCheckSettings.Dwd`, with redirects disabled (`LiveCheckRunner.CreateTransport`).
  - **Output**: the runner returns the dataset, both documents, the schema events, the requests and the parser's notices. `LiveObservationArtifacts.WriteAsync` writes `observation.json`, `observation.xml` and `summary.txt` to a temporary directory outside the repository.
- **Deviations from the plan's test list (recorded)**:
  - The offline tests are in the new file `LiveCheckObservationRunnerTests` instead of the existing runner test files.
  - The plan's `RunIfEnabledAsync_Observation...` names became `RunObservationIfEnabledAsync_...`, after the new entry.
  - `RunAsync_P4Path_MakesNoObservationRequest` was not added, because the P4 path never resolves the observation parser and its existing request-sequence tests stay unchanged and green.
  - The budget test exercises `RequestBudgetHandler` with the observation budget constant directly, because the parser itself never needs more than eight requests.

## 2. S0 Live-check runner observation mode

- **Scope**: test code only (`tests/WeatherParser.Parsing.Tests/Live/`): `LiveCheckRunner`, `LiveCheckSettings`, `LiveCheckResult` (new record `LiveObservationResult`), `LiveCheckTests` (new live test `LiveCheck_DwdObservation_OnlyWhenExplicitlyEnabled`, `[TestCategory("Live")]`, 300 s timeout), the new `LiveObservationArtifacts` and the new `LiveCheckObservationRunnerTests` (9 test methods, 17 cases). No production change.
- **Cases covered**:
  - opt-in (six non-matching variable combinations return `null` before any request; `IsObservationMode` truth table)
  - the request sequence (four lists, then the four archives, exactly 8 requests)
  - no MOSMIX or selection request and no evidence cache written
  - the ninth request refused by the observation budget
  - three missing products recorded as 404 requests and three `OBSERVATION-PRODUCT-MISSING` notices
  - an archive server error propagating as `CatalogRetrievalException` with one request only (no retry)
  - schema-valid, equivalent JSON and XML
  - the written documents (byte for byte) and every summary field
- **Red**:
  - A build failure with CS0117 for the six missing members (`ModeVariable`, `IsObservationMode`, `RunObservationIfEnabledAsync`, `RunObservationAsync`, `ObservationRequestBudget`, `ObservationStationId`).
  - For the artifact test (Codex round-1 fix), CS0103 for the missing `LiveObservationArtifacts`.
- **Green**: live-folder tests 45 passed, 2 skipped (the two live tests); Parsing 1928 passed of 1930 (2 skipped). Build 0 warnings.
- **Codex five-angle review**:
  - Round 1 (`p510_s0_codex_r1.md`), 1 finding, confirmed: the document writing and the summary were exercised only by the live test. The writing moved unchanged into `LiveObservationArtifacts`; the new offline test `RunObservationAsync_WritesObservationJsonXmlAndSummary` was Red first.
  - Round 2 (`p510_s0_codex_r2.md`), 0 findings.
- **Pre-commit gate**: results directory `TestResults/p502-precommit-20261025T052844Z/`; test-only change, production blocks unchanged; 7481/7484.
- **Commit**: `a87c7bb`.
- **Authorship**:
  - qwen3-coder:30b generated every S0 file from the orchestrating agent's briefs.
  - qwen corrected its own defects on compiler-guided briefs: `ContainExactly`, which FluentAssertions 8 lacks; an `async` method without `await`; an unused variable.
  - The orchestrating agent wrote the briefs and this record, and ran every build, test, gate and review.

## 3. Candidate and integration target

- Candidate: `a87c7bb55180561bbbb86dde851fbdce9311f32d` (`a87c7bb`), draft PR #7, branch features/develop/P5.
- Integration target: PR base develop = `1e604dc7b3f17826d19207cbec9a699e40e5a30f`. It was read from `gh pr view 7` (`baseRefOid`) and from `git rev-parse origin/develop` after a fetch on 2026-10-25, and is unchanged since P5.01.

## 4. S1 Windows local CI

- **Command and run**: `pwsh ./scripts/ci-local.ps1 -Commit a87c7bb55180561bbbb86dde851fbdce9311f32d -TargetRef 1e604dc7b3f17826d19207cbec9a699e40e5a30f -RunSettings coverage.runsettings` exited 0. It ran from 2026-10-25T05:29:30Z to 05:30:31Z (1 min 3 s); run directory artifacts/ci/a87c7bb-20261025T052930Z (gitignored).
- **commits.json**: commit `a87c7bb...`, targetCommit `1e604dc...`, mode two-pass, exitCode 0.
- **Tests**:
  - Parsing: 1930 total, 1928 passed, 0 failed; the two live tests not executed.
  - Coverage.Runner 56/56; Coverage 99/99; SchemaGeneration 46/46; package consumer 36/36.
  - Target pass: Parsing 1662 of 1663 (the P4 live test not executed).
- **Gate**: passed. Comparability, Measurement 7481/7484, Minimum (≥ 80 %), Baseline 6811/6814 and Comparison (7481/7484 exceeds 6811/6814, unrounded counters) are all true. gate-result.json SHA-256 `3941203d3f9ff3cf5425fbd2572c26f89ad75515ba656791bc487cc96330c013`.
- **C-116 package evidence**:
  - Package: WeatherParser.Parsing 0.1.0-initial.
  - Packed SHA-512: `okqIt5QbFk4Mtv+s/eSTxUANWPI7Js1nuIUQv1eKXaGeXJWVI80amMlTt52IjaN3LcSBf9SMS/iGDA8DfI448g==` (package-hash.txt, recomputed from the run's nupkg and equal).
  - Entries: _rels/.rels, [Content_Types].xml, lib/net10.0/WeatherParser.Parsing.dll, LICENSE, package/services/metadata/core-properties/nuget.psmdcp, README.md, schemas/export/v1/weatherparser-export-v1.xsd, WeatherParser.Parsing.nuspec.
  - Consumer lock: only the contentHash line was rewritten ("Consumer lock file contentHash rewritten (one line)"); the locked restore succeeded.

## 5. S2 Hosted CI

- **Run**: run 40165707736 on `a87c7bb55180561bbbb86dde851fbdce9311f32d` (pull_request) completed with success; jobs ubuntu-x64, windows-x64 and debian13-arm64 all succeeded.
- **Artifacts**: downloaded with `gh run download 40165707736` (ci-ubuntu-x64 id 12588266870, 2383633 B; ci-windows-x64 id 12588087447, 2430581 B; ci-debian13-arm64 id 12587678312, 2382282 B). Each commits.json names commit `a87c7bb...` and targetCommit `1e604dc...`, with exitCode 0 and mode two-pass, so the SHA pair is verified.
- **Each job**:
  - Parsing 1928 of 1930 passed (the two live tests not executed).
  - Coverage.Runner 56, Coverage 99, SchemaGeneration 46, package consumer 36/36.
  - Target pass: Parsing 1662 of 1663.
  - Gate passed.
- **C-116 per job**: in each job the packed SHA-512 equals the hash recomputed from the artifact's nupkg, and the package has the 8 expected entries.
  - debian13-arm64: `+NQjr4o6E95btkGR5RQYv4224tMESz/n/BX2Q2rE7hclzXPsHFHgiiBzjPu4ebhvFW6FxEK9ylZSyV/XdoFd/A==`
  - ubuntu-x64: `IMcFVFtppFhjDqTJFHuLISV22XVioyEBoERmVorUxCLkhXuBNPuIrMd7RTumfT0iDwzk8ObHUluPbChrzXf39A==`
  - windows-x64: `dHibPZ1+AbAi7EEYaoUUz1oEKpdNCaUPxLWAaU/Gh6be2elwkbvG7Upuf+k8LjAekHlehmwrTXhdXy57FNuTGQ==`
- **Hashes across runs**: the hashes differ between jobs and from the local run, because each job packs its own build (as in P4.11).

## 6. S3 NanoPi R76S (Debian 13 ARM64)

- **Bundle**:
  - Created with `git bundle create weatherparser.bundle features/develop/P5 p510-target`. p510-target was a temporary local branch at `1e604dc`, deleted after bundling.
  - Heads: `a87c7bb` refs/heads/features/develop/P5 and `1e604dc` refs/heads/p510-target; `git bundle verify` reported a complete history.
  - Size 1518776 B, SHA-256 `5cc4942464db2846b27b75e5d9361db9be963f7cccc2f3d82d550cf3c7be0590`, equal on both sides after scp.
- **Target and procedure**:
  - `ssh R76S`: aarch64, Debian GNU/Linux 13 (trixie); working directory ~/weatherparser-p510.
  - Script per CONTRIBUTING.md section 7: clone the bundle, detach at origin/features/develop/P5, create the ci-commit and ci-target branches (`a87c7bb...` and `1e604dc...`), run `bash scripts/prepare-debian-arm64.sh > env.sh` and `. ./env.sh`, then `pwsh ./scripts/ci-local.ps1 -Commit ci-commit -TargetRef ci-target -RunSettings coverage.runsettings`.
- **Environment**:
  - prepare exited 0: .NET SDK 10.0.402 and PowerShell 7.6.7 were already installed (nothing installed); runtime Microsoft.NETCore.App 10.0.13; ICU libicuuc.so.76.
  - dotnet info: OS Platform Linux, OS Version 13, RID linux-arm64, Architecture arm64.
- **Run**:
  - ci-local exited 0; run `a87c7bb-20261025T052958Z`, 05:29:58Z to 05:35:27Z (run completed in 5 min 45 s).
  - The wrapper's final `ls` failed on a carriage return in the command text. It ran after the run had finished and did not affect the run.
  - The run directory was copied back with scp to artifacts/ci/arm64-a87c7bb-20261025T052958Z (gitignored).
  - The working directory was removed afterwards; the older directories weatherparser-p211 and weatherparser-p211b were left untouched.
- **Results**:
  - commits.json: commit `a87c7bb`, targetCommit `1e604dc`, exitCode 0, two-pass.
  - Tests: Parsing 1930 total, 1928 passed, 0 failed (the two live tests not executed); Coverage.Runner 56, Coverage 99, SchemaGeneration 46, package consumer 36/36; target Parsing 1662 of 1663.
  - Gate: passed with all five rules true, 7481/7484 above 6811/6814; gate-result.json SHA-256 `a8e2471c8b0f3cd2a571cbc22a817032c6ade78d24c893cb7ef36989e019cc8a`.
  - Package: packed SHA-512 `5R6x8vM+++g4Y857O/mrNfet7IOu5vntsawg8TamRLIqhcwjJTs9ZeQIP65r5mImLOSsGJw11ItsLTReM+4uoA==`, recomputed equal; the 8 expected entries.
- **Comparison with Windows S1**:
  - The 1930 Parsing test results are identical (test name and outcome) after normalizing the decimal separator of data-row display names: 0 differences.
  - Every result passed except the two live tests (NotExecuted) on both platforms.
  - Block counters are identical, 7481/7484, against baseline 6811/6814.
  - The gate-result.json files have identical content. Their SHA-256 values differ only because the Windows file has CRLF and the NanoPi file LF line endings (equal after line-ending normalization).
  - The package hashes differ because each platform packs its own build (C-116 holds within each run).

## 7. S4 Live parse

- **Conditions**: REQ-092 source conditions as recorded in P5.02 (DWD data under CC BY 4.0, attribution "Deutscher Wetterdienst (DWD)", DWD stores the requesting IP address for at most 7 days); OC-13: one station, one run, budget 8, no retries.
- **Command**: `$env:WEATHERPARSER_LIVE_CHECK='1'; $env:WEATHERPARSER_LIVE_CHECK_MODE='observation'; dotnet test tests/WeatherParser.Parsing.Tests -c Release --no-build --filter "FullyQualifiedName~LiveCheck_DwdObservation_OnlyWhenExplicitlyEnabled"`; both variables were removed afterwards.
- **Run**:
  - Working tree clean at `a87c7bb`; build `--no-incremental` 0 warnings.
  - Started 2026-10-25T05:30:11Z; test duration 512 ms; passed.
  - Output: C:\Users\<user>\AppData\Local\Temp\wp-live-observation-20261025T053013Z (outside the repository).
- **Requests**: 6 of budget 8; all HTTP 200, no redirect, no 404; declared Content-Length equal to bytes read for every request:
  - CDC station lists: tu `466155`, ff `277779`, rr `1377975`, sd `66357` (equal to the P4 live sizes of 2026-10-23)
  - `10minutenwerte_TU_01526_now.zip` `1921`
  - `10minutenwerte_nieder_01526_now.zip` `893`
  
  Station 01526 is in the tu and rr lists only, so no wind or solar archive was requested, as in the P4 live result.
- **Dataset**:
  - source `dwd:01526`, location Fulda-Horas, 50.5668, 9.6532, altitude 245 m.
  - retrieved_at `2026-10-25T05:30:13`, measured_at `2026-10-25T04:50:00`, timestamp_quality `source`.
- **Exported quantities**: each with measured_at `2026-10-25T04:50:00` and quality `2`.
  - temperature_c `8.2`
  - humidity_percent `94`
  - rain_rate_mm_h `0.78`, with period 2026-10-25T04:40:00 to 04:50:00 (0.13 mm per 10 minutes × 6, derived back from the exported value; the raw archive was not retained)
- **Notices**: one `OBSERVATION-READING-OMITTED` for pressure (reason `Missing`), so neither pressure output is exported. This is the parser's reported result. The raw archive was not retained, so the `PP_10` column was not inspected independently. The P4 live availability of this station also lists no pressure. There were no other notices.
- **Documents**: schema events 0 (v1 schema); JSON parsed; JSON ↔ XML equivalence passed (`ExportEquivalence`, P5.08). observation.json `672 B`, observation.xml `1074 B`.
- **Format**: no deviation from the P5.02 contract facts was found.
- **Repetition**: one run, not repeated (SC-16 pattern).

## 8. S5 T-P5 audit, defects and outcome

**Platform evidence for every row**: the S1 Windows run, the S2 hosted run 40165707736 and the S3 NanoPi run, all on `a87c7bb`. Each had 1928 Parsing tests passed (the live tests not executed) and 36 package-consumer tests passed, and its gate passed with 7481/7484 blocks. Step evidence is in [P5_VERIFICATION.md](P5_VERIFICATION.md).

| T-P5 ID | Owning steps | Tests | Step evidence | Result |
|---------|--------------|-------|---------------|--------|
| T-P5-01 | P5.03 | `DwdStationResolverTests` (ID grammar cases), `DwdStationExceptionTests` | section 3, `c01d0fa`, CI 40133178690 | Passed |
| T-P5-02 | P5.03 | `DwdStationResolverTests` (unknown station, metadata, products, conflict) | section 3 | Passed |
| T-P5-03 | P5.04 | `CdcNowEvidenceReaderColumnsTests`; the P4 reader tests unchanged | section 4, `b32985c`, CI 40133908078 | Passed |
| T-P5-04 | P5.05 | `DwdObservationMapperTests` | section 5, `a1a1928`, CI 40136315869 | Passed |
| T-P5-05 | P5.05, P5.08 | `DwdObservationMapperTests`, `DwdObservationExportContractTests` (wire values) | sections 5 and 8 | Passed |
| T-P5-06 | P5.05, P5.08 | `DwdObservationMapperPeriodTests`, `DwdObservationExportContractTests` | sections 5 and 8 | Passed |
| T-P5-07 | P5.05, P5.07, P5.08 | `DwdObservationMapperTests` (calm and zero direction), `DwdObservationParserLoggingTests`, `ComposedDwdObservationPipelineTests`, `DwdObservationExportContractTests` (all calm, all zero direction) | sections 5, 7 and 8 | Passed |
| T-P5-08 | P5.05, P5.08 | `DwdObservationMapperTests` (quality token), `DwdObservationExportContractTests` (quality per measurement) | sections 5 and 8 | Passed |
| T-P5-09 | P5.05, P5.07 | `DwdObservationMapperTimeTests`, `DwdObservationParserTests` | sections 5 and 7 | Passed |
| T-P5-10 | P5.06, P5.07 | `DwdObservationSourceTests`, `DwdObservationSourceDeadlineTests`, `CdcRequestComposerTests`, `CatalogDownloaderObservationDeadlineTests`, `CdcCatalogEndpointsValidatorTests`, `DwdObservationParserDeadlineTests`, `DwdObservationParserFailureTests` | sections 6 and 7, `eba34c2` (CI 40140714640), `18c984a` (CI 40145190771) | Passed |
| T-P5-11 | P5.07 | `DwdObservationParserTests`, `ObservationDatasetBuilderTests`, `ComposedDwdObservationPipelineTests` | section 7 | Passed |
| T-P5-12 | P5.08 | `DwdObservationExportContractTests`, `ObservationExportEquivalenceTests`, `AllRootsEquivalenceTests` | section 8, `b65e660`, CI 40147071920 | Passed |
| T-P5-13 | P5.03 to P5.09, P5.10 | `DwdObservationDependencyInjectionTests`, `PackageDwdObservationTests`, `ServiceRegistrationCompletenessTests`, `NoSharedStateTests`; C-116 package evidence from the committed runs | section 9 (`1301b84`, CI 40148837765, `ci-local.ps1` on `1301b84` exit 0); this record sections 4 to 6 | Passed |
| T-P5-14 | P5.09 | README section "DWD observations (library)"; package check (CONTRIBUTING.md section 4) | section 9 | Passed |
| T-P5-15 | P5.03 to P5.09, P5.10 S0 | Red, Green and Refactor recorded per slice, including first-run characterization tests recorded as existing behaviour | sections 3 to 9; this record section 2 | Passed |
| T-P5-16 | P5.10 | Coverage gate: 7481/7484 (99.96 %) against the integration-target baseline 6811/6814 (99.96 %) remeasured in the same run; minimum 80 %; strict improvement on unrounded counters; comparable collection settings | this record sections 4 to 6 | Passed |
| T-P5-17 | P5.10 | All P5 tests and the gate on the NanoPi R76S, separate from Windows; identical results after decimal-separator normalization | this record section 6 | Passed |
| T-P5-18 | P5.10 | `LiveCheck_DwdObservation_OnlyWhenExplicitlyEnabled` with the offline `LiveCheckObservationRunnerTests` | this record sections 2 and 7 | Passed (one run) |
| T-P5-19 | P5.07 | `DwdObservationParserLoggingTests`, `DwdObservationParserFailureTests` | section 7 | Passed |

### Defects

- No open defect. No production defect was found in P5.10; production code is unchanged since P5.07 (P5.08, P5.09 and P5.10 S0 changed tests, test helpers and documentation only).
- The Codex finding of S0 concerned the new test-only runner mode and was fixed test-first before the candidate was committed (section 2).

### Observations (not defects)

- The packed package is not byte-reproducible across builds: each run's packed SHA-512 differs, while within every run the restored hash equals the packed hash (C-116).
- The live station 01526 currently reports no pressure; the omission is logged with its reason. The documented pressure path is covered by the synthetic tests (T-P5-05).
- The `now` data are not yet fully quality-controlled; the live values had `QN` 2 (README limitation, OC-07).

### Outcome

- **READY** for P5.11:
  - every mandatory T-P5 row passes on Windows and on the NanoPi R76S
  - the coverage gate passed
  - hosted CI is green for the candidate with the verified SHA pair
  - no defect is open
  - the optional live parse ran once within the OC-13 and REQ-092 conditions
- **Open limitations (not passes)**:
  - Release hardware acceptance stays with P9 (REQ-084).
  - The live parse is a single snapshot of 2026-10-25 and is not repeated (SC-16 pattern).
  - The hosted `debian13-arm64` container is not counted as target evidence.
  - CLI (`--ParseDWD`, P3), MQTT (P8) and scheduled operation are later phases.
- The P5 acceptance decision belongs to the user in P5.11.

### Authorship and review

- qwen3-coder:30b generated the S0 test code (section 2). The orchestrating agent ran every command of S1 to S4, extracted every value from the run directories, artifacts and live output, and wrote this record.
- Codex reviewed this record read-only:
  - Round 1 (`p510_doc_r1.md`), 4 findings, all confirmed and fixed by the orchestrating agent:
    - the pressure statement went beyond the retained evidence (now qualified as the parser's result);
    - the offline case count was wrong (17, not 19);
    - the gate-result hash difference is only a line-ending difference;
    - the review rounds of this record were not recorded.
  - Further rounds are listed in [P5_VERIFICATION.md](P5_VERIFICATION.md) section 10.

## 9. Rerun on candidate `fd461c8` after the PR #7 review fixes

- **Trigger**: the peer review of `6492091` on PR #7 (2026-10-25) reported two Minor findings. The user instructed in chat: "Retry the commit and push of the PR #7 review fixes, reply on the two review threads, then rerun Windows ci-local, the NanoPi run and hosted CI on the new candidate (no new live parse) and update the P5.10 records."
- **Finding 1 (production fix, confirmed)**:
  - **Defect**: the CDC reader accepts `MESS_DATUM` `000101010000`. For a wind, precipitation or solar row the mapper's period start `AddMinutes(-10)` then threw `ArgumentOutOfRangeException` instead of a documented failure.
  - **Red**: new cases in `DwdObservationMapperPeriodTests` for ff, rr and sd at 0001-01-01 00:00 and 00:09 UTC; all 6 failed with `ArgumentOutOfRangeException`. The three 00:10 cases (period start 0001-01-01 00:00) passed on their first run.
  - **Green**: `DwdObservationMapper.Reading` throws `CatalogFormatException` `CDC-EVIDENCE-INVALID` before constructing a period that cannot be represented; instantaneous readings are not restricted. Parsing 1937 passed, 2 skipped.
- **Finding 2 (test fix, confirmed)**:
  - **Gap**: every package observation test served the TU archive only, so the packaged wind, precipitation and solar conversions and periods were never exercised.
  - **Fix**: `ConsumerSelectionHost.ArrangeCdcFullStation` serves the ff, rr and sd archives as well, and `ConsumerFixtures.CdcNowZip` writes each product's DWD header. The complete-station and expected-graph tests now assert in JSON and XML: `wind_speed_kmh` 12 (3.47 m/s), `wind_direction_deg` 270, `rain_rate_mm_h` 3 (0.5 mm per 10 minutes) and `solar_irradiance_w_m2` 600 (36.0 J/cm² per 10 minutes). Each has `measured_at` 2026-10-21T11:50:00, quality 3 and the period 11:40:00 to 11:50:00; the parse makes 8 requests. The missing-product test keeps the TU-only arrangement.
  - **First run**: characterization; the new expectations passed on their first run against the freshly packed library. Package consumer 36/36.
- **Review and gate**:
  - Codex five-angle review of the fix (`pr7_codex_r1.md`): 0 findings.
  - Pre-commit gate: `TestResults/p502-precommit-20261025T060545Z/`, 7491/7494 against 6811/6814 (the guard adds 10 covered blocks).
  - Commit `fd461c8`; replies posted on both review threads.
- **Authorship**:
  - qwen3-coder:30b generated the regression tests, the guard and the package-test changes.
  - qwen also switched the missing-product test to the full station, which the brief said to leave unchanged; it restored the TU-only arrangement on a precise correction brief.
  - The orchestrating agent wrote the briefs and this record, and ran every build, test, gate and run.
- **Candidate**: `fd461c8cededddf53a039c31adad68a110c1c7c9` (`fd461c8`) against the unchanged integration target `1e604dc7b3f17826d19207cbec9a699e40e5a30f`.
- **S1 Windows**:
  - `ci-local.ps1` exit 0, 06:53:46Z to 06:54:52Z; run directory artifacts/ci/fd461c8-20261025T065346Z. commits.json: commit `fd461c8...`, targetCommit `1e604dc...`, two-pass, exitCode 0.
  - Tests: Parsing 1939 total, 1937 passed, 0 failed (the two live tests not executed); Coverage.Runner 56, Coverage 99, SchemaGeneration 46, package consumer 36/36; target Parsing 1662 of 1663.
  - Gate: passed with all five rules true, 7491/7494 above 6811/6814; gate-result.json SHA-256 `964ac345c54cc205f1839828f919219f5a28c4650c5bf801fa6d198fe22d87b2`.
  - Package: packed SHA-512 `H3eh/g0+Ip6jMDvoYOzEqalfXN/T80+4Ukjxa8Y0+c+/98sz2trIVwG1PXDryKJupM/YVuKO/eKP/Qr/g2km3Q==`, recomputed equal, 8 entries; consumer lock: only the contentHash line rewritten.
- **S2 Hosted CI**:
  - Run 40170729670 (pull_request) succeeded: debian13-arm64, ubuntu-x64 and windows-x64.
  - Artifacts: ci-debian13-arm64 id 12590250740, 2385008 B; ci-windows-x64 id 12590161103, 2434908 B; ci-ubuntu-x64 id 12589453846, 2388432 B.
  - Each commits.json names `fd461c8...` and `1e604dc...`, with exit 0 and mode two-pass.
  - Each job: Parsing 1937 of 1939 passed, Coverage.Runner 56, Coverage 99, SchemaGeneration 46, package consumer 36/36, target Parsing 1662 of 1663; gate 7491/7494; 8 package entries.
  - Packed SHA-512, each equal to its recomputed hash:
    - debian13-arm64 `k5VSa3fSAFyhbV+jfmqcjalQoeW2CckhwIHnLETS1zYi9DcP0F5a8dwCZyOdilmzLLFi+4X74TxS0t46fORZmA==`
    - ubuntu-x64 `l5H7I6zm7yAxGwd4CftxGATOL40YwUaNOOR0vyw2h0rEgD9aRFm+bDlywYzWx0vDSJfuP/zIUhiTEy7IflmsjA==`
    - windows-x64 `ybG7HG17gQb7EEi6BuOInu+G8Jvd1Lb4gHZc2Y5OTMSRpjhZk3rTUrVMEKxFEJfBbas6xrkaf+9kcWZnpcY7AA==`
- **S3 NanoPi R76S**:
  - Bundle: features/develop/P5 at `fd461c8` and the temporary branch p510-target at `1e604dc` (deleted after bundling), complete history. Size 1532232 B, SHA-256 `63ef0b2e023bc26a8b0e5d8ac81e9f5c36a39d3c394f81d878bb875ecdd7ae7a`, equal on both sides.
  - Procedure as in section 6, working directory ~/weatherparser-p510b; prepare exit 0 (nothing installed; SDK 10.0.402, PowerShell 7.6.7, runtime 10.0.13, ICU 76).
  - Run: ci-local exit 0, run `fd461c8-20261025T065355Z` (06:53:55Z to 06:59:18Z; run completed in 5 min 40 s). Copied back to artifacts/ci/arm64-fd461c8-20261025T065355Z; working directory removed afterwards.
  - Results: Parsing 1937 of 1939 passed, Coverage.Runner 56, Coverage 99, SchemaGeneration 46, package consumer 36/36, target Parsing 1662 of 1663; gate passed 7491/7494; gate-result.json SHA-256 `7030168b9feac3710a9a67a89035adb5147637c872bdd5bd2811e467344f2e10`; packed SHA-512 `pTeA9PGxJRxg5tG5lx53YuMDsdCN2kj5tJqQuIT/fpaqWSJRm3rUhm/qWBb6/gdXUzfEESOjFi6OMaHvgZHL/Q==`, recomputed equal.
  - Comparison with S1: all 1939 Parsing test names and outcomes identical after decimal-separator normalization (0 differences; only the two live tests NotExecuted). The gate-result.json content is identical after line-ending normalization.
- **S4**: not repeated. The live row T-P5-18 rests on the single run of section 7 on `a87c7bb` (SC-16 pattern). The fix changes only the rejection of timestamps before 0001-01-01 00:10, which the live data cannot reach.
- **T-P5 audit**:
  - The section 8 table holds for `fd461c8`, with platform evidence from this section.
  - T-P5-06 gains the boundary cases of `DwdObservationMapperPeriodTests`.
  - T-P5-13 gains the full-station `PackageDwdObservationTests`.
  - No open defect.
- **Outcome**: **READY** for P5.11 on candidate `fd461c8`.
- **Codex review of this section** (`pr7_doc_r1.md`): 0 findings.
