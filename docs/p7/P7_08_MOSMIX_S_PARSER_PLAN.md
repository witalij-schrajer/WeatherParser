# P7.08 – MOSMIX_S Parser Service and Product Independence: Implementation Plan

**Status: executed on 2026-10-27 (reconciled with the approved [P7 forecast contract](P7_FORECAST_CONTRACT.md) in P7.02 S6; [verification record](P7_VERIFICATION.md) section 8).** Refines the P7.08 row of section 5 and the "Separation", "Archive and XML" and "Run" rows of section 4 of [PHASE_P7_PLAN.md](../../PHASE_P7_PLAN.md).

## 1. Goal and Scope
Goal: the public MOSMIX_S parser with the structure of P7.07 over the streamed all-stations file, and the proof that both parsers configured at once stay independent in endpoints, options, results and state.
In scope: `IMosmixSParser`/`MosmixSParser` with `ParseAsync(string mosmixId, CancellationToken)` and the result of MF-02; ID validation with the MOSMIX_S grammar (MF-03); one deadline per parse from `MosmixSForecastOptions.ParseDeadline` and the request bound of MF-05; the streamed read of P7.04 S3 to the end of the file (MF-06); `MOSMIX-ID-UNKNOWN` for an absent placemark; freshness, omissions, notices, rejection and logging as in P7.07; independence tests with both parsers in one container; registration.
Out of scope with owner: several IDs per download (not offered, MF-06); the MOSMIX_S evidence refresh of SC-22 (unchanged); wire form → P7.09; README and package → P7.10; target timing → P7.11.

## 2. Preconditions and Inputs
- P7.03 to P7.06 complete; P7.07 complete (its patterns and helpers); P7.02 approved MF-03, MF-05, MF-06, MF-09, MF-14 and MF-15 on 2026-10-26 (MOSMIX_S `ParseDeadline` default 5 minutes, request bound equal to it).
- Existing: `IMosmixSForecastSource`, `IMosmixForecastDocumentReader.ReadAllStationsAsync`, `IMosmixElementMapper`, `IForecastRunBuilder`, the P7.07 tests as the case list.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Shared parser code | Internal helpers used by both parsers (validation steps, deadline handling, freshness, logging) without a product parameter that changes behavior / a common base class with a product switch (rejected: REQ-109) | Internal helpers with product-specific inputs (product constant, options, source); each parser has its own public class and interface | User (REQ-109); helpers Orchestrator, recorded | S1 |
| Large-file test data | Generated at test time by `MosmixKmzBuilder` (thousands of placemarks, the selected one first, middle or last) / committed files (rejected: REQ-083 and size) | Generated; the generated sizes recorded | Orchestrator, recorded | S2 |
| Independence evidence | Composed tests with one container holding both parsers, two HTTP handler expectations and both option sets | As stated (T-P7-15) | Orchestrator, recorded | S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Forecasts/Mosmix/IMosmixSParser.cs`, `MosmixSParser.cs` and internal helpers shared with `MosmixLParser` | Create, Change | Parser and shared internals |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration |
| `tests/WeatherParser.Parsing.Tests/Forecasts/Mosmix/MosmixSParserTests.cs`, `ComposedMosmixSPipelineTests.cs`, `MosmixProductIndependenceTests.cs` | Create | Unit, composed and independence tests |
| `docs/p7/P7_VERIFICATION.md` | Change | P7.08 section |

## 5. Procedure
Every slice: roles and checks as in P7.03 section 5. The P7.07 tests stay green after the extraction of shared helpers (characterization of the refactoring).

### S1 Validation, deadline, request bound and precedence
**Red**: the P7.07 S1 case list for MOSMIX_S (`ParseAsync_InvalidId_ThrowsBeforeRequestAndTimer` with the MOSMIX_S grammar, `ParseAsync_DeadlineFromOptionsAndTimeProvider`, cancellation and deadline during retrieval and after reading, precedence during build, timer disposal, failures propagate with no fallback to MOSMIX_L), plus `ParseAsync_StreamLongerThan30Seconds_WithinDeadline_Succeeds` and `ParseAsync_StalledBeyondDeadline_ClassifiedDeadlineNotTimeout` (fake clock, real downloader; the request bound equals the deadline, MF-05), `AddWeatherParsing_ResolvesMosmixSParser`; `ServiceRegistrationCompletenessTests` red while unregistered.
**Green**: validation, deadline and precedence; registration; S1 green before S2.

### S2 Streamed run
**Red**: `ParseAsync_PlacemarkFirstMiddleLast_Run` (generated all-stations files), `ParseAsync_PlacemarkAbsent_ThrowsIdUnknown`, `ParseAsync_DuplicateIdAfterPlacemark_Fails`, `ParseAsync_CorruptAfterPlacemark_Fails`, `ParseAsync_RunWithProductSourceAndLocation` (`MOSMIX_S`, `dwd-mosmix-s:<id>`), the P7.07 S2 time, freshness, omission, not-exported-element, rejection and absence cases for MOSMIX_S, `ParseAsync_OneRunOneRequest`.
**Green**: streamed parse.

### S3 Diagnostics and independence
**Red**: the P7.07 S3 diagnostics cases for MOSMIX_S, including `ParseAsync_MemberName_NeverInLogsOrMessages`; `BothParsers_OneContainer_EachUsesOwnEndpointAndOptions` (requests counted per endpoint; each parser's deadline and `MaxRunAge` independent), `BothParsers_SameId_DistinctSourcesAndProducts`, `BothParsers_FailureOfOne_DoesNotAffectOther`, `BothParsers_ConcurrentParses_NoSharedState` (one parse of each product at once with distinct results), `PublicApi_NoProductSwitch` (no public parser type accepts a product parameter; reflection over the public forecast types), and `NoSharedStateTests` extended to the new services.
**Green**: fixes only where a case fails.

**Refactor**: remove duplication with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P7 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P7-01, T-P7-03 (parser level) | REQ-103, REQ-063 | Validation first, request bound, precedence |
| S2 | T-P7-06, T-P7-11 to T-P7-14 | REQ-104, REQ-105, REQ-107, REQ-110 | Streamed run, times, freshness, rejection |
| S3 | T-P7-15, T-P7-17 | REQ-109, REQ-016, REQ-072 | Independence; diagnostics |
| all | T-P7-18 (registration), T-P7-20 | REQ-078, REQ-085, REQ-086 | Registration; Red evidence; coverage |

## 7. Evidence to Record
- Red names and failures, counts, coverage, authorship, Codex rounds; generated file sizes and the composed request counts

## 8. Risks and Limitations
- (a) Every MOSMIX_S parse downloads the whole all-stations file (about 37.5 MB); README states the cost and the retrieval guidance (MF-16)
- (b) Test-time generation of large files lengthens the test run; sizes are chosen to stay within the CI time budget and recorded

## 9. Acceptance Criteria
- [ ] Every listed new-behavior test green with recorded Red; characterization tests (refactoring guards and existing behavior) green before and after and recorded as characterization, never with a manufactured Red; P7.07 tests unchanged and green
- [ ] Independence proven in one container; no public product switch
- [ ] Registration and architecture tests green; coverage gate passed; Codex 0 findings or reported
