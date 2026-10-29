# P3.07 – Command Handlers and Export Output: Implementation Plan

**Status: planned on 2026-10-29; under review, not yet accepted.** Refines the P3.07 row of section 5 and the "Output", "Separation" and "Restrictions" rows of section 4 of [PHASE_P3_PLAN.md](../../PHASE_P3_PLAN.md).

## 1. Goal and Scope
Goal: the five command handlers that call the library services with the validated request, build the result of CL-09 and write it as one complete JSON or XML document to stdout or to a new file, with partial results and the restrictions of CL-16 and CL-17; MQTT output is wired through a publisher seam that P3.08 implements.
In scope: one handler per command behind an interface (Proposed: `ICommandHandler<TRequest>`); `--SelectDWD` over `ICdcStationSelector`, `--SelectMOSMIX` over `IMosmixLocationSelector` (and, per CL-17, `IMosmixSEvidenceRefresher`), `--ParseDWD` over `IDwdObservationParser`, `--ParseOstHessen` over `IOsthessenWetterParser` (datasets and station failures), `--ParseMOSMIX` over `IMosmixLParser` or `IMosmixSParser` selected by `--Product`, never a fallback; the result roots of CL-09 (standalone lists; `ExportDocument` with observations or forecast runs and `ExportError` records, C-83 to C-86); the output writer (Proposed: `IOutputWriter`) for stdout and for files under CL-12; the OW-14 rule of CL-16 before retrieval; the outcome reported to the exit-code model of P3.06; the publisher seam (Proposed: `IMessagePublisher`) with a fake in tests.
Out of scope with owner: the MQTT client, acknowledgment and retries → P3.08; signals and the command deadline → P3.09.

## 2. Preconditions and Inputs
- P3.06 complete; P3.02 approved CL-09, CL-12, CL-16 and CL-17.
- Inputs: REQ-002, REQ-023, REQ-030 to REQ-032, REQ-040, REQ-041, REQ-065 to REQ-069, REQ-101, REQ-103, REQ-105, REQ-111; C-01, C-83 to C-88; SC-22; OW-14; the library result types (P3.01 extraction).
- Test sources: a loopback fake HTTP server or the library's injectable HTTP handler seam with synthetic fixtures of every source (the synthetic fixtures of the library tests are the model; nothing is copied from a real source file; OsthessenWetter fixtures are synthetic, OW-14).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Test HTTP boundary | A loopback fake HTTP server in process tests and a fake `HttpMessageHandler` in handler tests / only one of them | Both: handler tests replace the registered `HttpClient` with one over a fake handler (no socket); a small number of process tests use a loopback server bound to `127.0.0.1` with an ephemeral port, so the real composition root is exercised; no test reaches the internet (REQ-083) | Orchestrator, recorded (CL-20) | S1 to S4 |
| `ExportError` messages | Fixed English text per ID / the library message | Per CL-10: a fixed English text per ID, plus the library message where the library documents it as free of values; one line, no CR or LF (C-86); `source` in the C-50 or C-68 form of the failed item | Orchestrator, recorded (CL-10) | S2 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `examples/WeatherParser.Cli/Commands/…` | Create | Five handlers, result building |
| `examples/WeatherParser.Cli/Output/…` | Create | Output writer for stdout and files (CL-12), publisher seam |
| `examples/WeatherParser.Cli/CompositionRoot.cs` | Change | Registrations |
| `tests/WeatherParser.Cli.Tests/Commands/…`, `Output/…`, `Fixtures/…` | Create | Handler, output and process tests; synthetic fixtures; the fake HTTP server |
| `docs/p3/P3_VERIFICATION.md` | Change | P3.07 section |

## 5. Procedure
Every slice: roles and checks as in P3.03 section 5.

### S1 Selection commands
**Red**: `SelectDwd_Json_StandaloneCdcStationList`, `SelectDwd_Xml_SchemaValidAgainstPackagedXsd`, `SelectDwd_EmptySelection_ValidEmptyListExitSuccess`, `SelectMosmix_Json_StandaloneMosmixLocationList`, `SelectMosmix_Xml_SchemaValid`, `SelectMosmix_MosmixSWithoutIndex_EvidenceMissing` (exit and ID per CL-11 and the library ID), `SelectMosmix_RefreshOption_RefreshesBeforeSelection` (per CL-17; if option A is approved: `SelectMosmix_HasNoRefreshOption`), `SelectDwd_Mqtt_OneNonRetainedMessageToPublisher` (fake publisher; QoS 1, retain false).
**Green**: the two selection handlers.

### S2 Parse commands
**Red**: `ParseDwd_Json_ExportDocumentWithOneObservation`, `ParseDwd_RetrievalFailure_ExportDocumentWithError_ExitFailure`, `ParseOstHessen_SeveralMatches_SeparateDatasets`, `ParseOstHessen_SomeStationsFail_ObservationsAndErrors_ExitPartial`, `ParseOstHessen_NoMatch_Fails`, `ParseMosmix_MosmixL_UsesMosmixLParserOnly`, `ParseMosmix_MosmixS_UsesMosmixSParserOnly` (strict mocks: the other parser is never called), `ParseMosmix_Json_ExportDocumentWithForecastRun_NoMeasuredAt`, `Parse_Xml_SchemaValid` (each parse command), `Parse_Mqtt_OneMessagePerDatasetOrRun_StandaloneRoot` (fake publisher; never the envelope), `ObservationCommands_NeverPublishForecasts`, `ParseMosmix_NeverPublishesObservations`, `Exports_JsonAndXml_SemanticallyEquivalent` (every command; UTC, omitted values, zeros, leading-zero IDs, umlauts, XML escaping, through the real serializers).
**Green**: the three parse handlers and the result building.

### S3 File and stdout output
**Red**: `Stdout_ContainsOnlyTheDocument` (process test), `Stdout_DocumentWrittenAfterCompleteSerialization` (a serializer double that fails midway: stdout stays empty), `OutputFile_ExistingDestination_RejectedBeforeRetrieval` (zero fake-server requests), `OutputFile_MissingDirectory_RejectedBeforeRetrieval`, `OutputFile_Success_Utf8WithoutBom_NoTemporaryFileLeft`, `OutputFile_DestinationCreatedDuringRun_NotOverwritten_TemporaryFileDeleted`, `OutputFile_WriteFailure_NoDestinationNoTemporaryFile`, `OutputFile_IdenticalInput_IdenticalBytes`, `JsonXmlMode_NoBrokerSettingsNeeded_NoConnection` (the fake publisher records no call).
**Green**: output writer.

### S4 OsthessenWetter restriction
**Red**: `ParseOstHessen_Mqtt_WithoutPermission_FailsBeforeRetrieval` (ID `CLI-OSTHESSEN-PUBLICATION-NOT-PERMITTED` or as approved; zero requests), `ParseOstHessen_Mqtt_WithPermission_PublishesSyntheticDatasets`, `ParseOstHessen_Json_UnaffectedByPermission` (per CL-16; if option B is approved, every mode fails without the permission).
**Green**: the CL-16 check.

**Refactor**: with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P3 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P3-10, T-P3-17 | REQ-002, REQ-023, REQ-101, SC-22 | Selection results |
| S2 | T-P3-11, T-P3-13, T-P3-06 | REQ-030 to REQ-032, REQ-040, REQ-041, REQ-067, REQ-103, REQ-105, REQ-111 | Parse results, separation, equivalence |
| S3 | T-P3-12 | REQ-065 to REQ-069, REQ-073 | Output |
| S4 | T-P3-16 | OW-14 | Restriction |
| all | T-P3-20 | REQ-085, REQ-086 | Red evidence; coverage |

## 7. Evidence to Record
- Red names and failures, counts, coverage counters, authorship, the fixture list (all synthetic), Codex rounds

## 8. Risks and Limitations
- (a) The `ExportDocument` rule "a `source` at most once" (C-85) means a command must never produce two outcomes for one item; the handler tests cover a failed and a succeeded item of different sources and the library rejects a violation at serialization
- (b) MQTT behavior in this step is a fake publisher; acknowledgment, TLS and broker behavior are P3.08 and P3.11 evidence

## 9. Acceptance Criteria
- [ ] Every listed test green with recorded Red
- [ ] Every command produces the CL-09 roots in JSON and XML (schema-valid) and the fake-publisher messages for MQTT
- [ ] No file, broker or retrieval side effect on rejected input; coverage gate passed; Codex 0 findings or reported
