# P3.04 – Argument Grammar and Validation: Implementation Plan

**Status: planned on 2026-10-29; accepted by the user on 2026-10-29 with the phase plan ("Accept the plans"); not started: P3 starts only on a separate instruction, and the CL rows are decided in P3.02.** Refines the P3.04 row of section 5 and the "Validation first" row of section 4 of [PHASE_P3_PLAN.md](../../PHASE_P3_PLAN.md).

## 1. Goal and Scope
Goal: turn the argument vector into exactly one validated command request, or a usage error, before any configuration, network, file or broker access.
In scope: the parser of CL-02 behind its interface (Proposed: `ICommandLineParser`) and the per-command validator (Proposed: `ICommandRequestValidator`) producing immutable request objects (Proposed: `SelectDwdRequest`, `ParseDwdRequest`, `ParseOstHessenRequest`, `SelectMosmixRequest`, `ParseMosmixRequest`, `HelpRequest`) with the global options `--Output`, `--OutputFile`, `--Config` and, if approved, `--Verbose` and the CL-17 option; value parsing through the library (`ICoordinateParser` for coordinates; the library's ID and pattern rules as approved in the contract); complete help texts per command (commands, options, units, examples).
Out of scope with owner: whether `--OutputFile` exists on disk → P3.07 (CL-12); configuration values such as the DWD reference station → P3.05; exit-code numbers → P3.06 (this step returns a typed usage error).

## 2. Preconditions and Inputs
- P3.03 complete; P3.02 approved CL-02, CL-03 and CL-17.
- Inputs: REQ-010, REQ-011, REQ-020, REQ-021, REQ-040, REQ-065, REQ-066, REQ-100, REQ-103, REQ-111; the library's `ICoordinateParser` (SC-07 grammar) and the ID grammars recorded in the P3.01 extraction (DWD station IDs, MOSMIX IDs, the OsthessenWetter pattern rules).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Value validation owner | Reuse library validation / duplicate it in the CLI | Reuse: coordinates through `ICoordinateParser`, search limits through the library's `SearchParameters` rules where they are public; only syntax the library does not expose (for example the `--Product` values, the shape of `--MOSMIXID` before retrieval) is checked in the CLI, with the rule cited from the contract; a library exception is mapped to the usage error with its library ID | Orchestrator, recorded (CL-03) | S2 |
| Numeric format | Invariant culture only / current culture | Invariant culture only, a `.` decimal separator, no thousands separators, no exponent; tested under a non-invariant current culture | Orchestrator, recorded (REQ-021) | S2 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `examples/WeatherParser.Cli/Arguments/…` | Create | Parser, validator, request types, help texts |
| `examples/WeatherParser.Cli/CompositionRoot.cs` | Change | Registration |
| `tests/WeatherParser.Cli.Tests/Arguments/…` | Create | Unit tests; process tests for help and for zero network access |
| `docs/p3/P3_VERIFICATION.md` | Change | P3.04 section |

## 5. Procedure
Every slice: roles and checks as in P3.03 section 5.

### S1 Grammar
**Red**: `Parse_NoArguments_UsageErrorCommandMissing`, `Parse_TwoCommands_UsageErrorCommandConflict`, `Parse_UnknownCommand_UsageError`, `Parse_UnknownOption_UsageError`, `Parse_DuplicateOption_UsageError`, `Parse_DuplicateOptionDifferentCase_UsageError`, `Parse_OptionNamesAnyCase_Accepted` (`--selectdwd`, `--SELECTDWD`, `--Latitude`/`--latitude`), `Parse_CommandAtAnyPosition_Accepted`, `Parse_MissingValue_UsageError`, `Parse_ValueStartingWithDoubleDash_UsageError`, `Parse_LangitudeTypo_UnknownOption`, `Parse_OptionNotAllowedForCommand_UsageError` (one case per row of the CL-03 allowed-options table), `Parse_MissingRequiredOption_UsageError` (each required option of each command), `Parse_OutputFileWithMqtt_UsageError`, `Parse_OutputFileWithoutOutput_UsageError` (default `mqtt`), `Parse_UnknownOutputFormat_UsageError`.
**Green**: parser and grammar table.

### S2 Values
**Red**: `Validate_DecimalCoordinates_Accepted`, `Validate_QuotedDmsCoordinates_Accepted` (`50° 33′ 18″ N`, `9° 40′ 26″ E`), `Validate_CoordinateOutOfRange_UsageErrorWithLibraryId` (`COORDINATE-INVALID`), `Validate_WrongHemisphere_UsageError`, `Validate_UnderGermanCulture_SameResult` (current culture `de-DE`), `Validate_NegativeAltitude_Accepted`, `Validate_ZeroDistanceAndAltitudeDifference_Accepted`, `Validate_NegativeDistance_UsageError`, `Validate_ProductValues_OnlyMosmixSAndMosmixL` (case per the contract), `Validate_MosmixIdShape` (leading zeros and alphanumeric IDs accepted unchanged as strings; empty and malformed rejected), `Validate_DwdStationIdShape` (leading zeros preserved), `Validate_StationNamePattern` (`*`, `?`, umlauts accepted; empty rejected).
**Green**: validator and request types.

### S3 Help and no side effects
**Red**: `Help_EveryCommand_ListsOptionsUnitsAndExamples`, `Help_WithCommand_PrintsCommandHelp`, `Process_InvalidArguments_NoConfigurationRead_NoNetwork_NoFile` (the process test passes a configuration path that does not exist and a loopback fake HTTP server; every invalid-argument case fails with the usage error, the server records zero requests, the working directory stays empty).
**Green**: help texts; the validation runs before configuration loading in `Main`.

**Refactor**: with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P3 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P3-02 | REQ-010, REQ-011, REQ-065, REQ-066 | Grammar and combinations |
| S2 | T-P3-03 | REQ-020, REQ-021, REQ-040, REQ-100, REQ-103 | Values, culture independence |
| S3 | T-P3-03, T-P3-04 | REQ-010 | Help; no side effects on invalid input |
| all | T-P3-20 | REQ-085, REQ-086 | Red evidence; coverage |

## 7. Evidence to Record
- Red names and failures, counts, coverage counters, authorship, Codex rounds

## 8. Risks and Limitations
- (a) Windows and Linux shells pass quoted DMS strings differently; the process tests pass arguments as an argument list, not a command string, and the README examples are checked on both platforms in P3.11

## 9. Acceptance Criteria
- [ ] Every listed test green with recorded Red
- [ ] No invalid argument reaches configuration, network, file or broker
- [ ] Coverage gate passed; Codex 0 findings or reported
