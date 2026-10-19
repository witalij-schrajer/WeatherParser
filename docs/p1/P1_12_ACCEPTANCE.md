# P1.12 – P1 Acceptance Record

**Audit decision (2026-10-19): ACCEPTED for the P1 package/test/coverage foundation. Platform runtime status: PROVEN for the P1-scope checks executed on the NanoPi R76S (Debian 13 ARM64). Formal acceptance by the user is pending.** Implements the [P1.12 plan](P1_12_ACCEPTANCE_HANDOFF_PLAN.md); the next phase is described in the [P2 handoff](P2_HANDOFF.md).

This is acceptance of the P1 foundation only, not release readiness (P9), not full hardware acceptance (P9) and not a hosted GitHub CI result.

## 1. Review identity

| Field | Value |
|---|---|
| Review date | 2026-10-19 |
| Reviewer | Claude (orchestrating LLM) on the user's instruction; documentation drafts by the local qwen3-coder:30b model, reviewed by Claude; formal acceptance: the user |
| Candidate | `654bf56ce67095ed686c7a966c6e9ed69b2aa5d6` (`features/develop/initial`, identical to `origin/features/develop/initial` at the review; P1.11 accepted state) |
| Target | `0a2dabeaa4b4baa836b87d7a4aa1a456e3dcb552`, the genuine initial bootstrap seed `C` (P1.09), an ancestor of the candidate (30 commits) |
| Script revision | `scripts/ci-local.ps1` at the candidate (last changed in `660dc7e`, P1.10) |
| Working-tree exclusions | None: the working tree was clean, and the run tests only the committed candidate from a fresh clone |
| Command | `pwsh ./scripts/ci-local.ps1 -Commit 654bf56 -TargetRef 0a2dabe -RunSettings coverage.runsettings` (two-pass, no `-Bootstrap`) |
| Run ID | `artifacts/ci/654bf56-20261019T205341Z/` (local, gitignored, retained) |

**Initial bootstrap (plan section 4).** No accepted integrated baseline existed before P1.09, so the first fault-free full pipeline run on the committed seed `0a2dabe` is the genuine bootstrap: `-Commit 0a2dabe -TargetRef 0a2dabe -Bootstrap`, exit 0, 52/56 blocks. Its seed artifacts (`current/bootstrap-baseline.json`, collection settings, SHA-256 provenance) are recorded in the [P1.09 record](P1_09_LOCAL_CI.md#genuine-initial-bootstrap-seed-2026-10-19). The candidate is a later commit, so it is evaluated by the normal two-pass comparison against `0a2dabe`, which is remeasured in the same run with identical settings. Bootstrap was not used again.

## 2. Candidate run results

- **Exit:** 0. Every stage passed: SDK check, locked restore, Release build, the four test projects, pack and package-content check, package consumer, conversion, target pass and gate.
- **Toolchain:** .NET SDK 10.0.402 and .NET runtime 10.0.13 (host, `win-x64`), recorded separately. `Microsoft.CodeCoverage` 18.11.0; `dotnet-coverage` 18.11.0 (manifest and tool); collector "Code Coverage"; configuration Release; RID and converter RID `win-x64`.
- **Collection settings:** run-settings SHA-256 `dc6f662e4f6faaa853c33ed5497f182a31527fd126961411a604b683ffcf05d2` (`coverage.runsettings`, static managed instrumentation), exclusions `*.Tests.dll`, `WeatherParser.Coverage*.dll`. Both passes are identical except `commit`.
- **Tests (current pass):** WeatherParser.Parsing.Tests 15, WeatherParser.Coverage.Tests 91, WeatherParser.Coverage.Runner.Tests 54, WeatherParser.Package.Tests 2; all passed, none skipped. The target pass ran WeatherParser.Parsing.Tests: 8 passed. Build warnings: 0 in both passes.
- **Package:** `WeatherParser.Parsing.0.1.0-initial.nupkg`, SHA-512 `+21dY5USLmQ0i6FFlV8ip5Sp/w+Gyk7ZqWK1WsaEXEtU5lC9tNmgqEHuqEw592fWkorxSGTn+Wmv7A9DP4TKzg==`. Entries: `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE`, the nuspec, `[Content_Types].xml`, `_rels/.rels` and `nuget.psmdcp` only.
- **Consumer:** the only lock change in the disposable clone was the documented one-line `contentHash` rewrite. The locked restore passed, the restored hash was checked against the packed hash (a mismatch would exit 70), and the consumer tests passed.
- **Native block counters:** current `WeatherParser.Parsing.dll` 54/56 (96.43%), target 52/56 (92.86%). One raw `.coverage` report per pass.
- **Change scope:** 18 files since the target, including `coverage.runsettings`, `scripts/ci-local.ps1`, `scripts/prepare-debian-arm64.sh` and `tests/WeatherParser.Parsing.Tests/EndpointUriValidatorTests.cs`. Classified as a **code** change, with no documentation bypass.
- **Gate verdicts:** Comparability passed ("Baseline is comparable."); Measurement passed ("Block counters 54/56 are valid."); Minimum passed (54/56 ≥ 80%); Baseline passed ("Baseline block counters 52/56 are valid."); **Comparison passed** ("Block coverage 54/56 (96.43%) exceeds baseline 52/56 (92.86%).").
- **SHA-256 of the run artifacts:** `.nupkg` `41fa100c487e125bd060eacbcfe187e49e51f6a9bae333b528ba166144220c4c`; `gate-result.json` `8c76191964f208952bc97603b185a2b790783864408467310a3217cf08fae269`; `commits.json` `22d000b282c40cb26134509eb2d708552749b53457dc8980a53a5c6f7681f01b`; `current/collection-settings.json` `87fe8528d55db9ad675cdc504d49929cd4214aabe1732e636994fc029b1dce1f`; `target/collection-settings.json` `dd7d1c1fa807248f222b4ddeb4682371d98fb29aca2d7d0545df66ef3f23b9ac`; `current/coverage/coverage.xml` `bee3214d79a4ea779675f695a3c2b0802404ca68f234beba0cc1f09f40a5e4c5`.

## 3. Coverage provenance (T-P1-24)

- **Module:** the current report contains one `WeatherParser.Parsing.dll` module (id `93E3120F6D9BDF49BB5B0DC5623A4D2CF2AE55D1`), counted once, and the test module `WeatherParser.Parsing.Tests.dll`, which the exclusions remove. The library is measured from source-reference tests only. The package consumer is not instrumented, so the package assembly is not double-counted.
- **Source identity:** the report lists the 5 library source files that contain executable code, each with a SHA-256 checksum. All 5 match the candidate's committed blobs as checked out with CRLF line endings (`core.autocrlf=true`): `EndpointUriValidator.cs`, `ParserEndpointDefinition.cs`, `ParserEndpointValidationException.cs`, `ParserEndpointValidator.cs`, `ServiceCollectionExtensions.cs`. The two interface files contain no executable code and are absent, as expected.
- **Limitations:** the native report carries no assembly version, PDB GUID or package identity. The module `id` is not claimed to be a PDB identity. Build and package identity are established independently: the run builds, packs and consumes the package from the same clone and commit, and records the package hash. The package also embeds the commit ID (nuspec `repository commit`, informational version `+<SHA>`; P1.09 finding 5).
- **Separate gates:** checker and runner self-coverage are not part of the product gate (`WeatherParser.Coverage*.dll` is excluded). Their behaviour is covered by the 91 + 54 meta-tests.

## 4. Platform evidence (P1.10)

**Status: PROVEN for the checks executed on the target. No deferral was needed.**
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
- **Changes after the P1.11 revision:** only the acceptance commit `654bf56`, which is the candidate itself. Its README status-line change was re-checked by a CI run (documentation-only, exit 0) before this audit.
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
| T-P1-06 | Pass | Subjects built in `[TestInitialize]`; strict mocks verified with `VerifyAll()` in `[TestCleanup]` (`ParserEndpointValidatorTests` and the coverage test classes); tests without mocks need no cleanup | Foundation, met |
| T-P1-07 | Pass | No HTTP, MQTT, socket or device access in `src`, `tests` or `tools` (source search); URIs are `example.org` test data only; the CI clone restores only from nuget.org and the local feed | Foundation, met |
| T-P1-08 | Pass | Fresh-clone locked restore in this run; the only lock change is the reviewed consumer `contentHash` rewrite; drift detection: P1.09 `LockDrift` exit 30, `ConsumerLockDrift` exit 70 | Foundation, met |
| T-P1-09 | Pass | `Evaluate_80_20_Null_Code_True_AllRulesPass`, `Evaluate_79_21_Null_Code_True_MinimumFailsWhileBaselinePasses`; candidate 54/56 ≥ 80% | Foundation, met |
| T-P1-10 | Pass | `Evaluate_85_15_Vs_85_15_Code_ComparisonFailsEqualityBelow100`, `Evaluate_86_14_Vs_85_15_Code_ComparisonPasses`, `EqualBaseline_CodeChange_FailsComparison`; this candidate strictly improves (54/56 > 52/56); P1.09's two-pass exit 90 for an equal code change | Foundation, met |
| T-P1-11 | Pass | `ReadAsync_CoberturaFormat_ThrowsCoverageInputException`, `ReadAsync_ModuleWithoutBlockCounters_ThrowsCoverageInputException`, `NonNativeOrIncompleteReport_IsInputError` | Foundation, met |
| T-P1-12 | Pass | `Evaluate_99999_1_Vs_100_0_Code_ComparisonFailsRounded100NotExact`, `ReadAsync_Rounded100Report_ReturnsExactCounters`, `Rounded100_AgainstExact100Baseline_FailsComparison` | Foundation, met |
| T-P1-13 | Pass | `ReadAsync_MissingReport_…`, `ReadAsync_MalformedXml_…`, `MalformedOrMissingReport_IsInputError`, `ZeroDenominator_FailsMeasurement`; script level: P1.09 `MissingCoverage` exit 80 | Foundation, met |
| T-P1-14 | Pass | `Evaluate_85_15_Null_Code_False_BaselineFailsMissingAfterBootstrap`, `NoBaselineNoBootstrap_FailsBaselineRule`, `SuppliedBaselineMissing_IsInputError`, `PassingReport_P109BootstrapInvocation_Passes`; genuine bootstrap on `0a2dabe` (section 1) | Foundation, met |
| T-P1-15 | Pass | `Check_StaleCommit_Fails`, `ReadAsync_StaleCommitFixture_ReadsCommitUnchanged`; script: external baseline commit mismatch exits 20 (P1.09 review fixes) | Foundation, met |
| T-P1-16 | Pass | `Check_DifferentToolName_Fails`, `Check_DifferentToolVersion_Fails`, `Check_DifferentFormat_Fails`, `Check_DifferentExclusions_Fails`, `IncomparableBaseline_FailsComparability`; script: `IncomparableTarget` and a Debug/Release mismatch exit 85, `RunSettingsTamper` exit 85 | Foundation, met |
| T-P1-17 | Pass | `Aggregate_TwoProductionModules_ExpectedResult`, `Aggregate_SameNameDifferentModuleId_Throws`, `Aggregate_SameModuleIdDifferentName_Throws`, `DuplicateModule_IsInputError`; one product module in this run (section 3) | Foundation, met |
| T-P1-18 | Pass | `Evaluate_100_0_Vs_100_0_Code_ComparisonPassesExact100`, `Evaluate_85_15_Vs_85_15_DocumentationOnly_AllPass`, `Evaluate_84_16_Vs_85_15_DocumentationOnly_ComparisonFails`, `Evaluate_84_16_Vs_85_15_Code_ComparisonFailsDecrease`; documentation-only runs `ef9b9ac` and `83579f4`/`8a96f58` passed at equality | Foundation, met |
| T-P1-19 | Pass | Script level: `FailingTest` 50, `MissingCoverage` 80, `GateFailure` 90, `IncomparableTarget` 85, `SdkMismatch` 10 (with `commits.json`), `LockDrift` 30, `ConsumerLockDrift` 70 (P1.09); `FailingTest` 50 and `RunSettingsTamper` 85 on ARM64 (P1.10); run directories and diagnostics retained | Foundation, met |
| T-P1-20 | Pass | Class A: RID restore and build on Windows (NU1004 finding; projects stay RID-neutral by user decision). Class B: actual Debian 13 ARM64 runs (section 4) | Platform, PROVEN at P1 scope |
| T-P1-21 | Pass | `Deps_file_declares_the_library_as_a_restored_package_not_a_project_reference`; same-run package and hash check in this run | Foundation, met |
| T-P1-22 | Pass | `Package_exposes_only_the_expected_library_assembly`; allowed package entries; no consumer `ProjectReference` (the script fails with exit 70 otherwise) | Foundation, met |
| T-P1-23 | Pass | `Classify_ChangedFiles_ExpectedResult`, `Classify_NonMarkdownPathEvenInDocumentationFolder_ReturnsCode`, `Classify_EmptyList_ExpectedResult`; this run classifies the 18-file scope as code | Foundation, met |
| T-P1-24 | Pass with recorded limitations | Section 3: native counters only; module counted once; source checksums trace to the candidate; no PDB identity claimed | Foundation, met |

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

**ACCEPTED** (audit decision; formal acceptance by the user pending). Every mandatory P1 foundation criterion has current, traceable passing evidence from the committed candidate `654bf56` and its fresh-clone run against the genuine bootstrap seed `0a2dabe`, and every test ID T-P1-01 to T-P1-24 has a passing outcome. The platform-runtime status is PROVEN for the P1-scope checks executed on the Debian 13 ARM64 target, so no runtime deferral is needed. P2 may start on a separate instruction; see the [P2 handoff](P2_HANDOFF.md).
