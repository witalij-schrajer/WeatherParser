# P6.03 – Station Name Pattern: Implementation Plan

**Status: executed on 2026-10-25 ([record](P6_VERIFICATION.md) section 3): Red evidence per slice, T-P6-01 cases green, coverage gate passed (7564/7567), Codex review 2 findings fixed test-first, then 1 record count fixed.** Refines the P6.03 row of section 5 and the "Pattern" row of section 4 of [PHASE_P6_PLAN.md](../../PHASE_P6_PLAN.md). Reconciled on 2026-10-25 with the OsthessenWetter contract approved by the user on 2026-10-25 ([contract](P6_OSTHESSEN_CONTRACT.md), option A in every row): OW-09 and OW-13 as planned.

## 1. Goal and Scope
Goal: an injectable matcher that validates a station name pattern and decides whether a station name matches it, with the glob semantics of REQ-040, culture-independent and within an O(pattern length × name length) bound, before any request is made.
In scope: pattern validation (`OSTHESSEN-PATTERN-INVALID`); `*` and `?`; literal treatment of every other character; invariant case folding; NFC normalization of pattern and name; whole-name match; bounded-time algorithm; the exception type `OsthessenWetterException` with its error-ID guard; registration in `AddWeatherParsing`.
Out of scope with owner: reading names from the page → P6.04; applying the match to the station set and the no-match failure → P6.07; CLI argument handling → P3.

## 2. Preconditions and Inputs
- P6.02 approved OW-09 and OW-13.
- Existing patterns: `DwdStationException` (error-ID guard), `ServiceRegistrationCompletenessTests`, `NoSharedStateTests`.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Interface (Proposed) | `IStationNamePatternMatcher` with `StationNamePattern Validate(string? pattern)` and `bool IsMatch(StationNamePattern pattern, string name)`; `StationNamePattern` an immutable value holding the normalized pattern / one method taking strings | Validated value type, so P6.07 validates once before the request and matches many names | Orchestrator, recorded | S1 |
| Algorithm | Two-pointer glob matching that backtracks only to the most recent `*` (each name position is revisited at most once per pattern position, so at most O(pattern × name) steps; the classic worst case is `*` followed by a long literal suffix) / conversion to `Regex` | Two-pointer; no regex, so metacharacters stay literal by construction; the bound is stated as O(pattern × name), not linear (OW-09 approved) | Orchestrator, recorded; bound User (OW-09) | S2 |
| `?` unit | One `char` / one Unicode scalar (text element) after NFC | One Unicode scalar after NFC (OW-09), so `M?s` matches `Müs` in NFC and NFD input | User (OW-09) | S2 |
| Case folding | `ToUpperInvariant` per scalar / culture-specific comparison | Invariant per scalar, culture-independent; `ß` folds to itself | User (OW-09) | S2 |
| Folder | `src/WeatherParser.Parsing/Observations/Osthessen/` / other | Proposed (phase plan section 8) | Orchestrator, recorded | S1 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Observations/Osthessen/IStationNamePatternMatcher.cs` | Create | Interface |
| `src/WeatherParser.Parsing/Observations/Osthessen/StationNamePatternMatcher.cs` | Create | Implementation |
| `src/WeatherParser.Parsing/Observations/Osthessen/StationNamePattern.cs` | Create | Validated value |
| `src/WeatherParser.Parsing/Exceptions/OsthessenWetterException.cs` | Create | `ErrorId` constants (OW-13) and guard |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Singleton registration |
| `tests/WeatherParser.Parsing.Tests/Observations/Osthessen/StationNamePatternMatcherTests.cs` | Create | Behavior tests |
| `tests/WeatherParser.Parsing.Tests/Exceptions/OsthessenWetterExceptionTests.cs` | Create | Guard tests |
| `docs/p6/P6_VERIFICATION.md` | Change | P6.03 section |

## 5. Procedure
Every slice: qwen generates the tests first, then the minimal implementation and the refactoring, from briefs with the real declarations; the orchestrating agent runs each Red, the build, the tests and the coverage, reviews the diff and records authorship. Check: `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`, then the pre-commit coverage gate.

### S1 Validation and exception
**Red**
- `Validate_Null_ThrowsPatternInvalid`, `Validate_Empty_ThrowsPatternInvalid`, `Validate_WhitespaceOnly_ThrowsPatternInvalid`
- `Validate_128Characters_Accepted`, `Validate_129Characters_ThrowsPatternInvalid`
- `Validate_StarOnly_Accepted`
- `Validate_NfdInput_StoredAsNfc`
- `OsthessenWetterException_UnknownErrorId_ThrowsArgumentException`, `OsthessenWetterException_EveryDefinedId_Accepted`
- `AddWeatherParsing_ResolvesStationNamePatternMatcher`; `ServiceRegistrationCompletenessTests` red before registration

Expected failure: `NotImplementedException` from the stub; the completeness test fails with its missing-registration assertion.

**Green**: validation in the order null, empty or whitespace, length; NFC; exception message in English without echoing the pattern beyond its length; registration.

### S2 Matching
**Red**
- `IsMatch_NoWildcard_WholeNameOnly` (`Müs` matches `Müs`, not `Müs-Hohhütte`)
- `IsMatch_Star_MatchesZeroOneMany` (`Müs*` matches `Müs`, `Müs-Hohhütte`)
- `IsMatch_Question_MatchesExactlyOne` (`M?s` matches `Müs`, not `Ms` or `Müüs`)
- `IsMatch_Question_SupplementaryScalar` (a synthetic name `A𝄞B` with one supplementary scalar: `A?B` matches, `A??B` does not)
- `IsMatch_CaseInsensitive_WithUmlauts` (`müs`, `MÜS`)
- `IsMatch_NfcAndNfdEqual`
- `IsMatch_RegexMetacharactersLiteral` (`[`, `]`, `.`, `+`, `\`, `^`, `$`, `(`, `|`)
- `IsMatch_EszettLiteral` (`Roß*` does not match `Rossbach`; the table join of OW-05 is not the pattern rule)
- `IsMatch_LeadingAndTrailingStar` (`*hütte`, `*sal*`)
- `IsMatch_ManyStars_StepCountWithinBound` (`*a*a*a*a*a*a*a*a*b` against 2000 `a`: no match, and the matcher's step counter, exposed to tests through an internal hook, stays at or below pattern length × (name length + 1))
- `IsMatch_LongLiteralSuffix_StepCountWithinBound` (`*` followed by 126 `a` and `b` against 2000 `a`: no match, step count within the same bound)
- `IsMatch_CultureIndependent` (results equal under `tr-TR`, `de-DE` and the invariant culture)

**Green**: two-pointer matching over Unicode scalars.

**Refactor**: remove duplication with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P6 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1, S2 | T-P6-01 | REQ-040 | Grammar, case, NFC, Unicode scalars, whole-name match, literal metacharacters, validation boundaries, bounded time |
| S1 | T-P6-13 (registration) | REQ-078 | Interface registered; completeness test green |
| S1, S2 | T-P6-15 | REQ-085 | Red evidence per slice |

## 7. Evidence to Record
- Red test names with observed failures; Green and Refactor results; test counts; coverage counters; authorship (qwen: tests and code; orchestrating agent: briefs, review, records); Codex rounds

## 8. Risks and Limitations
- (a) Names with combining characters not covered by NFC composition — matched by scalar after NFC; recorded
- (b) The pattern semantics differ from shell globbing (no `[...]`) — intended by REQ-040; README states it in P6.09

## 9. Acceptance Criteria
- [ ] Every listed test green with recorded Red
- [ ] No regex used for matching; step-count bound cases green
- [ ] Registration and architecture tests green
- [ ] Coverage gate passed; Codex review 0 findings or reported
