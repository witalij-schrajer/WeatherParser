# P5.04 – Observation File Reading: Implementation Plan

**Status: executed on 2026-10-24 ([record](P5_VERIFICATION.md) section 4): Red evidence per slice, T-P5-03 cases green, P4 regression green, coverage gate passed, Codex review with 1 finding (fixed in this plan's S2 expected-failure text).** Reconciled with the approved observation contract ([OC rows](P5_OBSERVATION_CONTRACT.md), approved 2026-10-24) on 2026-10-24: OC-07 and OC-14; no change of content. Refines the P5.04 row of section 5 and the "File reading" row of section 4 of [PHASE_P5_PLAN.md](../../PHASE_P5_PLAN.md).

## 1. Goal and Scope
Goal: extend the shared `ICdcNowEvidenceReader` component additively (OC-14, SC-21) so that every row exposes its `QN` quality level and the raw tokens of all data columns of the four products, while the SC-19 bounds, grammar and failure semantics and every P4 caller stay unchanged.
In scope: additive member for `QN` on `CdcNowEvidenceRow`; all data columns exposed as raw trimmed tokens (grammar validation still only for the used columns, SC-19); synthetic archives for `tu`, `ff`, `rr` and `sd` through `CdcNowArchiveBuilder`; regression of the P4 reader tests and of `CdcAvailabilityDeriver`; no new public service interface (no registration change).
Out of scope with owner: numeric interpretation, units and quality mapping → P5.05; retrieval → P5.06; station lists → P5.03/P5.06; any change of the SC-19 bounds → none (a change would be a contract decision for the user).

## 2. Preconditions and Inputs
- P5.02 approved OC-07 (quality code form) and recorded OC-14.
- Existing reader, row record, tests and the archive builder (P4.06).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Form of the additive member | `string QualityLevel { get; init; }` with a default on the positional record / a new positional parameter (breaks existing constructions) | Init property with default, so P4 callers and tests compile unchanged | Orchestrator, recorded | S1 |
| Exposure of ignored columns | all data columns in `Values` as raw trimmed tokens, validated only for the used columns / used columns only | All columns exposed, grammar unchanged (SC-19 "ignored columns are not validated") | Orchestrator, recorded (OC-14) | S2 |
| Fixture strategy | `CdcNowArchiveBuilder` extended for the four products / files under `tests/WeatherParser.Parsing.Tests/Fixtures/CdcNow/` | Builder (consistent with P4, synthetic, SC-14) | Orchestrator, recorded | S1 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Parsing/CdcNowEvidenceRow.cs` | Change | Additive `QualityLevel` member |
| `src/WeatherParser.Parsing/Parsing/CdcNowEvidenceReader.cs` | Change | Expose `QN` and all data columns; grammar and bounds unchanged |
| `src/WeatherParser.Parsing/Parsing/ICdcNowEvidenceReader.cs` | Change | Documentation only: the returned rows carry `QualityLevel` and the raw tokens of all data columns (ignored columns unvalidated) |
| `tests/WeatherParser.Parsing.Tests/Parsing/CdcNowArchiveBuilder.cs` | Change only if needed | `Header` and `Valid` already build all four products; a parameter varying the `QN` token per row is added only if the existing builder cannot express it |
| `tests/WeatherParser.Parsing.Tests/Parsing/CdcNowEvidenceReaderColumnsTests.cs` | Create | New behavior tests |
| `tests/WeatherParser.Parsing.Tests/Parsing/CdcNowEvidenceReaderTests.cs` | Unchanged | P4 regression |
| `docs/p5/P5_VERIFICATION.md` | Change | P5.04 section |

## 5. Procedure
### S1 Quality level exposed
**Red**
- `ReadAsync_ExposesQualityLevelPerRow` (`3` and `2` on two rows)
- `ReadAsync_QualityLevelKeptAsString` (`003` stays `003`, no numeric conversion)
- `ReadAsync_InvalidQualityLevel_StillCdcEvidenceInvalid` (regression of the SC-19 grammar)
- `CdcNowEvidenceRow_DefaultConstruction_CompilesWithoutQualityLevel` (P4 construction form)

Expected failure: the defaulted `QualityLevel` property is added first without reader population, so `ReadAsync_ExposesQualityLevelPerRow` and `ReadAsync_QualityLevelKeptAsString` fail on their assertions (empty value); `ReadAsync_InvalidQualityLevel_StillCdcEvidenceInvalid` and `CdcNowEvidenceRow_DefaultConstruction_CompilesWithoutQualityLevel` pass on first run and are recorded as existing behavior.

**Green**
- `QualityLevel` filled from the trimmed `QN` field.

**Refactor**
- remove duplication with all tests green.

**Roles**
- qwen generates the tests, then the minimal change and the refactoring; the orchestrating agent writes the briefs with the real declarations, runs each Red, build, tests and coverage, reviews the diff and records authorship.

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`; `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S2 All data columns exposed
**Red**
- `ReadAsync_TuArchive_ExposesAllFiveColumns` (`PP_10`, `TT_10`, `TM5_10`, `RF_10`, `TD_10`)
- `ReadAsync_FfArchive_ExposesBothColumns`
- `ReadAsync_RrArchive_ExposesThreeColumns` (`RWS_DAU_10`, `RWS_10`, `RWS_IND_10`)
- `ReadAsync_SdArchive_ExposesFourColumns` (`DS_10`, `GS_10`, `SD_10`, `LS_10`)
- `ReadAsync_IgnoredColumnWithArbitraryToken_NotValidated` (an ignored column holding `abc` keeps the file valid, SC-19)
- `ReadAsync_UsedColumnInvalidToken_StillCdcEvidenceInvalid`
- `ReadAsync_MissingUsedColumn_StillCdcEvidenceInvalid`
- `ReadAsync_TokensAreTrimmed`

Expected failure: `ReadAsync_TuArchive_ExposesAllFiveColumns`, `ReadAsync_RrArchive_ExposesThreeColumns` and `ReadAsync_SdArchive_ExposesFourColumns` fail because `Values` holds only the used columns (`TM5_10`, `TD_10`, `RWS_DAU_10`, `RWS_IND_10`, `DS_10`, `SD_10`, `LS_10` absent); `ReadAsync_IgnoredColumnWithArbitraryToken_NotValidated` fails too, on its exposure assertion (`KeyNotFoundException` for `TM5_10`), while its acceptance of the arbitrary token is existing behavior (corrected after the Codex review of 2026-10-24); `ReadAsync_FfArchive_ExposesBothColumns` (no ignored column in `ff`), `ReadAsync_UsedColumnInvalidToken_StillCdcEvidenceInvalid`, `ReadAsync_MissingUsedColumn_StillCdcEvidenceInvalid` and `ReadAsync_TokensAreTrimmed` pass on first run and are recorded as existing behavior.

**Green**
- all data columns between `QN` and `eor` copied as trimmed raw tokens; validation set unchanged.

**Refactor**
- as in S1.

**Roles**
- as in S1.

**Check**
- as in S1.

### S3 P4 regression and bounds
**Red**
- No Red: characterization; the existing `CdcNowEvidenceReaderTests`, `CdcAvailabilityDeriver*Tests` and the P4 pipeline tests stay green; the existing boundary tests `ReadAsync_UncompressedSizeAboveLimit_IsInvalid` and `ReadAsync_RowLimitAboveTwoThousand_IsInvalid` with their at-limit counterparts (`CatalogFormatException` `CDC-EVIDENCE-INVALID`) are run as regression (expected to pass on first run and recorded as existing behavior).

**Green**
- no change expected; any failure is a defect of S1/S2 fixed test-first.

**Refactor**
- none.

**Roles**
- the orchestrating agent runs the full Parsing test project and records counts; qwen fixes defects if any.

**Check**
- as in S1.

### S4 Review and evidence
**Red**
- No Red: review slice.

**Green**
- build, all tests, pre-commit coverage with the changed-file list from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard`, native block counters, baseline and gate result.

**Roles**
- the orchestrating agent runs the checks; Codex reviews the uncommitted diff read-only; qwen fixes confirmed findings test-first; checkpoint commit when the user requests it; push; hosted CI run recorded.

**Check**
- pre-commit coverage (CONTRIBUTING.md section 5); Codex review; commit and CI run recorded in `docs/p5/P5_VERIFICATION.md`.

## 6. Tests and Test IDs
| Step-local case | T-P5 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P5-03 | REQ-032 | `QN` exposed per row as a string; invalid `QN` still rejected |
| S2 | T-P5-03 | REQ-032, REQ-083 | All columns of `tu`, `ff`, `rr`, `sd` exposed as raw trimmed tokens; used-column grammar and missing-column failure unchanged; ignored columns not validated; synthetic archives |
| S3 | T-P5-03 | REQ-083 | SC-19 bounds (1 MiB, 2000 rows) and P4 callers unchanged |
| S1–S2 | T-P5-15 | REQ-085 | Recorded Red → Green → Refactor evidence |

## 7. Evidence to Record
- Red names with the observed assertion failures of S1 (`ReadAsync_ExposesQualityLevelPerRow`, `ReadAsync_QualityLevelKeptAsString`) and S2; the S1 and S2 cases that passed on first run recorded as existing behavior
- Existing-behavior records of S3; test counts before and after
- Native block counters and gate result; Codex review and dispositions; authorship

## 8. Risks and Limitations
- (a) Exposing ignored columns raises memory per row — bounded by SC-19 (at most 2000 rows, 1 MiB member)
- (b) A positional-parameter change would break P4 tests — the init-property form avoids it
- (c) Column names of the real files differ from the P4 facts — detected by P5.02; the reader validates the used columns only

## 9. Acceptance Criteria
- [ ] Each behavior increment has recorded Red evidence
- [ ] All T-P5-03 cases pass; P4 reader and deriver tests unchanged and green
- [ ] SC-19 bounds and failure semantics unchanged
- [ ] No new public service interface; `ServiceRegistrationCompletenessTests` green
- [ ] Build without warnings; pre-commit coverage gate passed
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user
- [ ] No numeric interpretation, retrieval or mapping code added
