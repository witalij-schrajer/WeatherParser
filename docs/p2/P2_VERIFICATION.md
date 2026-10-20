# P2 Verification Record

**Status (2026-10-20): P2.01 complete (READY); P2.02 slices S0–S7 executed on the working tree of features/develop/P2; not yet committed; no candidate run for P2.02 yet.**

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
- Not yet executed: the two-pass `scripts/ci-local.ps1` run for a committed P2.02 candidate, the package consumer for P2.02 and any ARM64 run.

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
