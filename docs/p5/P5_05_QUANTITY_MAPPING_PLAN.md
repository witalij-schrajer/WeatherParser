# P5.05 – Quantity Mapping, Units, Periods and Quality: Implementation Plan

**Status: executed on 2026-10-24 ([record](P5_VERIFICATION.md) section 5): Red evidence per slice, T-P5-04 to T-P5-09 cases green, coverage gate passed, Codex review 1 then 0 findings.** Reconciled with the approved observation contract ([OC rows](P5_OBSERVATION_CONTRACT.md), approved 2026-10-24) on 2026-10-24: OC-04, OC-05 (`RWS_10` option A), OC-06 (option B) and OC-07; calm rule and the `DD_10` = 0 test fixed, `RWS_10` period stated as nominal; after the PR #7 review of 2026-10-24: mapping result with mapper omissions for the all-excluded direction branch. Refines the P5.05 row of section 5 and the rows "Values", "Units and periods" and "Times" of section 4 of [PHASE_P5_PLAN.md](../../PHASE_P5_PLAN.md).

## 1. Goal and Scope
Goal: an injectable mapper that turns the rows of a station's product archives (P5.04 `CdcNowEvidence`, all columns and `QN`) into `ObservationReading` items for the P2 builder: one reading per SC-03 quantity from the most recent usable row of its column (OC-04), with the verified unit, pressure reference, averaging period, missing-marker handling, calm-wind rule and `QN` quality (OC-05 to OC-07), and the row's `MESS_DATUM` normalized to UTC through `ISourceTimeNormalizer`.
In scope: `IDwdObservationMapper`/`DwdObservationMapper` (Proposed) with `DwdMappingResult Map(IReadOnlyList<CdcNowEvidence> evidence)`, where `DwdMappingResult` carries `Readings` (`IReadOnlyList<ObservationReading>`, for the builder) and `Omissions` (`IReadOnlyList<DwdMappingOmission>`, the OC-06 omissions the builder cannot see; `DwdMappingOmission(QuantityKind Kind, DwdMappingOmissionReason Reason)` with the reasons `CalmWind` and `ZeroDirection`; Proposed names); column → `QuantityKind` mapping (`TT_10` `Temperature`, `RF_10` `RelativeHumidity`, `PP_10` `Pressure`, `FF_10` `MeanWindSpeed`, `DD_10` `WindDirection`, `RWS_10` `PrecipitationAccumulation`, `GS_10` `RadiationEnergy`); usability through `IQuantityValidator` (SC-03: token parsed as `decimal`, −999 in any spelling is missing, otherwise `double` accepted by the validator for the kind); latest usable row per column; `PressureUnit.Hectopascal` and `PressureReference.Sensor` for `PP_10`; `WindSpeedUnit.MetresPerSecond` for `FF_10`; `AveragingPeriod(MESS_DATUM − 10 min, MESS_DATUM)` for `FF_10`, `DD_10`, `RWS_10`, `GS_10`; `GS_10` × 10000 (J/cm² → J/m²) before the reading; calm wind per OC-06 with the direction rule (OC-06 option B): the `DD_10` rows are taken newest first and missing rows are skipped; a row is excluded when `FF_10` of the same row is numerically 0 (reason `CalmWind`, which wins when both apply) or `DD_10` is numerically 0 (reason `ZeroDirection`); the reading comes from the newest non-missing row that is neither excluded nor rejected by the validator; when no such row exists, the newest non-missing row decides: if it is excluded, the result carries no direction reading and exactly one `DwdMappingOmission(WindDirection, <its reason>)`, and no raw value is passed on (a raw `DD_10` of 0 is valid under C-43 and would become north); if it is rejected by the validator, its value is passed on so that the builder records the rejection reason; if every row is missing, a reading with `Value` null is passed on (`Missing`); `Quality` = `QualityLevel` of the chosen row per OC-07; a column whose rows are all missing (−999 or no rows) yields a reading with `Value` null so that the builder records `OmissionReason.Missing`; a column with non-missing rows but no validator-accepted row yields a reading with its latest non-missing value, so that the builder records the real reason (`OutOfRange`; `NonFinite` cannot arise from the SC-19 grammar and stays covered by the P2 builder tests); registration in `AddWeatherParsing`.
Out of scope with owner: reading archives → P5.04; retrieval → P5.06; range validation, conversion to output units, rounding and omission diagnostics → the P2 builder (C-39 to C-46, C-58, C-59) used by P5.07, except the OC-06 omissions, which the mapper reports in `Omissions` because no reading reaches the builder; dataset assembly and logging → P5.07; columns without a REQ-052 field (`TM5_10`, `TD_10`, `RWS_DAU_10`, `RWS_IND_10`, `DS_10`, `SD_10`, `LS_10`) → not mapped (contract).

## 2. Preconditions and Inputs
- P5.02 approved OC-04 to OC-07; P5.04 complete (`QualityLevel` and all columns on `CdcNowEvidenceRow`).
- Existing services: `IQuantityValidator` (C-42, C-43), `ISourceTimeNormalizer` (C-29), `AveragingPeriod` (C-44), `ObservationReading` (C-57).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Interface and type names | `IDwdObservationMapper`/`DwdObservationMapper`, folder `Observations/Dwd/` / other | Proposed names | Orchestrator, recorded | S1 |
| Usability for "latest usable row" | SC-03 rule (parsed, not −999, accepted by `IQuantityValidator`) / parsed and not −999 only | SC-03 rule (OC-04): the latest row the validator accepts; when no row is accepted, the latest non-missing value is passed on so that the builder records its rejection reason (C-42, C-59) | User (OC-04) | S1, S2 |
| Calm-wind rule | omit `DD_10` when `FF_10` of the same row is 0 or `DD_10` is 0 (option B) / only when `FF_10` is 0 (option A) / export as delivered (option C) | Option B, approved 2026-10-24 | User (OC-06) | S4 |
| Quality form | plain `QN` string (C-53) | Per OC-07 | User (OC-07) | S4 |
| Energy unit conversion | × 10000 in the mapper before `RadiationEnergy` / a new converter service | In the mapper as an exact `decimal` factor (C-45 expects J/m²); documented in the contract | Orchestrator, recorded (OC-05) | S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Observations/Dwd/IDwdObservationMapper.cs` | Create | Interface |
| `src/WeatherParser.Parsing/Observations/Dwd/DwdObservationMapper.cs` | Create | Proposed implementation with `IQuantityValidator` and `ISourceTimeNormalizer` by constructor |
| `src/WeatherParser.Parsing/Observations/Dwd/DwdColumnMapping.cs` | Create | Proposed internal table column → kind, unit, reference, period flag, factor |
| `src/WeatherParser.Parsing/Observations/Dwd/DwdMappingResult.cs`, `DwdMappingOmission.cs`, `DwdMappingOmissionReason.cs` | Create | Proposed result type (`Readings`, `Omissions`), omission record and reason enum (`CalmWind`, `ZeroDirection`) |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Singleton registration |
| `tests/WeatherParser.Parsing.Tests/Observations/Dwd/DwdObservationMapperTests.cs` | Create | Behavior tests with strict mocks for the validator and normalizer where interactions matter, real instances where values matter |
| `tests/WeatherParser.Parsing.Tests/Observations/Dwd/DwdObservationMapperPeriodTests.cs` | Create | Period and conversion tests |
| `docs/p5/P5_VERIFICATION.md` | Change | P5.05 section |

## 5. Procedure
### S1 Kinds, units and latest usable value
**Red**
- `Map_TuArchive_ProducesTemperatureHumidityPressure`
- `Map_FfArchive_ProducesWindSpeedAndDirection`
- `Map_RrArchive_ProducesPrecipitationAccumulation`
- `Map_SdArchive_ProducesRadiationEnergy`
- `Map_Pressure_HectopascalSensorReference`
- `Map_WindSpeed_MetresPerSecondUnit`
- `Map_LatestRowWins` (two rows, later `MESS_DATUM` chosen)
- `Map_RowsOutOfOrder_LatestByTimeNotByPosition`
- `Map_LatestRowRejectedByValidator_OlderAcceptedRowChosen` (validator mock rejects the latest value)
- `Map_UnmappedColumnsIgnored` (`TM5_10`, `TD_10`, `DS_10`, `SD_10`, `LS_10`, `RWS_DAU_10`, `RWS_IND_10` produce no reading)
- `Map_NullEvidence_ThrowsArgumentNullException`
- `AddWeatherParsing_ResolvesDwdObservationMapper`
- `ServiceRegistrationCompletenessTests` red before registration

Expected failure: the mapper tests fail with `NotImplementedException` from the stub; `AddWeatherParsing_ResolvesDwdObservationMapper` fails with the container's resolution `InvalidOperationException` and `ServiceRegistrationCompletenessTests` with its missing-registration assertion.

**Green**
- mapping table; the result is a `DwdMappingResult` with an empty `Omissions` list until S4; per column the rows ordered by `MeasuredAtUtc` descending; first row whose token is usable; `ObservationReading` with kind, `double` value, unit and reference members and `Time` from `ISourceTimeNormalizer.NormalizeComplete(new DateTimeOffset(row.MeasuredAtUtc))` of the chosen row (valid per-reading times exist from S1 on, as the S2 real-builder test needs them); registration.

**Refactor**
- remove duplication with all tests green.

**Roles**
- qwen generates the tests first, then the minimal implementation and the refactoring; the orchestrating agent writes the briefs with the real declarations, runs each Red, build, tests and coverage, reviews the diff and records authorship.

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`; `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S2 Missing markers and genuine zeros
**Red**
- `Map_MinusNineNineNine_IsMissing`
- `Map_MinusNineNineNineWithDecimals_IsMissing` (`-999.0`, `-999.000`)
- `Map_ZeroIsUsable` (`0.0` temperature, `0.00` precipitation, `0` wind speed)
- `Map_OnlyMissingRows_YieldsReadingWithNullValue` (so the builder records `Missing`)
- `Map_AllRowsOutOfRange_PassesLatestValueForBuilderOutOfRange` (real validator and real builder: `RF_10` `101` in every row → humidity omitted with `OutOfRange`, temperature exported)
- `Map_BoundaryValues_AcceptedByRealValidator` (humidity `0` and `100`, wind direction `360`)
- `Map_LatestMissingOlderUsable_OlderChosen`
- `Map_TokenNotNumeric_TreatedAsUnusable` (an ignored-column token never reaches the mapper's used columns; a used-column token is guaranteed numeric by SC-19, so this test documents the guard only)

Expected failure: `Map_MinusNineNineNine_IsMissing`, `Map_MinusNineNineNineWithDecimals_IsMissing`, `Map_OnlyMissingRows_YieldsReadingWithNullValue`, `Map_LatestMissingOlderUsable_OlderChosen` and `Map_AllRowsOutOfRange_PassesLatestValueForBuilderOutOfRange` fail against the S1 implementation (−999 handled as a value; no fallback value); `Map_ZeroIsUsable`, `Map_BoundaryValues_AcceptedByRealValidator` and `Map_TokenNotNumeric_TreatedAsUnusable` are expected to pass on first run and are recorded as existing behavior.

**Green**
- `decimal` parse with the invariant culture, −999 comparison numerically, zero kept.

**Refactor**
- as in S1.

**Roles**
- as in S1.

**Check**
- as in S1.

### S3 Periods and energy conversion
**Red**
- `Map_WindSpeed_PeriodTenMinutesEndingAtMeasurement`
- `Map_WindDirection_PeriodTenMinutes`
- `Map_Precipitation_PeriodTenMinutes`
- `Map_RadiationEnergy_PeriodTenMinutes`
- `Map_Temperature_NoPeriod`
- `Map_Humidity_NoPeriod`
- `Map_Pressure_NoPeriod`
- `Map_RadiationEnergy_ConvertsJoulePerSquareCentimetreToSquareMetre` (`36.0` J/cm² → `360000` J/m²; with the 600 s period the builder yields 600 W/m² in P5.08)
- `Map_RadiationEnergyZero_IsZeroNotMissing`

Expected failure: the period tests of wind speed, wind direction, precipitation and radiation and `Map_RadiationEnergy_ConvertsJoulePerSquareCentimetreToSquareMetre` fail against the S2 implementation (no period, factor 1); `Map_Temperature_NoPeriod`, `Map_Humidity_NoPeriod`, `Map_Pressure_NoPeriod` and `Map_RadiationEnergyZero_IsZeroNotMissing` are expected to pass on first run and are recorded as existing behavior.

**Green**
- `AveragingPeriod(measuredAt.AddMinutes(-10), measuredAt)` for the four period columns; exact factor `10000m`; for `RWS_10` this is the nominal source interval without any instrument correction (OC-05 option A).

**Refactor**
- as in S1.

**Roles**
- as in S1.

**Check**
- as in S1.

### S4 Calm wind and quality
**Red**
- `Map_CalmWind_DirectionOmittedPerContract` (`FF_10` `0.0` and `DD_10` `180` in the same row → no direction reading from that row; an earlier row with `FF_10` above 0 supplies the direction)
- `Map_DirectionZero_OmittedAsNoReliableDirection` (`FF_10` `3.0`, `DD_10` `0` → no direction reading from that row, OC-06 option B)
- `Map_MissingSpeed_DoesNotSuppressDirection` (`FF_10` `-999`, `DD_10` `270` → direction reading kept, OC-06)
- `Map_AllRowsCalm_NoDirectionReadingAndOneCalmWindOmission` (every row `FF_10` `0.0`, `DD_10` `180`; temperature rows valid → no `WindDirection` reading, exactly one omission `WindDirection`/`CalmWind`, the temperature reading present)
- `Map_AllRowsZeroDirection_NoDirectionReadingAndOneZeroDirectionOmission` (every row `FF_10` `3.0`, `DD_10` `0` → no `WindDirection` reading, in particular none with value 0, exactly one omission `WindDirection`/`ZeroDirection`)
- `Map_CalmAndZeroDirectionInSameRow_ReasonCalmWind` (`FF_10` `0.0`, `DD_10` `0` in the only row → reason `CalmWind`)
- `Map_NewestNonMissingRowDecidesReason` (newest row `DD_10` `-999`, then a `DD_10` `0` row, then a calm row → reason `ZeroDirection`; the missing row neither decides nor yields a null reading)
- `Map_ExcludedNewestAndRejectedOlder_ReasonFromNewest` (newest non-missing row calm, older row `DD_10` `400` rejected by the validator → no reading, omission `CalmWind`)
- `Map_NoExclusion_EmptyOmissions` (valid wind rows → `Omissions` empty)
- `Map_WindWithSpeed_DirectionKept`
- `Map_Direction360_PassedThroughForC43` (normalization is the validator's)
- `Map_QualityFromChosenRow` (`QN` `2` of the chosen row, not of the latest row)
- `Map_QualityFormPerContract` (the unchanged trimmed `QN` token, for example `"2"`, OC-07)

Expected failure: `Map_CalmWind_DirectionOmittedPerContract`, `Map_DirectionZero_OmittedAsNoReliableDirection`, `Map_AllRowsCalm_NoDirectionReadingAndOneCalmWindOmission`, `Map_AllRowsZeroDirection_NoDirectionReadingAndOneZeroDirectionOmission`, `Map_CalmAndZeroDirectionInSameRow_ReasonCalmWind`, `Map_NewestNonMissingRowDecidesReason` and `Map_ExcludedNewestAndRejectedOlder_ReasonFromNewest` fail against the S3 implementation (no calm rule: the direction is exported for a calm row and for `DD_10` = 0, and no omission is reported), and `Map_QualityFromChosenRow` and `Map_QualityFormPerContract` fail (no `Quality` set); `Map_WindWithSpeed_DirectionKept`, `Map_Direction360_PassedThroughForC43`, `Map_MissingSpeed_DoesNotSuppressDirection` and `Map_NoExclusion_EmptyOmissions` pass on first run and are recorded as existing behavior.

**Green**
- calm rule per OC-06 option B using the same row's `FF_10` token (numerically 0) and the `DD_10` token (numerically 0); a skipped calm or zero-direction row lets the next older row supply the direction; the all-excluded branch of section 1 (no reading, one `DwdMappingOmission`, never the raw value); `Quality` from `QualityLevel`.

**Refactor**
- as in S1.

**Roles**
- as in S1.

**Check**
- as in S1.

### S5 Times
**Red**
- `Map_MeasuredAt_IsUtcSourceQuality` (`NormalizeComplete` called with the UTC `MESS_DATUM`
- `TimestampQuality.Source`; no `source_timezone`)
- `Map_DifferentColumnsDifferentTimes_Preserved` (temperature 12:00, humidity 11:50)
- `Map_NeverUsesRetrievalTime` (no `TimeProvider` dependency; retrieval time not an input)
- `Map_NormalizerFailure_Propagates` (`SourceTimeNormalizationException` from the mock propagates)

Expected failure: none; S1 already delegates every time to the normalizer per chosen row, so these four tests are expected to pass on first run and are recorded as existing behavior (characterization); a failure is a defect of S1 fixed test-first.

**Green**
- no change expected.

**Refactor**
- as in S1.

**Roles**
- as in S1.

**Check**
- as in S1.

### S6 Review and evidence
**Red**
- No Red: review slice.

**Green**
- build, all tests, pre-commit coverage with the changed-file list from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard`, native block counters, baseline and gate result.

**Roles**
- the orchestrating agent runs the checks; Codex reviews the uncommitted diff read-only; qwen fixes confirmed findings test-first; checkpoint commit when the user requests it; push; hosted CI run recorded.

**Check**
- pre-commit coverage (CONTRIBUTING.md section 5); Codex review; commit and CI run recorded in `docs/p5/P5_VERIFICATION.md`.

## 6. Tests and Test IDs
| Step-local case | T-P5 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S2 | T-P5-04 | REQ-032, REQ-053 | −999 in every spelling missing; zeros usable; only-missing column → null value for the builder's `Missing` reason; all-rejected column → latest non-missing value for the builder's `OutOfRange` reason; non-finite tokens excluded by the SC-19 grammar (builder coverage from P2) |
| S1 | T-P5-05 | REQ-052, REQ-054 | Kinds, hPa with `Sensor` reference, m/s unit flag; both pressure outputs and rounding are the builder's (verified on the wire in P5.08) |
| S3 | T-P5-06 | REQ-055 | Periods on wind, precipitation and radiation; none on `TT_10` (instantaneous) and on `RF_10` and `PP_10` (no period documented); J/cm² → J/m² |
| S4 | T-P5-07 | REQ-053 | Calm wind per OC-06; never an invented north; all-calm and all-zero-direction columns → no direction reading and exactly one mapper omission with the actual reason |
| S4 | T-P5-08 | REQ-032 | `QN` of the chosen row as `quality` per OC-07 |
| S1, S5 | T-P5-09 | REQ-043, REQ-051, REQ-056 | Latest usable row per column; per-column UTC times preserved; no retrieval time |
| S1 | T-P5-13 (partial) | REQ-016, REQ-078 | Mapper behind its interface, constructor dependencies, registration |
| S1–S5 | T-P5-15 | REQ-085 | Recorded Red → Green → Refactor evidence |

## 7. Evidence to Record
- Red names with observed failures; existing-behavior records
- Commands, test counts, native block counters and gate result
- OC-06 and OC-07 variants implemented; Codex review and dispositions; authorship

## 8. Risks and Limitations
- (a) Choosing the latest validator-accepted row per column can combine readings of different times — intended (REQ-056), each reading carries its own time
- (b) `GS_10` unit stated as J/cm² by the DWD description — verified in P5.02; a wrong factor would be a contract defect, not a mapper defect
- (c) Calm-wind semantics of DWD not documented — OC-06 option B (approved 2026-10-24) is a recorded project assumption; no calm row was observed on 2026-10-24, so the rule is tested with synthetic rows only
- (d) Instrument-dependent precipitation time stamp (Pluvio Ott about 5 minutes late) — not corrected (OC-05 option A); a limitation for README (P5.09) and the P5 acceptance (P5.11)
- (e) The validator's acceptance depends on C-42 ranges — a range change is a P2 contract decision

## 9. Acceptance Criteria
- [ ] Each behavior increment has recorded Red evidence
- [ ] All T-P5-04 to T-P5-09 cases of this step pass
- [ ] No `TimeProvider`, HTTP or stream access in the mapper
- [ ] Only SC-03 columns mapped; the seven other columns never produce a reading
- [ ] Mapper registered; completeness and no-shared-state tests green
- [ ] Build without warnings; pre-commit coverage gate passed
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user
- [ ] No dataset assembly, logging or export code added
