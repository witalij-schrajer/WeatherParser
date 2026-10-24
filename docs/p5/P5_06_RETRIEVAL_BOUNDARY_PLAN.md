# P5.06 – Observation Retrieval Boundary: Implementation Plan

**Status: plan only (2026-10-24); nothing is implemented or verified yet.** Reconciled with the approved observation contract ([OC rows](P5_OBSERVATION_CONTRACT.md), approved 2026-10-24) on 2026-10-24: OC-03, OC-09, OC-10 and OC-11; no change of content (the approved `ObservationDeadline` range is the SC-18 range planned here). Refines the P5.06 row of section 5 and the "Retrieval" row of section 4 of [PHASE_P5_PLAN.md](../../PHASE_P5_PLAN.md).

## 1. Goal and Scope
Goal: an injectable observation source that reads the four CDC station lists and the resolved station's product archives through the P4 `ICatalogDownloader` under the observation deadline (OC-09, OC-11), with HTTP 404 as a controlled missing product (OC-03), the SC-09 bounds and no retries, and the additive `CatalogDeadlineKind.Observation` whose expiry is reported as `OBSERVATION-DEADLINE-EXCEEDED` while the P4 selection behavior stays unchanged.
In scope: `DwdObservationOptions` (`ObservationDeadline`, default 120 s, permitted greater than zero and at most 1 hour (the SC-18 range); `Timeout.InfiniteTimeSpan`, zero, negative values, values above the maximum and `TimeSpan.MaxValue` throw `OBSERVATION-LIMIT-INVALID` at construction, before any timer is created or any request made (a `CancellationTokenSource` delay above about 49.7 days throws `ArgumentOutOfRangeException` and an infinite delay never fires; both reproduced on .NET 10 on 2026-10-24); approved, OC-09); `CatalogDeadlineKind.Observation` and its mapping in `CatalogDownloader` to `ObservationLimitException` `OBSERVATION-DEADLINE-EXCEEDED` (approved, OC-09, OC-10); `IDwdObservationSource`/`DwdObservationSource` (Proposed) with `Task<IReadOnlyList<CdcProductList>> ReadStationListsAsync(CancellationToken deadline, CancellationToken cancellationToken)` and `Task<IReadOnlyDictionary<string, CdcNowEvidence?>> ReadArchivesAsync(string stationId, IReadOnlyList<string> productCodes, CancellationToken deadline, CancellationToken cancellationToken)` (a null value = HTTP 404); URI composition shared with the P4 source through a public injectable composer `ICdcRequestComposer`/`CdcRequestComposer` (Proposed; public declarations, because `CdcCatalogSource` is public and the architecture test `NonVisibleInterfaces_AreOnlyTheEntityValidationHook` permits no other non-visible interface; no duplicated composition rules); at most 4 parallel archive requests; registration.
Out of scope with owner: ID validation and lookup → P5.03; row parsing → P5.04; mapping → P5.05; orchestration, `TimeProvider` and logging → P5.07; a `DwdObservationOptions` validator interface if the user prefers one over construction-time checks → OC-09.

## 2. Preconditions and Inputs
- P5.02 approved OC-03, OC-09, OC-10, OC-11; P5.03 and P5.04 complete.
- Existing: `ICatalogDownloader`, `CatalogRequest`, `CatalogDeadlineKind`, `CdcCatalogEndpoints` (validated by `ICdcCatalogEndpointsValidator`), `ICdcStationCatalogReader`, `ICdcNowEvidenceReader`, `CdcCatalogSource` (reference for composition and parallelism), `CatalogRetrievalException`, `CatalogFormatException`.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Options validation | checks in the `DwdObservationSource` constructor raising `ObservationLimitException` `OBSERVATION-LIMIT-INVALID` (approved in OC-10) / a separate validator interface | Constructor checks with the approved ID (pattern of `SELECTION-LIMIT-INVALID`) | User (OC-09, OC-10) | S1 |
| Deadline range | the SC-18 range (greater than zero, at most 1 hour) / another finite maximum | The SC-18 range, so that the P5.07 deadline timer always receives a valid finite delay; approved in OC-09 before S1 starts and recorded identically in `PHASE_P5_PLAN.md` section 3 and `P5_02_SOURCE_FACTS_AND_CONTRACT_PLAN.md` | User (OC-09) | S1 |
| Deadline mapping | additive enum member and `CatalogDownloader` mapping / exception translation in the source | Additive member mapped in `CatalogDownloader`; `Selection` and `Refresh` behavior unchanged and covered by the existing tests | Orchestrator, recorded (OC-09) | S1 |
| Shared URI composition | injectable `ICdcRequestComposer`/`CdcRequestComposer` (Proposed) used by `CdcCatalogSource` and `DwdObservationSource`, registered in `AddWeatherParsing` / duplicated code | The injectable service, extracted from `CdcCatalogSource` test-first (REQ-078, C-107); the P4 tests stay green with the composer added to the `CdcCatalogSource` constructor and to the test fixtures | Orchestrator, recorded (SC-21) | S2, S3 |
| Bounds | `ListOrCatalogMaxBytes` for lists, `PerStationFileMaxBytes` for archives (SC-09) / other | SC-09 constants | User (approved SC-09) | S2, S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Observations/Dwd/DwdObservationOptions.cs` | Create | Options class |
| `src/WeatherParser.Parsing/Observations/Dwd/IDwdObservationSource.cs` | Create | Interface |
| `src/WeatherParser.Parsing/Observations/Dwd/DwdObservationSource.cs` | Create | Proposed implementation |
| `src/WeatherParser.Parsing/Exceptions/ObservationLimitException.cs` | Create | `ErrorId` `OBSERVATION-DEADLINE-EXCEEDED`, `OBSERVATION-LIMIT-INVALID` with the guard pattern |
| `src/WeatherParser.Parsing/Catalogs/CatalogDeadlineKind.cs` | Change | Additive `Observation` |
| `src/WeatherParser.Parsing/Catalogs/CatalogDownloader.cs` | Change | Mapping of the new kind |
| `src/WeatherParser.Parsing/Catalogs/CdcCatalogSource.cs` | Change | Uses the extracted composition helper; behavior unchanged |
| `src/WeatherParser.Parsing/Catalogs/ICdcRequestComposer.cs`, `src/WeatherParser.Parsing/Catalogs/CdcRequestComposer.cs` | Create | Proposed composition service (list URIs, archive URIs per product) behind an interface, registered as a singleton |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Singleton registration |
| `tests/WeatherParser.Parsing.Tests/TestConsumerServices.cs`, `tests/WeatherParser.Package.Tests/TestConsumerServices.cs` | Change | Register `DwdObservationOptions` so that every validated container still builds (C-108) |
| `tests/WeatherParser.Parsing.Tests/Observations/Dwd/DwdObservationSourceTests.cs` | Create | Lists, archives, 404, bounds, parallelism |
| `tests/WeatherParser.Parsing.Tests/Observations/Dwd/DwdObservationSourceDeadlineTests.cs` | Create | Deadline, cancellation precedence, options validation |
| `tests/WeatherParser.Parsing.Tests/Catalogs/CatalogDownloaderObservationDeadlineTests.cs` | Create | Downloader mapping of the new kind; existing kinds unchanged |
| `docs/p5/P5_VERIFICATION.md` | Change | P5.06 section |

## 5. Procedure
### S1 Options and deadline kind
OC-09 must have approved the finite permitted range of `ObservationDeadline` before S1 starts (recommended: greater than zero and at most 1 hour, as SC-18). The constructor rejects zero, negative values, `Timeout.InfiniteTimeSpan`, values above the approved maximum and `TimeSpan.MaxValue` with `OBSERVATION-LIMIT-INVALID` before creating a timer or making a request; the message names the option and the permitted range.

**Red**
- `DwdObservationOptions_DefaultDeadline_Is120Seconds`
- `DwdObservationSource_ZeroDeadline_ThrowsLimitInvalid`
- `DwdObservationSource_NegativeDeadline_ThrowsLimitInvalid`
- `DwdObservationSource_InfiniteDeadline_ThrowsLimitInvalid`
- `DwdObservationSource_SmallestPositiveDeadline_IsAccepted` (one tick)
- `DwdObservationSource_DeadlineAtApprovedMaximum_IsAccepted`
- `DwdObservationSource_DeadlineOneTickAboveApprovedMaximum_ThrowsLimitInvalid`
- `DwdObservationSource_MaxValueDeadline_ThrowsLimitInvalid`
- `DwdObservationSource_NullOptions_ThrowsArgumentNullException`
- `DwdObservationSource_InvalidEndpoints_ThrowsEndpointInvalidAtConstruction` (through the injected `ICdcCatalogEndpointsValidator`; `ENDPOINT-INVALID`; no downloader call)
- `DwdObservationSource_Construction_MakesNoRequest`
- `ObservationLimitException_UnknownErrorId_ThrowsArgumentException`
- `CatalogDownloader_ObservationDeadlineExpired_ThrowsObservationDeadlineExceeded`
- `CatalogDownloader_SelectionDeadlineExpired_StillSelectionDeadlineExceeded` (regression)
- `CatalogDownloader_CallerCancelledAndObservationDeadline_ThrowsOperationCanceled` (precedence)
- `AddWeatherParsing_ResolvesDwdObservationSource`
- `ServiceRegistrationCompletenessTests` red before registration
Expected failure: the enum member and exception type do not exist (compile error recorded as the Red observation); the stub throws `NotImplementedException`.

**Green**
- options class, exception type, enum member, downloader mapping, constructor validation of the options against the approved range (both bounds inclusive on the accepted side) and of `CdcCatalogEndpoints` through the injected `ICdcCatalogEndpointsValidator` (SC-12), registration.

**Refactor**
- remove duplication with all tests green.

**Roles**
- qwen generates the tests first, then the minimal implementation and the refactoring; the orchestrating agent writes the briefs with the real declarations, runs each Red, build, tests and coverage, reviews the diff and records authorship.

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`; `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S2 Station lists
**Red**
- `ReadStationListsAsync_RequestsFourListsWithObservationKind` (endpoint names, URIs, `ListOrCatalogMaxBytes`, `NotFoundIsAbsent` false, `DeadlineKind.Observation`);
- `ReadStationListsAsync_ReturnsProductListsInProductOrder` (`tu`, `ff`, `rr`, `sd`)
- `ReadStationListsAsync_NotFound_IsRetrievalFailure` (`NotFoundIsAbsent` false → `CATALOG-RETRIEVAL-FAILED`);
- `ReadStationListsAsync_FormatFailure_Propagates` (`CatalogFormatException` from the reader);
- `ReadStationListsAsync_RetrievalFailure_Propagates`;
- `ReadStationListsAsync_DeadlineExpired_ThrowsObservationDeadlineExceeded`;
- `ReadStationListsAsync_CallerCancelled_ThrowsOperationCanceled`;
- `CdcCatalogSource_UsesSharedComposition_P4TestsUnchanged` (the existing `CdcCatalogSource` tests stay green after the extraction, with the composer added to its constructor and to the test fixtures).
Expected failure: `NotImplementedException`.

**Green**
- composition service extracted and registered; four requests through `ICatalogDownloader` with the observation kind; `ICdcStationCatalogReader.ReadAsync` per stream.

**Refactor**
- as in S1.

**Roles**
- as in S1.

**Check**
- as in S1.

### S3 Archives
**Red**
- `ReadArchivesAsync_ComposesArchiveUriPerProduct` (`10minutenwerte_TU_00044_now.zip` under the air-temperature `now` directory; `wind`, `nieder`, `SOLAR` likewise);
- `ReadArchivesAsync_UsesPerStationBoundAndObservationKind` (`PerStationFileMaxBytes`, `NotFoundIsAbsent` true);
- `ReadArchivesAsync_NotFound_ReturnsNullForThatProduct`;
- `ReadArchivesAsync_OtherFailure_Propagates` (`CATALOG-RETRIEVAL-FAILED`, `CATALOG-TOO-LARGE`, `CDC-EVIDENCE-INVALID`);
- `ReadArchivesAsync_NeverMoreThanFourConcurrentRequests` (a gate in the downloader mock records the peak concurrency with the four products; the bound equals the product count, so the test proves that all four may run in parallel and that no request beyond the given products is made)
- `ReadArchivesAsync_DuplicateProductCode_ThrowsArgumentExceptionBeforeAnyRequest`;
- `ReadArchivesAsync_UnknownProductCode_ThrowsArgumentException`;
- `ReadArchivesAsync_RequestsOnlyGivenProducts`;
- `ReadArchivesAsync_DeadlineExpiredDuringArchive_ThrowsObservationDeadlineExceeded`;
- `ReadArchivesAsync_CallerCancelledDuringArchive_ThrowsOperationCanceled`;
- `ReadArchivesAsync_PassesRequestedStationIdToReader` (SC-19 identity check).
Expected failure: `NotImplementedException`.

**Green**
- per product one request with the composed URI; `ICdcNowEvidenceReader.ReadAsync(stream, stationId, productCode, token)`; null on 404; parallelism bounded as in `CdcCatalogSource`; duplicate or unknown codes rejected before any request.

**Refactor**
- as in S1.

**Roles**
- as in S1.

**Check**
- as in S1.

### S4 Review and evidence
**Red**
- No Red: review slice.

**Green**
- build, all tests, pre-commit coverage with the changed-file list from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard`, native block counters, baseline and gate result.

**Roles**
- the orchestrating agent runs the checks; Codex reviews the uncommitted diff read-only; qwen fixes confirmed findings test-first; checkpoint commit when the user requests it; push; hosted CI run recorded.

**Check**
- pre-commit coverage (CONTRIBUTING.md section 5); the existing package-consumer tests against the freshly packed library (CONTRIBUTING.md section 4) before the review; Codex review; commit and CI run recorded in `docs/p5/P5_VERIFICATION.md`.

## 6. Tests and Test IDs
| Step-local case | T-P5 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P5-10 | REQ-063 | Options validated at construction against the approved finite range (both boundaries, one tick above, infinite, `TimeSpan.MaxValue`); `Observation` deadline kind mapped to `OBSERVATION-DEADLINE-EXCEEDED`; `Selection` unchanged; caller cancellation precedence |
| S2, S3 | T-P5-10 | REQ-014, REQ-063 | Consumer endpoints only; SC-09 bounds; no retries; 404 → null; other failures propagate; at most eight requests, 4 in parallel; deadline during lists and archives |
| S1 | T-P5-13 (partial) | REQ-016, REQ-078 | Source behind its interface; registration; completeness test green |
| S1–S3 | T-P5-15 | REQ-085 | Recorded Red → Green → Refactor evidence |

## 7. Evidence to Record
- Red names with observed failures (including compile-error observations); existing-behavior records of the P4 regression
- Commands, test counts, native block counters and gate result
- Codex review and dispositions; authorship

## 8. Risks and Limitations
- (a) Extracting the composition helper changes P4 code — covered by the existing `CdcCatalogSource` tests, which must stay green
- (b) A list format failure fails the parse even when the station's archives are fine — intended (SC-10 analog), recorded in the contract
- (c) The archive name pattern changes at DWD — detected by P5.02 and the optional live check, not by offline tests

## 9. Acceptance Criteria
- [ ] Each behavior increment has recorded Red evidence
- [ ] All T-P5-10 cases of this step pass; P4 downloader and source tests unchanged and green
- [ ] No built-in or fallback URL; all URIs from `CdcCatalogEndpoints`
- [ ] `ObservationDeadline` outside the approved range (including infinite and `TimeSpan.MaxValue`) fails construction with `OBSERVATION-LIMIT-INVALID`; both accepted boundaries construct
- [ ] An archive 404 never fails the parse; a station-list 404 and every other domain failure propagate with their `ErrorId`; caller cancellation (`OperationCanceledException`) and argument exceptions propagate unchanged
- [ ] Source registered; completeness and no-shared-state tests green
- [ ] Build without warnings; pre-commit coverage gate passed
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user
- [ ] No mapping, dataset assembly or logging code added
