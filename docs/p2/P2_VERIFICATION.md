# P2 Verification Record

**Status (2026-10-20): P2.01 complete (READY); P2.02 completed (commit `b1ba0d2`, candidate run section 2.7, marked completed by the user); P2.03 completed (commit `0f59e1c`, marked completed by the user; section 3); P2.04 completed (commit `15928cf`, marked completed by the user; section 4); P2.05 completed (commit `02393bb`, marked completed by the user; section 5); P2.06 completed (commit `90106eb`, marked completed by the user; section 6); hosted CI of the draft pull request #3 checks every push.**

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
