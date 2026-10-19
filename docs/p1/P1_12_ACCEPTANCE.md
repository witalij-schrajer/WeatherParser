# P1.12 – P1 Acceptance Record

**Audit decision (2026-10-19): ACCEPTED for the P1 package/test/coverage foundation. Platform runtime status: PROVEN for the P1-scope checks executed on the NanoPi R76S (Debian 13 ARM64). P1 accepted by the user on 2026-10-19 ([acceptance](#p1-acceptance-2026-10-19)).** Implements the [P1.12 plan](P1_12_ACCEPTANCE_HANDOFF_PLAN.md); the next phase is described in the [P2 handoff](P2_HANDOFF.md).

This is acceptance of the P1 foundation only, not release readiness (P9), not full hardware acceptance (P9) and not a hosted GitHub CI result.

## 1. Review identity

| Field | Value |
|---|---|
| Review date | 2026-10-19 |
| Reviewer | Claude (orchestrating LLM) on the user's instruction; documentation drafts by the local qwen3-coder:30b model, reviewed by Claude; formal acceptance: the user |
| Candidate | `6ac88e783ce10fa6d676860ba831ee371abd9cdf` (`features/develop/initial`, local): the P1.11 accepted state `654bf56`, the P1.12 documents, the T-P1-06 test correction (section 12) and the corrections of the second review (section 13). Intermediate candidate `463eae5` (`artifacts/ci/463eae5-20261019T211011Z`) is superseded. The first audit run used `654bf56` (`artifacts/ci/654bf56-20261019T205341Z`, exit 0, same gate result); this run supersedes it |
| Target | `0a2dabeaa4b4baa836b87d7a4aa1a456e3dcb552`, the genuine initial bootstrap seed `C` (P1.09), an ancestor of the candidate |
| Script revision | `scripts/ci-local.ps1` at the candidate (last changed in `660dc7e`, P1.10) |
| Working-tree exclusions | None: the working tree was clean, and the run tests only the committed candidate from a fresh clone |
| Command | `pwsh ./scripts/ci-local.ps1 -Commit 6ac88e7 -TargetRef 0a2dabe -RunSettings coverage.runsettings` (two-pass, no `-Bootstrap`) |
| Run ID | `artifacts/ci/6ac88e7-20261019T213031Z/` (local, gitignored, retained) |

**Initial bootstrap (plan section 4).** No accepted integrated baseline existed before P1.09, so the first fault-free full pipeline run on the committed seed `0a2dabe` is the genuine bootstrap: `-Commit 0a2dabe -TargetRef 0a2dabe -Bootstrap`, exit 0, 52/56 blocks. Its seed artifacts (`current/bootstrap-baseline.json`, collection settings, SHA-256 provenance) are recorded in the [P1.09 record](P1_09_LOCAL_CI.md#genuine-initial-bootstrap-seed-2026-10-19). The candidate is a later commit, so it is evaluated by the normal two-pass comparison against `0a2dabe`, which is remeasured in the same run with identical settings. Bootstrap was not used again.

## 2. Candidate run results

- **Exit:** 0. Every stage passed: SDK check, locked restore, Release build, the four test projects, pack and package-content check, package consumer, conversion, target pass and gate.
- **Toolchain:** .NET SDK 10.0.402 and .NET runtime 10.0.13 (host, `win-x64`), recorded separately. `Microsoft.CodeCoverage` 18.11.0; `dotnet-coverage` 18.11.0 (manifest and tool); collector "Code Coverage"; configuration Release; RID and converter RID `win-x64`.
- **Collection settings:** run-settings SHA-256 `dc6f662e4f6faaa853c33ed5497f182a31527fd126961411a604b683ffcf05d2` (`coverage.runsettings`, static managed instrumentation), exclusions `*.Tests.dll`, `WeatherParser.Coverage*.dll`. Both passes are identical except `commit`.
- **Tests (current pass):** WeatherParser.Parsing.Tests 15, WeatherParser.Coverage.Tests 99, WeatherParser.Coverage.Runner.Tests 56, WeatherParser.Package.Tests 2; all passed, none skipped (10 new regression tests from section 13). The target pass ran WeatherParser.Parsing.Tests: 8 passed. Build warnings: 0 in both passes.
- **Package:** `WeatherParser.Parsing.0.1.0-initial.nupkg`, SHA-512 `ld4iecLFargjHkaan1xitVHa/j5GuV5VanGuoCR2olCRDetWgY8h/dqNEF76Di5fLpnrQLpDqORsZK85oYd8jQ==`. Entries: `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE`, the nuspec, `[Content_Types].xml`, `_rels/.rels` and `nuget.psmdcp` only.
- **Consumer:** the only lock change in the disposable clone was the documented one-line `contentHash` rewrite. The locked restore passed, the restored hash was checked against the packed hash (a mismatch would exit 70), and the consumer tests passed.
- **Native block counters:** current `WeatherParser.Parsing.dll` 54/56 (96.43%), target 52/56 (92.86%). One raw `.coverage` report per pass.
- **Change scope:** 35 files since the target, including `coverage.runsettings`, `scripts/ci-local.ps1`, `scripts/prepare-debian-arm64.sh`, `tests/WeatherParser.Parsing.Tests/EndpointUriValidatorTests.cs` the three test classes corrected in section 12 and the coverage-tooling, test and script changes of section 13. Classified as a **code** change, with no documentation bypass.
- **Gate verdicts:** Comparability passed ("Baseline is comparable."); Measurement passed ("Block counters 54/56 are valid."); Minimum passed (54/56 ≥ 80%); Baseline passed ("Baseline block counters 52/56 are valid."); **Comparison passed** ("Block coverage 54/56 (96.43%) exceeds baseline 52/56 (92.86%).").
- **SHA-256 of the run artifacts:** `.nupkg` `819f64425820f739f222345f0a9d79caa899dabafedfcf036e72a0ea314022fb`; `gate-result.json` `8c76191964f208952bc97603b185a2b790783864408467310a3217cf08fae269` (identical to the earlier candidate runs, because the gate content is the same); `commits.json` `1b47a1af9afcbe2cec2f9595b3a768a2b5fdce05406bbac8b525b712c96b6357`; `current/collection-settings.json` `91541a0fe3a038c750231121396faee4dedab83753fe7e66152d69c00b428c13`; `target/collection-settings.json` `1add8ed80136636ca8a347f808eae752bc4740a51166a624845c18ec7d0b1b95`; `current/coverage/coverage.xml` `4044206cd1ef56e5a9cd11712191a981ff8e332114a8c268c4d94ed18815821d`.

## 3. Coverage provenance (T-P1-24)

- **Module:** the current report contains one `WeatherParser.Parsing.dll` module (id `FBFB226F3D1F1742B3F17CC3E084EBE3DB2D0AED`), counted once, and the test module `WeatherParser.Parsing.Tests.dll`, which the exclusions remove. The library is measured from source-reference tests only. The package consumer is not instrumented, so the package assembly is not double-counted.
- **Source identity:** the report lists the 5 library source files that contain executable code, each with a SHA-256 checksum. All 5 match the candidate's committed blobs as checked out with CRLF line endings (`core.autocrlf=true`): `EndpointUriValidator.cs`, `ParserEndpointDefinition.cs`, `ParserEndpointValidationException.cs`, `ParserEndpointValidator.cs`, `ServiceCollectionExtensions.cs`. The two interface files contain no executable code and are absent, as expected.
- **Limitations:** the native report carries no assembly version, PDB GUID or package identity. The module `id` is not claimed to be a PDB identity. Build and package identity are established independently: the run builds, packs and consumes the package from the same clone and commit, and records the package hash. The package also embeds the commit ID (nuspec `repository commit`, informational version `+<SHA>`; P1.09 finding 5).
- **Separate gates:** checker and runner self-coverage are not part of the product gate (`WeatherParser.Coverage*.dll` is excluded). Their behaviour is covered by the 99 + 56 meta-tests.

## 4. Platform evidence (P1.10)

**Status: PROVEN for the checks executed on the target. No deferral was needed.** The target runs used script revision `660dc7e`. The later corrections (sections 12 and 13: test setup, coverage-tooling overflow and exclusion checks, resume input hashes) were verified on Windows only. They tighten failure detection and change no product code; the target was not re-run, because its working directory and installs were removed after P1.10 and no new target authorization was requested.
- **Target:** NanoPi R76S, Debian 13.3 (trixie), `aarch64`, SDK 10.0.402 and runtime 10.0.13 in user directories.
- **Run on the target:** full two-pass run of `660dc7e` against `c6cb86a`, exit 0. It ran locked restore, Release build, 162 tests, pack, package-content check, and the package consumer against the package built on the target. Conversion and gate also ran on the target (`converterRid` `linux-arm64`); gate PASSED with 54/56 against 52/56.
- **Fault injections on the target:** `FailingTest` exit 50; `RunSettingsTamper` exit 85.
- **Split flow:** `-CollectOnly` on the target, then `-ResumeFrom` on Windows. Collection host `linux-arm64`, conversion and gate host `win-x64`, kept distinct in the settings files. It passed after the ARM64 work directory had been deleted, and a tampered runner exits 20.

Windows and ARM64 counters are equal for the same commits; that comparison is descriptive only. Evidence: [P1.10 record](P1_10_DEBIAN_ARM64.md), [COMPATIBILITY.md](../COMPATIBILITY.md). Not covered: serializers, timezones, DI under load, CLI, MQTT, network sources (later phases) and the REQ-084 release and hardware checks (P9).

## 5. Evidence index P1.01–P1.11

| Package | Status | Record | Revision / run | Notes and superseding evidence |
|---|---|---|---|---|
| P1.01 | Complete (2026-10-15) | [P1_01_BASELINE.md](P1_01_BASELINE.md) | historical | P0 completion and P1 scope; inventory preserved byte-identical (P1.11 check) |
| P1.02 | Complete (2026-10-15) | [P1_02_TOOLING_REVIEW.md](P1_02_TOOLING_REVIEW.md) | historical | Its "SDK update open" finding is closed by P1.03 (SDK 10.0.402); current versions and licenses: [DEPENDENCIES.md](../../DEPENDENCIES.md) |
| P1.03 | Complete | [P1_03_BUILD_SETUP.md](P1_03_BUILD_SETUP.md) | historical | The record still says clean-checkout reproducibility is outstanding; superseded by P1.09 (fresh-clone runs) and this candidate run |
| P1.04 | Complete (2026-10-16) | [P1_04_PACKAGE_CONSUMER.md](P1_04_PACKAGE_CONSUMER.md) | historical | Its 2 consumer tests are re-run in every CI run, here 2 passed against the same-run package |
| P1.05 | Complete | [P1_05_TDD_BASELINE.md](P1_05_TDD_BASELINE.md) | historical | Original Red→Green→Refactor of `ParserEndpointValidatorTests`; historical counts superseded by this run |
| P1.06 | Complete | [P1_06_DI_CONTRACTS.md](P1_06_DI_CONTRACTS.md) | historical | `AddWeatherParsing`, real DI resolution; tests re-run here |
| P1.07 | Complete | [P1_07_COVERAGE_CONTRACT.md](P1_07_COVERAGE_CONTRACT.md) | historical | Native report contract and per-assembly mapping; P1.10 added static instrumentation with identical Windows counters |
| P1.08 | Accepted 2026-10-19 | [P1_08_COVERAGE_EVALUATION.md](P1_08_COVERAGE_EVALUATION.md#p108-acceptance-2026-10-19) | `b6100e2`, `a562f63` | The header still reads "not yet accepted"; the dated acceptance section supersedes it. The missing original Red-step evidence was accepted by the user |
| P1.09 | Accepted 2026-10-19 | [P1_09_LOCAL_CI.md](P1_09_LOCAL_CI.md#p109-acceptance-2026-10-19) | `0a2dabe` seed; script up to `532bd71` | Bootstrap seed; fault injections; P1.10 correction about the tool resolver cache appended |
| P1.10 | Accepted 2026-10-19 | [P1_10_DEBIAN_ARM64.md](P1_10_DEBIAN_ARM64.md#p110-acceptance-2026-10-19) | `660dc7e`; `arm64-660dc7e-*` runs | Class A and Class B evidence; target cleaned up afterwards |
| P1.11 | Accepted 2026-10-19 | [P1_11_DEVELOPER_INSTRUCTIONS.md](P1_11_DEVELOPER_INSTRUCTIONS.md#p111-acceptance-2026-10-19) | `654bf56` | See section 6 |

## 6. P1.11 handover (plan step 7)

- **Result revision and part (b):** P1.11's record covers part (b) for P1.08 closure, P1.09 and P1.10. No item is Planned or OPEN; NETSDK1141 stays a historical remedy.
- **Checks recorded:** English-only rules and the comment scan (inline/block, XML docs, scripts and configuration: 83 files, no finding); current status and commands (walkthrough of `ba93447`, re-verification of `8a96f58` and `d721940`); versions and package sources (single-source check); links; the 84 requirement IDs in identical order; license and inventory preservation; the byte-identical package-readme check.
- **Named handover items:** none. P1.11 handed over no OPEN platform item.
- **Changes after the P1.11 revision `654bf56`:** the P1.12 documents (`bd3f257`, `084b5fd`), the T-P1-06 test correction (`463eae5`, section 12) and the second-review corrections (`5435c63`, `6ac88e7`, section 13), all inside the candidate. Its README status-line change was re-checked by a CI run (documentation-only, exit 0) before this audit.
- **Acceptance impact:** none.

## 7. Test-ID matrix (T-P1-01 – T-P1-24)

All tests named below ran and passed in the candidate run unless a row says otherwise. Historical evidence is labelled as such.

| ID | Outcome | Evidence | Classification |
|---|---|---|---|
| T-P1-01 | Pass | Historical Red→Green→Refactor in P1.05 (`ParserEndpointValidatorTests`); current behaviour tests pass. P1.08's missing Red step was accepted by the user; P1.10's `EndpointUriValidatorTests` specify existing behaviour, verified by two mutations (P1.10 record) | Foundation, met |
| T-P1-02 | Pass | `ValidateAndMap_WithInvalidUri_ThrowsParserEndpointValidationException` (English message "has an invalid URI"); no retrieval exists in the library | Foundation, met |
| T-P1-03 | Pass | `ValidateAndMap_WithTwoDistinctValidEndpoints_MapsEachWithoutConfusion`, `ValidateAndMap_WithDuplicateEndpointName_ThrowsParserEndpointValidationException` | Foundation, met |
| T-P1-04 | Pass | Interfaces `IEndpointUriValidator`, `IParserEndpointValidator`; constructor injection (P1.05, P1.06 records); no service locator in the library | Foundation, met |
| T-P1-05 | Pass | `AddWeatherParsing_WithoutFurtherRegistration_ResolvesParserEndpointValidator`, `AddWeatherParsing_ResolvedValidator_PerformsRealValidation`, `GetRequiredService_WithoutAnyRegistration_ThrowsInvalidOperationException`, `GetRequiredService_WithOnlyParserEndpointValidatorRegistered_ThrowsInvalidOperationException` | Foundation, met |
| T-P1-06 | Pass after correction | Failed in the first audit: `CoverageGateRealReportTests` (P1.08) and `ServiceCollectionExtensionsTests` (P1.06) built their subjects inside the tests, and `RunnerEndToEndTests` (P1.08) built the runner per call. Returned to the owners and corrected in `463eae5` (section 12). Now every class with a subject or shared dependency builds it in `[TestInitialize]`; `PackageConsumptionTests` has no subject; strict mocks verified with `VerifyAll()` in `[TestCleanup]` (`ParserEndpointValidatorTests` and the coverage test classes); tests without mocks need no cleanup | Foundation, met |
| T-P1-07 | Pass | No HTTP, MQTT, socket or device access in `src`, `tests` or `tools` (source search); URIs are `example.org` test data only; the CI clone restores only from nuget.org and the local feed | Foundation, met |
| T-P1-08 | Pass | Fresh-clone locked restore in this run; the only lock change is the reviewed consumer `contentHash` rewrite; drift detection: P1.09 `LockDrift` exit 30, `ConsumerLockDrift` exit 70 | Foundation, met |
| T-P1-09 | Pass | `Evaluate_80_20_Null_Code_True_AllRulesPass`, `Evaluate_79_21_Null_Code_True_MinimumFailsWhileBaselinePasses`; candidate 54/56 ≥ 80% | Foundation, met |
| T-P1-10 | Pass | `Evaluate_85_15_Vs_85_15_Code_ComparisonFailsEqualityBelow100`, `Evaluate_86_14_Vs_85_15_Code_ComparisonPasses`, `EqualBaseline_CodeChange_FailsComparison`; this candidate strictly improves (54/56 > 52/56); P1.09's two-pass exit 90 for an equal code change | Foundation, met |
| T-P1-11 | Pass | `ReadAsync_CoberturaFormat_ThrowsCoverageInputException`, `ReadAsync_ModuleWithoutBlockCounters_ThrowsCoverageInputException`, `NonNativeOrIncompleteReport_IsInputError` | Foundation, met |
| T-P1-12 | Pass | `Evaluate_99999_1_Vs_100_0_Code_ComparisonFailsRounded100NotExact`, `ReadAsync_Rounded100Report_ReturnsExactCounters`, `Rounded100_AgainstExact100Baseline_FailsComparison` | Foundation, met |
| T-P1-13 | Pass after correction | `ReadAsync_MissingReport_…`, `ReadAsync_MalformedXml_…`, `MalformedOrMissingReport_IsInputError`, `ZeroDenominator_FailsMeasurement`; script level: P1.09 `MissingCoverage` exit 80; overflowing counters are rejected since section 13 (`ReadAsync_ModuleCountersSumExceedsInt64_ThrowsCoverageInputException`, `ReadAsync_ExistingFile_CountersSumExceedsInt64_ThrowsCoverageInputException`, `OverflowingCounters_IsInputError`) | Foundation, met |
| T-P1-14 | Pass | `Evaluate_85_15_Null_Code_False_BaselineFailsMissingAfterBootstrap`, `NoBaselineNoBootstrap_FailsBaselineRule`, `SuppliedBaselineMissing_IsInputError`, `PassingReport_P109BootstrapInvocation_Passes`; genuine bootstrap on `0a2dabe` (section 1) | Foundation, met |
| T-P1-15 | Pass | `Check_StaleCommit_Fails`, `ReadAsync_StaleCommitFixture_ReadsCommitUnchanged`; script: external baseline commit mismatch exits 20 (P1.09 review fixes) | Foundation, met |
| T-P1-16 | Pass | `Check_DifferentToolName_Fails`, `Check_DifferentToolVersion_Fails`, `Check_DifferentFormat_Fails`, `Check_DifferentExclusions_Fails`, `IncomparableBaseline_FailsComparability`; script: `IncomparableTarget` and a Debug/Release mismatch exit 85, `RunSettingsTamper` exit 85 | Foundation, met |
| T-P1-17 | Pass after correction | `Aggregate_TwoProductionModules_ExpectedResult`, `Aggregate_SameNameDifferentModuleId_Throws`, `Aggregate_SameModuleIdDifferentName_Throws`, `DuplicateModule_IsInputError`; one product module in this run (section 3). Corrected in section 13: checked totals and aggregation (`Total_SumExceedsInt64_ThrowsOverflowException`, `Aggregate_CountersSumExceedsInt64_ThrowsCoverageInputException`, `Evaluate_MaxValue_MaxValue_Null_Code_True_MeasurementFailsWithoutException`) and start-anchored exclusion patterns (`Aggregate_ExclusionPatternIsAnchoredAtStart_DoesNotExcludeLongerName`, `ExclusionPattern_DoesNotExcludeLongerAssemblyName_FailsMinimum`) | Foundation, met |
| T-P1-18 | Pass | `Evaluate_100_0_Vs_100_0_Code_ComparisonPassesExact100`, `Evaluate_85_15_Vs_85_15_DocumentationOnly_AllPass`, `Evaluate_84_16_Vs_85_15_DocumentationOnly_ComparisonFails`, `Evaluate_84_16_Vs_85_15_Code_ComparisonFailsDecrease`; documentation-only runs `ef9b9ac` and `83579f4`/`8a96f58` passed at equality | Foundation, met |
| T-P1-19 | Pass | Script level: `FailingTest` 50, `MissingCoverage` 80, `GateFailure` 90, `IncomparableTarget` 85, `SdkMismatch` 10 (with `commits.json`), `LockDrift` 30, `ConsumerLockDrift` 70 (P1.09); `FailingTest` 50 and `RunSettingsTamper` 85 on ARM64 (P1.10); run directories and diagnostics retained | Foundation, met |
| T-P1-20 | Pass | Class A: RID restore and build on Windows (NU1004 finding; projects stay RID-neutral by user decision). Class B: actual Debian 13 ARM64 runs (section 4) | Platform, PROVEN at P1 scope |
| T-P1-21 | Pass | `Deps_file_declares_the_library_as_a_restored_package_not_a_project_reference`; same-run package and hash check in this run | Foundation, met |
| T-P1-22 | Pass | `Package_exposes_only_the_expected_library_assembly`; allowed package entries; no consumer `ProjectReference` (the script fails with exit 70 otherwise) | Foundation, met |
| T-P1-23 | Pass | `Classify_ChangedFiles_ExpectedResult`, `Classify_NonMarkdownPathEvenInDocumentationFolder_ReturnsCode`, `Classify_EmptyList_ExpectedResult`; this run classifies the 18-file scope as code | Foundation, met |
| T-P1-24 | Pass after correction, with recorded limitations | Section 3: native counters only; module counted once; source checksums trace to the candidate; no PDB identity claimed. Corrected in section 13: exclusions no longer remove unrelated assemblies, and a resume package now hashes and verifies its changed-file list and raw reports | Foundation, met |

## 8. Acceptance checklist (PHASE_P1_PLAN.md section 8)

| Item | Outcome | Evidence |
|---|---|---|
| P0 completion and P1 implementation instruction documented | Met | P1.01 record |
| Minimal build, reproducible restore and pinned compatible dependencies with license evidence | Met | P1.02, P1.03; DEPENDENCIES.md; locked fresh-clone restore and Release build in this run (0 warnings) |
| Actual `.nupkg` and isolated package consumption verified | Met | Section 2; T-P1-21/22 |
| Meaningful TDD cycle, interfaces, constructor DI, MSTest/Moq/FluentAssertions and setup/cleanup | Met | P1.05, P1.06; T-P1-01 – T-P1-06 |
| Microsoft.CodeCoverage report with traceable block/metric mapping | Met | Section 3; P1.07 |
| At least 80% block coverage; bootstrap/documentation/100% rules demonstrated | Met | 54/56 = 96.43%; T-P1-09, T-P1-14, T-P1-18; genuine bootstrap `0a2dabe` |
| Error/comparability/provenance/PR-classification tests pass | Met | T-P1-11 – T-P1-17, T-P1-23, T-P1-24 |
| Local CI procedures/artifacts verified; truthful GitHub/ARM runtime status | Met | P1.09, this run; ARM64 PROVEN at P1 scope; **no hosted GitHub workflow exists, and none is claimed** |
| English output/comments and complete English documentation | Met | P1.11 comment-language scan and documentation checks; CONTRIBUTING.md |
| P1 acceptance record with versions, working revision, evidence and limitations | Met | This record |

## 9. Limitations carried forward

- No hosted CI: all pipeline evidence comes from local runs (Windows x64, NanoPi R76S).
- Release readiness, full hardware acceptance and the REQ-084 checks belong to P9.
- The package identity `WeatherParser.Parsing` 0.1.0-initial is a local test package; package ID, feed and publication are release decisions (P9).
- The committed consumer lock is stale by design (commit-dependent package hash). The CI script and CONTRIBUTING.md section 4 handle it.
- The RID-specific locked restore fails with NU1004; the projects stay RID-neutral by user decision (P1.10).
- REQUIREMENTS.md (REQ-004) and AGENTS.md still name SDK 10.0.303 as the historical starting point of 2026-10-14. These are binding texts and were left unchanged; the current toolchain is in DEPENDENCIES.md.
- The P1.03 and P1.08 record headers are stale; later evidence supersedes them (section 5). They were not edited, because historical records are append-only.

## 10. Decision

**ACCEPTED** (audit decision; P1 formally accepted by the user on 2026-10-19, see "P1 acceptance" below). Every mandatory P1 foundation criterion has current, traceable passing evidence from the committed candidate `6ac88e7` and its fresh-clone run against the genuine bootstrap seed `0a2dabe`, and every test ID T-P1-01 to T-P1-24 has a passing outcome. The platform-runtime status is PROVEN for the P1-scope checks executed on the Debian 13 ARM64 target, so no runtime deferral is needed. P2 may start on a separate instruction; see the [P2 handoff](P2_HANDOFF.md).

## 11. Documentation revision (plan step 8)

This record, the P2 handoff and the status updates were committed after the audit, as `bd3f257`. At that time the tested candidate was `654bf56` (now superseded by `463eae5`, section 12); the documentation revision `bd3f257` changes only Markdown files (`DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md`, `README.md` and the two new records).

`README.md` is the packed package readme, so the documentation revision got its own package check, using `pwsh ./scripts/ci-local.ps1 -Commit bd3f257 -TargetRef 654bf56 -RunSettings coverage.runsettings`, exit 0, run `artifacts/ci/bd3f257-20261019T210213Z`:
- **Tests:** WeatherParser.Parsing.Tests 15, WeatherParser.Coverage.Tests 91, WeatherParser.Coverage.Runner.Tests 54 and WeatherParser.Package.Tests 2, all passed. The consumer lock rewrite in the clone was exactly one line.
- **Package:** SHA-512 `yJBV7t+Cbvb+0HFpxgmOQSDKSpWRQXSl05bp8q57RsElgUGukRM6zyl1qWZpS9cL9EKxupHM46rLtt5WjPL09A==`, allowed entries only. The packed README.md and LICENSE equal the committed files apart from the CRLF line endings added at checkout.
- **Gate:** documentation-only, "Block coverage 54/56 (96.43%) is at or above baseline 54/56 (96.43%)". SHA-256 of `gate-result.json`: `36aa8f6f6aa5bd9f82b58caad8e2e31368b80b270f25f917b07e11fd944e2916`.

This section was added in a later commit that does not change `README.md`.

## 12. Review finding T-P1-06 and correction (2026-10-19)

An external review of `084b5fd` confirmed the run evidence: 162 passing tests in both Windows runs, all recorded artifact hashes, the five source checksums, a re-executed gate at 54/56 against 52/56, all 24 test-ID rows, 114 relative links, and the final package contents, README reference and embedded commit. It found one defect: T-P1-06 was marked passed although two test classes did not build their subjects in `[TestInitialize]`, contrary to REQ-081 and AGENTS.md "Tests". The P1.08 exception covers only the missing Red-step evidence, not this rule.

**Disposition (user decision, 2026-10-19):** fix the defects and gate the corrected candidate against the same deliberate target as the audit, the seed `0a2dabe`.

- **Correction (`463eae5`, generated by qwen, reviewed by Claude), a pure refactor.** Test names, attributes, arrange values and assertions are unchanged:
  - `ServiceCollectionExtensionsTests` (P1.06) creates its `ServiceCollection` in `[TestInitialize]`.
  - `CoverageGateRealReportTests` (P1.08) builds the gate with its real dependencies there.
  - `RunnerEndToEndTests` (P1.08) creates the output writers and the composed runner there.
- **Verification:** Release build with 0 warnings; WeatherParser.Parsing.Tests 15, WeatherParser.Coverage.Tests 91 and WeatherParser.Coverage.Runner.Tests 54, all passed, with the same counts as before.
- **Candidate run:** `463eae5` against `0a2dabe` (`artifacts/ci/463eae5-20261019T211011Z`), exit 0, gate passed at 54/56 against 52/56; superseded by `6ac88e7` (section 13).
- **Gate statement:** the test refactor changes no product code. Measured against its parent `654bf56` on its own, it would be a code change with equal coverage (54/56 both), which the gate rejects below 100% (Comparison). It was not evaluated that way. The candidate's complete change scope since the accepted bootstrap baseline was evaluated instead, and the refactor is part of that scope.

## 13. Second review findings and corrections (2026-10-19)

A second external review of `2c273c5` confirmed the earlier evidence: 162 tests in an isolated copy, consistent acceptance artifacts. It found three defects that the existing suite missed. All three were returned to their owners and fixed by qwen from Claude's briefs, test-first for the tooling.

1. **[P1.08] Counter overflow could pass the gate.** `BlockCounters.Total` added two `long` counters unchecked. With both at `long.MaxValue` the total wrapped to −2, and the runner reported a valid measurement, a passed minimum and exit 0.
   - **Fix:** `Total` uses checked arithmetic. Both readers reject counters whose total exceeds the range (input error). The aggregator sums with checked arithmetic and reports an overflow as an input error. The evaluator treats an out-of-range total as an invalid measurement or baseline, without throwing.
2. **[P1.08] Exclusion patterns matched unintended assemblies.** The wildcard matcher was not anchored at the start: `WeatherParser.Coverage*.dll` also excluded `OtherWeatherParser.CoverageDomain.dll`, turning an actual 50% into a reported 100%.
   - **Fix:** patterns now match the whole module name, case-insensitively, with `*` matching any sequence.
3. **[P1.09/P1.10] The resume package omitted gate inputs.** `resume.json` hashed the runner and tool manifest, but `-ResumeFrom` only checked that `changed-files.txt` existed, and the raw reports had no hashes; a replaced changed-file list was accepted.
   - **Fix:** the package records the SHA-256 of `changed-files.txt` and of every raw `.coverage` report (paths relative to the run). `-ResumeFrom` verifies them and rejects missing, extra or changed reports and packages without these hashes (exit 20).

**Red → Green (findings 1 and 2, commit `5435c63`).**
- **New tests and fixtures:** `BlockCountersTests` (2), three `ProvenanceAggregatorTests`, one test each in `RuleEvaluatorTests`, `CoverageReportReaderTests` and `BaselineMetadataReaderTests`, two `RunnerEndToEndTests`, and the fixtures `report-overflow.xml` and `report-unanchored-exclusion.xml`.
- **Red:** before the fix, exactly the eight defect-probing tests failed. The two characterization tests (`Total_SumWithinInt64_ReturnsSum`, `Aggregate_TestsSuffixPattern_StillExcludesTestAssembly`) and all existing tests passed.
- **Corrected test:** the first version of `Evaluate_MaxValue_MaxValue_Null_Code_True_MeasurementFailsWithoutException` expected Minimum and Baseline to pass, which would have preserved the defect. It was corrected to the existing zero-denominator contract: exactly one failed Measurement rule. Against the original evaluator the corrected test fails; against the fix it passes.
- **Green:** WeatherParser.Parsing.Tests 15, WeatherParser.Coverage.Tests 99, WeatherParser.Coverage.Runner.Tests 56; 0 warnings.

**Script verification (finding 3, commit `6ac88e7`, Windows).**

| Run | Exit | Result |
|---|---|---|
| Full two-pass run against `0a2dabe` | 0 | This record's candidate run (section 2) |
| `-CollectOnly` | 0 | `resume.json` records `changedFilesSha256` and both raw reports |
| `-ResumeFrom` on an unchanged copy | 0 | "Gate passed (resumed)" |
| `-ResumeFrom` after replacing `changed-files.txt` | 20 | "changed-files.txt hash mismatch" |
| `-ResumeFrom` after appending a byte to the current raw report | 20 | "Raw coverage report hash mismatch for current/coverage/raw/…" |
| `-ResumeFrom` with an extra `.coverage` file in the target pass | 20 | "Unexpected raw coverage report: target/coverage/raw/…/extra.coverage" |

**Gate statement.** All three corrections change tooling, tests and the CI script, not the product library. `WeatherParser.Coverage*.dll` is excluded from the product gate, so product coverage stays at 54/56. Measured against its parent on its own, each correction commit would be a code change with equal coverage, which the gate rejects below 100%. Following the user's disposition for section 12, the candidate's complete change scope since the accepted bootstrap baseline was evaluated instead: `6ac88e7` against `0a2dabe`, gate passed at 54/56 against 52/56 (section 2).

## P1 acceptance (2026-10-19)

The user formally accepted P1 on 2026-10-19, on the evidence of this record and after the corrections of sections 12 and 13.
- **Accepted foundation:** candidate `6ac88e783ce10fa6d676860ba831ee371abd9cdf`, two-pass run `artifacts/ci/6ac88e7-20261019T213031Z` against the bootstrap seed `0a2dabe`, gate passed at 54/56 against 52/56.
- **Test IDs:** all 24 have passing outcomes.
- **Platform:** ARM64 runtime PROVEN at P1 scope, with the platform note of section 4.
- **Not included:** release readiness, full hardware acceptance, the REQ-084 checks and a hosted GitHub CI run (all later phases or separate instructions).
- **Next phase:** P2, as described in the [P2 handoff](P2_HANDOFF.md). It starts only on a separate instruction.
