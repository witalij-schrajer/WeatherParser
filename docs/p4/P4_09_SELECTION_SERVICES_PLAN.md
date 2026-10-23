# P4.09 – Selection Services and List Assembly: Implementation Plan

**Status: complete (2026-10-23): implemented (S1 to S4), committed as `f22bc7a`, hosted CI run 40010632025 green; records in [P4_VERIFICATION.md](P4_VERIFICATION.md) section 9.** Original planning note: plan only on 2026-10-21. **Reconciled with the approved selection contract (SC-11, SC-13, SC-17, SC-18, SC-22, SC-23) on 2026-10-21 (P4.02 S6).** Refines the P4.09 row of section 5 and the "Lists" and "Exports" rows of section 4 of [PHASE_P4_PLAN.md](../../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 3, 1, 1, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0). Later: availability deriver and resolver invoked by the selectors and diagnostics logging (cross-plan review); the orchestrating agent split one joined bullet line and replaced one test line itself after failed qwen attempts. S6 record (2026-10-21): qwen reconciled this plan from the orchestrating agent's change list and fixed the confirmed review findings section by section; the orchestrating agent edited these parts itself after failed qwen attempts, for regressions or for recorded design decisions: S1/S2 expected failures and registration tests; CDC clock lines and the MOSMIX completion-time test; section 6 cross-references; composed-pipeline fixture; MOSMIX_L 404 instead of `unknown` (cross-plan). Codex five-angle review rounds: 8, 6, 6, 3, 2, 2, 1, 0 findings; cross-plan five-angle review of all P4 plans: rounds 4, 3, 1, 1, 0 findings.

## 1. Goal and Scope
Goal: two injectable selection services — one for CDC stations, one for MOSMIX locations — that take validated `SearchParameters` (P4.03), obtain the catalog data through the P4.08 sources, evaluate every candidate with `ISearchAreaFilter` (P4.04), build the P2 list roots `CdcStationList` and `MosmixLocationList` with all required members, order entries through `IListEntryOrdering`, write the empty-result notice through the approved logging abstraction, and are proven with the real serializers and the v1 output schema.

In scope:
- `ICdcStationSelector`/`CdcStationSelector` and `IMosmixLocationSelector`/`MosmixLocationSelector` interfaces and implementations
- `SelectAsync` methods returning `CdcStationList` or `MosmixLocationList`
- Metadata assignment: `Source`, `SearchParameters`, `RetrievedAtUtc`
- Entry assembly with all required fields from catalog records, availability, distance and altitude difference
- Filtering with `ISearchAreaFilter` (outside candidates omitted silently, not-filterable excluded)
- Ordering through `IListEntryOrdering`
- Empty result handling: valid empty list plus exactly one empty-result notice with stable ID
- Failure propagation per catalog-failure semantics
- Serialization tests using real serializers and v1 schema validation

Out of scope:
- Coordinate parsing and search-parameter validation → P4.03
- Distance and filters → P4.04
- Catalog reading and availability → P4.05–P4.07
- HTTP → P4.08
- Integrated DI configuration examples, README usage and new package-consumer scenarios → P4.10
- Output modes, files, stdout, exit codes → P3
- MQTT → P8

## 2. Preconditions and Inputs
- P4.03–P4.08 complete
- P4.02 approved the public-API, logging, retrieval-time, source-literal and service-registration SC rows
- `CdcStationList`, `MosmixLocationList`, `CdcStationListEntry`, `MosmixLocationListEntry` types defined in P2
- `ISearchAreaFilter`, `IListEntryOrdering` interfaces from P4.04
- `ICdcCatalogSource`, `IMosmixCatalogSource` interfaces from P4.08
- `ICdcAvailabilityDeriver` (P4.06, takes the reference time as a parameter) and `IMosmixAvailabilityResolver` (P4.07)
- `IJsonExportSerializer`, `IXmlExportSerializer` interfaces from P2
- `TimeProvider` abstract class (BCL) registered by the consumer (for example `TimeProvider.System`) as decided in SC-13; the test containers already register it since P4.08 S5
- Approved logging abstraction and notice IDs from P4.02

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Interface and type names | Proposed `ICdcStationSelector`/`CdcStationSelector` and `IMosmixLocationSelector`/`MosmixLocationSelector` with `SelectAsync(SearchParameters parameters, CancellationToken cancellationToken)` returning `CdcStationList` or `MosmixLocationList` / names from the approved public-API SC row | `ICdcStationSelector`/`CdcStationSelector` and `IMosmixLocationSelector`/`MosmixLocationSelector` with `SelectAsync(SearchParameters parameters, CancellationToken cancellationToken)` returning `CdcStationList` or `MosmixLocationList` (SC-11, approved 2026-10-21) | User (P4.02, approved 2026-10-21) | S1–S3 |
| Time source | `TimeProvider` (BCL, no package) | `TimeProvider` (BCL, no package) (SC-13) | User (P4.02, approved 2026-10-21) | S1, S2 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Selection/ICdcStationSelector.cs` | Create | Interface with `Task<CdcStationList> SelectAsync(SearchParameters parameters, CancellationToken cancellationToken)` |
| `src/WeatherParser.Parsing/Selection/CdcStationSelector.cs` | Create | Implementation class with constructor injection of catalog source, filter, ordering, time source, logger |
| `src/WeatherParser.Parsing/Selection/IMosmixLocationSelector.cs` | Create | Interface with `Task<MosmixLocationList> SelectAsync(SearchParameters parameters, CancellationToken cancellationToken)` |
| `src/WeatherParser.Parsing/Selection/MosmixLocationSelector.cs` | Create | Implementation class with constructor injection of catalog source, filter, ordering, time source, logger |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration for both selectors in the slice that adds each interface |
| `tests/WeatherParser.Parsing.Tests/Selection/CdcStationSelectorTests.cs` | Create | Unit tests with strict mocks of dependencies, plus the composed CDC pipeline test |
| `tests/WeatherParser.Parsing.Tests/Selection/MosmixLocationSelectorTests.cs` | Create | Unit tests with strict mocks of dependencies |
| `tests/WeatherParser.Parsing.Tests/Selection/SelectionExportContractTests.cs` | Create | Tests for real serializer and schema validation |
| `tests/WeatherParser.Parsing.Tests/Architecture/ServiceRegistrationCompletenessTests.cs` | Change | Add new selectors to validated containers |
| `tests/WeatherParser.Parsing.Tests/Architecture/NoSharedStateTests.cs` | Change | Update if new dependencies are added |
| `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` | Change | Positive-resolution tests for new selectors |
| `tests/WeatherParser.Package.Tests/PackageServiceResolutionTests.cs` | Change | Update if new dependencies are added |
| `tests/WeatherParser.Package.Tests/PackageSchemaDeliveryTests.cs` | Change | Update if new dependencies are added |
| `tests/WeatherParser.Package.Tests/PackageSerializationTests.cs` | Change | Update if new dependencies are added |
| `docs/p4/P4_VERIFICATION.md` | Append | P4.09 section |

## 5. Procedure
### S1 CDC selection

**Red**
- `SelectAsync_NullOrUnassignedParameters_ThrowsSearchParameterInvalid` (null argument or unassigned member → `SearchParameterException` with ID `SEARCH-PARAMETER-INVALID`)
- `SelectAsync_SnapshotIndependence` (the selector copies the five values into a snapshot before any request; changing caller values do not affect the selection)
- `SelectAsync_SharedValidationPolicy` (the snapshot is validated with the factory’s policy and becomes the result's `SearchParameters`; boundary values are tested for both lower and upper limits)
- `SelectAsync_InvalidInputZeroRequests` (invalid input results in zero per-station requests; `SearchParameterException` with ID `SEARCH-PARAMETER-INVALID`; **Decided: zero interactions with all retrieval operations** (SC-11))
- `SelectAsync_InsideCandidates_BecomeEntriesWithAllFields` (every required member of `CdcStationListEntry` assigned from the record, the evaluation and the availability; leading-zero ID kept)
- `SelectAsync_ResultSerializesWithAllRequiredMembers` (the assembled `CdcStationList` serializes with the real `IJsonExportSerializer` without a C-27 failure; drives complete assembly test-first)
- `SelectAsync_OutsideCandidates_Omitted`
- `SelectAsync_NotFilterableCandidates_ExcludedWithNoticeAsDecided`
- `SelectAsync_InactiveStations_NeverEntries`
- `SelectAsync_DerivesAvailabilityWithSampledTime` (the time source is read once; `ICdcAvailabilityDeriver.Derive` is called once with that UTC value, and the same value, converted per the SC rule, becomes `RetrievedAtUtc`)
- `SelectAsync_LogsSourceDiagnosticsOnce` (each diagnostic in the source result is logged exactly once with its SC notice ID; a successful list is still returned under skip semantics)
- `SelectAsync_LogsDerivationDiagnosticsOnce` (diagnostics returned by `ICdcAvailabilityDeriver`, including those for excluded stations, are logged exactly once with their SC notice IDs)
- `SelectAsync_OrdersThroughListEntryOrdering` (the ordering service is called once with the filtered entries and its result is used unchanged)
- `SelectAsync_SetsMetadata` (`Source` literal `dwd:cdc-10min-now`, `SearchParameters` the same values, `RetrievedAtUtc` from the time source)
- `SelectAsync_ClockWithFractionalSeconds_ConvertedPerRule` (for example `2026-10-21T12:34:56.789Z` becomes the SC result, never an `ExportTimestamp` exception)
- `SelectAsync_NoMatches_EmptyListAndOneNotice` (no substitute; notice ID from the SC row)
- `SelectAsync_SourceFailure_PropagatesWithoutList` (retrieval, format and availability failures; no notice written as "empty")
- `ComposedCdcPipeline_RealSourceReadersDeriverAndSelector` (offline composition with the real P4.05/P4.06 readers and deriver, the P4.08 source, a mocked `HttpMessageHandler`, synthetic fixtures with one inactive station and one malformed skipped row, a fake time source and a capturing logger: the inactive station is not an entry, one malformed skipped row and one station whose altitude differs between two lists (a `CDC-STATION-CONFLICT` derivation diagnostic), the reader diagnostic and the derivation diagnostic are each logged once, `retrieved_at` follows the SC rule)
- `SelectAsync_PassesCancellationToken`
- `SelectAsync_IsDeterministicForIdenticalInput`
- `SelectAsync_CandidateCapBoundary_ExactlyMaxEvidenceCandidates_Passes` (the count is checked before any per-station request; exactly `MaxEvidenceCandidates` passes, one more fails with `SELECTION-TOO-MANY-CANDIDATES`)
- `SelectAsync_CandidatesExactlyAtCap_Proceeds` (exactly at the cap, no failure)
- `SelectAsync_CandidatesOneAboveCap_FailsBeforeAnyPerStationRequest` (one above the cap, fails before any per-station request with `SELECTION-TOO-MANY-CANDIDATES`)
- `SelectAsync_DeadlineExceededDuringInitialRetrieval_FailsWithSelectionDeadlineExceeded` (deadline exceeded during initial catalog retrieval fails the selection with `SELECTION-DEADLINE-EXCEEDED`)
- `SelectAsync_DeadlineExceededDuringPerStationRequests_FailsWithSelectionDeadlineExceeded` (deadline exceeded during per-station requests fails the selection with `SELECTION-DEADLINE-EXCEEDED`)
- `SelectAsync_ParallelismNeverAbove4_RequestsInParallel` (at most 4 per-station requests in parallel)
- `SelectAsync_ReferenceInstantReadOnce` (reference instant read once from `TimeProvider`, advancing fake clock verifies fixed reference during selection)
- `SelectAsync_ActivityDateFromReferenceInstant` (the UTC date of the reference instant is used for filtering activity, not a later time source read)
- `SelectAsync_PassesCancellationToken_WithSimultaneousCallerCancellationAndSelectionDeadlineExpiry_ThrowsOperationCanceledExceptionWithOriginalToken` (when both caller cancellation and selection deadline expiry occur, `OperationCanceledException` with the original token is thrown)

- `AddWeatherParsing_ResolvesCdcStationSelector` (validated container, C-108)
- `ServiceRegistrationCompletenessTests` red when `ICdcStationSelector` appears, before registration

Expected failure: behavior tests fail with `NotImplementedException` from the selector stub; the completeness test fails with its missing-registration assertion.

**Green**
- Assembly of `CdcStationList` with all required members
- Registration in `ServiceCollectionExtensions`
- Fixture updates for any new consumer-supplied dependency in the same slice
- Explicit sequence: catalog download → union/conflict/activity pre-filter → geographic/altitude filter → candidate cap → candidate-product evidence → derivation
- Deadline includes initial retrieval, cap equality passes, excess fails before evidence requests, and concurrency stays at four
- Reference instant is read once before any request; activity filtering uses the UTC date of that instant
- CDC entries carry no evidence timestamps (SC-23 applies to `MosmixProductAvailability` only); no clock reads besides the reference instant

**Refactor**
- Remove duplication with shared internal mapping helpers where CDC and MOSMIX semantics are identical; no switchable selector

**Roles**
- qwen generates tests first, then minimal implementation and refactoring
- orchestrating agent writes briefs, runs each Red, build, tests, schema validation and coverage, reviews the diff and records authorship

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S2 MOSMIX selection

**Red**
- `SelectAsync_MissingIndex_FailsBeforeFurtherRequests` (SC-22 index missing → `MOSMIX-S-EVIDENCE-MISSING` before any MOSMIX_L request)
- `SelectAsync_StaleIndex_NoticeOnceAndTimestampsReported` (SC-22 index older than `MosmixSEvidenceMaxAge` → used with one `MOSMIX-S-EVIDENCE-STALE` notice and timestamps in result)
- `SelectAsync_Mosmix_ReferenceInstantFixedAndEvidenceTimesFromCompletion` (advancing fake `TimeProvider`: `retrieved_at` is the reference instant read once before the first request; each MOSMIX_L item's `evidence_retrieved_at` is the clock value at its own download completion, truncated to whole seconds; MOSMIX_S items carry the index `downloaded_at`; SC-13)
- `SelectAsync_MosmixL404_IsUnavailable` (SC-05 MOSMIX_L option A, 404 → `unavailable`)
- `SelectAsync_CandidateCap_AppliesBeforeMosmixLRequests` (SC-18 `MaxEvidenceCandidates` → too many candidates before any MOSMIX_L request)
- `SelectAsync_InsideLocations_BecomeEntriesWithAllFields` (explicit expected values for `mosmix_id`, `name`, `latitude`, `longitude`, `altitude_m`, `distance_km`, `altitude_difference_m` and all five search parameters, using distinct coordinates, reference and location altitudes that differ, and positive, negative and zero altitude differences; one `MosmixProductAvailability` per product from P4.07, unchanged; alphanumeric and leading-zero IDs kept)
- `SelectAsync_ResultSerializesWithAllRequiredMembers` (the assembled `MosmixLocationList` serializes with the real `IJsonExportSerializer` without a C-27 failure)
- `SelectAsync_ResolvesProductAvailabilityForEachEntry` (`IMosmixAvailabilityResolver` is called for every inside location and its items are used unchanged)
- `SelectAsync_LogsSourceDiagnosticsOnce_ExceptAlreadyLogged` (other reader diagnostics are logged once; missing-altitude exclusion diagnostics marked as already logged by P4.07 are not logged again)
- `SelectAsync_NotFilterableLocations_Excluded` (the P4.07 notice is not duplicated)
- `SelectAsync_AllLocationsNotFilterable_ReturnsEmptyListWithOneEmptyResultNotice` (mocked source returns no selectable locations: a valid empty list and exactly one empty-result notice from the selector; the selector writes no exclusion notice)
- `SelectAsync_OutsideLocations_Omitted`
- `SelectAsync_OrdersThroughListEntryOrdering`
- `SelectAsync_SetsMetadata` (`Source` literal `dwd:mosmix-stationskatalog`, `SearchParameters` the same values, `RetrievedAtUtc` from the time source)
- `SelectAsync_ClockWithFractionalSeconds_ConvertedPerRule`
- `SelectAsync_NoMatches_EmptyListAndOneNotice`
- `SelectAsync_SourceFailure_PropagatesWithoutList`
- `SelectAsync_NeverUsesCdcSource` (no CDC dependency in the constructor)
- `SelectAsync_PassesCancellationToken`
- `SelectAsync_EvidenceTimestampsInExportedDocuments` (export tests with real serializers and the regenerated v1 schema include both members present and omitted)
- `SelectAsync_InvalidSearchParameters_NullOrUnassignedInput_ThrowsSearchParameterException` (SC-11 snapshot validation: null or unassigned member → `SearchParameterException`)
- `SelectAsync_InvalidSearchParameters_InvalidValues_ThrowsSearchParameterException` (SC-11 shared policy validation: invalid values in the snapshot → `SearchParameterException` with correct error ID `SEARCH-PARAMETER-INVALID`)
- `SelectAsync_SearchParametersAreCopiedAndImmutable` (SC-11 snapshot independence: selector uses a copy of the input parameters, not the caller's mutable reference)
- `SelectAsync_MosmixLRequestsRespectParallelismLimit` (SC-18 parallel requests: at most 4 MOSMIX_L requests in parallel)
- `SelectAsync_SelectionDeadlineIncludesAllOperations` (SC-18 deadline: includes catalog download, index read, filtering and evidence requests)
- `SelectAsync_StaleIndexAtMaxAge_ReportsNoNotice` (SC-22 strict greater-than comparison: index at exactly `MosmixSEvidenceMaxAge` is not stale)
- `SelectAsync_StaleIndexJustBeyondMaxAge_ReportsNotice` (SC-22 strict greater-than comparison: index just beyond `MosmixSEvidenceMaxAge` is stale)
- `SelectAsync_IndexReadBeforeCatalogDownload` (SC-22 index read before any request: catalog download only after successful index read)
- `SelectAsync_CandidateCap_EqualsLimit_Proceeds` (SC-18 `MaxEvidenceCandidates` = limit → exactly the number of candidates allowed, proceeds before any MOSMIX_L request)
- `SelectAsync_CandidateCap_ExceedsLimit_Fails` (SC-18 `MaxEvidenceCandidates` > limit → too many candidates, fails with `SELECTION-TOO-MANY-CANDIDATES` before any MOSMIX_L request)
- `SelectAsync_SelectionDeadlineAndCancellation_ThrowsOperationCanceledException` (SC-09 precedence: simultaneous caller cancellation and deadline expiry → `OperationCanceledException` carrying the caller's token)
- `SelectAsync_InvalidSearchParameters_NullInput_ThrowsSearchParameterException` (SC-11 null input → `SearchParameterException`)
- `SelectAsync_InvalidSearchParameters_UnassignedMember_ThrowsSearchParameterException` (SC-11 unassigned member → `SearchParameterException`)
- `SelectAsync_InvalidSearchParameters_InvalidAssignedValues_ThrowsSearchParameterException` (SC-11 invalid assigned values → `SearchParameterException` with correct error ID `SEARCH-PARAMETER-INVALID`)
- `SelectAsync_InvalidSearchParameters_ZeroRequests_ThrowsNoHttpCalls` (SC-11 zero HTTP requests for invalid input)

- `AddWeatherParsing_ResolvesMosmixLocationSelector` (validated container, C-108)
- `ServiceRegistrationCompletenessTests` red when `IMosmixLocationSelector` appears, before registration

Expected failure: behavior tests fail with `NotImplementedException` from the selector stub; the completeness test fails with its missing-registration assertion.

**Green**
- Assembly of `MosmixLocationList` with all required members
- Registration in `ServiceCollectionExtensions`
- Order reference instant → read the MOSMIX_S index (missing → `MOSMIX-S-EVIDENCE-MISSING` before any further request; stale → used with one `MOSMIX-S-EVIDENCE-STALE` notice) → catalog download → filter → candidate check (`SELECTION-TOO-MANY-CANDIDATES`) → MOSMIX_L files of in-area locations (at most 4 in parallel) → resolver (P4.07) → entries; entries carry `evidence_retrieved_at` and `evidence_issued_at` from the resolver
- Search parameters snapshot validation with shared policy before any request
- Deadline includes all operations: catalog download, index read, filtering and evidence requests
- Parallelism limited to 4 MOSMIX_L requests
- Reference instant is fixed for the whole selection; evidence timestamps follow their own completion times

**Refactor**
- Remove duplication with shared internal mapping helpers where CDC and MOSMIX semantics are identical; no switchable selector

**Roles**
- qwen generates tests first, then minimal implementation and refactoring
- orchestrating agent writes briefs, runs each Red, build, tests, schema validation and coverage, reviews the diff and records authorship

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S3 Export contract with real serializers

Tests resolve the selectors and the real serializers from a validated container with mocked catalog sources (or the real P4.08 sources with a mocked `HttpMessageHandler` and synthetic fixtures). S1 and S2 drive assembly test-first; the S3 tests are contract characterization tests: a test that passes on its first run is recorded as existing behavior, a failing one is a real Red that is fixed in the selectors.

**Red**
- `CdcSelection_SerializesToEquivalentJsonAndXml` (same values in both formats, UTF-8, culture independence under `de-DE`)
- `MosmixSelection_SerializesToEquivalentJsonAndXml`
- `Selections_XmlValidatesAgainstV1Schema` (with `ExportSchemaValidation`, C-104)
- `EmptySelections_SerializeAsValidEmptyLists` (both formats, schema-valid)
- `Selections_PreserveLeadingZeroIdsUmlautsAndEscaping` (synthetic names with `ä`, `&`, `<`)
- `Selections_AllRequiredMembersAssigned` (no C-27 serialization failure)
- `Selections_MosmixL404_UnavailableWithoutEvidenceTimestamps` (a MOSMIX_L file answering HTTP 404 gives `unavailable` with an empty element list and both evidence members omitted; `unknown` cannot arise from the selectors under SC-05 option A and is covered by the P4.07 export tests)
- `Selections_DeterministicDocuments` (identical input → byte-identical documents)
- `ComposedMosmixPipeline_AllLocationsWithoutAltitude_NoticeCountsByIdAndEmptyList` (offline composition with the real MOSMIX reader, source and selector, synthetic missing-altitude catalog, mocked `HttpMessageHandler` and one shared capturing logger: one exclusion notice per input location, exactly one empty-result notice by its distinct ID, a valid empty list, no failure)
- `ComposedMosmixPipeline_ExclusionAndOtherDiagnostic_EachLoggedOnce` (one location without altitude and one malformed skipped row: one exclusion notice from the reader and one diagnostic notice from the selector)
- `Selections_EvidenceTimestampsInExportedDocuments` (export tests with real serializers and the regenerated v1 schema include both members present and omitted)
- `Export_EvidenceTimestamps_PresentAndOmitted` (export tests with the real serializers and the regenerated v1 schema include `evidence_retrieved_at` and `evidence_issued_at` both present (evidence used) and omitted (MOSMIX_L HTTP 404, no evidence), as UTC `YYYY-MM-DDTHH:mm:ss` without offset; `source` literals `dwd:cdc-10min-now` and `dwd:mosmix-stationskatalog`; `retrieved_at` = the reference instant truncated to whole seconds)
- `Selections_CdcActivityDate_FromReferenceInstantUtcDate` (SC-13: the CDC activity date is derived from the UTC date of the reference instant)

**Green**
- Corrections in the selectors only; no export contract change (C-03)
- `Selections_CdcActivityDate_FromReferenceInstantUtcDate` is added to ensure that the CDC activity date is derived from the UTC date of the reference instant
- CDC has no evidence timestamp contract: no CDC completion-time clock reads (SC-13, SC-23)

**Refactor**
- Remove duplication with all tests green

**Roles**
- qwen generates tests first, then minimal implementation and refactoring
- orchestrating agent writes briefs, runs each Red, build, tests, schema validation and coverage, reviews the diff and records authorship

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S4 Review and evidence

**No Red: review slice.**

Pre-commit coverage on the working tree per CONTRIBUTING.md section 5 (changed-file list from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard`; baseline via `write-baseline` from a `coverage.xml` of the integration target collected the same way in a separate `git worktree`; runner `evaluate`; native covered/total block counters, baseline counters and runner result; exit 0 before Codex). Package consumer: pack the library and run the existing package-consumer tests against it per CONTRIBUTING.md section 4. Then Codex reviews the uncommitted diff read-only; qwen fixes confirmed findings test-first and the orchestrating agent reruns build, tests and coverage; checkpoint commit when the user requests it; `pwsh ./scripts/ci-local.ps1 -Commit <commit-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings` on that commit; push and hosted CI record.

## 6. Tests and Test IDs
| Step-local case | T-P4 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P4-11 | REQ-023, REQ-024, REQ-068 | CDC entries with all fields; outside omitted; not-filterable and inactive excluded; ordering service used; metadata; fractional-second clock; empty list with one notice; failures propagate; deterministic |
| S2 | T-P4-12 | REQ-101, REQ-102, REQ-068 | MOSMIX entries with one availability item per product; IDs kept; not-filterable excluded without duplicate notice; ordering; metadata; empty list with one empty-result notice; all-unfilterable case with separate notice counts; every entry field asserted; failures propagate; no CDC use |
| S3 | T-P4-13 | REQ-067, REQ-068, REQ-082 | Both lists through real JSON/XML serializers, equivalent, schema-valid, empty lists, leading zeros, umlauts, escaping, `unavailable` without evidence members for a MOSMIX_L 404, all required members, byte-identical for identical input |
| S1, S2 | T-P4-14 (partial) | REQ-016 | Selectors resolved through their interfaces; registration completeness (C-107) |
| S1–S3 | T-P4-15 | REQ-085 | Recorded Red → Green → Refactor evidence |
| S2 | T-P4-12 (cross-reference T-P4-19, owned by P4.07/P4.08) | REQ-101, REQ-102, REQ-068 | Missing index throws `MOSMIX-S-EVIDENCE-MISSING`; stale index older than `MosmixSEvidenceMaxAge` uses data and reports one `MOSMIX-S-EVIDENCE-STALE` notice with age and limit; evidence timestamps in result are present when derived from evidence; **zero warnings at max age, one warning just beyond** |
| S3 | T-P4-13 (cross-reference T-P4-20, owned by P4.07/P4.10) | REQ-067, REQ-068, REQ-079 | Exported lists include `evidence_retrieved_at` and `evidence_issued_at` as UTC timestamps in the C-06 format, with proper JSON and XML serialization; schema validates timestamp formats; empty or null values omitted correctly |

## 7. Evidence to Record
- SC rows covered by each test
- Red names and observed failures
- Existing-behavior records
- JSON and XML samples of a populated and an empty list (synthetic data) with schema-validation results
- Commands
- Test counts
- Native block counters and runner result
- Package-consumer result
- Codex review and dispositions
- Authorship

## 8. Risks and Limitations
- (a) A notice written for a failure as if the list were empty — failure and empty paths are tested separately
- (b) `retrieved_at` precision — the fractional-second clock test
- (c) Unassigned required members only detected at serialization — S3 serializes every assembled list
- (d) Duplicate notices between P4.07 and P4.09 — the MOSMIX test checks the logger call count
- (e) CDC/MOSMIX cross-use — separate constructors without the other catalog's dependencies

## 9. Acceptance Criteria
- [x] Each behavior increment has recorded Red evidence (the S3 contract and composed-pipeline tests are characterization tests recorded as existing behaviour, as section 5 S3 allows)
- [x] All T-P4-11 to T-P4-13 cases pass
- [x] Empty selections are valid lists with exactly one empty-result notice (approved notice ID) and no substitute; exclusion notices are written once per excluded location and never duplicated
- [x] Failures never yield lists
- [x] Documents equivalent and schema-valid in both formats
- [x] No export contract change
- [x] Selectors registered, DI fixtures updated, completeness tests green
- [x] Package-consumer tests pass against the packed library
- [x] Build without warnings
- [x] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user (AGENTS.md)
- [x] No CLI, file output or MQTT code added
