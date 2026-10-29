# P3.09 – Cancellation, Deadlines and Process Behavior: Implementation Plan

**Status: planned on 2026-10-29; accepted by the user on 2026-10-29 with the phase plan ("Accept the plans"); not started: P3 starts only on a separate instruction, and the CL rows are decided in P3.02.** Refines the P3.09 row of section 5 and the "Single command" and "Cancellation" rows of section 4 of [PHASE_P3_PLAN.md](../../PHASE_P3_PLAN.md).

## 1. Goal and Scope
Goal: a signal or the overall command deadline cancels all work through one token, removes temporary files, logs a summary and ends the process with the approved exit code, never with a success claim; and process tests prove single execution and termination on both platforms.
In scope: the signal handling of CL-18 (SIGINT and SIGTERM through `PosixSignalRegistration`, Ctrl+C on Windows), the grace period, the second-signal rule; the command deadline of CL-14 linked to the signal token; propagation of the token to every library, output and transport call; cleanup of the temporary file of CL-12; the cancellation outcome and exit code; process tests of single execution, termination and channels.
Out of scope with owner: live and target runs → P3.11.

## 2. Preconditions and Inputs
- P3.08 complete; P3.02 approved CL-11, CL-14 and CL-18.
- Inputs: REQ-006, REQ-007, REQ-063, REQ-066; the library's cancellation contracts (caller cancellation surfaces as `OperationCanceledException` and wins over library deadlines, README).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Signal test method | Send real signals to a child process / simulate through the seam | Both: unit tests trigger the cancellation seam (Proposed: `IShutdownSignal`); process tests send SIGINT and SIGTERM to the child on Linux (`kill`) and a Ctrl+C event or the seam's test trigger on Windows, where console signals cannot be sent reliably to a child without a console; the Windows limitation is recorded | Orchestrator, recorded (CL-18) | S2 |
| Deadline versus caller cancellation | One outcome for both / separate IDs | Separate IDs (`CLI-DEADLINE-EXCEEDED`, exit `1`; `CLI-CANCELLED`, exit `4`) as proposed in CL-10 and CL-11, or as approved | User (CL-10, CL-11) | S1 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `examples/WeatherParser.Cli/Process/…` | Create | Signal registration, grace period, deadline, cancellation outcome |
| `examples/WeatherParser.Cli/Program.cs` | Change | Token wiring |
| `tests/WeatherParser.Cli.Tests/Process/…` | Create | Unit and process tests |
| `docs/p3/P3_VERIFICATION.md` | Change | P3.09 section |

## 5. Procedure
Every slice: roles and checks as in P3.03 section 5.

### S1 Token, deadline and cleanup
**Red**: `Cancel_DuringRetrieval_ExitCancelled_NoDocument`, `Cancel_DuringSerialization_TemporaryFileDeleted_NoDestination`, `Cancel_DuringPublication_NoSuccessClaim_SummaryLogged`, `Deadline_Exceeded_ExitPerCl11_WithDeadlineId`, `Token_PassedToEveryLibraryCall` (strict mocks assert the linked token), `Cancel_AfterOutputCompleted_StillReportsCompletedOutcome` (a signal after the move of the file does not delete the completed file and does not change a completed outcome).
**Green**: token, deadline and cleanup wiring.

### S2 Signals and process behavior
**Red**: `Process_Sigint_DuringSlowRetrieval_Exit4_NoTemporaryFile` (Linux; the loopback fake server delays its response), `Process_Sigterm_DuringSlowRetrieval_Exit4` (Linux), `Process_SecondSignal_TerminatesImmediately` (Linux), `Process_GracePeriodExpired_Terminates`, `Process_CtrlC_Windows_ViaTestTrigger_Exit4` (Windows), `Process_OneCommandThenExit_NoLingeringProcess` (the child exits and no grandchild remains), `Process_NonInteractive_StdinClosed_Works`, `Process_StdoutAndStderrSeparated_InEveryOutcome`.
**Green**: signal registration and grace period.

**Refactor**: with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P3 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P3-18, T-P3-12, T-P3-09 | REQ-006, REQ-007, REQ-066 | Cancellation, deadline, cleanup |
| S2 | T-P3-18 | REQ-006, REQ-007 | Signals, single execution, channels |
| all | T-P3-20 | REQ-085, REQ-086 | Red evidence; coverage |

## 7. Evidence to Record
- Red names and failures, counts per platform (the Linux-only signal tests are counted from hosted CI and the NanoPi), coverage counters, authorship, Codex rounds

## 8. Risks and Limitations
- (a) Signal tests are timing-sensitive; each waits for a readiness line on stderr before signalling instead of sleeping, and has a bounded timeout
- (b) Windows console-signal behavior differs from POSIX signals; the Windows evidence is the test trigger plus a manual Ctrl+C check recorded in P3.11

## 9. Acceptance Criteria
- [ ] Every listed test green with recorded Red on its platform
- [ ] No success claim and no partial file after cancellation; the process always ends
- [ ] Coverage gate passed; Codex 0 findings or reported
