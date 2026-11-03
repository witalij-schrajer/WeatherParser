# P2.03 – CDC and MOSMIX List Graphs: Implementation Plan

**Status: completed (2026-10-20, marked completed by the user).** Implemented test-first in commit `0f59e1c`; hosted CI run 39396695630 of draft pull request #3 passed on Windows x64, Ubuntu x64 and Debian 13 ARM64; evidence in [P2_VERIFICATION.md](../../p2/P2_VERIFICATION.md) section 3. Original planning note: this document started as a plan on 2026-10-20. This plan refines the P2.03 row of section 5 and the CDC/MOSMIX rows of section 4 of [PHASE_P2_PLAN.md](../PHASE_P2_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; qwen's correction round fixed the requested points but duplicated lines, so the orchestrating agent applied the five specified corrections (S0 roles, S2 test path, S4/S5 Red test names, verification artifact row) to the first draft itself. Extended on 2026-10-20 after the user's review of the P2.03 contract draft: slice S6 adds regression tests for C-27 (unassigned required members fail in both formats with an untouched destination; explicit zeros and the constant `schema_version` stay valid); text drafted by qwen, inserted and capitalized by the orchestrating agent.

## 1. Goal and Scope
Goal: complete typed, `[Serializable]` export graphs for the CDC station list (REQ-023) and the MOSMIX location list (REQ-101–102 entity parts), with list metadata and search parameters, independent string IDs, product availability including explicitly unknown availability, empty lists, and a deterministic ordering service; proven with the real serializers.

In scope:
- List root and entry entities for both lists
- A shared search-parameter value entity (REQ-074 allows reusable value entities)
- An ordering service with interface
- JSON/XML contract tests for populated and empty lists
- Serializable traversal extended to the new types
- DI registration of the ordering service
- Contract-record updates

Out of scope (owner):
- Catalog retrieval, coordinate parsing, geodesic distance, altitude filtering, availability derivation, exclusion of locations without altitude, the empty-result log notice → P4
- Envelope and multi-root document → P2.08
- XSDs → P2.09
- Package-consumer evidence → P2.10
- MQTT topics → P8

## 2. Preconditions and Inputs
- Prerequisite steps: P2.02 complete (contract record approved; omission pattern, time representation, naming, numeric type, serializer services in place)
- Existing repository facts: The `WeatherParser.Parsing` library project exists with its test project; the `ServiceCollectionExtensions.AddWeatherParsing(IServiceCollection)` registration method is defined
- Required user authorization: Contract decisions must be reviewed and approved by the user before proceeding to S1

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Type names | Proposed `CdcStationList`, `CdcStationListEntry`, `MosmixLocationList`, `MosmixLocationListEntry`, `SearchParameters` / other | Proposed names | User (contract review) | S1, S2 |
| Search-parameter field names | `latitude`, `longitude`, `altitude_m`, `max_distance_km`, `max_altitude_difference_m` / names mirroring CLI options | First option (units in names like REQ-052) | User (contract review) | S1 |
| `available_measurements` vocabulary | REQ-052 field names (for example `temperature_c`) / DWD product codes | REQ-052 field names; mapping from DWD products is P4/P5 work | User (contract review) | S1 |
| MOSMIX availability shape | per entry, one item per product with `product` (`MOSMIX_S`/`MOSMIX_L`), `availability` (`available`/`unavailable`/`unknown`) and `available_forecast_elements` / one list per product with a list-level `product` field | Per-entry per-product items, so unknown availability is explicit per product | User (contract review) | S2 |
| MOSMIX element vocabulary | DWD element names as published (for example `TTT`) / REQ-052 names | DWD element names, verified in P4/P7; mapping to REQ-052 names happens in forecast normalization | User (contract review) | S2 |
| ID ordering tie-break | ordinal string comparison of the ID / numeric comparison | Ordinal (`StringComparer.Ordinal`): culture-independent and keeps leading zeros and alphanumeric IDs distinct | Orchestrator, recorded | S3 |
| Numeric types for coordinates/distances | `decimal` (as decided in P2.02) / `double` | Follow the P2.02 numeric-type decision | Orchestrator, recorded | S1, S2 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p2/P2_EXPORT_CONTRACT.md` | Change | Add section 3 decisions and field mappings |
| `src/WeatherParser.Parsing/Export/CdcStationList.cs` | Create | CDC station list root entity |
| `src/WeatherParser.Parsing/Export/CdcStationListEntry.cs` | Create | CDC station list entry entity |
| `src/WeatherParser.Parsing/Export/MosmixLocationList.cs` | Create | MOSMIX location list root entity |
| `src/WeatherParser.Parsing/Export/MosmixLocationListEntry.cs` | Create | MOSMIX location list entry entity |
| `src/WeatherParser.Parsing/Export/MosmixProductAvailability.cs` | Create | MOSMIX product availability entity |
| `src/WeatherParser.Parsing/Export/SearchParameters.cs` | Create | Search parameters value entity |
| `src/WeatherParser.Parsing/Export/IListEntryOrdering.cs` | Create | Interface for list entry ordering service |
| `src/WeatherParser.Parsing/Export/ListEntryOrdering.cs` | Create | Implementation of list entry ordering service |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Register the ordering service |
| `tests/WeatherParser.Parsing.Tests/Export/CdcStationListContractTests.cs` | Create | Tests for CDC list contract |
| `tests/WeatherParser.Parsing.Tests/Export/MosmixLocationListContractTests.cs` | Create | Tests for MOSMIX list contract |
| `tests/WeatherParser.Parsing.Tests/Export/ListEntryOrderingTests.cs` | Create | Tests for ordering service |
| `tests/WeatherParser.Parsing.Tests/Export/ExportEntitySerializableTests.cs` | Change | Extend to include new types |
| `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` | Change | Add test for DI registration |
| `tests/WeatherParser.Parsing.Tests/Export/ListRequiredMemberGuardTests.cs` | Create | Regression tests for C-27 in both formats, explicit zeros and the constant schema version. |
| `docs/p2/P2_VERIFICATION.md` | Change | Append per-slice evidence for P2.03 |

## 5. Procedure
1. **S0 Contract record**
   - Action: add the section 3 decisions and field mappings to `docs/p2/P2_EXPORT_CONTRACT.md` and obtain the user's review before S1; no code, so no Red/Green.
   - Roles: qwen drafts the prose from the orchestrator's decisions; the orchestrator verifies it against REQUIREMENTS.md and the phase plan and records the user's approval.

2. **S1 CDC list contract**
   - Red: `Serialize_CdcList_WritesRequiredFieldsInBothFormats` (entry `station_id` `"00044"`, negative `altitude_difference_m`, search parameters, `retrieved_at`) and `Serialize_EmptyCdcList_WritesEmptyCollection`. Expected failure: types first created without serializer attributes write PascalCase names (`StationId`) instead of `station_id`; an uninitialized entries list is omitted in XML and written as `null` in JSON.
   - Green: Add attributes per the P2.02 naming decision; initialize entries list empty.
   - Refactor: None.
   - Roles: qwen generates tests, minimal implementation and refactoring; orchestrator writes briefs, runs Red test, confirms failure reason, runs build and tests, reviews diff, returns defects to qwen, records authorship.
   - Check: `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`; `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

3. **S2 MOSMIX list contract**
   - Red: `Serialize_MosmixList_PreservesAlphanumericAndLeadingZeroIds` (synthetic IDs `"X123A"` and `"01234"`), `Serialize_MosmixList_WritesUnknownAvailabilityExplicitly`, `Serialize_EmptyMosmixList_WritesEmptyCollection`. Expected failure: as S1 for names/empty list; unknown availability written as an omitted field instead of `unknown` until the availability enum and its explicit serialization are added.
   - Green: Add availability entity and enum with explicit wire values.
   - Refactor: None.
   - Roles: qwen generates tests, minimal implementation and refactoring; orchestrator writes briefs, runs Red test, confirms failure reason, runs build and tests, reviews diff, returns defects to qwen, records authorship.
   - Check: `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`; `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

4. **S3 Ordering service**
   - Red: `Order_SortsByDistanceThenOrdinalId` (equal distances, IDs `"00044"`, `"00100"`, `"44"`), `Order_IsDeterministicForIdenticalInput`, `Order_EmptyInput_ReturnsEmpty`. Expected failure: the first stub returns the input order.
   - Green: `OrderBy(distance).ThenBy(id, StringComparer.Ordinal)`.
   - Refactor: None.
   - Roles: qwen generates tests, minimal implementation and refactoring; orchestrator writes briefs, runs Red test, confirms failure reason, runs build and tests, reviews diff, returns defects to qwen, records authorship.
   - Check: `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`; `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

5. **S4 Serializable traversal**
   - Red: `ExportTypes_AreSerializable` (extended to the new types) fails because the new types are first created without `[Serializable]`.
   - Green: Add the attribute.
   - Refactor: None.
   - Roles: qwen generates tests, minimal implementation and refactoring; orchestrator writes briefs, runs Red test, confirms failure reason, runs build and tests, reviews diff, returns defects to qwen, records authorship.
   - Check: `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`; `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

6. **S5 DI registration**
   - Red: `AddWeatherParsing_RegistersListEntryOrdering` fails because the registration is missing.
   - Green: Register the ordering service in `ServiceCollectionExtensions.AddWeatherParsing`.
   - Refactor: None.
   - Roles: qwen generates tests, minimal implementation and refactoring; orchestrator writes briefs, runs Red test, confirms failure reason, runs build and tests, reviews diff, returns defects to qwen, records authorship.
   - Check: `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`; `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

7. **S6 Required members (C-27)**
   - Red: `ListRequiredMemberGuardTests` in `tests/WeatherParser.Parsing.Tests/Export/`: for each list root, one representative unassigned required scalar and one unassigned required string — `CdcStationListEntry.DistanceKm`, `CdcStationListEntry.StationId`, `SearchParameters.MaxDistanceKm`, `MosmixLocationListEntry.MosmixId`, `MosmixProductAvailability.Availability`, list `RetrievedAtUtc` and `Source` — each serialized with both `IJsonExportSerializer` and `IXmlExportSerializer`; assert `ExportSerializationException` and `stream.Length` 0 (destination untouched). Expected failure: the S1/S2 entities use plain properties, so the unassigned value is written as `0`, `null` or a default and no exception is thrown.
   - Characterization (not a Red): `SerializeAsync_ExplicitZeroValues_AreWritten` (both formats) with `Latitude = 0`, `Longitude = 0`, `DistanceKm = 0`, `AltitudeDifferenceM = 0` and `MaxAltitudeDifferenceM = 0` explicitly assigned — they are written as `0` and must keep passing after Green; `SerializeAsync_SchemaVersionNotAssigned_IsWrittenAsOne` (both formats) — `schema_version` is the constant `"1"` and needs no assignment.
   - Green: assignment tracking for every required member except `schema_version`, reusing the P2.02 pattern (getter throws `InvalidOperationException` for an unassigned value, which both serializers report as `ExportSerializationException`).
   - Refactor: share one internal presence-tracking value type for required scalars and strings instead of repeating fields.
   - Roles: qwen generates tests, minimal implementation and refactoring; orchestrator writes briefs, runs Red test, confirms failure reason, runs build and tests, reviews diff, returns defects to qwen, records authorship.
   - Check: `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`; `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

## 6. Tests and Test IDs
| Step-local case | T-P2 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P2-02 | REQ-023, REQ-067–068 | All CDC entry/list/search fields in both formats; `"00044"` kept; negative altitude difference; valid empty list |
| S2 | T-P2-03 | REQ-101–102, REQ-067 | Alphanumeric and leading-zero MOSMIX IDs kept; per-product availability including `unknown`; elements; metadata; valid empty list |
| S3 | T-P2-02, T-P2-03 | REQ-023, REQ-068, REQ-101 | Distance then ordinal ID; deterministic for identical input; empty input |
| S4 | T-P2-01 | REQ-074, REQ-076 | New types `[Serializable]` with public parameterless constructors (partial – completed in P2.08) |
| S5 | T-P2-19 | REQ-070–071, REQ-078 | Ordering service resolved through its interface (partial – completed in P2.10) |
| S6 | T-P2-02, T-P2-03, T-P2-17 | REQ-023, REQ-053, REQ-067, REQ-101 | Unassigned required members fail in both formats with an untouched destination; explicitly assigned zeros are written; `schema_version` needs no assignment. |

## 7. Evidence to Record
- In `docs/p2/P2_VERIFICATION.md`: Red names with observed failure messages, Green/Refactor results, commands, test counts, authorship; contract decisions and user approval date in `docs/p2/P2_EXPORT_CONTRACT.md`
- For each slice: the orchestrator records the Red test name, expected failure reason, Green result, and Refactor outcome
- The orchestrator records the final test run command and its output

## 8. Risks and Limitations
- (a) Availability vocabulary depends on facts verified only in P4/P7 — the contract fixes the shape, P4/P7 fill verified values, and unknown stays explicit
- (b) Ordering on unrounded values versus rounded display could look unsorted if a later phase rounds distances — the contract states that order follows the exported values
- (c) The strict-improvement coverage rule requires tests for every new block, including the ordering stub paths

## 9. Acceptance Criteria
- [x] S0 approval recorded before S1
- [x] Red failure recorded for S1–S6
- [x] Populated and empty lists of both kinds pass in both formats
- [x] IDs never converted to numbers
- [x] No geodesy, catalog or filter code added
- [x] Unassigned required members of every list root fail with `ExportSerializationException` in both formats and leave the destination untouched
- [x] Explicitly assigned zeros are written and `schema_version` needs no assignment
- [x] All tests pass

All criteria are evidenced in [P2_VERIFICATION.md](../../p2/P2_VERIFICATION.md) section 3 (slices S0–S6, coverage characterization, enum review fix) and by the hosted CI run 39396695630 of commit `0f59e1c`.
