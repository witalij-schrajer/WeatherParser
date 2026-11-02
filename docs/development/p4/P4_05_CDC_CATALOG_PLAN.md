# P4.05 – CDC Station Catalog Reader: Implementation Plan

**Status: complete (2026-10-22): implemented (S1 to S4), committed as `0195b4f`, hosted CI run 39828777889 green; records in [P4_VERIFICATION.md](../../p4/P4_VERIFICATION.md) section 5.** Original planning note: plan only on 2026-10-21. Refines the P4.05 row of section 5 and the "CDC catalog" row of section 4 of [PHASE_P4_PLAN.md](../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 6, 3, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0). Section 9 was lost in a qwen full-file correction, found by the orchestrating agent and restored by qwen (delta review: 0 findings). Reconciled with the approved selection contract (SC-02, SC-10, SC-14, SC-17) on 2026-10-21 (P4.02 S6). S6 record (2026-10-21): qwen reconciled this plan from the orchestrating agent's change list and fixed the confirmed review findings section by section; the orchestrating agent edited these parts itself after failed qwen attempts, for regressions or for recorded design decisions: S1 Red list; S3 error IDs and failure-semantics paragraph; goal and scope bullets; section 3/4 reader signature with `endpointName` (orchestrator decision); fixture exemptions; section 6 and 9 rows; altitude diagnostic moved to P4.06 (cross-plan); `CatalogFormatException` and `CatalogDiagnostic` creation (cross-plan). Codex five-angle review rounds: 3, 4, 5, 3, 2, 0 findings; cross-plan five-angle review of all P4 plans: rounds 4, 3, 1, 1, 0 findings.

## 1. Goal and Scope

Goal: an injectable reader that parses one CDC `now` station list in the SC-02 format from a caller-supplied `Stream` (no network), producing typed in-memory catalog records — station ID as string with leading zeros, name with umlauts preserved, latitude, longitude, altitude, from-date and to-date (SC-02) — and applying the SC-10 failure semantics, test-first with small synthetic fixtures.

In scope:
- asynchronous reading with `CancellationToken`
- decoding with the SC encoding (umlauts such as `ä`, `ö`, `ü`, `ß` survive; a wrongly decoded fixture is detected by a test)
- header/blank-line handling as documented
- numeric fields parsed with the invariant culture into `decimal` (C-26), no rounding
- IDs kept as strings exactly as in the source (never converted to numbers, leading zeros kept)
- malformed records (missing fields, non-numeric coordinates or altitude, out-of-range values, invalid IDs or dates, empty names) skipped with a `CATALOG-RECORD-SKIPPED` diagnostic naming the endpoint, the physical line number and the reason; duplicate IDs exclude all their records (SC-10)
- a stream where no record is valid is a format failure, never a successful empty catalog
- an empty stream, a header-only stream or a stream without any syntactically valid record → `CatalogFormatException` `CATALOG-FORMAT-INVALID` (SC-10)
- missing altitude representation of SC-02, carried as "no altitude" (null), never 0
- synthetic fixtures under `tests/WeatherParser.Parsing.Tests/Fixtures/Cdc/` built from the verified format, with no copied DWD rows (REQ-083)
- registration per slice

Out of scope with owner:
- availability per station (`available_measurements`, inactive stations) → P4.06
- HTTP retrieval, size limits of downloads and endpoints → P4.08
- filtering and distance → P4.04/P4.09
- list assembly and export → P4.09
- logging of notices → P4.09 (this step returns diagnostics in its result; it does not log)
- MOSMIX catalog → P4.07

## 2. Preconditions and Inputs

- P4.02 approved the CDC-catalog-format, catalog-failure-semantics, public-API and service-registration SC rows
- The SC row lists the documented fields and example layout from which fixtures are built

## 3. Decisions Required

| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Format, encoding and failure semantics | — | As approved in the CDC-catalog and catalog-failure-semantics SC rows; this step only implements them (SC-02, SC-10) | User (P4.02, approved 2026-10-21) | S1–S3 |
| Interface and type names | — | `ICdcStationCatalogReader`/`CdcStationCatalogReader` (SC-11) with `ReadAsync(Stream catalog, string endpointName, CancellationToken cancellationToken)` returning `CdcStationCatalog` (records plus diagnostics); `CdcStationRecord` is an internal model, not an export entity | Orchestrator, recorded (SC-11 approves the interfaces as proposed in this plan) | S1 |
| Stream ownership | reader leaves the caller's stream open / reader disposes it | Leave open (consistent with the P2 serializer stream rule) | Orchestrator, recorded | S1 |
| Folder | `src/WeatherParser.Parsing/Catalogs/` (namespace `WeatherParser.Parsing.Catalogs`) / other | Proposed `Catalogs/` (phase plan section 8) | Orchestrator, recorded | S1 |
| Reader API endpoint context | — | The caller passes the configured endpoint name as `endpointName`; the reader uses it only in diagnostics (SC-10 requires the endpoint name in `CATALOG-RECORD-SKIPPED`), never for retrieval | Orchestrator, recorded | S1, S3 |

## 4. Artifacts

| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Catalogs/ICdcStationCatalogReader.cs` | Create | Interface `ICdcStationCatalogReader` with `Task<CdcStationCatalog> ReadAsync(Stream catalog, string endpointName, CancellationToken cancellationToken)` |
| `src/WeatherParser.Parsing/Catalogs/CdcStationCatalogReader.cs` | Create | Proposed class `CdcStationCatalogReader` implementing `ICdcStationCatalogReader` |
| `src/WeatherParser.Parsing/Catalogs/CdcStationCatalog.cs` | Create | Proposed record type `CdcStationCatalog` (records plus `CatalogDiagnostic` items) |
| `src/WeatherParser.Parsing/Catalogs/CatalogDiagnostic.cs` | Create | Shared diagnostic record (ID such as `CATALOG-RECORD-SKIPPED`, endpoint name, physical line number, station or location ID when known, reason); reused by P4.06 to P4.09 |
| `src/WeatherParser.Parsing/Exceptions/CatalogFormatException.cs` | Create | Public exception with read-only `ErrorId` (SC-17: `CATALOG-FORMAT-INVALID`, later also `CDC-EVIDENCE-INVALID` in P4.06 and `MOSMIX-EVIDENCE-INVALID` in P4.07); English message without input values |
| `src/WeatherParser.Parsing/Catalogs/CdcStationRecord.cs` | Create | Proposed record type `CdcStationRecord` (internal model, not an export entity) |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration in S1 |
| `tests/WeatherParser.Parsing.Tests/Catalogs/CdcStationCatalogReaderTests.cs` | Create | Tests for the reader |
| `tests/WeatherParser.Parsing.Tests/Fixtures/Cdc/*` | Create | Synthetic fixtures (SC-14: verified layout, encoding and line endings, invented IDs and names with umlauts, at most 50 lines each) |
| `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` | Extend | Add test for registration |

## 5. Procedure

### S1 Valid records

**Red**
- `ReadAsync_ValidFixture_ReturnsAllRecords` (three synthetic stations)
- `ReadAsync_KeepsLeadingZeroIds` (`00044`, `01234`)
- `ReadAsync_PreservesUmlautsInNames` (for example `Würzburg`, `Großer Arber` written in the SC encoding)
- `ReadAsync_ParsesCoordinatesAndAltitudeInvariantCulture` (thread culture `de-DE`, restored afterwards; exact `decimal` values)
- `ReadAsync_LeavesStreamOpenOnSuccess`
- `ReadAsync_InvalidHeaderToken_IsFormatFailure` (a header token missing, extra or replaced; the nine SC-02 tokens in order are required; `CatalogFormatException` `CATALOG-FORMAT-INVALID`)
- `ReadAsync_InvalidDashLine_IsFormatFailure` (line 2 not only dashes and spaces; `CATALOG-FORMAT-INVALID`)
- `ReadAsync_InvalidDate_Skipped` (a date that is not a valid `yyyyMMdd` calendar date; record skipped with `CATALOG-RECORD-SKIPPED`)
- `ReadAsync_InvalidAltitudeFormat_Skipped` (altitude not matching `-?\d+`; `CATALOG-RECORD-SKIPPED`)
- `ReadAsync_InvalidCoordinateFormat_Skipped` (latitude or longitude not matching `-?\d+\.\d+`; `CATALOG-RECORD-SKIPPED`)
- `ReadAsync_NonDigitId_Skipped` (for example `A1234`; `CATALOG-RECORD-SKIPPED`)
- `ReadAsync_FourOrSixDigitId_Skipped` (`CATALOG-RECORD-SKIPPED`)
- `ReadAsync_EmptyStationName_Skipped` (`CATALOG-RECORD-SKIPPED`)
- `ReadAsync_EmptyAltitude_NullNotZero` (record kept with no altitude, never 0; the reader returns no notice for it — the single `CDC-STATION-NO-ALTITUDE` notice per station is produced in P4.06 across the four lists, SC-02)
- `ReadAsync_MixedValidInvalidCatalog_SkipsInvalidAndReturnsValid` (valid records returned, each invalid record skipped with a `CATALOG-RECORD-SKIPPED` diagnostic; no exception)
- `AddWeatherParsing_ResolvesCdcStationCatalogReader` (validated container, C-108)
- `ServiceRegistrationCompletenessTests` red when `ICdcStationCatalogReader` appears, before registration

Cross-list conflicts are P4.06 work; selection notices and `SELECTION-EMPTY` are P4.09 work.

Expected failure: behavior tests fail with `NotImplementedException` from the stub; the completeness test fails with its missing-registration assertion.

**Green**
- Parsing per the CDC-catalog SC format (fields, encoding, invariant-culture `decimal`); registration of `ICdcStationCatalogReader` → `CdcStationCatalogReader` as a singleton in `AddWeatherParsing`.
- Ordinary valid-format fixture lines are padded to 1000 characters with trailing spaces, as in the real lists; deliberate boundary inputs (the 2048/2049-character lines) and malformed inputs are exempt
- Fixtures use ISO-8859-1 encoding and CRLF line endings (SC-02, SC-14), except the deliberate UTF-8 fixture of `ReadAsync_WrongEncodingFixture_IsDetected`
- Five-digit station IDs (positions 0–4) must be numeric, trimmed; empty or non-digit IDs rejected.
- Station name (position 61–100) must not be empty, trimmed.
- Altitude (position 23–38) is null if blank, otherwise parsed as `int` in range −500 to 5000.
- Latitude (position 39–50) and longitude (position 51–60) are parsed as invariant `decimal` in ranges −90..90 and −180..180.

**Refactor**
- Remove duplication with all tests green.

**Roles**
- qwen generates the fixtures from the orchestrator's format description, the tests first, then the minimal implementation and refactoring
- The orchestrating agent writes the briefs with the SC format facts and real declarations, checks every fixture against the SC format and that it contains no copied DWD rows, runs each Red, build, tests and coverage, reviews the diff and records authorship

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S2 Missing and boundary values

**Red**
- `ReadAsync_MissingAltitude_IsNullNotZero` (per the SC representation)
- `ReadAsync_ZeroAltitude_IsZeroNotMissing`
- `ReadAsync_CoordinatesAtBounds_Accepted` (latitude ±90, longitude ±180 if representable)
- `ReadAsync_ActivePeriodFields_Parsed` (date fields as SC-02 defines: `von_datum` at characters 6–13 and `bis_datum` at 15–22 (`yyyyMMdd`, valid calendar dates); UTC handling of SC-03)
- `ReadAsync_HeaderAndBlankLines_HandledAsDocumented`

Expected failure: S1 implementation mis-handles these (for example parses a missing altitude as 0); tests that already pass are recorded as existing behavior.

**Green**
- qwen makes the minimal change for each observed S2 failure (nullable altitude, zero altitude kept, bounds, header and blank-line handling, active-period fields); the orchestrating agent reviews the diff and reruns the affected and regression tests; tests that already passed stay recorded as existing behavior.

**Refactor**
- Remove duplication with all tests green.

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S3 Malformed input and failure semantics

**Red**
- `ReadAsync_MissingField_SkippedWithDiagnostic` (`CATALOG-RECORD-SKIPPED` diagnostic with endpoint name, physical line number and reason)
- `ReadAsync_NonNumericCoordinate_SkippedWithDiagnostic`
- `ReadAsync_OutOfRangeCoordinate_SkippedWithDiagnostic`
- `ReadAsync_EmptyId_SkippedWithDiagnostic`
- `ReadAsync_DuplicateId_ExcludesAllRecordsWithDiagnostic`
- `ReadAsync_ValidInvalidValidRecords_SkippedWithDiagnostic` (synthetic fixture with header, blank line, valid record, malformed record, valid record: both valid records are returned and one `CATALOG-RECORD-SKIPPED` diagnostic names the endpoint, the physical line number of the malformed record and the reason; no exception, SC-10)
- `ReadAsync_AllRecordsInvalid_IsFormatFailure` (`CatalogFormatException` `CATALOG-FORMAT-INVALID`; never an empty successful catalog)
- `ReadAsync_EmptyStream_IsFormatFailure` (`CATALOG-FORMAT-INVALID`)
- `ReadAsync_WrongEncodingFixture_IsDetected` (SC-02 fixes ISO-8859-1: a UTF-8 fixture with umlauts yields names that differ from the expected ones, which the test asserts; no encoding detection)
- `ReadAsync_CancelledBeforeRead_ThrowsOperationCanceled`
- `ReadAsync_CancelledDuringRead_ThrowsOperationCanceled`
- `ReadAsync_NonNumericAltitude_SkippedWithDiagnostic`
- `ReadAsync_NumericOverflow_SkippedWithDiagnostic`
- `ReadAsync_DisallowedSeparatorOrGrouping_Rejected`
- `ReadAsync_LeavesStreamOpenOnFormatFailureAndCancellation`
- `ReadAsync_LineAt2048Characters_Accepted`
- `ReadAsync_LineOf2049Characters_IsFormatFailure` (`CATALOG-FORMAT-INVALID`)
- `ReadAsync_HeaderTokensReordered_IsFormatFailure` (`CATALOG-FORMAT-INVALID`)
- `ReadAsync_AltitudeOutOfRange_Skipped` (`-501`, `5001`; `-500` and `5000` accepted)
- `ReadAsync_InvalidCalendarDate_Skipped`
- `ReadAsync_EmptyAltitudeInMixedFixture_KeptAsNull` (no skip and no diagnostic for an empty altitude)
- `ReadAsync_AllRecordsExcludedButValid_IsNotFormatFailure` (every record part of a duplicate pair: empty result with `CATALOG-RECORD-SKIPPED` diagnostics, no exception)
- `ReadAsync_InvalidHeaderTokens_MissingToken_IsFormatFailure`
- `ReadAsync_InvalidHeaderTokens_ExtraToken_IsFormatFailure`
- `ReadAsync_InvalidHeaderTokens_WrongTokenValue_IsFormatFailure`
- `ReadAsync_InvalidDashLine_IsFormatFailure`
- `ReadAsync_InvalidAltitudeField_GrammarMismatch_SkippedWithDiagnostic`
- `ReadAsync_InvalidLatitudeField_GrammarMismatch_SkippedWithDiagnostic`
- `ReadAsync_InvalidLongitudeField_GrammarMismatch_SkippedWithDiagnostic`
- `ReadAsync_InvalidFromToDateFields_GrammarMismatch_SkippedWithDiagnostic`
- `ReadAsync_NonFiveDigitId_SkippedWithDiagnostic` (SC-02: station ID must be exactly 5 digits; a record with `A1234` or `1234` is rejected)
- `ReadAsync_EmptyName_SkippedWithDiagnostic` (SC-02: name field must be non-empty; a record with blank name is rejected)

**Green**
- qwen makes the minimal change for each observed S3 failure (failure semantics with line diagnostics, mixed valid/invalid input, all-invalid detection, numeric and separator checks, encoding check, cancellation, stream left open, unconditional line-length enforcement, format validation of header tokens, altitude range, date validity, empty altitude handling, field grammar checks, five-digit ID and non-empty name rules); the orchestrating agent reviews the diff and reruns the affected and regression tests; tests that already passed stay recorded as existing behavior.

**Refactor**
- Remove duplication with all tests green.

**Roles**
- Same as S1

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S4 Review and evidence

**Red**
- No Red: review slice.

Pre-commit coverage (working tree): after build and all tests pass, run the CONTRIBUTING.md section 5 commands on the working tree: collect "Code Coverage" with `coverage.runsettings` for `tests/WeatherParser.Parsing.Tests`, convert with `dotnet tool run dotnet-coverage merge … -f xml`, write the changed-file list from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard` (uncommitted and untracked step files included), create the baseline with the runner's `write-baseline` from a `coverage.xml` of the integration target collected the same way in a separate `git worktree` checkout of that commit, and run the runner's `evaluate`; record native covered/total block counters of `WeatherParser.Parsing.dll`, the baseline counters and the runner result. Runner exit 0 is required before the Codex review. `pwsh ./scripts/ci-local.ps1 -Commit <commit-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings` is a separate check that runs only after a user-requested commit, on that committed revision, followed by the hosted CI run of the push.

Then Codex reviews the uncommitted diff read-only; qwen fixes confirmed findings test-first and the orchestrating agent reruns build, tests and the pre-commit coverage; checkpoint commit when the user requests it; push to the draft pull request and record the hosted CI run.

**Green**
- Build, all tests, pre-commit coverage with the changed-file list from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard`, native block counters, baseline and gate result

## 6. Tests and Test IDs

| Step-local case | T-P4 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P4-06 | REQ-023, REQ-083 | Valid records, leading-zero IDs as strings, umlauts, invariant-culture `decimal` values, stream left open |
| S2 | T-P4-06 | REQ-023, REQ-102 (by analogy for missing altitude) | Missing altitude null not 0 (the station notice follows in P4.06), zero altitude distinct from missing, coordinate bounds, header and dash lines, from-date and to-date (SC-02) |
| S3 | T-P4-06 | REQ-024, REQ-083 | Malformed records per the approved failure semantics with line diagnostics; all-invalid input is a format failure; empty stream; encoding mismatch detected; cancellation before and during reading; stream left open on failure and cancellation; numeric overflow, disallowed separators, malformed altitude; unconditional line-length enforcement; header token reordering; altitude out-of-range; invalid calendar date; empty altitude → null without a reader diagnostic (the station-level `CDC-STATION-NO-ALTITUDE` diagnostic is P4.06 work) |
| S1 | T-P4-14 (partial) | REQ-016 | Reader resolved through its interface; registration completeness (C-107); **AddWeatherParsing_ResolvesCdcStationCatalogReader** (Red: fails to resolve, expected `InvalidOperationException`) |
| S1–S3 | T-P4-15 | REQ-085 | Recorded Red → Green → Refactor evidence |

## 7. Evidence to Record

- Fixture provenance and the SC row each fixture follows
- Red names with observed failures
- Existing-behavior records
- Commands
- Test counts
- Native block counters and gate result
- Codex review and dispositions
- Authorship

## 8. Risks and Limitations

- (a) The real catalog differs from the documented format (extra columns, changed widths) — the reader follows the SC row; P4.11's separately enabled live check compares one real download
- (b) Encoding mistakes corrupt umlauts silently — the encoding test fails on corruption
- (c) Skipping malformed lines can hide a format change — the all-invalid rule and the diagnostics count make it visible, and P4.09 decides how diagnostics reach the log
- (d) Large catalogs and memory — streaming line by line; the download bound is P4.08's
- (e) Line-length bound: unconditional enforcement per SC-02

## 9. Acceptance Criteria

- [x] Each behavior increment has recorded Red evidence
- [x] All T-P4-06 cases pass
- [x] IDs never numeric
- [x] Missing altitude never 0; zero altitude kept
- [x] All-invalid input never yields a successful empty catalog
- [x] Fixtures synthetic and documented
- [x] Reader registered, completeness tests green
- [x] Build without warnings
- [x] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user (AGENTS.md)
- [x] No availability, retrieval or export code added
- [x] Catalog reader handles one list; cross-list conflicts are not handled by this reader (SC-10)
- [x] Malformed records are skipped with `CATALOG-RECORD-SKIPPED` notice (SC-10)
- [x] Format failures (`CATALOG-FORMAT-INVALID`) are reported for header/dash line mismatch, empty stream, line longer than 2048 characters, or no syntactically valid record (SC-10)
- [x] Duplicate IDs within one file exclude all records with `CATALOG-RECORD-SKIPPED` notice (SC-10)
- [x] An empty altitude yields a record with no altitude (never 0) and no reader diagnostic; `CatalogFormatException` and `CatalogDiagnostic` created here for reuse by later steps
- [x] If every syntactically valid record is excluded (for example all part of duplicate pairs), the reader returns an empty result with `CATALOG-RECORD-SKIPPED` diagnostics and no exception (SC-10); the selection outcome is P4.09 work
- [x] A format failure of the list surfaces as `CatalogFormatException` `CATALOG-FORMAT-INVALID`; how the selection reacts is P4.09 work (SC-10)
