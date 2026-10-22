# P4.06 – CDC Availability Derivation: Implementation Plan

**Status: implemented on 2026-10-22 (S1 to S4, records in [P4_VERIFICATION.md](P4_VERIFICATION.md) section 6); not yet committed.** Original planning note: plan only on 2026-10-21. Reconciled with the approved selection contract (SC-02, SC-03, SC-19, SC-21) on 2026-10-21 (P4.02 S6).** Refines the P4.06 row of section 5 and the "CDC availability" row of section 4 of [PHASE_P4_PLAN.md](../../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 3, 1, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0). Later: Codex-outage acceptance wording. S6 record (2026-10-21): qwen reconciled this plan from the orchestrating agent's change list and fixed the confirmed review findings section by section; the orchestrating agent edited these parts itself after failed qwen attempts, for regressions or for recorded design decisions: removal of a duplicated S3 slice; S2/S3 test lines and S3 expected failure; section 3 deriver and evidence-reader signatures (orchestrator decision); valid-layouts test; section 9 criterion; one altitude notice per station (cross-plan). Codex five-angle review rounds: 5, 3, 2, 2, 2, 1, 0 findings; cross-plan five-angle review of all P4 plans: rounds 4, 3, 1, 1, 0 findings.

## 1. Goal and Scope
Goal: an injectable service that derives, per CDC station, the verified `available_measurements` (REQ-052 field names per C-21, for example `temperature_c`) and the station's suitability (active versus inactive) from the `now` files read by the injectable `ICdcNowEvidenceReader` (SC-19), in two stages (SC-03): a pre-filter on the from- and to-dates of the four `now` station lists read by the P4.05 reader `ICdcStationCatalogReader` (no separate metadata reader), followed by usable values in the station's per-station `now` files read by the injectable `ICdcNowEvidenceReader` (SC-19); (REQ-024), test-first with synthetic fixtures.

In scope:
- CDC evidence reader (`ICdcNowEvidenceReader`, SC-19) for the SC-approved metadata format from caller-supplied streams (asynchronous, `CancellationToken`, invariant culture, encoding per SC, stream left open, failure semantics per SC)
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
| Interface and type names | — | `ICdcAvailabilityDeriver`/`CdcAvailabilityDeriver` (SC-11) with two operations: `SelectCandidates(IReadOnlyList<CdcStationCatalog> lists, DateTimeOffset referenceTimeUtc)` (S2: union, conflicts and candidate products, returning candidates plus diagnostics) and `Derive(CdcCandidates candidates, IReadOnlyDictionary<CdcEvidenceKey, CdcNowEvidence?> evidence, DateTimeOffset referenceTimeUtc)` (S3: usable values, `null` for an absent product file, returning `CdcAvailabilityResult` with per-station items and all diagnostics, including those of excluded stations); the station lists come from the P4.05 `ICdcStationCatalogReader`, no separate metadata reader exists | Orchestrator, recorded (SC-11 approves the interfaces as proposed in this plan) | S2, S3 |
| Reference time validation | accept any `DateTimeOffset` / require offset zero (UTC) | Require offset zero; a non-UTC value throws `ArgumentException` with an English message (AGENTS.md UTC rule) | Orchestrator, recorded | S2 |
| Order of `available_measurements` | ordinal order of REQ-052 names / mapping-table order | Ordinal order, deterministic for identical input (REQ-068) | Orchestrator, recorded | S2 |
| CDC evidence reader interface | — | `ICdcNowEvidenceReader`/`CdcNowEvidenceReader` (SC-11, SC-19) with `ReadAsync(Stream archive, string stationId, string productCode, CancellationToken cancellationToken)` returning `CdcNowEvidence` (validated header columns and rows of that product); the caller supplies the downloaded stream, retrieval stays in P4.08 | Orchestrator, recorded | S1 |
| CDC evidence reader implementation | — | As per SC-19 contract | User (P4.02, approved 2026-10-21) | S1 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Catalogs/ICdcAvailabilityDeriver.cs` | Create | Interface per SC-19 |
| `src/WeatherParser.Parsing/Catalogs/CdcAvailabilityDeriver.cs` | Create | Implementation per SC-19 |
| `src/WeatherParser.Parsing/Catalogs/CdcStationAvailability.cs` | Create | Type per SC-19 |
| `src/WeatherParser.Parsing/Catalogs/CdcCandidates.cs` | Create | Type per SC-19 |
| `src/WeatherParser.Parsing/Catalogs/CdcEvidenceKey.cs` | Create | Type per SC-19 |
| `src/WeatherParser.Parsing/Parsing/ICdcNowEvidenceReader.cs` | Create | Interface per SC-19; takes `Stream` parameter |
| `src/WeatherParser.Parsing/Parsing/CdcNowEvidenceReader.cs` | Create | Implementation per SC-19 |
| `src/WeatherParser.Parsing/Parsing/CdcNowEvidence.cs` | Create | Type per SC-19 |
| `src/WeatherParser.Parsing/Parsing/CdcAvailabilityResult.cs` | Create | Type per SC-19 |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration in the slice that adds each interface |
| `tests/WeatherParser.Parsing.Tests/Catalogs/CdcAvailabilityDeriverTests.cs` | Create | Tests for deriver |
| `tests/WeatherParser.Parsing.Tests/Parsing/CdcNowEvidenceReaderTests.cs` | Create | Tests for evidence reader |
| `tests/WeatherParser.Parsing.Tests/Fixtures/Cdc/` | Create | Synthetic fixtures |
| `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` | Extend | Registration tests |

## 5. Procedure
### S1 CDC evidence reader
**Red**
- `ReadAsync_ValidEvidenceFixture_ReturnsEntries`
- `ReadAsync_KeepsLeadingZeroIds`
- `ReadAsync_DateFieldsParsedAsDocumented`
- `ReadAsync_MalformedEntry_FollowsFailureSemantics`
- `ReadAsync_AllEntriesInvalid_IsFormatFailure`
- `ReadAsync_CancelledBeforeAndDuringRead_ThrowsOperationCanceled`
- `ReadAsync_LeavesStreamOpenOnSuccessFailureAndCancellation`
- `ReadAsync_StreamThrowsDuringRead_PropagatesFailure`
- `ReadAsync_EvidenceFileHasWrongMemberName_Throws`
- `ReadAsync_EvidenceFileHasTwoMembers_Throws`
- `ReadAsync_EvidenceFileOversizedCompressed_Throws`
- `ReadAsync_EvidenceFileOversizedUncompressed_Throws`
- `ReadAsync_EvidenceFileHasWrongStationId_Throws`
- `ReadAsync_EvidenceFileHasDuplicateTimestamps_Throws`
- `ReadAsync_ValidProductLayouts_Accepted` (parameterized over valid `tu`, `ff`, `rr` and `sd` archives with their SC-19 product columns; malformed layouts stay in the separate rejection tests)
- `ReadAsync_EvidenceFileWritesNothingToDisk`
- `ReadAsync_HeaderFieldsAreUniqueAndOrdered`
- `ReadAsync_HeaderHasRequiredColumnsForTuProduct`
- `ReadAsync_HeaderHasRequiredColumnsForFfProduct`
- `ReadAsync_HeaderHasRequiredColumnsForRrProduct`
- `ReadAsync_HeaderHasRequiredColumnsForSdProduct`
- `ReadAsync_DataRowFieldCountMatchesHeader`
- `ReadAsync_StationIdMatchesExpectedPattern`
- `ReadAsync_MessDatumIsWellFormedAndValid`
- `ReadAsync_QnFieldMatchesNumericGrammar`
- `ReadAsync_ProductColumnValuesMatchNumericGrammar`
- `ReadAsync_EorFieldIsLiteral`
- `ReadAsync_DataRowTimestampsAreUnique`
- `ReadAsync_MaximumRowsLimitIsEnforced`
- `ReadAsync_ExactRowLimitIsAccepted`
- `ReadAsync_HeaderFieldNamesAreTrimmed`
- `ReadAsync_DataRowFieldsAreTrimmed`
- `ReadAsync_IgnoresExtraColumnsInHeaderAndData`
- `ReadAsync_CompressedSizeLimitIsEnforced`
- `ReadAsync_UncompressedSizeLimitIsEnforced`
- `ReadAsync_TextEncodingIsIso88591`
- `ReadAsync_ThrowsCatalogFormatExceptionOnInvalidFile`
- `ReadAsync_EvidenceFileHasExactlyOneMiBCompressed_ThrowsIfOver`
- `ReadAsync_EvidenceFileHasExactlyOneMiBUncompressed_ThrowsIfOver`
- `ReadAsync_EvidenceFileHasExactlyOneMiBCompressed_AcceptsIfAtLimit`
- `ReadAsync_EvidenceFileHasExactlyOneMiBUncompressed_AcceptsIfAtLimit`
- `ReadAsync_EvidenceFileHasExactGrammarBoundaryValues_Accepts`
- `ReadAsync_EvidenceFileHasExactGrammarBoundaryValues_ThrowsIfOver`
- `ReadAsync_EvidenceFileHasCRLFAndLFLineEndings_AcceptsBoth`
- `AddWeatherParsing_ResolvesCdcNowEvidenceReader` (validated container, C-108)
- `ServiceRegistrationCompletenessTests` red when the interface appears, before registration. Expected failures: behavior tests fail with `NotImplementedException` from the method stubs; the registration and completeness tests fail with their assertion on the missing registration (constructors stay free of throwing stubs); both are recorded before Green.

**Green**
- Reader implements `ICdcNowEvidenceReader` per SC-19
- Registration in `ServiceCollectionExtensions`
- Archive must contain exactly one member named `produkt_zehn_now_<code>_<yyyyMMdd>_<yyyyMMdd>_<id>.txt` with the product code (`tu`, `ff`, `rr`, `sd`) and the requested station ID
- At most 1 MiB compressed and 1 MiB uncompressed, enforced while streaming
- Text decoded as ISO-8859-1, CRLF or LF
- At most 2000 data rows
- Line 1 is the header: fields split on `;` and trimmed; names unique; first `STATIONS_ID`, second `MESS_DATUM`, third `QN`, last `eor`; it must contain the product's columns (`tu`: `PP_10`, `TT_10`, `RF_10`; `ff`: `FF_10`, `DD_10`; `rr`: `RWS_10`; `sd`: `GS_10`); further columns are allowed and ignored
- Every data row has the header's field count; fields are trimmed; `STATIONS_ID` matches `^\d{1,5}$` and equals the numeric value of the requested ID; `MESS_DATUM` matches `^\d{12}$` and is a valid `yyyyMMddHHmm` UTC time; `QN` matches `^\d{1,3}$`; each used product column matches `^-?\d{1,6}(\.\d{1,3})?$` (a value numerically equal to −999 means missing, SC-03); the last field is the literal `eor`; `MESS_DATUM` values are unique
- Archives are read as in-memory streams; member names are only compared, never used as file-system paths, and nothing is written to disk
- Any violation makes the file invalid: `CatalogFormatException` with `ErrorId` `CDC-EVIDENCE-INVALID`

**Refactor**
- Remove duplication with all tests green.

**Roles**
- qwen generates fixtures from the orchestrator's format description, tests first, then minimal implementation and refactoring
- the orchestrating agent writes briefs with the SC facts and real declarations (`CdcStationCatalog`, `CdcStationRecord`, `ServiceCollectionExtensions`), checks fixtures for synthetic provenance, runs each Red, build, tests and coverage, reviews the diff and records authorship

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S2 Pre-filter, union and conflicts
**Red**
- `Derive_UnionOfFourProductLists`
- `Derive_StationWithConflictingAttributes_ExcludedWithConflictDiagnostic` (one case each for altitude, coordinates and name differing between lists; `CDC-STATION-CONFLICT`)
- `SelectCandidates_StationWithoutAltitude_OneNoAltitudeDiagnosticPerStation` (a station without altitude in one or all four lists yields exactly one `CDC-STATION-NO-ALTITUDE` diagnostic naming the endpoint and the station ID and is marked not filterable, SC-02)
- `Derive_StationInOnlyOneList_Kept`
- `Derive_CandidateProductRule` (from-date ≤ reference UTC date, to-date ≥ reference UTC date − 1 day and to-date ≤ reference UTC date + 1 day)
- `Derive_FromDateAfterToDateOrToDateBeyondBound_SkippedAsMalformed` (`CATALOG-RECORD-SKIPPED`)
- `Derive_StationWithoutCandidateProduct_IsInactive`
- `Derive_ActivityAtReferenceTimeBoundary` (both sides of each date bound of the candidate product rule; list dates treated as UTC dates, SC-03)
- `AddWeatherParsing_ResolvesCdcAvailabilityDeriver` (validated container)
- `ServiceRegistrationCompletenessTests` red when the interface appears, before registration. Expected failures: behavior tests fail with `NotImplementedException` from the method stubs; the registration and completeness tests fail with their assertion on the missing registration (constructors stay free of throwing stubs); both are recorded before Green.
- `Derive_StationExcludedByConflictDiagnosticIsAccessibleToCaller` **(SC-03)**
- `Derive_AllRecordsMalformed_ThrowsCatalogFormatException` **(SC-10)**

**Green**
- Union
- Conflict exclusion
- Candidate product rule
- Malformed record handling
- Inactive station handling
- Registration
- Caller-accessible diagnostics for excluded stations
- `CATALOG-FORMAT-INVALID` when all records are malformed **(SC-10)**

**Refactor**
- Remove duplication with all tests green.

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S3 Usable-value derivation
**Red**
- `Derive_TakesReferenceInstantAndParsedEvidence` (takes the reference instant (UTC `DateTimeOffset`, offset zero) and per station the parsed evidence of its candidate products (absent evidence for HTTP 404))
- `Derive_MapsApprovedProductsToReq052Names` (SC-03 mapping with validator kinds: `TT_10` Temperature, `RF_10` RelativeHumidity, `PP_10` Pressure giving `pressure_hpa` and `pressure_mmhg`, `FF_10` MeanWindSpeed, `DD_10` WindDirection, `RWS_10` PrecipitationAccumulation, `GS_10` RadiationEnergy)
- `Derive_UsableValueRule` (token parsed as decimal, numerically −999 means missing, otherwise converted to double and accepted by the P2 `IQuantityValidator`)
- `Derive_ValueInTimeWindow` (row's `MESS_DATUM` in the closed interval [reference instant − 3 h, reference instant + 10 min]; both sides of each bound)
- `Derive_StationWithNoAvailableQuantity_IsInactive`
- `Derive_OrdersMeasurementsOrdinally`
- `Derive_IsDeterministicForIdenticalInput`
- `Derive_NonUtcReferenceTime_Throws`
- `Derive_404ProductContributesNothing`
- `Derive_MissingMarkerSpellings` (`-999`, `-999.0`, `-0999`)
- `Derive_ValidatorRejections` (humidity 101, negative wind speed)
- `Derive_GenuineZerosUsable`
- `AddWeatherParsing_ResolvesCdcAvailabilityDeriver` (validated container)
- `Derive_ExcludedStationDiagnosticsAreReturned` (excluded station diagnostics must be accessible to caller, preserving SC notice ID and context)

Expected failure: the usable-value tests (mapping, usable-value rule, window bounds, missing-marker spellings, validator rejections, genuine zeros, HTTP 404 product) fail against the S2 implementation, which does not yet evaluate evidence; `AddWeatherParsing_ResolvesCdcAvailabilityDeriver` and the excluded-diagnostics test already pass after S2 and are recorded as existing behavior.

**Green**
- Mapping
- Usable value rule
- Time window
- Inactive station handling
- Ordering
- Registration
- Excluded diagnostics

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
| S1 | T-P4-07 | REQ-024, REQ-083 | Evidence format per SC-19: IDs, dates, malformed and all-invalid input, cancellation, stream ownership, member name, number of members, oversized data, wrong station ID, duplicate timestamps, product layouts, no disk writes |
| S2 | T-P4-07 | REQ-024, REQ-052, REQ-068 | Union of four lists; conflict exclusion; candidate product rule; malformed record handling; activity threshold on both sides; inactive not suitable; ordinal deterministic order; UTC reference time |
| S3 | T-P4-07 | REQ-024, REQ-052 | Usable value rule; time window; station with no available quantity inactive; ordering; non-UTC reference time throws; 404 product contributes nothing; missing marker spellings; validator rejections; genuine zeros usable |
| S1, S2, S3 | T-P4-14 (partial) | REQ-016 | Services resolved through their interfaces; registration completeness (C-107) |
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
- (e) P5 builds its observation parsing on `ICdcNowEvidenceReader` (SC-21); design internals for reuse

## 9. Acceptance Criteria
- [ ] Each behavior increment has recorded Red evidence
- [ ] All T-P4-07 cases pass
- [ ] No REQ-052 name without approved mapping
- [ ] No `unknown` or invented vocabulary in `available_measurements`
- [ ] Missing metadata never yields an empty success, unless all candidate products return HTTP 404 (valid absence of evidence) or all stations are inactive (SC-03)
- [ ] Inactive stations never marked suitable
- [ ] Services registered and completeness tests green
- [ ] Build without warnings
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user (AGENTS.md)
- [ ] No retrieval, filtering, export or logging code added
- [ ] **Red** test for `CatalogFormatException` / `CATALOG-FORMAT-INVALID` when all records in a catalog are malformed (SC-10)
- [ ] **Green** rule: `CatalogFormatException` / `CATALOG-FORMAT-INVALID` is thrown if no syntactically valid record remains after skipping malformed ones (SC-10)
- [ ] **Red** test: valid records that are all inactive or excluded give an empty candidate or availability result with their diagnostics and no exception (SC-10); the empty list and its single `SELECTION-EMPTY` notice are P4.09 work
