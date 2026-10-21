# P4.09 – Selection Services and List Assembly: Implementation Plan

**Status: plan only (2026-10-21); nothing is implemented or verified yet.** Refines the P4.09 row of section 5 and the "Lists" and "Exports" rows of section 4 of [PHASE_P4_PLAN.md](../../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 3, 1, 1, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0). Later: availability deriver and resolver invoked by the selectors and diagnostics logging (cross-plan review); the orchestrating agent split one joined bullet line and replaced one test line itself after failed qwen attempts.

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
- `TimeProvider` abstract class (BCL) or library interface as decided in P4.02 (if `TimeProvider` is chosen, tests subclass it and override `GetUtcNow()`)
- Approved logging abstraction and notice IDs from P4.02

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Interface and type names | Proposed `ICdcStationSelector`/`CdcStationSelector` and `IMosmixLocationSelector`/`MosmixLocationSelector` with `SelectAsync(SearchParameters parameters, CancellationToken cancellationToken)` returning `CdcStationList` or `MosmixLocationList` / names from the approved public-API SC row | The approved SC row wins | User (P4.02) | S1–S3 |
| Time source | `TimeProvider` (BCL, no package) / a library interface | As approved in the retrieval-time SC row; tests use a fake that returns a value with fractional seconds | User (P4.02) | S1 |
| Order of evaluation | filter every candidate, then order / order first | Filter, then order through `IListEntryOrdering`; the time is read once per selection, before retrieval or after it as the SC row decides, and the same value is used for the whole list | Orchestrator, recorded | S1, S2 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Selection/ICdcStationSelector.cs` | Create | Interface with `Task<CdcStationList> SelectAsync(SearchParameters parameters, CancellationToken cancellationToken)` |
| `src/WeatherParser.Parsing/Selection/CdcStationSelector.cs` | Create | Implementation class with constructor injection of catalog source, filter, ordering, time source, logger |
| `src/WeatherParser.Parsing/Selection/IMosmixLocationSelector.cs` | Create | Interface with `Task<MosmixLocationList> SelectAsync(SearchParameters parameters, CancellationToken cancellationToken)` |
| `src/WeatherParser.Parsing/Selection/MosmixLocationSelector.cs` | Create | Implementation class with constructor injection of catalog source, filter, ordering, time source, logger |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration for both selectors in the slice that adds each interface |
| `tests/WeatherParser.Parsing.Tests/Selection/CdcStationSelectorTests.cs` | Create | Unit tests with strict mocks of dependencies, plus the composed CDC pipeline test
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
- `SelectAsync_InsideCandidates_BecomeEntriesWithAllFields` (every required member of `CdcStationListEntry` assigned from the record, the evaluation and the availability; leading-zero ID kept)
- `SelectAsync_ResultSerializesWithAllRequiredMembers` (the assembled `CdcStationList` serializes with the real `IJsonExportSerializer` without a C-27 failure; drives complete assembly test-first)
- `SelectAsync_OutsideCandidates_Omitted`
- `SelectAsync_NotFilterableCandidates_ExcludedWithNoticeAsDecided`
- `SelectAsync_InactiveStations_NeverEntries`
- `SelectAsync_DerivesAvailabilityWithSampledTime` (the time source is read once; `ICdcAvailabilityDeriver.Derive` is called once with that UTC value, and the same value, converted per the SC rule, becomes `RetrievedAtUtc`)
- `SelectAsync_LogsSourceDiagnosticsOnce` (each diagnostic in the source result is logged exactly once with its SC notice ID; a successful list is still returned under skip semantics)
- `SelectAsync_LogsDerivationDiagnosticsOnce` (diagnostics returned by `ICdcAvailabilityDeriver`, including those for excluded stations, are logged exactly once with their SC notice IDs)
- `SelectAsync_OrdersThroughListEntryOrdering` (the ordering service is called once with the filtered entries and its result is used unchanged)
- `SelectAsync_SetsMetadata` (`Source` literal, `SearchParameters` the same values, `RetrievedAtUtc` from the time source)
- `SelectAsync_ClockWithFractionalSeconds_ConvertedPerRule` (for example `2026-10-21T12:34:56.789Z` becomes the SC result, never an `ExportTimestamp` exception)
- `SelectAsync_NoMatches_EmptyListAndOneNotice` (no substitute; notice ID from the SC row)
- `SelectAsync_SourceFailure_PropagatesWithoutList` (retrieval, format and availability failures; no notice written as "empty")
- `ComposedCdcPipeline_RealSourceReadersDeriverAndSelector` (offline composition with the real P4.05/P4.06 readers and deriver, the P4.08 source, a mocked `HttpMessageHandler`, synthetic fixtures with one inactive station and one malformed skipped row, a fake time source and a capturing logger: the inactive station is not an entry, one malformed skipped row and metadata for a station missing from the catalog (a derivation diagnostic), the reader diagnostic and the derivation diagnostic are each logged once, `retrieved_at` follows the SC rule)
- `SelectAsync_PassesCancellationToken`
- `SelectAsync_IsDeterministicForIdenticalInput`
- `AddWeatherParsing_ResolvesCdcStationSelector` (validated container, C-108)
- Completeness test red when the interface appears (assertion failure; behavior tests fail with `NotImplementedException`)

**Green**
- Assembly of `CdcStationList` with all required members
- Registration in `ServiceCollectionExtensions`
- Fixture updates for any new consumer-supplied dependency in the same slice

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
- `SelectAsync_InsideLocations_BecomeEntriesWithAllFields` (explicit expected values for `mosmix_id`, `name`, `latitude`, `longitude`, `altitude_m`, `distance_km`, `altitude_difference_m` and all five search parameters, using distinct coordinates, reference and location altitudes that differ, and positive, negative and zero altitude differences; one `MosmixProductAvailability` per product from P4.07, unchanged; alphanumeric and leading-zero IDs kept)
- `SelectAsync_ResultSerializesWithAllRequiredMembers` (the assembled `MosmixLocationList` serializes with the real `IJsonExportSerializer` without a C-27 failure)
- `SelectAsync_ResolvesProductAvailabilityForEachEntry` (`IMosmixAvailabilityResolver` is called for every inside location and its items are used unchanged)
- `SelectAsync_LogsSourceDiagnosticsOnce_ExceptAlreadyLogged` (other reader diagnostics are logged once; missing-altitude exclusion diagnostics marked as already logged by P4.07 are not logged again)
- `SelectAsync_NotFilterableLocations_Excluded` (the P4.07 notice is not duplicated)
- `SelectAsync_AllLocationsNotFilterable_ReturnsEmptyListWithOneEmptyResultNotice` (mocked source returns no selectable locations: a valid empty list and exactly one empty-result notice from the selector; the selector writes no exclusion notice)
- `SelectAsync_OutsideLocations_Omitted`
- `SelectAsync_OrdersThroughListEntryOrdering`
- `SelectAsync_SetsMetadata`
- `SelectAsync_ClockWithFractionalSeconds_ConvertedPerRule`
- `SelectAsync_NoMatches_EmptyListAndOneNotice`
- `SelectAsync_SourceFailure_PropagatesWithoutList`
- `SelectAsync_NeverUsesCdcSource` (no CDC dependency in the constructor)
- `SelectAsync_PassesCancellationToken`
- `AddWeatherParsing_ResolvesMosmixLocationSelector`
- Completeness test red when the interface appears

**Green**
- Assembly of `MosmixLocationList` with all required members
- Registration in `ServiceCollectionExtensions`

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
- `Selections_UnknownAvailabilitySerializedExplicitly` (`unknown` with an empty element list)
- `Selections_DeterministicDocuments` (identical input → byte-identical documents)
- `ComposedMosmixPipeline_AllLocationsWithoutAltitude_NoticeCountsByIdAndEmptyList` (offline composition with the real MOSMIX reader, source and selector, synthetic missing-altitude catalog, mocked `HttpMessageHandler` and one shared capturing logger: one exclusion notice per input location, exactly one empty-result notice by its distinct ID, a valid empty list, no failure)
- `ComposedMosmixPipeline_ExclusionAndOtherDiagnostic_EachLoggedOnce` (one location without altitude and one malformed skipped row: one exclusion notice from the reader and one diagnostic notice from the selector)

**Green**
- Corrections in the selectors only; no export contract change (C-03)

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
| S3 | T-P4-13 | REQ-067, REQ-068, REQ-082 | Both lists through real JSON/XML serializers, equivalent, schema-valid, empty lists, leading zeros, umlauts, escaping, `unknown` explicit, all required members, byte-identical for identical input |
| S1, S2 | T-P4-14 (partial) | REQ-016 | Selectors resolved through their interfaces; registration completeness (C-107) |
| S1–S3 | T-P4-15 | REQ-085 | Recorded Red → Green → Refactor evidence |

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
- [ ] Each behavior increment has recorded Red evidence
- [ ] All T-P4-11 to T-P4-13 cases pass
- [ ] Empty selections are valid lists with exactly one empty-result notice (approved notice ID) and no substitute; exclusion notices are written once per excluded location and never duplicated
- [ ] Failures never yield lists
- [ ] Documents equivalent and schema-valid in both formats
- [ ] No export contract change
- [ ] Selectors registered, DI fixtures updated, completeness tests green
- [ ] Package-consumer tests pass against the packed library
- [ ] Build without warnings
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user (AGENTS.md)
- [ ] No CLI, file output or MQTT code added
