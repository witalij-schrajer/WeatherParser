# P5.07 – Parser Service and Dataset Assembly: Implementation Plan

**Status: executed on 2026-10-25 ([record](../../p5/P5_VERIFICATION.md) section 7): Red evidence per slice, T-P5-09 to T-P5-11 and T-P5-19 cases green, coverage gate passed, Codex review 5, 1, 0 findings (all fixed test-first). S3 and S4 tests are in `DwdObservationParserDeadlineTests` and `DwdObservationParserLoggingTests`.** Reconciled with the approved observation contract ([OC rows](../../p5/P5_OBSERVATION_CONTRACT.md), approved 2026-10-24) on 2026-10-24: OC-02 (option A), OC-03, OC-04, OC-08, OC-10 and OC-11; conflict failure test added; after the PR #7 review of 2026-10-24: mapper omissions (`DwdMappingResult.Omissions`) logged on success and before a rejection, with composed all-calm, all-zero-direction and rejected cases. Refines the P5.07 row of section 5 and the rows "Dataset" and "Logging" of section 4 of [PHASE_P5_PLAN.md](../PHASE_P5_PLAN.md).

## 1. Goal and Scope
Goal: the public observation parser `IDwdObservationParser`/`DwdObservationParser` (OC-08, approved) that, for one station ID, validates the ID before any request, reads the lists and resolves the station, retrieves the station's product archives, maps the rows and builds the `ObservationDataset` through `IObservationDatasetBuilder` with provider `dwd`, `retrieved_at` from the consumer's `TimeProvider`, every notice and omission logged with a stable ID, and failure precedence caller cancellation > observation deadline > request timeout.
In scope: `Task<ObservationDataset> ParseAsync(string stationId, CancellationToken cancellationToken)`; the sequence: reference instant read once from `TimeProvider` and truncated to whole seconds (SC-13 analog) → deadline token from `DwdObservationOptions.ObservationDeadline` (finite by the OC-09 range validated in P5.06, so the timer delay is always valid) → ID grammar through `IDwdStationResolver.ValidateId(string stationId)` (part of the P5.03 interface, see decisions) → `IDwdObservationSource.ReadStationListsAsync` → `IDwdStationResolver.Resolve` → `IDwdObservationSource.ReadArchivesAsync` for the station's products → `IDwdObservationMapper.Map` (returning `DwdMappingResult`, P5.05) → `IObservationDatasetBuilder.Build` with `ObservationInput { Provider = "dwd", Location, RetrievedAtUtc, Readings }` (the result's `Readings`); notices `OBSERVATION-PRODUCT-MISSING` (per 404), `OBSERVATION-READING-OMITTED` (kind and reason per mapper omission in `DwdMappingResult.Omissions` and per builder `OmittedReading`, logged on a successful dataset and before a rejection propagates, one notice per omitted kind), list diagnostics (`CATALOG-RECORD-SKIPPED`, `CDC-STATION-CONFLICT`) not yet logged; domain failures propagate with their `ErrorId` (`DwdStationException`, `CatalogRetrievalException`, `CatalogFormatException`, `ObservationLimitException`); the builder's `ObservationDatasetRejectedException` (C-60, no `ErrorId`, unchanged), `OperationCanceledException` and argument exceptions propagate as they are; registration.
Out of scope with owner: `StationID` versus configured station, exit codes, stdout → P3; serialization → P5.08 (the parser returns the entity); README → P5.09; MQTT → P8.

## 2. Preconditions and Inputs
- P5.02 approved OC-03, OC-04, OC-08, OC-10, OC-11; P5.03 to P5.06 complete.
- Existing: `IObservationDatasetBuilder` (C-57 to C-60), `ObservationInput`, `ObservationReading`, `TimeProvider` registration by the consumer (SC-13), `ILogger<T>` pattern of the P4 selectors.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| ID validation before any request | `IDwdStationResolver.ValidateId(string stationId)` returning the validated ID (grammar only), added to the P5.03 interface; `Resolve` keeps re-checking / a separate validator interface | Add `ValidateId` to the resolver (P5.03 plan reconciled in P5.02 S5), so the parser rejects an invalid ID before `ReadStationListsAsync` | Orchestrator, recorded | S2 |
| Logging shape | structured `ILogger<DwdObservationParser>` events named by the notice ID with station ID, product code, kind and reason as properties / a result type with notices | Logging (OC-08); event names from a `DwdObservationNoticeIds` constants class (Proposed) | User (OC-08) | S4 |
| `retrieved_at` | reference instant before the first request, whole seconds / completion time | Before the first request, truncated (SC-13 analog) | Orchestrator, recorded (OC-04) | S1 |
| Missing-product behavior | notice and continue / fail | Notice and continue (OC-03); a station whose every product is missing ends in `ObservationDatasetRejectedException` through the builder (no readings) | User (OC-03) | S2 |
| Omission diagnostics on rejection | additive `OmittedReadings` property on `ObservationDatasetRejectedException` (P2 library type, not an export-contract change) / parsing the exception message / duplicating the builder's validation | The additive property, approved in OC-10 on 2026-10-24 and required, so that the parser logs every omitted kind and reason before the rejection propagates | User (OC-10) | S4 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Observations/Dwd/IDwdObservationParser.cs` | Create | Public interface |
| `src/WeatherParser.Parsing/Observations/Dwd/DwdObservationParser.cs` | Create | Implementation (OC-08) with `DwdObservationOptions`, `IDwdStationResolver`, `IDwdObservationSource`, `IDwdObservationMapper`, `IObservationDatasetBuilder`, `TimeProvider`, `ILogger<DwdObservationParser>` by constructor |
| `src/WeatherParser.Parsing/Observations/Dwd/DwdObservationNoticeIds.cs` | Create | Constants `OBSERVATION-PRODUCT-MISSING`, `OBSERVATION-READING-OMITTED` |
| `src/WeatherParser.Parsing/Observations/ObservationDatasetRejectedException.cs` | Change | Additive `OmittedReadings` property (approved in OC-10, required); test-first in this step |
| `src/WeatherParser.Parsing/Observations/ObservationDatasetBuilder.cs` | Change | Populates the approved `OmittedReadings` of the exception at the rejection site (C-60); constructor, message and every existing builder test unchanged |
| `src/WeatherParser.Parsing/Observations/Dwd/IDwdStationResolver.cs` | Change only if needed | `ValidateId` is already part of the P5.03 interface; this step changes the file only if P5.03 deviated from its plan |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Singleton registration |
| `tests/WeatherParser.Parsing.Tests/Observations/Dwd/DwdObservationParserTests.cs` | Create | Sequence, dataset, notices with strict mocks and a fake `TimeProvider` |
| `tests/WeatherParser.Parsing.Tests/Observations/Dwd/DwdObservationParserFailureTests.cs` | Create | Failure order, precedence, rejection |
| `tests/WeatherParser.Parsing.Tests/Observations/Dwd/ComposedDwdObservationPipelineTests.cs` | Create | Real resolver, mapper and builder over synthetic archives with a mocked downloader (characterization of the composed pipeline) |
| `docs/p5/P5_VERIFICATION.md` | Change | P5.07 section |

## 5. Procedure
### S1 Sequence and dataset
**Red**
- `ParseAsync_ValidStation_ReturnsDatasetFromBuilder`
- `ParseAsync_ReadsReferenceInstantOnceBeforeFirstRequest` (advancing fake clock; `RetrievedAtUtc` equals the first read, truncated to whole seconds)
- `ParseAsync_PassesProviderDwdAndLocation`
- `ParseAsync_RequestsOnlyResolvedProducts`
- `ParseAsync_SourceIsDwdColonStationId` (through the real builder: `dwd:00044`)
- `ParseAsync_DatasetMeasuredAtIsLatestReading` (real builder, C-55)
- `ParseAsync_NullStationId_ThrowsStationIdInvalidBeforeAnySourceCall` (OC-01: the resolver's `ValidateId` raises `DwdStationException` `STATION-ID-INVALID` for `null`; it propagates unchanged; strict source mock without setups)
- `AddWeatherParsing_ResolvesDwdObservationParser`
- `ServiceRegistrationCompletenessTests` red before registration
Expected failure: the parser tests fail with `NotImplementedException` from the stub; `AddWeatherParsing_ResolvesDwdObservationParser` fails with the container's resolution `InvalidOperationException` and `ServiceRegistrationCompletenessTests` with its missing-registration assertion.

**Green**
The sequence of section 1 with constructor dependencies, forwarding the caller's token only (the deadline token source and the stage checkpoints come in S3; no logging yet); registration.

**Refactor**
Remove duplication with all tests green.

**Roles**
qwen generates the tests first, then the minimal implementation and the refactoring; the orchestrating agent writes the briefs with the real declarations, runs each Red, build, tests and coverage, reviews the diff and records authorship.

**Check**
`dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`; `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S2 Failure order and missing products
**Red**
- `ParseAsync_InvalidId_ThrowsBeforeAnySourceCall` (strict source mock without setups)
- `ParseAsync_UnknownStation_ThrowsBeforeAnyArchiveRequest`
- `ParseAsync_ConflictingStation_ThrowsMetadataConflictBeforeAnyArchiveRequest` (OC-02 option A: `STATION-METADATA-CONFLICT` propagates unchanged, no archive request; the logging of its diagnostic is S4)
- `ParseAsync_MissingProduct_LogsNoticeAndContinues` (`OBSERVATION-PRODUCT-MISSING` with station and product)
- `ParseAsync_AllProductsMissing_ThrowsObservationDatasetRejected` (through the real builder; nothing logged as success)
- `ParseAsync_RetrievalFailure_Propagates` (`CatalogRetrievalException` unchanged)
- `ParseAsync_FormatFailure_Propagates` (`CDC-EVIDENCE-INVALID`)
- `ParseAsync_NoUsableReading_ThrowsObservationDatasetRejected`
- `ParseAsync_NeverUsesAnotherStationsData` (the resolver result's ID is the only ID passed to the source)
Expected failure: `ParseAsync_MissingProduct_LogsNoticeAndContinues` fails (the S1 implementation logs nothing) and `ParseAsync_AllProductsMissing_ThrowsObservationDatasetRejected` fails if the S1 implementation does not pass an empty reading list to the builder; `ParseAsync_InvalidId_ThrowsBeforeAnySourceCall`, `ParseAsync_UnknownStation_ThrowsBeforeAnyArchiveRequest`, `ParseAsync_ConflictingStation_ThrowsMetadataConflictBeforeAnyArchiveRequest`, `ParseAsync_RetrievalFailure_Propagates`, `ParseAsync_FormatFailure_Propagates`, `ParseAsync_NoUsableReading_ThrowsObservationDatasetRejected` and `ParseAsync_NeverUsesAnotherStationsData` are expected to pass on first run against the S1 sequence and are recorded as existing behavior; S1 is never weakened to manufacture failures.

**Green**
Order and propagation as specified; the `OBSERVATION-PRODUCT-MISSING` notice logged per missing product (the remaining logging comes in S4).

**Refactor**
As in S1.

**Roles**
As in S1.

**Check**
As in S1.

### S3 Deadline and cancellation precedence
**Red**
- `ParseAsync_DeadlineExpiresDuringLists_ThrowsObservationDeadlineExceeded`
- `ParseAsync_DeadlineExpiresDuringArchives_ThrowsObservationDeadlineExceeded`
- `ParseAsync_DeadlineExpiresBeforeMapping_ThrowsObservationDeadlineExceeded` (fake clock advanced inside the source callback after the archives returned; the mapper is never called)
- `ParseAsync_CallerCancelsDuringMapping_NoSuccessfulReturn` (cancellation requested inside the mapper mock; `OperationCanceledException` with the caller token; the builder is never called)
- `ParseAsync_DeadlineExpiresDuringBuild_ThrowsBeforeReturning` (fake clock advanced inside the builder mock; no dataset returned)
- `ParseAsync_CallerCancelsDuringLists_ThrowsOperationCanceledWithCallerToken`
- `ParseAsync_CallerCancelsAndDeadlineExpired_CallerWins`
- `ParseAsync_DeadlineResourcesReleasedAfterSuccessAndFailure` (an instrumented fake `TimeProvider` first shows that a deadline timer was created, then no outstanding timer after a successful and after a failed parse)
- `ParseAsync_SecondParseHasIndependentDeadline`
Expected failure: `ParseAsync_DeadlineExpiresDuringLists_ThrowsObservationDeadlineExceeded`, `ParseAsync_DeadlineExpiresDuringArchives_ThrowsObservationDeadlineExceeded`, `ParseAsync_DeadlineExpiresBeforeMapping_ThrowsObservationDeadlineExceeded`, `ParseAsync_DeadlineExpiresDuringBuild_ThrowsBeforeReturning`, `ParseAsync_CallerCancelsAndDeadlineExpired_CallerWins` and `ParseAsync_DeadlineResourcesReleasedAfterSuccessAndFailure` fail against the S2 implementation, which forwards only the caller's token (no deadline source, no checkpoints; the release test first asserts that a deadline timer was registered with the fake `TimeProvider`, so it cannot pass vacuously); `ParseAsync_CallerCancelsDuringLists_ThrowsOperationCanceledWithCallerToken`, `ParseAsync_CallerCancelsDuringMapping_NoSuccessfulReturn` and `ParseAsync_SecondParseHasIndependentDeadline` may pass on first run and are then recorded as existing behavior.

**Green**
Linked token source per parse; cancellation and deadline checkpoints after the lists, after the archives, before the build and before returning; precedence rule; disposal.

**Refactor**
As in S1.

**Roles**
As in S1.

**Check**
As in S1.

### S4 Logging
**Red**
- `ParseAsync_LogsOmittedReadingsWithKindAndReason` (`OBSERVATION-READING-OMITTED`, Warning)
- `ParseAsync_RejectedDataset_LogsEveryOmissionBeforeThrowing` (real builder, every reading missing: the omitted kinds and reasons are logged, then `ObservationDatasetRejectedException` propagates)
- `ParseAsync_MapperOmissions_LoggedOnSuccess` (mapper mock returns a valid temperature reading and the omission `WindDirection`/`CalmWind` → one `OBSERVATION-READING-OMITTED` with kind `WindDirection` and reason `CalmWind`; the dataset is returned)
- `ParseAsync_MapperOmissions_LoggedBeforeRejection` (real builder; mapper mock returns only missing readings and the omission `WindDirection`/`ZeroDirection` → notices for the mapper omission and every builder omission, each kind once, then `ObservationDatasetRejectedException` propagates)
- `ParseAsync_LogsMissingProductAsWarning`
- `ParseAsync_LogsListDiagnosticsNotYetLogged` (`CATALOG-RECORD-SKIPPED` once; `AlreadyLogged` respected)
- `ParseAsync_ConflictingStation_LogsConflictDiagnosticBeforeFailure` (the `Diagnostic` of the `STATION-METADATA-CONFLICT` exception logged once as `CDC-STATION-CONFLICT` with both endpoint names, then the exception propagates unchanged)
- `ParseAsync_LogEntriesCarryStationIdAndUtc`
- `ParseAsync_SuccessLogsNoValues` (no measurement values in log messages)
- `ParseAsync_NothingLoggedOnInvalidId`
Expected failure: `ParseAsync_LogsOmittedReadingsWithKindAndReason`, `ParseAsync_MapperOmissions_LoggedOnSuccess`, `ParseAsync_MapperOmissions_LoggedBeforeRejection`, `ParseAsync_LogsListDiagnosticsNotYetLogged`, `ParseAsync_ConflictingStation_LogsConflictDiagnosticBeforeFailure`, `ParseAsync_LogEntriesCarryStationIdAndUtc` and `ParseAsync_RejectedDataset_LogsEveryOmissionBeforeThrowing` fail against the S3 implementation (no omission and diagnostic logging, no structured properties, the exception not yet populated by the builder); `ParseAsync_LogsMissingProductAsWarning` (logged since S2), `ParseAsync_SuccessLogsNoValues` and `ParseAsync_NothingLoggedOnInvalidId` pass on first run and are recorded as existing behavior.

**Green**
Structured events with the notice ID as event name and the properties of section 1; the builder populates the approved `OmittedReadings` of `ObservationDatasetRejectedException` from its collected omissions before throwing (OC-10), so the parser logs them before the rejection propagates.

**Refactor**
As in S1.

**Roles**
As in S1.

**Check**
As in S1.

### S5 Composed pipeline
**Red**
No Red: characterization.
`ComposedDwdObservationPipelineTests` run the real resolver, source (mocked `ICatalogDownloader` serving synthetic lists and archives), mapper and builder end to end for one station with all four products, a station with a missing product, a station with only missing values, a station whose wind rows are all calm (`FF_10` 0) and one whose `DD_10` values are all 0, each with valid temperature (no `wind_direction_deg` in the dataset, exactly one `OBSERVATION-READING-OMITTED` for `WindDirection` with reason `CalmWind` or `ZeroDirection`), and a station whose wind rows all have `DD_10` 0 with `FF_10` −999 while every other mapped column is missing (no exportable reading, because a calm `FF_10` of 0 would be a valid wind-speed reading: `ObservationDatasetRejectedException`, preceded by exactly one `ZeroDirection` notice for `WindDirection` from the mapper and one notice per builder omission); expected to pass on first run and recorded as existing behavior; any failure is a defect of P5.03 to P5.07 fixed test-first in the owning step.

**Green**
No change expected.

**Refactor**
None.

**Roles**
qwen generates the composed tests and the synthetic inputs; the orchestrating agent reviews them for SC-14 compliance and runs them.

**Check**
As in S1.

### S6 Review and evidence
**Red**
No Red: review slice.

**Green**
Build, all tests, pre-commit coverage with the changed-file list from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard`, native block counters, baseline and gate result.

**Roles**
The orchestrating agent runs the checks; Codex reviews the uncommitted diff read-only; qwen fixes confirmed findings test-first; checkpoint commit when the user requests it; push; hosted CI run recorded.

**Check**
Pre-commit coverage (CONTRIBUTING.md section 5); Codex review; commit and CI run recorded in `docs/p5/P5_VERIFICATION.md`.

## 6. Tests and Test IDs
| Step-local case | T-P5 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P5-11 | REQ-050, REQ-031 | `dwd:<id>`, location, `retrieved_at` once and truncated, dataset `measured_at` latest, `timestamp_quality` `source`; only the resolved station's data |
| S2 | T-P5-11 | REQ-031, REQ-050 | Invalid IDs fail before any request; unknown IDs fail after the list retrieval and before any archive request; missing products controlled; no usable reading → rejection, never a success |
| S1, S5 | T-P5-09 | REQ-043, REQ-056 | Per-quantity times preserved through the pipeline; retrieval time only in `retrieved_at` |
| S3 | T-P5-10 | REQ-063 | Deadline during lists and archives → `OBSERVATION-DEADLINE-EXCEEDED`; caller cancellation wins |
| S4, S5 | T-P5-19 | REQ-044, REQ-072, REQ-053 | Notices with stable IDs, station ID and UTC; omissions with kind and reason from the builder and the mapper (OC-06), on success and before a rejection; no values in logs; nothing logged before validation |
| S1 | T-P5-13 (partial) | REQ-016, REQ-078 | Parser behind its public interface; constructor dependencies; registration |
| S1–S4 | T-P5-15 | REQ-085 | Recorded Red → Green → Refactor evidence |

## 7. Evidence to Record
- Red names with observed failures; characterization records of S5
- Commands, test counts, native block counters and gate result
- Codex review and dispositions; authorship

## 8. Risks and Limitations
- (a) `ValidateId` depends on the P5.03 interface — reconciled in P5.02 S5 before P5.03 starts
- (b) Logging as the notice channel means consumers must register logging to see notices — documented in P5.09 README
- (c) A station present in the lists but with every archive missing yields a rejection, not an empty dataset — intended (REQ-050)

## 9. Acceptance Criteria
- [ ] Each behavior increment has recorded Red evidence
- [ ] All T-P5-09, T-P5-10, T-P5-11 and T-P5-19 cases of this step pass
- [ ] No request before ID validation; no archive request before resolution
- [ ] Domain failures propagate with their `ErrorId`; builder rejection, cancellation and argument exceptions propagate unchanged; no fabricated values; never another station's data
- [ ] Parser registered; completeness and no-shared-state tests green
- [ ] Build without warnings; pre-commit coverage gate passed
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user
- [ ] No serialization, README or CLI code added
