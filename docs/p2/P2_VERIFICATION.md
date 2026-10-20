# P2 Verification Record

**Status (2026-10-20): P2.01 complete (READY); P2.02 committed (`b1ba0d2`) with a passing two-pass candidate run (section 2.7); P2.03 completed (commit `0f59e1c`, marked completed by the user; section 3); hosted CI of the draft pull request #3 checks every push.**

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
