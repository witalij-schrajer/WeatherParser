# P6.09 – DI Registration, README and Package Consumption: Implementation Plan

**Status: plan only (2026-10-25); nothing is implemented yet.** Refines the P6.09 row of section 5 of [PHASE_P6_PLAN.md](../../PHASE_P6_PLAN.md). Subject to reconciliation with the approved OW-04, OW-10, OW-13 and OW-14 (P6.02).

## 1. Goal and Scope
Goal: prove that a consumer can register and use the OsthessenWetter parser through `AddWeatherParsing` with its own endpoint, options and station metadata, both inside the solution and from the same-run `.nupkg`, and document the library usage, source conditions and diagnostics in README.
In scope: integrated DI tests (consumer-registered `OsthessenWetterEndpoints`, `OsthessenWetterOptions`, `HttpClient`, `TimeProvider`, logging; `ValidateOnBuild`; independent containers; invalid options fail at resolution); package-consumer tests over the consumer host; README section "OsthessenWetter observations (library)"; feature status turned from planned to implemented; phase-status passages; README package check.
Out of scope with owner: CLI → P3; MQTT topics → P8; publication → P9.

## 2. Preconditions and Inputs
- P6.08 complete.
- Existing: `ServiceCollectionExtensionsTests`, the P5.09 DI tests, `tests/WeatherParser.Package.Tests` (`ConsumerSelectionHost`, `ConsumerFixtures`, `ConsumerHttpHandler`), README section "DWD observations (library)" as the pattern, CONTRIBUTING.md section 4.
- Registration consequence (as in P5): consumers of `AddWeatherParsing` must register the OsthessenWetter endpoint and options, because `ValidateOnBuild` resolves every registered service; README states it.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| README content | Usage example with endpoint, options and metadata; source conditions and their clarification status (OW-14); attribution "Osthessen-Wetter.de"; retrieval guidance (external scheduling, at most one retrieval every 10 minutes, consumer User-Agent); pattern semantics; result and failure handling; error and notice IDs; limitations (supported revision, stale displays, precipitation not exported) | As stated | User (OW-14 wording) | S2 |
| Package fixtures | Synthetic HTML embedded in the package-test project / shared with the parsing tests | Own copy in the package-test project, so the consumer stays independent of the parsing test project | Orchestrator, recorded | S1 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `tests/WeatherParser.Parsing.Tests/Observations/Osthessen/OsthessenWetterDependencyInjectionTests.cs` | Create | Integrated DI tests |
| `tests/WeatherParser.Package.Tests/PackageOsthessenWetterTests.cs`, consumer host and fixture changes | Create/Change | Package-consumer tests |
| `README.md` | Change | New section, feature status, phase status |
| `AGENTS.md`, `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md`, `PHASE_P6_PLAN.md` | Change | Status passages |
| `docs/p6/P6_VERIFICATION.md` | Change | P6.09 section |

## 5. Procedure
Roles and checks as in P6.03 section 5; the README is written by the orchestrating agent.

### S1 DI and package consumption
**Characterization** (as in P5.09: the behavior was implemented and test-driven in P6.03 to P6.07, so these tests are expected to pass on first run; the first runs are recorded as characterization; a failure caused by a product defect keeps its correct expectation as the reproducing Red test and is fixed test-first through qwen): `AddWeatherParsing_WithConsumerOptions_ResolvesParser`, `AddWeatherParsing_MissingOsthessenOptions_FailsValidateOnBuild`, `AddWeatherParsing_InvalidDeadline_FailsAtResolution`, `AddWeatherParsing_TwoContainers_IndependentEndpointsAndMetadata`, `AddWeatherParsing_ParserSingleton`, `Package_ParseAsync_OneAndSeveralStations` (consumer host, HTTP handler mocked), `Package_Datasets_SerializeAndValidate`, `Package_NoMatch_ThrowsPatternNoMatch`.
**Green**: test hosts and fixtures only; production changes only if a defect appears (then test-first in the owning step's code).

### S2 README and status
- README section written; feature status of OsthessenWetter parsing changed to implemented (library); status passages updated; README package check (CONTRIBUTING.md section 4) on the working tree and `ci-local.ps1` on the committed revision.

## 6. Tests and Test IDs
| Step-local case | T-P6 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P6-13 | REQ-013, REQ-014, REQ-016, REQ-078 | DI with consumer options, independent containers, same-run package |
| S2 | T-P6-14 | REQ-090, REQ-092 | README content and package check |
| S1 | T-P6-15 | REQ-085 | Characterization runs recorded; Red only for a reproduced defect |

## 7. Evidence to Record
- First-run outcome per test (characterization), any reproduced defect with its Red, counts, the package hash (packed equals restored), the README SHA-256 in and outside the package, authorship, Codex rounds

## 8. Risks and Limitations
- (a) Existing consumers that do not use OsthessenWetter must now register its options (as with `DwdObservationOptions` in P5) — documented in README; recorded as a limitation
- (b) The consumer lock is not committed (CONTRIBUTING.md section 4)

## 9. Acceptance Criteria
- [ ] DI and package tests green; first runs recorded as characterization
- [ ] README section complete, including the source-condition status; package check passed
- [ ] Status passages consistent (AGENTS.md table)
- [ ] Coverage gate passed; Codex 0 findings or reported
