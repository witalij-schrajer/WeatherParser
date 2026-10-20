# P2 Verification Record

**Status (2026-10-20): P2.01 complete (READY); P2.02 completed (commit `b1ba0d2`, candidate run section 2.7, marked completed by the user); P2.03 completed (commit `0f59e1c`, marked completed by the user; section 3); P2.04 completed (commit `15928cf`, marked completed by the user; section 4); P2.05 completed (commit `02393bb`, marked completed by the user; section 5); P2.06 completed (commit `90106eb`, marked completed by the user; section 6); P2.07 completed (commit `41c29d6`, marked completed by the user; section 7); P2.08 completed (commit `7c9b919`, marked completed by the user; section 8); P2.09 completed (commit `77c92a9`, marked completed by the user; section 9); P2.10 completed (commits `f7b9b9e` and `186f900`, marked completed by the user; section 10); hosted CI of the draft pull request #3 checks every push.**

This record collects the evidence of every P2 step as required by [PHASE_P2_PLAN.md](../../PHASE_P2_PLAN.md) section 8; model output is never evidence; only the runs listed here, executed by the orchestrating agent, count.

## 1. P2.01 – Foundation check

Outcome READY on 2026-10-20; full record in [P2_01_FOUNDATION_CHECK.md](P2_01_FOUNDATION_CHECK.md); commit `6594a37`; integration target `64e460b` measured at 54/56.

## 2. P2.02 – Contract decisions and first serializer slice

### 2.1 Contract approval (S0)

Decisions C-01 to C-15 approved by the user on 2026-10-20 after two review rounds (user findings on C-05 omission semantics and C-03 provisional v1 were applied); record: [P2_EXPORT_CONTRACT.md](P2_EXPORT_CONTRACT.md).

### 2.2 Slice evidence

| Slice | Red test(s) and observed failure | Green / Refactor | Tests after slice |
|-------|----------------------------------|------------------|-------------------|
| S1 omission | `ObservationDatasetJsonContractTests.SerializeAsync_MissingTemperature_OmitsTemperatureProperty` failed: "Expected doc.RootElement.GetProperty("measurements").TryGetProperty("temperature_c", out _) to be False, but found True." (JSON wrote `"temperature_c": null`). XML missing/zero/non-zero and JSON zero/non-zero passed immediately (characterization, as recorded in the contract section 5). | `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` on `TemperatureC` | 21 passed |
| S2 UTC lexical form | `UtcTimestampContractTests`: JSON and XML timestamp tests failed with "differs at index 19" (the appended `Z`); four guard tests failed with "Expected a <System.ArgumentException> to be thrown, but no exception was thrown." | Internal value type `ExportTimestamp` (validation, wire format, parsing); `RetrievedAtUtc`/`MeasuredAtUtc` ignored by both serializers, `RetrievedAtWire`/`MeasuredAtWire` carry the field names. Refactor: four characterization tests in `TimestampWireReadBackTests` for the wire setters (read-back in both formats, malformed text rejected), passed immediately | 31 passed |
| S3 escaping | `EscapingContractTests.SerializeAsync_Json_Umlaut_IsWrittenUnescaped` failed (JSON contained `\u00C4` instead of `Ä`); `SerializeAsync_Xml_InvalidXmlCharacter_ThrowsExportSerializationException` failed (found `InvalidOperationException` "There was an error generating the XML document." with inner `ArgumentException` for 0x01). Three round-trip tests passed immediately (characterization) | JSON options with `JavaScriptEncoder.Create(UnicodeRanges.All)` held in an instance field; XML failures wrapped in `ExportSerializationException` | 36 passed |
| S4 culture | Characterization (not a Red): `CultureInvarianceContractTests.SerializeAsync_UnderCulture_WritesInvariantNumbersAndTimestamps` for `de-DE` and `en-US` passed immediately | none | 38 passed |
| S5 Serializable | `ExportEntitySerializableTests.ExportTypes_ReachableFromObservationDataset_AreSerializable` failed naming `MeasurementValue`, `ObservationDataset`, `ObservationMeasurements`; traversal and constructor tests passed | `[Serializable]` on the three entities | 41 passed |
| S6 service contract | `ExportSerializerStreamContractTests.SerializeAsync_Xml_DeclaresOnlyTheExportNamespace` failed (3 declarations: `xmlns:xsi`, `xmlns:xsd`, `xmlns`); added test `SerializeAsync_Xml_InvalidCharacterAfterLargeContent_WritesNothingToDestination` failed ("Expected stream.Length to be 0L, but found 196576L"). Stream left open, no BOM, pre-cancelled token, destination write failure (JSON and XML) and small-document invalid character passed immediately (characterization) | XML: buffer then `CopyToAsync`, `XmlWriter` with UTF-8 without BOM, only the export namespace declared, serializer creation inside the failure wrapping. Refactor: per-instance `ConcurrentDictionary<Type, XmlSerializer>` cache; JSON also buffers then copies | 52 passed |
| S7 DI | `ServiceCollectionExtensionsTests.AddWeatherParsing_WithoutFurtherRegistration_ResolvesExportSerializers` and `AddWeatherParsing_ExportSerializers_AreSingletonsPerContainer` failed: "No service for type ... has been registered." | Singleton registrations of both serializers in `AddWeatherParsing` | 54 passed |

### 2.3 Checks run on the working tree (2026-10-20)

- `dotnet build WeatherParser.Ci.slnf -c Release`: 0 warnings, 0 errors.
- Tests: `WeatherParser.Parsing.Tests` 66 passed; `WeatherParser.Coverage.Tests` 99 passed; `WeatherParser.Coverage.Runner.Tests` 56 passed; 0 failed, 0 skipped.
- Local native block coverage (`dotnet test tests/WeatherParser.Parsing.Tests --collect "Code Coverage" --settings coverage.runsettings`, converted with `dotnet-coverage` 18.11.0): `WeatherParser.Parsing.dll` 179 covered, 2 not covered (98.90 %, after the review fixes in section 2.6); the two uncovered blocks are the pre-existing P1 constructor `ParserEndpointValidator(IEndpointUriValidator)`. An unused constructor `ExportSerializationException(string)` that was uncovered was removed. An unused `ExportTimestamp.IsSet` property found by the coverage report after the review fixes was also removed. Against the integration target 54/56 this is a strict improvement; the gate itself is evaluated only by the committed-candidate run (not yet executed).
- Not yet executed for P2.02: any ARM64 run (planned in P2.11).

### 2.4 Deviations and design notes

1. Slice order: S1 already created minimal `JsonExportSerializer`/`XmlExportSerializer` services because the contract tests serialize through them; S6 then added the stream, buffering and namespace behavior.
2. S1 XML half and several S6 guarantees passed immediately and are recorded as characterization, not as Red failures (contract section 5; phase plan section 8).
3. The timestamp wire setters were added in the S2 Green step because `XmlSerializer` maps only read/write properties; their behavior is covered by characterization tests added in the S2 refactor.
4. `ExportTimestamp` is an internal immutable value type (REQ-070 allows values to be constructed normally), used instead of a static helper class, so no doing class without an interface was introduced.
5. XML documents declare only the export namespace; this was required by the contract example (C-02) and is now tested.

### 2.5 Authorship

qwen3-coder:30b generated all production code and tests of S1–S7 and the refactorings from the orchestrating agent's briefs, and corrected its defects in further rounds (build errors, a regression that removed the JSON property names, string-based XML names, a vacuous byte comparison, nullable warnings, an invented type name). The orchestrating agent wrote the briefs, ran every Red, build, test and coverage run, reviewed every diff, confirmed each failure reason before Green, and itself applied four mechanical corrections after qwen did not: null-forgiving operators in `TimestampWireReadBackTests`, removal of usings already provided by ImplicitUsings in eleven files, removal of a redundant `#nullable enable`, and a tolerant file-block parser in its own tooling. In this record it corrected one value that qwen had decoded (`\u00C4` in the S3 row).

### 2.6 Review fixes (2026-10-20)
The user's review found three defects; each was fixed test-first with regression tests:

| Finding | Regression test(s) and observed failure | Fix |
|---------|----------------------------------------|-----|
| Unset required timestamps were exported as `0001-01-01T00:00:00` | `RequiredValueGuardTests`: `SerializeAsync_Json_UnsetRetrievedAt_ThrowsExportSerializationException`, `SerializeAsync_Xml_UnsetRetrievedAt_ThrowsExportSerializationException`, `SerializeAsync_Json_UnsetMeasuredAt_ThrowsExportSerializationException`, `SerializeAsync_Xml_UnsetMeasuredAt_ThrowsExportSerializationException` failed: "Expected a <WeatherParser.Parsing.Serialization.ExportSerializationException> to be thrown, but no exception was thrown." | `ExportTimestamp` tracks whether it was set; `ToWire(fieldName)` throws for an unset value, which both serializers report as `ExportSerializationException` with nothing written. Four older S1 tests that had relied on the fabricated timestamp were completed with `MeasuredAtUtc` |
| JSON serialization failures escaped unwrapped (C-10) | `JsonFailureWrappingTests.SerializeAsync_UnsupportedPropertyType_ThrowsExportSerializationExceptionAndWritesNothing` failed: found `System.NotSupportedException`; `SerializeAsync_PreCancelledToken_StillThrowsOperationCanceledException` passed (characterization) | Serialization into the buffer is wrapped in `ExportSerializationException` except `OperationCanceledException`; the copy to the destination stays outside, so destination write failures and cancellation propagate unchanged |
| `schema_version` was mutable (C-02) | `RequiredValueGuardTests.SchemaVersion_AssignedOtherValue_ThrowsArgumentException`, `JsonReadBack_OtherSchemaVersion_IsRejected` and `XmlReadBack_OtherSchemaVersion_IsRejected` failed: no exception was thrown; `SchemaVersion_Default_IsApprovedLiteral` passed (characterization) | `SchemaVersion` always returns `ExportNamespaces.SchemaVersionV1` (`"1"`); its setter, needed for read-back, rejects any other value with `ArgumentException` |

| An unassigned `MeasurementValue.Value` was exported as `0` (user decision 2026-10-20: fix now in P2.02) | `RequiredValueGuardTests.SerializeAsync_Json_UnsetMeasurementValue_ThrowsExportSerializationException` and `SerializeAsync_Xml_UnsetMeasurementValue_ThrowsExportSerializationException` failed: "Expected a <WeatherParser.Parsing.Serialization.ExportSerializationException> to be thrown, but no exception was thrown." | `Value` tracks whether it was assigned; reading an unassigned value throws, which both serializers report as `ExportSerializationException` with nothing written; an assigned zero is still written |

Result: `WeatherParser.Parsing.Tests` 66 passed, `WeatherParser.Coverage.Tests` 99 passed, `WeatherParser.Coverage.Runner.Tests` 56 passed; build 0 warnings.

Authorship: qwen3-coder:30b wrote the regression tests and fixes and corrected its own defects (an invalid private struct constructor, incomplete test data, the unused `IsSet` member, a documentation sentence placed on the class instead of the `Value` property); the orchestrating agent wrote the briefs, confirmed every failure reason and ran all builds, tests and coverage runs, restored one sentence of section 2.3 that qwen dropped while updating this record, and in the last update repaired a broken backtick in section 1 and moved a misplaced authorship phrase back into this paragraph.

### 2.7 Committed candidate run (2026-10-20)

| Item | Value |
|---|---|
| Command | `pwsh ./scripts/ci-local.ps1 -Commit b1ba0d2b8a81b91b01cf7fd7fe043c8adf798c22 -TargetRef 64e460b475b4a29b9e3ddd331394076e53982d9e -RunSettings coverage.runsettings` |
| Candidate / target | `b1ba0d2b8a81b91b01cf7fd7fe043c8adf798c22` / `64e460b475b4a29b9e3ddd331394076e53982d9e` (PR base `develop`), mode two-pass |
| Run directory | `artifacts/ci/b1ba0d2-20261020T065138Z` (local, gitignored); exit code 0; 36 s |
| Build | 0 warnings in both passes |
| Tests (current pass) | `WeatherParser.Parsing.Tests` 66, `WeatherParser.Coverage.Tests` 99, `WeatherParser.Coverage.Runner.Tests` 56, `WeatherParser.Package.Tests` 2; all passed; target pass `WeatherParser.Parsing.Tests` 15 passed |
| Package | `WeatherParser.Parsing.0.1.0-initial.nupkg`, SHA-512 `KFkqf7aUwjPKKAOX2MmhHxih8nH7jAzwcwOJU618/45Lw0UQKOLUo0+3jLz+PfonRU74i4j1jWCTOjzK+xMO1A==`, SHA-256 `424d4e5e94837c014bba94e78f9f1f369ced9d4beabef29d835bc719cc6b9ece`; package-content check passed; consumer lock: the one permitted `contentHash` line rewritten |
| Collection settings | identical in both passes except `commit`; run settings SHA-256 `dc6f662e4f6faaa853c33ed5497f182a31527fd126961411a604b683ffcf05d2` |
| Changed files | 41 (code, tests and documentation; no documentation-only exemption) |
| Gate | passed; `gate-result.json` SHA-256 `73aa839b60e430866d4da452ccb46b22427c31d9a2ca35c5d96630f3b7c6d5ec` |
| Native block counters | current 179/181 (98.90 %), remeasured target 54/56 (96.43 %) |
| Gate rules | Comparability passed ("Baseline is comparable."); Measurement passed ("Block counters 179/181 are valid."); Minimum passed ("179/181 (98.90%) is at or above the required minimum of 80%"); Baseline passed ("Baseline block counters 54/56 are valid."); Comparison passed ("Block coverage 179/181 (98.90%) exceeds baseline 54/56 (96.43%).") |

The run was executed and checked by the orchestrating agent; it is Windows x64 evidence only and not P2 acceptance evidence (P2.11 verifies the final candidate, including the Debian 13 ARM64 target).

Record note: qwen drafted this section's values as a list; the orchestrating agent converted them into this table without changing any value.

### 2.8 Hosted CI and completion (2026-10-20)

- Hosted CI run 39383342526 of draft pull request #3 on head `6d4b6a2` (contains `b1ba0d2`) passed: `windows-x64`, `ubuntu-x64` and `debian13-arm64` success (Debian 13 ARM64 container on a GitHub-hosted runner, not NanoPi R76S target evidence).
- The user marked P2.02 completed on 2026-10-20; all acceptance criteria of the [P2.02 plan](P2_02_CONTRACT_AND_FIRST_SLICE_PLAN.md) are checked, with the deviations of section 2.4. This is step completion, not P2 acceptance (P2.11/P2.12).
- Recorded by the orchestrating agent (run results and user decision).

## 3. P2.03 – CDC and MOSMIX list graphs

### 3.1 Contract approval (S0)
Decisions C-16 to C-27 (`P2_EXPORT_CONTRACT.md` section 7) approved by the user on 2026-10-20 after one review round: the user required C-27 regression tests in both formats (added to the plan as slice S6) and an explicit exemption of the constant `schema_version` from C-27. The enum type names `MosmixProduct` and `AvailabilityState`, not named by C-16, were chosen by the orchestrating agent (wire literals per C-23).

### 3.2 Slice evidence
A table `| Slice | Red test(s) and observed failure | Green / Refactor | Tests after slice |` with rows:
| S1 CDC list | `CdcStationListContractTests` (4 tests) failed: JSON `KeyNotFoundException` (PascalCase names, no `schema_version`), XML root "CdcStationList" instead of `{urn:weatherparser:export:v1}cdc_station_list`, empty `stations` element missing | Explicit names, constant `schema_version`, `retrieved_at` through `ExportTimestamp`, collections initialized with XML container/item names | 70 passed |
| S2 MOSMIX list | `MosmixLocationListContractTests` (6 tests) failed on the missing mapping (names, XML root `MosmixLocationList`) | Mapping plus enum wire values (`[XmlEnum]` and `[JsonStringEnumMemberName]` with `JsonStringEnumConverter<T>`); review corrections: `[XmlEnum]` wrongly placed on properties, uninitialized `Locations`/`Products`, two XML test defects (concatenated element text instead of `mosmix_id`, wrong navigation to the product items) | 76 passed |
| S3 ordering | `Order_CdcEntries_SortsByDistanceThenOrdinalId` and `Order_MosmixEntries_SortsByDistanceThenOrdinalId` failed: the stub returned the input order (for example `{"44", "00100", "00044", "99999"}` instead of `{"99999", "00044", "00100", "44"}`) | `OrderBy(distance).ThenBy(id, StringComparer.Ordinal)` | 83 passed |
| S4 Serializable | `ExportTypes_ReachableFromExportRoots_AreSerializable` failed naming the eight new types | `[Serializable]` on the six classes and two enums | 83 passed |
| S5 DI | `AddWeatherParsing_WithoutFurtherRegistration_ResolvesListEntryOrdering` failed: "No service for type 'WeatherParser.Parsing.Export.IListEntryOrdering' has been registered." | Singleton registration in `AddWeatherParsing` | 84 passed |
| S6 required members (C-27) | `ListRequiredMemberGuardTests`: every missing-member row failed with "no exception was thrown" except `RetrievedAtUtc` (already guarded since P2.02); the `schema_version` characterization failed with `NullReferenceException`, which exposed a defect: `MosmixLocationList.SearchParameters` was `null` by default | Internal value type `RequiredMember<T>`; `MosmixLocationList.SearchParameters` initialized | 104 passed |
| S6 completion | The orchestrating agent found `Latitude`, `Longitude`, `AltitudeM` and `AltitudeDifferenceM` of both entry types still unconverted (the first tests covered one representative member per type); the tests were extended to every required member (14 CDC and 16 MOSMIX keys); 16 rows failed as expected | Remaining members converted; Refactor: `MeasurementValue.Value` also uses `RequiredMember<decimal>` | 148 passed |

### 3.3 Coverage finding and characterization
The local coverage run after S6 measured 344/372 (92.47 %) — below the integration target 54/56 (96.43 %), so the strict-improvement gate would have failed. The uncovered blocks were the read-back paths of both list roots (`SchemaVersion` setter, `RetrievedAtWire` setter, `RetrievedAtUtc` getter). `ListReadBackTests` (7 characterization tests: JSON and XML read-back of both lists, rejection of other `schema_version` values, acceptance of `"1"`) closed the gap; they passed after correcting two qwen test defects (a nonexistent `.Element` member, JSON test data that used the XML item name).

### 3.4 Checks run on the working tree (2026-10-20)
- `dotnet build WeatherParser.Ci.slnf -c Release`: 0 warnings, 0 errors.
- Tests: `WeatherParser.Parsing.Tests` 155 passed, `WeatherParser.Coverage.Tests` 99 passed, `WeatherParser.Coverage.Runner.Tests` 56 passed.
- Local native block coverage of `WeatherParser.Parsing.dll`: 370 covered, 2 not covered (99.46 %); the two uncovered blocks are the pre-existing P1 constructor `ParserEndpointValidator(IEndpointUriValidator)`.
- The committed-candidate gate runs in the hosted CI of the draft pull request #3 against its PR base.

### 3.5 Authorship
qwen3-coder:30b generated all production code and tests of S1–S6, the refactoring and the characterization tests from the orchestrating agent's briefs and corrected its defects in further rounds. The orchestrating agent wrote the briefs (including verbatim builders for the C-27 tests), confirmed every failure reason, ran all builds, tests and coverage runs, found the incomplete C-27 conversion and the coverage gap, and itself applied three mechanical corrections: a missing `using` in `ServiceCollectionExtensionsTests`, an unguarded `RetrievedAtUtc` assignment in its own builder, and the removal of usings provided by ImplicitUsings in thirteen files.

Record note: qwen drafted the slice rows of 3.2 as a list; the orchestrating agent converted them into the table without changing any value.

### 3.6 Review fix: undefined and integer enum values (2026-10-20)

- **Finding**: Product = (MosmixProduct)99 and Availability = (AvailabilityState)99 were accepted; JSON wrote the integer `99` while XML failed with `ExportSerializationException`, violating C-23's fixed string vocabulary and JSON/XML consistency; both enum converters also accepted integers when reading JSON.
- **Regression tests**: `EnumVocabularyGuardTests` `Product_UndefinedValue_ThrowsArgumentException` and `Availability_UndefinedValue_ThrowsArgumentException` failed ("Expected a <System.ArgumentException> to be thrown, but no exception was thrown."); `JsonRead_IntegerProduct_IsRejected` and `JsonRead_IntegerAvailability_IsRejected` failed ("Expected a <System.Text.Json.JsonException> to be thrown, but no exception was thrown."); `Product_DefinedValues_AreAccepted`, `Availability_DefinedValues_AreAccepted` and `JsonRead_UnknownProductString_IsRejected` passed (characterization).
- **Fix**: the `Product` and `Availability` setters reject undefined values with `ArgumentException` (`Enum.IsDefined`), so an undefined value never reaches either serializer; both enums use the internal `StrictStringEnumConverter<TEnum>` (`allowIntegerValues: false`), so JSON neither reads nor writes integers.
- **Result**: `WeatherParser.Parsing.Tests` 162 passed, `WeatherParser.Coverage.Tests` 99, `WeatherParser.Coverage.Runner.Tests` 56; build 0 warnings; local native block coverage 390 covered, 2 not covered (99.49 %, the two pre-existing P1 blocks).
- **Authorship**: qwen3-coder:30b wrote the regression tests and the fix from the orchestrating agent's briefs; the orchestrating agent confirmed each failure reason and ran all builds, tests and the coverage run.

### 3.7 Hosted CI and completion (2026-10-20)

- Commit `0f59e1c` pushed to `features/develop/P2`; hosted CI run 39396695630 of draft pull request #3 (against its PR base) passed: `windows-x64` success, `ubuntu-x64` success, `debian13-arm64` success (Debian 13 ARM64 container on a GitHub-hosted runner, not NanoPi R76S target evidence).
- The user marked P2.03 completed on 2026-10-20; all acceptance criteria of the [P2.03 plan](P2_03_LIST_GRAPHS_PLAN.md) are checked. This is step completion, not P2 acceptance (P2.11/P2.12).
- Recorded by the orchestrating agent (run results and user decision).

## 4. P2.04 – UTC and source-time normalization

### 4.1 Contract approval (S0)

Decisions C-28 to C-35 (`P2_EXPORT_CONTRACT.md` section 8) were approved by the user on 2026-10-20 after one review round. The user's review found that the midnight rule could hide a DST ambiguity (retrieval 2026-10-25T01:15:00 UTC, display 02:45) and that non-nullable inputs could not represent a missing time. The fixes are the DST check before and after the midnight rule (C-31) and nullable inputs with valid midnight (C-29, C-33). The user chose a midnight tolerance of 5 minutes (C-30); a follow-up review aligned the P2.04 plan, which still called the tolerance OPEN.

### 4.2 Slice evidence

| Slice | Red test(s) and observed failure | Green / Refactor | Tests after slice |
|---|---|---|---|
| S1 explicit offsets | `NormalizeComplete_WithOffset_ReturnsUtcAndSourceQuality` failed: "Expected result.UtcInstant to be <2026-10-25 00:30:00>, but found <0001-01-01 00:00:00.000>." (stub) | `DateTimeOffset.UtcDateTime`, quality `source` | 163 passed |
| S2 Berlin local | `NormalizeCompleteLocal_BerlinLocal_ConvertsWinterAndSummer` failed (stub result `0001-01-01`; the strict resolver mock reported that `Resolve` was never called) | Conversion through the resolved Europe/Berlin zone; `source_timezone` `Europe/Berlin` | 164 passed |
| S3 DST gap/overlap | `NormalizeCompleteLocal_NonexistentLocalTime_IsRejected` found a raw `System.ArgumentException`; `NormalizeCompleteLocal_AmbiguousLocalTimeWithoutOffset_IsRejected`: no exception was thrown | `IsInvalidTime`/`IsAmbiguousTime` checks with `SourceTimeNormalizationException` | 166 passed |
| S4 date inference | Seven tests failed against the stub, including `InferDate_DisplayAmbiguousOnRetrievalDate_IsRejected` (retrieval 2026-10-25T01:15:00 UTC, display 02:45), `InferDate_RolledOverDateAmbiguous_IsRejected` (retrieval 2026-10-25T23:30:00 UTC, display 02:45), `InferDate_DisplayNonexistentOnRetrievalDate_IsRejected` and `InferDate_ToleranceBoundary_FiveMinutes` | Berlin date of the retrieval instant; DST check on the retrieval date, 5-minute midnight rule, DST check on the chosen date; `estimated_date` | 173 passed |
| S5 input guards | `Normalize_NullInputs_AreRejectedAndNeverUseRetrievalTime` found `System.InvalidOperationException`; `Normalize_NonUtcRetrievalInstant_IsRejected` and `NormalizeCompleteLocal_NonUnspecifiedKind_IsRejected` found `Moq.MockException` (the zone was resolved before any guard); `Normalize_SubSecondInput_IsRejected`: no exception was thrown. `InferDate_DisplayedMidnight_IsValid` passed (characterization) | Guards before the zone lookup in all three methods | 178 passed |
| S6 time-zone data | `Normalize_ResolverCannotProvideZone_RaisesDefinedFailure` (two rows) found raw `System.TimeZoneNotFoundException` and `System.InvalidTimeZoneException`; `TimeZoneResolverTests` passed (characterization) | `ResolveBerlin()` wraps both in `SourceTimeNormalizationException` naming the zone and the missing ICU/tzdata | 182 passed |
| S1 completion check | Characterization: `NormalizeComplete_SecondPassOfOverlapWithOffset_ReturnsUtc` (2026-10-25 02:30 +01:00 → 2026-10-25T01:30:00) passed immediately; the completion check found this worked example of C-28 unasserted | none | 186 passed (after S8) |
| S7 serialization | Characterization: `NormalizedTimes_SerializeWithoutSuffixInBothFormatsUnderDeDe` passed immediately | none | 183 passed |
| S8 DI | `AddWeatherParsing_WithoutFurtherRegistration_ResolvesTimeServices` failed: "No service for type 'WeatherParser.Parsing.Time.ITimeZoneResolver' has been registered." | Singleton registrations of both time services | 184 passed |

### 4.3 Checks run on the working tree (2026-10-20)

- `dotnet build WeatherParser.Ci.slnf -c Release`: 0 warnings, 0 errors.
- Tests: `WeatherParser.Parsing.Tests` 186 passed (185 before the completion check), `WeatherParser.Coverage.Tests` 99 passed, `WeatherParser.Coverage.Runner.Tests` 56 passed.
- Local native block coverage of `WeatherParser.Parsing.dll`: 508/512 after S8; the characterization test `Constructor_NullResolver_ThrowsArgumentNullException` covered the constructor guard, giving 510/512 (99.61 %); unchanged at 510/512 after the completion-check test. The two remaining uncovered blocks are the pre-existing P1 constructor `ParserEndpointValidator(IEndpointUriValidator)`.
- Time-zone data on this machine: Windows ICU data used by .NET 10.0.13. The Debian ARM64 behavior (tzdata) is verified in P2.11.
- Committed as `15928cf` and pushed to draft pull request #3 with the completion records; hosted CI run 39403102531 passed: `windows-x64` success, `ubuntu-x64` success, `debian13-arm64` success (Debian 13 ARM64 container on a GitHub-hosted runner, not NanoPi R76S target evidence).

### 4.4 Authorship

qwen3-coder:30b generated all production code and tests of S1–S8 and the characterization tests from the orchestrating agent's briefs, and corrected its own defects in further rounds (garbled using lines that broke the build in S3, a Red test omitted in S6). The orchestrating agent wrote the briefs including the exact inference algorithm, confirmed every failure reason, ran all builds, tests and coverage runs, and applied mechanical corrections itself: removal of duplicate and unused usings and moving the `using` of `SourceTimeNormalizer` before the namespace. qwen's draft of this section lost the failure evidence and misstated the coverage sequence, so the orchestrating agent wrote this section from its verified records.

### 4.5 Completion (2026-10-20)

- The user reviewed the P2.04 implementation without findings and marked P2.04 completed on 2026-10-20; all acceptance criteria of the [P2.04 plan](P2_04_TIME_NORMALIZATION_PLAN.md) are checked. This is step completion, not P2 acceptance (P2.11/P2.12).
- Before checking "Every example value from the behavior facts asserted", the orchestrating agent found the +01:00 example of C-28 unasserted; qwen3-coder:30b added the characterization test from the agent's brief, and the agent re-ran build (0 warnings), tests (186/99/56 passed) and coverage (510/512).
- Recorded by the orchestrating agent (user decision and run results).

## 5. P2.05 – Units, pressure and output rounding

### 5.1 Contract approval (S0)

Decisions C-36 to C-47 (`P2_EXPORT_CONTRACT.md` section 9) were approved by the user on 2026-10-20 after one review round. The user's review found that the single pressure bound of C-41 ignored that the REQ-054 factors are not exact reciprocals (0.7500617 × 1.3332239 = 1.00000018491463; 100000000 mmHg gives a difference of 18.491463 mmHg) and that C-44 accepted sub-second period endpoints that the whole-second timestamp mapping cannot export. The fixes are bounds per original unit including the factor discrepancy (C-41) and the rejection of sub-second endpoints (C-44); the plan's S2 and S4 Red tests were updated. The user chose the wind-direction range [0, 360] with 360 normalized to 0 (C-43).

### 5.2 Slice evidence

| Slice | Red test(s) and observed failure | Green / Refactor | Tests after slice |
|---|---|---|---|
| S1 output rounding | `Round_UsesIncrementAndAwayFromZero` against a naive `decimal.Round(value, places)`: "Expected ... to be "20.3", but "20.2" differs near "2" (index 3)." and likewise −20.25 → −20.2, 1000.5 → 1000, 54.5 → 54, 12.5 → 12, 36.5 → 36; `Round_ResultScaleEqualsIncrementDigits` (20 → "20" instead of "20.0"); `Round_NegativeZeroResult_IsReturnedAsZero` ("Expected decimal.IsNegative(result) to be False, but found True."); `Round_UndefinedKind_ThrowsArgumentOutOfRangeException` (no exception). 15 rows without a tie, the scale-reduction rows and the unchanged kinds passed immediately (characterization) | `MidpointRounding.AwayFromZero`, scale fixed to the increment, negative zero returned as zero, undefined kinds rejected; increments in one `switch` keyed by `QuantityKind` (the Refactor target was met in Green) | 210 passed |
| S2 pressure | Against a converter that rounds hPa before deriving mmHg: `Convert_FromHpa_DerivesBothOutputsFromUnroundedOriginal` "Expected result.MmHg to be 750M, but found 751M"; `Convert_FromHpa_RoundsTheUnroundedMmHgValue` (strict mock: `Round(Pressure, 750.8117617)` not set up); both bound sweeps (h = 800.5: mmHg 601 differs by 0.57560915 from 600.42439085; m = 601.2: hPa 801 differs by 0.53420868 from 801.53420868); undefined reference and null constructor argument (no exception). `Convert_FromMmHg_UsesReverseFactor`, the three reference rows and the 100000000 mmHg case passed immediately (characterization) | Both outputs from the unrounded original through `IOutputRounding`; factors as named, documented constants (REQ-054); reference and constructor guards | 221 passed |
| S3 validity | 37 of 56 failed against a stub accepting every value: non-finite rows threw `System.OverflowException`; missing values returned 0 instead of `null`; humidity, wind-direction and kind ranges were accepted ("Expected boolean to be False, but found True."); 360 stayed 360; values beyond the `decimal` range threw `OverflowException`; undefined kind (no exception). The 19 accepting rows passed immediately (characterization) | Missing → `NonFinite` → `decimal` range → kind range; 360 normalized to 0 | 277 passed |
| S3 review fix | The orchestrating agent found that `(double)decimal.MaxValue` rounds above `decimal.MaxValue`, so the guard `>` let that value reach the cast. Rows ±7.922816251426434E+28 failed with `System.OverflowException` | Guard changed to `>=` | 279 passed |
| S4 periods | 9 of 13 failed against stubs: `AveragingPeriod` end not after start, non-UTC start or end, sub-second start (one tick) or end (one millisecond) (no exception); 360000 J/m² over 600 s returned 360000 (expected 600), 1800000 over 1 h (500), 0.5 mm over 10 min (3); missing period (no exception); unrounded 0.1 mm over 7 min. Endpoint/duration, one-second period, retained period and 1.2 mm over 1 h (identity coincidence) passed | UTC, whole-second and positive-duration checks with `ArgumentException`; division by whole seconds, rain rate × 3600; missing period → `QuantityConversionException`; one shared code path (no separate Refactor needed) | 292 passed |
| S5 kind separation | 5 of 7 failed ("no exception was thrown"): gust → mean wind, illuminance ↔ irradiance, probability → accumulation, an allowed source with a wrong target, and the exhaustive pair test over all 144 kind pairs. Missing period and the two defined pairs passed | Pair check before the period check; every pair except the two defined ones raises `QuantityConversionException` naming both kinds | 299 passed |
| S6 wind speed | Against an identity stub: "36.0" expected but "10", "12.492" but "3.47", "0.0" but "0", "1.80" but "0.5"; rounding after conversion gave 3 instead of 12 | Exact factor 3.6, unrounded | 304 passed |
| S7 DI | `AddWeatherParsing_WithoutFurtherRegistration_ResolvesQuantityServices` and `AddWeatherParsing_QuantityServices_AreSingletonsPerContainer` failed: "No service for type 'WeatherParser.Parsing.Quantities.IOutputRounding' has been registered." | Singleton registrations of the five services | 306 passed |

### 5.3 Checks run on the working tree (2026-10-20)

- `dotnet restore WeatherParser.Ci.slnf --locked-mode`: succeeded. `dotnet build WeatherParser.Ci.slnf -c Release`: 0 warnings, 0 errors.
- Tests: `WeatherParser.Parsing.Tests` 306 passed (314 after the review fixes in section 5.6), `WeatherParser.Coverage.Tests` 99 passed, `WeatherParser.Coverage.Runner.Tests` 56 passed.
- Local native block coverage of `WeatherParser.Parsing.dll`: 703/710 after S7. The uncovered blocks were the compiler-generated static constructors of `PressureConverter` (3) and `WindSpeedConverter` (2): a `const decimal` is emitted as a `static readonly` field initialized in a static constructor while every use is inlined, so that constructor never runs. After changing the factors to `static readonly decimal` (behavior unchanged, all tests green): 705/707 (99.72 %). The two remaining uncovered blocks are the pre-existing P1 constructor `ParserEndpointValidator(IEndpointUriValidator)`. After the review fixes of section 5.6: 727/729 (99.73 %), same two blocks.
- Acceptance checks: no lux ↔ W/m² path exists (exhaustive pair test); no service rounds before converting (`PressureConverter` rounds both outputs from the original, `WindSpeedConverter` and `PeriodRateConverter` return unrounded values).
- Committed as `02393bb` together with the review fixes of section 5.6; hosted CI result in section 5.7.

### 5.4 Deviations and design notes

1. `PressureUnit` (`Hectopascal`, `MillimetreOfMercury`) types the `OriginalUnit` of C-40; C-36 does not list it by name.
2. The wire values of `PressureReference` (`sensor`, `sea_level`, `unknown`) are mapped when the value is placed into the observation graph (P2.06); P2.05 defines only the enum.
3. The S4 Green was deliberately minimal (formula chosen by the source kind only), so that the S5 pair checks had a genuine Red.
4. `QuantityKindSeparationTests` also asserts that the failure message names both kinds; qwen added this assertion unasked, and the orchestrating agent kept it.

### 5.5 Authorship

qwen3-coder:30b drafted the contract section 9 and its review revision, and generated all production code and tests of S1–S7, the S3 regression rows and fix, and the `static readonly` refactoring from the orchestrating agent's briefs; it corrected its own defects in further rounds (missing and redundant usings, MSTest assertions instead of FluentAssertions, a duplicate record constructor, signed instead of absolute bound checks, an unavailable `ExpectedException` attribute). The orchestrating agent wrote the briefs, confirmed every failure reason, ran all builds, tests and coverage runs, found the decimal-range boundary and the uncoverable static constructors, and applied mechanical corrections itself where qwen repeatedly failed: the C-41 wording (origin labels, a duplicated fragment, a `|` inside the table), file-scoped namespaces and removal of implicit usings in the S4 files and two others, restoring the naive S4 stubs and the per-endpoint non-UTC test, restoring both S4 test files after qwen rewrote them unasked during Green, the two "end" messages in `AveragingPeriod`, `BeCloseTo` → `BeApproximately`, comment placement in `WindSpeedConverterTests`, and replacing invented collection assertions in the S7 test. The orchestrating agent wrote this section from its verified records, because it records run results.

### 5.6 Review fixes (2026-10-20)

The user reviewed the uncommitted P2.05 implementation and reported two findings; both were fixed test-first, and contract rows C-42 and C-45 record the clarified behavior.

| Finding | Red test(s) and observed failure | Green / Refactor | Tests after fix |
|---|---|---|---|
| 1. The range check ran after the cast to `decimal`, which rounds: humidity `100.00000000000001` became valid 100, direction `360.00000000000006` became north (0), illuminance `-1e-30` became valid zero | `Validate_OutOfRangeBeforeDecimalConversion_IsRejected`: rows humidity 100.00000000000001, direction 360.00000000000006, illuminance −1e-30 and probability −1e-30 failed ("Expected boolean to be False, but found True."); the row pressure 1e-30 passed because the decimal value 0 failed the existing `> 0` check (it guards that check) | Range check on the original `double` before the conversion. Refactor: coverage showed two unreachable blocks in the decimal re-check (the conversion preserves order and the inclusive bounds 0, 100, 360 are exact in both types), so the re-check was reduced to the strict pressure bound (`number <= 0m`) | 311 passed (the refactoring ran after fix 2: 314 passed) |
| 2. `value * 3600m / seconds` overflowed for 1e26 mm over one hour although the result is exactly 1e26 mm/h | `ToMeanRainRate_LargeAccumulationOverOneHour_IsExact` and `ToMeanRainRate_IntermediateOverflowWithRepresentableResult_IsComputed` (1e26 mm over 7 s, result ≈ 5.14e28) failed with `System.OverflowException`; `ToMeanRainRate_UnrepresentableResult_RaisesQuantityConversionException` (`decimal.MaxValue` over 1 s) found `System.OverflowException` instead of `QuantityConversionException` | 3600 and the period seconds are reduced by their greatest common divisor; the product is tried before the quotient; an unrepresentable result raises `QuantityConversionException` with the overflow as inner exception (new constructor `(string, Exception)`) | 314 passed |

Authorship: qwen3-coder:30b generated the regression tests, both fixes and the refactoring from the orchestrating agent's briefs, each on the first try. The orchestrating agent wrote the briefs, added the pressure 1e-30 row and the two overflow cases beyond the user's examples, confirmed the failure reasons, found the unreachable blocks, ran all checks, removed two re-added `using System;` lines, extended the exception summary and the validator remark, and wrote the contract clarifications and this section.

### 5.7 Hosted CI and completion (2026-10-20)

- Commit `02393bb` pushed to `features/develop/P2`; hosted CI run 39411112653 of draft pull request #3 (against its PR base) passed: `windows-x64` success, `ubuntu-x64` success, `debian13-arm64` success (Debian 13 ARM64 container on a GitHub-hosted runner, not NanoPi R76S target evidence).
- The user asked to mark P2.05 completed once the hosted CI succeeded; all acceptance criteria of the [P2.05 plan](P2_05_UNITS_PRESSURE_ROUNDING_PLAN.md) are checked. This is step completion, not P2 acceptance (P2.11/P2.12).
- Recorded by the orchestrating agent (run results and user decision).

## 6. P2.06 – Observation graph

### 6.1 Contract approval (S0)

qwen3-coder:30b drafted decisions C-48 to C-62 (section 10 of `P2_EXPORT_CONTRACT.md`). At the user's request the draft went through read-only reviews by the OpenAI Codex CLI before approval: 8, 6, 5, 3, 3 and 2 findings in the first six reviews (the user extended the loop after the fifth), 0 findings in the seventh. The orchestrating agent confirmed every finding against the code or by reproduction (for example the regular expression `$` matching before a trailing line feed, the interval arithmetic of the pressure example, the scale of 7e28) and had qwen revise the rows; the review added C-63 (converter overflow), C-64 (graph validation on serialization) and C-65 (unrepresentable output scale). The user chose C-50 (`provider:station_id`), C-51 (latitude and longitude required) and C-55 (latest reading time) and approved C-48 to C-65 on 2026-10-20.

### 6.2 Slice evidence

| Slice | Red test(s) and observed failure | Green / Refactor | Tests after slice |
|---|---|---|---|
| S1 shape | `ObservationDatasetContractTests` against stub members ignored by both serializers: JSON `KeyNotFoundException` and XML `NullReferenceException` at `location`; `ExportTypes_ReachableFromExportRoots_AreSerializable` named `PressureReference` and `TimestampQuality` | Members mapped in both formats, enum wire values (C-61), `[Serializable]`; 24 existing tests then failed on the now-required `measured_at` and were migrated to the fixture `ObservationTestData` (no assertion removed or added, checked by diff) | 316 passed |
| S1b required members and setter rules | `ObservationRequiredMemberGuardTests` and `ObservationSetterRuleTests`: 39 of 78 failed (unassigned `source`, `location`, enums, identity and coordinates serialized; setter rules absent: "no exception was thrown"); already guarded members passed (characterization) | Assignment tracking (C-27 pattern), identity and coordinate checks, `source_timezone` and `quality` setters, undefined-enum guards | 395 passed |
| S1c graph validation (C-64) | `ObservationGraphValidationTests`: all 25 violating graphs were serialized; 11 valid graphs accepted (characterization) | Internal `IExportRootValidation` implemented by `ObservationDataset`, called by both serializers inside their failure wrapping; tests that used `source` as a text carrier moved to `location.name`; zero temperatures written as `0.0` | 431 passed |
| C-63/C-65 | `FromMmHg(decimal.MaxValue)` and `ToKilometresPerHour(3e28)` raised `OverflowException`; `Round(Temperature, 7e28)` returned scale 0 | `QuantityConversionException` with inner `OverflowException`; scale check in `OutputRounding`. Refactor: removed the unreachable overflow wrap in `FromHpa` (factor below 1) | 436 passed |
| S2–S7 builder | One cycle against a naive stub (times stamped with the retrieval time, mapping by unit only, no validation, conversion, rounding, diagnostics or rejection): 16 of 20 behavior tests (`ObservationDatasetBuilderTests`) and 36 of 36 rule tests (`ObservationBuilderRulesTests`) failed for the slice reasons | `ObservationDatasetBuilder` (C-57 to C-60): input validation, duplicate detection, omission precedence, conversions and rounding through the P2.05 services, dataset time and quality | 492 passed |
| S8 serializable and equivalence | Characterization: `Serialize_FullObservation_JsonAndXmlAreSemanticallyEqual` and the traversal test passed immediately (attributes added in S1) | none | 493 passed |
| S9 DI | Both DI tests failed: "No service for type 'WeatherParser.Parsing.Observations.IObservationDatasetBuilder' has been registered." | Singleton registration | 495 passed |
| Coverage gaps | Characterization tests for the pressure-pair branches (`ObservationPressurePairValidationTests`), builder input rules (`ObservationBuilderInputTests`) and read-back (`ObservationReadBackTests`) | none | 511 passed |

### 6.3 Implementation review by Codex

The first read-only review of the uncommitted implementation reported four findings, each confirmed by a Red regression test in `ObservationReviewRegressionTests`: null `measurements` leaked a `NullReferenceException` from XML serialization; an invalid graph with a cancelled token raised `ExportSerializationException` in JSON instead of `OperationCanceledException` (C-10); the builder called the validator before detecting a duplicate (C-57); JSON wrote `pressure_reference` before the inherited members (C-53 order). Fixes: null check first in the graph validation, cancellation checked before validation, two-pass builder (validation and duplicates first), `[JsonPropertyOrder]` on the value members. The second review reported no findings. A test for directly mapped irradiance and rain rate closed the coverage gap the two-pass change revealed.

### 6.4 Checks run on the working tree (2026-10-20)

- `dotnet restore WeatherParser.Ci.slnf --locked-mode`: succeeded. `dotnet build WeatherParser.Ci.slnf -c Release`: 0 warnings, 0 errors.
- Tests: `WeatherParser.Parsing.Tests` 516 passed, `WeatherParser.Coverage.Tests` 99 passed, `WeatherParser.Coverage.Runner.Tests` 56 passed.
- Local native block coverage of `WeatherParser.Parsing.dll`: 1643/1645 (99.88 %); the two uncovered blocks are the pre-existing P1 constructor `ParserEndpointValidator(IEndpointUriValidator)`.
- Committed as `90106eb`; hosted CI result in section 6.7.

### 6.5 Deviations and design notes

1. S2–S7 ran as one Red/Green cycle against one naive stub with test groups per slice, instead of one cycle per slice.
2. `MeasurementValue` is no longer sealed: `PressureMeasurementValue` derives from it (C-54).
3. Graph validation lives in the export root through the internal interface `IExportRootValidation`, because the serializers are generic.
4. The contract test fixture `ObservationTestData` builds a dataset that is complete under the contract; P2.02 to P2.05 tests now use it.
5. During the step the qwen invocation changed (user-provided analysis, recorded in `AGENTS.md`): `/api/chat` with a fixed system policy, the real source of referenced types as API reference, one authorized file per request and an allowlist when applying output.

### 6.6 Authorship

qwen3-coder:30b drafted the contract rows and their revisions, generated the entities, the graph validation, the builder, the converter amendments, the DI registration and most tests from the orchestrating agent's briefs. The model had to be restarted twice (once by the user). With the earlier single-prompt invocation it repeatedly invented project APIs and rewrote files it was not given, so the orchestrating agent restored reviewed files and applied mechanical corrections (missing namespaces and usings, member paths, test data, contract references in messages) and, under the AGENTS.md exception after repeated failed briefs, wrote `ObservationBuilderRulesTests`, `ObservationExportEquivalenceTests` and the two DI tests itself. The OpenAI Codex CLI reviewed the contract draft and the implementation read-only. The orchestrating agent confirmed every failure reason and finding, ran all builds, tests, coverage and reviews, and wrote this section from its verified records because it records run results.

### 6.7 Hosted CI and completion (2026-10-20)

- Commit `90106eb` pushed to `features/develop/P2`; hosted CI run 39431824712 of draft pull request #3 (against its PR base) passed: `windows-x64` success, `ubuntu-x64` success, `debian13-arm64` success (Debian 13 ARM64 container on a GitHub-hosted runner, not NanoPi R76S target evidence).
- The user asked to mark P2.06 completed once the hosted CI succeeded; all acceptance criteria of the [P2.06 plan](P2_06_OBSERVATION_GRAPHS_PLAN.md) are checked. This is step completion, not P2 acceptance (P2.11/P2.12).
- Recorded by the orchestrating agent (run results and user decision).

## 7. P2.07 – Forecast run

### 7.1 Contract approval (S0)

qwen3-coder:30b drafted decisions C-66 to C-81 (section 11 of `P2_EXPORT_CONTRACT.md`) through the `/api/chat` invocation of AGENTS.md. Before approval, read-only reviews by the OpenAI Codex CLI reported 7, 2 and 0 findings; the orchestrating agent confirmed every finding, qwen revised the rows, and the orchestrating agent applied three edits qwen omitted (C-68 alternative, C-76 interval-kind errors, the field-mapping wire members). The user approved C-66 to C-81 on 2026-10-20.

### 7.2 Slice evidence

| Slice | Red test(s) and observed failure | Green / Refactor | Tests after slice |
|---|---|---|---|
| S1 shape and standalone root | `ForecastRunContractTests` against stub entities without serializer attributes: `KeyNotFoundException` at `schema_version` and `dataset_type` | Serializer mapping, `[Serializable]`, `forecast_run` root, constants, wire timestamps, C-05 omission, `mean` wire value | 520 passed |
| S2 no `measured_at` | Characterization: `ForecastTypes_HaveNoMeasuredAtMember` and `Serialize_ForecastRun_ContainsNoMeasuredAt` passed immediately | none | 520 passed |
| Entity rules (C-67, C-69, C-70, C-72, C-74, C-80) | `ForecastRequiredMemberGuardTests` and `ForecastSetterRuleTests`: unassigned members serialized and setter rules absent ("no exception was thrown"); unset timestamps passed (characterization) | Assignment tracking, name, coordinate, MOSMIX ID and enum guards | 579 passed |
| C-79 graph validation | `ForecastGraphValidationTests`: all 17 violating graphs serialized | `ForecastRun` implements `IExportRootValidation` | 596 passed |
| S3–S7 builder | One cycle against a naive stub (input order kept, missing values filtered before pairing, mapping by unit only): 11 of 11 behavior tests and 30 of 35 rule tests failed for their slice reasons (14:00 first instead of 12:00; 3 steps instead of 2; no intervals; 20.25 unrounded; no diagnostics; wrong failure types); 5 rule rows passed through entity setters (characterization) | `ForecastRunBuilder` (C-75 to C-78): input and provenance checks before any service call, index-aligned pairing, omission diagnostics, conversions and rounding through the P2.05 services, sorting, rejection without usable values | 642 passed |
| S8 serializable and equivalence | Characterization: traversal with the `ForecastRun` root and `Serialize_FullForecastRun_JsonAndXmlAreSemanticallyEqual` passed immediately | none | 644 passed |
| S9 DI | Both DI tests failed: "No service for type 'WeatherParser.Parsing.Forecasts.IForecastRunBuilder' has been registered." | Singleton registration | 645 passed |
| Coverage gaps | Read-back (`ForecastReadBackTests`) and branch tests; the read-back test found a defect: the `dataset_type` setter rejected every value, so a run could not be read back (JSON `ArgumentException`, XML "There is an error in XML document") | Setter accepts the constant (C-67) | 676 passed |

### 7.3 Implementation review by Codex

Round 1 reported five findings, each confirmed by a Red regression test in `ForecastReviewRegressionTests`: a missing series kind fell back to `Temperature` (`InvalidOperationException`), an undefined series product was classified as a run rejection, a `null` step leaked a `NullReferenceException` from XML serialization, `dataset_type` was serialized last (C-67 order), and an XML `nil` assertion was vacuous. Round 2 reported three findings: pressure consistency arithmetic overflowed before the range check (`OverflowException` in XML), malformed series MOSMIX IDs were classified as run rejections, and three test classes did not create their subjects in `[TestInitialize]`. All were fixed test-first; rounds 3 and 4 reported no findings.

### 7.4 Checks run on the working tree (2026-10-20)

- `dotnet restore WeatherParser.Ci.slnf --locked-mode`: succeeded. `dotnet build WeatherParser.Ci.slnf -c Release`: 0 warnings, 0 errors.
- Tests: `WeatherParser.Parsing.Tests` 684 passed, `WeatherParser.Coverage.Tests` 99 passed, `WeatherParser.Coverage.Runner.Tests` 56 passed.
- Local native block coverage of `WeatherParser.Parsing.dll`: 2708/2710 (99.93 %); the two uncovered blocks are the pre-existing P1 constructor `ParserEndpointValidator(IEndpointUriValidator)`. An intermediate measurement of 2574/2698 was below the PR base and was closed with characterization tests; unreachable branches left by review fixes were removed by refactoring.
- Committed as `41c29d6`; hosted CI result in section 7.7.

### 7.5 Deviations and design notes

1. S3–S7 ran as one Red/Green cycle against one naive stub with separate behavior and rule test files.
2. `MosmixProduct` is reused from P2.03; steps without values are omitted; run provenance on each series replaces the plan's separate mixed-input check (C-75, C-76).
3. Graph validation reuses the internal `IExportRootValidation` of P2.06.
4. The fixture `ForecastTestData` (written by the orchestrating agent) builds a contract-complete run.

### 7.6 Authorship

qwen3-coder:30b (through the `/api/chat` invocation of AGENTS.md) drafted the contract rows and generated the entities, the graph validation, the builder and most tests from the orchestrating agent's briefs. Where qwen deviated from the briefs (scope creep with tracking during S1 Green, copied validation code, invented members such as `StationId` and a 12-character ID rule, weakened or vacuous assertions, missing data rows), the orchestrating agent reverted or corrected the output, applied mechanical fixes, and under the AGENTS.md exception wrote `ForecastRunBuilderRulesTests`, the fixture and the two rewritten regression tests itself. The OpenAI Codex CLI reviewed the contract draft and the implementation read-only. The orchestrating agent confirmed every failure reason and finding, ran all builds, tests, coverage and reviews, and wrote this section from its verified records because it records run results.

### 7.7 Hosted CI and completion (2026-10-20)

- Commit `41c29d6` pushed to `features/develop/P2`; hosted CI run 39446243064 of draft pull request #3 (against its PR base) passed: `windows-x64` success, `ubuntu-x64` success, `debian13-arm64` success (Debian 13 ARM64 container on a GitHub-hosted runner, not NanoPi R76S target evidence).
- The user marked P2.07 completed on 2026-10-20; all acceptance criteria of the [P2.07 plan](P2_07_FORECAST_GRAPHS_PLAN.md) are checked. This is step completion, not P2 acceptance (P2.11/P2.12).
- Recorded by the orchestrating agent (run results and user decision).

## 8. P2.08 – Serializer envelope, errors and supported roots

### 8.1 Contract approval (S0)

qwen3-coder:30b drafted decisions C-82 to C-94 (section 12 of `P2_EXPORT_CONTRACT.md`) through the `/api/chat` invocation of AGENTS.md. Before approval, read-only reviews by the OpenAI Codex CLI reported 3, 3 and 0 findings (round 1: partial writes during the copy, XML newline handling, the C-23 availability invariant; round 2: duplicate products, one outcome per source, the deferred builder/validation refactor); the orchestrating agent confirmed every finding and qwen revised the rows. The orchestrating agent restored an example paragraph qwen dropped, added backticks to the mapping paths and removed leftover "Recommended:" prefixes. The user approved C-82 to C-94 on 2026-10-20 and chose the recommended options of C-84 (both result kinds allowed), C-87 (no extra partial-result member) and C-90 (keep `IExportRootValidation`).

### 8.2 Slice evidence

| Slice | Red test(s) and observed failure | Green / Refactor | Tests after slice |
|---|---|---|---|
| S1 envelope | `ExportDocumentContractTests` against a stub without attributes and with null collections: JSON names `{"SchemaVersion", "Observations", "ForecastRuns"}` differ at index 0; the empty-envelope test failed on the missing `observations` member | `export_document` root, member names, XML containers and items, collections initialized empty | 686 passed |
| S2 typed errors | Envelope tests extended for `errors` and the two partial-result tests; `ExportErrorRuleTests`: 32 failures (JSON name `Errors` at index 3; no XML `error` element; every setter-rule row "no exception was thrown"; unassigned members serialized). Kept-value rows passed (characterization) | `errors` mapping; `ExportError` with C-27 tracking and C-86 setter rules | 723 passed |
| S3 supported roots | `SupportedRootTests`: all five unsupported roots (`string`, dictionary, anonymous type, `object`, `ForecastStep`) were serialized ("no exception was thrown"); the five supported roots and the cancellation-before-root order passed immediately (characterization) | `SupportedExportRoots` checked after the cancellation check in both serializers; `ExportSerializationException(string)` constructor | 734 passed |
| S8 serializable graph | `ExportEntitySerializableTests` with all five roots: "found at least these items {ExportDocument, ExportError}" | `[Serializable]` on both | 734 passed |
| S10 graph validation (C-91) | `ExportGraphValidationTests` (13 envelope rows) and `ListGraphValidationTests` (15 list rows): every violating graph was serialized ("no exception was thrown") | `ExportDocument`, `CdcStationList` and `MosmixLocationList` implement `IExportRootValidation` | 764 passed |
| C-92 line breaks (regression) | `LineBreakPreservationTests`: XML read-back differed at index 5 (the CR was lost) and the XML text contained a raw CR (default `NewLineHandling.Replace`); JSON read-back passed | `NewLineHandling.Entitize` in the XML writer | 768 passed |
| S4, S5, S6, S7, S9 | Characterization over all five roots, passed immediately: `AllRootsEquivalenceTests` (6), `AllRootsCultureTests` (10), `SerializerStreamBehaviourTests` (20), `AllRootsDeterminismTests` (11) | none | 815 passed |
| Coverage gap | `ExportDocumentReadBackTests`: JSON and XML read-back with byte-identical re-serialization; `schema_version` setter (characterization) | none | 819 passed |

Plan S10 tests for observation datasets and forecast runs (`ObservationGraphValidationTests`, `ForecastGraphValidationTests`) already existed from P2.06 and P2.07 and pass unchanged; the wrong `dataset_type` is rejected by its setter (`ForecastSetterRuleTests`), as recorded in the deviation list of contract section 12.

### 8.3 Test IDs completed

| T-P2 ID | Tests |
|---|---|
| T-P2-01 | `ExportEntitySerializableTests` (all five roots, including `ExportDocument` and `ExportError`) |
| T-P2-05 | `AllRootsCultureTests` |
| T-P2-15 | `ExportDocumentContractTests`, `ExportErrorRuleTests`, `AllRootsDeterminismTests.SerializeTwice_IdenticalInput_ProducesIdenticalBytes` |
| T-P2-16 | `AllRootsEquivalenceTests`, `AllRootsDeterminismTests.SameInstance_SerializesToBothFormatsWithoutMutation`, `ExportDocumentReadBackTests` |
| T-P2-17 | `AllRootsEquivalenceTests.Envelope_KeepsSpecialTextZeroOmissionAndLeadingZerosInBothFormats`, `LineBreakPreservationTests` |
| T-P2-18 | `SupportedRootTests`, `SerializerStreamBehaviourTests`, `ExportGraphValidationTests`, `ListGraphValidationTests`, `JsonFailureWrappingTests` |

### 8.4 Implementation review by Codex

Round 1 reported three findings, all confirmed: the stream tests did not prove that the caller's token reaches the destination write or cover a failure after a partial write; `JsonFailureWrappingTests` had become vacuous because its unsupported root type is now rejected before `JsonSerializer` runs; caller order was not tested with unsorted input. qwen added `SerializeAsync_TokenCanceledDuringCopy_PropagatesAndLeavesStreamOpen`, `SerializeAsync_DestinationFailsAfterPartialWrite_PropagatesAndLeavesStreamOpen`, a supported-root wrapping test and `Serialize_UnsortedItems_KeepCallerOrderInBothFormats`. A mutation check by the orchestrating agent (JSON copy with `CancellationToken.None`) made the five JSON copy-cancellation rows fail and was reverted. Round 2 reported no findings.

### 8.5 Checks run on the working tree (2026-10-20)

- `dotnet restore WeatherParser.Ci.slnf --locked-mode`: succeeded. `dotnet build WeatherParser.Ci.slnf -c Release`: 0 warnings, 0 errors.
- Tests: `WeatherParser.Parsing.Tests` 830 passed, `WeatherParser.Coverage.Tests` 99 passed, `WeatherParser.Coverage.Runner.Tests` 56 passed.
- Local native block coverage of `WeatherParser.Parsing.dll`: 3031/3033 (99.93 %); the two uncovered blocks are the pre-existing P1 constructor `ParserEndpointValidator(IEndpointUriValidator)`. An intermediate measurement of 3019/3033 left the envelope's `schema_version` setter uncovered and was closed with the read-back tests.
- Committed as `7c9b919`; hosted CI result in section 8.8.

### 8.6 Deviations and design notes

1. Graph validation stays in the internal `IExportRootValidation` (C-90); no `IExportGraphValidator` and no registration change.
2. Builders and root validation remain separate implementations of the same rules (open risk); `ObservationExportEquivalenceTests` and `ForecastRunExportEquivalenceTests` pass builder results through both serializers, so a divergence fails a test.
3. C-92 changes the XML newline handling of every root; this is a behavior fix of the P2.02 serializer, introduced with a Red regression test.
4. Shared fixtures `ListTestData` and `AllRootsTestData` build contract-complete roots; serialization uses the concrete static type because `object` roots are rejected (C-88).
5. Cancellation stays coarse for XML (before serialization and during the copy, C-10); a partial document may remain in the destination after a copy-phase failure (C-89).

### 8.7 Authorship

qwen3-coder:30b (through the `/api/chat` invocation of AGENTS.md) drafted the contract rows and generated all entities, the root registry, the validation, the serializer changes, all tests and both fixtures from the orchestrating agent's briefs. Where qwen deviated from the briefs (unrequested validation and usings, attributes added to a stub, an inverted assertion, weakened `ContainInOrder` checks, a corrupted using directive, hand-built fixtures, `MemoryStream`-derived test streams that bypassed the asynchronous write path), the defects were returned to qwen with compiler output or test results; the orchestrating agent applied only mechanical corrections (removed comment lines and redundant using directives, moved two assertions, removed three stub attributes). The OpenAI Codex CLI reviewed the contract draft and the implementation read-only. The orchestrating agent confirmed every failure reason and finding, ran all builds, tests, coverage and reviews, and wrote this section from its verified records because it records run results.

### 8.8 Hosted CI and completion (2026-10-20)

- Commit `7c9b919` pushed to `features/develop/P2`; hosted CI run 39466213992 of draft pull request #3 (against its PR base) passed: `windows-x64` success, `ubuntu-x64` success, `debian13-arm64` success (Debian 13 ARM64 container on a GitHub-hosted runner, not NanoPi R76S target evidence).
- The user marked P2.08 completed on 2026-10-20; all acceptance criteria of the [P2.08 plan](P2_08_SERIALIZER_ENVELOPE_PLAN.md) are checked. This is step completion, not P2 acceptance (P2.11/P2.12).
- Recorded by the orchestrating agent (run results and user decision).

## 9. P2.09 – Reproducible output XSDs

### 9.1 S0 generator feasibility spike (2026-10-20)

The throwaway test `SchemaExporterSpikeTests.Spike_ExportSchemas_ForAllFiveRoots` (generated by qwen3-coder:30b) imports the `XmlSerializer` mappings of the five roots with one `XmlReflectionImporter`, exports them with one `XmlSchemaExporter` and writes the platform facts, each schema text and its SHA-256 to the test output. The schemas were written with `XmlWriterSettings` UTF-8 without BOM, two-space indentation and `\n` line endings. Target access to the NanoPi R76S is not authorized; the ARM64 run used the hosted `debian13-arm64` job (Debian 13 container on a GitHub-hosted ARM64 runner), as the plan allows.

| Platform | Run | Build and test command | TRX | Spike test executed |
|---|---|---|---|---|
| Windows 11 (10.0.26200) X64, SDK 10.0.402, runtime .NET 10.0.13 | Local working tree | `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, then `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~SchemaExporterSpikeTests" --logger "trx;LogFileName=p2-09-spike.trx"` | `p2-09-spike.trx` | 1 executed, passed |
| Windows Server (10.0.26100) X64, .NET 10.0.13 | Hosted CI run 39472096093, commit `f358d0c` | `scripts/ci-local.ps1` (full build, unfiltered test run with TRX logger) | `current/trx/WeatherParser.Parsing.Tests.trx` | 1 executed, passed (831 executed in total) |
| Ubuntu 24.04.5 LTS X64, .NET 10.0.13 | same run | same | same | 1 executed, passed (831 executed) |
| Debian GNU/Linux 13 (trixie) Arm64, .NET 10.0.13 | same run | same | same | 1 executed, passed (831 executed) |

Deviation: the hosted runs used the unfiltered CI test command instead of the filtered spike command; their TRX lists the spike test by name with an executed result, which meets the plan's acceptance rule.

Findings, identical on all four runs:

- Output: one schema for the namespace `urn:weatherparser:export:v1`, 14 976 bytes, SHA-256 `30AA57583FBBA6BE82E03BC1A624A2304E3EB9A9A0ADB91E7FAC8BDE59480169`; the schema text from each hosted TRX equals the local Windows output byte for byte.
- Confirmed expectations of the plan: every reference-type member, including required strings, objects and collection containers, is exported with `minOccurs="0"`; members with a `{Name}Specified` companion are optional; suffix-free timestamps are plain `xs:string`; enums are `xs:enumeration` restrictions with their wire values; `decimal` is `xs:decimal`; non-nullable value types are `minOccurs="1"`.
- Not anticipated by the plan: nullable decimals and enums (`altitude_m`, the `forecast_run` point values, `pressure_reference`), every collection item and every global root element are exported with `nillable="true"`, although C-04 forbids `xsi:nil`; the constants `schema_version` and `dataset_type` are optional plain strings without a fixed value; the second string array type is named `ArrayOfString1`; `pressure` values use `xs:extension` of `MeasurementValue`; the exporter expresses no grammar or range rules (for example the C-50 source form, the C-86 `error_id` pattern, coordinate ranges).
- The throwaway test is removed in the S1 diff (plan S0).

### 9.2 Contract approval (S0)

qwen3-coder:30b drafted decisions C-95 to C-106 (section 13 of `P2_EXPORT_CONTRACT.md`) from the S0 findings. Read-only reviews by the OpenAI Codex CLI reported 2, 2 and 0 findings (round 1: validation warnings not enabled, `fixed` constants accepting empty elements; round 2: non-ASCII digits and a trailing line feed accepted by the timestamp pattern, reproduced by the orchestrating agent with a local `XmlSchemaSet` on .NET 10.0.13, and evolution wording tied to one delivery option). The user approved C-95 to C-106 on 2026-10-20 and chose the tool project (C-96), derived requiredness (C-99) and delivery inside the package (C-106).

### 9.3 Slice evidence

| Slice | Red test(s) and observed failure | Green / Refactor | Tests after slice |
|---|---|---|---|
| S1 projects and golden file | `ExportSchemaGeneratorTests` (4) against a stub returning no bytes ("Expected _bytes not to be empty", missing end and comment, no roots) and `GoldenFileTests.GeneratedSchema_MatchesCommittedFile` ("File.Exists(path) ... found False") | Tool library `tools/WeatherParser.SchemaGeneration` and test project with lock files, solution and filter entries; generator with the C-97 writer settings in one method; one regeneration run (`WEATHERPARSER_REGENERATE_SCHEMAS=1`, ended inconclusive); spike test removed | 5 tool tests passed |
| S2 reproducibility | Characterization: `ReproducibilityTests.TwoCleanGenerations_ProduceIdenticalBytesAndSha256` passed immediately | none | 6 |
| S3 refinements | Tool: `SchemaRefinementTests` 22 failures (`nillable` present; required elements `minOccurs="0"`; timestamps `xs:string`; `UtcTimestamp` and `SchemaVersionV1` missing); 5 optional rows passed. Validation: `SchemaRefinementValidationTests` 7 failures ("Expected collection not to be empty" for a missing `source`, a `Z` suffix, `xsi:nil`, a missing, empty or wrong `schema_version` and a wrong `dataset_type`); the real export validated | `ExportSchemaRefiner` behind `IExportSchemaRefiner`, injected into the generator; regeneration and review of the schema difference; refactor of the refiner style | 34 tool, 838 Parsing |
| S4 real exports | Characterization: `RealExportValidationTests` (all five roots, empty collections and optional objects, pressure and period members) passed immediately | none | 845 Parsing |
| S5 negatives and semantics | `SchemaNegativeTests.TimestampSemantics_RejectImpossibleCalendarDate` against a stub ("Expected collection to contain a single item, but the collection is empty"); the other rows (wrong namespace, unknown element, six malformed timestamps including non-ASCII digits and a trailing line feed, bad constants inside the envelope) passed immediately because of the S3 refinements | `TimestampSemantics` checks every element typed `UtcTimestamp` with `DateTime.TryParseExact` | 863 Parsing |
| S6 secure validation | Characterization: on .NET 10.0.13 default reader settings already prohibit DTDs and the schema set resolved no import (checked by the orchestrating agent before writing the tests), so the plan's Red expectation does not hold; `SecureValidationTests` passed immediately. Mutation check: an `XmlUrlResolver` on the schema set made the `file` import row fail | C-104 settings made explicit (defense in depth) | 866 Parsing |
| S7 contract guard | Characterization: `ContractMappingGuardTests` (no `xs:any`, namespace, envelope and error record, collection items) passed immediately | none | 38 tool |
| Review fixes | `RefinerMappingGuardTests`: implicitly mapped property and getter-only `Collection<string>` not rejected, entity in a `Collection<T>` not refined ("no exception was thrown", `minOccurs` "0"); `SecureValidationTests.TimestampSemantics_DocumentWithDtd_IsRejected` ("no exception was thrown") | Refiner rejects every exported element without an explicitly mapped property, unwraps any `IEnumerable<T>`, follows base types and fails for an unrefined complex type; the semantic check reads through the shared secure settings; `ClassificationGuardTests` (exhaustive, mutation-checked); full `dataset_type` constant matrix | 43 tool, 873 Parsing |

### 9.4 Test IDs completed

| T-P2 ID | Tests |
|---|---|
| T-P2-20 | `ReproducibilityTests`, `GoldenFileTests`, `ExportSchemaGeneratorTests` |
| T-P2-21 | `RealExportValidationTests`, `SchemaRefinementValidationTests`, `SchemaNegativeTests` |
| T-P2-22 | `SecureValidationTests` |
| T-P2-23 | `ContractMappingGuardTests`, `SchemaRefinementTests`, `ClassificationGuardTests`, `RefinerMappingGuardTests` |

### 9.5 Implementation review by Codex

Six rounds reported 2, 1, 1, 1, 1 and 0 findings, each confirmed by the orchestrating agent and fixed test-first through qwen: (1) implicitly mapped properties were skipped silently and no exhaustive classification guard existed, and the documented regeneration command used `--no-build`; (2) the semantic timestamp check parsed DTDs through `XDocument.Load`; (3) getter-only collections other than `List<T>` bypassed the implicit-mapping check; (4) entities inside non-`List` collections were not refined and the guard shared the gap; (5) the `dataset_type` constant matrix was incomplete. Round 6 reported no findings.

### 9.6 Checks run on the working tree (2026-10-20)

- `dotnet restore WeatherParser.Ci.slnf --locked-mode`: succeeded. `dotnet build WeatherParser.Ci.slnf -c Release`: 0 warnings, 0 errors.
- Tests: `WeatherParser.Parsing.Tests` 873 passed, `WeatherParser.SchemaGeneration.Tests` 43 passed, `WeatherParser.Coverage.Tests` 99 passed, `WeatherParser.Coverage.Runner.Tests` 56 passed.
- Committed schema `schemas/export/v1/weatherparser-export-v1.xsd`: 15 469 bytes, SHA-256 `4817672EE025B1F62FF71A0DC91FC92DFD90C8FDE230F44DCABCD6FC91D0A42B`; two clean generations into empty directories are byte-identical (`ReproducibilityTests`); `.gitattributes` keeps LF.
- Local native block coverage of `WeatherParser.Parsing.dll`: 3031/3033 (99.93 %), unchanged; P2.09 changes no library source. The tool is outside the product coverage gate and is covered by its own behavior tests and three mutation checks.
- `scripts/ci-local.ps1` runs the new test project in the current pass; this is verified only by the hosted CI after the commit (the script tests committed states). Committed as `77c92a9`; the committed schema blob has LF line endings and the same SHA-256. Hosted CI run 39482435638 of draft pull request #3 passed on `windows-x64`, `ubuntu-x64` and `debian13-arm64` (Debian 13 ARM64 container on a GitHub-hosted runner, not NanoPi R76S target evidence); each job's `current/trx/WeatherParser.SchemaGeneration.Tests.trx` lists 43 executed and passed tests, including `GeneratedSchema_MatchesCommittedFile`, so the golden file matches a fresh generation on all three platforms.

### 9.7 Deviations and design notes

1. C-96: the generator lives in a separate tool project (user choice); validation tests of real exports stay in `WeatherParser.Parsing.Tests` and read the committed schema, which the test project copies to its output.
2. C-99 derives requiredness from the C-05 omission attribute; no `[ExportRequired]` marker.
3. Negative documents are produced by mutating real exports instead of static fixture files, so they follow the mappings.
4. S6 is characterization on .NET 10 (see 9.3); the C-104 settings are explicit in the shared test helper.
5. The `ci-local.ps1` change was inserted by the orchestrating agent: the 1379-line script exceeds one qwen output.
6. Builders and root validation remain separate implementations (P2.08 risk); P2.09 adds no library code.

### 9.8 Authorship

qwen3-coder:30b (through the `/api/chat` invocation of AGENTS.md) drafted the contract rows and generated the spike test, both project files, the generator, the refiner, all tests, both test helpers and the documentation passages from the orchestrating agent's briefs. Where qwen deviated (invented members such as `ForecastRun.Steps`, `ObservationDataset.RetrievedAt` and `ContainExactly`, dropped `DataRow` and `DynamicData` attributes, dropped backticks, Status cells and a contract phrase, a false review date in DEPENDENCIES.md, missing summaries), defects were returned with compiler or test output; the orchestrating agent applied mechanical corrections, wired the solution, filter and lock files, wrote `.gitattributes`, inserted the `ci-local.ps1` lines, and fixed the test attributes of `SchemaNegativeTests` itself after three failed briefs (AGENTS.md exception). The OpenAI Codex CLI reviewed the contract draft and the implementation read-only. The orchestrating agent confirmed every failure reason and finding, ran all builds, tests, coverage, mutation checks and reviews, and wrote this record.

### 9.9 Hosted CI and completion (2026-10-20)

- Commit `77c92a9` pushed to `features/develop/P2`; hosted CI run 39482435638 of draft pull request #3 (against its PR base) passed: `windows-x64` success, `ubuntu-x64` success, `debian13-arm64` success (Debian 13 ARM64 container on a GitHub-hosted runner, not NanoPi R76S target evidence); `WeatherParser.SchemaGeneration.Tests` ran in each job with 43 executed and passed tests, including the golden-file comparison (section 9.6).
- The user marked P2.09 completed on 2026-10-20; all acceptance criteria of the [P2.09 plan](P2_09_OUTPUT_SCHEMAS_PLAN.md) are checked. The schema-delivery decision (C-106, inside the package) is implemented in P2.10. This is step completion, not P2 acceptance (P2.11/P2.12).
- Recorded by the orchestrating agent (run results and user decision).

## 10. P2.10 – DI and actual package consumption

### 10.1 Contract approval (S0)

qwen3-coder:30b drafted decisions C-107 to C-116 (section 14 of `P2_EXPORT_CONTRACT.md`) after the orchestrating agent scanned the library's static fields (the `decimal`, `Regex`, `TimeSpan` and `string` fields, and one mutable `static readonly HashSet<Type>` in `SupportedExportRoots`) and checked the consumer project. Read-only reviews by the OpenAI Codex CLI reported 6, 2 and 0 findings; the orchestrating agent confirmed every finding (including that the README still stated "P2 has not started"), qwen revised the rows, and the orchestrating agent set two cells qwen did not change to the briefed text. The user approved C-107 to C-116 on 2026-10-20 and chose to allowlist `Regex` (C-109).

### 10.2 Slice evidence

| Slice | Red test(s) and observed failure | Green / Refactor | Tests after slice |
|---|---|---|---|
| S1 registration and validation | Characterization: `ServiceRegistrationCompletenessTests` (every visible service interface registered as singleton of its only implementation, only closed type-based descriptors, `IExportRootValidation` the only non-visible interface, missing dependency fails on build) passed immediately; mutation check: removing the `IForecastRunBuilder` registration made the completeness test fail | none | 877 Parsing |
| S2 no shared state | `NoSharedStateTests.LibraryTypes_DeclareNoStaticState`: "found ... SupportedExportRoots.Roots : HashSet`1"; container independence and cache isolation passed immediately (characterization) | `SupportedExportRoots` decides support with a static method instead of a static collection | 880 Parsing |
| S3 consumer resolves and serializes | Consumer build against the stale restored package: CS0234 for `WeatherParser.Parsing.Export`, `Forecasts`, `Observations`, `Quantities`, `Serialization`, `Time` and CS0246 `ServiceProvider` (no DI container reference) | `Microsoft.Extensions.DependencyInjection` added to the consumer; same-run pack, stale cache entry removed, lock refreshed, locked restore; `ConsumerTestData`, `PackageServiceResolutionTests`, `PackageSerializationTests` | 10 consumer |
| S4 schema delivery | `PackageSchemaDeliveryTests`: 7 failures "Expected File.Exists(PackagedSchema) to be True ... found False"; `$(PkgWeatherParser_Parsing)` checked in `nuget.g.props` | Schema packed at `schemas/export/v1/` (plain package file); `ci-local.ps1` allowlist and CONTRIBUTING.md section 4 gain exactly this entry | 17 consumer |
| S5 documentation | README status, solution paragraph and export section; `DEVELOPMENT_PLAN.md` and `PHASE_P1_PLAN.md` status; DEPENDENCIES.md (runtime sentence, two missing DI rows with MIT licenses checked in the nuspecs, direct-package count); CONTRIBUTING.md section 4; package check rerun after the README change | — | — |
| Review fixes | — | Stronger consumer cross-format checks; no `Exists` condition on the packaged schema item (a missing file now fails the build) | 17 consumer |

### 10.3 Package evidence (local, working tree)

- Package `WeatherParser.Parsing.0.1.0-initial.nupkg` entries: `_rels/.rels`, `WeatherParser.Parsing.nuspec`, `LICENSE`, `README.md`, `lib/net10.0/WeatherParser.Parsing.dll`, `schemas/export/v1/weatherparser-export-v1.xsd`, `[Content_Types].xml`, `package/services/metadata/core-properties/nuget.psmdcp`; `README.md`, `LICENSE` and the schema are byte-identical to the working tree.
- Packed SHA-512 of the final local pack `fduKbeo4EGi5RndgO062aL1R4Dt7rL+2KMqGATNVXyLhPnnhxRuHEvEr6ihnEhWIm58Qq1XEmDbTLYJmYmQG5A==` equals the restored `.nupkg.sha512` after the locked restore.
- Consumer lock: the committed change adds only the `Microsoft.Extensions.DependencyInjection` entry (9 lines); the `WeatherParser.Parsing` `contentHash` keeps its previously committed value (C-111, C-116); the same-run refresh was reset after each local run.
- S6 (`ci-local.ps1` on a committed checkpoint) is recorded after the commit.

### 10.4 Implementation review by Codex

Round 1 reported three findings, all confirmed: the `Exists` condition could leave a stale schema from an earlier build in the consumer output; the consumer serialization test accepted a JSON document that kept only `schema_version`; the DEPENDENCIES.md direct-package count was wrong. All were fixed through qwen; round 2 reported no findings.

### 10.5 Checks run on the working tree (2026-10-20)

- `dotnet restore WeatherParser.Ci.slnf --locked-mode`: succeeded. `dotnet build WeatherParser.Ci.slnf -c Release`: 0 warnings, 0 errors.
- Tests: `WeatherParser.Parsing.Tests` 880, `WeatherParser.SchemaGeneration.Tests` 43, `WeatherParser.Coverage.Tests` 99, `WeatherParser.Coverage.Runner.Tests` 56 and the package consumer `WeatherParser.Package.Tests` 17 passed.
- Local native block coverage of `WeatherParser.Parsing.dll`: 3035/3037 (99.93 %); the two uncovered blocks are the pre-existing P1 constructor `ParserEndpointValidator(IEndpointUriValidator)`.
- Committed as `f7b9b9e` and `186f900` (C-109 amendment); hosted CI and S6 results in section 10.10.

### 10.6 Test IDs

| T-P2 ID | Tests |
|---|---|
| T-P2-19 | `ServiceRegistrationCompletenessTests`, `NoSharedStateTests`, existing `ServiceCollectionExtensionsTests` |
| T-P2-24 | `PackageConsumptionTests`, `PackageServiceResolutionTests`, `PackageSerializationTests`, `PackageSchemaDeliveryTests` |
| T-P2-25 | `PackageSchemaDeliveryTests`, the package check of section 10.3 and the S6 run after the commit |

### 10.7 Deviations and design notes

1. The registration tests live in the new `Architecture/ServiceRegistrationCompletenessTests.cs` instead of extending `ServiceCollectionExtensionsTests.cs`; the service list is derived by reflection (C-107).
2. S1 and the container and cache parts of S2 are characterization; the static-state check found the real `HashSet<Type>` defect.
3. The `ci-local.ps1` allowlist line was inserted by the orchestrating agent (the 1379-line script exceeds one qwen output).
4. DEPENDENCIES.md had omitted both DI packages since P1; P2.10 lists them.

### 10.8 Authorship

qwen3-coder:30b drafted the contract rows and generated all tests, the consumer fixture, the `SupportedExportRoots` change, the project-file changes and every documentation passage from the orchestrating agent's briefs; each documentation passage was checked for exact equality with the brief before it was applied. Where qwen deviated (invented `BeGreaterOrEqualTo`, a `StreamReader` misuse, invalid switch expressions, missing XML namespaces, two contract cells left unchanged, missing Status cells), defects were returned with compiler or test output, and the orchestrating agent applied mechanical corrections. The orchestrating agent ran the pack, cache, lock and consumer procedure, reset the consumer lock hash, inserted the `ci-local.ps1` entry, ran all builds, tests, coverage, mutation checks and reviews, and wrote this record. The OpenAI Codex CLI reviewed the contract draft and the implementation read-only.

### 10.9 S6 run 1 and the C-109 amendment (2026-10-20)

- `pwsh ./scripts/ci-local.ps1 -Commit f7b9b9e -TargetRef 64e460b -RunSettings coverage.runsettings` (run directory `artifacts/ci/f7b9b9e-20261020T195020Z`) failed with exit 50 in the test stage: `LibraryTypes_DeclareNoStaticState` reported `Microsoft.CodeCoverage.Instrumentation.Static.Tracker.StaticManagedTrackerTemplate_<guid>` types with static `Byte*` fields. Static coverage instrumentation (`coverage.runsettings`) injects them into the assembly under test; the local P2.10 runs had not collected coverage. The orchestrating agent reproduced the failure with `--collect "Code Coverage" --settings coverage.runsettings`.
- C-109 was amended: types whose namespace equals or starts with `Microsoft.CodeCoverage.Instrumentation.` (ordinal) are exempt, and the package consumer proves that the packed assembly contains no such type. Codex reviewed the amendment read-only with 1 and 0 findings (round 1: the amendment had been spliced into the reflection expression because the approved cell contained unescaped pipes, which are now escaped); the user approved it on 2026-10-20.
- qwen added the exemption to `NoSharedStateTests` (passes with and without coverage collection) and `Package_assembly_contains_no_coverage_instrumentation_types` to the consumer (18 consumer tests).

### 10.10 S6 package identity and hash evidence (2026-10-20)

- `pwsh ./scripts/ci-local.ps1 -Commit 186f900 -TargetRef 64e460b -RunSettings coverage.runsettings` (script revision of commit `186f900`; run directory `artifacts/ci/186f900-20261020T195555Z`) passed with exit 0 in 39 s.
- Tests: `WeatherParser.Parsing.Tests` 880, `WeatherParser.Coverage.Tests` 99, `WeatherParser.Coverage.Runner.Tests` 56, `WeatherParser.SchemaGeneration.Tests` 43 and the consumer `WeatherParser.Package.Tests` 18 passed; the target pass on `64e460b` ran its 15 product tests.
- Package entries (allowlist check passed): `_rels/.rels`, `WeatherParser.Parsing.nuspec`, `LICENSE`, `README.md`, `lib/net10.0/WeatherParser.Parsing.dll`, `schemas/export/v1/weatherparser-export-v1.xsd`, `[Content_Types].xml`, `package/services/metadata/core-properties/nuget.psmdcp`.
- Packed SHA-512 (`package-hash.txt`) `8jkDhZ3nqd4aQLHqpjSUK7sE6nh0whsIcgglmuRyJ2HC9SUL8rVmv4mgGrPdai8O4N7GOmGQF4rxOtNLIwsfRA==`; the script compared it with the restored `.nupkg.sha512` of the isolated package folder (it fails with exit 70 on a mismatch).
- Consumer lock: the run rewrote exactly one line, the `WeatherParser.Parsing` `contentHash` (logged "Consumer lock file contentHash rewritten (one line)"); every other locked dependency, including the committed `Microsoft.Extensions.DependencyInjection` entry, stayed unchanged (the script fails with exit 70 otherwise).
- Coverage gate: passed; block coverage 3035/3037 (99.93 %) exceeds the PR-base baseline 54/56 (96.43 %), comparable collection settings.
- Hosted CI run 39487633043 of draft pull request #3 for `186f900` passed on `windows-x64`, `ubuntu-x64` and `debian13-arm64` (Debian 13 ARM64 container on a GitHub-hosted runner, not NanoPi R76S target evidence); each job's TRX lists 880 executed and passed `WeatherParser.Parsing.Tests` (including `LibraryTypes_DeclareNoStaticState` under coverage instrumentation) and 18 passed consumer tests. The earlier run 39486827200 for `f7b9b9e` had failed on that test before the C-109 amendment (section 10.9).

### 10.11 Completion (2026-10-20)

- The user marked P2.10 completed on 2026-10-20 after hosted CI run 39487633043 and the S6 run passed (section 10.10); all acceptance criteria of the [P2.10 plan](P2_10_DI_PACKAGE_CONSUMPTION_PLAN.md) are checked. This is step completion, not P2 acceptance (P2.11/P2.12).
- Recorded by the orchestrating agent (run results and user decision).

## 11. P2.11 – Integrated coverage and platform verification

### 11.1 Decisions (2026-10-20)

- The user authorized access to the NanoPi R76S through the SSH alias `R76S` (bundle transfer, `scripts/prepare-debian-arm64.sh` user-directory install, `ci-local.ps1`, P2-specific checks, copying the run directory back) and chose the network-blocked run for the offline proof.
- Recorded by the orchestrating agent: both locales if installed (only `C.utf8` is installed on the target; no system packages were installed); a defect is fixed in a new candidate and V1 to V6 are repeated, earlier runs stay recorded.

### 11.2 V1 – Identities

| Item | Candidate 1 | Candidate 2 (final) |
|---|---|---|
| Candidate commit | `40628b1fe0af74495702cdc159ec9e5835d779cd` | `bced7be380c3e637fdd042c4165cd074e25f25a6` |
| PR / target | draft pull request #3 into `develop` | same |
| PR base | `64e460b475b4a29b9e3ddd331394076e53982d9e` | same |
| Diff against the PR base | 205 files, 178 code, script or configuration files (not documentation-only) | same plus the CONTRIBUTING.md section 7 fix |
| Script revision | `scripts/ci-local.ps1` of `f7b9b9e` | same |
| SDK / runtime | 10.0.402 / `Microsoft.NETCore.App` 10.0.13 (Windows and target) | same |
| Run settings | `coverage.runsettings`, SHA-256 `DC6F662E4F6FAAA853C33ED5497F182A31527FD126961411A604B683FFCF05D2` | same |

### 11.3 V2 – Local two-pass run on Windows

| Candidate | Run directory | Exit | Tests (Parsing, Coverage, Runner, SchemaGeneration, consumer, target pass) | Warnings | Package SHA-512 | Gate |
|---|---|---|---|---|---|---|
| 1 | `artifacts/ci/40628b1-20261020T200519Z` | 0 (39 s) | 880, 99, 56, 43, 18, 15 | 0 | `YChNjlG87adzhWowqSmtcvom8K7PZ61kWCpBbNb+31ohyTipePx5HVMUldAZfAEdQZI7oh8B93pJf15bxHdC0Q==` | passed |
| 2 | `artifacts/ci/bced7be-20261020T203551Z` | 0 (46 s) | 880, 99, 56, 43, 18, 15 | 0 | `nTYtQEeriukG4BQnYfybiDaTpqHEiRQtuZqGCZY26g+BUEJT3Lwc7tMT++x1PqpsrK2vCYHJFRCI6SYjVUlMdg==` | passed |

Gate of candidate 2 (`gate-result.json`, SHA-256 `774C212BC9ACAEDEA2F30FB5687DA137D968895ADA94A13DB17BAFED19916678`): Comparability, Measurement (3035/3037 valid), Minimum (99.93 % at or above 80 %), Baseline (54/56 valid) and Comparison (3035/3037 exceeds the remeasured PR-base baseline 54/56, 96.43 %) all true; two-pass mode, no bootstrap, not documentation-only. Package entries: `_rels/.rels`, `WeatherParser.Parsing.nuspec`, `LICENSE`, `README.md`, `lib/net10.0/WeatherParser.Parsing.dll`, `schemas/export/v1/weatherparser-export-v1.xsd`, `[Content_Types].xml`, `package/services/metadata/core-properties/nuget.psmdcp`; the consumer-lock rewrite was one line.

### 11.4 V3 – Hosted workflow

- Candidate 1: run 39488784495 succeeded on `windows-x64`, `ubuntu-x64` and `debian13-arm64`.
- Candidate 2: run 39493310294 succeeded on `windows-x64`, `ubuntu-x64` and `debian13-arm64`.
- The `debian13-arm64` job is a Debian 13 container on a GitHub-hosted ARM64 runner and is recorded separately; it is not target evidence.

### 11.5 V4 – NanoPi R76S target run and P2-specific checks

Host: FriendlyElec NanoPi R76S (`NanoPi-R76S`), Debian GNU/Linux 13 (trixie), `aarch64`, RID `linux-arm64`; SDK 10.0.402 and `Microsoft.NETCore.App` 10.0.13 installed by `scripts/prepare-debian-arm64.sh` into user directories, PowerShell 7.6.7, ICU 76 (`libicuuc.so.76`), tzdata `2025b-4+deb13u1`, locales: only `C.utf8`.

| Run | Procedure | Exit | Tests | Package SHA-512 | Gate |
|---|---|---|---|---|---|
| Candidate 1, run 1 | CONTRIBUTING.md section 7 as then written (`-Commit origin/features/develop/P2 -TargetRef origin/develop`) | 20 after the current pass | current pass 880, 99, 56, 43 and consumer 18 passed; target pass failed: "git checkout --detach --quiet 64e460b... failed with exit code 128" | `lhegkzh3Z7BvUD3dn6s6WcdKUrv4pISS3ptNzXsFBFZ2D1cH7s1GULrx7aUgyIlX3dsuUZX6bXt1cvbxfx1bnQ==` | not reached (defect D1) |
| Candidate 1, run 2 | local branches `ci-commit` and `ci-target`, as the hosted workflow does | 0 (2:53) | 880, 99, 56, 43, 18, 15 | `dyfJy4dOfryYNHBdArxUGR2MgprAgKe/iOqQmYK9RLaRwn/04DEyWDAfU3c+IRAReXSIxUbipJ1DnqeqZDe0Ag==` | passed, 3035/3037 vs 54/56 |
| Candidate 2 | corrected section 7 executed verbatim in a fresh directory (bundle SHA-256 `734322D8B8E013A7BFE984A4801DAF2C405BBC1C0886E9F25BB8D13AA582A297`) | 0 (3:19) | 880, 99, 56, 43, 18, 15 | `B96skjTa2IfCHhtVeOHDYUhHID8y6wTelfDOo6zIbjcd+WYAJ4wBpBfArnb4HcgRs2PgEw2GX3nP+XVZTXhNaA==` | passed, 3035/3037 vs 54/56; `gate-result.json` SHA-256 `FA9CF8D54512F92F490E7D6CE0A371895BB527F72F70B5CF44B5CCA8F4314599`; run directory copied to `artifacts/ci/arm64-bced7be-20261020T203606Z` (37 files) |

P2-specific results on the target (candidate 2): both serializers for all five roots (`AllRootsEquivalenceTests`, `AllRootsDeterminismTests`, `SerializerStreamBehaviourTests`, `LineBreakPreservationTests`) passed; `Europe/Berlin` resolution, DST gap and overlap handling and the missing-time-zone-data failure (`TimeZoneResolverTests`, `SourceTimeNormalizerTests`) passed with tzdata `2025b-4+deb13u1`; culture checks under `en-US` and `de-DE` (`AllRootsCultureTests`, `CultureInvarianceContractTests`) passed with the ICU culture data while the system locale is `C.utf8` (the `de_DE.UTF-8` system locale is not installed and was not installed); the schema regenerated on the target equals the committed file (`GeneratedSchema_MatchesCommittedFile` passed; SHA-256 `4817672EE025B1F62FF71A0DC91FC92DFD90C8FDE230F44DCABCD6FC91D0A42B`, identical to Windows); the package consumer ran 18 tests against the same-run package, including schema delivery and validation.

### 11.6 V5 – Offline behavior

The standard test projects ran on the target in a network namespace (`unshare -rn`, no root) with only the loopback interface up; inside it a TCP probe to `1.1.1.1:443` failed and `nuget.org` did not resolve. Candidate 1 and candidate 2 each passed `WeatherParser.Parsing.Tests` 880, `WeatherParser.SchemaGeneration.Tests` 43, `WeatherParser.Coverage.Tests` 99, `WeatherParser.Coverage.Runner.Tests` 56 and `WeatherParser.Package.Tests` 18. A first attempt without loopback aborted because `vstest.console` connects to the test host over loopback; this is a test-runner requirement, not a network dependence. A source search found no `HttpClient`, `WebRequest`, `TcpClient`, socket or MQTT use in `src`, `tools` or `tests`; the only matches are string literals.

### 11.7 V6 – TDD and evidence audit

All 14 test classes that use mocks create them in `[TestInitialize]` and call `VerifyAll()` in `[TestCleanup]` with `MockBehavior.Strict` (12 through a `MockRepository`, `ParserEndpointValidatorTests` and `SourceTimeNormalizerTests` with strict mocks directly). Every test class named in sections 2 to 10 exists, except the deliberately removed P2.09 S0 spike. qwen/orchestrating-agent authorship is recorded per step in sections 2 to 10. Candidate 2 changes only CONTRIBUTING.md, so the audit applies to both candidates.

| T-P2 ID | Steps and slices | Tests | Red or characterization evidence |
|---|---|---|---|
| T-P2-01 | P2.02 S5, P2.03 S4, P2.06 S8, P2.07 S8, P2.08 S8 | `ExportEntitySerializableTests` (traversal from all five roots) | Red in sections 2 and 8.2 (S8: "found ... ExportDocument, ExportError"); sections 3, 6, 7 characterization |
| T-P2-02 | P2.03 S1, S3, S6 | `CdcStationListContractTests`, `ListEntryOrderingTests`, `ListRequiredMemberGuardTests`, `ListReadBackTests` | Red in section 3 |
| T-P2-03 | P2.03 S2, S3, S6 | `MosmixLocationListContractTests`, `ListEntryOrderingTests`, `EnumVocabularyGuardTests`, `ListRequiredMemberGuardTests` | Red in section 3 |
| T-P2-04 | P2.05 S3, P2.06 S1, S3, S4, P2.08 S10 | `QuantityValidatorTests`, `ObservationDatasetBuilderTests`, `ObservationBuilderRulesTests`, `ObservationDatasetContractTests`, `ObservationGraphValidationTests` | Red in sections 5 and 6; section 8.2 characterization for the existing graph rules |
| T-P2-05 | P2.02 S2, S4, P2.04 S7, P2.08 S5 | `UtcTimestampContractTests`, `CultureInvarianceContractTests`, `NormalizedTimeSerializationTests`, `AllRootsCultureTests` | Red in sections 2 and 4; section 8.2 characterization (all roots) |
| T-P2-06 | P2.04 S1, S2, S4, S5, P2.06 S6 | `SourceTimeNormalizerTests`, `ObservationBuilderRulesTests` | Red in sections 4 and 6 |
| T-P2-07 | P2.04 S3, S4, S6 | `SourceTimeNormalizerTests`, `TimeZoneResolverTests` | Red in section 4; target tzdata run in V4 |
| T-P2-08 | P2.06 S2, S6 | `ObservationDatasetBuilderTests`, `ObservationBuilderRulesTests`, `ObservationGraphValidationTests` | Red in section 6 |
| T-P2-09 | P2.05 S2, P2.06 S5 | `PressureConverterTests`, `ObservationPressurePairValidationTests` | Red in sections 5 and 6 |
| T-P2-10 | P2.05 S1, S6, P2.06 S5 | `OutputRoundingTests`, `WindSpeedConverterTests` | Red in sections 5 and 6 |
| T-P2-11 | P2.05 S4 | `PeriodRateConverterTests`, `AveragingPeriodTests` | Red in section 5 |
| T-P2-12 | P2.05 S5, P2.06 S7, P2.07 S7 | `QuantityKindSeparationTests`, `ObservationBuilderRulesTests`, `ForecastRunBuilderRulesTests` | Red in sections 5, 6 and 7 |
| T-P2-13 | P2.07 S1, S2, S3, S6 | `ForecastRunContractTests`, `ForecastRunBuilderTests`, `ForecastGraphValidationTests` | Red in section 7 (S2 characterization) |
| T-P2-14 | P2.07 S4, S5, S6, P2.08 S10 | `ForecastRunBuilderTests`, `ForecastRunBuilderRulesTests`, `ForecastGraphValidationTests` | Red in section 7; section 8.2 characterization |
| T-P2-15 | P2.08 S1, S2, S9 | `ExportDocumentContractTests`, `ExportErrorRuleTests`, `AllRootsDeterminismTests` | Red in section 8.2 (S9 characterization) |
| T-P2-16 | P2.02 S2, P2.06 S8, P2.07 S8, P2.08 S4, S7 | `ObservationExportEquivalenceTests`, `ForecastRunExportEquivalenceTests`, `AllRootsEquivalenceTests`, `AllRootsDeterminismTests`, `ExportDocumentReadBackTests` | Red in section 2; sections 6, 7 and 8.2 characterization |
| T-P2-17 | P2.02 S1, S3, P2.03 S6, P2.08 S4 and C-92 | `EscapingContractTests`, `ObservationDatasetJsonContractTests`, `AllRootsEquivalenceTests`, `LineBreakPreservationTests` | Red in sections 2, 3 and 8.2 (C-92 regression) |
| T-P2-18 | P2.02 S6, S7, P2.08 S3, S6, S10 | `ExportSerializerStreamContractTests`, `JsonFailureWrappingTests`, `SupportedRootTests`, `SerializerStreamBehaviourTests`, `ExportGraphValidationTests`, `ListGraphValidationTests` | Red in sections 2 and 8.2 (S6 characterization, mutation-checked) |
| T-P2-19 | P2.02 S7, P2.03 S5, P2.04 S8, P2.05 S7, P2.06 S9, P2.07 S9, P2.10 S1, S2 | `ServiceCollectionExtensionsTests`, `ServiceRegistrationCompletenessTests`, `NoSharedStateTests` | Red in sections 2 to 7 (registration) and 10.2 (static state); S1 characterization, mutation-checked |
| T-P2-20 | P2.09 S1, S2 | `GoldenFileTests`, `ReproducibilityTests`, `ExportSchemaGeneratorTests` | Red in section 9.3 (S2 characterization); golden file matched on three hosted platforms |
| T-P2-21 | P2.09 S3, S4, S5 | `SchemaRefinementTests`, `SchemaRefinementValidationTests`, `RealExportValidationTests`, `SchemaNegativeTests` | Red in section 9.3 (S4 characterization) |
| T-P2-22 | P2.09 S6 | `SecureValidationTests` | Characterization on .NET 10 (section 9.3), mutation-checked; Red for the semantic DTD path in section 9.5 |
| T-P2-23 | P2.09 S7 | `ContractMappingGuardTests`, `ClassificationGuardTests`, `RefinerMappingGuardTests` | Characterization (S7) and Red for the review fixes in section 9.3, mutation-checked |
| T-P2-24 | P2.10 S3, S4 | `PackageConsumptionTests`, `PackageServiceResolutionTests`, `PackageSerializationTests`, `PackageSchemaDeliveryTests` | Red in section 10.2 |
| T-P2-25 | P2.10 S4, S5, S6 | `PackageSchemaDeliveryTests` and the ci-local.ps1 package check | Red in section 10.2; S6 run in section 10.10 |
| T-P2-26 | P2.11 V5, V6 | all standard test projects | this audit, the mock rule check and the network-blocked target run |
| T-P2-27 | P2.11 V1, V2 | ci-local.ps1 coverage gate | V2 gate result |
| T-P2-28 | P2.11 V4 | target run and P2-specific checks | V4 results |

### 11.8 V7 – Defects

| ID | Defect | Handling | Status |
|---|---|---|---|
| D1 | CONTRIBUTING.md section 7 did not check out the candidate and did not create local branches, so `ci-local.ps1` (which clones with `--no-local`) could not reach a PR base that is not an ancestor of the candidate; the target pass failed with exit 20 | qwen drafted the corrected procedure (checkout, `ci-commit` and `ci-target` branches, explanation); commit `bced7be`; V1 to V6 repeated for candidate 2, whose target run executed the corrected procedure verbatim and passed | Fixed |

### 11.9 V8 – Outcome

**READY** for P2.12. Candidate `bced7be380c3e637fdd042c4165cd074e25f25a6` passed the local two-pass gate on Windows and on the NanoPi R76S, the hosted workflow on all three jobs, the P2-specific target checks and the network-blocked offline run; every T-P2 row has evidence and no defect is open. Verified platform results are also recorded in [COMPATIBILITY.md](../COMPATIBILITY.md). Hardware release acceptance (REQ-084) remains P9.

### 11.10 Authorship

qwen3-coder:30b drafted the D1 procedure text (verified exact) and formatted the audit table from the mapping extracted by the orchestrating agent (backticks restored by the orchestrating agent). The orchestrating agent identified the candidates, ran every Windows, target and offline command, checked the hosted runs, extracted and verified the audit mapping, and wrote this section and the COMPATIBILITY.md update from its records because they record run results.

### 11.11 Acceptance record

The P2.12 acceptance record with the evidence matrix, contract decisions, limitations and decisions is [P2_12_ACCEPTANCE.md](P2_12_ACCEPTANCE.md); the P4 handoff is [P4_HANDOFF.md](P4_HANDOFF.md).
