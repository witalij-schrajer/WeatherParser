# P4.06 – CDC Availability Derivation: Implementation Plan

**Status: plan only (2026-10-21); nothing is implemented or verified yet.** Refines the P4.06 row of section 5 and the "CDC availability" row of section 4 of [PHASE_P4_PLAN.md](../../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 3, 1, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0). Later: Codex-outage acceptance wording.

## 1. Goal and Scope
Goal: an injectable service that derives, per CDC station, the verified `available_measurements` (REQ-052 field names per C-21, for example `temperature_c`) and the station's suitability (active versus inactive) from the DWD product metadata approved in the CDC-availability SC row of `docs/p4/P4_SELECTION_CONTRACT.md` (REQ-024), and an injectable reader for that metadata if the SC row requires a separate metadata format, test-first with synthetic fixtures.

In scope:
- Metadata reader for the SC-approved metadata format from caller-supplied streams (asynchronous, `CancellationToken`, invariant culture, encoding per SC, stream left open, failure semantics per SC) — only if the SC row says the metadata has its own format; otherwise reuse of `ICdcStationCatalogReader` from P4.05 is recorded
- Mapping from SC-approved products to REQ-052 names
- Joining metadata to catalog records by string station ID (exact ordinal match, leading zeros significant — `00044` and `44` are different stations)
- Activity rule per SC (for example an end date before the reference time minus the SC tolerance makes the station inactive)
- Stations present in metadata but absent from the catalog and vice versa handled per SC
- Duplicate metadata rows per station and product handled per SC
- Registration per slice

Out of scope with owner:
- Reading the station catalog itself → P4.05
- HTTP retrieval of metadata → P4.08
- Filtering, distance, list assembly, ordering, the clock and logging → P4.04/P4.09
- MOSMIX availability → P4.07
- Observation data parsing → P5

## 2. Preconditions and Inputs
- P4.05 complete
- P4.02 approved the CDC-availability, catalog-failure-semantics, public-API and service-registration SC rows, including the product-to-REQ-052 mapping table and the activity rule

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Products, mapping and activity rule | — | As approved in the CDC-availability SC row; this step only implements it | User (P4.02) | S1–S3 |
| Interface and type names | Proposed `ICdcAvailabilityDeriver`/`CdcAvailabilityDeriver` with `Derive(CdcStationCatalog catalog, CdcProductMetadata metadata, DateTimeOffset referenceTimeUtc)` returning Proposed `CdcStationAvailability` items (station ID, ordered REQ-052 names, `IsActive`, diagnostics); Proposed `ICdcProductMetadataReader`/`CdcProductMetadataReader` only if the SC metadata format differs from the catalog format / names from the approved public-API SC row | The approved SC row wins | User (P4.02) | S1–S3 |
| Reference time validation | accept any `DateTimeOffset` / require offset zero (UTC) | Require offset zero; a non-UTC value throws `ArgumentException` with an English message (AGENTS.md UTC rule) | Orchestrator, recorded | S2 |
| Order of `available_measurements` | ordinal order of REQ-052 names / mapping-table order | Ordinal order, deterministic for identical input (REQ-068) | Orchestrator, recorded | S2 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Catalogs/ICdcAvailabilityDeriver.cs` | Create | Proposed interface |
| `src/WeatherParser.Parsing/Catalogs/CdcAvailabilityDeriver.cs` | Create | Proposed implementation |
| `src/WeatherParser.Parsing/Catalogs/CdcStationAvailability.cs` | Create | Proposed type |
| `src/WeatherParser.Parsing/Catalogs/CdcProductMetadata.cs` | Create | Proposed type |
| — | — | — |
| — | Only if SC requires separate format: | — |
| `src/WeatherParser.Parsing/Catalogs/ICdcProductMetadataReader.cs` | Create | Proposed interface |
| `src/WeatherParser.Parsing/Catalogs/CdcProductMetadataReader.cs` | Create | Proposed implementation |
| — | — | — |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration in the slice that adds each interface |
| `tests/WeatherParser.Parsing.Tests/Catalogs/CdcAvailabilityDeriverTests.cs` | Create | Tests for deriver |
| `tests/WeatherParser.Parsing.Tests/Catalogs/CdcProductMetadataReaderTests.cs` | Create | Tests for reader (if exists) |
| `tests/WeatherParser.Parsing.Tests/Fixtures/Cdc/` | Create | Synthetic fixtures |
| `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` | Extend | Registration tests |

## 5. Procedure
### S1 Metadata reader (conditional: only if the SC row defines a separate metadata format; otherwise record "reuse of `ICdcStationCatalogReader`, no S1" and start with S2)
**Red**
- `ReadAsync_ValidMetadataFixture_ReturnsEntries`
- `ReadAsync_KeepsLeadingZeroIds`
- `ReadAsync_DateFieldsParsedAsDocumented`
- `ReadAsync_MalformedEntry_FollowsFailureSemantics`
- `ReadAsync_AllEntriesInvalid_IsFormatFailure`
- `ReadAsync_CancelledBeforeAndDuringRead_ThrowsOperationCanceled`
- `ReadAsync_LeavesStreamOpenOnSuccessFailureAndCancellation`
- `ReadAsync_StreamThrowsDuringRead_PropagatesFailure`
- `AddWeatherParsing_ResolvesCdcProductMetadataReader` (validated container, C-108)
- `ServiceRegistrationCompletenessTests` red when the interface appears, before registration. Expected failures: behavior tests fail with `NotImplementedException` from the method stubs; the registration and completeness tests fail with their assertion on the missing registration (constructors stay free of throwing stubs); both are recorded before Green.

**Green**
- Reader per SC
- Registration

**Refactor**
- Remove duplication with all tests green.

**Roles**
- qwen generates fixtures from the orchestrator's format description, tests first, then minimal implementation and refactoring
- the orchestrating agent writes briefs with the SC facts and real declarations (`CdcStationCatalog`, `CdcStationRecord`, `ServiceCollectionExtensions`), checks fixtures for synthetic provenance, runs each Red, build, tests and coverage, reviews the diff and records authorship

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S2 Mapping and activity
**Red**
- `Derive_MapsApprovedProductsToReq052Names` (each SC mapping row yields its name)
- `Derive_UnmappedProduct_IsNotEmitted`
- `Derive_CatalogMembershipWithoutMetadata_YieldsNoMeasurements` (and follows the failure semantics; never claims quantities)
- `Derive_JoinsByExactStringId` (`00044` metadata does not attach to station `44`)
- `Derive_ActivityAtReferenceTimeBoundary` (end date exactly at, just before and just after the SC threshold: active, inactive, active as the SC rule defines)
- `Derive_InactiveStation_IsNotSuitable`
- `Derive_OrdersMeasurementsOrdinally`
- `Derive_IsDeterministicForIdenticalInput`
- `Derive_NonUtcReferenceTime_Throws`
- `AddWeatherParsing_ResolvesCdcAvailabilityDeriver` (validated container)
- `ServiceRegistrationCompletenessTests` red when the interface appears, before registration. Expected failures: behavior tests fail with `NotImplementedException` from the method stubs; the registration and completeness tests fail with their assertion on the missing registration (constructors stay free of throwing stubs); both are recorded before Green.

**Green**
- Mapping
- Join
- Activity rule
- Ordering
- Registration

**Refactor**
- Remove duplication with all tests green.

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S3 Missing and inconsistent metadata
**Red**
- `Derive_MissingMetadataForAllStations_FollowsFailureSemantics` (never an empty successful derivation)
- `Derive_EmptyParsedMetadata_FollowsFailureSemantics` (parsed metadata object without entries; never an empty successful derivation). Unreadable metadata streams are tested at the reader boundary in S1 (`ReadAsync_StreamThrowsDuringRead_PropagatesFailure`, also on the reuse path of `ICdcStationCatalogReader` from P4.05); HTTP retrieval failures belong to P4.08.
- `Derive_MetadataForUnknownStation_HandledAsDecided`
- `Derive_DuplicateMetadataRows_HandledAsDecided` (several rows for the same station and the same product, including conflicting activity periods, tested in both input orders: the result follows the SC duplicate policy and never depends on input order; distinct products are covered separately by `Derive_MixedCurrentAndExpiredProducts_FollowsActivityRule`)
- `Derive_MixedCurrentAndExpiredProducts_FollowsActivityRule` (one station with a current and an expired product, tested in both metadata orders: suitability and the emitted names follow the SC aggregation rule, independent of input order)
- `Derive_OnlyUnmappedProductCurrent_HandledAsDecided` (the only current product has no approved mapping: no names are emitted and suitability follows the SC rule)
- `Derive_MultipleProductsSameQuantity_NameListedOnce`
- Tests that already pass are recorded as existing behavior. Expected failure: S2 implementation returns empty or duplicated results.

**Green**
- Failure semantics and de-duplication per SC

**Refactor**
- Remove duplication with all tests green.

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S4 Review and evidence
**No Red: review slice.**

Pre-commit coverage (working tree): after build and all tests pass, run the CONTRIBUTING.md section 5 commands on the working tree (collection with `coverage.runsettings`, conversion with `dotnet tool run dotnet-coverage merge … -f xml`, changed-file list from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard`, baseline with the runner's `write-baseline` from a `coverage.xml` of the integration target collected the same way in a separate `git worktree` checkout, runner `evaluate`); record native covered/total block counters, baseline counters and runner result; runner exit 0 before the Codex review. Then Codex reviews the uncommitted diff read-only; qwen fixes confirmed findings test-first and the orchestrating agent reruns build, tests and coverage; checkpoint commit when the user requests it; `pwsh ./scripts/ci-local.ps1 -Commit <commit-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings` on that commit; push to the draft pull request and record the hosted CI run.

## 6. Tests and Test IDs
| Step-local case | T-P4 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P4-07 | REQ-024, REQ-083 | Metadata format per SC (if separate): IDs, dates, malformed and all-invalid input, cancellation, stream ownership |
| S2 | T-P4-07 | REQ-024, REQ-052, REQ-068 | Approved mapping only; membership no proof; exact string join; activity threshold on both sides; inactive not suitable; ordinal deterministic order; UTC reference time |
| S3 | T-P4-07 | REQ-024 | Missing or unreadable metadata per failure semantics, never an empty success; unknown stations and duplicates as decided; one name per quantity |
| S1, S2 | T-P4-14 (partial) | REQ-016 | Services resolved through their interfaces; registration completeness (C-107) |
| S1–S3 | T-P4-15 | REQ-085 | Recorded Red → Green → Refactor evidence |

## 7. Evidence to Record
- The SC mapping rows each test covers
- Fixture provenance
- Red names and observed failures
- Existing-behavior records
- Commands
- Test counts
- Native block counters and runner result
- Codex review and dispositions
- Authorship

## 8. Risks and Limitations
- (a) DWD metadata may describe products, not individual quantities — only SC-approved mappings are emitted, everything else stays absent
- (b) the activity threshold could hide stations with short reporting gaps — the SC rule and its boundary tests make the behavior explicit
- (c) joining on numeric IDs would merge different stations — exact ordinal string join is tested
- (d) stale metadata files — the P4.11 live check compares one real retrieval, limitations recorded

## 9. Acceptance Criteria
- [ ] Each behavior increment has recorded Red evidence
- [ ] All T-P4-07 cases pass
- [ ] No REQ-052 name without approved mapping
- [ ] No `unknown` or invented vocabulary in `available_measurements`
- [ ] Missing metadata never yields an empty success
- [ ] Inactive stations never marked suitable
- [ ] Services registered and completeness tests green
- [ ] Build without warnings
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user (AGENTS.md)
- [ ] No retrieval, filtering, export or logging code added
