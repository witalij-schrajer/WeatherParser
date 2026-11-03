# P4.03 – Coordinate Parsing and Search Parameters: Implementation Plan

**Status: complete (2026-10-22): implemented on 2026-10-21 (S1 to S4), committed as `a3b79d7`, hosted CI run 39737759157 green; records in [P4_VERIFICATION.md](../../p4/P4_VERIFICATION.md) section 3.** Original planning note: plan only on 2026-10-20. Refines the P4.03 row of section 5 and the "Coordinates" and "Search parameters" rows of section 4 of [PHASE_P4_PLAN.md](../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 6, 1, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0). Later: Codex-outage acceptance wording. The orchestrating agent swapped the Options and Recommended cells of the DMS-precision row itself after two failed precise briefs. Reconciled with the approved selection contract (SC-07, SC-11, SC-17) on 2026-10-21 (P4.02 S6). S6 record (2026-10-21): qwen reconciled this plan from the orchestrating agent's change list and fixed the confirmed review findings section by section; the orchestrating agent edited these parts itself after failed qwen attempts, for regressions or for recorded design decisions: S1 20/21-fractional-digit tests; S3 altitude-bound tests, Refactor and Check; the section 6 S3 row. Codex five-angle review rounds: 5, 3, 1, 2, 3, 1, 0 findings; cross-plan five-angle review of all P4 plans: rounds 4, 3, 1, 1, 0 findings.

## 1. Goal and Scope
Goal: an injectable coordinate parser for decimal degrees and DMS with hemisphere (REQ-021) and an injectable search-parameter service that validates the reference location and limits (REQ-020, REQ-022) and produces the existing P2 export entity `SearchParameters` with unchanged values, test-first, registered in `AddWeatherParsing`.

In scope:
- latitude/longitude parsing from strings (decimal and DMS per SC-07 of `docs/p4/P4_SELECTION_CONTRACT.md`);
- range checks (latitude −90 to +90, longitude −180 to +180, inclusive);
- minutes and seconds 0 inclusive to 60 exclusive;
- rejection of empty input, nonfinite tokens (for example `NaN`, `Infinity`), wrong hemisphere letters for the axis, conflicting sign and hemisphere, malformed input;
- invariant-culture interpretation independent of the thread culture;
- search-parameter validation (reference altitude may be negative; `MaxDistance` and `MaxAltitudeDifference` finite and ≥ 0, zero allowed);
- creation of `SearchParameters` (`latitude`, `longitude`, `altitude_m`, `max_distance_km`, `max_altitude_difference_m`) without rounding (C-26);
- English exception messages;
- DI registration (C-107) in the same step.

Out of scope:
- distance and filters → P4.04;
- catalogs → P4.05–P4.07;
- selection services that consume the parameters → P4.09;
- CLI option names and argument quoting → P3;
- logging → P4.09 (no notices are produced here).

## 2. Preconditions and Inputs
- P4.01 READY.
- P4.02 approved the coordinate-grammar, public-API and service-registration SC rows.

## 3. Decisions Required

| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Accepted spellings and conflicts | — | Decimal: `^[+-]?\d{1,3}(\.\d{1,20})?$` (at most 3 integer and 20 fractional digits, so the value is exact in `decimal` and the range check uses the exact value); no exponent, grouping, comma or inner whitespace; surrounding whitespace trimmed; invariant culture. DMS: `D° M′ [S″] H` with degree sign U+00B0 required, minute sign U+2032 or U+0027, second sign U+2033 or U+0022; D `\d{1,3}`, M `\d{1,2}` with 0–59, S `\d{1,2}(\.\d{1,15})?` with 0 ≤ S < 60 and `.` as separator, seconds optional; any longer token is rejected; spaces between components optional; hemisphere `N`/`S` for latitude, `E`/`W` for longitude, case-insensitive; any sign together with a hemisphere is rejected; DMS without hemisphere is rejected; other symbols (for example U+00BA) are rejected; every rejection is `CoordinateFormatException` `COORDINATE-INVALID`. (SC-07) | User (P4.02, approved 2026-10-21) | S1, S2 |
| Interface and type names | Proposed `ICoordinateParser`/`CoordinateParser` with `ParseLatitude(string)` and `ParseLongitude(string)` returning `decimal`; Proposed `ISearchParameterFactory`/`SearchParameterFactory` with `Create(decimal latitude, decimal longitude, decimal altitudeM, decimal maxDistanceKm, decimal maxAltitudeDifferenceM)` returning `SearchParameters` / names from the approved public-API SC row | `ICoordinateParser`/`CoordinateParser`, `ISearchParameterFactory`/`SearchParameterFactory` (SC-11) | User (P4.02, approved 2026-10-21) | S1–S3 |
| Failure type | Proposed `CoordinateFormatException` and `SearchParameterException` (public, English messages, input echoed only in sanitized form) / `ArgumentException` subclasses / types from the approved SC row | `CoordinateFormatException` with `ErrorId` `COORDINATE-INVALID`, `SearchParameterException` with `ErrorId` `SEARCH-PARAMETER-INVALID`, each exposing a read-only `ErrorId` string (SC-17) | User (P4.02, approved 2026-10-21) | S1–S3 |
| DMS precision | exact `decimal` arithmetic without rounding (C-26) / rounding to a fixed number of decimals | Full `decimal` precision of the conversion without additional rounding (C-26); expected values are derived independently before implementation (for example `9° 40′ 26″ E` = 9 + 40/60 + 26/3600 computed to 28 significant digits) and compared with an explicit tolerance of `0.0000000000000000000001` (1e-22); `50° 33′ 18″ N` must equal `50.555` exactly; a regression case detects premature rounding to fewer decimals | Orchestrator, recorded | S2 |
| Folder | `src/WeatherParser.Parsing/Geography/` (namespace `WeatherParser.Parsing.Geography`) / other | Proposed `Geography/` (phase plan section 8) | Orchestrator, recorded | S1 |

## 4. Artifacts

| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Geography/ICoordinateParser.cs` | Create | Interface with `ParseLatitude(string)` and `ParseLongitude(string)` returning `decimal` |
| `src/WeatherParser.Parsing/Geography/CoordinateParser.cs` | Create | Implementation of `ICoordinateParser` |
| `src/WeatherParser.Parsing/Geography/ISearchParameterFactory.cs` | Create | Interface with `Create(decimal latitude, decimal longitude, decimal altitudeM, decimal maxDistanceKm, decimal maxAltitudeDifferenceM)` returning `SearchParameters` |
| `src/WeatherParser.Parsing/Geography/SearchParameterFactory.cs` | Create | Implementation of `ISearchParameterFactory` |
| `src/WeatherParser.Parsing/Exceptions/CoordinateFormatException.cs` | Create | Exception type with read-only `ErrorId` property `COORDINATE-INVALID` (SC-17) |
| `src/WeatherParser.Parsing/Exceptions/SearchParameterException.cs` | Create | Exception type with read-only `ErrorId` property `SEARCH-PARAMETER-INVALID` (SC-17) |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Register both services as singletons by implementation type, C-107 |
| `tests/WeatherParser.Parsing.Tests/Geography/CoordinateParserTests.cs` | Create | Tests for coordinate parsing |
| `tests/WeatherParser.Parsing.Tests/Geography/SearchParameterFactoryTests.cs` | Create | Tests for search parameter creation |
| `tests/WeatherParser.Parsing.Tests/Exceptions/CoordinateFormatExceptionTests.cs` | Create | Tests for `CoordinateFormatException` properties and behavior |
| `tests/WeatherParser.Parsing.Tests/Exceptions/SearchParameterExceptionTests.cs` | Create | Tests for `SearchParameterException` properties and behavior |
| `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` | Change | Update if it lists registrations |
| `tests/WeatherParser.Parsing.Tests/Architecture/ServiceRegistrationCompletenessTests.cs` | Verify | Must stay green unchanged |
| `docs/p4/P4_VERIFICATION.md` | Append | P4.03 section |

## 5. Procedure

### S1 Decimal degrees
**Red**: 
- `ParseLatitude_DecimalDegrees_ReturnsExactValue` (`50.5549`)
- `ParseLongitude_DecimalDegrees_ReturnsExactValue` (`9.6738`)
- `ParseLatitude_AtInclusiveBounds_Accepts` (`-90`, `90`)
- `ParseLongitude_AtInclusiveBounds_Accepts` (`-180`, `180`)
- `ParseLatitude_OutOfRange_Throws` (`90.0001`, `-90.0001`)
- `ParseLongitude_OutOfRange_Throws` (`180.0001`, `-180.0001`)
- `Parse_NonFiniteOrEmpty_Throws` (`NaN`, `Infinity`, `-Infinity`, empty, whitespace)
- `Parse_DecimalWith20FractionalDigits_Accepted` (`50.55490000000000000001`, exactly 20 fractional digits, SC-07)
- `Parse_DecimalWith21FractionalDigits_Throws` (`50.554900000000000000001`, `COORDINATE-INVALID`)
- `Parse_DecimalWithMoreThan3IntegerDigits_Throws` (`0050.5549`)
- `Parse_ExponentGroupingOrComma_Throws` (`1e1`, `1,000`, `50,5549`)
- `Parse_UnderGermanCulture_UsesInvariantFormat` (thread culture `de-DE`: `50.5549` accepted as 50.5549, `50,5549` rejected)
- `AddWeatherParsing_ResolvesCoordinateParser` (container built with validation on, C-108)

Expected failure: the stub throws `NotImplementedException`.

**Green**: invariant-culture parsing with range checks.  
Add `ICoordinateParser` → `CoordinateParser` as a singleton by implementation type in `AddWeatherParsing` in the same slice; the existing `ServiceRegistrationCompletenessTests` turns red when the interface appears (record it as Red evidence) and green after the registration.

**Refactor**: 
- Consolidate parsing logic into a single method for decimal degrees.
- Ensure all range checks use exact `decimal` comparisons.
- Remove redundant or duplicated validation code.

**Roles**: qwen generates tests and code; the orchestrating agent writes briefs, runs Red, builds, tests and coverage, reviews and records authorship.

**Check**: restore, build and test commands run successfully with no regressions.

### S2 DMS with hemisphere
**Red**:
- `ParseLatitude_Dms_North_ReturnsDecimal` (`50° 33′ 18″ N` → `50.555`)
- `ParseLongitude_Dms_East_ReturnsDecimal` (`9° 40′ 26″ E`)
- `Parse_Dms_SouthAndWest_AreNegative`
- `Parse_Dms_AsciiApostropheAndQuote_Accepted` (ASCII apostrophe U+0027 and quote U+0022 accepted)
- `Parse_Dms_SecondsOptional_Accepted` (`50° 33′ N`)
- `Parse_Dms_LowercaseHemisphere_Accepted`
- `Parse_Dms_WithoutHemisphere_Throws`
- `Parse_Dms_AnySignWithHemisphere_Throws` (`+50° 33′ 18″ N` and `-50° 33′ 18″ S`)
- `Parse_Dms_MasculineOrdinalInsteadOfDegree_Throws` (U+00BA)
- `Parse_Dms_TooManyDigits_Throws`
- `Parse_Dms_MinutesOrSecondsAtSixty_Throws` (`60′`, `60″`; `59″` accepted)
- `Parse_Dms_WrongHemisphereForAxis_Throws` (`E`/`W` for latitude, `N`/`S` for longitude)
- `Parse_Dms_ConflictingSignAndHemisphere_Throws` (for example `-50° 33′ 18″ N`, and any sign the SC grammar forbids together with a hemisphere)
- `Parse_Dms_ResultOutOfRange_Throws` (`90° 0′ 1″ N`, `180° 0′ 1″ E`)
- `Parse_Dms_Malformed_Throws` (missing degree symbol, trailing text)
- `Parse_Dms_NegativeMinutesOrSeconds_Throws` (for example `50° -1′ 0″ N`)
- `Parse_Dms_ZeroMinutesAndSeconds_Accepted` (`50° 0′ 0″ N` → `50`)
- `Parse_Dms_AtPolesAndAntimeridian_Accepted` (`90° 0′ 0″ N`, `90° 0′ 0″ S`, `180° 0′ 0″ E`, `180° 0′ 0″ W`)
- `Parse_Dms_UnderGermanCulture_UsesInvariantFormat` (thread culture `de-DE`, DMS with fractional seconds)
- `Parse_Dms_TooManyDigitsInDegrees_Throws` (`0050° 33′ 18″ N`)
- `Parse_Dms_TooManyDigitsInMinutes_Throws` (`50° 033′ 18″ N`)
- `Parse_Dms_TooManyDigitsInSeconds_Throws` (`50° 33′ 018″ N`)
- `Parse_Dms_FractionalSeconds_Valid15Digits_Accepted` (`50° 33′ 18.123456789012345″ N`)
- `Parse_Dms_FractionalSeconds_Invalid16Digits_Throws` (`50° 33′ 18.1234567890123456″ N`)

Expected failure: the acceptance tests (`ParseLatitude_Dms_North_ReturnsDecimal`, `ParseLongitude_Dms_East_ReturnsDecimal`, `Parse_Dms_SouthAndWest_AreNegative`, `Parse_Dms_AtPolesAndAntimeridian_Accepted`, `Parse_Dms_ZeroMinutesAndSeconds_Accepted`, `Parse_Dms_AsciiApostropheAndQuote_Accepted`, `Parse_Dms_FractionalSeconds_Valid15Digits_Accepted`) fail because the S1 implementation rejects all DMS input. The rejection tests already pass against S1 (it rejects every DMS string); they are recorded as existing behavior, not as Red, and stay as regression checks while DMS support is added.

**Green**: DMS grammar of SC-07, conversion degrees + minutes/60 + seconds/3600 in `decimal`.

**Refactor**: 
- Consolidate parsing logic for DMS strings into a shared helper method.
- Ensure all DMS components are validated for range and format before conversion.
- Apply invariant culture formatting during parsing to avoid locale-specific issues.

**Roles**: qwen generates tests and code; the orchestrating agent writes briefs, runs Red, builds, tests and coverage, reviews and records authorship.

**Check**: 
- Restore, build, and test the solution.
- Run all coordinate-related unit tests including DMS acceptance and rejection cases.
- Confirm that no regressions were introduced in decimal or DMS parsing.

### S3 Search parameters
**Red**:
- `Create_ValidInput_ReturnsSearchParametersWithUnchangedValues` (all five fields read back exactly)
- `Create_NegativeReferenceAltitude_Accepted` (`-12.5`)
- `Create_AllZeroValues_ReturnsAllFieldsReadable` (latitude, longitude, altitude and both limits `0`; all five properties are read back as `0`, which detects an unassigned `RequiredMember`)
- `Create_NegativeMaxDistance_Throws`
- `Create_NegativeMaxAltitudeDifference_Throws`
- `Create_ReferenceAltitudeAtRepresentableBounds_Accepted` (SC-11: the difference to every accepted catalog altitude from −500 to 9000 m must be representable in `decimal`; reference altitudes `decimal.MaxValue - 500m` and `decimal.MinValue + 9000m` are accepted)
- `Create_ReferenceAltitudeBeyondRepresentableBounds_Throws` (reference altitudes `decimal.MaxValue - 499m` and `decimal.MinValue + 8999m` → `SearchParameterException` `SEARCH-PARAMETER-INVALID`)
- `Create_LargeLimits_Accepted` (SC-11 validation policy: nonnegative limits have no additional upper caps; test with limit `1000000m` and `1000000km`)
- `Create_LatitudeOrLongitudeOutOfRange_Throws` (latitude `-90.0001`/`90.0001`, longitude `-180.0001`/`180.0001`, supplied as numbers)
- `Create_CoordinatesAtInclusiveBounds_Accepted` (latitude `-90`/`90`, longitude `-180`/`180`; all five properties read back unchanged)
- `AddWeatherParsing_ResolvesSearchParameterFactory` (container built with validation on, C-108)

Expected failure: stub throws `NotImplementedException`.

**Green**: validation and `SearchParameters` creation; no rounding.  
Nonfinite arguments cannot occur with `decimal` parameters; nonfinite rejection is tested at the string parser boundary (S1).  
Add `ISearchParameterFactory` → `SearchParameterFactory` as a singleton by implementation type in `AddWeatherParsing` in the same slice; the existing `ServiceRegistrationCompletenessTests` turns red when the interface appears (record it as Red evidence) and green after the registration.

**Refactor**: remove duplication with all tests green; the factory and the P4.09 selectors share one validation policy (SC-11).

**Roles**: qwen generates tests and code; the orchestrating agent writes briefs, runs Red, builds, tests and coverage, reviews and records authorship.

**Check**:
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S4 Review and evidence
The orchestrating agent runs build, all tests and the coverage procedure of CONTRIBUTING.md section 5, writing the changed-file list from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard` so uncommitted and untracked step files are included; records the native covered/total block counters of `WeatherParser.Parsing.dll`, the baseline counters and the gate result. Codex reviews the uncommitted diff read-only; qwen fixes confirmed findings test-first; checkpoint commit when the user requests it, then push to the draft pull request and record the hosted CI run.

**Roles**: 
- qwen generates the tests first, then the minimal implementation and refactoring, from briefs with the real declarations (`SearchParameters`, `ServiceCollectionExtensions`, the approved SC rows);
- the orchestrating agent writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff against AGENTS.md, REQUIREMENTS.md and the SC rows, returns defects to qwen and records authorship;
- the briefs require MSTest `[TestClass]`/`[TestMethod]`, FluentAssertions, the subject built in `[TestInitialize]`, a shared strict `MockRepository` with `VerifyAll()` in `[TestCleanup]` where mocks are used, and restoration of a changed thread culture in `finally` or `[TestCleanup]`; the orchestrating agent checks these conventions in review.

**Check**: 
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

## 6. Tests and Test IDs

| Step-local case | T-P4 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P4-01 | REQ-021, REQ-082 | Decimal degrees, inclusive bounds, out of range, nonfinite and empty input, culture independence; test boundary values for 3 integer and 20 fractional digits (SC-07) |
| S2 | T-P4-02 | REQ-021, REQ-082 | DMS with N/S/E/W, minutes/seconds 0 ≤ x < 60 including negative and zero values, poles and antimeridian, wrong hemisphere, conflicting sign, `50° 33′ 18″ N`, `9° 40′ 26″ E`, range after conversion, culture independence; test boundary values for degree, minute and second formats (SC-07) |
| S3 | T-P4-03 | REQ-020, REQ-022 | Negative reference altitude allowed, limits ≥ 0 including zero, all five values readable and unchanged in `SearchParameters`; validation policy: altitude differences against every accepted catalog altitude from −500 to 9000 m must remain representable in `decimal`, and nonnegative limits have no additional upper caps; concrete large-limit inputs: 1000000 m (accepted), 1000000 km (accepted), 500 km (accepted); reference altitudes `decimal.MaxValue - 500m` and `decimal.MinValue + 9000m` accepted, `decimal.MaxValue - 499m` and `decimal.MinValue + 8999m` rejected (SC-11) |
| S1, S2, S3 | T-P4-14 (partial) | REQ-016 | Services resolved through their interfaces; registration completeness (C-107) green at the end of each slice |
| S1–S3 | T-P4-15 | REQ-085 | Recorded Red → Green → Refactor evidence |

## 7. Evidence to Record
- Red test names with observed failure messages
- Green/Refactor results
- Commands with exit codes
- Test counts
- Native covered/total block counters, baseline counters and gate result (pre-commit run), and the committed-revision CI result after an authorized commit
- Codex review run and finding dispositions
- Authorship

## 8. Risks and Limitations
- (a) Unicode look-alike symbols (for example `º` or typographic quotes) may be typed by users — only the spellings approved in the SC row are accepted, others fail with a clear message;
- (b) non-terminating DMS fractions in `decimal` — expected values are derived independently with the 1e-22 tolerance of section 3; a premature-rounding regression case guards the precision;
- (c) culture leaks through `decimal.Parse` overloads without `CultureInfo.InvariantCulture` — the German-culture test guards it;
- (d) exception messages echoing raw input could carry control characters into logs — messages describe the reason and sanitize the input.

## 9. Acceptance Criteria
- [x] Each behavior increment has recorded Red evidence with its expected failure; rejection tests that already passed are recorded as existing behavior
- [x] All T-P4-01 to T-P4-03 cases pass
- [x] No rounding of coordinates or limits
- [x] Both services registered and `ServiceRegistrationCompletenessTests` green
- [x] Build without warnings
- [x] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user (AGENTS.md)
- [x] No distance, catalog or CLI code added
- [x] `ErrorId` values asserted for every failure
