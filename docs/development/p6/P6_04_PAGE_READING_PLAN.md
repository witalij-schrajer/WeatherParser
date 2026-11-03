# P6.04 – Page Reading: Implementation Plan

**Status: executed on 2026-10-25 ([record](../../p6/P6_VERIFICATION.md) section 4): reader and AngleSharp dependency, OW-11 amendment of 2026-10-25 implemented test-first, coverage gate passed (7784/7787), Codex round 3 with 0 findings.** Refines the P6.04 row of section 5 and the "Page reading" row of section 4 of [PHASE_P6_PLAN.md](../PHASE_P6_PLAN.md). Reconciled on 2026-10-25 with the OsthessenWetter contract approved by the user on 2026-10-25 ([contract](../../p6/P6_OSTHESSEN_CONTRACT.md), option A in every row): OW-02 AngleSharp 1.8.5; A7 compound temperature cell added to S2 and the fixtures (contract section 6.1); OW-01, OW-05, OW-11 and OW-12 as planned.

## 1. Goal and Scope
Goal: an injectable reader that turns the page markup of the documented revision into raw station rows of the overview and the wind table, joins the wind rows to the overview rows, and reports format deviations — without interpreting values, units or times.
In scope: the HTML parser package of OW-02 (pinned, lock files, `DEPENDENCIES.md`); parsing with scripts, styles and resource loading disabled; locating both tables by their labels; header-label check; per row: status title, name text, time text, cell texts with their unit suffix; the join key and join of OW-05; failures `OSTHESSEN-FORMAT-CHANGED`; notices `OSTHESSEN-WIND-TABLE-MISSING` and `OSTHESSEN-WIND-ROW-UNMATCHED` as diagnostics; synthetic fixtures; registration.
Out of scope with owner: number, unit and time interpretation → P6.05; retrieval → P6.06; logging the diagnostics → P6.07.

## 2. Preconditions and Inputs
- P6.02 approved OW-01, OW-02, OW-05, OW-11 and OW-12 and recorded the structural assumptions and the package evaluation.
- Existing types: `CatalogDiagnostic` (diagnostic pattern), `CatalogFormatException` (format failure pattern).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Interface and types (Proposed) | `IOsthessenWetterPageReader` with `Task<OsthessenWetterPage> ReadAsync(Stream html, CancellationToken)`; `OsthessenWetterPage` (`Stations` as `OsthessenWetterStationRow` in overview order, `Diagnostics`); row: `Name`, `Status` (`Online`/`Offline`), `OverviewTimeText`, `OverviewCells` (by column), `WindTimeText?`, `WindCells?` / a synchronous `Read` (rejected: the downloader's synchronous `BoundedStream.Read` cannot interrupt a pending read of the live HTTP body, so a body that stalls after the headers would outlast `PageDeadline` and caller cancellation) | Proposed; the HTTP body is consumed only with asynchronous reads using the token passed in (the downloader's linked token); if the selected parser package needs synchronous input, the reader first copies the body asynchronously into memory (within the 2 MiB bound and the deadline) and parses the in-memory copy; the live HTTP body is never read synchronously; raw texts only, interpretation in P6.05 | Orchestrator, recorded (PR #8 review of `608497d`) | S1, S2 |
| Package | AngleSharp 1.8.5 (MIT), default configuration without scripting and without a resource loader (approved, OW-02) | Pinned to 1.8.5 | User (OW-02) | S1 |
| Input bound | The reader reads at most the bytes the retrieval boundary allows (2 MiB, OW-10) / own bound | The stream is already bounded by P6.06; the reader adds no second limit but stops after the two tables | Orchestrator, recorded | S2 |
| Fixtures | Synthetic HTML files under `tests/WeatherParser.Parsing.Tests/Fixtures/Osthessen/` reproducing the structural features of contract section 3 (malformed tags, legacy references, offline rows, empty cells, name spellings, row order, the compound `Temp.2m` cell with the signed change, the trend image) | Proposed; no copy of the real page | Orchestrator, recorded | S2 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `Directory.Packages.props`, `src/WeatherParser.Parsing/WeatherParser.Parsing.csproj`, lock files | Change | Package reference (OW-02) |
| `DEPENDENCIES.md` | Change | Package, version, licence, purpose, check date |
| `src/WeatherParser.Parsing/Observations/Osthessen/IOsthessenWetterPageReader.cs`, `OsthessenWetterPageReader.cs`, `OsthessenWetterPage.cs`, `OsthessenWetterStationRow.cs` | Create | Reader and raw types |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration |
| `tests/WeatherParser.Parsing.Tests/Fixtures/Osthessen/*.html` | Create | Synthetic fixtures |
| `tests/WeatherParser.Parsing.Tests/Observations/Osthessen/OsthessenWetterPageReaderTests.cs` | Create | Behavior tests |
| `docs/p6/P6_VERIFICATION.md` | Change | P6.04 section |

## 5. Procedure
Every slice: roles and checks as in P6.03 section 5. The package addition is verified with `dotnet restore --locked-mode` after the lock files are regenerated, and its licence file is read from the restored package.

### S1 Package and secure parsing
**Red**
- `ReadAsync_ScriptElement_NotExecutedAndIgnored`, `ReadAsync_ExternalResources_NeverRequested` (a fixture with `<img>`, `<link>` and `<iframe>` to an unreachable host; the reader completes without network access)
- `ReadAsync_NullStream_ThrowsArgumentNullException`, `ReadAsync_CancelledToken_ThrowsOperationCanceled`
- `ReadAsync_UsesOnlyAsynchronousReads` (a test stream whose synchronous `Read` throws; the reader completes)
- `ReadAsync_PendingReadCancelled_ThrowsOperationCanceled` (a test stream whose pending `ReadAsync` completes only when its token is cancelled; cancelling the token ends the read)
- `AddWeatherParsing_ResolvesOsthessenWetterPageReader`; `ServiceRegistrationCompletenessTests` red with its missing-registration assertion while the interface is unregistered

**Green**: package configured without scripting and without a resource loader; asynchronous body consumption (with the in-memory copy if the package needs it); singleton registration of the reader, before S2 starts. S2 and S3 keep both registration checks green and add no registration.

### S2 Tables and rows
**Red**
- `ReadAsync_ValidPage_ReturnsOverviewRowsInPageOrder`
- `ReadAsync_Row_StatusNameTimeAndCells`
- `ReadAsync_OfflineRow_StatusOfflineAndEmptyCells`
- `ReadAsync_EmptyCell_IsEmptyText` (never zero)
- `ReadAsync_TemperatureCell_PrimaryValueOnly` (the `Temp.2m` cell yields only the text of its `<font size='3'>` value with its unit; the signed change after `<br>` is not part of it, A7)
- `ReadAsync_TemperatureMissingChangePresent_TemperatureEmpty` (an empty or missing primary temperature yields an empty temperature text, never the change)
- `ReadAsync_TrendCell_NotRead` (the `Temp.-Trend` image and its `title` are not returned as a value)
- `ReadAsync_LegacyReferences_DecodedLikeBrowser` (`Ro&szligbach` → `Roßbach`, `Hohh&uumltte;` → `Hohhütte;`)
- `ReadAsync_UnclosedAndStrayTags_Tolerated`
- `ReadAsync_OtherSections_NeverRead` (daily-extreme lists with an error message containing a host name; no row and no diagnostic carries that text)
- `ReadAsync_OverviewTableMissing_ThrowsFormatChanged`, `ReadAsync_OverviewHeaderChanged_ThrowsFormatChanged`, `ReadAsync_RowWithMissingCell_ThrowsFormatChanged`
- `ReadAsync_UnknownStatusTitle_ThrowsFormatChanged`, `ReadAsync_MissingStatus_ThrowsFormatChanged`
- `ReadAsync_DuplicateOverviewName_Reported` (two rows with the same name are both returned; identity collisions are decided in P6.05)

**Green**: table location by header labels; cell extraction.

### S3 Wind table and join
**Red**
- `ReadAsync_WindRowsJoinedByKey_DifferentOrder`
- `ReadAsync_JoinKey_EszettCaseTrailingSemicolon` (`Roßbach`/`Rossbach`, `Müs-Hohhütte`/`Müs-Hohhütte;`, case)
- `ReadAsync_WindRowMissing_DiagnosticUnmatched`, `ReadAsync_TwoWindCandidates_DiagnosticUnmatched`
- `ReadAsync_WindTableMissing_DiagnosticAndNoWindCells`, `ReadAsync_WindHeaderChanged_DiagnosticAndNoWindCells`
- Added after the OW-11 amendment of 2026-10-25 (contract section 6): a wind row deviating from A5 (cell count, name, status) is ignored and its station gets `OSTHESSEN-WIND-ROW-UNMATCHED`, other stations keep their wind (`..._RowIgnoredStationUnmatched`, `ReadAsync_OneDeviatingWindRow_OnlyThatStationLosesWind`, `ReadAsync_DeviatingAndValidWindRowForSameStation_StationUnmatched`, `ReadAsync_WindRowWithMissingCell_OtherStationKeepsWind`, `ReadAsync_WindRowWithSingleCell_IgnoredWithoutNotice`).

**Green**: join key NFC, invariant fold, `ß`→`ss`, trailing `;` and whitespace removed; exactly one candidate joins, and none when a deviating row carries the same key.

**Refactor**: remove duplication with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P6 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1, S2 | T-P6-02 | REQ-044, REQ-045 | Tables, rows, references, malformed markup, deviations, other sections ignored |
| S3 | T-P6-03 | REQ-041 | Join by key, order-independent, unmatched and ambiguous rows |
| S2 | T-P6-19 (input side) | REQ-072 | Page error text never reaches rows or diagnostics |
| all | T-P6-15 | REQ-085 | Red evidence per slice |

## 7. Evidence to Record
- Package version, licence, lock-file diff; Red names and failures; counts; coverage; authorship; Codex rounds

## 8. Risks and Limitations
- (a) The package decodes references differently from the observed browser behavior — S2 fails; OW-02 is revisited with the user
- (b) A structural change after P6.02 — the documented revision stays the supported one (REQ-045); the live check of P6.10 detects drift

## 9. Acceptance Criteria
- [ ] Package pinned with licence recorded; `DEPENDENCIES.md` updated in the same change
- [ ] Every listed test green with recorded Red; fixtures synthetic
- [ ] No network access, scripts or resource loading during parsing
- [ ] Registration and architecture tests green; coverage gate passed; Codex 0 findings or reported
