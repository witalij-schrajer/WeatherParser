# P7.05 – Element Mapping and Series Preparation: Implementation Plan

**Status: planned (2026-10-26); not started. Written before the P7.02 contract; it is reconciled with the approved contract in P7.02 S6 before its first Red test.** Refines the P7.05 row of section 5 and the "Values" and "Times" rows of section 4 of [PHASE_P7_PLAN.md](../../PHASE_P7_PLAN.md).

## 1. Goal and Scope
Goal: an injectable mapper that turns one validated raw forecast document (P7.04) of one product into a `ForecastRunInput` for `IForecastRunBuilder`: every approved element mapped to its quantity kind with the verified source unit converted to the unit the builder expects, interval quantities given their verified periods, missing tokens kept as `null` at their own index, and absent mapped elements reported — without changing any value's time.
In scope: the approved element table of MF-10 and MF-11 (element name, quantity kind, source unit, scaling to the builder's unit, period length, pressure reference); unit scaling (K → °C, Pa → hPa, kJ/m² → J/m², kg/m² → mm, m/s kept for the builder's wind conversion); periods `[valid_at − h, valid_at]` (MF-13); run identity (product, MOSMIX ID, issue time) on the input and on every series (C-75); the location of MF-08; the freshness input of MF-09 when P7.03 added it; the notice data for absent mapped elements (`MOSMIX-ELEMENT-ABSENT`); the MF-18 vocabulary check; registration.
Out of scope with owner: freshness computation and logging → P7.07, P7.08 (the mapper takes the freshness result as input); validation, conversion to km/h, rate conversion, rounding → the existing builder (C-77); retrieval → P7.06.

## 2. Preconditions and Inputs
- P7.02 approved MF-08 and MF-10 to MF-13 and MF-18 with the element table; P7.03 complete (the approved additive kinds and members exist, or are recorded as not applicable); P7.04 complete (`MosmixForecastDocument`).
- Existing: `ForecastRunInput`, `ForecastSeries`, `QuantityKind`, `PressureUnit`, `PressureReference`, `WindSpeedUnit`, `AveragingPeriod`, `IPeriodRateConverter` (RadiationEnergy in J/m², PrecipitationAccumulation in mm, C-45), `ForecastLocation`.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Interface and types (Proposed) | `IMosmixElementMapper` with `MosmixMappingResult Map(MosmixForecastDocument document, MosmixProduct product, string mosmixId, DateTime retrievedAtUtc, <freshness input>)`; result: `Input` (`ForecastRunInput`), `AbsentElements` (mapped element names missing from the document) and `NotExportedElements` (element names present in the document but not approved for export, each with reason `NotExportable`) | Proposed; a pure synchronous mapping, no I/O | Orchestrator, recorded | S1 |
| Element table | A static table in code derived from the contract record, one row per approved element / configuration by the consumer (rejected: the mapping is a library contract) | Static table; a test compares it with the contract table row by row | User (MF-10, MF-11) | S1, S2 |
| Unit scaling (Proposed) | Behind an injectable converter (Proposed `IMosmixUnitScaler`) with one method per scaling / inside the mapper | Injectable, because it is executable logic reused by both products (REQ-078); scaling only, never rounding (REQ-057) | Orchestrator, recorded | S1 |
| Product element sets | One table with per-product presence from the P7.02 record / two tables | One table; an element absent from a product's file is reported, never borrowed from the other product (REQ-109) | User (MF-10) | S2 |
| Diagnostics for not-approved elements | One `NotExportable` entry per element (not per step, so a MOSMIX_L file with about 100 unmapped elements and 247 steps does not produce some 25 000 entries) carried by the mapping result, returned by the parsers beside the builder's omissions and logged once per element as `FORECAST-VALUE-OMITTED` with kind and reason / one `OmittedForecastValue` per step and element | Per element; the per-step form is rejected for its volume; recorded with MF-12 and MF-15 in P7.02 | User (P7.02) | S2 |
| Vocabulary check (MF-18) | Test that every table element appears in the recorded element definitions with the recorded unit, and a recorded comparison with the P4 availability vocabulary | As stated; a mismatch is a P4 amendment proposal for the user, not a code change in this step | User (MF-18) | S3 |

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
**Red**: `Scale_Kelvin_ToCelsius` (273.15 → 0, 253.15 → −20, 0 K boundary), `Scale_Pascal_ToHectopascal` (101325 → 1013.25), `Scale_KiloJoule_ToJoule`, `Scale_NonFinite_Throws` (defensive; source tokens are finite), `Map_Temperature_SeriesInCelsius`, `Map_Pressure_HectopascalWithRecordedReference` (`sea_level` only when the contract records the reduction, otherwise `unknown`), `Map_WindDirection_Degrees`, `Map_WindSpeed_MetresPerSecondForBuilder`, `Map_RunIdentityOnInputAndEverySeries` (C-75 provenance), `Map_Location_FromDocument`, `Map_HumidityAndIlluminance_NoSeries` (no source element, no zero-filled series).
**Green**: scaler and point mapping; registration of both interfaces in this slice, `ServiceRegistrationCompletenessTests` red while unregistered.

### S2 Interval elements, missing values and absences
**Red**: `Map_Rad1h_RadiationEnergyWithHourPeriod` (period `[valid_at − 1 h, valid_at]` at every index), `Map_RR1c_AccumulationWithHourPeriod`, `Map_ApprovedForecastOnlyElements_OwnKindsAndPeriods` (MF-11, if approved; gust and amount periods as recorded), `Map_NotApprovedElements_NoSeriesAndNotExportedEntry` (probabilities, codes, cloud cover never mapped to another quantity; each listed once in `NotExportedElements` with reason `NotExportable`, in document order), `Map_ApprovedElements_NeverInNotExported`, `Map_DashTokens_NullAtOwnIndex` (first, middle, last index; later values unshifted), `Map_FirstStepPeriodBeforeIssueTime_Kept` (MF-13, recorded, not corrected), `Map_AbsentMappedElement_ReportedNotBorrowed`, `Map_AllMappedElementsAbsent_EmptySeriesList` (the builder rejects it later), `Map_TimelinePassedUnchanged`.
**Green**: interval mapping and absence reporting.

### S3 Composed mapping and vocabulary
**Red (composed, real builder)**: `Composed_ValidDocument_RunWithConvertedValues` (°C, hPa and mmHg from one unrounded value, km/h, W/m² and mm/h means with periods), `Composed_ZeroValues_Kept` (0 °C, 0 mm, calm wind per C-42 rules), `Composed_OutOfRange_OmittedNotClamped` (negative wind, pressure outside C-42), `Composed_WindDirectionLimits` (parameterized through the mapper and the real builder: 0 kept as 0, 360 normalized to 0 (C-43), the smallest step below 0 and above 360 omitted with `OutOfRange`, the other quantities of the step kept in every case), `Composed_OnlyMissingAndOutOfRange_RejectedWithOmissions` (P7.03 S1 member), `Composed_HorizonEqualsTimeline` (short and long synthetic timelines).
**Characterization**: `ElementTable_MatchesContractTable` (every row: element, kind, unit, period, reference), `ElementTable_ElementsInRecordedDefinitions` (MF-18).
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
