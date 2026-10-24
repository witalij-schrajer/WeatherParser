# P5.08 – Export Equivalence and Schema Validation: Implementation Plan

**Status: plan only (2026-10-24); nothing is implemented or verified yet.** Refines the P5.08 row of section 5 and the "Export" row of section 4 of [PHASE_P5_PLAN.md](../../PHASE_P5_PLAN.md).

## 1. Goal and Scope
Goal: prove that the `ObservationDataset` produced by the P5 pipeline serializes through both real serializers to equivalent, schema-valid documents that carry the UTC forms, omitted members, periods, quality strings, pressure reference and rounded values the contract requires, without any further retrieval.
In scope: contract tests over the composed pipeline (real resolver, mapper, builder; mocked `ICatalogDownloader` with synthetic lists and archives of all four products); JSON ↔ XML equivalence; XML schema validation; wire checks; no production change expected (characterization); defects are fixed test-first in the owning step.
Out of scope with owner: serializer or schema changes → none planned (a defect in them is a P2 regression fixed test-first with a recorded additive decision if the contract is touched, C-03); DI and package consumer → P5.09; CLI output → P3.

## 2. Preconditions and Inputs
- P5.07 complete; the composed pipeline tests of P5.07 S5 pass.
- Existing helpers: `ExportSchemaValidation.Validate` (reusable as it is) and the private equivalence routines of the P2 contract tests (extracted into a shared helper first, section 3); the v1 schema in `schemas/export/v1/`.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Test placement | `tests/WeatherParser.Parsing.Tests/Observations/Dwd/DwdObservationExportContractTests.cs` reusing the P4 helpers / new helpers | Extract the private comparison routines (`AllRootsEquivalenceTests.AssertEquivalent`, `ObservationExportEquivalenceTests.FlattenJson` and `FlattenXml`) into a shared internal test helper `tests/WeatherParser.Parsing.Tests/Export/ExportEquivalence.cs` (Proposed) first, switching the existing tests to it and keeping them green; `ExportSchemaValidation.Validate` is reused as it is | Orchestrator, recorded | S1 |
| Expected values | computed from the synthetic inputs by hand in the brief (for example `36.0` J/cm² over 600 s → `600` W/m², `3.47` m/s → `12` km/h, `1013.25` hPa → `1013`/`760`) / derived at test time | Hand-computed literals in the tests (no implementation mirroring) | Orchestrator, recorded | S1, S2 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `tests/WeatherParser.Parsing.Tests/Observations/Dwd/DwdObservationExportContractTests.cs` | Create | Equivalence, schema and wire tests |
| `tests/WeatherParser.Parsing.Tests/Observations/Dwd/DwdObservationSyntheticInputs.cs` | Create | Synthetic lists and archives for one station with all four products, one with a missing product, one with calm wind, one with only-missing columns (SC-14, no copied DWD rows) |
| `tests/WeatherParser.Parsing.Tests/Export/ExportEquivalence.cs` | Create | Shared equivalence helper extracted from the private routines |
| `tests/WeatherParser.Parsing.Tests/Export/AllRootsEquivalenceTests.cs`, `tests/WeatherParser.Parsing.Tests/Observations/ObservationExportEquivalenceTests.cs` | Change | Use the shared helper; behavior unchanged |
| `docs/p5/P5_VERIFICATION.md` | Change | P5.08 section |

## 5. Procedure
### S1 Equivalence and schema
**Red**
- No Red: characterization over the P5.07 pipeline; `Export_FullStation_JsonAndXmlEquivalent`; `Export_FullStation_XmlValidAgainstV1Schema`; `Export_MissingProduct_JsonAndXmlEquivalent`; `Export_SerializationNeedsNoFurtherRetrieval` (the downloader mock records exactly the parse's requests; both serializations add none); tests expected to pass on first run are recorded as existing behavior; a failure is a defect of P5.05 to P5.07 or of the P2 serializers, fixed test-first in the owning step.

**Green**
- no change expected.

**Refactor**
- none.

**Roles**
- qwen generates the tests and synthetic inputs from the orchestrating agent's brief with hand-computed expected values; the orchestrating agent checks every expected value, runs the tests and records which passed on first run.

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`; `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S2 Wire details
**Red**
- No Red: characterization; `Export_MeasuredAtPerQuantity_UtcFormWithoutZ` (`YYYY-MM-DDTHH:mm:ss`, differing times per quantity); `Export_DatasetMeasuredAt_IsLatest`; `Export_RetrievedAt_WholeSeconds`; `Export_OmittedQuantities_Absent` (no `illuminance_lux`; a missing-product quantity absent in JSON and XML); `Export_Periods_StartAndEndOnWindPrecipitationRadiation`; `Export_NoPeriodOnTemperatureHumidityPressure`; `Export_Quality_PerContract` (OC-07 form on every exported value); `Export_PressureReference_Sensor_BothUnits` (`1013.25` → `1013` hPa and `760` mmHg); `Export_Rounding_PerREQ057` (temperature `20.25` → `20.3`, wind `3.47` m/s → `12`, humidity `50.5` → `51`); `Export_Irradiance_FromEnergyAndPeriod` (`36.0` J/cm² → `600` W/m²); `Export_RainRate_FromAccumulationAndPeriod` (`0.5` mm → `3` mm/h); `Export_CalmWind_DirectionPerContract`; `Export_UmlautsAndLeadingZeros_Preserved` (`Würzburg`, `00044`, `dwd:00044`).

**Green**
- no change expected.

**Refactor**
- none.

**Roles**
- as in S1.

**Check**
- as in S1.

### S3 Review and evidence
**Red**
- No Red: review slice.

**Green**
- build, all tests, pre-commit coverage with the changed-file list from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard`, native block counters, baseline and gate result (test-only changes leave the library counters unchanged; the gate must still pass).

**Roles**
- the orchestrating agent runs the checks; Codex reviews the uncommitted diff read-only; qwen fixes confirmed findings test-first; checkpoint commit when the user requests it; push; hosted CI run recorded.

**Check**
- pre-commit coverage (CONTRIBUTING.md section 5); Codex review; commit and CI run recorded in `docs/p5/P5_VERIFICATION.md`.

## 6. Tests and Test IDs
| Step-local case | T-P5 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P5-12 | REQ-067, REQ-068, REQ-069, REQ-079 | JSON ↔ XML equivalence; schema-valid; one complete document; no further retrieval |
| S2 | T-P5-12 | REQ-051 | UTC forms per quantity and dataset |
| S2 | T-P5-05, T-P5-06, T-P5-07, T-P5-08 (wire) | REQ-052, REQ-054, REQ-055, REQ-057, REQ-053, REQ-032 | Rounded outputs, both pressures with reference, periods with start and end, conversions with period, calm wind, quality on the wire |
| S1–S2 | T-P5-15 | REQ-085 | Characterization recorded with provenance (tests that passed on first run) |

## 7. Evidence to Record
- Expected values with their derivation; tests that passed on first run (characterization) and any defect found with its owning step
- Commands, test counts, native block counters and gate result
- Codex review and dispositions; authorship

## 8. Risks and Limitations
- (a) A schema or serializer defect surfaces here — it is a P2 regression, fixed test-first; a contract change would need a recorded additive decision (C-03)
- (b) Hand-computed expected values can be wrong — each derivation is recorded and checked by the orchestrating agent
- (c) Test-only changes do not raise library coverage — the gate compares the unchanged library counters and must still pass

## 9. Acceptance Criteria
- [ ] All T-P5-12 cases pass; every wire case of T-P5-05 to T-P5-08 passes
- [ ] No production change, or every change is a test-first defect fix recorded in the owning step
- [ ] Expected values recorded with derivation
- [ ] Build without warnings; pre-commit coverage gate passed
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user
- [ ] No DI, README or package change added
