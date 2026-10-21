# P4.05 – CDC Station Catalog Reader: Implementation Plan

**Status: plan only (2026-10-21); nothing is implemented or verified yet.** Refines the P4.05 row of section 5 and the "CDC catalog" row of section 4 of [PHASE_P4_PLAN.md](../../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 6, 3, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0). Section 9 was lost in a qwen full-file correction, found by the orchestrating agent and restored by qwen (delta review: 0 findings).

## 1. Goal and Scope

Goal: an injectable reader that parses the CDC station catalog in the exact format approved in the CDC-catalog SC row of `docs/p4/P4_SELECTION_CONTRACT.md` from a caller-supplied `Stream` (no network), producing typed in-memory catalog records — station ID as string with leading zeros, name with umlauts preserved, latitude, longitude, altitude, active period fields if the format has them — and applying the approved catalog failure semantics, test-first with small synthetic fixtures.

In scope:
- asynchronous reading with `CancellationToken`
- decoding with the SC encoding (umlauts such as `ä`, `ö`, `ü`, `ß` survive; a wrongly decoded fixture is detected by a test)
- header/blank-line handling as documented
- numeric fields parsed with the invariant culture into `decimal` (C-26), no rounding
- IDs kept as strings exactly as in the source (never converted to numbers, leading zeros kept)
- malformed records (missing fields, non-numeric coordinates or altitude, out-of-range coordinates, empty ID, duplicate IDs) handled per the approved failure semantics (fail with an English diagnostic naming the line number, or skip with a recorded diagnostic — as the SC row decides)
- a stream where no record is valid is a format failure, never a successful empty catalog
- an empty stream (or header only) handled as the SC row decides
- missing altitude representation per the SC row, carried as "no altitude" (null), never 0
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
| Format, encoding and failure semantics | — | As approved in the CDC-catalog and catalog-failure-semantics SC rows; this step only implements them | User (P4.02) | S1–S3 |
| Interface and type names | Proposed `ICdcStationCatalogReader`/`CdcStationCatalogReader` with `ReadAsync(Stream catalog, CancellationToken cancellationToken)` returning a Proposed `CdcStationCatalog` (records plus diagnostics); Proposed record type `CdcStationRecord` (internal model, not an export entity) / names from the approved public-API SC row | The approved SC row wins | User (P4.02) | S1–S3 |
| Stream ownership | reader leaves the caller's stream open / reader disposes it | Leave open (consistent with the P2 serializer stream rule) | Orchestrator, recorded | S1 |
| Line-length bound inside the reader | none (size bound only in P4.08) / a maximum line length from the SC row | Follow the SC row; if it sets none, the download size bound of P4.08 is the only bound and this is recorded as a limitation | User (P4.02) | S3 |
| Folder | `src/WeatherParser.Parsing/Catalogs/` (namespace `WeatherParser.Parsing.Catalogs`) / other | Proposed `Catalogs/` (phase plan section 8) | Orchestrator, recorded | S1 |

## 4. Artifacts

| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Catalogs/ICdcStationCatalogReader.cs` | Create | Proposed interface `ICdcStationCatalogReader` with method `Task<CdcStationCatalog> ReadAsync(Stream catalog, CancellationToken cancellationToken)` |
| `src/WeatherParser.Parsing/Catalogs/CdcStationCatalogReader.cs` | Create | Proposed class `CdcStationCatalogReader` implementing `ICdcStationCatalogReader` |
| `src/WeatherParser.Parsing/Catalogs/CdcStationCatalog.cs` | Create | Proposed record type `CdcStationCatalog` (records plus diagnostics) |
| `src/WeatherParser.Parsing/Catalogs/CdcStationRecord.cs` | Create | Proposed record type `CdcStationRecord` (internal model, not an export entity) |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration in S1 |
| `tests/WeatherParser.Parsing.Tests/Catalogs/CdcStationCatalogReaderTests.cs` | Create | Tests for the reader |
| `tests/WeatherParser.Parsing.Tests/Fixtures/Cdc/*` | Create | Synthetic fixtures (Proposed: embedded or copied to output, decided in S1 and recorded) |
| `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` | Extend | Add test for registration |

## 5. Procedure

### S1 Valid records

**Red**
- `ReadAsync_ValidFixture_ReturnsAllRecords` (three synthetic stations)
- `ReadAsync_KeepsLeadingZeroIds` (`00044`, `01234`)
- `ReadAsync_PreservesUmlautsInNames` (for example `Würzburg`, `Großer Arber` written in the SC encoding)
- `ReadAsync_ParsesCoordinatesAndAltitudeInvariantCulture` (thread culture `de-DE`, restored afterwards; exact `decimal` values)
- `ReadAsync_NegativeAltitude_Kept` (if the SC format allows negative altitudes)
- `ReadAsync_LeavesStreamOpenOnSuccess`
- `AddWeatherParsing_ResolvesCdcStationCatalogReader` (validated container, C-108)
- `ServiceRegistrationCompletenessTests` turns red when the interface appears

Expected failure: stub throws `NotImplementedException`.

**Green**
- Parsing per the CDC-catalog SC format (fields, encoding, invariant-culture `decimal`); registration of `ICdcStationCatalogReader` → `CdcStationCatalogReader` as a singleton in `AddWeatherParsing`.

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
- `ReadAsync_ActivePeriodFields_Parsed` (only if the format has them; date fields as the SC row defines, UTC handling per the SC row)
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
- `ReadAsync_MissingField_FollowsFailureSemantics` (diagnostic names the line number; fail or skip as decided)
- `ReadAsync_NonNumericCoordinate_FollowsFailureSemantics`
- `ReadAsync_OutOfRangeCoordinate_FollowsFailureSemantics`
- `ReadAsync_EmptyId_FollowsFailureSemantics`
- `ReadAsync_DuplicateId_FollowsFailureSemantics`
- `ReadAsync_ValidInvalidValidRecords_FollowsFailureSemantics` (synthetic fixture with header, blank line, valid record, malformed record, valid record: under skip semantics both valid records are returned and the diagnostic names the physical line number of the malformed record; under fail semantics the read fails without a successful partial result)
- `ReadAsync_AllRecordsInvalid_IsFormatFailure` (never an empty successful catalog)
- `ReadAsync_EmptyStream_FollowsSemantics`
- `ReadAsync_WrongEncodingFixture_IsDetected` (a fixture in a different encoding produces a diagnostic or a test-visible mismatch, as the SC row decides)
- `ReadAsync_CancelledBeforeRead_ThrowsOperationCanceled`
- `ReadAsync_CancelledDuringRead_ThrowsOperationCanceled`
- `ReadAsync_NonNumericAltitude_FollowsFailureSemantics`
- `ReadAsync_NumericOverflow_FollowsFailureSemantics`
- `ReadAsync_DisallowedSeparatorOrGrouping_Rejected`
- `ReadAsync_LeavesStreamOpenOnFormatFailureAndCancellation`
- `ReadAsync_LineAtMaximumLength_Accepted` (only if the line-length SC row sets a maximum)
- `ReadAsync_LineAboveMaximumLength_FollowsFailureSemantics` (only if the line-length SC row sets a maximum; otherwise neither test nor enforcement exists and the limitation in section 8 applies)

Expected failure: S1/S2 implementation throws unspecific exceptions or returns partial data; rejection tests that already pass are recorded as existing behavior.

**Green**
- qwen makes the minimal change for each observed S3 failure (failure semantics with line diagnostics, mixed valid/invalid input, all-invalid detection, numeric and separator checks, encoding check, cancellation, stream left open, line-length bound if set); the orchestrating agent reviews the diff and reruns the affected and regression tests; tests that already passed stay recorded as existing behavior.

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
| S2 | T-P4-06 | REQ-023, REQ-102 (by analogy for missing altitude) | Missing altitude null not 0, zero altitude distinct from missing, coordinate bounds, documented header/blank lines, active-period fields if present |
| S3 | T-P4-06 | REQ-024, REQ-083 | Malformed records per the approved failure semantics with line diagnostics; all-invalid input is a format failure; empty stream; encoding mismatch detected; cancellation before and during reading; stream left open on failure and cancellation; numeric overflow, disallowed separators, malformed altitude; line-length bound if set |
| S1 | T-P4-14 (partial) | REQ-016 | Reader resolved through its interface; registration completeness (C-107) |
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
- (e) Line-length bound: if the SC row sets a maximum, the reader enforces it; otherwise, this is recorded as a limitation

## 9. Acceptance Criteria

- [ ] Each behavior increment has recorded Red evidence
- [ ] All T-P4-06 cases pass
- [ ] IDs never numeric
- [ ] Missing altitude never 0; zero altitude kept
- [ ] All-invalid input never yields a successful empty catalog
- [ ] Fixtures synthetic and documented
- [ ] Reader registered, completeness tests green
- [ ] Build without warnings
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user (AGENTS.md)
- [ ] No availability, retrieval or export code added
