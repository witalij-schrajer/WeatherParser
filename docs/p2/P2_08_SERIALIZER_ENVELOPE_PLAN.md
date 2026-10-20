# P2.08 – Full Serializer and Envelope Contract: Implementation Plan

**Status: completed (2026-10-20, marked completed by the user after the hosted CI passed).** Implemented test-first in commit `7c9b919`; hosted CI run 39466213992 of draft pull request #3 passed on Windows x64, Ubuntu x64 and Debian 13 ARM64. Contract decisions C-82 to C-94 refine this plan (graph validation stays in `IExportRootValidation` instead of an injected `IExportGraphValidator`, one outcome per source, XML newline handling C-92; see the deviation list in section 12 of the export contract); evidence in [P2_VERIFICATION.md](P2_VERIFICATION.md) section 8. Original planning note: this document started as a plan on 2026-10-20. This plan refines the P2.08 row of section 5, the "Envelope/errors" row of section 4 and the "Envelope and roots", "Ordering" and "Serializer service contract" decisions of section 3 of [PHASE_P2_PLAN.md](../../PHASE_P2_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief and corrected by qwen in one review round (S0 without Red/Green/Check). User review (2026-10-20): qwen added graph validation through both serializers (S10) and reclassified S6 as characterization over two rounds; the orchestrating agent appended one risk sentence that qwen did not apply. qwen then added the validator registration; the orchestrating agent moved that sentence from the document end into the S10 Green line and added backticks.

## 1. Goal and Scope
Goal: complete the export contract: the typed multi-result envelope with typed error records, the full set of supported roots (envelope, CDC list, MOSMIX list, standalone observation dataset, standalone forecast run), rejection of unsupported roots, and whole-graph proof with the real serializers — semantic JSON/XML equivalence, UTC and culture invariance, escaping and omission, deterministic output, UTF-8 without BOM, caller-owned streams, cancellation and failure behavior — all from the same prepared graph without a second assembler.

In scope:
- Envelope and error entities
- Supported-root registry in the serializer services
- Completion of the T-P2-01, -05, -15, -16, -17 and -18 evidence

Out of scope:
- XSD generation and schema validation → P2.09
- Package-consumer DI → P2.10
- File/stdout destinations, overwrite protection, atomic file creation and exit codes → P3 (REQ-066)
- MQTT message creation and topics → P8
- Logging → callers

Prerequisites: P2.03, P2.06 and P2.07 complete (all root graphs exist).

## 2. Preconditions and Inputs
- P2.03, P2.06 and P2.07 have completed their respective deliverables.
- The existing library project `src/WeatherParser.Parsing/` and test projects `tests/WeatherParser.Parsing.Tests/` are available.
- User authorization for contract decisions in section 3.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Envelope type and collections | Proposed `ExportDocument` with `schema_version`, `observations`, `forecast_runs`, `errors`; station lists only as standalone roots / one envelope for every result kind | First option (confirms the P2.02 S0 record; lists come from single-result commands) | User (contract review) | S1 |
| Error record fields | Proposed `ExportError` with `error_id` (stable ID), `message` (English), `source`, optional `station_id` or `mosmix_id` / other | Proposed fields; no stack traces, configuration or secrets | User (contract review) | S2 |
| Partial-result marker | no extra field: a non-empty `errors` collection marks a partial result / explicit `result_status` field | OPEN – user decides; the test data covers the chosen form | User | S2 |
| Order of results inside the envelope | caller order preserved (deterministic for identical input) / sorted by `source` | Caller order preserved and documented; lists and steps keep their own ordering rules | Orchestrator, recorded | S1, S9 |
| Supported-root registry | fixed set of five root types checked before serialization / any type | Fixed set; anything else raises `ExportSerializationException` | Orchestrator, recorded | S3 |
| Graph validation owner | the serializer services call an injected `IExportGraphValidator` before writing, for every root, in both formats / rely on the builders only | First option: builders cannot stop publicly constructed graphs, and REQ-050/REQ-107 forbid exporting unusable datasets or runs as success | Orchestrator, recorded | S10 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p2/P2_EXPORT_CONTRACT.md` | Change | Envelope, error record, roots |
| `docs/p2/P2_VERIFICATION.md` | Change | P2.08 evidence, completed T-P2 rows |
| `src/WeatherParser.Parsing/Export/ExportDocument.cs` | Create | Proposed |
| `src/WeatherParser.Parsing/Export/ExportError.cs` | Create | Proposed |
| `src/WeatherParser.Parsing/Serialization/JsonExportSerializer.cs` | Change | Supported-root check, shared XML serializer cache for all roots |
| `src/WeatherParser.Parsing/Serialization/XmlExportSerializer.cs` | Change | Supported-root check, shared XML serializer cache for all roots |
| `src/WeatherParser.Parsing/Serialization/SupportedExportRoots.cs` | Create | Proposed |
| `src/WeatherParser.Parsing/Serialization/IExportGraphValidator.cs` | Create | Proposed: validation contract for all five roots |
| `src/WeatherParser.Parsing/Serialization/ExportGraphValidator.cs` | Create | Proposed: required fields, usable readings/values, chronological unique `valid_at`, `dataset_type` literal, suffix-free UTC strings |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Register `IExportGraphValidator` |
| `tests/WeatherParser.Parsing.Tests/Export/ExportDocumentContractTests.cs` | Create | Tests for envelope structure |
| `tests/WeatherParser.Parsing.Tests/Export/AllRootsEquivalenceTests.cs` | Create | Semantic equivalence tests |
| `tests/WeatherParser.Parsing.Tests/Export/AllRootsCultureTests.cs` | Create | Culture invariance tests |
| `tests/WeatherParser.Parsing.Tests/Export/ExportGraphTraversal.cs` | Create | Test helper that walks every custom type reachable from the roots |
| `tests/WeatherParser.Parsing.Tests/Serialization/SupportedRootTests.cs` | Create | Tests for supported root registry |
| `tests/WeatherParser.Parsing.Tests/Serialization/SerializerStreamBehaviourTests.cs` | Create | Tests for stream, encoding, cancellation and failure |
| `tests/WeatherParser.Parsing.Tests/Serialization/ExportGraphValidationTests.cs` | Create | Proposed: invalid graphs rejected through both serializers |
| `tests/WeatherParser.Parsing.Tests/Export/ExportEntitySerializableTests.cs` | Change | Add tests for `[Serializable]` attribute |

## 5. Procedure
### S0 Contract confirmation
**Action**: Confirm or update the envelope, error-record and partial-result decisions in `docs/p2/P2_EXPORT_CONTRACT.md`; user review before S1. No code, so no Red/Green.
**Roles**: qwen drafts prose; the orchestrator verifies against REQ-068–069 and REQ-072 and records the user's approval.

### S1 Envelope with multiple results and empty selection
**Red**: `Serialize_EnvelopeWithTwoObservationDatasets_IsOneCompleteDocument` and `Serialize_EmptyEnvelope_WritesEmptyCollections`. Expected failure: `ExportDocument` is first created without attributes and with uninitialized collections, so names differ and empty collections are omitted.
**Green**: Attributes; collections initialized empty; caller order preserved.
**Refactor**: None expected.
**Roles**: qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check**: 
- `dotnet restore WeatherParser.Ci.slnf --locked-mode`
- `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S2 Typed partial errors
**Red**: `Serialize_PartialResult_ContainsDatasetAndTypedError` and `Serialize_Error_IsNeverWrittenAsObservation` (the error appears only under `errors`). Expected failure: the error entity is missing from the XML mapping (`XmlArrayItem` not declared), so the XML document lacks the error.
**Green**: Error entity and mapping; partial marker per S0.
**Refactor**: None expected.
**Roles**: qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check**: 
- `dotnet restore WeatherParser.Ci.slnf --locked-mode`
- `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S3 Supported roots
**Red**: `Serialize_EachSupportedRoot_Succeeds` (five roots, both formats) and `Serialize_UnsupportedRoot_ThrowsExportSerializationException` (a `string`, a `Dictionary<string, object>`, an anonymous object). Expected failure: the serializers accept any type.
**Green**: Registry check before serialization.
**Refactor**: None expected.
**Roles**: qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check**: 
- `dotnet restore WeatherParser.Ci.slnf --locked-mode`
- `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S4 Whole-graph semantic equivalence
**Characterization (not a Red)**: `AllRoots_JsonAndXml_AreSemanticallyEqual` parse each root's JSON (`JsonDocument`) and XML (`XDocument`) and compare IDs, metadata, units, times, quantities, omissions, zeros, umlauts, XML-special characters and order. A failure is a bug with a regression test first.
**Green**: None expected.
**Refactor**: None expected.
**Roles**: qwen generates tests; the orchestrator runs them and records that they passed immediately or starts a regression test first.
**Check**: 
- `dotnet restore WeatherParser.Ci.slnf --locked-mode`
- `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S5 UTC and culture across all roots
**Characterization (not a Red)**: `AllRoots_UnderEnUsAndDeDe_WriteInvariantNumbersAndSuffixFreeUtc` (every timestamp `YYYY-MM-DDTHH:mm:ss`, `.` decimal separator). A failure is a bug with a regression test first.
**Green**: None expected.
**Refactor**: None expected.
**Roles**: qwen generates tests; the orchestrator runs them and records that they passed immediately or starts a regression test first.
**Check**: 
- `dotnet restore WeatherParser.Ci.slnf --locked-mode`
- `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S6 Stream, encoding, cancellation and failure
**Characterization (not a Red)**: `SerializeAsync_DestinationFailsMidWrite_PropagatesAndLeavesStreamOpen`, `SerializeAsync_TokenCancelledBeforeCopy_WritesNothing` and `SerializeAsync_AllRoots_WriteUtf8WithoutBom` extend the guarantees implemented and tested in P2.02 S6 to all five roots. They are expected to pass immediately; a failure is a regression, handled with a regression test first and a fix, never by reintroducing the defect to obtain a Red.
**Green**: None expected; document the cancellation granularity in the contract record.
**Refactor**: None expected.
**Roles**: qwen generates tests; the orchestrator runs them and records that they passed immediately or starts a regression test first.
**Check**: 
- `dotnet restore WeatherParser.Ci.slnf --locked-mode`
- `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S7 Same prepared graph for both formats
**Characterization (not a Red)**: `SameInstance_SerializesToBothFormatsWithoutMutation` serializes one root instance with both serializers and asserts the instance is unchanged and no mapping layer exists between the services and the entities.
**Green**: None expected.
**Refactor**: None expected.
**Roles**: qwen generates tests; the orchestrator runs them and records that they passed immediately or starts a regression test first.
**Check**: 
- `dotnet restore WeatherParser.Ci.slnf --locked-mode`
- `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S8 Full Serializable traversal
**Red**: `AllReachableExportTypes_AreSerializableWithParameterlessConstructor` uses `ExportGraphTraversal` from all five roots. Expected failure: `ExportDocument` and `ExportError` are first created without `[Serializable]`.
**Green**: Add the attribute.
**Refactor**: None expected.
**Roles**: qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check**: 
- `dotnet restore WeatherParser.Ci.slnf --locked-mode`
- `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S9 Deterministic output
**Characterization (not a Red)**: `SerializeTwice_IdenticalInput_ProducesIdenticalBytes` for every root and both formats.
**Green**: None expected.
**Refactor**: None expected.
**Roles**: qwen generates tests; the orchestrator runs them and records that they passed immediately or starts a regression test first.
**Check**: 
- `dotnet restore WeatherParser.Ci.slnf --locked-mode`
- `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S10 Validation of publicly constructed graphs
**Red**: `Serialize_ObservationWithoutUsableReadings_IsRejectedInBothFormats`, `Serialize_ForecastRunWithUnsortedOrDuplicateValidAt_IsRejectedInBothFormats`, `Serialize_RootMissingRequiredField_IsRejectedInBothFormats` (for example an observation dataset without `source`) and `Serialize_ForecastRunWithWrongDatasetType_IsRejected`; each graph is built directly through the public entity setters, not through a builder, and each test calls both `IJsonExportSerializer` and `IXmlExportSerializer`. Expected failure: both serializers write the invalid graph and report success, because only the root type is checked.
**Green**: `ExportGraphValidator` applies the rules; both serializers call it through the injected `IExportGraphValidator` before writing anything and raise `ExportSerializationException` naming the violated rule; nothing is written to the destination on failure. `ExportGraphValidator` is registered in `ServiceCollectionExtensions.AddWeatherParsing`, and the existing registration test is extended to resolve `IExportGraphValidator`.
**Refactor**: Share the rule set with the P2.06/P2.07 builders where the rules are identical, so builders and validator cannot diverge.
**Roles**: qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check**: 
- `dotnet restore WeatherParser.Ci.slnf --locked-mode`
- `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

## 6. Tests and Test IDs
| Step-local case | T-P2 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P2-15 | REQ-068, REQ-074 | Multi-result envelope and empty selection as one complete document (completes T-P2-15 with S2 and S9) |
| S2 | T-P2-15 | REQ-068, REQ-072 | Typed partial errors distinct from valid data; errors never written as observations |
| S3 | T-P2-18 | REQ-062, REQ-069, REQ-075 | Five supported roots including standalone dataset and run; unsupported roots rejected |
| S4 | T-P2-16, T-P2-17 | REQ-067, REQ-075–077 | Semantic JSON/XML equivalence, escaping, omission and zeros for every root (completes both IDs) |
| S5 | T-P2-05 | REQ-051, REQ-067 | Suffix-free UTC and invariant numbers in every root under `en-US` and `de-DE` (completes T-P2-05) |
| S6 | T-P2-18 | REQ-068–069, REQ-071 | UTF-8 without BOM, caller stream left open, cancellation and write failures propagate, never a success signal (completes T-P2-18) |
| S7 | T-P2-16 | REQ-069, REQ-075 | One prepared graph serves both formats without a second assembler |
| S8 | T-P2-01 | REQ-074, REQ-076–077 | Every reachable custom type `[Serializable]` with a public parameterless constructor (completes T-P2-01) |
| S9 | T-P2-15 | REQ-068 | Identical input produces identical bytes in both formats |
| S10 | T-P2-04, T-P2-14, T-P2-18 | REQ-050, REQ-068–069, REQ-107 | Publicly constructed invalid graphs rejected through both serializer entry points before anything is written |

## 7. Evidence to Record
- All entries go to `docs/p2/P2_VERIFICATION.md` per slice (Red names with observed failure messages, characterization results, Green/Refactor results, commands, test counts, authorship)
- A table marking T-P2-01, -05, -15, -16, -17 and -18 as completed with their test names
- Decisions and approval date go to `docs/p2/P2_EXPORT_CONTRACT.md`

## 8. Risks and Limitations
- Characterization tests that pass immediately prove behavior but not a Red — the record states this honestly instead of claiming TDD failures. S6 re-checks P2.02 guarantees for all roots and must never be turned into a Red by reintroducing a defect.
- Byte-identical output depends on stable property order and on no time-dependent fields — any generated timestamp would break S9, so none is added.
- The partial-result marker is an OPEN user decision.
- XML cancellation remains coarse-grained (before and after serialization) — documented, not hidden.
- Every new code block needs a meaningful test (strict-improvement coverage rule).
- Public setters let consumers bypass the builders — S10 enforces the rules again at serialization time in both formats.

## 9. Acceptance Criteria
- [x] S0 approval recorded
- [x] Red failures recorded for S1–S3, S8 and S10
- [x] S4–S7 and S9 recorded as characterization or with regression tests for any defect found
- [x] T-P2-01, -05, -15, -16, -17 and -18 marked completed with test names
- [x] No file/stdout or MQTT code added
- [x] All tests pass
- [x] Invalid publicly constructed graphs are rejected through both serializers before anything is written
