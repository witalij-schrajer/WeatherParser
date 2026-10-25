# P6.07 – Parser Service and Dataset Assembly: Implementation Plan

**Status: plan only (2026-10-25); nothing is implemented yet.** Refines the P6.07 row of section 5 and the "Station set", "Dataset" and "Logging" rows of section 4 of [PHASE_P6_PLAN.md](../../PHASE_P6_PLAN.md). Reconciled on 2026-10-25 with the OsthessenWetter contract approved by the user on 2026-10-25 ([contract](P6_OSTHESSEN_CONTRACT.md), option A in every row): OW-03 collision scope all overview names; OW-16 `retrieved_at` sampled after the page source returns, with the delayed-response test (contract section 6.1); OW-08, OW-12 and OW-13 as planned.

## 1. Goal and Scope
Goal: the public parser that validates the pattern, retrieves the page once under one deadline, reads, matches, maps and builds one dataset per usable matched station, reports every other matched station as a failure, and logs every notice, failure and omission with stable IDs — following the P5.07 parser pattern.
In scope: `IOsthessenWetterParser`/`OsthessenWetterParser` with `ParseAsync(string stationNamePattern, CancellationToken)` and the result of OW-08; pattern validation before any request; one deadline per parse from `OsthessenWetterOptions.PageDeadline` created from the injected `TimeProvider`; failure precedence caller cancellation, deadline, other; `retrieved_at` sampled once on successful return from the page source, before mapping and date inference, truncated to whole seconds and reused for every `InferDate` call and every dataset (OW-16; the deadline still starts before the retrieval); matching against overview names; collision check over all overview names (approved, OW-03); station failures `OSTHESSEN-STATION-OFFLINE`, `OSTHESSEN-STATION-METADATA-MISSING`, `OSTHESSEN-STATION-NO-READING`; failures `OSTHESSEN-PATTERN-NO-MATCH`, `OSTHESSEN-NO-USABLE-STATION`; dataset assembly through `IObservationDatasetBuilder` with provider `osthessenwetter`; logging through `ILogger<OsthessenWetterParser>`; registration.
Out of scope with owner: wire form → P6.08; README and package consumption → P6.09; CLI → P3; MQTT → P8.

## 2. Preconditions and Inputs
- P6.03 to P6.06 complete; P6.02 approved OW-08, OW-12, OW-13 and OW-16.
- Existing: `IObservationDatasetBuilder`, `ObservationDatasetRejectedException.OmittedReadings`, the `DwdObservationParser` pattern and its tests (`ComposedDwdObservationPipelineTests`), `CapturingLogger<T>` test helper.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Result types | `OsthessenWetterParseResult` (`Datasets` as `IReadOnlyList<ObservationDataset>`, `StationFailures` as `IReadOnlyList<OsthessenWetterStationFailure>` with `StationName`, `ErrorId`, `Reason`) (approved, OW-08) | Approved; the result type is a library type, not an export entity | User (OW-08) | S2 |
| Collision scope | All overview names (approved, OW-03) / the matched names (rejected) | All overview names, so a source ID never depends on the pattern | User (OW-03) | S2 |
| Order | Page order / name order | Page order of the overview table | User (OW-08) | S2 |
| Log content | Station name, error or notice ID, kind and reason, UTC, source; never values, page text or the endpoint query | As stated (T-P6-19) | Orchestrator, recorded | S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Observations/Osthessen/IOsthessenWetterParser.cs`, `OsthessenWetterParser.cs`, `OsthessenWetterParseResult.cs`, `OsthessenWetterStationFailure.cs`, `OsthessenWetterNoticeIds.cs` | Create | Parser, result and IDs |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration |
| `tests/WeatherParser.Parsing.Tests/Observations/Osthessen/OsthessenWetterParserTests.cs` | Create | Unit tests with strict mocks of the P6.03–P6.06 interfaces |
| `tests/WeatherParser.Parsing.Tests/Observations/Osthessen/ComposedOsthessenWetterPipelineTests.cs` | Create | Composed tests with only the HTTP handler mocked and synthetic fixtures |
| `docs/p6/P6_VERIFICATION.md` | Change | P6.07 section |

## 5. Procedure
Every slice: roles and checks as in P6.03 section 5.

### S1 Validation, deadline and precedence
**Red**: `ParseAsync_InvalidPattern_ThrowsBeforeRequest` (the strict source mock has no setup), `ParseAsync_DeadlineFromOptionsAndTimeProvider`, `ParseAsync_CallerCancelledDuringRetrieval_ThrowsOperationCanceled`, `ParseAsync_DeadlineDuringRetrieval_ThrowsDeadlineExceeded`, `ParseAsync_BothAtOnce_CallerCancellationWins`, `ParseAsync_PageUnavailable_Propagates` (no dataset, no fallback), `ParseAsync_FormatChanged_Propagates`, `ParseAsync_RetrievedAtSampledAfterPageSourceReturns` (the clock is read after the source returns, once, truncated to whole seconds), `ParseAsync_DelayedResponseAcrossMidnight_ReadingGetsResponseDate` (request starts at Berlin 23:59:55, the page source returns at 00:00:05 with a `00:00` display: the reading is dated on the response's local date, OW-16), and interruptions after retrieval with a deterministic `ManualTimeProvider` and cancellation sources: `ParseAsync_CallerCancelledDuringMapping_ThrowsOperationCanceled`, `ParseAsync_DeadlineDuringMapping_ThrowsDeadlineExceeded`, `ParseAsync_DeadlineBetweenStations_NoPartialResult`, `ParseAsync_DeadlineDuringBuild_WinsOverBuilderRejection`, `ParseAsync_CancellationAndDeadlineDuringBuild_CancellationWins`, `ParseAsync_DeadlineTimerDisposedOnEveryPath` (success, failure, cancellation), `ParseAsync_Composed_BodyStalledAfterHeaders_DeadlineExceeded` (composed pipeline with the real downloader and reader; the stalled body ends at `PageDeadline`), metadata validation at parser construction moved here from P6.06 S1 (T-P6-08, C-51; resolved through `AddWeatherParsing` with the consumer's invalid `OsthessenWetterOptions` and a strict HTTP handler without setup, so no request is made): `Construct_MetadataLatitudeOrLongitudeOutOfRange_ThrowsOptionsInvalid`, `Construct_MetadataDuplicateName_ThrowsOptionsInvalid`, `Construct_MetadataEmptyName_ThrowsOptionsInvalid`, `Construct_MetadataNullEntry_ThrowsOptionsInvalid`, `AddWeatherParsing_ResolvesOsthessenWetterParser`; `ServiceRegistrationCompletenessTests` red with its missing-registration assertion while the interface is unregistered.

**Green**: validation, deadline and precedence in the parser; singleton registration of the parser; S1 green before S2 starts. S2 and S3 keep both registration checks green and add no registration.

### S2 Station set and datasets
**Red**: `ParseAsync_OneMatch_OneDataset`, `ParseAsync_SeveralMatches_AllInPageOrder`, `ParseAsync_NoMatch_ThrowsPatternNoMatch`, `ParseAsync_OfflineStation_Failure`, `ParseAsync_OfflineRowWithValidValues_FailureNoDataset` (composed: an offline row whose cells hold valid values and a time is still a station failure `OSTHESSEN-STATION-OFFLINE`), `ParseAsync_OldOnlineDisplay_DatasetWithInferredTimes` (composed: no freshness filter; measurement times as inferred), `ParseAsync_MetadataMissing_Failure`, `ParseAsync_NoUsableReading_FailureNeverDataset`, `ParseAsync_MatchesButNoDataset_ThrowsNoUsableStationWithFailures`, `ParseAsync_MixedStations_AllDatasetsAndFailuresOneRequest` (usable stations before and after an offline and a metadata-missing station), `ParseAsync_Collision_ThrowsIdCollision`, `ParseAsync_CollisionOutsideMatchedSet_ThrowsIdCollision` (two colliding overview names of which only one matches the pattern: the parse fails, because the scope is all overview names, OW-03), `ParseAsync_Dataset_SourceLocationTimes` (`osthessenwetter:<slug>`, original name with umlauts, coordinates, altitude or none, `retrieved_at`, dataset `measured_at` the latest reading, `timestamp_quality` `estimated_date`), `ParseAsync_NeverFirstMatchOnly`.

**Green**: station set, failures and dataset assembly; S2 green before S3 starts.

### S3 Diagnostics
**Red**: `ParseAsync_Notices_LoggedWithIdUtcSourceStation` (wind table missing, wind row unmatched, value unreadable, time unresolved), `ParseAsync_Omissions_LoggedWithKindAndReason`, `ParseAsync_OmissionsLoggedBeforeRejection`, `ParseAsync_StationFailures_Logged`, `ParseAsync_Logs_NoValuesNoPageTextNoQuery` (a fixture whose other sections contain an error message with a host and a user name, and an endpoint with a query).

**Green**: logging.

**Refactor**: remove duplication with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P6 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P6-09 (parser level) | REQ-046, REQ-063 | Validation first, one deadline, precedence, no fallback |
| S2 | T-P6-10, T-P6-11, T-P6-07 (composed) | REQ-041, REQ-050, C-55, C-56 | Station set, failures, mixed parse, dataset values |
| S2 | T-P6-05 (composed) | REQ-042, REQ-056 | Markers and per-table times in the datasets |
| S3 | T-P6-19 | REQ-044, REQ-072, REQ-082 | Diagnostics content and sanitization |
| all | T-P6-13 (registration), T-P6-15 | REQ-078, REQ-085 | Registration; Red evidence |

## 7. Evidence to Record
- Red names and failures, counts, coverage, authorship, Codex rounds; the composed tests' request counts

## 8. Risks and Limitations
- (a) A consumer that needs only one station must still configure metadata for that station only; stations without metadata are failures, not errors of the whole parse (OW-04, OW-08)
- (b) Datasets of several stations share one `retrieved_at` but keep their own measurement times

## 9. Acceptance Criteria
- [ ] Every listed test green with recorded Red; mixed-station composed test green with exactly one request
- [ ] Never a dataset without usable readings; never a reduction to the first match
- [ ] Logs carry IDs and context and no values, page text or query strings
- [ ] Registration and architecture tests green; coverage gate passed; Codex 0 findings or reported
