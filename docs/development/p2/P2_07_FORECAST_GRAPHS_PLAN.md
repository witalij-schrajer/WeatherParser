# P2.07 – Forecast Graphs: Implementation Plan

**Status: completed (2026-10-20, marked completed by the user after the hosted CI passed).** Implemented test-first in commit `41c29d6`; hosted CI run 39446243064 of draft pull request #3 passed on Windows x64, Ubuntu x64 and Debian 13 ARM64. Contract decisions C-66 to C-81 refine this plan (series provenance instead of a separate mixed-input check, omitted empty steps, graph validation C-79, reused `MosmixProduct`); evidence in [P2_VERIFICATION.md](../../p2/P2_VERIFICATION.md) section 7. Original planning note: this document started as a plan on 2026-10-20. This plan refines the P2.07 row of section 5 and the "Forecast run" row of section 4 of [PHASE_P2_PLAN.md](../PHASE_P2_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief and corrected by qwen in one review round (goal label, S2 roles, S8 Green line); qwen placed the S2 roles line under S8, and the orchestrating agent moved it to S2.

## 1. Goal and Scope
Goal: a separate forecast-run export graph for `MOSMIX_S` and `MOSMIX_L` with issue, retrieval and validity times kept distinct, chronologically ordered steps, typed values including interval quantities with period start/end, and an interface-based builder that aligns prepared synthetic series to their times without shifting, rejects mixed runs/products, missing issue times and runs without usable values; proven with both real serializers, the forecast run also serializing as a standalone JSON root (P8 MQTT payload, REQ-062).

In scope:
- Forecast run, location, step, values and interval-value entities
- Product enum
- Forecast-run builder with interface and its input model
- DI registration
- Contract-record updates

Out of scope:
- KML/KMZ reading → P7
- DWD element verification → P7
- Element-to-quantity mapping → P7
- Source units and pressure reference of MOSMIX elements → P7
- Archive limits → P7
- Forecast-only quantities beyond REQ-052 names (gusts, probabilities, totals) → added in P7 as additive contract changes with documented names, units and semantics
- Envelope with multiple runs → P2.08
- XSDs → P2.09
- MQTT topics/retain → P8

## 2. Preconditions and Inputs
- P2.02, P2.04, P2.05 complete
- P2.06 complete (shared value-entity conventions, `source` format decision)

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Type names | Proposed `ForecastRun`, `ForecastLocation`, `ForecastStep`, `ForecastValues`, `IntervalValue`, `MosmixProduct` / other | Proposed names | User (contract review) | S1 |
| Location entity | separate `ForecastLocation` (`name`, `latitude`, `longitude`, optional `altitude_m`; `mosmix_id` stays at run level) / reuse `ObservationLocation` | Separate type: REQ-074 keeps observation and forecast dataset types separate, and MOSMIX IDs must never be equated with CDC station IDs | User (contract review) | S1 |
| Forecast `source` identifier | same format as the P2.06 decision with distinct provider parts for S and L / other | Follow P2.06 and include the product so S and L never collide | User | S1 |
| Interval value shape | `value`, `period_start`, `period_end`, `aggregation` (for example `mean`) / `value` plus duration only | First option (REQ-105 requires start/end) | User (contract review) | S4 |
| Step ordering | builder sorts prepared steps by `valid_at` and rejects duplicate `valid_at` / builder rejects unsorted input | Sort and reject duplicates: source order is not guaranteed, and duplicates must never be silently dropped | Orchestrator, recorded | S3 |
| Builder input model | timeline plus one value series per quantity aligned by index (nullable entries for missing values) / prebuilt steps | Timeline plus aligned series: the no-shift rule is only testable on series; real series parsing stays in P7 | Orchestrator, recorded | S5 |
| Initial quantity set | REQ-052 names only, with `rain_rate_mm_h` as interval mean proving the interval mechanism / define gusts, probabilities and totals now | REQ-052 names only; forecast-only quantities are added in P7 after element verification | User (contract review) | S1, S4 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p2/P2_EXPORT_CONTRACT.md` | Change | Forecast mapping and decisions |
| `docs/p2/P2_VERIFICATION.md` | Change | P2.07 evidence |
| `src/WeatherParser.Parsing/Export/ForecastRun.cs` | Create | Forecast run entity |
| `src/WeatherParser.Parsing/Export/ForecastLocation.cs` | Create | Forecast location entity |
| `src/WeatherParser.Parsing/Export/ForecastStep.cs` | Create | Forecast step entity |
| `src/WeatherParser.Parsing/Export/ForecastValues.cs` | Create | Forecast values entity |
| `src/WeatherParser.Parsing/Export/IntervalValue.cs` | Create | Interval value entity |
| `src/WeatherParser.Parsing/Export/MosmixProduct.cs` | Create | MOSMIX product enum |
| `src/WeatherParser.Parsing/Forecasts/IForecastRunBuilder.cs` | Create | Forecast run builder interface |
| `src/WeatherParser.Parsing/Forecasts/ForecastRunBuilder.cs` | Create | Forecast run builder implementation |
| `src/WeatherParser.Parsing/Forecasts/ForecastRunInput.cs` | Create | Forecast run input model |
| `src/WeatherParser.Parsing/Forecasts/ForecastRunRejectedException.cs` | Create | Exception for invalid inputs |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Add DI registration for builder |
| `tests/WeatherParser.Parsing.Tests/Forecasts/ForecastRunBuilderTests.cs` | Create | Tests for the builder |
| `tests/WeatherParser.Parsing.Tests/Export/ForecastRunContractTests.cs` | Create | Contract tests for forecast run |
| `tests/WeatherParser.Parsing.Tests/Export/ExportEntitySerializableTests.cs` | Change | Extend to include forecast types |
| `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` | Change | Add test for builder registration |

## 5. Procedure
### S0 Contract record
**Action:** Record the section 3 decisions and the forecast field mapping (JSON and XML names) in `docs/p2/P2_EXPORT_CONTRACT.md`; user review before S1.
**Roles:** qwen drafts prose from the orchestrator's decisions; the orchestrator verifies against REQ-105–107 and records the approval.

### S1 Run graph shape and standalone root
**Red:** `Serialize_ForecastRun_WritesRequiredMetadataInBothFormats` (`dataset_type` `forecast`, `mosmix_id` `"X123A"`, `product` `MOSMIX_L`, `issued_at`, `retrieved_at`, two steps) and `Serialize_ForecastRun_AsStandaloneJsonRoot_IsOneCompleteDocument`. Expected failure: the types are first created without serializer attributes and without the `dataset_type` constant, so field names and the literal do not match.
**Green:** Add attributes, constant, product enum with exact wire literals `MOSMIX_S`/`MOSMIX_L`.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S2 No observation time in forecasts
**Characterization (not a Red):** `ForecastTypes_HaveNoMeasuredAtMember` (reflection over the forecast graph) and `Serialize_ForecastRun_ContainsNoMeasuredAt` (both formats). Expected to pass immediately because the forecast types are separate; record it as a guard, and treat any failure as a bug with a regression test first.
**Roles:** qwen generates the guard tests; the orchestrator runs them, records that they passed immediately (or, on failure, starts a regression test before any fix) and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S3 Chronological order and duplicates
**Red:** `Build_UnsortedTimeline_ProducesChronologicalSteps` and `Build_DuplicateValidAt_IsRejected`. Expected failure: the first builder stub keeps input order and accepts duplicates.
**Green:** Sort by `valid_at`; reject duplicates with `ForecastRunRejectedException`.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S4 Interval quantities
**Red:** `Build_IntervalQuantity_KeepsPeriodStartAndEnd` (`rain_rate_mm_h` derived from 0.5 mm over 2026-07-15 11:50:00–12:00:00 → 3 mm/h through `IPeriodRateConverter`) and `Build_IntervalWithEndNotAfterStart_IsRejected`. Expected failure: the stub writes a plain value without period.
**Green:** `IntervalValue` with start/end and aggregation.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S5 Alignment without shifting
**Red:** `Build_MissingValue_DoesNotShiftLaterValues` (timeline 12:00, 13:00, 14:00; temperature series 14.0, missing, 15.0 → 15.0 stays at 14:00 and 13:00 has no temperature) and `Build_SeriesLengthDiffersFromTimeline_IsRejected`. Expected failure: a stub that filters missing entries before pairing moves 15.0 to 13:00.
**Green:** Index-aligned pairing; length check.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S6 Run identity and usable values
**Red:** `Build_MissingIssuedAt_IsRejected`, `Build_InputsFromDifferentRunsOrProducts_AreRejected` (different `issued_at` or product or `mosmix_id`), `Build_NoUsableValues_IsRejected`. Expected failure: the stub accepts all inputs.
**Green:** Identity and usability checks with actionable English messages.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S7 Kind separation
**Red:** `Build_GustSeries_IsNotWrittenAsWindSpeed`, `Build_ProbabilitySeries_IsNotWrittenAsRainRate`, `Build_MissingHumidity_IsOmittedNotZero`. Expected failure: the stub maps series by unit only and fills missing values with zero.
**Green:** Kind checks through the P2.05 services; omission instead of zero.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S8 Serializable traversal and equivalence
**Red:** `ExportTypes_AreSerializable` extended to the forecast types (fails until `[Serializable]` is added) and `Serialize_ForecastRun_JsonAndXmlAreSemanticallyEqual`. Expected failure: missing attributes.
**Green:** add the attribute to the forecast types; fix any mapping difference found by the equivalence test.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S9 DI registration
**Red:** `AddWeatherParsing_RegistersForecastRunBuilder` fails because the registration is missing.
**Green:** Add registration for builder interface in `ServiceCollectionExtensions`.
**Refactor:** none expected
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

## 6. Tests and Test IDs
| Step-local case | T-P2 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P2-13 | REQ-105, REQ-111 | Complete run metadata, product and location identity in both formats; standalone JSON root |
| S2 | T-P2-13 | REQ-105 | No `measured_at` member or element anywhere in the forecast graph |
| S3 | T-P2-13 | REQ-105, REQ-068 | Chronological steps; duplicate validity times rejected |
| S4 | T-P2-14 | REQ-055, REQ-105–106 | Interval start/end retained; invalid intervals rejected |
| S5 | T-P2-14 | REQ-107 | Missing values omitted without shifting; series length mismatch rejected |
| S6 | T-P2-13, T-P2-14 | REQ-107 | Missing issue time, mixed runs/products and runs without usable values rejected |
| S7 | T-P2-12 | REQ-106 | Gusts and probabilities never substituted; missing humidity omitted, not zero |
| S8 | T-P2-01, T-P2-16 | REQ-067, REQ-074–077 | Forecast types `[Serializable]`; JSON and XML semantically equal (partial – completed in P2.08) |
| S9 | T-P2-19 | REQ-070–071, REQ-078 | Builder resolved through its interface (partial – completed in P2.10) |

## 7. Evidence to Record
- All entries go to `docs/p2/P2_VERIFICATION.md` per slice (Red names with observed failure messages, S2 as characterization, Green/Refactor results, commands, test counts, authorship), except decisions and approval date, which go to `docs/p2/P2_EXPORT_CONTRACT.md`.

## 8. Risks and Limitations
- The relation between `valid_at` and interval end for MOSMIX elements is unverified — P2.07 asserts only that start/end are kept and ordered, P7 verifies the source semantics.
- Forecast-only quantities are deferred, so the first P7 change to `ForecastValues` must follow the recorded evolution rules.
- A large `MOSMIX_L` run produces many steps — P2.07 tests stay synthetic and small, and size limits remain P7.
- Every new code block needs a meaningful test (strict-improvement coverage rule).

## 9. Acceptance Criteria
- [x] S0 approval recorded
- [x] Red failures recorded for S1 and S3–S9
- [x] S2 recorded as characterization
- [x] The standalone forecast root serializes as one complete JSON document
- [x] No `measured_at` in any forecast output
- [x] No shifted values
- [x] All tests pass
