# P5.09 – DI Registration, README and Package Consumption: Implementation Plan

**Status: plan only (2026-10-24); nothing is implemented or verified yet.** Refines the P5.09 row of section 5 of [PHASE_P5_PLAN.md](../../PHASE_P5_PLAN.md).

## 1. Goal and Scope
Goal: prove the integrated DI configuration of the observation parser with consumer-registered options, document the library feature for consumers in README.md (registration, attribution, IP note, retrieval guidance, error and notice IDs, quality-code meaning), and prove same-run package consumption of the parser from the independent consumer project.
In scope: DI scenarios (consumer registers the complete P4 consumer boundary (`CdcCatalogEndpoints`, `MosmixCatalogEndpoints`, `HttpClient`, `TimeProvider`, logging; the shared `TestConsumerServices` helpers) plus `DwdObservationOptions`; two independent containers with different options; a container missing `DwdObservationOptions` fails at `BuildServiceProvider` validation, C-108); README section "DWD observations (library)" with the OC-12 guidance and the OC-10 IDs; README feature status of DWD observation parsing turned from planned to implemented for the library while `--ParseDWD` stays a planned CLI command; package-consumer tests over the same-run `.nupkg` (parser resolved from the package, synthetic lists and archives served by a test `HttpMessageHandler`, serialized dataset checked against hand-computed expected values and JSON ↔ XML equivalence, as in `PackageSelectionEquivalenceTests`); README package check (CONTRIBUTING.md section 4); AGENTS.md consistency table (README change → package check; no package version change, so `DEPENDENCIES.md` unchanged).
Out of scope with owner: CLI configuration and `--ParseDWD` execution → P3; MQTT topics → P8; publication → P9; new production behavior → none (defects are fixed test-first in the owning step).

## 2. Preconditions and Inputs
- P5.08 complete; OC-10 and OC-12 approved.
- Existing: `AddWeatherParsing`, the P4 DI tests (`SelectionDependencyInjectionTests`, `SelectionDependencyInjectionRoutingTests`), the package-consumer project and its P4 tests, README sections of P4.10.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| README placement | new top-level section "DWD observations (library)" after "Station and location selection (library)" / subsection of the existing one | New section with the same subsections (consumer registration, retrieval guidance, error and notice IDs) | Orchestrator, recorded | S3 |
| Retrieval guidance text | per OC-12 (at most one parse per station per hour, external scheduling, no polling) | As approved in OC-12 | User (OC-12) | S3 |
| Quality-code documentation | README explains `quality` values `1`, `2`, `3` with the DWD meanings / link only | Explain the three levels and link the DWD descriptions | Orchestrator, recorded (OC-07) | S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `tests/WeatherParser.Parsing.Tests/Observations/Dwd/DwdObservationDependencyInjectionTests.cs` | Create | DI scenarios |
| `tests/WeatherParser.Package.Tests/PackageDwdObservationTests.cs` | Create | Parser from the package, parse over synthetic inputs, JSON and XML outputs |
| `tests/WeatherParser.Package.Tests/PackageDwdObservationEquivalenceTests.cs` | Create | JSON ↔ XML equivalence and expected-graph checks of the package result over fixed synthetic inputs and a fixed fake `TimeProvider` (pattern of `PackageSelectionEquivalenceTests`); no source-side oracle and no project reference |
| `README.md` | Change | New section; feature status; error and notice IDs table; data-sources section attribution |
| `docs/p5/P5_VERIFICATION.md` | Change | P5.09 section |

## 5. Procedure
### S1 DI scenarios
**Red**
- `AddWeatherParsing_WithConsumerOptions_ResolvesDwdObservationParser`
- `AddWeatherParsing_WithoutDwdObservationOptions_FailsAtBuildValidation` (C-108; the complete P4 boundary is registered, so the validation message names `DwdObservationOptions`)
- `AddWeatherParsing_TwoContainersDifferentDeadlines_Independent`
- `AddWeatherParsing_RegistersNoHttpClientTimeProviderOrOptions` (the consumer registers them)
- `AddWeatherParsing_ParserIsSingleton`
- `AddWeatherParsing_ParserUsesConsumerHttpClient` (a recording handler sees the parse's requests)

Expected failure: characterization is likely for most scenarios (production registration exists since P5.07); scenarios that pass on first run are recorded as existing behavior; a failing scenario is a defect fixed test-first in P5.07.

**Green**
- no change expected.

**Refactor**
- none.

**Roles**
- qwen generates the tests; the orchestrating agent reviews, runs and records first-run results.

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`; `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S2 Package consumer
**Red**
- `Package_ResolvesDwdObservationParser`
- `Package_ParseAsync_SyntheticStation_ProducesSchemaValidJsonAndXml` (handler serves four synthetic lists and four archives)
- `Package_ParseAsync_MissingProduct_Continues`
- `Package_ParseAsync_InvalidId_ThrowsStationIdInvalidWithoutRequest`
- `Package_Result_MatchesExpectedGraphAndJsonXmlEquivalent` (fixed inputs and clock; expected values hand-computed as in P5.08; JSON and XML equivalent)

Expected failure: none is designated as Red; the same-run `.nupkg` is packed and restored first (CONTRIBUTING.md section 4), then the tests are characterization recorded with their first-run results; a failing scenario is a reproduced defect fixed test-first by qwen in the owning step.

**Green**
- no change expected.

**Refactor**
- none.

**Roles**
- qwen generates the consumer tests; the orchestrating agent runs CONTRIBUTING.md section 4 (pack, restore with the refreshed consumer lock, test) and records the packed and restored hashes; the consumer lock is not committed.

**Check**
- CONTRIBUTING.md section 4 commands; `dotnet test tests/WeatherParser.Package.Tests -c Release`.

### S3 README
**Red**
- No Red: documentation.

**Green**
- new README section with a consumer registration example that extends the complete P4 registration (both endpoint option classes, `HttpClient`, `TimeProvider`, logging) by `DwdObservationOptions` (default 120 s and the approved OC-09 range of `ObservationDeadline` with the rejected values, as the SC-18 passage does for `SelectionDeadline`), the OC-12 retrieval guidance, the CC BY 4.0 attribution "Deutscher Wetterdienst (DWD)" with links, the IP-retention note, the error and notice IDs `STATION-ID-INVALID`, `STATION-UNKNOWN`, `OBSERVATION-DEADLINE-EXCEEDED`, `OBSERVATION-LIMIT-INVALID`, `OBSERVATION-PRODUCT-MISSING`, `OBSERVATION-READING-OMITTED`, the `quality` levels, and the feature-status change (library implemented, CLI planned); README package check.

**Refactor**
- none.

**Roles**
- the orchestrating agent writes the README text, verifies every ID, URL and example against the code and the contract, runs the package check (README.md identical in the package) and records it.

**Check**
- CONTRIBUTING.md section 4 package check on the working tree; `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`.

### S4 Review and evidence
**Red**
- No Red: review slice.

**Green**
- build, all tests, pre-commit coverage with the changed-file list from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard`, native block counters, baseline and gate result.

**Roles**
- the orchestrating agent runs the checks; Codex reviews the uncommitted diff read-only (including the README text); confirmed findings in code and tests are fixed by qwen, in the README text by the orchestrating agent; checkpoint commit when the user requests it; push; hosted CI run recorded; the README package check is repeated on the committed revision through `ci-local.ps1`.

**Check**
- pre-commit coverage (CONTRIBUTING.md section 5); Codex review; `pwsh ./scripts/ci-local.ps1 -Commit <sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings` after the commit; commit and CI run recorded in `docs/p5/P5_VERIFICATION.md`.

## 6. Tests and Test IDs
| Step-local case | T-P5 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P5-13 | REQ-013, REQ-014, REQ-016, REQ-078 | Consumer-registered options, `HttpClient`, `TimeProvider`, logging; independent containers; missing options fail at validation; singleton parser behind its interface |
| S2 | T-P5-13 | REQ-013, REQ-016 | Same-run package consumer resolves and uses the parser; equal results |
| S3 | T-P5-14 | REQ-090, REQ-092 | README section: attribution, IP note, retrieval guidance, IDs, quality levels, feature status; package check |
| S1–S2 | T-P5-15 | REQ-085 | Characterization recorded with provenance |

## 7. Evidence to Record
- First-run results of every DI and package scenario; packed and restored package hashes; the consumer lock not committed
- README package check result; Codex review and dispositions; authorship

## 8. Risks and Limitations
- (a) The README grows long — the new section mirrors the P4.10 structure; no duplication of the P4 transport text (link instead)
- (b) Package tests depend on the same-run `.nupkg` — hashes recorded per run (C-116)
- (c) Guidance "once per hour" is a project recommendation, not a provider guarantee — stated as such (SC-16 pattern)

## 9. Acceptance Criteria
- [ ] All T-P5-13 and T-P5-14 cases pass; first-run results recorded
- [ ] README section complete and consistent with the code and the contract; package check passed on the working tree and on the committed revision
- [ ] `DEPENDENCIES.md` unchanged (no new package)
- [ ] Build without warnings; pre-commit coverage gate passed
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user
- [ ] No CLI or MQTT content added
