# P6.04 – Page Reading: Implementation Plan

**Status: plan only (2026-10-25); nothing is implemented yet.** Refines the P6.04 row of section 5 and the "Page reading" row of section 4 of [PHASE_P6_PLAN.md](../../PHASE_P6_PLAN.md). Subject to reconciliation with the approved OW-01, OW-02, OW-05, OW-11 and OW-12 (P6.02).

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
| Interface and types (Proposed) | `IOsthessenWetterPageReader` with `OsthessenWetterPage Read(Stream html, CancellationToken)`; `OsthessenWetterPage` (`Stations` as `OsthessenWetterStationRow` in overview order, `Diagnostics`); row: `Name`, `Status` (`Online`/`Offline`), `OverviewTimeText`, `OverviewCells` (by column), `WindTimeText?`, `WindCells?` | Proposed; raw texts only, interpretation in P6.05 | Orchestrator, recorded | S2 |
| Package | As approved in OW-02 | Pinned to the evaluated version | User (OW-02) | S1 |
| Input bound | The reader reads at most the bytes the retrieval boundary allows (2 MiB, OW-10) / own bound | The stream is already bounded by P6.06; the reader adds no second limit but stops after the two tables | Orchestrator, recorded | S2 |
| Fixtures | Synthetic HTML files under `tests/WeatherParser.Parsing.Tests/Fixtures/Osthessen/` reproducing the structural features (malformed tags, legacy references, offline rows, empty cells, name spellings, row order) | Proposed; no copy of the real page | Orchestrator, recorded | S2 |

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
- `Read_ScriptElement_NotExecutedAndIgnored`, `Read_ExternalResources_NeverRequested` (a fixture with `<img>`, `<link>` and `<iframe>` to an unreachable host; the reader completes without network access)
- `Read_NullStream_ThrowsArgumentNullException`, `Read_CancelledToken_ThrowsOperationCanceled`

**Green**: package configured without scripting and without a resource loader.

### S2 Tables and rows
**Red**
- `Read_ValidPage_ReturnsOverviewRowsInPageOrder`
- `Read_Row_StatusNameTimeAndCells`
- `Read_OfflineRow_StatusOfflineAndEmptyCells`
- `Read_EmptyCell_IsEmptyText` (never zero)
- `Read_LegacyReferences_DecodedLikeBrowser` (`Ro&szligbach` → `Roßbach`, `Hohh&uumltte;` → `Hohhütte;`)
- `Read_UnclosedAndStrayTags_Tolerated`
- `Read_OtherSections_NeverRead` (daily-extreme lists with an error message containing a host name; no row and no diagnostic carries that text)
- `Read_OverviewTableMissing_ThrowsFormatChanged`, `Read_OverviewHeaderChanged_ThrowsFormatChanged`, `Read_RowWithMissingCell_ThrowsFormatChanged`
- `Read_UnknownStatusTitle_ThrowsFormatChanged`, `Read_MissingStatus_ThrowsFormatChanged`
- `Read_DuplicateOverviewName_Reported` (two rows with the same name are both returned; identity collisions are decided in P6.05)

**Green**: table location by header labels; cell extraction.

### S3 Wind table and join
**Red**
- `Read_WindRowsJoinedByKey_DifferentOrder`
- `Read_JoinKey_EszettCaseTrailingSemicolon` (`Roßbach`/`Rossbach`, `Müs-Hohhütte`/`Müs-Hohhütte;`, case)
- `Read_WindRowMissing_DiagnosticUnmatched`, `Read_TwoWindCandidates_DiagnosticUnmatched`
- `Read_WindTableMissing_DiagnosticAndNoWindCells`, `Read_WindHeaderChanged_DiagnosticAndNoWindCells`
- `AddWeatherParsing_ResolvesOsthessenWetterPageReader`; completeness test red before registration

**Green**: join key NFC, invariant fold, `ß`→`ss`, trailing `;` and whitespace removed; exactly one candidate joins.

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
