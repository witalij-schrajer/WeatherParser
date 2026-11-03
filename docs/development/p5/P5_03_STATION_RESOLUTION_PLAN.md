# P5.03 – Station Resolution: Implementation Plan

**Status: executed on 2026-10-24 ([record](../../p5/P5_VERIFICATION.md) section 3): Red evidence per slice, T-P5-01 and T-P5-02 cases green, coverage gate passed, Codex review with 0 findings.** Reconciled with the approved observation contract ([OC rows](../../p5/P5_OBSERVATION_CONTRACT.md), approved 2026-10-24) on 2026-10-24: OC-01, OC-02 (option A) and OC-10; conflict variant A kept, variant B removed. Refines the P5.03 row of section 5 and the "Station resolution" row of section 4 of [PHASE_P5_PLAN.md](../PHASE_P5_PLAN.md).

## 1. Goal and Scope
Goal: an injectable resolver that validates one station ID (OC-01), finds the station in the four CDC station lists supplied by the caller (OC-02), assembles the `ObservationLocation` (name with umlauts, coordinates, altitude or none, ID with leading zeros) and names the products whose lists contain the station — without any HTTP request and before any `now` archive is touched.
In scope: ID grammar check as the first action, exposed as ValidateId so that the parser (P5.07) rejects an invalid ID before any request; lookup over `CdcProductList` records in the product order `tu`, `ff`, `rr`, `sd`; location assembly; product set; cross-list conflict handling per OC-02; failures `STATION-ID-INVALID` and `STATION-UNKNOWN` (and `STATION-METADATA-CONFLICT`, S3) through `DwdStationException` (approved, OC-10); registration in `AddWeatherParsing`.
Out of scope with owner: reading the lists and archives over HTTP → P5.06; row parsing → P5.04; quantity mapping → P5.05; dataset assembly and logging of diagnostics → P5.07; `StationID` versus configured reference station → P3.

## 2. Preconditions and Inputs
- P5.02 approved OC-01, OC-02 and OC-10 on 2026-10-24; OC-02 conflict handling decided as option A (fail with `STATION-METADATA-CONFLICT`).
- Existing types: `CdcProductList`, `CdcStationCatalog`, `CdcStationRecord`, `CatalogDiagnostic` (P4), `ObservationLocation` (P2).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Interface and types | `IDwdStationResolver`/`DwdStationResolver` with `string ValidateId(string stationId)` (grammar check only, returns the validated ID, used by the parser before any request) and `DwdResolvedStation Resolve(string stationId, IReadOnlyList<CdcProductList> lists)` (calls `ValidateId` first); result `DwdResolvedStation` (`StationId`, `Location` as `ObservationLocation`, `ProductCodes` in product order, `Diagnostics`) / other names | Proposed names; synchronous because no I/O happens | Orchestrator, recorded (OC-08 names the parser only) | S1 |
| Conflict handling | fail with `STATION-METADATA-CONFLICT` after the `CDC-STATION-CONFLICT` diagnostic (approved, OC-02 option A) / use the first list in product order and emit `CDC-STATION-CONFLICT` (rejected) | Option A, approved 2026-10-24 (OC-02); only its tests are planned | User (OC-02) | S3 |
| Folder | `src/WeatherParser.Parsing/Observations/Dwd/`, namespace `WeatherParser.Parsing.Observations.Dwd` / other | Proposed (phase plan section 8) | Orchestrator, recorded | S1 |
| Exception type | `DwdStationException` with `ErrorId` `STATION-ID-INVALID`, `STATION-UNKNOWN`, `STATION-METADATA-CONFLICT` (pattern of `SelectionLimitException`; the conflict case also carries the `CDC-STATION-CONFLICT` `CatalogDiagnostic` in an additive `Diagnostic` property so that the parser can log it, P5.07) / reuse of an existing type | New type (OC-10, approved) | User (OC-10) | S1 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Observations/Dwd/IDwdStationResolver.cs` | Create | Interface |
| `src/WeatherParser.Parsing/Observations/Dwd/DwdStationResolver.cs` | Create | Proposed implementation |
| `src/WeatherParser.Parsing/Observations/Dwd/DwdResolvedStation.cs` | Create | Proposed result record |
| `src/WeatherParser.Parsing/Exceptions/DwdStationException.cs` | Create | `ErrorId` constants and guard as in `SelectionLimitException` |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Singleton registration |
| `tests/WeatherParser.Parsing.Tests/Observations/Dwd/DwdStationResolverTests.cs` | Create | Behavior tests |
| `tests/WeatherParser.Parsing.Tests/Observations/Dwd/DwdStationExceptionTests.cs` | Create | `ErrorId` guard tests |
| `tests/WeatherParser.Parsing.Tests/Architecture/ServiceRegistrationCompletenessTests.cs` | Unchanged | Must stay green after registration |
| `docs/p5/P5_VERIFICATION.md` | Change | P5.03 section |

## 5. Procedure
### S1 ID grammar
**Red**
- `ValidateId_FiveAsciiDigits_ReturnsId`
- `ValidateId_InvalidInputs_ThrowStationIdInvalid` (the same inputs as the `Resolve_*` grammar cases)
- `ValidateId_Null_ThrowsStationIdInvalid` (OC-01: `null` is an invalid ID, `DwdStationException` `STATION-ID-INVALID`, not `ArgumentNullException`)
- `Resolve_FiveAsciiDigits_Accepted`
- `Resolve_EmptyId_ThrowsStationIdInvalid`
- `Resolve_WhitespaceId_ThrowsStationIdInvalid`
- `Resolve_NonDigitId_ThrowsStationIdInvalid` (`A1234`)
- `Resolve_UnicodeDigits_ThrowsStationIdInvalid` (Arabic-Indic digits)
- `Resolve_TrailingNewline_ThrowsStationIdInvalid` (`00044\n`)
- `Resolve_FourDigits_ThrowsStationIdInvalid`
- `Resolve_SixDigits_ThrowsStationIdInvalid`
- `Resolve_NullLists_ThrowsArgumentNullException`
- `Resolve_InvalidId_DoesNotTouchLists` (a list collection whose enumeration throws is never enumerated)
- `DwdStationException_UnknownErrorId_ThrowsArgumentException`
- `AddWeatherParsing_ResolvesDwdStationResolver`
- `ServiceRegistrationCompletenessTests` red when the interface appears before registration

Expected failure: `NotImplementedException` from the stub; the completeness test fails with its missing-registration assertion.

**Green**
- grammar `\A[0-9]{5}\z` (`RegexOptions.CultureInvariant`) checked before anything else; exception with English message without the input value echoed unsanitized (the ID is five digits or rejected); registration.

**Refactor**
- remove duplication with all tests green.

**Roles**
- qwen generates the tests first, then the minimal implementation and the refactoring; the orchestrating agent writes the briefs with the real declarations, runs each Red, build, tests and coverage, reviews the diff and records authorship.

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`; `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S2 Lookup and metadata
**Red**
- `Resolve_UnknownId_ThrowsStationUnknown` (in no list)
- `Resolve_StationInOneList_ReturnsLocationFromThatList`
- `Resolve_KeepsLeadingZeros` (`00044`)
- `Resolve_PreservesUmlautsInName` (`Würzburg`)
- `Resolve_MissingAltitude_IsNullNeverZero`
- `Resolve_ZeroAltitude_IsZero`
- `Resolve_ProductCodes_InProductOrder` (station in `rr` and `tu` → `tu`, `rr`)
- `Resolve_ShuffledLists_ProductOrderCanonical` (lists supplied as `rr`, `sd`, `tu` → `tu`, `rr`, `sd`)
- `Resolve_StationInAllFourLists_ReturnsFourProducts`
- `Resolve_CoordinatesExactDecimal` (no rounding)

Expected failure: the lookup and metadata tests (`Resolve_StationInOneList_ReturnsLocationFromThatList` to `Resolve_CoordinatesExactDecimal`) fail against the S1 implementation, which throws `STATION-UNKNOWN` for every valid ID or returns no location; `Resolve_UnknownId_ThrowsStationUnknown` passes on first run against that implementation and is recorded as existing behavior, not as Red evidence.

**Green**
- ordinal lookup by `StationId` in each `list.Catalog.Records`; `ObservationLocation` with `StationId`, `Name`, `Latitude`, `Longitude`, `AltitudeM`; product codes in the canonical order `tu`, `ff`, `rr`, `sd` regardless of the order of the supplied lists, filtered to those containing the station.

**Refactor**
- as in S1.

**Roles**
- as in S1.

**Check**
- as in S1.

### S3 Cross-list conflicts
**Red**
- `Resolve_IdenticalRecordsInSeveralLists_NoDiagnostic`
- `Resolve_ConflictingRecords_ThrowsStationMetadataConflict` (OC-02 option A: `DwdStationException` `STATION-METADATA-CONFLICT` whose `Diagnostic` is the `CDC-STATION-CONFLICT` `CatalogDiagnostic` naming both endpoint names, for a differing name, latitude, longitude or altitude, including altitude present in one list and absent in the other)
- `Resolve_ConflictingRecordsShuffledLists_SameFailure` (the failure does not depend on the input order of the lists)

Expected failure: `Resolve_ConflictingRecords_ThrowsStationMetadataConflict` and `Resolve_ConflictingRecordsShuffledLists_SameFailure` fail against the S2 implementation, which returns the first record silently; `Resolve_IdenticalRecordsInSeveralLists_NoDiagnostic` passes on first run against it and is recorded as existing behavior, not as Red evidence.

**Green**
- comparison of name, coordinates and altitude across the lists; behavior per OC-02.

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
- the orchestrating agent runs the checks; Codex reviews the uncommitted diff read-only; qwen fixes confirmed findings test-first; checkpoint commit when the user requests it; push to the draft pull request; hosted CI run recorded.

**Check**
- pre-commit coverage (CONTRIBUTING.md section 5); Codex review; commit and CI run recorded in `docs/p5/P5_VERIFICATION.md`.

## 6. Tests and Test IDs
| Step-local case | T-P5 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P5-01 | REQ-030, REQ-031 | Grammar `\A[0-9]{5}\z`; empty, whitespace, non-digit, Unicode digits, trailing newline, four and six digits rejected with `STATION-ID-INVALID`; lists untouched on rejection |
| S2, S3 | T-P5-02 | REQ-031, REQ-023 | Unknown ID → `STATION-UNKNOWN`; location fields exact; leading zeros; umlauts; altitude null never zero; products per station; conflicts per OC-02 |
| S1 | T-P5-13 (partial) | REQ-016, REQ-078 | Resolver behind its interface, registered as a singleton; completeness test green |
| S1–S3 | T-P5-15 | REQ-085 | Recorded Red → Green → Refactor evidence |

## 7. Evidence to Record
- Red names with observed failures; existing-behavior records
- Commands, test counts, native block counters and gate result
- OC-02 variant kept; Codex review and dispositions; authorship

## 8. Risks and Limitations
- (a) The lists change format — the resolver consumes P4 records only; format handling stays in `ICdcStationCatalogReader`
- (b) A station in a list without a `now` archive (observed on 2026-10-21) — handled by P5.06 as a missing product, not here
- (c) Conflict policy decided on 2026-10-24 (OC-02 option A); 0 conflicts were observed on 2026-10-21 and 2026-10-24, so the failure path is tested with synthetic lists only

## 9. Acceptance Criteria
- [ ] Each behavior increment has recorded Red evidence
- [ ] All T-P5-01 and T-P5-02 cases pass
- [ ] No HTTP, stream or file access in this step's production code
- [ ] IDs never numeric; altitude never a substituted zero
- [ ] Resolver registered; completeness and no-shared-state tests green
- [ ] Build without warnings; pre-commit coverage gate passed
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user
- [ ] No retrieval, parsing or export code added
