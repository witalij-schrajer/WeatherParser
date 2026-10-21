# P4.07 – MOSMIX Location Catalog and Product Availability: Implementation Plan

**Status: plan only (2026-10-21); nothing is implemented or verified yet. Reconciled with the approved selection contract (SC-04, SC-05, SC-08, SC-20, SC-22, SC-23) on 2026-10-21 (P4.02 S6).** Refines the P4.07 row of section 5 and the "MOSMIX catalog" and "MOSMIX availability" rows of section 4 of [PHASE_P4_PLAN.md](../../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 4, 2, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0). Later: exclusion diagnostics marked as already logged (cross-plan review). S6 record (2026-10-21): qwen reconciled this plan from the orchestrating agent's change list and fixed the confirmed review findings section by section; the orchestrating agent edited these parts itself after failed qwen attempts, for regressions or for recorded design decisions: merge of a duplicated S3 slice; S2 lower elevation bound (twice, after qwen regressions) and negative-value tests; S0 commands; S4 issue-time, namespace, ignored-element, exact-limit and all-missing tests; S1 injectable options validator (orchestrator decision); section 3 stale-notice row; section 4 artifacts and exception creation; section 6 and 9 mappings; S6 resolver without logging. Codex five-angle review rounds: 10, 9, 8, 4, 4, 4, 2, 2, 1, 0 findings; cross-plan five-angle review of all P4 plans: rounds 4, 3, 1, 1, 0 findings.

## 1. Goal and Scope

Goal: an injectable reader for the official MOSMIX location catalog in the format approved in the MOSMIX-catalog SC row (REQ-100–102), producing typed in-memory MOSMIX location records with their own string IDs, and an injectable per-product availability service that yields, for every location and for both products `MOSMIX_S` and `MOSMIX_L`, exactly one availability item with `AvailabilityState` `available`, `unavailable` or `unknown` and the verified DWD element names (C-23, C-24), test-first with synthetic fixtures.

In scope:
- Catalog reader (asynchronous, `CancellationToken`, encoding per SC, invariant culture, stream left open, failure semantics per SC, coordinate notation conversion with range checks, missing altitude representation)
- Availability derivation from the SC-approved evidence (location/product availability evidence and element-vocabulary evidence are distinguished: a product-wide element list establishes element names only, never availability for a location; without verified location evidence or an approved derivation the state is `unknown`)
- Deterministic order of element names (as defined by the SC row, otherwise ordinal)
- Registration per slice
- Synthetic fixtures under `tests/WeatherParser.Parsing.Tests/Fixtures/Mosmix/` with no copied DWD rows
- Secure handling of KML/KMZ and `pointforecast_dwd_extension_V1_0.xsd` for evidence files (SC-20), including structural checks, namespace validation, and streaming limits (SC-09)

Out of scope with owner:
- Distance and filters → P4.04/P4.09; the empty-result notice and list assembly → P4.09 (the missing-altitude exclusion notice is written in this step, S2)
- HTTP retrieval → P4.08
- List assembly, ordering and export → P4.09
- MOSMIX forecast parsing → P7
- The CDC catalog → P4.05/P4.06

## 2. Preconditions and Inputs

- P4.02 approved the MOSMIX-catalog, MOSMIX-availability, catalog-failure-semantics, public-API and service-registration SC rows
- P4.04 delivered the `NotFilterable` handling that consumes a null altitude
- The orchestrating agent provides the real declarations (`MosmixProduct`, `AvailabilityState`, `MosmixProductAvailability`, `MosmixLocationList`, `MosmixLocationListEntry`, `ServiceCollectionExtensions`)
- The SC rows define the catalog format, coordinate notation and availability evidence

## 3. Decisions Required

| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Catalog format, coordinate notation and availability evidence | — | As approved in the MOSMIX-catalog SC row (SC-04); this step only implements them | User (P4.02, approved 2026-10-21) | S1–S4 |
| Interface and type names | Proposed `IMosmixLocationCatalogReader`/`MosmixLocationCatalogReader` with `ReadAsync(Stream catalog, CancellationToken cancellationToken)` returning Proposed `MosmixLocationCatalog` (records plus diagnostics) with Proposed `MosmixLocationRecord` (internal model, not an export entity); Proposed `IMosmixAvailabilityResolver`/`MosmixAvailabilityResolver` returning one `MosmixProductAvailability` per product per location / names from the approved public-API SC row | The approved SC row wins | User (P4.02, approved 2026-10-21) | S1–S4 |
| Rounding of converted coordinates | none (C-26) / fixed decimals | None; exact `decimal` conversion of the documented notation; tests derive expected values independently | Orchestrator, recorded | S2 |
| Stale-index notice and resolver inputs | — | The resolver takes the reference instant, the MOSMIX_S index and the per-location MOSMIX_L evidence and returns items with evidence timestamps; it does not log. The P4.09 selector reads the index once per selection, fails with `MOSMIX-S-EVIDENCE-MISSING` before any request when it is unusable and writes the single `MOSMIX-S-EVIDENCE-STALE` notice (SC-22) | Orchestrator, recorded | S6 |
| MOSMIX_L evidence | Option A: per-location requests from `single_stations/` directory with HTTP 404 → `unavailable`; otherwise file read under SC-20 → `available` with elements whose series contain at least one numeric value; a valid file without such elements → `unavailable`. Option B: cache mechanism as MOSMIX_S (SC-22) with the all-stations file (85.7 MB, section 3.3), no per-location requests. Option C: `unknown` for every location | Decided: Option A (SC-05) | User (P4.02, approved 2026-10-21) | S1–S4 |
| Logging of notices | `Microsoft.Extensions.Logging.Abstractions` 10.0.13 (latest stable on 2026-10-21; licence MIT, a `net10.0` dependency group and the dependency `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.13, per the package's nuspec in section 2; recheck at adoption). Services take `ILogger<T>`; the consumer registers logging | Required (SC-08) | User (P4.02, approved 2026-10-21) | S1–S4 |
| Evidence file format for MOSMIX_L | As defined in SC-20 | Required (SC-20) | User (P4.02, approved 2026-10-21) | S1–S4 |
| MOSMIX_S evidence cache store | As defined in SC-22 | Required (SC-22) | User (P4.02, approved 2026-10-21) | S1–S4 |
| Export members for evidence timestamps | As defined in SC-23 | Required (SC-23) | User (P4.02, approved 2026-10-21) | S1–S4 |

## 4. Artifacts

| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Catalogs/IMosmixLocationCatalogReader.cs` | Create | Interface for catalog reader |
| `src/WeatherParser.Parsing/Catalogs/MosmixLocationCatalogReader.cs` | Create | Implementation of catalog reader |
| `src/WeatherParser.Parsing/Catalogs/MosmixLocationCatalog.cs` | Create | Type for catalog result |
| `src/WeatherParser.Parsing/Catalogs/MosmixLocationRecord.cs` | Create | Type for location record |
| `src/WeatherParser.Parsing/Catalogs/IMosmixAvailabilityResolver.cs` | Create | Interface for availability resolver |
| `src/WeatherParser.Parsing/Catalogs/MosmixAvailabilityResolver.cs` | Create | Implementation of availability resolver |
| `src/WeatherParser.Parsing/Exceptions/SelectionLimitException.cs`, `src/WeatherParser.Parsing/Exceptions/EvidenceCacheException.cs` | Create | Public exceptions with read-only `ErrorId` (SC-17), first thrown by the S1 validator; `CatalogFormatException` and `CatalogDiagnostic` are reused from P4.05 |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration in the slice that adds each interface |
| `tests/WeatherParser.Parsing.Tests/Catalogs/MosmixLocationCatalogReaderTests.cs` | Create | Tests for catalog reader |
| `tests/WeatherParser.Parsing.Tests/Catalogs/MosmixAvailabilityResolverTests.cs` | Create | Tests for availability resolver |
| `tests/WeatherParser.Parsing.Tests/Fixtures/Mosmix/*` | Create | Synthetic fixtures |
| `Directory.Packages.props`, `src/WeatherParser.Parsing/WeatherParser.Parsing.csproj`, affected `packages.lock.json` files, `DEPENDENCIES.md` | Change | Add `Microsoft.Extensions.Logging.Abstractions` 10.0.13 and `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.13 and `Microsoft.Extensions.DependencyInjection` 10.0.13 (SC-08) |
| `tests/WeatherParser.Parsing.Tests/Architecture/ServiceRegistrationCompletenessTests.cs`, `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` (positive-resolution tests), `tests/WeatherParser.Parsing.Tests/Architecture/NoSharedStateTests.cs`, `tests/WeatherParser.Package.Tests/PackageServiceResolutionTests.cs`, `PackageSchemaDeliveryTests.cs`, `PackageSerializationTests.cs` | Change | Register logging in validated containers if the logger is a constructor dependency |
| `src/WeatherParser.Parsing/Export/MosmixProductAvailability.cs` | Change | Add `EvidenceRetrievedAtWire`/`EvidenceRetrievedAtUtc` and `EvidenceIssuedAtWire`/`EvidenceIssuedAtUtc` as optional members appended after `available_forecast_elements` (C-117 to C-119) |
| `tools/WeatherParser.SchemaGeneration/ExportSchemaRefiner.cs` | Change | Extend so a `DateTime?` companion is recognized like `DateTime` (C-119) |
| `schemas/export/v1/weatherparser-export-v1.xsd` | Regenerate | With the new members appended (C-118) |
| `src/WeatherParser.Parsing/Evidence/MosmixL/IMosmixLEvidenceReader.cs` | Create | Interface for MOSMIX_L evidence reader |
| `src/WeatherParser.Parsing/Evidence/MosmixL/MosmixLEvidenceReader.cs` | Create | Implementation of MOSMIX_L evidence reader |
| `tests/WeatherParser.Parsing.Tests/Evidence/MosmixL/MosmixLEvidenceReaderTests.cs` | Create | Tests for MOSMIX_L evidence reader |
| `src/WeatherParser.Parsing/Evidence/MosmixS/IMosmixEvidenceCache.cs` | Create | Interface for MOSMIX_S evidence cache |
| `src/WeatherParser.Parsing/Evidence/MosmixS/FileSystemMosmixEvidenceCache.cs` | Create | File-system implementation of MOSMIX_S evidence cache |
| `tests/WeatherParser.Parsing.Tests/Evidence/MosmixS/FileSystemMosmixEvidenceCacheTests.cs` | Create | Tests for MOSMIX_S evidence cache |
| `src/WeatherParser.Parsing/Evidence/MosmixS/MosmixCatalogEndpoints.cs` | Create | Type for MOSMIX_S catalog endpoints configuration |
| `tests/WeatherParser.Parsing.Tests/Fixtures/MosmixS/*` | Create | Synthetic fixtures for MOSMIX_S evidence cache |
| `src/WeatherParser.Parsing/Evidence/MosmixS/IMosmixCatalogEndpointsValidator.cs`, `src/WeatherParser.Parsing/Evidence/MosmixS/MosmixCatalogEndpointsValidator.cs` | Create | Injectable options validation, registered by `AddWeatherParsing` (S1) |
| `tests/WeatherParser.Parsing.Tests/Catalogs/MosmixCatalogEndpointsValidatorTests.cs` | Create | Validation boundary matrix of SC-12, SC-18 and SC-22 (S1) |

## 5. Procedure

### S0 Export members C-117 to C-119

**Red**
- `ExportEntitySerializableTests` with the members present and omitted (real JSON and XML serializers)
- `ExportSchemaGenerationTests` for the optional timestamp
- Schema validation rejecting an offset, a `Z` and fractional seconds
- **Test `ExportEntitySerializableTests` rejects non-UTC companion values** — Expected failure: `DateTimeKind` is not `Utc`
- **Test `ExportEntitySerializableTests` rejects fractional seconds in companion values** — Expected failure: `DateTime` has sub-second precision
- **Test `ExportEntitySerializableTests` enforces paired timestamp presence** — Expected failure: if one evidence timestamp is present, the other must also be present

**Green**
- Add `EvidenceRetrievedAtWire`/`EvidenceRetrievedAtUtc` and `EvidenceIssuedAtWire`/`EvidenceIssuedAtUtc` to `MosmixProductAvailability.cs`
- Extend `ExportSchemaRefiner.cs` so a `DateTime?` companion is recognized like `DateTime`
- Regenerate `schemas/export/v1/weatherparser-export-v1.xsd`

**Refactor**
- Remove duplication with all tests green

**Roles**
- qwen generates tests and code
- The orchestrating agent writes briefs, runs Red, builds, tests and coverage, reviews and records authorship

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`
- `dotnet test tests/WeatherParser.SchemaGeneration.Tests/WeatherParser.SchemaGeneration.Tests.csproj -c Release --no-build`

### S1 Catalog records and IDs

**Red**
- `ReadAsync_ValidFixture_ReturnsAllLocations`
- `ReadAsync_KeepsAlphanumericAndLeadingZeroIds` (synthetic IDs such as `X123A`, `01234`, and an ID that equals a CDC-style ID like `00044` stays a MOSMIX ID string)
- `ReadAsync_PreservesUmlautsInNames`
- `ReadAsync_LeavesStreamOpenOnSuccess`
- `AddWeatherParsing_ResolvesMosmixLocationCatalogReader` (validated container, C-108)
- `ServiceRegistrationCompletenessTests` red when the interface appears (assertion failure on missing registration; behavior tests fail with `NotImplementedException`)
- `ReadAsync_HeaderTokensReordered_IsFormatFailure`
- `ReadAsync_FourCharacterIdWithLeadingSpace_Accepted`
- `ReadAsync_IdNotMatchingGrammar_Skipped`
- `Validate_Defaults_Accepted` (`MaxEvidenceCandidates` 100, `SelectionDeadline` 120 s and `MosmixSRefreshDeadline` 10 minutes when unset; required values set)
- `Validate_MaxEvidenceCandidates_Boundaries` (0 and 10001 → `SelectionLimitException` `SELECTION-LIMIT-INVALID`; 1 and 10000 accepted)
- `Validate_SelectionDeadline_Boundaries` (zero and 1 h plus one tick rejected with `SELECTION-LIMIT-INVALID`; one tick and exactly 1 h accepted)
- `Validate_MosmixSEvidenceMaxAge_Boundaries` (missing, 1 minute minus one tick and 365 days plus one tick → `EvidenceCacheException` `EVIDENCE-CACHE-SETTINGS-INVALID`; exactly 1 minute and 365 days accepted)
- `Validate_MosmixSRefreshDeadline_Boundaries` (zero and 1 h plus one tick rejected with `EVIDENCE-CACHE-SETTINGS-INVALID`; exactly 1 h accepted)
- `Validate_MosmixSAllStationsUriAndCacheDirectory_Invalid` (relative or non-HTTP(S) file URI, relative cache path → `EVIDENCE-CACHE-SETTINGS-INVALID`)
- `Validate_DirectoryAndCatalogUriRules` (directory URI without trailing `/`, with query, with fragment or not HTTP(S) → `ParserEndpointValidationException`; catalog URI with a query accepted unchanged)
- `Validate_NoNetworkOrFileSystemAccess` (a cache directory that does not exist is accepted at validation; strict mocks show no other interaction)
- `AddWeatherParsing_ResolvesMosmixCatalogEndpointsValidator` (validated container, C-108)

**Green**
- ISO-8859-1, LF; line 1 split on whitespace must be exactly the tokens `ID`, `ICAO`, `NAME`, `LAT`, `LON`, `ELEV` in this order (padding may vary) and line 2 must consist of dashes and spaces, otherwise format failure
- Per record: ID = characters 0–4 trimmed, matching `^[A-Z0-9]{4,5}$` (string; leading zeros kept); character 5 a space; ICAO characters 6–9 ignored; name = characters 11–30 trimmed, non-empty; the rest of the line (from character 31) is split on whitespace into exactly three tokens (latitude, longitude, elevation) or exactly two tokens (latitude, longitude; no elevation); any other count is malformed
- `MosmixCatalogEndpoints` is a plain options type with the SC-12, SC-18 and SC-22 settings and their defaults; it holds no logic. Validation lives in the injectable `IMosmixCatalogEndpointsValidator`/`MosmixCatalogEndpointsValidator` (`Validate(MosmixCatalogEndpoints options)`, registered as a singleton by `AddWeatherParsing`, C-107), which uses the existing `IParserEndpointValidator` for URIs; every service that takes the options (in P4.07 the index store and the resolver, in P4.08 the catalog source and the refresher) calls it in its constructor, so an invalid configuration fails when such a service is constructed (first resolution), not at `ValidateOnBuild`, without network or file-system access
- Directory URIs must be absolute HTTP(S) with a path ending in `/`, no query and no fragment; list and catalog endpoints are absolute HTTP(S) URIs used unchanged (a query is allowed); URI checks go through the existing `IParserEndpointValidator`
- Invalid `MaxEvidenceCandidates` (1 to 10000, default 100) or `SelectionDeadline` (> 0 and ≤ 1 h, default 120 s) → `SelectionLimitException` `SELECTION-LIMIT-INVALID`
- Invalid `MosmixSAllStationsUri` (absolute HTTP(S) file URI), `EvidenceCacheDirectory` (absolute local path), missing or out-of-range `MosmixSEvidenceMaxAge` (1 minute to 365 days, no default) or `MosmixSRefreshDeadline` (> 0 and ≤ 1 h, default 10 minutes) → `EvidenceCacheException` `EVIDENCE-CACHE-SETTINGS-INVALID` (SC-22)
- Invalid catalog or directory URIs → `ParserEndpointValidationException` (its additive `ErrorId` follows in P4.08)
- Tests register `MosmixCatalogEndpoints` as the consumer would (SC-12) in their validated containers (C-108); `CdcCatalogEndpoints` belongs to P4.08

**Refactor**
- Remove duplication with all tests green (Recommended: share line-reading helpers with the P4.05 reader only through an internal helper, never one switchable reader for both catalogs)
- Endpoint validation reuses the existing `IParserEndpointValidator`/`ParserEndpointValidator`; limit and cache-setting checks stay private to the options validation; no second implementation of a public service interface (C-107)

**Roles**
- qwen generates fixtures from the orchestrating agent's format description, tests first, then minimal implementation and refactoring
- The orchestrating agent writes briefs with the SC facts and the real declarations (`MosmixProduct`, `AvailabilityState`, `MosmixProductAvailability`, `MosmixLocationList`, `MosmixLocationListEntry`, `ServiceCollectionExtensions`), checks fixtures for synthetic provenance and independently derived expected values, runs each Red, build, tests and coverage, reviews the diff and records authorship

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S2 Coordinate and altitude normalization

**Red**
- `ReadAsync_ConvertsDegreesMinutesToDecimalDegrees` (independently derived expected values, no rounding)
- `ReadAsync_NegativeTokens_AreSouthAndWest` (a leading `-` on latitude or longitude gives a negative decimal value, SC-04)
- `ReadAsync_MinuteSixty_Skipped` (a `GORI`-like line with minute 60)
- `ReadAsync_MinuteFiftyNine_Accepted`
- `ReadAsync_ElevationBounds_BothSides` (−501 and 9001 skipped, −500 and 9000 accepted)
- `ReadAsync_CoordinatesAtBoundsAccepted_AndJustOutsideRejected` (`90.00`/`-90.00` and `180.00`/`-180.00` accepted; `90.01` and `180.01` skipped as out of range, SC-04)
- `ReadAsync_MinuteComponentLimits_Skipped` (SC-04 degrees-and-minutes notation: minutes `59` accepted, `60` skipped with `CATALOG-RECORD-SKIPPED` before conversion, so `50.60` never becomes 51 degrees; a token with more or fewer than two minute digits does not match `^-?\d{1,3}\.\d{2}$` and is skipped)
- `ReadAsync_MissingAltitude_IsNullNotZero_WithDiagnostic`
- `ReadAsync_ZeroAltitude_IsZeroNotMissing`
- `ReadAsync_NegativeAltitude_Kept` (for example `-350`, within −500..9000, SC-04)
- `ReadAsync_ParsesUnderGermanCulture` (culture restored)
- `ReadAsync_TwoTokenLine_ExcludedWithNoAltitudeNotice` (the catalog result's selectable locations exclude a location with missing altitude, and one notice with the stable notice ID of the logging SC row is written through the approved logging abstraction, verified with a strict mock of that abstraction; the record stays visible in the diagnostics, marked as already logged so that P4.09 does not log it again)
- `ReadAsync_AllRecordsWithoutAltitude_IsNotFormatFailure` (every record syntactically valid but without elevation: no exception, empty selectable result, one `MOSMIX-LOCATION-NO-ALTITUDE` notice per location; SC-10 counts syntactically valid records before exclusions)

Expected failure: S1 implementation passes raw notation through or treats missing altitude as 0; tests that already pass are recorded as existing behavior.

**Green**
- Conversion per SC-04
- Elevation grammar and range validation per SC-04
- Exclusion of locations with missing altitude and the notice `MOSMIX-LOCATION-NO-ALTITUDE` through the approved logging abstraction
- Package preparation (unconditional): version, license and .NET 10 compatibility check; central version and project reference; `dotnet restore WeatherParser.Ci.slnf --force-evaluate` once, lock diff reviewed, `DEPENDENCIES.md` updated; if the consumer's dependency graph changes: pack the library, refresh `tests/WeatherParser.Package.Tests`, run `dotnet restore tests/WeatherParser.Package.Tests --locked-mode` and the consumer tests while the refreshed hash is present, then restore the committed `WeatherParser.Parsing` `contentHash` and keep the reviewed structural lock changes (C-116); afterwards every restore uses `--locked-mode`
- Register logging (for example `services.AddLogging()`) in the validated containers of `tests/WeatherParser.Parsing.Tests/Architecture/ServiceRegistrationCompletenessTests.cs`, `tests/WeatherParser.Parsing.Tests/Architecture/NoSharedStateTests.cs`, the positive-resolution tests of `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs`, and `tests/WeatherParser.Package.Tests/PackageServiceResolutionTests.cs`, `PackageSchemaDeliveryTests.cs`, `PackageSerializationTests.cs`, in this slice before the reader's new dependency makes them fail; keep the separate missing-dependency failure tests

**Refactor**
- Remove duplication with all tests green

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S3 Malformed input

**Red**
- `ReadAsync_MalformedRecord_SkippedWithDiagnostic` (line diagnostic, `CATALOG-RECORD-SKIPPED`)
- `ReadAsync_ValidInvalidValidRecords_SkippedWithDiagnostic`
- `ReadAsync_AllRecordsInvalid_IsFormatFailure`
- `ReadAsync_DuplicateId_ExcludesAllWithDiagnostic` (`CATALOG-RECORD-SKIPPED`)
- `ReadAsync_NumericOverflowOrDisallowedSeparator_SkippedWithDiagnostic` (replaces `ReadAsync_NumericOverflowOrDisallowedSeparator_ThrowsCoordinateFormatException`)
- `ReadAsync_EmptyStream_IsFormatFailure`
- `ReadAsync_CancelledBeforeAndDuringRead_ThrowsOperationCanceled`
- `ReadAsync_StreamThrowsDuringRead_PropagatesFailure`
- `ReadAsync_LeavesStreamOpenOnFailureAndCancellation`
- `ReadAsync_LineAt2048Characters_Accepted`
- `ReadAsync_LineOf2049Characters_IsFormatFailure`
- `ReadAsync_FourTokenLine_Skipped`

Expected failure: earlier implementation returns partial data or unspecific exceptions; passing rejection tests recorded as existing behavior.

**Green**
- Failure semantics per SC-04 and SC-10

**Refactor**
- Remove duplication with all tests green

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S4 MOSMIX_L evidence reader

Every SC-20 violation throws `CatalogFormatException` with `ErrorId` `MOSMIX-EVIDENCE-INVALID`.

**Red**
- `Read_ThrowsCatalogFormatException_WhenDtdPresent` (DTD in XML)
- `Read_ThrowsCatalogFormatException_WhenMoreThanOneKmlMember` (two `.kml` members)
- `Read_ThrowsCatalogFormatException_WhenCompressedSizeExceeds1MiB` (over 1 MiB compressed)
- `Read_ThrowsCatalogFormatException_WhenUncompressedSizeExceeds16MiB` (over 16 MiB uncompressed)
- `Read_ThrowsCatalogFormatException_WhenPlacemarkNameDoesNotMatchRequestedId` (wrong placemark name)
- `Read_ThrowsCatalogFormatException_WhenForecastTokenCountDoesNotMatchTimeSteps` (N tokens expected, M provided)
- `Read_AcceptsIssueTimeWithoutFraction` (`2026-10-21T03:00:00Z`)
- `Read_AcceptsIssueTimeWithZeroFraction` (`2026-10-21T03:00:00.000Z`, the observed form, SC-20)
- `Read_ThrowsCatalogFormatException_WhenIssueTimeIsInvalidUtc` (invalid UTC time in `dwd:IssueTime`)
- `Read_ThrowsCatalogFormatException_WhenRequiredPathsAreMissing` (missing required elements)
- `Read_ThrowsCatalogFormatException_WhenElementNameIsNotUnique` (duplicate element names)
- `Read_ThrowsCatalogFormatException_WhenForecastValueHasInvalidTokenFormat` (non-conforming token like `1.2.3` or `+123`)
- `Read_ReturnsElementsAndIssueTime_WhenValidKmlFileProvided` (valid case, returns elements and issue time)
- `Read_ThrowsCatalogFormatException_WhenKmlMemberCountIsZero` (no `.kml` member)
- `Read_ThrowsCatalogFormatException_WhenProductDefinitionIsMissing` (missing `dwd:ProductDefinition`)
- `Read_ThrowsCatalogFormatException_WhenProductIDIsNotMOSMIX` (wrong `dwd:ProductID`)
- `Read_ThrowsCatalogFormatException_WhenIssueTimeHasNoZeroFractionalSeconds` (non-zero fractional seconds in `dwd:IssueTime`)
- `Read_ThrowsCatalogFormatException_WhenForecastTimeStepsIsEmpty` (no `dwd:TimeStep`)
- `Read_CountsOnlyForecastsAtTheSpecifiedPath` (forecast-like elements elsewhere are ignored, SC-20: other elements and attributes are ignored)
- `Read_ThrowsCatalogFormatException_WhenNamespaceUriIsWrong` (a required element in a different namespace URI)
- `Read_AcceptsOtherPrefixesBoundToTheCorrectNamespaceUris`
- `Read_AcceptsArchiveAtExactly1MiBCompressedAnd16MiBUncompressed` (exact limits accepted; one byte more rejected by the size tests above)
- `Read_ThrowsCatalogFormatException_WhenDuplicateRequiredNodesExist` (duplicate required nodes)
- `Read_AllTokensMissing_ReturnsNoAvailableElementsWithIssueTime` (a valid file whose series contain only `-` tokens is read successfully: no available elements, issue time kept; S6 turns it into `unavailable` with evidence timestamps, SC-05, SC-20)
- `Read_ReturnsElement_WhenOneTokenIsZero` (a single `0` token, SC-20: element is available if at least one token is numeric)
- `Read_ThrowsCatalogFormatException_WhenIntegerPartExceedsNineDigits` (token with 10 digits in integer part)
- `Read_ThrowsCatalogFormatException_WhenFractionalPartExceedsSixDigits` (token with 7 digits in fractional part)

Expected failure: stub throws `NotImplementedException`; completeness assertion before registration.

**Green**
- `IMosmixLEvidenceReader` implementation with full SC-20 rules
- Registration

**Refactor**
- Remove duplication with all tests green

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S5 MOSMIX_S evidence index and cache store

**Red**
- `Read_ReturnsNull_WhenIndexFileMissing` (index file missing)
- `Read_ReturnsNull_WhenIndexFileUnreadable` (unreadable index file)
- `Read_ReturnsNull_WhenIndexFileSizeExceeds8MiB` (index exceeds 8 MiB)
- `Read_ReturnsNull_WhenIndexJsonInvalid` (malformed JSON in index)
- `Read_ReturnsNull_WhenIndexFormatVersionWrong` (wrong `format_version`)
- `Read_ReturnsNull_WhenIndexSourceKeyMismatch` (source key mismatch)
- `Read_ReturnsNull_WhenIndexContentViolatesSC20Rules` (index content violates SC-20 rules, per SC-22 return "missing" not throw)
- `Write_RoundTripIndexSuccessfully` (write and read back index)
- `Write_ThrowsWhenFailedWriteKeepsPreviousIndex` (failed write does not overwrite valid index)
- `Write_ThrowsWhenBoundsExceeded_LocationCount` (more than 20000 locations)
- `Write_ThrowsWhenBoundsExceeded_ElementNameCount` (more than 200 elements per location)
- `Write_ThrowsWhenBoundsExceeded_NestingDepth` (JSON nesting depth > 8)
- `Write_ThrowsWhenBoundsExceeded_IndexFileSize` (index file > 8 MiB)
- `Read_ReturnsIndex_WhenValidIndexPresent` (valid index read successfully)
- `Read_AcceptsIndexAtSizeLimit8MiB` (index at 8 MiB boundary, per SC-22 acceptance)
- `Read_RejectsIndexAboveSizeLimit8MiB` (index above 8 MiB boundary, per SC-22 rejection)
- `Read_AcceptsLocationCountAtLimit20000` (20000 locations, per SC-22 acceptance)
- `Read_RejectsLocationCountAboveLimit20000` (more than 20000 locations, per SC-22 rejection)
- `Read_AcceptsElementNameCountAtLimit200` (200 elements per location, per SC-22 acceptance)
- `Read_RejectsElementNameCountAboveLimit200` (more than 200 elements per location, per SC-22 rejection)
- `Read_AcceptsNestingDepthAtLimit8` (nesting depth at 8, per SC-22 acceptance)
- `Read_RejectsNestingDepthAboveLimit8` (nesting depth above 8, per SC-22 rejection)
- `Read_AcceptsValidTimestamps` (timestamps within bounds, per SC-22)
- `Read_RejectsInvalidTimestamps` (timestamps outside bounds, per SC-22 return "missing")
- `Read_AcceptsValidLocationIDs` (location IDs matching grammar, per SC-22)
- `Read_RejectsInvalidLocationIDs` (location IDs not matching grammar, per SC-22 return "missing")
- `Read_AcceptsValidElementNames` (element names matching grammar, per SC-22)
- `Read_RejectsInvalidElementNames` (element names not matching grammar, per SC-22 return "missing")

Expected failure: stub throws `NotImplementedException`; completeness assertion before registration.

**Green**
- `IMosmixEvidenceCache` implementation with SC-22 rules
- Registration

**Refactor**
- Remove duplication with all tests green

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S6 Per-product availability

**Red**
- `Resolve_MissingIndex_ThrowsEvidenceMissing` (throws `EvidenceCacheException` with ID `MOSMIX-S-EVIDENCE-MISSING`)
- `Resolve_StaleIndex_UsedWithItsTimestamps` (index older than `MosmixSEvidenceMaxAge` still used; items carry the index `downloaded_at` and `issued_at`; the resolver writes no log entry — the single notice is P4.09 work)
- `Resolve_MosmixLAbsent_IsUnavailable` (HTTP 404 for MOSMIX_L file → `unavailable`)
- `Resolve_EvidenceTimestamps_Set` (timestamps from evidence file are set correctly)
- `Resolve_ReturnsExactlyOneItemPerProduct` (`MOSMIX_S` and `MOSMIX_L`, no duplicates, deterministic product order)
- `Resolve_NoLocationEvidence_IsUnavailable` (a MOSMIX_S placemark missing from a usable index → `unavailable`)
- `Resolve_ProductWideElementListWithoutLocationEvidence_IsUnavailable` (a populated product-wide element list but no location evidence yields `unavailable` with empty elements)
- `Resolve_AvailableProduct_ListsOnlyVerifiedElements` (element names exactly as in the SC evidence, deterministic order)
- `Resolve_UnavailableProduct_HasEmptyElements`
- `Resolve_ProductsAreIndependent` (evidence for `MOSMIX_L` never fills `MOSMIX_S` and vice versa)
- `Resolve_NeverUsesCdcData` (the resolver takes no CDC input; a MOSMIX ID equal to a CDC ID string gets only MOSMIX evidence)
- `Resolve_AbsentMosmixLEvidence_IsUnavailable` (the resolver receives already-read evidence: an absent MOSMIX_L file gives `unavailable`; malformed or throwing streams are S4 reader tests, unusable indexes are P4.09 selector tests)
- `Resolve_IsDeterministicForIdenticalInput`
- `Resolve_ResultPassesP2ListValidation` (a `MosmixLocationList` built with the items serializes with the real `IJsonExportSerializer` and `IXmlExportSerializer` without a C-23 validation error)
- `AddWeatherParsing_ResolvesMosmixAvailabilityResolver` (validated container); completeness test red when the interface appears

Expected failure: stub throws `NotImplementedException`; completeness assertion before registration.

**Green**
- Availability per SC-05, SC-20, SC-22, SC-23
- Registration
- No logging in the resolver; the stale-index notice is written once per selection by the P4.09 selector (section 3)

**Refactor**
- Remove duplication with all tests green

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S7 Review and evidence

**No Red: review slice.**

Pre-commit coverage on the working tree per CONTRIBUTING.md section 5 (changed-file list from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard`; baseline via `write-baseline` from a `coverage.xml` of the integration target collected the same way in a separate `git worktree`; runner `evaluate`; native covered/total block counters, baseline counters and runner result; exit 0 before Codex). Then Codex reviews the uncommitted diff read-only; qwen fixes confirmed findings test-first and the orchestrating agent reruns build, tests and coverage; checkpoint commit when the user requests it; `pwsh ./scripts/ci-local.ps1 -Commit <commit-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings` on that commit; push and hosted CI record. Package consumer: pack the library and run the existing package-consumer tests against the freshly packed library per CONTRIBUTING.md section 4 before the Codex review.

## 6. Tests and Test IDs

| Step-local case | T-P4 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S0 | T-P4-20 | REQ-067, REQ-068, REQ-079 | Export members, optional timestamp in the generator, schema rejects offsets, `Z` and fractional seconds |
| S1 | T-P4-08 | REQ-101, REQ-083 | Alphanumeric and leading-zero MOSMIX IDs as strings, never CDC IDs; umlauts; stream left open |
| S1 | T-P4-10 (P4.07 part) | REQ-014, REQ-102 | `IMosmixCatalogEndpointsValidator`: defaults, both sides of the SC-18 and SC-22 ranges, directory and catalog URI rules, no network or file-system access, registration |
| S2 | T-P4-08 | REQ-102, REQ-082 | Documented notation to decimal degrees without rounding; bounds on both sides; missing altitude null with diagnostic, excluded from selectable locations with one notice, zero altitude kept; culture independence |
| S3 | T-P4-08 | REQ-102, REQ-083 | Malformed, mixed and all-invalid input per failure semantics; duplicates; overflow and separators; empty stream; cancellation; stream failure; stream ownership |
| S4 | T-P4-09 | REQ-101, REQ-102 | MOSMIX_L evidence reader (SC-20): one `.kml` member, size limits at and above 1 MiB and 16 MiB, DTD prohibited, namespaces by URI, one `dwd:IssueTime` with absent or zero fraction, N ≥ 1 time steps, the requested placemark only, unique element names, token grammar and count; violations `MOSMIX-EVIDENCE-INVALID` |
| S5 | T-P4-19 | REQ-101, REQ-102, REQ-083 | MOSMIX_S index store (SC-22): round trip, "missing" for every unusable index, read and write bounds (8 MiB, depth 8, 20000 locations, 200 names per location, ID and name grammars), atomic replacement |
| S6 | T-P4-09 | REQ-101, REQ-102 | One item per product; MOSMIX_S from the index (`available` with elements, `unavailable` without placemark or elements), missing index `MOSMIX-S-EVIDENCE-MISSING`, stale index used without resolver logging (strictly older than `MosmixSEvidenceMaxAge`); MOSMIX_L 404 `unavailable` (SC-05 option A); evidence timestamps present exactly when evidence was used |
| S0–S6 | T-P4-15 | REQ-085 | Recorded Red → Green → Refactor evidence |
| S0, S6 | T-P4-20 | REQ-067, REQ-068, REQ-079 | Export members `evidence_retrieved_at` and `evidence_issued_at` (C-117 to C-119): generator, regenerated schema, real serializers (S0); the resolver sets them (S6) |

## 7. Evidence to Record

- SC-20 covered by each test
- SC-22 covered by each test
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
- (b) DWD may not publish per-location availability — **Decided: MOSMIX_L, option A (SC-05)**; HTTP 404 → `unavailable`; otherwise the file is read under the evidence-file contract of SC-20 → `available` with the elements whose series contain at least one numeric value, in ordinal order; a valid file without such elements → `unavailable`
- (c) Confusion with CDC IDs — separate types, no CDC input to the resolver
- (d) A product-level element list may change between runs — the SC check date is recorded and P4.11's live check compares
- (e) Reuse of evidence readers — P7 extends the MOSMIX_L reader internals and KML stream reading through shared internal components instead of writing a second parser (SC-21)

## 9. Acceptance Criteria

- [ ] Each behavior increment has recorded Red evidence
- [ ] All T-P4-08, T-P4-09 and T-P4-20 cases pass
- [ ] IDs never numeric or mapped to CDC
- [ ] Missing altitude never 0
- [ ] Exactly one availability item per product, `unknown` explicit, elements only when `available`
- [ ] No fallback between products or catalogs
- [ ] Services registered and completeness tests green
- [ ] Build without warnings
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user (AGENTS.md)
- [ ] No retrieval, distance or altitude-difference filtering, list assembly or forecast parsing code added (only the missing-altitude exclusion of S2)
- [ ] MOSMIX_L evidence reader (S4) enforces every SC-20 rule, each tested on both sides
- [ ] MOSMIX_S index store (S5) round-trips the SC-22 index, returns "missing" for every SC-22 case and replaces the index atomically
- [ ] Resolver (S6) applies SC-05 option A and SC-22: stale index still used, no resolver logging, evidence timestamps set exactly when evidence was used; the missing-index failure and the single `MOSMIX-S-EVIDENCE-STALE` notice are P4.09 work
- [ ] Export members C-117 to C-119 added, schema regenerated and validated (S0)
- [ ] Logging package and DI version raise of SC-08 recorded in `DEPENDENCIES.md`
- [ ] Cache read tests verify that unusable indexes result in "missing" return, not exceptions; resolver tests verify `MOSMIX-S-EVIDENCE-MISSING` is thrown
- [ ] T-P4-15 applies to S4 (MOSMIX_L evidence reader behavior)
- [ ] All T-P4-19 cases of S5 (index store) pass; the refresh part of T-P4-19 is P4.08
- [ ] All SC-05 and SC-22 decisions are reflected in the implementation and tests
- [ ] Evidence cache traceability includes all relevant SC-22 index fields and download/issue times
- [ ] The test matrix covers all slices with applicable behavior tests, including S0–S6
