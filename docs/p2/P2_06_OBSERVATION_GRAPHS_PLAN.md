# P2.06 – Observation Graphs: Implementation Plan

**Status: implemented on `features/develop/P2` (2026-10-20), not yet committed; contract decisions C-48 to C-65 extend this plan (graph validation C-64, converter amendments C-63/C-65); evidence in [P2_VERIFICATION.md](P2_VERIFICATION.md) section 6.** Original planning note: this document started as a plan on 2026-10-20. This plan refines the P2.06 row of section 5, the Observations row of section 4 and the "Observation time semantics" and "Quantity/quality/period/error shape" decisions of section 3 of [PHASE_P2_PLAN.md](../../PHASE_P2_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief and corrected by qwen in one review round (goal label, test path, verification artifact row, evidence bullet).

## 1. Goal and Scope
Goal: The complete observation export graph — dataset metadata, location, all REQ-052 quantities with their own timestamps, quality, source-timezone, pressure-reference and period metadata — and an interface-based builder that assembles it from P2.04 time results and P2.05 quantity services, applies omission and output rounding, enforces the dataset-time rule and rejects datasets without usable readings; proven with both real serializers.

In scope:
- Extending the P2.02 slice types into the full graph
- Per-quantity value entity
- Location entity
- Builder service with interface
- Builder result with diagnostics for omitted readings
- DI registration
- Contract-record updates

Out of scope:
- DWD CSV/ZIP parsing, `-999` markers and quality-flag meanings → P5
- OsthessenWetter page parsing and fixtures → P6
- Forecasts → P2.07
- Envelope, multiple datasets and typed errors in one document → P2.08
- XSDs → P2.09
- MQTT messages → P8
- Logging of omitted readings → caller (P5/P6, P3)

Prerequisites: P2.02 (slice graph, serializers), P2.04 (time normalization), P2.05 (rounding, pressure, validator, period converter, kinds) complete.

## 2. Preconditions and Inputs
- P2.02 completed with slice graph and real serializers
- P2.04 completed with time normalization services
- P2.05 completed with quantity services
- Existing repository facts: `src/WeatherParser.Parsing/`, `tests/WeatherParser.Parsing.Tests/`
- Required user authorization for contract decisions in section 3

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Measurement shape | `measurements` object with one typed property per REQ-052 field, each a value object (`value`, `measured_at`, `timestamp_quality`, optional `source_timezone`, optional `period`, optional `quality`) / a list of entries with a `name` field | Typed property per field: keeps the required field names in both formats and avoids untyped name/value lists | User (contract review) | S1 |
| Pressure representation | two value objects `pressure_hpa` and `pressure_mmhg`, each with the same `measured_at` and `pressure_reference` / one `pressure` object with both units | Two value objects named as in REQ-052, both carrying `pressure_reference` | User (contract review) | S5 |
| Dataset `measured_at` when readings differ | latest per-quantity time / earliest / reject differing times | Decided: latest reading time (user decision 2026-10-20, contract C-55); per-quantity times stay authoritative | User | S2 |
| Dataset `timestamp_quality` | `estimated_date` if any exported quantity is estimated, else `source` / always `source` | First option, so an estimate is never hidden at dataset level | User (contract review) | S6 |
| `source` format | `<provider>:<station id>` such as `dwd:00044` / separate `provider` and `station_id` fields | Decided: one string `<provider>:<station_id>` (user decision 2026-10-20, contract C-50) | User | S1 |
| `location` fields | `station_id`, `name`, `latitude`, `longitude`, optional `altitude_m` / other | First option; unknown altitude omitted, never zero | User (contract review) | S1 |
| Source quality metadata | optional `quality` string per value holding the source's quality code unchanged / none | Optional string; meanings are verified per provider in P5/P6 | User (contract review) | S1 |
| Omitted-reading diagnostics | builder result carries a non-exported list of omitted readings with reasons / silent omission | Diagnostics list, so callers can log omissions (AGENTS.md: no silent suppression) | Orchestrator, recorded | S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p2/P2_EXPORT_CONTRACT.md` | Change | Record section 3 decisions and full observation field mapping (JSON and XML names) |
| `src/WeatherParser.Parsing/Export/ObservationDataset.cs` | Change | Extend from the P2.02 slice; add required fields |
| `src/WeatherParser.Parsing/Export/ObservationMeasurements.cs` | Change | Extend from the P2.02 slice; add all nine fields |
| `src/WeatherParser.Parsing/Export/MeasurementValue.cs` | Create | Proposed type for per-quantity value with `value`, `measured_at`, `timestamp_quality`, optional `source_timezone`, optional `period`, optional `quality` |
| `src/WeatherParser.Parsing/Export/PressureMeasurementValue.cs` | Create | Proposed type for pressure values with `pressure_reference` |
| `src/WeatherParser.Parsing/Export/ObservationLocation.cs` | Create | Proposed type for location fields |
| `src/WeatherParser.Parsing/Observations/IObservationDatasetBuilder.cs` | Create | Interface for builder service |
| `src/WeatherParser.Parsing/Observations/ObservationDatasetBuilder.cs` | Create | Implementation of the builder service |
| `src/WeatherParser.Parsing/Observations/ObservationInput.cs` | Create | Proposed type for normalized inputs per quantity |
| `src/WeatherParser.Parsing/Observations/ObservationBuildResult.cs` | Create | Result type with dataset and diagnostics |
| `src/WeatherParser.Parsing/Observations/ObservationDatasetRejectedException.cs` | Create | Exception type for rejection cases |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Add DI registration for builder service |
| `tests/WeatherParser.Parsing.Tests/Observations/ObservationDatasetBuilderTests.cs` | Create | Tests for the builder |
| `tests/WeatherParser.Parsing.Tests/Export/ObservationDatasetContractTests.cs` | Create | Contract tests for observation graph |
| `tests/WeatherParser.Parsing.Tests/Export/ExportEntitySerializableTests.cs` | Change | Extend with new types |
| `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` | Change | Add DI registration test |
| `docs/p2/P2_VERIFICATION.md` | Change | Append per-slice evidence for P2.06 |

## 5. Procedure
### S0 Contract record
**Action:** Record the section 3 decisions and the full observation field mapping (JSON and XML names) in `docs/p2/P2_EXPORT_CONTRACT.md`; user review before S1.
**Roles:** qwen drafts prose from the orchestrator's decisions; the orchestrator verifies against REQ-050–057 and records the approval.

### S1 Full measurement shape
**Red:** `Serialize_FullObservation_WritesAllRequiredFieldsInBothFormats` (all nine fields, location, source, metadata; parsed with `JsonDocument` and `XDocument`). Expected failure: the P2.02 slice graph contains only `temperature_c`, so the other field assertions fail.
**Green:** Value entities and the extended measurement type.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S2 Per-quantity times
**Red:** `Build_DifferingReadingTimes_ArePreservedPerQuantity` (temperature 2026-07-15 12:00:00, humidity 11:50:00 UTC) and `Build_DatasetMeasuredAt_FollowsRecordedRule`. Expected failure: the first builder stub stamps every quantity with one dataset time.
**Green:** Per-quantity times from the inputs; dataset time per S0.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S3 Omission, zero and invalid readings
**Red:** `Build_MissingHumidity_IsOmitted`, `Build_ZeroWindSpeed_IsKept`, `Build_NonFiniteReading_IsOmittedAndReported` (diagnostics list), `Build_NegativeTemperature_IsKept`. Expected failure: the stub copies inputs unvalidated, so NaN reaches the graph and no diagnostic exists.
**Green:** Use `IQuantityValidator`; record omissions in the result.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S4 No usable readings
**Red:** `Build_AllReadingsUnusable_ThrowsObservationDatasetRejectedException`. Expected failure: the stub returns a dataset with empty `measurements`.
**Green:** Rejection with an actionable English message.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S5 Pressure, rounding and conversion integration
**Red:** `Build_Pressure_UsesConverterOnceAndKeepsReference` (mocked `IPressureConverter` and `IOutputRounding` in `MockRepository`, expectations verified by `VerifyAll()` in `[TestCleanup]`) and `Build_WithRealServices_RoundsOnlyAtOutput` (1000.5 hPa → 1001 / 750; 20.25 °C → 20.3). Expected failure: the stub writes unrounded values and calls no converter.
**Green:** Builder delegates to the P2.05 services.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S6 Quality and timezone markers
**Red:** `Build_EstimatedReading_CarriesEstimatedDateAndBerlinTimezone` and `Build_DatasetQuality_IsEstimatedWhenAnyReadingIsEstimated`. Expected failure: markers are not copied from `NormalizedSourceTime`.
**Green:** Copy markers; dataset quality per S0.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S7 Kind separation in the graph
**Red:** `Build_GustInput_IsNotWrittenAsWindSpeed` and `Build_RainAccumulationWithoutPeriod_IsNotWrittenAsRate`. Expected failure: the stub maps inputs by unit only.
**Green:** Kind checks through P2.05 services; rejected readings appear in diagnostics.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S8 Serializable traversal and equivalence
**Red:** `ExportTypes_AreSerializable` extended to the new types (fails until `[Serializable]` is added) and `Serialize_FullObservation_JsonAndXmlAreSemanticallyEqual` (same values, times, omissions, zeros, markers). Expected failure: missing attributes or mapping differences.
**Green:** Attributes; fix any mapping difference found.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S9 DI registration
**Red:** `AddWeatherParsing_RegistersObservationDatasetBuilder` fails because the registration is missing.
**Green:** Add DI registration for builder service.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

## 6. Tests and Test IDs
| Step-local case | T-P2 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P2-04 | REQ-050, REQ-052 | All required observation quantities and dataset fields present in both formats |
| S2 | T-P2-08 | REQ-050, REQ-056 | Distinct per-quantity timestamps survive both exports; dataset time follows the recorded rule |
| S3 | T-P2-04 | REQ-053 | Missing and invalid readings omitted and reported; zero and negative temperature kept |
| S4 | T-P2-04 | REQ-050 | Dataset without usable readings rejected, never exported as success |
| S5 | T-P2-09, T-P2-10 | REQ-054, REQ-057 | Pressure pair from one original with reference; rounding only at output (graph level) |
| S6 | T-P2-06, T-P2-08 | REQ-042, REQ-056 | `estimated_date` and `source_timezone` per reading; dataset quality never hides an estimate |
| S7 | T-P2-12 | REQ-044, REQ-052, REQ-055 | Gust and accumulation never relabeled as mean wind or rate |
| S8 | T-P2-01, T-P2-16 | REQ-067, REQ-074–077 | New types `[Serializable]`; JSON and XML semantically equal for the full graph (partial – completed in P2.08) |
| S9 | T-P2-19 | REQ-070–071, REQ-078 | Builder resolved through its interface (partial – completed in P2.10) |

## 7. Evidence to Record
- All entries go to `docs/p2/P2_VERIFICATION.md` per slice (Red names with observed failure messages, Green/Refactor results, commands, test counts, authorship), except decisions and approval date, which go to `docs/p2/P2_EXPORT_CONTRACT.md`

## 8. Risks and Limitations
- The dataset `measured_at` rule (C-55) and the `source` format (C-50) were decided by the user on 2026-10-20; the full decision set C-48 to C-65 is recorded in `docs/p2/P2_EXPORT_CONTRACT.md` section 10 and extends this plan (graph validation on serialization C-64, converter amendments C-63 and C-65)
- `XmlSerializer` needs the `{Name}Specified` omission pattern on every optional value object — S1/S8 assert no empty elements or `xsi:nil`
- Quality-code meanings are unknown until P5/P6 — the contract keeps the code unchanged and claims no meaning
- Every new code block needs a meaningful test (strict-improvement coverage rule)

## 9. Acceptance Criteria
- [ ] S0 approval recorded
- [ ] Red failures recorded for S1–S9
- [ ] All nine fields, per-quantity times, omissions, zeros, markers and pressure pairs asserted in both formats
- [ ] An all-unusable dataset never serializes
- [ ] All tests pass
