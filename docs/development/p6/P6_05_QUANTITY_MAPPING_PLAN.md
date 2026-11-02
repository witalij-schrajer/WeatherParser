# P6.05 – Quantity Mapping, Identity and Times: Implementation Plan

**Status: implemented (2026-10-25), slices S1 to S5 recorded in [P6_VERIFICATION.md](../../p6/P6_VERIFICATION.md) section 5.** Refines the P6.05 row of section 5 and the "Values", "Units and periods" and "Times" rows of section 4 of [PHASE_P6_PLAN.md](../PHASE_P6_PLAN.md). Reconciled on 2026-10-25 with the OsthessenWetter contract approved by the user on 2026-10-25 ([contract](../../p6/P6_OSTHESSEN_CONTRACT.md), option A in every row): compound temperature cases added to S5 (contract section 6.1, A7); OW-03, OW-04, OW-06, OW-07 and OW-11 as planned.

## 1. Goal and Scope
Goal: injectable services that turn the raw station rows of P6.04 into station identities, locations and `ObservationReading` lists with checked units, the wind period and inferred UTC times — each concern in its own service and slice, so that every slice has its own files and its own exit check.
In scope, by slice:
- S1 identity: slug derivation and collision detection (OW-03)
- S2 metadata: station metadata options validation and lookup (OW-04)
- S3 values: number grammar, unit suffix check and quantity mapping (OW-06, OW-11)
- S4 times: time-text grammar and date inference through `ISourceTimeNormalizer.InferDate` (OW-07)
- S5 composition: one mapper that applies S1 to S4 to a page and returns per-station results with omissions and diagnostics
Out of scope with owner: dataset assembly, station failures and logging → P6.07; retrieval and endpoint options → P6.06; wire form → P6.08.

## 2. Preconditions and Inputs
- P6.02 approved the rows named above; P6.04 delivers `OsthessenWetterPage` and `OsthessenWetterStationRow`.
- Existing types: `ObservationReading`, `ObservationLocation`, `QuantityKind`, `WindSpeedUnit`, `AveragingPeriod`, `ISourceTimeNormalizer`, `NormalizedSourceTime`, `SourceTimeNormalizationException`, `OmissionReason`.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Services (Proposed) | `IOsthessenWetterStationIdentity` (`string ToStationId(string name)`, `void EnsureUnique(IEnumerable<string> names)`), `IOsthessenWetterStationMetadataLookup` (lookup over the validated options), `IOsthessenWetterValueReader` (cell text → `double?` or unreadable, unit check), `IOsthessenWetterTimeReader` (time text → `NormalizedSourceTime` or omission), `IOsthessenWetterObservationMapper` (composition) / one mapper class | Separate services, one per slice, each with its own file set and tests (PR review of `80dc47b`, risk register) | Orchestrator, recorded | S1–S5 |
| Metadata type | `OsthessenWetterStationMetadata` (`Name`, `Latitude`, `Longitude`, `AltitudeM?`) as a list on `OsthessenWetterOptions` / a separate options class | On `OsthessenWetterOptions` (OW-04, OW-10); validated in S2 and at construction in P6.06 | User (OW-04) | S2 |
| Unreadable versus missing | Empty cell → reading omitted with `Missing` through the builder; text outside the grammar or unexpected unit → notice `OSTHESSEN-VALUE-UNREADABLE` and no reading | As stated (OW-11) | User (OW-11) | S3 |

## 4. Artifacts
| Slice | Path (under `src/WeatherParser.Parsing/Observations/Osthessen/` unless stated) | Tests (under `tests/WeatherParser.Parsing.Tests/Observations/Osthessen/`) |
|---|---|---|
| S1 | `IOsthessenWetterStationIdentity.cs`, `OsthessenWetterStationIdentity.cs` | `OsthessenWetterStationIdentityTests.cs` |
| S2 | `OsthessenWetterOptions.cs` (metadata part), `OsthessenWetterStationMetadata.cs`, `IOsthessenWetterStationMetadataLookup.cs`, `OsthessenWetterStationMetadataLookup.cs` | `OsthessenWetterStationMetadataLookupTests.cs` |
| S3 | `IOsthessenWetterValueReader.cs`, `OsthessenWetterValueReader.cs` | `OsthessenWetterValueReaderTests.cs` |
| S4 | `IOsthessenWetterTimeReader.cs`, `OsthessenWetterTimeReader.cs` | `OsthessenWetterTimeReaderTests.cs` |
| S5 | `IOsthessenWetterObservationMapper.cs`, `OsthessenWetterObservationMapper.cs`, `OsthessenWetterMappedStation.cs` | `OsthessenWetterObservationMapperTests.cs` |
| all | `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` (one registration per slice), `docs/p6/P6_VERIFICATION.md` | `ServiceRegistrationCompletenessTests` unchanged and green |
| S2 | The shared consumer setups that build validated containers: `tests/WeatherParser.Parsing.Tests/TestConsumerServices.cs`, `tests/WeatherParser.Package.Tests/TestConsumerServices.cs`, `ConsumerSelectionSupport.cs`, `tests/WeatherParser.Parsing.Tests/Selection/ConsumerBoundary.cs` and both container setups in `tests/WeatherParser.Parsing.Tests/Live/LiveCheckRunner.cs` | Register a valid synthetic `OsthessenWetterOptions` (with metadata) in the same slice that makes the lookup depend on it, so every existing `ValidateOnBuild` container stays green |

Each slice is committed separately when the user requests commits per slice; the shared registration file is changed by one slice at a time.

## 5. Procedure
Every slice: roles and checks as in P6.03 section 5; each slice ends with its own green test run and coverage check before the next starts.

### S1 Identity
**Red**: `ToStationId_Umlauts` (`Müs-Hohhütte` → `mues-hohhuette`), `ToStationId_Eszett` (`Roßbach` → `rossbach`), `ToStationId_SpacesAndPunctuation` (`Bad Salzschlirf` → `bad-salzschlirf`, runs collapsed, edges trimmed), `ToStationId_NfdEqualsNfc`, `ToStationId_NoAsciiLetterOrDigit_ThrowsFormatChanged`, `EnsureUnique_TwoNamesOneSlug_ThrowsIdCollision` (`Roßbach`, `Rossbach`), `EnsureUnique_DistinctSlugs_Passes`, `ToStationId_StableAcrossCultures`.
**Green**: rules of OW-03; registration.

### S2 Metadata
**Red**: `Lookup_ExactNfcName_ReturnsMetadata`, `Lookup_NfdConfiguredName_Matches`, `Lookup_Unknown_ReturnsNone`, `Validate_LatitudeOutOfRange_ThrowsOptionsInvalid` (−90.000001, 90.000001; −90 and 90 accepted), `Validate_LongitudeOutOfRange_ThrowsOptionsInvalid` (both sides, ±180 accepted), `Validate_DuplicateName_ThrowsOptionsInvalid`, `Validate_EmptyName_ThrowsOptionsInvalid`, `Validate_NullEntry_ThrowsOptionsInvalid`, `Lookup_AltitudeNone_IsNullNeverZero`, `Lookup_ZeroCoordinates_Valid`.
**Green**: validation and lookup; registration.

### S3 Values and units
**Red**: `Read_Decimal_ReturnsValue` (`10.3°C`), `Read_Negative_ReturnsValue` (`-4.2`), `Read_Zero_ReturnsZero`, `Read_Empty_ReturnsMissing`, `Read_CommaDecimal_Unreadable`, `Read_NaNInfinityExponentThousands_Unreadable`, `Read_OverflowDigits_ReturnsNonFinite` (a 400-digit number), `Read_WrongUnit_Unreadable` (`10.3 km/h` in the temperature column), `Read_CultureIndependent` (`de-DE`, `tr-TR`), `Map_Temperature_Kind`, `Map_Humidity_Kind`, `Map_Wind10_MeanWindSpeedKmh`, `Map_Gusts_NeverMeanWindSpeed` (both gust columns produce no `MeanWindSpeed` and no other exported kind), `Map_Wind60_NotMapped`, `Map_Precipitation_NotMapped` (OW-06 option A, approved; option B rejected), `Map_TrendAndExtremes_NotMapped`.
**Green**: grammar `-?[0-9]+(\.[0-9]+)?` with invariant parsing; unit table per column; registration.

### S4 Times
**Red**: `ReadTime_HhMmUhr_ReturnsEstimatedDate` (marker `estimated_date`, zone `Europe/Berlin`), `ReadTime_Missing_ReturnsMissingTime`, `ReadTime_InvalidText_ReturnsMissingTime` (`25:00 Uhr`, `9:5 Uhr`, `09:31`), `ReadTime_23_58_RetrievedAt_00_02_PreviousDay`, `ReadTime_00_00_RetrievedAt_00_01_SameDay`, `ReadTime_FiveMinutesLater_SameDay`, `ReadTime_FiveMinutesOneSecondLater_PreviousDay` (whole-minute display: 6 minutes later), `ReadTime_SpringGap_Unresolved` (02:30 on the last Sunday of March), `ReadTime_AutumnOverlap_Unresolved` (02:30 on the last Sunday of October), `ReadTime_RetrievalBothSidesOfTransition`, `ReadTime_RetrievalSubSecond_Truncated` (the retrieval instant is truncated to whole seconds before inference, C-33).
**Green**: grammar `\A([01][0-9]|2[0-3]):[0-5][0-9] Uhr\z`; `InferDate`; `SourceTimeNormalizationException` → unresolved with `OSTHESSEN-TIME-UNRESOLVED`; registration.

### S5 Composition
**Red**: `Map_Station_ReadingsWithOwnTimes` (overview time on temperature and humidity, wind time on wind), `Map_WindPeriod_TenMinutesBeforeWindTime`, `Map_UnmatchedWind_OverviewReadingsKept`, `Map_UnresolvedOverviewTime_OnlyWindReadings`, `Map_UnreadableValue_DiagnosticWithoutValueText`, `Map_OfflineStation_NoReadings` (status passed through for P6.07), `Map_MetadataMissing_Marked`, `Map_Collision_ThrowsBeforeAnyStationMapped`; with the real `IObservationDatasetBuilder` in the test (T-P6-04 through the builder): `MapThenBuild_Humidity0And100_Kept`, `MapThenBuild_HumidityBelow0_OutOfRange`, `MapThenBuild_Humidity100Point1_OutOfRangeNeverRoundedFirst`, `MapThenBuild_NegativeWind_OutOfRangeNeverClamped`, `MapThenBuild_ZeroWind_Kept`, `MapThenBuild_OverflowDigits_NonFinite`, `MapThenBuild_TemperatureWithPositiveNegativeOrZeroChange_OnlyTemperatureExported` (reader plus mapper on synthetic compound cells with changes `+1.1`, `-0.5` and `+0`: the exported temperature equals the primary value), `MapThenBuild_MissingTemperatureWithValidChange_TemperatureOmittedNeverChange`, `MapThenBuild_UnreadableTemperatureWithValidChange_NoticeAndNoTemperature`, `MapThenBuild_MissingOverviewTime_MissingTimeForTemperatureAndHumidity_WindKept`, `MapThenBuild_InvalidWindTime_MissingTimeForWind_OverviewKept`, `MapThenBuild_MissingTime_RetrievalTimeNeverSubstituted` (no exported reading carries the retrieval instant), `MapThenBuild_OldOnlineReading_KeptWithInferredTime` (an online row whose displayed time is 23:00 while the retrieval is at 10:00 local: kept, dated the previous day by C-30, no freshness cutoff), each asserting the omission kind and reason and that the sibling readings are exported.
**Green**: composition over S1 to S4; registration.

**Refactor** after S5: remove duplication across the slices with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P6 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P6-07 | REQ-041, REQ-050 | Slug rules, collisions, stability |
| S2 | T-P6-08 | REQ-041, C-51 | Metadata validation and lookup |
| S3 | T-P6-04 (mapping) | REQ-052, REQ-053, REQ-057, REQ-044 | Grammar, units, kinds, gusts and totals never relabelled |
| S4 | T-P6-05, T-P6-06 | REQ-042, REQ-043, REQ-056 | Inference, markers, midnight, DST |
| S5 | T-P6-03 (application) | REQ-041 | Joined and unmatched wind rows |
| all | T-P6-13 (registration), T-P6-15 | REQ-078, REQ-085 | Registration; Red evidence |

## 7. Evidence to Record
- Per slice: Red names and failures, Green and Refactor results, counts, coverage counters, authorship, Codex rounds

## 8. Risks and Limitations
- (a) A name with only non-Latin letters has no slug — fails as format change (S1); recorded
- (b) Stale displays older than one day are not detectable (OW-12) — documented limitation
- (c) Rounding is not applied here; the builder rounds on output (C-39), verified in P6.08

## 9. Acceptance Criteria
- [ ] Each slice green with recorded Red before the next starts; each registered
- [ ] Gusts, the 60-minute mean, precipitation, trend and extremes never mapped to an exported kind
- [ ] Times inferred only through `ISourceTimeNormalizer`; no second time-zone path
- [ ] Coverage gate passed; Codex 0 findings or reported
