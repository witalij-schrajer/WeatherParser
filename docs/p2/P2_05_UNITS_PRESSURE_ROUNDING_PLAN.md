# P2.05 – Units, Pressure and Output Rounding: Implementation Plan

**Status: completed (2026-10-20, marked completed by the user after the hosted CI passed).** Implemented test-first in commit `02393bb`; hosted CI run 39411112653 of draft pull request #3 passed on Windows x64, Ubuntu x64 and Debian 13 ARM64; evidence in [P2_VERIFICATION.md](P2_VERIFICATION.md) section 5. Original planning note: this document started as a plan on 2026-10-20 and was updated after the user's review of the P2.05 decisions (pressure bounds per original unit, whole-second period endpoints, wind-direction range decided). This plan refines the P2.05 row of section 5 and the pressure, rounding, conversion and omission paragraphs of section 4 of [PHASE_P2_PLAN.md](../../PHASE_P2_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief and corrected by qwen in one review round (concrete Refactor targets); the orchestrating agent reworded one table cell whose absolute-value bars (from its own brief) broke the Markdown table.

## 1. Goal and Scope
Goal: interface-based, source-independent services for output rounding, hPa/mmHg pressure conversion from one authoritative original, field-specific validity and omission, period-aware energy/accumulation transformations, and quantity-kind separation — each proven test-first with exact values.

In scope:
- Rounding service
- Pressure converter
- Quantity validator
- Period-rate converter
- Quantity-kind model
- DI registration
- Contract-record updates (numeric policy, tolerance, kinds)

Out of scope (owner):
- Mapping DWD/OsthessenWetter/MOSMIX source elements and their units to these services → P5, P6, P7
- Placing values into the observation graph → P2.06 and the forecast graph → P2.07
- Time normalization → P2.04
- Serialization → P2.02/P2.08

Prerequisites: P2.02 complete (numeric type `decimal` decided; omission pattern in place).

## 2. Preconditions and Inputs
- P2.02 completed with `decimal` type for numeric values and omission patterns established.
- Existing repository facts: `src/WeatherParser.Parsing/`, `tests/WeatherParser.Parsing.Tests/`.
- Required user authorization: contract decisions from section 3.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Service names | Proposed `IOutputRounding`/`OutputRounding`, `IPressureConverter`/`PressureConverter`, `IQuantityValidator`/`QuantityValidator`, `IPeriodRateConverter`/`PeriodRateConverter`, value `QuantityKind`, failure `QuantityConversionException` / other | Proposed names | User (contract review) | S1 |
| Input numeric boundary | services accept `double` from parsers and convert to `decimal` after rejecting NaN/±Infinity / services accept only `decimal` | Accept `double` at the validator boundary only; everything after validation is `decimal` | Orchestrator, recorded | S3 |
| Pressure tolerance statement | document the maximum deviation between the two rounded outputs / no statement | Document bounds per original unit including the factor discrepancy a × b = 1.00000018491463 (contract C-41) | User (contract review) | S2 |
| Wind direction range | [0, 360) with 360 normalized to 0 / [0, 360] kept as delivered / reject 360 | Decided: [0, 360] with 360 normalized to 0 (user decision 2026-10-20, contract C-43); the validator test covers 0, 359.9, 360 → 0, −1 and 360.1 | User | S3 |
| m/s → km/h conversion in P2 | include the exact factor 3.6 now / leave all unit conversions except pressure to provider phases | Include: the factor is exact and REQ-057 rounds km/h after conversion; source unit verification stays with P5/P7 | User (contract review) | S6 |
| Period representation | `TimeSpan` duration plus start/end UTC / duration only | Duration plus start/end, so the averaging period is preserved in the output (REQ-055) | User (contract review) | S4 |
| Quantity kinds | `MeanWindSpeed`, `WindGust`, `PrecipitationAccumulation`, `PrecipitationRate`, `RadiationEnergy`, `Irradiance`, `Illuminance`, `Probability` (and the REQ-052 kinds) / other | Proposed list; additional kinds need documented names, units and semantics | User (contract review) | S5 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p2/P2_EXPORT_CONTRACT.md` | Update | Numeric policy, tolerance, kinds, period representation |
| `docs/p2/P2_VERIFICATION.md` | Update | P2.05 evidence |
| `src/WeatherParser.Parsing/Quantities/IOutputRounding.cs` | Create | Interface for output rounding |
| `src/WeatherParser.Parsing/Quantities/OutputRounding.cs` | Create | Implementation of `IOutputRounding` |
| `src/WeatherParser.Parsing/Quantities/IPressureConverter.cs` | Create | Interface for pressure conversion |
| `src/WeatherParser.Parsing/Quantities/PressureConverter.cs` | Create | Implementation of `IPressureConverter` |
| `src/WeatherParser.Parsing/Quantities/PressureValues.cs` | Create | Result type: hPa, mmHg, reference, original unit |
| `src/WeatherParser.Parsing/Quantities/IQuantityValidator.cs` | Create | Interface for quantity validation |
| `src/WeatherParser.Parsing/Quantities/QuantityValidator.cs` | Create | Implementation of `IQuantityValidator` |
| `src/WeatherParser.Parsing/Quantities/IPeriodRateConverter.cs` | Create | Interface for period-rate conversion |
| `src/WeatherParser.Parsing/Quantities/PeriodRateConverter.cs` | Create | Implementation of `IPeriodRateConverter` |
| `src/WeatherParser.Parsing/Quantities/QuantityKind.cs` | Create | Enum for quantity kinds |
| `src/WeatherParser.Parsing/Quantities/AveragingPeriod.cs` | Create | Type for averaging period with start/end and duration |
| `src/WeatherParser.Parsing/Quantities/QuantityConversionException.cs` | Create | Exception type for quantity conversion errors |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Register the new services |
| `tests/WeatherParser.Parsing.Tests/Quantities/OutputRoundingTests.cs` | Create | Tests for output rounding |
| `tests/WeatherParser.Parsing.Tests/Quantities/PressureConverterTests.cs` | Create | Tests for pressure conversion |
| `tests/WeatherParser.Parsing.Tests/Quantities/QuantityValidatorTests.cs` | Create | Tests for quantity validation |
| `tests/WeatherParser.Parsing.Tests/Quantities/PeriodRateConverterTests.cs` | Create | Tests for period-rate conversion |
| `tests/WeatherParser.Parsing.Tests/Quantities/QuantityKindSeparationTests.cs` | Create | Tests for quantity-kind separation |
| `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` | Change | Add tests for service registration |

## 5. Procedure
### S0 Contract record
Action: Record the section 3 decisions in `docs/p2/P2_EXPORT_CONTRACT.md`; user review before S1.
Roles: qwen drafts prose from the orchestrator's decisions; the orchestrator verifies and records the approval.

### S1 Output rounding
**Red**: Data-driven `Round_UsesIncrementAndAwayFromZero` with all rounding examples. Expected failure: a first implementation using `Math.Round(value, digits)` returns 20.2 for 20.25 and −20.2 for −20.25 (banker's rounding).
**Green**: `MidpointRounding.AwayFromZero` with per-quantity increments.
**Refactor**: Move the per-quantity increments into one table keyed by quantity kind.
**Roles**: qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check**: `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S2 Pressure conversion
**Red**: `Convert_FromHpa_DerivesBothOutputsFromUnroundedOriginal` (1000.5 → 1001 hPa / 750 mmHg), `Convert_FromMmHg_UsesReverseFactor` (760 → 1013 / 760), `Convert_PreservesPressureReference` (all three references); `Convert_FromHpa_RoundedOutputsStayWithinOriginBounds` (800.0 to 1100.0 hPa in steps of 0.1, both directions of the hPa bounds), `Convert_FromMmHg_RoundedOutputsStayWithinOriginBounds` (600.0 to 825.0 mmHg in steps of 0.1, both directions of the mmHg bounds, plus 100000000 mmHg → 133322390 hPa with the reverse difference 18.491463 within its bound). Expected failure: a first implementation that rounds hPa before deriving mmHg yields 751 for the 1000.5 case.
**Green**: Derive both from the original, then round.
**Refactor**: Keep both conversion factors as named constants with their REQ-054 source in an XML documentation comment.
**Roles**: qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check**: `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S3 Validity and omission
**Red**: `Validate_NonFiniteValues_AreRejected` (NaN, ±Infinity), `Validate_HumidityOutsideRange_IsRejected` (−0.1, 100.1; 0 and 100 accepted), `Validate_ZeroAndNegativeTemperature_AreKept`, `Validate_MissingWindDirection_IsNeverDefaultedToNorth`, and the wind-direction range cases per S0. Expected failure: the stub accepts every value.
**Green**: Field-specific checks.
**Refactor**: none expected.
**Roles**: qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check**: `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S4 Period-aware transformations
**Red**: `ToMeanIrradiance_UsesKnownPeriod` (360000 J/m², 600 s → 600 W/m²), `ToMeanRainRate_UsesKnownPeriod` (0.5 mm, 10 min → 3 mm/h), `PeriodTransforms_RejectZeroNegativeOrMissingPeriod`, `PeriodTransforms_RetainPeriodInResult`; `AveragingPeriod_FractionalSecondEndpoint_IsRejected` (start or end one tick or one millisecond past a whole second → `ArgumentException`), `AveragingPeriod_OneWholeSecond_IsAccepted` (shortest valid period). Expected failure: the stub returns the input value unchanged, keeps no period and accepts fractional-second endpoints.
**Green**: Division by the period in seconds/hours with the period carried in the result.
**Refactor**: Share the period validation between the irradiance and rain-rate conversions.
**Roles**: qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check**: `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S5 Quantity-kind separation
**Red**: `Convert_GustToMeanWind_IsRejected`, `Convert_AccumulationToRateWithoutPeriod_IsRejected`, `Convert_IlluminanceToIrradiance_IsRejected`, `Convert_ProbabilityToAmount_IsRejected`. Expected failure: the stub converter does not check kinds.
**Green**: Kind checks with `QuantityConversionException`.
**Refactor**: none expected.
**Roles**: qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check**: `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S6 Wind speed m/s → km/h (only if decided in S0; otherwise record the slice as deferred to P5/P7)
**Red**: `ToKilometresPerHour_UsesExactFactor` (10 → 36; 3.47 → 12.492 → rounded 12). Expected failure: method absent from the stub returns the input.
**Green**: Implement conversion using exact factor 3.6.
**Refactor**: none expected.
**Roles**: qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check**: `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S7 DI registration
**Red**: `AddWeatherParsing_RegistersQuantityServices` fails because the registrations are missing.
**Green**: Register all new services in `ServiceCollectionExtensions`.
**Refactor**: none expected.
**Roles**: qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check**: `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

## 6. Tests and Test IDs
| Step-local case | T-P2 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P2-10 | REQ-057 | Every increment; positive and negative ties away from zero; no extra precision rule |
| S2 | T-P2-09 | REQ-054, REQ-057 | Both formulas; one unrounded original; reference kept; documented tolerance |
| S3 | T-P2-04 | REQ-052–053 | Nonfinite rejected; humidity range; zero and negative temperature kept; no defaulted north (partial – completed in P2.06) |
| S4 | T-P2-11 | REQ-055, REQ-106 | Known-period energy and rain transforms; zero/negative/missing period rejected; period retained |
| S5 | T-P2-12 | REQ-052, REQ-055, REQ-106 | Gust, mean wind, probability, accumulation, intensity, energy, illuminance kept distinct |
| S6 | T-P2-10 | REQ-052, REQ-057 | Exact km/h factor, rounding after conversion (if decided) |
| S7 | T-P2-19 | REQ-070–071, REQ-078 | Services resolved through their interfaces (partial – completed in P2.10) |

## 7. Evidence to Record
- All entries go to `docs/p2/P2_VERIFICATION.md` per slice (Red names with observed failure messages, Green/Refactor results, commands, test counts, authorship), except decisions and approval date, which go to `docs/p2/P2_EXPORT_CONTRACT.md`.
- For each slice, record the Red test name, expected failure reason, Green implementation details, Refactor steps, roles, check commands, and test count.

## 8. Risks and Limitations
- `decimal` keeps scale, so outputs must be normalized to the increment's scale before serialization (20.3, not 20.30) — S1 asserts the scale.
- Source units are not verified here, so a provider phase could feed the wrong unit — mitigation: services take explicit unit/kind inputs and P5–P7 verify source units.
- The wind-direction range was decided by the user (C-43: 360 normalized to 0); a source calm marker must be mapped to an omitted direction by the provider phases.
- Every new code block needs a meaningful test (strict-improvement coverage rule).

## 9. Acceptance Criteria
- [x] S0 approval recorded
- [x] Red failures recorded for S1–S5 and S7 (and S6 if included)
- [x] Every exact example value asserted
- [x] No lux ↔ W/m² path exists
- [x] No rounding before conversion anywhere
- [x] All tests pass
