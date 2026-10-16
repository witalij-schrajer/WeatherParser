# WeatherParser – P1 detailed plan

[Deutsch](PHASE_P1_PLAN.md) | [English](PHASE_P1_PLAN.en.md)

As of 2026-10-16. **P0 is complete. P1.01-P1.04 are complete; the Visual Studio build limitation is resolved, clean-checkout reproducibility remains pending.** The current task established the isolated package consumer in P1.04; domain tests, XSDs and CI workflows remain pending.

Basis: [requirements](REQUIREMENTS.en.md), [development plan](DEVELOPMENT_PLAN.en.md) and [repository rules](AGENTS.en.md).

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
| Language | English program output/comments; German/English documentation | Keep both document versions and developer procedures aligned |
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
| P1.01 | Check working state/P0 completion and confirm P1 scope | Completed on 2026-10-15; [entry audit](docs/p1/P1_01_BASELINE.en.md) |
| P1.02 | Verify SDK/packages, licenses and compatibility | Review complete; SDK update open; [report](docs/p1/P1_02_TOOLING_REVIEW.en.md) |
| P1.03 | Prepare minimal solution, library and test projects | Complete (SDK 10.0.402, locked restore, CLI build, IDE build); [record](docs/p1/P1_03_BUILD_SETUP.en.md); clean-checkout reproducibility pending |
| P1.04 | Establish local NuGet pack and isolated consumer | Complete; [record](docs/p1/P1_04_PACKAGE_CONSUMER.en.md) |
| P1.05 | Establish MSTest/Moq/FluentAssertions and TDD evidence | Meaningful first Red→Green→Refactor cycle; after P1.03 |
| P1.06 | Test-drive minimally needed package/DI contracts | Interfaces, endpoint mapping and real DI resolution; after P1.04/P1.05 |
| P1.07 | Collect Microsoft.CodeCoverage and verify report contract | Raw report, block breakdown and proven counter mapping; after P1.05/P1.06 |
| P1.08 | Test-drive coverage evaluation/exceptions | Boundary/error cases from section 5; after P1.07 |
| P1.09 | Prepare local CI job sequence/artifacts | Executable checks without production secrets; after P1.04–P1.08 |
| P1.10 | Verify Debian/ARM64 package foundations | Package/restore evidence; runtime success only after actual execution; after P1.09 |
| P1.11 | Complete bilingual developer instructions/evidence | Restore/build/pack/test/coverage procedures and limits; ongoing |
| P1.12 | Accept phase and document P2 handoff | Results, artifacts and remaining limitations; after all packages |

### Test-driven starting point

Select genuinely needed library behavior first, such as validating and unambiguously mapping consumer-supplied parser endpoints in DI integration. Demonstrate the expected contract failure, implement minimally and refactor under green tests. Create only necessary services/interfaces. Never register unimplemented parsers as functional or simulate successful parsing. Restore failures and existence-only checks do not prove TDD.

Construct subjects/shared mocks in `[TestInitialize]`; verify all mocks through `VerifyAll()` in `[TestCleanup]`. Real DI resolution and actual package consumption complement mocks. Keep tests offline. Serializer/time/rounding/XSD contracts follow in P2 and source adapters in P4–P7.

## 5. Coverage contract

Microsoft.CodeCoverage provides raw reports and the block breakdown. Verify the supported format and MSTest integration for the selected stable tool revision. Blocks are not branches: do not relabel a report value as another metric without evidence. Existing line/branch requirements remain. If a required metric cannot be demonstrated, document that specific limitation and resolve it before passing coverage acceptance; never fabricate values or silently replace the metric with block coverage.

| Case | Per-metric check |
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
| T-P1-09 | Minimum | 80% passes; lower values fail each metric |
| T-P1-10 | Regular code PR | Equality below 100% fails; actual improvement passes |
| T-P1-11 | Mixed metrics | One improvement cannot offset another metric's failure |
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
| T-P1-24 | Report/package provenance | Counters, blocks, PDB and version traceable; blocks not relabeled as branches |

## 7. CI sequence and deliverables

1. Verify SDK/packages, restore and build.
2. Pack locally, restore/build isolated consumer.
3. Run offline unit/DI/package tests with machine-readable results.
4. Collect Microsoft.CodeCoverage; provide raw report and readable block/code mapping.
5. Check PR category/baseline availability and apply the appropriate coverage rule.
6. Provide `.nupkg`, tool/license inventory, test/coverage reports, commit mapping and diagnostics as artifacts.

Public PR execution receives no production secrets or privileged publishing rights. No automatic NuGet/GitHub push. A locally verified pipeline is not a passed GitHub run; actual activation requires a separate instruction. Without an ARM64 target, report only available build/consumer evidence; full hardware acceptance remains P9.

## 8. Acceptance and handoff

- [ ] P0 completion and P1 implementation instruction documented.
- [ ] Minimal build, reproducible restore and pinned compatible dependencies with license evidence.
- [ ] Actual `.nupkg` and isolated package consumption verified.
- [ ] Meaningful TDD cycle, interfaces, constructor DI, MSTest/Moq/FluentAssertions and setup/cleanup demonstrated.
- [ ] Microsoft.CodeCoverage report with traceable block/metric mapping available.
- [ ] At least 80% for every required metric; bootstrap/documentation/100% rules demonstrated.
- [ ] Error/comparability/provenance/PR-classification tests pass.
- [ ] Local CI procedures/artifacts verified; truthful GitHub/ARM runtime status recorded.
- [ ] English output/comments and equivalent German/English documentation.
- [ ] P1 acceptance record with versions, working revision, evidence and limitations created.

P1 completes only after this evidence. P2 builds on the package/test foundation for export entities, UTC/unit/rounding logic, real JSON/XML serializers and generated output schemas. CLI decisions remain P3, source formats their parser phases and public package publication P9.


## Binding phase status and sequence

**2026-10-15: P0 complete; P1 solution/project scaffolding created. P1 is not yet accepted.**

Sequence with unchanged phase IDs: **P0 → P1 → P2 → P4 → P5 → P6 → P7 → P8 → P3 → P9**. Complete and accept the entire library first, including station selection, DWD/OsthessenWetter/MOSMIX_S/MOSMIX_L parsers, output entities, JSON/XML, XSDs, MQTT serializer and DI. Then begin P3 with the CLI usage example, configuration and example broker transport. Public release remains P9.

P1 contains only a technical package consumer as an integration test, not a CLI/application example. Before P3, accept source functionality directly through the library and its tests; CLI commands do not gate library phases. This sequence supersedes earlier numeric ordering or dependencies on P3. [P1 detailed plan](PHASE_P1_PLAN.en.md). The current task reviews existing tools and dependencies in P1.02; domain implementation remains deferred.


## Current implementation status (2026-10-15)

The structure portion of P1.03 is created: two-project solution, pinned SDK, shared build/package configuration and reserved package-test area. Restore/build verification is recorded in README. P1.02 dependency selection has initial evidence in DEPENDENCIES.en.md; complete transitive licensing and compatibility acceptance remain pending. P1.04–P1.12, TDD tests, coverage/report verification and full phase acceptance are not complete. Package consumers and CLI remain deferred as planned.


P1.01 entry audit completed on 2026-10-15: [record and starting inventory](docs/p1/P1_01_BASELINE.en.md). Existing scaffolding is preserved; no new domain code or test/coverage result is claimed. P1.02 review has since completed; current follow-up is recorded below.


**P1.02 review (2026-10-15) complete:** [tooling/license/compatibility report](docs/p1/P1_02_TOOLING_REVIEW.en.md). All 16 test packages reviewed; six direct versions current, no reported NuGet vulnerability/deprecation findings.

**P1.03 (2026-10-15) complete; Visual Studio build limitation resolved:** [build setup record](docs/p1/P1_03_BUILD_SETUP.en.md). global.json pinned to SDK 10.0.402, locked restore and CLI Release build succeeded. The previously reported Visual Studio/MSBuild NETSDK1141 failure was resolved via `dotnet build-server shutdown` and a full Visual Studio restart; a subsequent IDE solution build succeeded. Clean-checkout reproducibility after the first Git commit remains outstanding.
