# P2.02 – Contract Decisions and First Serializer Slice: Implementation Plan

**Status: completed (2026-10-20, marked completed by the user).** Implemented test-first in commit `b1ba0d2` (record `6d4b6a2`); local two-pass candidate run passed (179/181 against 54/56); hosted CI run 39383342526 of draft pull request #3 passed on Windows x64, Ubuntu x64 and Debian 13 ARM64; evidence in [P2_VERIFICATION.md](P2_VERIFICATION.md) section 2. Original planning note: this document started as a plan on 2026-10-20. This plan refines [PHASE_P2_PLAN.md](../../PHASE_P2_PLAN.md) section 3 and the P2.02 row of section 5. Drafted by the local qwen3-coder:30b model over three review rounds; the orchestrating agent wrote the briefs and reviewed each draft, and itself applied the specified corrections that qwen did not apply (section 3 envelope/version/evolution/empty rows, S0–S4 wording, section 6 mapping, section 7 evidence list).

## 1. Goal and Scope
Goal: Record contract decisions required for P2.02 before the first Red test, then implement a minimal typed export graph with injectable JSON and XML serializer services using real serializers. The implementation must be test-first and exercise all relevant themes from the step brief.

In scope:
- Record the required contract decisions in `docs/p2/P2_EXPORT_CONTRACT.md`
- Implement one minimal export graph (`ObservationDataset` and `ObservationMeasurements`) with `schema_version`, `source`, `retrieved_at`, and `measurements` containing only `temperature_c`
- Implement injectable JSON and XML serializer services
- Test-first approach using real serializers
- Use the recommended first slice graph, omission pattern, time representation, numeric type, naming approach, service shape, encoding/BOM, and invalid character handling

Out of scope:
- Full entity graphs (P2.03–P2.07)
- XSD generation (P2.09)
- Package-consumer DI evidence and missing-dependency tests → P2.10
- Time normalization service → P2.04
- Rounding and unit conversion → P2.05
- Complete observation graph → P2.06
- Envelope implementation and remaining roots → P2.08

## 2. Preconditions and Inputs
- P2.01 has completed with READY outcome and recorded user authorization
- Work happens on branch `features/develop/P2` after P2.01 recorded READY; the coverage integration target is the PR base (currently `develop` at `64e460b`) as remeasured by P2.01, measured at 54/56.
- Existing test project `tests/WeatherParser.Parsing.Tests` and library project `src/WeatherParser.Parsing/` are available
- The existing `ServiceCollectionExtensions.AddWeatherParsing(IServiceCollection)` method is the target for DI registration

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Envelope and roots | Record the full root list (envelope, CDC list, MOSMIX list, observation dataset, forecast run) and the envelope shape now / defer the envelope shape to P2.08 | Record now; implement only the observation-dataset root in P2.02 | User | P2.02, P2.08 |
| Version and XML namespace | `schema_version: "1"` and `urn:weatherparser:export:v1` / other literals | `schema_version: "1"` and `urn:weatherparser:export:v1` | User | P2.02, P2.09 |
| Evolution rules | Additive optional fields compatible within v1, breaking changes need a new `schema_version` and namespace / every change is breaking | First option, with XML compatibility checked per change (phase plan section 3) | User | P2.02, P2.09 |
| Missing versus empty | Omit missing numbers and write empty collections / omit empty collections too | Missing numbers omitted; zero written; empty collections written as empty arrays/elements, never omitted | User | P2.02, P2.03, P2.08 |
| Omission pattern | `{Name}Specified` boolean companion property or `ShouldSerialize{Name}()` method | `{Name}Specified` boolean companion property | Orchestrator, recorded | P2.02 |
| Time representation | Wire-format string property with internal UTC value or JSON converter plus XML surrogate | Wire-format string property with internal UTC value | User | P2.02, P2.04 |
| Sub-second values | Reject vs truncate | Reject | User | P2.02, P2.04 |
| Numeric type | `decimal` vs `double` | `decimal` | User | P2.02, P2.05 |
| Naming approach | Explicit attributes vs naming policy | Explicit attributes | Orchestrator, recorded | P2.02 |
| Serializer service shape | One interface per format vs factory pattern | One interface per format with generic `SerializeAsync<TRoot>(TRoot root, Stream destination, CancellationToken cancellationToken)` and central cache of `XmlSerializer` instances | Orchestrator, recorded | P2.02, P2.08 |
| Encoding/BOM | UTF-8 with or without BOM | UTF-8 without BOM | User | P2.02 |
| Invalid XML 1.0 characters | Defined serialization failure vs other | Defined serialization failure | User | P2.02 |
| First slice graph | Observation dataset with minimal fields, CDC station list entry | Observation dataset | User | P2.02 |
| Contract approval process | One user review of `docs/p2/P2_EXPORT_CONTRACT.md` before first Red test | One user review before first Red test | User | P2.02 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p2/P2_EXPORT_CONTRACT.md` | Create | Contract decisions and field mapping for the slice |
| `docs/p2/P2_VERIFICATION.md` | Create | First entries; links `docs/p2/P2_01_FOUNDATION_CHECK.md` |
| `src/WeatherParser.Parsing/Export/ObservationDataset.cs` | Create | Minimal observation dataset root entity |
| `src/WeatherParser.Parsing/Export/ObservationMeasurements.cs` | Create | Measurements entity holding `temperature_c` and its `measured_at` |
| `src/WeatherParser.Parsing/Serialization/IJsonExportSerializer.cs` | Create | Interface for JSON export serializer |
| `src/WeatherParser.Parsing/Serialization/IXmlExportSerializer.cs` | Create | Interface for XML export serializer |
| `src/WeatherParser.Parsing/Serialization/JsonExportSerializer.cs` | Create | Implementation of JSON export serializer |
| `src/WeatherParser.Parsing/Serialization/XmlExportSerializer.cs` | Create | Implementation of XML export serializer |
| `src/WeatherParser.Parsing/Serialization/ExportSerializationException.cs` | Create | Defined failure type for serialization errors |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Register both serializers |
| `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` | Change | Add registration test |
| `tests/WeatherParser.Parsing.Tests/Export/ObservationDatasetJsonContractTests.cs` | Create | Test class for JSON contract of observation dataset |
| `tests/WeatherParser.Parsing.Tests/Export/ObservationDatasetXmlContractTests.cs` | Create | Test class for XML contract of observation dataset |
| `tests/WeatherParser.Parsing.Tests/Export/ExportEntitySerializableTests.cs` | Create | Test class for serializable traversal |
| `tests/WeatherParser.Parsing.Tests/Serialization/JsonExportSerializerTests.cs` | Create | Test class for JSON serializer service |
| `tests/WeatherParser.Parsing.Tests/Serialization/XmlExportSerializerTests.cs` | Create | Test class for XML serializer service |

## 5. Procedure

### S0 – Contract record (no code)
- **Action:** write `docs/p2/P2_EXPORT_CONTRACT.md` with every section 3 decision and the slice field mapping; obtain and record the user's review. No Red/Green: S0 changes no code.
- **Roles:** qwen drafts the prose from the orchestrator's decisions; the orchestrator verifies it against REQUIREMENTS.md and the phase plan and records the user's approval.
- **Check:** document review; no build.

### S1 – Omission versus zero versus present
- **Red:** `Serialize_MissingTemperature_OmitsElement` and `Serialize_ZeroTemperature_WritesZero` (JSON and XML); expected failure with a plain `decimal?` property: XML writes `xsi:nil="true"`, JSON writes `"temperature_c": null`.
- **Green:** the omission pattern from section 3 (`{Name}Specified` companion with `[JsonIgnore]` plus `WhenWritingNull`).
- **Refactor:** None.
- **Roles:** qwen generates the tests, then the minimal implementation, then the refactoring; the orchestrator writes each brief, runs the Red test and confirms the failure reason before requesting Green, runs build and tests, reviews the diff and returns defects to qwen.
- **Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore` then `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S2 – UTC lexical form
- **Red:** `Serialize_UtcTimestamp_WritesSecondsWithoutSuffix` fails because a plain UTC `DateTime` serializes with a `Z` suffix; `Create_NonUtcOrSubSecondTimestamp_IsRejected` fails because no exception is thrown yet.
- **Green:** wire string property formatted `yyyy-MM-dd'T'HH:mm:ss` with `CultureInfo.InvariantCulture`; the internal UTC value is `[XmlIgnore][JsonIgnore]`; non-UTC kinds and non-zero sub-seconds are rejected.
- **Refactor:** None.
- **Roles:** qwen generates the tests, then the minimal implementation, then the refactoring; the orchestrator writes each brief, runs the Red test and confirms the failure reason before requesting Green, runs build and tests, reviews the diff and returns defects to qwen.
- **Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore` then `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S3 – Escaping and invalid characters
- **Red:** `Serialize_SourceWithUmlautsAndSpecialChars_RoundTrips` (`source` with umlauts, `<`, `&`, `"` read back through `JsonDocument` and `XDocument`) and `Serialize_InvalidXmlCharacter_ThrowsExportSerializationException` (`source` containing U+0001); expected failure of the second: the raw `ArgumentException` from `XmlWriter` escapes instead of the defined failure.
- **Green:** Implement validation to throw `ExportSerializationException` for invalid XML 1.0 characters.
- **Refactor:** None.
- **Roles:** qwen generates the tests, then the minimal implementation, then the refactoring; the orchestrator writes each brief, runs the Red test and confirms the failure reason before requesting Green, runs build and tests, reviews the diff and returns defects to qwen.
- **Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore` then `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S4 – Culture invariance
- **Characterization (not a Red):** `Serialize_UnderEnUsAndDeDe_UsesInvariantNumbersAndTimestamps` repeats the S1/S2 assertions with `CultureInfo.CurrentCulture` set to `en-US` and `de-DE` and restored afterwards; with S1/S2 implemented it is expected to pass immediately, and the record says so instead of claiming a Red failure. If it fails, the failure is treated as a bug: regression test first, then fix.
- **Roles:** qwen generates the tests, then the minimal implementation, then the refactoring; the orchestrator writes each brief, runs the Red test and confirms the failure reason before requesting Green, runs build and tests, reviews the diff and returns defects to qwen.
- **Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore` then `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S5 – Serializable traversal
- **Red:** `ExportTypes_AreSerializable` fails because the entity types created for S1/S2 do not carry `[Serializable]` yet.
- **Green:** Add `[Serializable]` attribute to all export entities and ensure parameterless constructors.
- **Refactor:** None.
- **Roles:** qwen generates the tests, then the minimal implementation, then the refactoring; the orchestrator writes each brief, runs the Red test and confirms the failure reason before requesting Green, runs build and tests, reviews the diff and returns defects to qwen.
- **Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore` then `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S6 – Service contract
- **Red:** `SerializeAsync_LeavesStreamOpen` fails because the first implementation disposes the writer together with the stream; `SerializeAsync_WritesNoBom` fails because the default `XmlWriter` UTF-8 encoding writes a BOM; `SerializeAsync_PreCancelledToken_WritesNothing` fails because no cancellation check exists yet; `SerializeAsync_DestinationWriteFails_Throws` fails because the failure is caught or wrapped incorrectly. Each expected reason is confirmed when the Red test runs.
- **Green:** Implement correct stream handling, BOM suppression, cancellation checks, and error propagation.
- **Refactor:** None.
- **Roles:** qwen generates the tests, then the minimal implementation, then the refactoring; the orchestrator writes each brief, runs the Red test and confirms the failure reason before requesting Green, runs build and tests, reviews the diff and returns defects to qwen.
- **Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore` then `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S7 – DI registration
- **Red:** `AddWeatherParsing_RegistersExportSerializers` fails because the registrations are missing.
- **Green:** Add registrations to `ServiceCollectionExtensions.AddWeatherParsing(IServiceCollection)`.
- **Refactor:** None.
- **Roles:** qwen generates the tests, then the minimal implementation, then the refactoring; the orchestrator writes each brief, runs the Red test and confirms the failure reason before requesting Green, runs build and tests, reviews the diff and returns defects to qwen.
- **Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore` then `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

## 6. Tests and Test IDs
| Step-local case | T-P2 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 Omission versus zero versus present | T-P2-17 | REQ-053, REQ-067 | Missing temperature omitted in both formats (no `xsi:nil`, no `null`); zero written (partial – completed in P2.08) |
| S2 UTC lexical form | T-P2-05, T-P2-16 | REQ-051, REQ-067 | `YYYY-MM-DDTHH:mm:ss` without `Z`/offset in both formats; non-UTC and sub-second values rejected (partial – completed in P2.04 and P2.08) |
| S3 Escaping and invalid characters | T-P2-17 | REQ-067, REQ-076–077 | Umlauts and XML-special characters round-trip; U+0001 yields the defined failure (partial – completed in P2.08) |
| S4 Culture invariance | T-P2-05 | REQ-051 | Invariant numbers and timestamps under `en-US` and `de-DE` (partial – completed in P2.04 and P2.08) |
| S5 Serializable traversal | T-P2-01 | REQ-074, REQ-076 | Every custom type of the slice graph is `[Serializable]` with a public parameterless constructor (partial – completed in P2.08) |
| S6 Service contract | T-P2-18 | REQ-069, REQ-071 | Caller stream left open, UTF-8 without BOM, pre-cancelled token writes nothing, destination write failure propagates (partial – completed in P2.08) |
| S7 DI registration | T-P2-18 | REQ-070–071, REQ-077 | `AddWeatherParsing` resolves both serializer interfaces from a real `ServiceCollection` (partial – completed in P2.08; package-consumer DI in P2.10) |
## 7. Evidence to Record
- Per slice in `docs/p2/P2_VERIFICATION.md`: Red test names with the observed failure message (S4: "characterization, passed immediately"), Green and Refactor results, commands, test counts, checkpoint commit IDs only if the user requested commits, and qwen/orchestrator authorship.
- The user's contract approval with its date in `docs/p2/P2_EXPORT_CONTRACT.md`.
- The link to `docs/p2/P2_01_FOUNDATION_CHECK.md` in `docs/p2/P2_VERIFICATION.md`.
## 8. Risks and Limitations
- Risk: A Red that fails only by compile error is invalid — Mitigation: Slice design ensures behavioral failure
- Risk: `decimal` keeps its scale, so `20.30m` and `20.3m` serialize differently — Mitigation: Contract tests compare numeric values; P2.05 fixes output scale
- Risk: Synchronous `XmlSerializer` limits cancellation granularity — Mitigation: Documented limitation in contract
- Risk: Every new code block needs a meaningful test due to strict-improvement coverage rule — Mitigation: Slice design ensures all blocks are tested

## 9. Acceptance Criteria
- [x] Contract decisions are recorded in `docs/p2/P2_EXPORT_CONTRACT.md` and approved by user before S1
- [x] Minimal observation dataset entity is implemented with correct property types and attributes
- [x] Measurements entity is implemented with correct property types and attributes
- [x] JSON and XML serializer services are implemented with injectable interfaces
- [x] Every slice except S0 and S4 has a recorded Red failure for the intended reason before its Green change; all tests pass after Refactor.
- [x] Implementation uses recommended patterns for omission, time representation, numeric type, naming approach, service shape, encoding/BOM, and invalid character handling
- [x] Entities are explicitly `[Serializable]` and traversable by reflection
- [x] Serializer services handle cancellation, stream ownership, and UTF-8 encoding correctly
- [x] A local coverage report shows no new code block without a meaningful test
- [x] No `xsi:nil`, no `Z` suffix and no BOM in the slice output

All criteria are evidenced in [P2_VERIFICATION.md](P2_VERIFICATION.md) section 2 and by hosted CI run 39383342526. Documented deviations (verification record section 2.4, contract section 5): S1's XML half and several S6 guarantees passed immediately and are recorded as characterization; S1 already created minimal serializer services; quantities are value objects (C-14), so the `{Name}Specified` scalar pattern of C-05 was not yet needed. The user's review fixes (unset timestamps and values, JSON failure wrapping, fixed `schema_version`) are recorded in section 2.6.