# WeatherParser – P1 detailed plan

As of 2026-10-19. **P0 is complete. P1.01-P1.12 are complete and P1 is accepted by the user (2026-10-19); the Visual Studio build limitation is resolved; clean-checkout reproducibility is verified on Windows x64 (P1.09), on Debian 13 ARM64 by P1.10 (accepted)**. P1.08's evaluator, runner and pinned tooling are accepted; the user explicitly accepted its missing original Red-step evidence. Local CI (P1.09) is accepted. P1.10 platform foundations are verified on the NanoPi R76S and accepted; P1.11 developer instructions are verified and accepted; P1 is accepted ([acceptance record](docs/p1/P1_12_ACCEPTANCE.md#p1-acceptance-2026-10-19)); P2 starts only on a separate instruction ([P2 handoff](docs/p1/P2_HANDOFF.md)). Update 2026-10-20: P2 was accepted by the user and is complete after the merge of pull request #3 into `develop` on 2026-10-20 (merge commit `74bf625`, [P2 acceptance record](docs/p2/P2_12_ACCEPTANCE.md)); this P1 record is unchanged. Update 2026-10-24: P4 was accepted by the user on candidate `b6bc47f` ([P4 acceptance record](docs/p4/P4_12_ACCEPTANCE.md)) and is complete: pull request #5 merged into `develop` on 2026-10-24 (merge commit `95bcb1e`); P5 started on 2026-10-24 on the user's instruction ([P5 handoff](docs/p4/P5_HANDOFF.md)), P5.01 complete (READY) and the P5 observation contract approved on 2026-10-24 ([contract](docs/p5/P5_OBSERVATION_CONTRACT.md)); this P1 record is unchanged. Update 2026-10-25: P5 was accepted by the user on candidate `fd461c8` ([P5 acceptance record](docs/p5/P5_11_ACCEPTANCE.md)); it is complete: pull request #7 merged into `develop` on 2026-10-25 (merge commit `29fd553`); P6 starts only on a separate instruction; P6 (OsthessenWetter observations) started on 2026-10-25 on the user's instruction on branch `features/develop/P6` from `develop` `8d8079a` ([P6 handoff](docs/p5/P6_HANDOFF.md), [P6 plan](PHASE_P6_PLAN.md)); P6.01 (foundation check) complete with outcome READY ([record](docs/p6/P6_01_FOUNDATION_CHECK.md)); P6.02: OsthessenWetter contract OW-01 to OW-16 approved by the user on 2026-10-25 ([contract](docs/p6/P6_OSTHESSEN_CONTRACT.md)) and the P6 plans reconciled with it; P6.03 to P6.09 implemented test-first on 2026-10-25 and 2026-10-26 ([P6 verification record](docs/p6/P6_VERIFICATION.md)): the OsthessenWetter parser is available in the library; P6.10 (integrated verification) READY on candidate `4373399` on Windows, hosted CI and the NanoPi R76S, with one authorized live check, and after the PR #9 review fixes again on candidate `12e7205` ([P6.10 record](docs/p6/P6_10_INTEGRATED_VERIFICATION.md)); P6.11: evidence audit ACCEPTED ([P6.11 acceptance record](docs/p6/P6_11_ACCEPTANCE.md), [P7 handoff](docs/p6/P7_HANDOFF.md)), P6 accepted by the user on 2026-10-26 on candidate `12e7205` (the library; production use and redistribution of OsthessenWetter values stay open until the source conditions are clarified, OW-14); it is complete: pull request #9 merged into `develop` on 2026-10-26 (merge commit `de9d4ba`); P7 (MOSMIX forecasts) started on 2026-10-26 on the user's instruction on branch `features/develop/P7` from `develop` `8009f1c` ([P7 handoff](docs/p6/P7_HANDOFF.md), [P7 plan](PHASE_P7_PLAN.md)); P7.01 (foundation check) complete with outcome READY ([record](docs/p7/P7_01_FOUNDATION_CHECK.md)); P7.02: MOSMIX forecast contract MF-01 to MF-18 approved by the user on 2026-10-26 ([contract](docs/p7/P7_FORECAST_CONTRACT.md)) and the P7 plans reconciled with it; P7.03 to P7.10 implemented on 2026-10-26 to 2026-10-28 ([P7 verification record](docs/p7/P7_VERIFICATION.md): P7.03 to P7.08 test-first, with the P7.06 deviation of planned tests added after Green, recorded and covered by the phase acceptance, P7.09 and P7.10 as characterization tests over existing behavior): the MOSMIX_L and MOSMIX_S forecast parsers are available in the library; P7.11 (integrated verification) READY on candidate `a36d15a` on Windows, hosted CI and the NanoPi R76S, with one authorized live parse per product ([P7.11 record](docs/p7/P7_11_INTEGRATED_VERIFICATION.md)); after the PR #11 review fixes the candidate is `2f1115b`, with P7.11 repeated and READY on Windows, hosted CI and the NanoPi R76S (authorized rerun); P7.12: evidence audit ACCEPTED and P7 accepted by the user on 2026-10-28 on candidate `2f1115b` ([acceptance record](docs/p7/P7_12_ACCEPTANCE.md), [P8 handoff](docs/p7/P8_HANDOFF.md)); it is complete: pull request #11 merged into `develop` on 2026-10-28 (merge commit `20b2ef0`); P8 (MQTT serializer and complete library integration) started on 2026-10-28 on the user's instruction on branch `features/develop/P8` from `develop` `8ed51c7` ([P8 plan](PHASE_P8_PLAN.md)); P8.01 (foundation check) complete with outcome READY ([record](docs/p8/P8_01_FOUNDATION_CHECK.md)); P8.02: MQTT message contract MQ-01 to MQ-15 approved by the user on 2026-10-28 ([contract](docs/p8/P8_MQTT_CONTRACT.md)) and the P8 plans reconciled with it; P8.03 to P8.08 implemented on 2026-10-28 and 2026-10-29 ([P8 verification record](docs/p8/P8_VERIFICATION.md)): the generic MQTT message serializer is available in the library and the complete library is consumed through the package, also concurrently; P8.09 (integrated verification) READY on candidate `290a0eb` on Windows, hosted CI and the NanoPi R76S, and after the PR #14 review fixes again on candidate `ce166eb` (authorized rerun; [P8.09 record](docs/p8/P8_09_INTEGRATED_VERIFICATION.md)); P8.10: evidence audit ACCEPTED, PR #14 review to 0 findings, and P8 and the complete library accepted by the user on 2026-10-29 on candidate `ce166eb` ([P8.10 acceptance record](docs/p8/P8_10_ACCEPTANCE.md), [P3 handoff](docs/p8/P3_HANDOFF.md)); it is complete: pull request #14 merged into `develop` on 2026-10-29 (merge commit `21b7210`); update 2026-10-29: P3 (example CLI) started on 2026-10-29 on the user's instruction ("P3 Implmentation") on branch `features/develop/P3` from `develop` `e623a83` (the merge of the P3 planning pull request #15; [P3 plan](PHASE_P3_PLAN.md), [P3 handoff](docs/p8/P3_HANDOFF.md)); P3.01 (foundation check) complete with outcome READY ([record](docs/p3/P3_01_FOUNDATION_CHECK.md)); P3.02: CLI contract CL-01 to CL-22 approved by the user on 2026-10-29 ([contract](docs/p3/P3_CLI_CONTRACT.md)) and the P3 plans reconciled with it; P3.03 to P3.10 implemented on 2026-10-29 and 2026-10-30 ([P3 verification record](docs/p3/P3_VERIFICATION.md)): the example CLI with its five commands, JSON/XML output, the MQTT broker transport (MQTTnet 5.2.0.1603) and the cancellation and deadline behavior, verified offline with local fake sources on Windows and in the hosted CI jobs (the broker integration tests passed against a local Mosquitto without TLS and credentials on Windows only) and documented in the README section "Example CLI"; P3.11 (integrated verification) READY on candidate `5696357` on Windows, hosted CI and the NanoPi R76S with the authorized broker run and live checks ([P3.11 record](docs/p3/P3_11_INTEGRATED_VERIFICATION.md)); P3.12: the user's pull request review fixed test-first (the CL-06 owner check of the credential file, an explicit empty `--StationID`), the automatic Codex review of pull request #16 to 0 findings, and P3 accepted by the user on 2026-10-31 on candidate `ec915fc` ("Accept, no rerun": the NanoPi, broker and live checks of P3.11 stay those of `5696357`; [P3.12 acceptance record](docs/p3/P3_12_ACCEPTANCE.md), [P9 handoff](docs/p3/P9_HANDOFF.md)); P3 is complete: the user merged pull request #16 into `develop` on 2026-10-31 (merge commit `2ba5502`); P9 (Debian/ARM64 release acceptance, operation and release preparation) started on 2026-10-31 on the user's instruction ("P9 Execution") on branch `features/develop/P9` from `develop` `ed7fc7a` (the merge of the P9 planning pull request #17; [P9 plan](PHASE_P9_PLAN.md), [P9 handoff](docs/p3/P9_HANDOFF.md)); P9.01 (foundation check) in progress; this P1 record is unchanged.

Basis: [requirements](REQUIREMENTS.md), [development plan](DEVELOPMENT_PLAN.md) and [repository rules](AGENTS.md).

## 1. Goal and scope

P1 establishes reproducible build, package, test and CI foundations for the independently usable NuGet library. The product contains parsers, output entities, a generic MQTT serializer and DI integration; implement these domain components in subsequent phases. An isolated package consumer proves actual NuGet consumption in P1.

Develop the final example CLI, including arguments, configuration, exit codes, deployment model and execution flow, in P3. P1 implements no CLI dispatcher, weather adapters, export entities, XSD generation or MQTT transport. Installation or configuration changes on R76S are outside this phase.

## 2. Approved foundations and remaining tasks

| Topic | Binding decision | P1 task |
|---|---|---|
| Platform | .NET ≥10, `net10.0`, Debian ≥13, at least ARM64 | Verify compatibility matrix and package consumption |
| Development environment | Visual Studio 2026; locally verified 18.8.3 with SDK 10.0.303 | Check servicing; pin the starting revision reproducibly and document any necessary change |
| Dependencies | Latest stable versions on adoption, compatible licenses | Check official sources, resolve and pin versions; disclose conflicts |
| Tests | MSTest, Moq, FluentAssertions, TDD | Enforce `[TestInitialize]`, `[TestCleanup]` and `VerifyAll()` |
| Coverage | Microsoft.CodeCoverage report including code blocks | Prove actual report format, counters and automated evaluation |
| Language | English program output, comments and documentation | Keep the canonical English documents and developer procedures aligned |
| Architecture | Constructor DI, interface per Doing class, configurable endpoints/topics | Verify minimally required public package contracts and DI boundaries |
| Publication | NuGet primary product; CLI usage example | Document local test package ID/version; public identity/release procedures remain release tasks |

P0 is no longer an open gate. Resolving versions and verifying coverage reports are P1 work, not new P0 domain questions. GitHub activation/publication requires a separate instruction; local verification is independent. Implementing P1 still requires an explicit implementation instruction.

## 3. Proposed structure

| Location | Responsibility |
|---|---|
| `src/WeatherParser.Parsing/` | Packable `net10.0` library without CLI or broker-transport dependencies |
| `tests/WeatherParser.Parsing.Tests/` | Behavior tests with source-project references |
| `tests/WeatherParser.Package.Tests/` | Isolated consumer of the actual `.nupkg` through `PackageReference` |
| `examples/WeatherParser.Cli/` | Reserved for P3; no CLI scaffold required in P1 |
| Repository root | Solution, `global.json`, build/package configuration, locks, `.editorconfig`, `.gitignore` |
| Future CI location | Restore/build/pack/test/coverage checks and artifacts |

Names are provisional. Create projects only when needed. No empty infrastructure project or classes merely to achieve coverage figures. Paths do not guarantee public package-name availability. Example/package consumers use the actual package; source references are allowed only in library unit tests.

## 4. Work packages

| Package | Work | Result / dependency |
|---|---|---|
| P1.01 | Check working state/P0 completion and confirm P1 scope | Completed on 2026-10-15; [entry audit](docs/p1/P1_01_BASELINE.md) |
| P1.02 | Verify SDK/packages, licenses and compatibility | Review complete; SDK update open; [report](docs/p1/P1_02_TOOLING_REVIEW.md) |
| P1.03 | Prepare minimal solution, library and test projects | Complete (SDK 10.0.402, locked restore, CLI build, IDE build); [record](docs/p1/P1_03_BUILD_SETUP.md); clean-checkout reproducibility verified on Windows x64 by accepted [P1.09](docs/p1/P1_09_LOCAL_CI.md#p109-acceptance-2026-10-19) |
| P1.04 | Establish local NuGet pack and isolated consumer | Complete; [record](docs/p1/P1_04_PACKAGE_CONSUMER.md) |
| P1.05 | Establish MSTest/Moq/FluentAssertions and TDD evidence | Complete (parser-endpoint validation, 6 passing tests); [record](docs/p1/P1_05_TDD_BASELINE.md) |
| P1.06 | Test-drive minimally needed package/DI contracts | Complete (`AddWeatherParsing` entry point, real DI resolution, missing registration detected, 8 passing tests); [record](docs/p1/P1_06_DI_CONTRACTS.md) |
| P1.07 | Collect Microsoft.CodeCoverage and verify report contract | Complete (raw report, block breakdown and proven per-assembly counter mapping); [record](docs/p1/P1_07_COVERAGE_CONTRACT.md) |
| P1.08 | Test-drive coverage evaluation/exceptions | Complete and accepted by the user (2026-10-19), including the documented original Red-step deviation; [acceptance record](docs/p1/P1_08_COVERAGE_EVALUATION.md#p108-acceptance-2026-10-19); [closure plan](docs/p1/P1_08B_COVERAGE_GATE_COMPLETION_PLAN.md) |
| P1.09 | Prepare local CI job sequence/artifacts | Implemented and verified on Windows x64 (2026-10-19): fresh-clone pipeline, seven fault injections, genuine bootstrap seed on `0a2dabe` (52/56 blocks, Release); passing two-pass run (documentation-only `ef9b9ac` against `0a2dabe`); `-BaselinePath` settings validation after review; **accepted by the user 2026-10-19** ([acceptance](docs/p1/P1_09_LOCAL_CI.md#p109-acceptance-2026-10-19)); [plan](docs/p1/P1_09_LOCAL_CI_PLAN.md); [record](docs/p1/P1_09_LOCAL_CI.md) |
| P1.10 | Verify Debian/ARM64 package foundations | Implemented and verified (2026-10-19) on the NanoPi R76S, Debian 13 ARM64: full CI run on the target, package consumer, conversion and gate on ARM64, gate passed (54/56 against 52/56); Class A NU1004 finding, projects stay RID-neutral; **accepted by the user 2026-10-19** ([acceptance](docs/p1/P1_10_DEBIAN_ARM64.md#p110-acceptance-2026-10-19)); [plan](docs/p1/P1_10_DEBIAN_ARM64_PLAN.md); [record](docs/p1/P1_10_DEBIAN_ARM64.md); [compatibility](docs/COMPATIBILITY.md) |
| P1.11 | Complete English developer instructions/evidence | Implemented and verified (2026-10-19): `CONTRIBUTING.md` holds every procedure with platform and status (decision D), README reduced, consistency table in AGENTS.md, Windows walkthrough and documentation checks passed; **accepted by the user 2026-10-19** ([acceptance](docs/p1/P1_11_DEVELOPER_INSTRUCTIONS.md#p111-acceptance-2026-10-19)); [plan](docs/p1/P1_11_DEVELOPER_INSTRUCTIONS_PLAN.md); [record](docs/p1/P1_11_DEVELOPER_INSTRUCTIONS.md) |
| P1.12 | Accept phase and document P2 handoff | Complete (2026-10-19): audit decision ACCEPTED, **P1 accepted by the user 2026-10-19** ([acceptance](docs/p1/P1_12_ACCEPTANCE.md#p1-acceptance-2026-10-19)); candidate `6ac88e7` against the bootstrap seed `0a2dabe`, gate passed (54/56 against 52/56), all 24 test IDs passed (T-P1-06, T-P1-13, T-P1-17 and T-P1-24 after review corrections), ARM64 runtime PROVEN at P1 scope; [plan](docs/p1/P1_12_ACCEPTANCE_HANDOFF_PLAN.md); [acceptance record](docs/p1/P1_12_ACCEPTANCE.md); [P2 handoff](docs/p1/P2_HANDOFF.md) |

### Test-driven starting point

Select genuinely needed library behavior first, such as validating and unambiguously mapping consumer-supplied parser endpoints in DI integration. Demonstrate the expected contract failure, implement minimally and refactor under green tests. Create only necessary services/interfaces. Never register unimplemented parsers as functional or simulate successful parsing. Restore failures and existence-only checks do not prove TDD.

Construct subjects/shared mocks in `[TestInitialize]`; verify all mocks through `VerifyAll()` in `[TestCleanup]`. Real DI resolution and actual package consumption complement mocks. Keep tests offline. Serializer/time/rounding/XSD contracts follow in P2 and source adapters in P4–P7.

## 5. Coverage contract

The native block counters from the Microsoft.CodeCoverage report are the sole coverage metric, as decided on 2026-10-19, replacing the former line and branch requirements. Verify the supported format and MSTest integration for the selected stable tool revision. Line-rate, branch-rate, or any other Cobertura-derived values must never be used as or substituted for the block metric; missing or unreadable block counters will result in failure. Never fabricate values.

| Case | Block-coverage check |
|---|---|
| Initial implementation without measurable target-branch baseline | At least 80%; no target comparison |
| Regular code PR, baseline below 100% | At least 80% and strictly above baseline |
| Exact 100% baseline | PR must reach exact 100%; equality allowed |
| Documentation-only PR | At least 80%; equality allowed, decreases forbidden |
| Unexpectedly missing/later broken baseline | Fail; no automatic bootstrap exemption |
| Missing/broken PR report or zero denominator | Fail; no fabricated coverage |

Compare unrounded counter ratios, not display percentages. Identify documentation-only PRs from the entire diff: changes to source, projects, packages, build/test/coverage/CI configuration or exclusions are not documentation-only changes. A filename alone does not grant a bypass.

Record production scope, exclusions, tool versions, report format and PR/target commit IDs. Do not count test code as production. Do not double-count the same library assembly from source/package tests; verify version/PDB/source provenance. Aggregate counters rather than averaging project percentages. Behavior/regression-test the checker itself; never exclude domain logic for metrics.

## 6. Planned tests

These IDs are planning references, not existing tests.

| Test ID | Subject | Expected result |
|---|---|---|
| T-P1-01 | Needed first library behavior | Recorded Red→Green→Refactor cycle |
| T-P1-02 | Invalid endpoint | Defined English error, no retrieval |
| T-P1-03 | Distinct parser endpoints | No cross-assignment/global URI replacement |
| T-P1-04 | Interfaces/constructor DI | No hidden service dependencies |
| T-P1-05 | Real DI resolution | Needed registration works; missing registration detected |
| T-P1-06 | Setup/cleanup | Subject setup and VerifyAll policy enforced |
| T-P1-07 | Offline execution | No production HTTP/MQTT/device access |
| T-P1-08 | Restore/versions | Reproducible clean checkout; uncontrolled changes detected |
| T-P1-09 | Minimum | 80% block coverage passes; lower values fail |
| T-P1-10 | Regular code PR | Equality below 100% fails; actual improvement passes |
| T-P1-11 | Metric identity | Line-rate, branch-rate or other Cobertura-derived values are never accepted as or substituted for native block counters; missing native block counters fail |
| T-P1-12 | Precision | Rounded 100% is not treated as exact 100% |
| T-P1-13 | Broken/missing report | Failure rather than green result |
| T-P1-14 | Bootstrap | Initially absent baseline: ≥80%, no comparison; later missing baseline fails |
| T-P1-15 | Commit identity | Stale/wrong baseline detected |
| T-P1-16 | Comparability | Different tools/exclusions/counter semantics detected |
| T-P1-17 | Aggregation | Aggregate counts, no duplicate package assembly |
| T-P1-18 | Exceptions | Exact 100%→100% and documentation equality pass; decreases fail |
| T-P1-19 | CI failure propagation | Test/coverage failures fail job; diagnostics preserved |
| T-P1-20 | ARM64 foundation | Restore/build evidence separate from actual runtime test |
| T-P1-21 | Actual NuGet artifact | Isolated consumer uses built package revision |
| T-P1-22 | Package boundary | No CLI/transport in package; no source reference in package test |
| T-P1-23 | Documentation classification | Code/build/package/CI/coverage changes receive no docs bypass |
| T-P1-24 | Report/package provenance | Counters, blocks, PDB and version traceable; block metric taken only from native block counters |

## 7. CI sequence and deliverables

1. Verify SDK/packages, restore and build.
2. Pack locally, restore/build isolated consumer.
3. Run offline unit/DI/package tests with machine-readable results.
4. Collect Microsoft.CodeCoverage; provide raw report and readable block/code mapping.
5. Check PR category/baseline availability and apply the appropriate coverage rule.
6. Provide `.nupkg`, tool/license inventory, test/coverage reports, commit mapping and diagnostics as artifacts.

Public PR execution receives no production secrets or privileged publishing rights. No automatic NuGet/GitHub push. A locally verified pipeline is not a passed GitHub run; actual activation requires a separate instruction. Without an ARM64 target, report only available build/consumer evidence; full hardware acceptance remains P9.

## 8. Acceptance and handoff

- [x] P0 completion and P1 implementation instruction documented (audit evidence: [acceptance record](docs/p1/P1_12_ACCEPTANCE.md#8-acceptance-checklist-phase_p1_planmd-section-8)).
- [x] Minimal build, reproducible restore and pinned compatible dependencies with license evidence (audit evidence: [acceptance record](docs/p1/P1_12_ACCEPTANCE.md#8-acceptance-checklist-phase_p1_planmd-section-8)).
- [x] Actual `.nupkg` and isolated package consumption verified (audit evidence: [acceptance record](docs/p1/P1_12_ACCEPTANCE.md#8-acceptance-checklist-phase_p1_planmd-section-8)).
- [x] Meaningful TDD cycle, interfaces, constructor DI, MSTest/Moq/FluentAssertions and setup/cleanup demonstrated (audit evidence: [acceptance record](docs/p1/P1_12_ACCEPTANCE.md#8-acceptance-checklist-phase_p1_planmd-section-8)).
- [x] Microsoft.CodeCoverage report with traceable block/metric mapping available (audit evidence: [acceptance record](docs/p1/P1_12_ACCEPTANCE.md#8-acceptance-checklist-phase_p1_planmd-section-8)).
- [x] At least 80% for block coverage; bootstrap/documentation/100% rules demonstrated (audit evidence: [acceptance record](docs/p1/P1_12_ACCEPTANCE.md#8-acceptance-checklist-phase_p1_planmd-section-8)).
- [x] Error/comparability/provenance/PR-classification tests pass (audit evidence: [acceptance record](docs/p1/P1_12_ACCEPTANCE.md#8-acceptance-checklist-phase_p1_planmd-section-8)).
- [x] Local CI procedures/artifacts verified; truthful GitHub/ARM runtime status recorded (audit evidence: [acceptance record](docs/p1/P1_12_ACCEPTANCE.md#8-acceptance-checklist-phase_p1_planmd-section-8)).
- [x] English output/comments and complete English documentation (audit evidence: [acceptance record](docs/p1/P1_12_ACCEPTANCE.md#8-acceptance-checklist-phase_p1_planmd-section-8)).
- [x] P1 acceptance record with versions, working revision, evidence and limitations created (audit evidence: [acceptance record](docs/p1/P1_12_ACCEPTANCE.md#8-acceptance-checklist-phase_p1_planmd-section-8)).

P1 completes only after this evidence. P2 builds on the package/test foundation for export entities, UTC/unit/rounding logic, real JSON/XML serializers and generated output schemas. CLI decisions remain P3, source formats their parser phases and public package publication P9.


## Binding phase status and sequence

**2026-10-15: P0 complete; P1 solution/project scaffolding created. P1 is not yet accepted.**

**2026-10-19: P1 accepted by the user (see the [P1 acceptance record](docs/p1/P1_12_ACCEPTANCE.md#p1-acceptance-2026-10-19)). P2 is next and starts only on a separate instruction.**

Sequence with unchanged phase IDs: **P0 → P1 → P2 → P4 → P5 → P6 → P7 → P8 → P3 → P9**. Complete and accept the entire library first, including station selection, DWD/OsthessenWetter/MOSMIX_S/MOSMIX_L parsers, output entities, JSON/XML, XSDs, MQTT serializer and DI. Then begin P3 with the CLI usage example, configuration and example broker transport. Public release remains P9.

P1 contains only a technical package consumer as an integration test, not a CLI/application example. Before P3, accept source functionality directly through the library and its tests; CLI commands do not gate library phases. This sequence supersedes earlier numeric ordering or dependencies on P3. [P1 detailed plan](PHASE_P1_PLAN.md). The current task reviews existing tools and dependencies in P1.02; domain implementation remains deferred.


## Current implementation status (2026-10-15)

The structure portion of P1.03 is created: two-project solution, pinned SDK, shared build/package configuration and reserved package-test area. Restore/build verification is recorded in README. P1.02 dependency selection has initial evidence in DEPENDENCIES.md; complete transitive licensing and compatibility acceptance remain pending. P1.04–P1.12, TDD tests, coverage/report verification and full phase acceptance are not complete. Package consumers and CLI remain deferred as planned.


P1.01 entry audit completed on 2026-10-15: [record and starting inventory](docs/p1/P1_01_BASELINE.md). Existing scaffolding is preserved; no new domain code or test/coverage result is claimed. P1.02 review has since completed; current follow-up is recorded below.


**P1.02 review (2026-10-15) complete:** [tooling/license/compatibility report](docs/p1/P1_02_TOOLING_REVIEW.md). All 16 test packages reviewed; six direct versions current, no reported NuGet vulnerability/deprecation findings. At that review, SDK 10.0.303 was outside servicing; update to selected 10.0.402 and verify before P1.03 acceptance. No SDK/package changes or new tests were made in P1.02. The later P1.03 record below closes the SDK finding.

**P1.03 (2026-10-15) complete; Visual Studio build limitation resolved:** [build setup record](docs/p1/P1_03_BUILD_SETUP.md). global.json pinned to SDK 10.0.402, locked restore and CLI Release build succeeded. The previously reported Visual Studio/MSBuild NETSDK1141 failure was resolved via `dotnet build-server shutdown` and a full Visual Studio restart; a subsequent IDE solution build succeeded. Clean-checkout reproducibility after the first Git commit remains outstanding.
