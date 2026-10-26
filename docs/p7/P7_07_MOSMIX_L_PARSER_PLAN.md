# P7.07 – MOSMIX_L Parser Service: Implementation Plan

**Status: planned (2026-10-26); reconciled on 2026-10-26 with the approved [P7 forecast contract](P7_FORECAST_CONTRACT.md) (P7.02 S6); not started.** Refines the P7.07 row of section 5 and the "Input", "Times", "Run" and "Logging" rows of section 4 of [PHASE_P7_PLAN.md](../../PHASE_P7_PLAN.md).

## 1. Goal and Scope
Goal: the public MOSMIX_L parser that validates the MOSMIX ID, retrieves the location's latest file once under one deadline, reads, maps and builds one `forecast_run`, determines its freshness, and logs every failure, notice and omission with stable IDs — following the P5.07 and P6.07 parser pattern.
In scope: `IMosmixLParser`/`MosmixLParser` with `ParseAsync(string mosmixId, CancellationToken)` and the result of MF-02; ID validation before any request or timer (MF-03); one deadline per parse from `MosmixLForecastOptions.ParseDeadline` created from the injected `TimeProvider`; precedence caller cancellation, deadline, every other failure at every checkpoint (after retrieval, after reading, after mapping, after building); `retrieved_at` sampled once after the response has arrived and been read, truncated to whole seconds (OW-16 pattern, MF-13); freshness against `MaxRunAge` (MF-09) with `MOSMIX-RUN-STALE`; the builder's omissions logged and returned, and carried by the rejection (P7.03 S1); `NotExportedElements` returned, logged as `MOSMIX-ELEMENT-NOT-EXPORTED` and carried by the rejection; `MOSMIX-ELEMENT-ABSENT` notices; `run_age_exceeded` always set; logging through `ILogger<MosmixLParser>`; registration.
Out of scope with owner: MOSMIX_S → P7.08; wire form → P7.09; README and package → P7.10; CLI `--ParseMOSMIX --Product MOSMIX_L` → P3; MQTT → P8.

## 2. Preconditions and Inputs
- P7.03 to P7.06 complete; P7.02 approved MF-02, MF-03, MF-09, MF-14 and MF-15 on 2026-10-26.
- Existing: `IMosmixLForecastSource`, `IMosmixForecastDocumentReader`, `IMosmixElementMapper`, `IForecastRunBuilder`, `ForecastRunRejectedException.OmittedValues`, the `OsthessenWetterParser` and `DwdObservationParser` patterns and their tests, `CapturingLogger<T>` and the manual time provider used in the P6 tests.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Result type (MF-02) | Approved: `MosmixParseResult` with `Run`, `OmittedValues`, `NotExportedElements`, `AbsentElements`, `Freshness` (`MosmixRunFreshness.Fresh` or `Stale`) and `RunAge` | A library type, not an export entity; shared by both parsers only as a data type, no behavior | User (MF-02, approved) | S2 |
| Sampling point of `retrieved_at` | After the source returns and the body is read / before the request | After the body is read, once, so the age compares the issue time with a retrieval that has completed; the deadline still starts before the request | Orchestrator, recorded (OW-16 precedent) | S2 |
| Freshness boundary | Approved: age = `retrieved_at − issued_at`; at the limit fresh, above stale; the run carries `run_age_exceeded` `true` or `false` | As stated (T-P7-13); a negative age (issue time after `retrieved_at`, clock skew) is at most the limit, so the run is `Fresh` with the negative `RunAge` returned unchanged and no failure (orchestrator reading of MF-09, recorded) | User (MF-09, approved) | S2 |
| Log content | Approved (MF-15): product, MOSMIX ID, IDs, kind and reason, UTC, sanitized endpoint, unmapped element names; never values, file text or archive member names | As approved | User (MF-15, approved) | S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Forecasts/Mosmix/IMosmixLParser.cs`, `MosmixLParser.cs`, `MosmixParseResult.cs`, `MosmixRunFreshness.cs`, `MosmixNoticeIds.cs` | Create | Parser, result, freshness, IDs |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration |
| `tests/WeatherParser.Parsing.Tests/Forecasts/Mosmix/MosmixLParserTests.cs` | Create | Unit tests with strict mocks of the P7.04 to P7.06 interfaces |
| `tests/WeatherParser.Parsing.Tests/Forecasts/Mosmix/ComposedMosmixLPipelineTests.cs` | Create | Composed tests with only the HTTP handler mocked and synthetic KMZ files |
| `docs/p7/P7_VERIFICATION.md` | Change | P7.07 section |

## 5. Procedure
Every slice: roles and checks as in P7.03 section 5.

### S1 Validation, deadline and precedence
**Red**: `ParseAsync_InvalidId_ThrowsBeforeRequestAndTimer` (empty, whitespace, embedded whitespace, `:`, over-long, grammar violation; the strict source mock has no setup and the time provider creates no timer), `ParseAsync_ValidIdsKeptExactly` (leading zero, letters), `ParseAsync_DeadlineFromOptionsAndTimeProvider`, `ParseAsync_CallerCancelledDuringRetrieval_ThrowsOperationCanceled`, `ParseAsync_DeadlineDuringRetrieval_ThrowsDeadlineExceeded`, `ParseAsync_BothAtOnce_CallerCancellationWins`, `ParseAsync_DeadlineAfterRead_BeforeMapping_ThrowsDeadlineExceeded`, `ParseAsync_DeadlineDuringBuild_WinsOverBuilderRejection`, `ParseAsync_CancellationAndDeadlineDuringBuild_CancellationWins`, `ParseAsync_DeadlineTimerDisposedOnEveryPath`, `ParseAsync_RetrievalFailures_Propagate` (unknown ID, retrieval failed, too large; no fallback to MOSMIX_S), `ParseAsync_FormatFailures_Propagate`, `ParseAsync_Composed_BodyStalledAfterHeaders_DeadlineExceeded`, `AddWeatherParsing_ResolvesMosmixLParser`; `ServiceRegistrationCompletenessTests` red while unregistered.
**Green**: validation, deadline and precedence; singleton registration; S1 green before S2.

### S2 Run, times and freshness
**Red**: `ParseAsync_ValidFile_RunWithProductSourceAndLocation` (`MOSMIX_L`, `dwd-mosmix-l:<id>`, name and coordinates from the file), `ParseAsync_RetrievedAtSampledAfterRead` (once, whole seconds), `ParseAsync_IssuedAtFromFile_ValidAtPerStep_NoMeasuredAt`, `ParseAsync_AgeAtLimit_Fresh`, `ParseAsync_AgeOneSecondAbove_StaleWithMarkerAndNotice`, `ParseAsync_IssueTimeAfterRetrieval_FreshWithNegativeAge` (clock skew: `Fresh`, `RunAge` negative, `run_age_exceeded` `false`, no crash), `ParseAsync_FreshRun_MarkerFalse`, `ParseAsync_OmissionsReturned`, `ParseAsync_NotExportedElements_ReturnedByName` (a file with probability and code elements: each name listed once, in file order), `ParseAsync_NoUsableValue_ExceptionCarriesNotExportedElements`, `ParseAsync_NoUsableValue_ThrowsWithOmissions` (`MOSMIX-NO-USABLE-VALUE` with `Product` `MosmixL`, carrying the builder's omissions), `ParseAsync_FormatFailure_MappedWithProduct` (the reader's internal format exception becomes `MosmixForecastException` with its ID and `Product` `MosmixL`), `ParseAsync_AbsentElements_NoticesAndRunFromRest`, `ParseAsync_OneRunOneRequest` (composed request count 1).
**Green**: run assembly and freshness.

### S3 Diagnostics
**Red**: `ParseAsync_Failures_LoggedWithIdUtcProductId`, `ParseAsync_Notices_Logged` (stale, element absent), `ParseAsync_Omissions_LoggedWithTimeKindReason`, `ParseAsync_NotExportedElements_LoggedOncePerElement` (`MOSMIX-ELEMENT-NOT-EXPORTED` with the element name, no values), `ParseAsync_OmissionsLoggedBeforeRejection`, `ParseAsync_Logs_NoValuesNoFileTextNoQuery` (an endpoint with a query; a file whose `kml:description` and an unused element contain host and user names; no series values in any log or message), `ParseAsync_MemberName_NeverInLogsOrMessages` (an archive whose `.kml` member name carries a distinctive marker text, for a successful parse and for a format failure: the marker appears in no log entry and no exception message).
**Green**: logging.

**Refactor**: remove duplication with all tests green; shared parser helpers with P7.08 only as internal components without a product switch.

## 6. Tests and Test IDs
| Step-local case | T-P7 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P7-01, T-P7-03 (parser level) | REQ-103, REQ-063 | Validation first, one deadline, precedence, no fallback |
| S2 | T-P7-11, T-P7-12, T-P7-13, T-P7-14 | REQ-104, REQ-105, REQ-107, REQ-110 | Run, times, freshness, rejection |
| S3 | T-P7-17 | REQ-072, REQ-082 | Diagnostics content and sanitization |
| all | T-P7-18 (registration), T-P7-20 | REQ-078, REQ-085, REQ-086 | Registration; Red evidence; coverage |

## 7. Evidence to Record
- Red names and failures, counts, coverage, authorship, Codex rounds; the composed tests' request counts

## 8. Risks and Limitations
- (a) Each parse requests one location file; parsing many locations repeats requests (external scheduling guidance in README, MF-16)
- (b) `MOSMIX_L_LATEST` may still name the previous run shortly after a run time; freshness marks it, the parser never waits or retries

## 9. Acceptance Criteria
- [ ] Every listed new-behavior test green with recorded Red; characterization tests (refactoring guards and existing behavior) green before and after and recorded as characterization, never with a manufactured Red; composed test with exactly one request
- [ ] Never `measured_at`; never a run from another product; stale runs marked
- [ ] Logs carry IDs and context and no values, file text or query strings
- [ ] Registration and architecture tests green; coverage gate passed; Codex 0 findings or reported
