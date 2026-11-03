# P3.06 – Logging, Error IDs and Exit Codes: Implementation Plan

**Status: planned on 2026-10-29; accepted by the user on 2026-10-29 with the phase plan ("Accept the plans"); reconciled on 2026-10-29 with the [P3 CLI contract](../../p3/P3_CLI_CONTRACT.md) approved by the user on 2026-10-29 ("Approve all"), section 10 of this plan; where this plan's earlier text differs, section 10 and the contract win.** Refines the P3.06 row of section 5 and the "Failures and status" row of section 4 of [PHASE_P3_PLAN.md](../PHASE_P3_PLAN.md).

## 1. Goal and Scope
Goal: every diagnostic of the CLI and of the library goes to stderr in one line with UTC, severity, stable ID, command and source or station; every outcome ends with the approved exit code; nothing secret or payload-like is ever written.
In scope: the stderr logger provider of CL-08 and its line format; registration of `ILogger<>` for the library; the CLI error IDs of CL-10 and the mapping of every library exception type (from the P3.01 extraction) to a log entry, an exit code and a failure kind (CL-09): an item failure, which for parse commands becomes an `ExportError` record with the failed item's C-50 or C-68 `source` (the record itself is written in P3.07), or a command-level failure without an identified item, which is reported on stderr only and produces no document; the exit codes of CL-11 replacing the provisional codes of P3.03; the outcome model (Proposed: `CommandOutcome` with succeeded and failed items) that P3.07 and P3.08 fill; a top-level handler for unexpected exceptions.
Out of scope with owner: command behavior → P3.07; transport failures → P3.08 (their IDs are defined here); signals → P3.09.

## 2. Preconditions and Inputs
- P3.05 complete; P3.02 approved CL-08, CL-10 and CL-11.
- Inputs: REQ-007, REQ-063, REQ-068, REQ-072, REQ-073; C-86, C-87; the library exception types and IDs and the notice IDs (P3.01 extraction; README "Error and notice IDs" of each library section).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Library exception messages in logs | Log the library message / log the ID only | Log the library message: the library documents that its messages name rules and identifiers, never payload content, placeholder values or secrets (P5 to P8 contracts); the CLI's own messages follow the same rule; a test with a marker value in configuration and data checks that no marker appears | Orchestrator, recorded (CL-08, CL-10) | S1, S2 |
| Severity of library notices | As the library logs them / remapped | As the library logs them (for example `SELECTION-EMPTY` Information, `MOSMIX-S-EVIDENCE-STALE` Warning) | Orchestrator, recorded | S1 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `examples/WeatherParser.Cli/Diagnostics/…` | Create | Logger provider, line formatter, error IDs, exception mapping, outcome model, exit codes |
| `examples/WeatherParser.Cli/Program.cs`, `CompositionRoot.cs` | Change | Logging registration, top-level handler, exit codes |
| `tests/WeatherParser.Cli.Tests/Diagnostics/…` | Create | Unit and process tests |
| `docs/p3/P3_VERIFICATION.md` | Change | P3.06 section |

## 5. Procedure
Every slice: roles and checks as in P3.03 section 5.

### S1 Logger and line format
**Red**: `LogLine_ContainsUtcSeverityIdCommandSource` (a fixed `TimeProvider`; the UTC field in the C-06 layout), `LogLine_WithoutSource_OmitsSourceField`, `LogLine_SingleLine_NewlinesInMessageEscaped`, `LogLine_Exception_TypeIdAndMessageOnly_NoStackTrace`, `Logger_WritesToStderrOnly` (captured writers: stdout empty), `Logger_DefaultLevelInformation`, `Logger_VerboseLowersToDebug` (if CL-08 approves `--Verbose`), `LibraryLogger_ResolvesForLibraryServices`.
**Green**: provider and formatter.

### S2 Error mapping and exit codes
**Red**: `Map_EachLibraryExceptionType_KeepsLibraryErrorId` (one case per type and ID of the P3.01 extraction), `Map_FailureKind_ItemOrCommandLevel` (each ID classified as in CL-09; for example `OSTHESSEN-PAGE-UNAVAILABLE` and `OSTHESSEN-PATTERN-NO-MATCH` command-level, an OsthessenWetter station failure and a DWD retrieval failure of the requested station item failures), `Map_CommandLevelFailure_HasNoSource` (no `source` is derived from a pattern or a placeholder), `Map_ArgumentError_ExitUsage`, `Map_ConfigurationError_ExitUsage`, `Outcome_AllSucceeded_ExitSuccess`, `Outcome_EmptySelection_ExitSuccess`, `Outcome_SomeFailed_ExitPartial`, `Outcome_AllFailed_ExitFailure`, `Outcome_Cancelled_ExitCancelled`, `Unexpected_Exception_ExitFailureWithCliUnexpected`, `ErrorIds_MatchC86Grammar` (every CLI ID), `Diagnostics_NoMarkerValues` (marker values in configuration and source data never appear on stderr), `Process_UsageAndConfigurationFailures_StdoutEmpty` (the failures that exist at this step, before any command runs: stdout stays empty and the exit code is the usage code of CL-11); the stdout behavior of command outcomes (empty when no export is produced, one complete envelope for partial and item failures) is tested in P3.07 S3, because it needs the command handlers.
**Green**: mapping, outcome model, exit codes in `Main`.

**Refactor**: with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P3 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P3-08 | REQ-072, REQ-073 | Line format and channel |
| S2 | T-P3-09, T-P3-08 | REQ-007, REQ-063, REQ-068, C-87 | IDs and exit codes |
| all | T-P3-20 | REQ-085, REQ-086 | Red evidence; coverage |

## 7. Evidence to Record
- Red names and failures, counts, coverage counters, authorship, the complete exception-to-ID-to-exit-code table, Codex rounds

## 8. Risks and Limitations
- (a) A future library version can add error IDs; unknown IDs are passed through unchanged and logged, and the mapping table is rechecked with every package update

## 9. Acceptance Criteria
- [ ] Every listed test green with recorded Red
- [ ] Every library exception type mapped; every exit code of CL-11 reachable and tested
- [ ] Coverage gate passed; Codex 0 findings or reported

## 10. Reconciliation with the approved contract (P3.02 S5)

- CL-08 A: the line format `<yyyy-MM-ddTHH:mm:ss> UTC <LEVEL> <ID> command=<command> [source=<source>] <message>`, `-` when no ID, CR and LF replaced by spaces; `Microsoft.Extensions.Logging` 10.0.13 (CL-22 A) added to `Directory.Packages.props` and `DEPENDENCIES.md` in this step.
- CL-10: the transport IDs are `CLI-MQTT-CONNECT-FAILED`, `CLI-MQTT-PUBLISH-FAILED`, `CLI-MQTT-PUBLISH-TIMEOUT` and `CLI-MQTT-MESSAGE-REFUSED`, plus the summary event `CLI-PUBLICATION-SUMMARY`; tests that name the former `MQTT-` transport IDs use these.
- CL-11 A: the library pre-request validation IDs `COORDINATE-INVALID`, `SEARCH-PARAMETER-INVALID`, `STATION-ID-INVALID`, `MOSMIX-ID-INVALID` and `OSTHESSEN-PATTERN-INVALID` map to exit `2`; `CLI-OUTPUT-FAILED` for a missing destination directory maps to `2`; cancellation maps to `4` also after successful items.
