# P7.05 – Element Mapping and Series Preparation: Implementation Plan

**Status: planned (2026-10-26); reconciled on 2026-10-26 with the approved [P7 forecast contract](P7_FORECAST_CONTRACT.md) (P7.02 S6); not started.** Refines the P7.05 row of section 5 and the "Values" and "Times" rows of section 4 of [PHASE_P7_PLAN.md](../../PHASE_P7_PLAN.md).

## 1. Goal and Scope
Goal: an injectable mapper that turns one validated raw forecast document (P7.04) of one product into a `ForecastRunInput` for `IForecastRunBuilder`: every approved element mapped to its quantity kind with the verified source unit converted to the unit the builder expects, interval quantities given their verified periods, missing tokens kept as `null` at their own index, and absent mapped elements reported — without changing any value's time.
In scope: the approved element table of MF-10 and MF-11 (element name, quantity kind, source unit, scaling to the builder's unit, period length, pressure reference); unit scaling (K → °C, Pa → hPa, kJ/m² → J/m², kg/m² → mm, m/s kept for the builder's wind conversion); periods `[valid_at − h, valid_at]` (MF-13); run identity (product, MOSMIX ID, issue time) on the input and on every series (C-75); the location of MF-08; the freshness input of MF-09 when P7.03 added it; the notice data for absent mapped elements (`MOSMIX-ELEMENT-ABSENT`); the MF-18 vocabulary check; registration.
Out of scope with owner: freshness computation and logging → P7.07, P7.08 (the mapper takes the freshness result as input); validation, conversion to km/h, rate conversion, rounding → the existing builder (C-77); retrieval → P7.06.

## 2. Preconditions and Inputs
- P7.02 approved MF-08 and MF-10 to MF-13 and MF-18 with the element table on 2026-10-26: `TTT` → `Temperature` (K − 273.15), `PPPP` → `Pressure` (Pa ÷ 100, `Hectopascal`, reference `Unknown`), `DD` → `WindDirection`, `FF` → `MeanWindSpeed` (`MetresPerSecond`), `FX1` → `WindGust` (`MetresPerSecond`, period one hour), `Rad1h` → `RadiationEnergy` (kJ/m² × 1000 = J/m², period one hour), `RR1c` → `PrecipitationAccumulation` (kg/m² = mm, period one hour; the builder derives both the rate and the amount); P7.03 complete (the builder amendments of MF-11 exist); P7.04 complete (`MosmixForecastDocument`).
- Existing: `ForecastRunInput`, `ForecastSeries`, `QuantityKind`, `PressureUnit`, `PressureReference`, `WindSpeedUnit`, `AveragingPeriod`, `IPeriodRateConverter` (RadiationEnergy in J/m², PrecipitationAccumulation in mm, C-45), `ForecastLocation`.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Interface and types (Proposed) | `IMosmixElementMapper` with `MosmixMappingResult Map(MosmixForecastDocument document, MosmixProduct product, string mosmixId, DateTime retrievedAtUtc, <freshness input>)`; result: `Input` (`ForecastRunInput`), `AbsentElements` (mapped element names missing from the document) and `NotExportedElements` (names of the elements present in the document but not mapped, in document order; MF-02) | Proposed; a pure synchronous mapping, no I/O | Orchestrator, recorded | S1 |
| Element table | A static table in code derived from the contract record, one row per approved element / configuration by the consumer (rejected: the mapping is a library contract) | Static table; a test compares it with the contract table row by row | User (MF-10, MF-11) | S1, S2 |
| Unit scaling (Proposed) | Behind an injectable converter (Proposed `IMosmixUnitScaler`) with one method per scaling / inside the mapper | Injectable, because it is executable logic reused by both products (REQ-078); scaling only, never rounding (REQ-057) | Orchestrator, recorded | S1 |
| Product element sets | One table with per-product presence from the P7.02 record / two tables | One table; an element absent from a product's file is reported, never borrowed from the other product (REQ-109) | User (MF-10) | S2 |
| Diagnostics for not-mapped elements | Approved (MF-02): the element name once per element in `NotExportedElements` (most unmapped elements have no `QuantityKind`, so they cannot be `OmittedForecastValue` entries, and a per-step form would produce some 25 000 entries for a MOSMIX_L file), returned by the parsers beside the builder's omissions and logged once per element as `MOSMIX-ELEMENT-NOT-EXPORTED` | As approved | User (MF-02, MF-14, approved) | S2 |
| Vocabulary check (MF-18) | Approved: a test takes the complete P4 availability vocabulary (the 114 MOSMIX_L and the 40 MOSMIX_S names of the P4 contract section 3.3) and the mapped names and asserts that each is among the 137 recorded `ShortName` values of the retrieved `MetElementDefinition.xml` (a test constant without definition texts), and that every mapped name is in its product's vocabulary; the P7.02 check found no mismatch | As approved; a later mismatch would be a P4 amendment proposal for the user, not a code change in this step | User (MF-18, approved) | S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Forecasts/Mosmix/IMosmixElementMapper.cs`, `MosmixElementMapper.cs`, `MosmixMappingResult.cs`, `MosmixElementTable.cs`, `IMosmixUnitScaler.cs`, `MosmixUnitScaler.cs` | Create | Mapper, table, scaler |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration |
| `tests/WeatherParser.Parsing.Tests/Forecasts/Mosmix/MosmixElementMapperTests.cs`, `MosmixUnitScalerTests.cs`, `MosmixComposedMappingTests.cs` | Create | Unit tests and composed tests through the real builder |
| `docs/p7/P7_VERIFICATION.md` | Change | P7.05 section with the MF-18 comparison |

## 5. Procedure
Every slice: roles and checks as in P7.03 section 5.

### S1 Unit scaling and point elements
**Red**: `Scale_Kelvin_ToCelsius` (273.15 → 0, 253.15 → −20, 0 K boundary), `Scale_Pascal_ToHectopascal` (101325 → 1013.25), `Scale_KiloJoule_ToJoule`, `Scale_KilogramPerSquareMetre_ToMillimetreUnchanged`, `Scale_NonFinite_Throws` (defensive; source tokens are finite), `Map_Temperature_SeriesInCelsius`, `Map_Pressure_HectopascalWithUnknownReference` (MF-10: `unknown`, never `sea_level`), `Map_WindDirection_Degrees`, `Map_WindSpeed_MetresPerSecondForBuilder`, `Map_RunIdentityOnInputAndEverySeries` (C-75 provenance), `Map_Location_FromDocument`, `Map_HumidityAndIlluminance_NoSeries` (no source element, no zero-filled series; `Td` is not used to derive humidity, MF-10).
**Green**: scaler and point mapping; registration of both interfaces in this slice, `ServiceRegistrationCompletenessTests` red while unregistered.

### S2 Interval elements, missing values and absences
**Red**: `Map_Rad1h_RadiationEnergyWithHourPeriod` (period `[valid_at − 1 h, valid_at]` at every index), `Map_RR1c_AccumulationWithHourPeriod`, `Map_FX1_WindGustWithHourPeriodAndMetresPerSecond` (MF-11), `Map_RR1c_SingleAccumulationSeries` (no second series for the amount; the builder derives it), `Map_NotMappedElements_NoSeriesAndListedByName` (`FX3`, `RR3c`, probabilities, codes and cloud cover never mapped to another quantity; each name listed once in `NotExportedElements`, in document order), `Map_ApprovedElements_NeverInNotExported`, `Map_DashTokens_NullAtOwnIndex` (first, middle, last index; later values unshifted), `Map_FirstStepPeriodBeforeIssueTime_Kept` (MF-13, recorded, not corrected), `Map_AbsentMappedElement_ReportedNotBorrowed`, `Map_AllMappedElementsAbsent_EmptySeriesList` (the builder rejects it later), `Map_TimelinePassedUnchanged`.
**Green**: interval mapping and absence reporting.

### S3 Composed mapping and vocabulary
**Red (composed, real builder)**: `Composed_ValidDocument_RunWithConvertedValues` (°C, hPa and mmHg from one unrounded value with `pressure_reference` `unknown`, km/h, W/m² and mm/h means, the gust maximum in km/h and the precipitation total in mm, each with its one-hour period), `Composed_ZeroValues_Kept` (0 °C, 0 mm, calm wind per C-42 rules), `Composed_OutOfRange_OmittedNotClamped` (negative wind, pressure outside C-42), `Composed_WindDirectionLimits` (parameterized through the mapper and the real builder: 0 kept as 0, 360 normalized to 0 (C-43), the smallest step below 0 and above 360 omitted with `OutOfRange`, the other quantities of the step kept in every case), `Composed_OnlyMissingAndOutOfRange_RejectedWithOmissions` (P7.03 S1 member), `Composed_HorizonEqualsTimeline` (short and long synthetic timelines).
**Characterization**: `ElementTable_MatchesContractTable` (every row: element, kind, unit, period, reference). **Red**: `Vocabulary_P4NamesAndMappedNames_AllDefined` and `Vocabulary_MappedNamesInProductVocabulary` (MF-18; Red while the recorded name constants do not exist).
**Green**: fixes only where a composed test fails.

**Refactor**: remove duplication with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P7 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1, S3 | T-P7-08 | REQ-052, REQ-053, REQ-054, REQ-057, REQ-106 | Point mapping, ranges, zero, rounding later |
| S2 | T-P7-09, T-P7-10 | REQ-055, REQ-044, REQ-106 | Interval periods; no relabelling |
| S2, S3 | T-P7-11, T-P7-12 | REQ-107, REQ-051, REQ-110 | Alignment, absences, horizon |
| S1 | T-P7-07 (mapping) | REQ-105 | Location carried |
| S3 | T-P7-23 | REQ-102 | Vocabulary check |
| all | T-P7-18 (registration), T-P7-20 | REQ-078, REQ-085, REQ-086 | Registration; Red evidence; coverage |

## 7. Evidence to Record
- Red names and failures, counts, coverage, authorship, Codex rounds; the MF-18 comparison result

## 8. Risks and Limitations
- (a) A DWD element definition changes after P7.02 — the table follows the recorded revision; a change needs a new contract decision
- (b) The first interval of a run may start before `issued_at` (recorded, MF-13)

## 9. Acceptance Criteria
- [ ] Every listed new-behavior test green with recorded Red; characterization tests (refactoring guards and existing behavior) green before and after and recorded as characterization, never with a manufactured Red; composed tests through the real builder green
- [ ] No value moved to another time; no quantity relabelled; no zero-filled absence
- [ ] Element table equal to the contract table
- [ ] Registration and architecture tests green; coverage gate passed; Codex 0 findings or reported
