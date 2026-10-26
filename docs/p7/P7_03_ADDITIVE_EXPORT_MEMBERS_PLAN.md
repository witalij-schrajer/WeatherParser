# P7.03 – Additive Export-Contract and Builder Members: Implementation Plan

**Status: planned (2026-10-26); not started. Written before the P7.02 contract; it is reconciled with the approved contract in P7.02 S6 before its first Red test.** Refines the P7.03 row of section 5 and the "Run" and "Export" rows of section 4 of [PHASE_P7_PLAN.md](../../PHASE_P7_PLAN.md).

## 1. Goal and Scope
Goal: extend the forecast part of export contract v1 and the forecast-run builder only additively, as approved in MF-02, MF-09 and MF-11, so that rejected runs keep their omission diagnostics, stale runs carry a wire marker, and the approved forecast-only quantities have their own members — with the frozen member order unchanged and the regenerated schema validating every produced run.
In scope: the additive `OmittedValues` member of `ForecastRunRejectedException` filled by `ForecastRunBuilder`; the freshness member appended after `forecasts` (MF-09) and its builder input; the approved forecast-only members of `ForecastValues` appended after `rain_rate_mm_h` (MF-11) with their aggregation members of `IntervalAggregation`; the builder rules and the graph-validation rules of C-79 for the new members; new C-numbered decisions in the P2 export contract (C-120 onwards, citing the MF rows); schema regeneration through `tools/WeatherParser.SchemaGeneration`; schema, serializer, determinism and equivalence tests; registration unchanged (C-81).
Out of scope with owner: element mapping → P7.05; parsers → P7.07, P7.08; MQTT payload use → P8.

## 2. Preconditions and Inputs
- P7.02 approved MF-02, MF-09 and MF-11 (and their names, types and aggregation semantics).
- Existing: `ForecastRunBuilder`, `ForecastRunInput`, `ForecastSeries`, `ForecastRunRejectedException`, `ForecastRun`, `ForecastValues`, `IntervalValue`, `IntervalAggregation`, `QuantityKind`, `ObservationDatasetRejectedException.OmittedReadings` (pattern), the export serializers and `IExportRootValidation`, `ExportSchemaGenerator`/`ExportSchemaRefiner`, the golden-file regeneration procedure of CONTRIBUTING.md ("Export schema generation (P2.09)"), the tests under `tests/WeatherParser.Parsing.Tests/Forecasts/`, `Schemas/` and `Serialization/`.
- Applicability: a part whose MF option is not approved is recorded as not applicable and skipped; the `OmittedValues` member (MF-02) is needed by every parser outcome and is always in scope.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Rejection diagnostics (MF-02) | Additive `IReadOnlyList<OmittedForecastValue> OmittedValues` on `ForecastRunRejectedException` (empty for rejections before any value is processed) / a separate result type | Additive member, as `ObservationDatasetRejectedException.OmittedReadings`; the existing constructor kept | User (MF-02) | S1 |
| Freshness member (MF-09) | Name and type approved in P7.02 (Proposed `run_age_exceeded`, optional `bool?`, written only when `true` or always when known) / an enum | As approved; appended after `forecasts` in `ForecastRun`, JSON and XML (C-67, C-105); builder input `ForecastRunInput` gains an additive optional member carrying the freshness result | User (MF-09) | S2 |
| Forecast-only members (MF-11) | As approved (Proposed `wind_gust_kmh` interval maximum, `precipitation_amount_mm` interval total) | Appended after `rain_rate_mm_h` in `ForecastValues` (C-73, C-105); `IntervalAggregation` gains `Maximum` (`maximum`) and `Sum` (`sum`) as additive members; the C-74 rule "always interval means" stays for the existing two members | User (MF-11) | S3 |
| Input kind of the amount (Proposed) | A new `QuantityKind` member (Proposed `PrecipitationAmount`) targeting `precipitation_amount_mm`, so one source total can feed both the C-77 rate (`PrecipitationAccumulation` → `rain_rate_mm_h`) and the amount without two series targeting the same field (C-76) / a series flag | New enum member; `WindGust` changes from `NotExportable` to the approved gust member with its period; recorded as C decisions | Orchestrator, recorded with the C rows; user approval through MF-11 | S3 |
| Validation rules | Extend C-79: new interval members require a period and the approved aggregation, ranges and output scales per C-42/C-57; the freshness member needs no further rule | As stated; recorded as C decisions | Orchestrator, recorded | S2, S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Forecasts/ForecastRunRejectedException.cs`, `ForecastRunBuilder.cs`, `ForecastRunInput.cs` | Change | Rejection diagnostics, freshness input, new kinds |
| `src/WeatherParser.Parsing/Export/ForecastRun.cs`, `ForecastValues.cs`, `IntervalAggregation.cs` | Change | Appended members, aggregation members, validation |
| `src/WeatherParser.Parsing/Quantities/QuantityKind.cs` | Change | Approved additive kind |
| `schemas/export/v1/weatherparser-export-v1.xsd` | Regenerate | Through the golden-file procedure only, never edited by hand |
| `tests/WeatherParser.Parsing.Tests/Forecasts/*`, `Schemas/*`, `Serialization/*`, `tests/WeatherParser.SchemaGeneration.Tests/*` | Change, Create | Behavior, schema, order and equivalence tests |
| `docs/p2/P2_EXPORT_CONTRACT.md` | Change | New C decisions with field-mapping rows (orchestrating agent) |
| `docs/p7/P7_VERIFICATION.md` | Change | P7.03 section |

## 5. Procedure
Every slice: qwen generates the tests first, then the minimal implementation and the refactoring, from briefs with the real declarations (AGENTS.md qwen invocation); the orchestrating agent runs each Red, the build, the tests and the coverage, reviews the diff (no weakened expectations, no unrequested files) and records authorship. Check: `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`, `dotnet test tests/WeatherParser.SchemaGeneration.Tests --configuration Release --no-build`, then the pre-commit coverage gate.

### S1 Rejection diagnostics
**Red**: `Build_NoUsableValue_ExceptionCarriesOmissions` (only missing and out-of-range values: the exception lists every omission with valid time, kind and reason in builder order), `Build_EmptyTimeline_ExceptionHasNoOmissions`, `Build_LengthMismatch_ExceptionHasNoOmissions` (rejections before value processing), `Constructor_MessageOnly_OmittedValuesEmpty`, `Constructor_NullOmissions_ThrowsArgumentNullException`.
**Green**: additive member and constructor; the builder passes its collected omissions.

### S2 Freshness member (only with MF-09 approved)
**Red**: `Build_StaleInput_RunCarriesMarker`, `Build_FreshInput_MarkerAsApproved` (absent or `false` per the approved form), `Serialize_Json_MemberAfterForecasts`, `Serialize_Xml_ElementAfterForecasts` (order checked on the real serializers' output), `Serialize_MarkerJsonXmlEquivalent`, `Schema_RunWithMarker_Valid`, `Schema_MarkerBeforeForecasts_Invalid` (a hand-ordered XML document fails validation), `Deserialize_DocumentWithoutMarker_StillValid` (documents of the earlier v1 form stay valid against the regenerated schema).
**Green**: entity, builder input, serializer mapping; schema regenerated with the golden-file procedure; the generated diff reviewed so that only the appended element is added.

### S3 Forecast-only members (only with MF-11 approved)
**Red**: `Build_GustSeries_ExportsIntervalMaximum` (m/s converted to km/h, period kept, aggregation `maximum`), `Build_GustWithoutPeriod_OmittedMissingPeriod`, `Build_NegativeGust_OmittedOutOfRange`, `Build_AmountSeries_ExportsIntervalSum` (mm, period, aggregation `sum`, zero kept), `Build_AccumulationAndAmountFromOneTotal_BothFields` (the same source values as `PrecipitationAccumulation` and as the amount kind: rate and amount both present, never merged), `Build_TwoAmountSeries_ThrowsArgumentException` (C-76), `Validate_GustWithMeanAggregation_Fails`, `Validate_RainRateWithSumAggregation_Fails` (C-74 kept for the existing members), `Serialize_NewMembersAfterRainRate_JsonAndXml`, `Serialize_NewMembersEquivalent`, `Schema_NewMembers_Valid`, `Schema_NewMembersBeforeRainRate_Invalid`, `Rounding_GustTiesAwayFromZero` (12.5 → 13 km/h on the wire).
**Green**: kinds, builder rules, aggregation members, validation, serializer mapping; schema regenerated and reviewed as in S2.

**Refactor**: remove duplication with all tests green; the builder and root-validation rules for the new members stay in one place each, with equivalence tests guarding them (inherited limitation).

### S4 Contract record
- New C decisions in `docs/p2/P2_EXPORT_CONTRACT.md` section 15 or a new P7 section: each member with name, type, position, omission rule, validation rule, wire mapping and the MF row it implements; the field-mapping table extended; the schema checksum recorded. Written by the orchestrating agent; Codex review of the step.

## 6. Tests and Test IDs
| Step-local case | T-P7 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P7-17 (builder level) | REQ-072, REQ-107 | Omissions survive a rejection |
| S2 | T-P7-13 (wire) | REQ-104, REQ-110 | Stale marker on both formats, appended position |
| S3 | T-P7-10 | REQ-044, REQ-055, REQ-106 | Gusts and totals in their own members; rates and amounts never merged |
| S2, S3 | T-P7-16 | REQ-067 to REQ-069, REQ-077, REQ-079 | Equivalence, order, regenerated schema |
| all | T-P7-18 (registration unchanged), T-P7-20 | REQ-078, REQ-085, REQ-086 | Registration; Red evidence; coverage |

## 7. Evidence to Record
- Red names and failures, counts, coverage, authorship, Codex rounds
- Schema diff summary and checksum; the C decisions added

## 8. Risks and Limitations
- (a) XML consumers validating against an older copy of the schema reject documents with the new members (C-105); documented in README in P7.10
- (b) `IntervalAggregation` gains members; consumers with exhaustive switches over it must handle them (additive enum members; recorded)
- (c) The builder and root validation keep separate implementations of the same rules (inherited); equivalence tests guard divergence

## 9. Acceptance Criteria
- [ ] Every listed new-behavior test of the applicable slices green with recorded Red; characterization tests (refactoring guards and existing behavior) green before and after and recorded as characterization, never with a manufactured Red
- [ ] Existing member order unchanged in entities, JSON, XML and schema; the new members appended
- [ ] Schema regenerated through the golden-file procedure only; earlier v1 documents still valid
- [ ] C decisions recorded with field mappings
- [ ] Registration and architecture tests green; coverage gate passed; Codex 0 findings or reported
