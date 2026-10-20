# P2.04 – UTC and Source-Time Normalization: Implementation Plan

**Status: completed (2026-10-20, marked completed by the user after a review without findings).** Implemented test-first on `features/develop/P2`; evidence in [P2_VERIFICATION.md](P2_VERIFICATION.md) section 4; the hosted CI run of draft pull request #3 follows the push. Original planning note: this document started as a plan on 2026-10-20. This plan refines the P2.04 row of section 5, the time paragraph of section 4 and the "Time representation" decision of section 3 of [PHASE_P2_PLAN.md](../../PHASE_P2_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief and corrected by qwen in one review round (S4 expected failures, S7 characterization, evidence location); the orchestrating agent restored one dropped backtick and the backticks of two paths. Extended on 2026-10-20 after the user's review of the P2.04 decisions: DST precedence over the midnight rule (two regression cases), nullable inputs for missing times with valid midnight, and the 5-minute tolerance boundary; text drafted by qwen, inserted by the orchestrating agent.

## 1. Goal and Scope
Goal: an injectable, source-independent time-normalization service that turns already-extracted source time information into unambiguous UTC values with quality markers, following REQ-042, REQ-043 and REQ-051, with midnight and Europe/Berlin DST handling proven by synthetic tests, and whose results serialize in the P2.02 lexical form `YYYY-MM-DDTHH:mm:ss` in both formats.

In scope:
- The normalization service and its interface
- An injectable time-zone resolver
- An immutable result value
- Rejection of ambiguous, nonexistent, sub-second and missing inputs with a defined exception
- DI registration
- Contract-record updates

Out of scope (owner):
- Extracting times from OsthessenWetter pages and page fixtures → P6
- DWD source times → P5
- MOSMIX issue/validity times → P7
- Placing the markers into the observation export graph → P2.06
- Rounding → P2.05
- MQTT → P8

## 2. Preconditions and Inputs
- Prerequisite steps: P2.02 complete (time representation and sub-second decisions approved; wire format implemented)
- Existing repository facts used:
  - `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs`
  - `docs/p2/P2_EXPORT_CONTRACT.md` (to be updated)
  - `tests/WeatherParser.Parsing.Tests/` project structure
- Required user authorization: contract review and approval for service/result names, input model, midnight tolerance, ambiguous time handling, marker literals, and time-zone data failure behavior

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Service and result names | Proposed `ISourceTimeNormalizer`/`SourceTimeNormalizer`, `ITimeZoneResolver`/`TimeZoneResolver`, result `NormalizedSourceTime`, failure `SourceTimeNormalizationException` / other | Proposed names | User (contract review) | S1 |
| Input model | separate methods for complete timestamps and time-of-day inputs / one method with a discriminated input value | Separate methods, so a complete timestamp can never enter the inference path | Orchestrator, recorded | S1, S4 |
| Midnight skew tolerance | none / a fixed tolerance recorded in the contract | Decided: 5 minutes (user decision 2026-10-20, recorded in C-30); S4 tests the boundary (12:05 same day, 12:06 previous day) | User | S4 |
| Ambiguous local time with no offset evidence | reject with defined failure / pick the earlier or later instant | Reject (phase plan: never select a DST interpretation arbitrarily) | User (contract review) | S3 |
| Marker literals | `source`, `estimated_date`, `source_timezone: "Europe/Berlin"` as in REQ-042 / other | REQ-042 literals | User (contract review) | S4 |
| Time-zone data failure | defined `SourceTimeNormalizationException` with an actionable English message / let `TimeZoneNotFoundException` escape | Defined failure | Orchestrator, recorded | S6 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `docs/p2/P2_EXPORT_CONTRACT.md` | Change | Record time normalization decisions from section 3 |
| `docs/p2/P2_VERIFICATION.md` | Change | Add P2.04 evidence for each slice |
| `src/WeatherParser.Parsing/Time/ISourceTimeNormalizer.cs` | Create | Interface for source time normalizer |
| `src/WeatherParser.Parsing/Time/SourceTimeNormalizer.cs` | Create | Implementation of the source time normalizer |
| `src/WeatherParser.Parsing/Time/ITimeZoneResolver.cs` | Create | Interface for time zone resolver |
| `src/WeatherParser.Parsing/Time/TimeZoneResolver.cs` | Create | Implementation of the time zone resolver |
| `src/WeatherParser.Parsing/Time/NormalizedSourceTime.cs` | Create | Immutable result value type |
| `src/WeatherParser.Parsing/Time/SourceTimeNormalizationException.cs` | Create | Custom exception for normalization failures |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Register both time services |
| `tests/WeatherParser.Parsing.Tests/Time/SourceTimeNormalizerTests.cs` | Create | Tests for source time normalizer |
| `tests/WeatherParser.Parsing.Tests/Time/TimeZoneResolverTests.cs` | Create | Tests for time zone resolver |
| `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` | Change | Add DI registration tests |
| `tests/WeatherParser.Parsing.Tests/Export/NormalizedTimeSerializationTests.cs` | Create | Tests for normalized time serialization |

## 5. Procedure
### S0 Contract record
**Action:** Record the section 3 decisions in `docs/p2/P2_EXPORT_CONTRACT.md`; user review before S1.
**Roles:** qwen drafts prose from the orchestrator's decisions; the orchestrator verifies and records the approval.

### S1 Complete timestamps with explicit UTC or offset
**Red:** `NormalizeComplete_WithOffset_ReturnsUtcAndSourceQuality` (2026-10-25 02:30 +02:00 → 00:30:00 UTC; +01:00 → 01:30:00 UTC). Expected failure: the first stub returns `default` values, so the UTC value and quality assertions fail.
**Green:** offset conversion, quality `source`.
**Refactor:** None required for this slice.
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S2 Complete local Europe/Berlin timestamps outside transitions
**Red:** `NormalizeComplete_BerlinLocal_ConvertsWinterAndSummer` (2026-01-15 12:00 → 11:00:00 UTC; 2026-07-15 12:00 → 10:00:00 UTC). Expected failure: the S1 implementation handles only explicit offsets and rejects or mis-converts a zone-only input.
**Green:** conversion through the resolved `Europe/Berlin` zone. The resolver is a mock in `MockRepository` returning the real `TimeZoneInfo` and verified in `[TestCleanup]`.
**Refactor:** None required for this slice.
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S3 DST gap and overlap
**Red:** `NormalizeComplete_NonexistentLocalTime_IsRejected` (2026-03-29 02:30) and `NormalizeComplete_AmbiguousLocalTimeWithoutOffset_IsRejected` (2026-10-25 02:30). Expected failure: `TimeZoneInfo.ConvertTimeToUtc` throws a raw `ArgumentException` for the gap and silently picks one interpretation for the overlap.
**Green:** `IsInvalidTime`/`IsAmbiguousTime` checks with the defined exception.
**Refactor:** None required for this slice.
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S4 Time-of-day date inference
**Red:** `InferDate_AfterLocalMidnight_UsesLocalRetrievalDate` (retrieval 2026-07-15 22:05 UTC = 2026-07-16 00:05 local, display 00:02 → 2026-07-15 22:02:00 UTC), `InferDate_DisplayAfterRetrievalTime_UsesPreviousDay` (same retrieval, display 23:55 → 2026-07-15 21:55:00 UTC), `InferDate_SetsEstimatedDateAndBerlinMarkers`, plus the 5-minute tolerance boundary (C-30). Expected failures: a first stub that takes the UTC date of the retrieval instant places the 00:02 reading one day early; once the local date is used, the 23:55 reading lands one day late until the midnight rule exists; markers are missing. Additional Red tests `InferDate_DisplayAmbiguousOnRetrievalDate_IsRejected` (retrieval 2026-10-25T01:15:00 UTC, display 02:45), `InferDate_RolledOverDateAmbiguous_IsRejected` (retrieval 2026-10-25T23:30:00 UTC, display 02:45) and `InferDate_ToleranceBoundary_FiveMinutes` (retrieval 2026-07-15T10:00:00 UTC, display 12:05 → `2026-07-15T10:05:00`, display 12:06 → `2026-07-14T10:06:00`); expected failure of the first two: the midnight rule moves the reading to an unambiguous previous date instead of rejecting it.
**Green:** local retrieval date, midnight rule, markers; inferred times that fall into the DST gap/overlap follow S3.
**Refactor:** None required for this slice.
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S5 Input guards and no substitution
**Red:** `Normalize_NullInputs_AreRejectedAndNeverUseRetrievalTime` (null passed to each of the three methods), `Normalize_NonUtcRetrievalInstant_IsRejected`, `Normalize_SubSecondInput_IsRejected`; expected failure: guards missing, so no exception is thrown. Characterization (not a Red): `InferDate_DisplayedMidnight_IsValid` (retrieval 2026-07-15T22:05:00 UTC, display 00:00 → `2026-07-15T22:00:00`, `estimated_date`).
**Green:** guards with the defined exception.
**Refactor:** None required for this slice.
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S6 Resolver and time-zone data failure
**Red:** `TimeZoneResolver_ResolvesEuropeBerlin` (real zone) and `Normalize_ResolverThrowsTimeZoneNotFound_RaisesDefinedFailure` (mock throws `TimeZoneNotFoundException`). Expected failure: the raw exception escapes.
**Green:** wrap with an actionable English message naming the zone and the missing time-zone data.
**Refactor:** None required for this slice.
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S7 Serialization of normalized times
**Characterization (not a Red):** `NormalizedTime_SerializesWithoutSuffixInBothFormatsUnderDeDe` places normalized results into the P2.02 slice graph and asserts `YYYY-MM-DDTHH:mm:ss` without `Z`. Expected: passes immediately if P2.02 is correct — record it as a characterization test, not a Red; a failure is treated as a bug with a regression test first.
**Green:** None required.
**Refactor:** None required.
**Roles:** qwen generates the test; the orchestrator runs it, records that it passed immediately (or, on failure, starts a regression test before any fix) and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S8 DI registration
**Red:** `AddWeatherParsing_RegistersTimeServices` fails because the registrations are missing.
**Green:** Register both time services in `ServiceCollectionExtensions`.
**Refactor:** None required for this slice.
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

## 6. Tests and Test IDs
| Step-local case | T-P2 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1, S2 | T-P2-06 | REQ-043, REQ-051 | Complete timestamps converted to UTC with quality `source`; winter/summer offsets |
| S3 | T-P2-07 | REQ-042–043, REQ-082 | Nonexistent and ambiguous local times rejected without evidence; explicit offsets resolve the overlap |
| S4 | T-P2-06, T-P2-07 | REQ-042, REQ-056 | Retrieval-date inference in Europe/Berlin, midnight rule, `estimated_date` and `source_timezone` markers |
| S5 | T-P2-06 | REQ-043, REQ-051 | Non-UTC retrieval instant, sub-second values and missing times rejected; retrieval time never substituted |
| S6 | T-P2-07 | REQ-005, REQ-071 | Real Europe/Berlin resolution; missing time-zone data yields the defined failure |
| S7 | T-P2-05 | REQ-051, REQ-067 | Normalized values serialize as `YYYY-MM-DDTHH:mm:ss` without suffix in both formats under `de-DE` (partial – completed in P2.08) |
| S8 | T-P2-19 | REQ-070–071, REQ-078 | Services resolved through their interfaces (partial – completed in P2.10) |

## 7. Evidence to Record
- All entries go to `docs/p2/P2_VERIFICATION.md` per slice, except the time decisions and their approval date, which go to `docs/p2/P2_EXPORT_CONTRACT.md`.
- Red names with observed failure messages (S7: characterization)
- Green/Refactor results
- Commands used for build and test runs
- Test counts per run
- Authorship per slice (qwen/orchestrator)
- Time decisions and approval date in `docs/p2/P2_EXPORT_CONTRACT.md`
- The time-zone data source on the executing machine (Windows ICU or Linux tzdata version) recorded with each run

## 8. Risks and Limitations
- (a) Time-zone data differs between hosts — the 2026 transitions are long-established rules, but tzdata/ICU versions are recorded
- (b) A Debian host or container without `tzdata` fails — defined failure, and P2.11 verifies on ARM64
- (c) The midnight tolerance is decided (5 minutes, user decision 2026-10-20, C-30); C-28 to C-35 were approved by the user on 2026-10-20 (S0)
- (d) Synthetic tests do not prove any real source's time format — that is P5–P7 work

## 9. Acceptance Criteria
- [x] S0 approval recorded
- [x] Red failures recorded for S1–S6 and S8
- [x] S7 recorded as characterization
- [x] Every example value from the behavior facts asserted
- [x] No use of `DateTime.Now`, `DateTime.Today` or local system time in the service
- [x] A date rollover never hides a DST gap or overlap (both regression cases asserted)
- [x] `null` inputs are rejected while displayed midnight `00:00` stays valid
- [x] All tests pass
