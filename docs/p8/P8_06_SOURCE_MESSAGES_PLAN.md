# P8.06 – Messages for Every Source and Root: Implementation Plan

**Status: planned on 2026-10-28; accepted by the user on 2026-10-28 with the phase plan; reconciled on 2026-10-28 with the P8.02 contract MQ-01 to MQ-15 approved by the user on 2026-10-28 (P8.02 S5); complete on 2026-10-29 ([verification record](P8_VERIFICATION.md#6-p806--messages-for-every-source-and-root)).** Refines the P8.06 row of section 5 and the "Separation" and "Semantics" rows of section 4 of [PHASE_P8_PLAN.md](../../PHASE_P8_PLAN.md).

## 1. Goal and Scope
Goal: prove with the real serializers that every result the library produces becomes the right MQTT message: DWD observations, OsthessenWetter observations (synthetic), MOSMIX_L and MOSMIX_S forecast runs and both station lists, with distinct topics where retained state must not collide.
In scope: contract tests through the composed services with only HTTP mocked, from synthetic fixtures of P4 to P7 (the existing test helpers), each result serialized as JSON and as MQTT message and compared byte for byte; separation of observations and forecasts; topics per product and location; complete runs; special characters; the generic serializer without provider branches (architecture test).
Out of scope with owner: package consumption → P8.07; publication of any value → P3, and for OsthessenWetter not before the OW-14 clarification.

## 2. Preconditions and Inputs
- P8.05 complete; MQ-03, MQ-05, MQ-08, MQ-10 approved.
- Existing: composed pipeline tests and fixture helpers of P5 (`DwdObservationParser`), P6 (`OsthessenWetterParser`), P7 (`MosmixLParser`, `MosmixSParser`, `MosmixKmzBuilder`) and P4 (selection), `XmlExportSerializer` and the v1 schema for the XML comparison.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Test configuration | The REQ-061 example templates as test configuration / invented templates | The REQ-061 examples as test data (`haus/weather/measurements/{source}/state`, `haus/weather/forecasts/{source}/state`, `haus/weather/stations/dwd/selection`, `haus/weather/stations/mosmix/selection`), plus one alternative configuration per uniqueness rule; never a library default | Orchestrator, recorded | S1 |
| OsthessenWetter data | Synthetic fixtures only (OW-14) / any recorded page | Synthetic fixtures only; no recorded OsthessenWetter value in any test (MQ-10) | User (MQ-10, OW-14) | S1 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `tests/WeatherParser.Parsing.Tests/Mqtt/MqttSourceMessageTests.cs` | Create | Per-source contract tests |
| `tests/WeatherParser.Parsing.Tests/Mqtt/MqttSerializerGenericityTests.cs` | Create | Architecture test |
| `docs/p8/P8_VERIFICATION.md` | Change | P8.06 section |

## 5. Procedure
Every slice: roles and checks as in P8.03 section 5. Behavior already implemented in P8.05 is exercised here; tests that pass on first run are recorded as characterization tests, not as Red (the P7.09 precedent), and any failure found is fixed test-first.

### S1 Observations and station lists
**Tests**: `DwdObservation_MessageTopicPayloadQosRetain` (topic `haus/weather/measurements/dwd:<id>/state`, payload equal to the JSON export, QoS 1, retained), `OsthessenWetterObservation_Synthetic_GenericMessage`, `TwoStations_DistinctTopics`, `CdcStationList_NotRetained`, `MosmixLocationList_NotRetained`, `LeadingZeroIdsAndUmlauts_UnchangedInTopicAndPayload`, `ObservationPayload_EqualsXmlContent` (the JSON payload and the XML export of the same dataset are semantically equivalent through the existing equivalence helper).

### S2 Forecast runs
**Tests**: `MosmixLRun_OneMessageWithEveryStep` (the step count of the payload equals the run), `MosmixSRun_OneMessageWithEveryStep`, `SameIdBothProducts_DistinctTopics`, `TwoLocations_DistinctTopics`, `ForecastPayload_HasNoMeasuredAt`, `ForecastRun_AlwaysForecastType`, `ObservationAndForecastTopics_NeverEqual_ForExampleConfiguration`, `StaleRun_MarkerInPayload` (`run_age_exceeded`), `LargestRunPayloadSize_Recorded` (a MOSMIX_L run with the maximum MF-07 time steps; the size is written to the test output and recorded).

### S2a Duplicate identity (MQ-15)
**Tests**: `IdenticalDatasets_ByteIdenticalPayloads` (two builds from the same input), `SameDatasetMeasuredAt_ChangedQuantityMeasuredAt_DifferentPayloads` (the C-55 example: temperature at 12:00 in both, humidity at 11:50 and 11:55; same `source`, same dataset `measured_at`, different payload bytes and different per-quantity `measured_at`), `CorrectedValueSameQuantityMeasuredAt_DifferentPayloads` (one value changed, every timestamp unchanged), `SameReadingsLaterRetrievedAt_DifferentPayloads` (only `retrieved_at` changed), `SameForecastRun_DifferentRetrievedAt_SameSourceAndIssuedAt` (two parses of one fixture at different clock times: same topic, `source` and `issued_at`, different `retrieved_at` and different payload bytes).

### S3 Genericity
**Tests**: `MqttTypes_ContainNoProviderLiterals` (a source-text scan of the string literals in `src/WeatherParser.Parsing/Mqtt/`: no literal contains a provider token of C-50, C-68 or OW-03 (`dwd:`, `dwd-mosmix-s`, `dwd-mosmix-l`, `osthessenwetter`) or `haus/weather`; references to the approved entity types and members (`MosmixLocationList`, `ForecastRun.MosmixId`, `MosmixProduct`) and the approved placeholder names of MQ-04 (`mosmix_id`, `product`) are allowed; no comparison of a root's `Source` or provider value with a literal), `MqttSerializer_DependsOnlyOnMqttRulesAndJsonSerializer` (constructor parameter types).

## 6. Tests and Test IDs
| Step-local case | T-P8 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P8-07, T-P8-11 | REQ-050, REQ-015 | Observations and lists from every source |
| S2 | T-P8-09, T-P8-10 | REQ-061, REQ-062, REQ-105 | Forecast separation, topics and completeness |
| S2a | T-P8-17 | REQ-062, C-55 | Duplicate identity |
| S3 | T-P8-11 | REQ-015 | Generic serializer |
| all | T-P8-16 | REQ-085, REQ-086 | Evidence; coverage |

## 7. Evidence to Record
- Test names, characterization or Red classification, counts, coverage counters, the largest payload size, authorship, Codex rounds

## 8. Risks and Limitations
- (a) The tests prove message content, not broker behavior; retained-state replacement on a real broker is shown in P3

## 9. Acceptance Criteria
- [ ] Every listed test green; classification recorded
- [ ] Payload equals the JSON export for every source and root
- [ ] Forecasts never as observations; product and location topics distinct
- [ ] Coverage gate passed; Codex 0 findings or reported
