# P4.07 – MOSMIX Location Catalog and Product Availability: Implementation Plan

**Status: plan only (2026-10-21); nothing is implemented or verified yet.** Refines the P4.07 row of section 5 and the "MOSMIX catalog" and "MOSMIX availability" rows of section 4 of [PHASE_P4_PLAN.md](../../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 4, 2, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0). Later: exclusion diagnostics marked as already logged (cross-plan review).

## 1. Goal and Scope

Goal: an injectable reader for the official MOSMIX location catalog in the format approved in the MOSMIX-catalog SC row (REQ-100–102), producing typed in-memory MOSMIX location records with their own string IDs, and an injectable per-product availability service that yields, for every location and for both products `MOSMIX_S` and `MOSMIX_L`, exactly one availability item with `AvailabilityState` `available`, `unavailable` or `unknown` and the verified DWD element names (C-23, C-24), test-first with synthetic fixtures.

In scope:
- Catalog reader (asynchronous, `CancellationToken`, encoding per SC, invariant culture, stream left open, failure semantics per SC, coordinate notation conversion with range checks, missing altitude representation)
- Availability derivation from the SC-approved evidence (location/product availability evidence and element-vocabulary evidence are distinguished: a product-wide element list establishes element names only, never availability for a location; without verified location evidence or an approved derivation the state is `unknown`)
- Deterministic order of element names (Recommended: as defined by the SC row, otherwise ordinal)
- Registration per slice
- Synthetic fixtures under `tests/WeatherParser.Parsing.Tests/Fixtures/Mosmix/` with no copied DWD rows

Out of scope with owner:
- Distance and filters → P4.04/P4.09; the empty-result notice and list assembly → P4.09 (the missing-altitude exclusion notice is written in this step, S2)
- HTTP retrieval → P4.08
- List assembly, ordering and export → P4.09
- MOSMIX forecast parsing, KML/KMZ and `pointforecast_dwd_extension_V1_0.xsd` → P7
- The CDC catalog → P4.05/P4.06

## 2. Preconditions and Inputs

- P4.02 approved the MOSMIX-catalog, MOSMIX-availability, catalog-failure-semantics, public-API and service-registration SC rows
- P4.04 delivered the `NotFilterable` handling that consumes a null altitude
- The orchestrating agent provides the real declarations (`MosmixProduct`, `AvailabilityState`, `MosmixProductAvailability`, `MosmixLocationList`, `MosmixLocationListEntry`, `ServiceCollectionExtensions`)
- The SC rows define the catalog format, coordinate notation and availability evidence

## 3. Decisions Required

| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Catalog format, coordinate notation and availability evidence | — | As approved in the MOSMIX-catalog and MOSMIX-availability SC rows; this step only implements them | User (P4.02) | S1–S4 |
| Interface and type names | Proposed `IMosmixLocationCatalogReader`/`MosmixLocationCatalogReader` with `ReadAsync(Stream catalog, CancellationToken cancellationToken)` returning Proposed `MosmixLocationCatalog` (records plus diagnostics) with Proposed `MosmixLocationRecord` (internal model, not an export entity); Proposed `IMosmixAvailabilityResolver`/`MosmixAvailabilityResolver` returning one `MosmixProductAvailability` per product per location / names from the approved public-API SC row | The approved SC row wins | User (P4.02) | S1–S4 |
| Rounding of converted coordinates | none (C-26) / fixed decimals | None; exact `decimal` conversion of the documented notation; tests derive expected values independently | Orchestrator, recorded | S2 |

## 4. Artifacts

| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Catalogs/IMosmixLocationCatalogReader.cs` | Create | Proposed interface |
| `src/WeatherParser.Parsing/Catalogs/MosmixLocationCatalogReader.cs` | Create | Proposed implementation |
| `src/WeatherParser.Parsing/Catalogs/MosmixLocationCatalog.cs` | Create | Proposed type |
| `src/WeatherParser.Parsing/Catalogs/MosmixLocationRecord.cs` | Create | Proposed type |
| `src/WeatherParser.Parsing/Catalogs/IMosmixAvailabilityResolver.cs` | Create | Proposed interface |
| `src/WeatherParser.Parsing/Catalogs/MosmixAvailabilityResolver.cs` | Create | Proposed implementation |
| `src/WeatherParser.Parsing/Catalogs/` | Create | Diagnostic/exception types required by the failure-semantics SC row (all Proposed) |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration in the slice that adds each interface |
| `tests/WeatherParser.Parsing.Tests/Catalogs/MosmixLocationCatalogReaderTests.cs` | Create | Tests for catalog reader |
| `tests/WeatherParser.Parsing.Tests/Catalogs/MosmixAvailabilityResolverTests.cs` | Create | Tests for availability resolver |
| `tests/WeatherParser.Parsing.Tests/Fixtures/Mosmix/*` | Create | Synthetic fixtures |
| `Directory.Packages.props`, `src/WeatherParser.Parsing/WeatherParser.Parsing.csproj`, affected `packages.lock.json` files, `DEPENDENCIES.md` | Change | Only if the logging SC row approves a new package |
| `tests/WeatherParser.Parsing.Tests/Architecture/ServiceRegistrationCompletenessTests.cs`, `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` (positive-resolution tests), `tests/WeatherParser.Parsing.Tests/Architecture/NoSharedStateTests.cs`, `tests/WeatherParser.Package.Tests/PackageServiceResolutionTests.cs`, `PackageSchemaDeliveryTests.cs`, `PackageSerializationTests.cs` | Change | Register logging in validated containers if the logger is a constructor dependency |

## 5. Procedure

### S1 Catalog records and IDs

**Red**
- `ReadAsync_ValidFixture_ReturnsAllLocations`
- `ReadAsync_KeepsAlphanumericAndLeadingZeroIds` (synthetic IDs such as `X123A`, `01234`, and an ID that equals a CDC-style ID like `00044` stays a MOSMIX ID string)
- `ReadAsync_PreservesUmlautsInNames`
- `ReadAsync_LeavesStreamOpenOnSuccess`
- `AddWeatherParsing_ResolvesMosmixLocationCatalogReader` (validated container, C-108)
- `ServiceRegistrationCompletenessTests` red when the interface appears (assertion failure on missing registration; behavior tests fail with `NotImplementedException`)

**Green**
- Parsing per SC
- Registration

**Refactor**
- Remove duplication with all tests green (Recommended: share line-reading helpers with the P4.05 reader only through an internal helper, never one switchable reader for both catalogs)

**Roles**
- qwen generates fixtures from the orchestrator's format description, tests first, then minimal implementation and refactoring
- The orchestrating agent writes briefs with the SC facts and the real declarations (`MosmixProduct`, `AvailabilityState`, `MosmixProductAvailability`, `MosmixLocationList`, `MosmixLocationListEntry`, `ServiceCollectionExtensions`), checks fixtures for synthetic provenance and independently derived expected values, runs each Red, build, tests and coverage, reviews the diff and records authorship

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S2 Coordinate and altitude normalization

**Red**
- `ReadAsync_ConvertsDocumentedNotationToDecimalDegrees` (independently derived expected values, no rounding)
- `ReadAsync_SouthAndWestValuesAreNegative` (if the notation encodes them)
- `ReadAsync_CoordinatesAtBoundsAccepted_AndJustOutsideRejected` (both sides of ±90/±180 if representable)
- `ReadAsync_NotationComponentLimits_FollowsFailureSemantics` (for the notation approved in P4.02: minute and second components just below 60 accepted, at 60 or negative rejected as malformed records before conversion, so for example 50 degrees 60 minutes never becomes 51 degrees)
- `ReadAsync_MissingAltitude_IsNullNotZero_WithDiagnostic`
- `SelectableLocations_ExcludeMissingAltitude_WithNotice` (the catalog result's selectable locations exclude a location with missing altitude, and one notice with the stable notice ID of the logging SC row is written through the approved logging abstraction, verified with a strict mock of that abstraction; the record stays visible in the diagnostics, marked as already logged so that P4.09 does not log it again)
- `ReadAsync_ZeroAltitude_IsZeroNotMissing`
- `ReadAsync_NegativeAltitude_Kept` (if representable)
- `ReadAsync_ParsesUnderGermanCulture` (culture restored)

Expected failure: S1 implementation passes raw notation through or treats missing altitude as 0; tests that already pass are recorded as existing behavior.

**Green**
- Conversion per SC
- Exclusion of locations with missing altitude and the notice through the approved logging abstraction
- Package preparation (only if the logging SC row approves a new package): version, license and .NET 10 compatibility check; central version and project reference; `dotnet restore WeatherParser.Ci.slnf --force-evaluate` once, lock diff reviewed, `DEPENDENCIES.md` updated; if the consumer's dependency graph changes: pack the library, refresh `tests/WeatherParser.Package.Tests`, run `dotnet restore tests/WeatherParser.Package.Tests --locked-mode` and the consumer tests while the refreshed hash is present, then restore the committed `WeatherParser.Parsing` `contentHash` and keep the reviewed structural lock changes (C-116); afterwards every restore uses `--locked-mode`
- If the logger is a constructor dependency: register logging (for example `services.AddLogging()`) in the validated containers of `tests/WeatherParser.Parsing.Tests/Architecture/ServiceRegistrationCompletenessTests.cs`, `tests/WeatherParser.Parsing.Tests/Architecture/NoSharedStateTests.cs`, the positive-resolution tests of `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs`, and `tests/WeatherParser.Package.Tests/PackageServiceResolutionTests.cs`, `PackageSchemaDeliveryTests.cs`, `PackageSerializationTests.cs`, in this slice before the reader's new dependency makes them fail; keep the separate missing-dependency failure tests

**Refactor**
- Remove duplication with all tests green

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S3 Malformed input

**Red**
- `ReadAsync_MalformedRecord_FollowsFailureSemantics` (line diagnostic)
- `ReadAsync_ValidInvalidValidRecords_FollowsFailureSemantics`
- `ReadAsync_AllRecordsInvalid_IsFormatFailure`
- `ReadAsync_DuplicateId_FollowsFailureSemantics`
- `ReadAsync_NumericOverflowOrDisallowedSeparator_FollowsFailureSemantics`
- `ReadAsync_EmptyStream_FollowsSemantics`
- `ReadAsync_CancelledBeforeAndDuringRead_ThrowsOperationCanceled`
- `ReadAsync_StreamThrowsDuringRead_PropagatesFailure`
- `ReadAsync_LeavesStreamOpenOnFailureAndCancellation`

Expected failure: earlier implementation returns partial data or unspecific exceptions; passing rejection tests recorded as existing behavior.

**Green**
- Failure semantics per SC

**Refactor**
- Remove duplication with all tests green

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S4 Per-product availability

**Red**
- `Resolve_ReturnsExactlyOneItemPerProduct` (`MOSMIX_S` and `MOSMIX_L`, no duplicates, deterministic product order)
- `Resolve_NoLocationEvidence_IsUnknownWithEmptyElements`
- `Resolve_ProductWideElementListWithoutLocationEvidence_IsUnknown` (a populated product-wide element list but no location evidence yields `unknown` with empty elements unless an approved derivation establishes coverage)
- `Resolve_AvailableProduct_ListsOnlyVerifiedElements` (element names exactly as in the SC evidence, deterministic order)
- `Resolve_UnavailableProduct_HasEmptyElements`
- `Resolve_ProductsAreIndependent` (evidence for `MOSMIX_L` never fills `MOSMIX_S` and vice versa)
- `Resolve_NeverUsesCdcData` (the resolver takes no CDC input; a MOSMIX ID equal to a CDC ID string gets only MOSMIX evidence)
- `Resolve_MissingUnreadableOrMalformedEvidence_FollowsFailureSemantics` (for each evidence source the SC design supports: a missing source, a stream that throws and a malformed record follow the approved failure semantics and are distinguishable from legitimate `unknown`; never silently converted into `unknown`)
- `Resolve_IsDeterministicForIdenticalInput`
- `Resolve_ResultPassesP2ListValidation` (a `MosmixLocationList` built with the items serializes with the real `IJsonExportSerializer` and `IXmlExportSerializer` without a C-23 validation error)
- `AddWeatherParsing_ResolvesMosmixAvailabilityResolver` (validated container); completeness test red when the interface appears

Expected failure: stub throws `NotImplementedException`; completeness assertion before registration.

**Green**
- Availability per SC
- Registration

**Refactor**
- Remove duplication with all tests green

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S5 Review and evidence

**No Red: review slice.**

Pre-commit coverage on the working tree per CONTRIBUTING.md section 5 (changed-file list from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard`; baseline via `write-baseline` from a `coverage.xml` of the integration target collected the same way in a separate `git worktree`; runner `evaluate`; native covered/total block counters, baseline counters and runner result; exit 0 before Codex). Then Codex reviews the uncommitted diff read-only; qwen fixes confirmed findings test-first and the orchestrating agent reruns build, tests and coverage; checkpoint commit when the user requests it; `pwsh ./scripts/ci-local.ps1 -Commit <commit-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings` on that commit; push and hosted CI record. Package consumer: pack the library and run the existing package-consumer tests against the freshly packed library per CONTRIBUTING.md section 4 before the Codex review.

## 6. Tests and Test IDs

| Step-local case | T-P4 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P4-08 | REQ-101, REQ-083 | Alphanumeric and leading-zero MOSMIX IDs as strings, never CDC IDs; umlauts; stream left open |
| S2 | T-P4-08 | REQ-102, REQ-082 | Documented notation to decimal degrees without rounding; bounds on both sides; missing altitude null with diagnostic, excluded from selectable locations with one notice, zero altitude kept; culture independence |
| S3 | T-P4-08 | REQ-102, REQ-083 | Malformed, mixed and all-invalid input per failure semantics; duplicates; overflow and separators; empty stream; cancellation; stream failure; stream ownership |
| S4 | T-P4-09 | REQ-101, REQ-102 | One item per product; `unknown` with empty elements without location evidence, also when a product-wide element list exists; evidence failures distinct from `unknown`; verified elements only when `available`; products independent; no CDC fallback; C-23 validation passes with real serializers |
| S1, S4 | T-P4-14 (partial) | REQ-016 | Services resolved through their interfaces; registration completeness (C-107) |
| S1–S4 | T-P4-15 | REQ-085 | Recorded Red → Green → Refactor evidence |

## 7. Evidence to Record

- SC rows covered by each test
- Fixture provenance and independently derived conversion values
- Red names and observed failures
- Existing-behavior records
- Commands
- Test counts
- Native block counters and runner result
- Codex review and dispositions
- Authorship

## 8. Risks and Limitations

- (a) The documented notation is ambiguous (for example degrees.minutes written like decimals) — conversion follows the SC row and boundary fixtures detect a decimal misreading
- (b) DWD may not publish per-location availability — `unknown` is the explicit outcome, never "all elements"
- (c) Confusion with CDC IDs — separate types, no CDC input to the resolver
- (d) A product-level element list may change between runs — the SC check date is recorded and P4.11's live check compares

## 9. Acceptance Criteria

- [ ] Each behavior increment has recorded Red evidence
- [ ] All T-P4-08 and T-P4-09 cases pass
- [ ] IDs never numeric or mapped to CDC
- [ ] Missing altitude never 0
- [ ] Exactly one availability item per product, `unknown` explicit, elements only when `available`
- [ ] No fallback between products or catalogs
- [ ] Services registered and completeness tests green
- [ ] Build without warnings
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user (AGENTS.md)
- [ ] No retrieval, distance or altitude-difference filtering, list assembly or forecast parsing code added (only the missing-altitude exclusion of S2)
