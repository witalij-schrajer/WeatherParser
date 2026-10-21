# P4.04 – Geodesy and Inclusive Filters: Implementation Plan

**Status: plan only (2026-10-21); nothing is implemented or verified yet.** Refines the P4.04 row of section 5 and the "Distance and filters" row of section 4 of [PHASE_P4_PLAN.md](../../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 4, 1, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0). Later: Codex-outage acceptance wording and an S3 Green placeholder replaced. The orchestrating agent replaced one decision-row phrase itself after two failed precise briefs; an S3 Green placeholder was later replaced by qwen.

## 1. Goal and Scope
Goal: an injectable distance calculator implementing the geodetic method approved in P4.02 (REQ-022: documented WGS84 method) and an injectable search-area filter that applies both inclusive limits separately — horizontal distance ≤ `max_distance_km` and absolute altitude difference ≤ `max_altitude_difference_m` — and yields `distance_km` and the signed `altitude_difference_m` (candidate altitude minus reference altitude) for a candidate, test-first, registered in `AddWeatherParsing`.

In scope:
- Distance between two WGS84 points from `decimal` degrees, using `double` arithmetic internally where trigonometry requires it, with the result converted back to `decimal` without rounding beyond the conversion (C-26); reference distances from the published source named in the approved geodetic-method SC row, with the SC tolerance; edge cases: identical points (0 km), points on both sides of the antimeridian (for example longitudes `179.9` and `-179.9` give a short distance), poles, and — if the SC method is iterative (for example an ellipsoidal inverse method) — the non-convergence/near-antipodal case handled as decided in the SC row (failure with an English message or a documented fallback stated in the SC row, never a silent wrong value); nonfinite intermediate results rejected; filter evaluation with both limits inclusive at the exact boundary; altitude filtered separately from distance (no 3-D distance, no degrees-based Euclidean distance); negative reference altitudes; a candidate without altitude is reported as "not filterable" (distinct result), never treated as altitude 0 (REQ-102); DI registration per slice.

Out of scope with owner:
- Parsing coordinates and validating `SearchParameters` → P4.03
- Reading catalogs and deciding which candidates exist → P4.05/P4.07
- Excluded-location notices and logging → P4.07/P4.09
- Ordering of results → `IListEntryOrdering` in P4.09
- CLI → P3

## 2. Preconditions and Inputs
- P4.02 approved the geodetic-method (method, citation, reference distances, tolerance, convergence handling) and public-API SC rows
- P4.03 delivered `SearchParameters` creation

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Geodetic method, reference data and tolerance | — | As approved in the geodetic-method SC row; this step only implements it | User (P4.02) | S1 |
| Interface and type names | Proposed `IDistanceCalculator`/`DistanceCalculator` with `CalculateKm(decimal latitude1, decimal longitude1, decimal latitude2, decimal longitude2)` returning `decimal`; Proposed `ISearchAreaFilter`/`SearchAreaFilter` with `Evaluate(SearchParameters parameters, decimal latitude, decimal longitude, decimal? altitudeM)` returning a Proposed result type `SearchAreaEvaluation` (match state `Inside`, `Outside`, `NotFilterable`; nullable `DistanceKm` and nullable signed `AltitudeDifferenceM`, both null for `NotFilterable`) / names from the approved public-API SC row | The approved SC row wins; Proposed names only where it leaves them open | User (P4.02) | S1–S3 |
| Boundary comparison | compare the `decimal` distance from the calculator with `max_distance_km` using `<=` / compare in `double` | `decimal` `<=` on the values that are exported, so the exported `distance_km` and the filter decision always agree | Orchestrator, recorded | S2 |
| `double` → `decimal` conversion | direct cast / `decimal.Parse` of the round-trip string | Direct cast, documented; a nonfinite or out-of-range `double` throws before conversion | Orchestrator, recorded | S1 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Geography/IDistanceCalculator.cs` | Create | Proposed interface with `CalculateKm(decimal latitude1, decimal longitude1, decimal latitude2, decimal longitude2)` returning `decimal` |
| `src/WeatherParser.Parsing/Geography/DistanceCalculator.cs` | Create | Proposed implementation class with the approved method |
| `src/WeatherParser.Parsing/Geography/ISearchAreaFilter.cs` | Create | Proposed interface with `Evaluate(SearchParameters parameters, decimal latitude, decimal longitude, decimal? altitudeM)` returning `SearchAreaEvaluation` |
| `src/WeatherParser.Parsing/Geography/SearchAreaFilter.cs` | Create | Proposed implementation class |
| `src/WeatherParser.Parsing/Geography/SearchAreaEvaluation.cs` | Create | Proposed result type with state enum (`Inside`, `Outside`, `NotFilterable`), nullable `DistanceKm` and nullable signed `AltitudeDifferenceM` (both null for `NotFilterable`); not an export entity |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Add registrations for `IDistanceCalculator` and `ISearchAreaFilter` |
| `tests/WeatherParser.Parsing.Tests/Geography/DistanceCalculatorTests.cs` | Create | Tests for distance calculator |
| `tests/WeatherParser.Parsing.Tests/Geography/SearchAreaFilterTests.cs` | Create | Tests for search-area filter |
| `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` | Extend | Add resolve tests for the new services |
| `docs/p4/P4_VERIFICATION.md` | Append | Section for P4.04 |

## 5. Procedure
### S1 Distance calculator
**Red**
- `CalculateKm_PublishedReferencePairs_WithinTolerance` (the reference pairs and expected distances listed in the geodetic-method SC row, compared with its tolerance)
- `CalculateKm_IdenticalPoints_ReturnsZero`
- `CalculateKm_IsSymmetric`
- `CalculateKm_AcrossAntimeridian_ReturnsShortDistance` (`179.9`/`-179.9` on the same latitude)
- `CalculateKm_PoleToPole_MatchesReference` (if the SC reference data contain it; otherwise the SC-row behavior for that case)
- `CalculateKm_NonConvergingCase_BehavesAsDecided` (only if the SC method is iterative)
- `AddWeatherParsing_ResolvesDistanceCalculator` (container built with `ValidateOnBuild` and `ValidateScopes`, C-108)
- `ServiceRegistrationCompletenessTests` turns red when `IDistanceCalculator` appears

Expected failure: the stub throws `NotImplementedException`; completeness test red until registration.

**Green**
- The SC method implementation
- Registration in `AddWeatherParsing`

**Refactor**
- Remove duplication with tests green

**Roles**
- qwen generates tests first, then minimal implementation and refactoring, from briefs with the real declarations (`SearchParameters`, `ServiceCollectionExtensions`, the approved SC rows and reference data)
- The orchestrating agent writes the briefs, checks the reference values against the cited source itself (model output is no evidence for reference distances), runs each Red, build, tests and coverage, reviews the diff and records authorship

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S2 Search-area filter
Tests use a shared strict `MockRepository`; the subject is constructed in `[TestInitialize]` with the mocked `IDistanceCalculator` injected through its interface; `[TestCleanup]` calls `VerifyAll()`; exact call counts are checked with `Verify(..., Times.Once())`.

**Red**
- `Evaluate_DistanceBelowEqualAboveMax` (mock returns `max - 0.000001`, `max`, `max + 0.000001` → `Inside`, `Inside`, `Outside`)
- `Evaluate_DistanceDecimalsThatCollapseToSameDouble_AreDistinguished` (two distinct `decimal` values, one equal to `max` and one a single unit in the 28th significant digit above it, that convert to the same `double`: the first is `Inside`, the second `Outside`)
- `Evaluate_AltitudeDifferenceBelowEqualAboveMax_BothSigns` (signed differences `±(max - 0.001)`, `±max`, `±(max + 0.001)` → inside, inside, outside on both signs)
- `Evaluate_AltitudeDecimalsThatCollapseToSameDouble_AreDistinguished` (reference altitude `0`, maximum difference `1`; candidate altitudes `1.000000000000000000000000001` and `-1.000000000000000000000000001` → `Outside` with the exact signed `AltitudeDifferenceM`; `1` and `-1` → `Inside`; subtraction, absolute value and comparison stay in `decimal`)
- `Evaluate_AltitudeFilteredSeparately` (distance inside, altitude outside → `Outside`; distance outside, altitude inside → `Outside`)
- `Evaluate_ReturnsSignedAltitudeDifference` (candidate 120 m, reference 300 m → `-180`)
- `Evaluate_NegativeReferenceAltitude` (reference `-5`)
- `Evaluate_ZeroLimits` (only an identical point with equal altitude is inside)
- `Evaluate_MissingCandidateAltitude_IsNotFilterable` (never treated as 0; when `max_altitude_difference_m` would accept 0 the result is still `NotFilterable`; `DistanceKm` and `AltitudeDifferenceM` are null, never 0)
- `Evaluate_CallsCalculatorExactlyOnce` (the strict mock verifies one call with `Times.Once()`)
- `Evaluate_MissingAltitude_DoesNotCallCalculator` (`Times.Never()`)
- `AddWeatherParsing_ResolvesSearchAreaFilter` (container built with `ValidateOnBuild` and `ValidateScopes`, C-108)
- `ServiceRegistrationCompletenessTests` turns red when `ISearchAreaFilter` appears

Expected failure: stub throws `NotImplementedException`. Green: inclusive `decimal` comparisons, signed difference, `NotFilterable` before any distance call when the altitude is missing (Recommended: no calculator call for a not-filterable candidate; the test fixes the decided order), registration.

**Green**
- Implementation of inclusive `decimal` comparisons
- Signed difference calculation
- `NotFilterable` handling before any distance call when the altitude is missing
- Registration in `AddWeatherParsing`

**Refactor**
- Remove duplication with tests green

**Roles**
- qwen generates tests first, then minimal implementation and refactoring, from briefs with the real declarations (`SearchParameters`, `ServiceCollectionExtensions`, the approved SC rows and reference data)
- The orchestrating agent writes the briefs, checks the reference values against the cited source itself (model output is no evidence for reference distances), runs each Red, build, tests and coverage, reviews the diff and records authorship

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S3 Real composition
**Red**
- `Evaluate_WithRealCalculator_LimitsAroundReturnedDistance` (real `DistanceCalculator` from DI; `max_distance_km` set just below, equal to and just above the returned `decimal` distance with altitude inside → `Outside`, `Inside`, `Inside`; the evaluation's `DistanceKm` equals `CalculateKm` for the same points)

If it already passes it is recorded as existing behavior (no Red).

**Green**
- Only if the S3 test fails: qwen corrects `SearchAreaFilter` so the decision uses the same `decimal` distance it reports; otherwise no change (existing behavior recorded)

**Refactor**
- Remove duplication with tests green

**Roles**
- qwen generates tests first, then minimal implementation and refactoring, from briefs with the real declarations (`SearchParameters`, `ServiceCollectionExtensions`, the approved SC rows and reference data)
- The orchestrating agent writes the briefs, checks the reference values against the cited source itself (model output is no evidence for reference distances), runs each Red, build, tests and coverage, reviews the diff and records authorship

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S4 Review and evidence
No Red: review slice. The orchestrating agent runs build and all tests, then the coverage procedure of CONTRIBUTING.md section 5 with the changed-file list written from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard` (uncommitted and untracked step files included) and a comparably collected baseline of the integration target; it records native covered/total block counters of `WeatherParser.Parsing.dll`, the baseline counters and the gate result (strict improvement below a 100 % baseline). Then Codex reviews the uncommitted diff read-only; qwen fixes confirmed findings test-first; checkpoint commit when the user requests it; push to the draft pull request and record the hosted CI run.

## 6. Tests and Test IDs
| Step-local case | T-P4 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P4-04 | REQ-022 | Published reference distances within the SC tolerance; identical points; symmetry; antimeridian; poles; convergence behavior as decided |
| S2 | T-P4-05 | REQ-022, REQ-082 | Distance and absolute altitude limits inclusive at the exact boundary and exclusive just above; altitude filtered separately; signed altitude difference; negative reference altitude; zero limits |
| S2 | T-P4-08 (partial) | REQ-102 | Missing candidate altitude yields `NotFilterable`, never 0 (the notice is added in P4.07/P4.09) |
| S1, S2 | T-P4-14 (partial) | REQ-016 | Services resolved through their interfaces; registration completeness (C-107) |
| S1–S3 | T-P4-15 | REQ-085 | Recorded Red → Green → Refactor evidence |

## 7. Evidence to Record
- The reference source (citation, URL, values) checked by the orchestrating agent
- Red names with observed failures
- Existing-behavior records
- Commands
- Test counts
- Native block counters and gate result
- Codex review and dispositions
- Authorship

## 8. Risks and Limitations
- (a) Floating-point differences across platforms (Windows x64 versus ARM64) near the tolerance — tolerance from the SC row, P4.11 reruns the tests on the NanoPi R76S
- (b) An exported `distance_km` that disagrees with the filter decision — the filter compares the same `decimal` it reports (S3)
- (c) Iterative methods failing near antipodal points — handled as decided in the SC row and tested
- (d) Reference values copied from a model instead of the source — the orchestrating agent verifies each value against the cited publication

## 9. Acceptance Criteria
- [ ] Each behavior increment has recorded Red evidence
- [ ] All T-P4-04 and T-P4-05 cases pass on Windows (ARM64 in P4.11)
- [ ] Missing altitude never treated as 0
- [ ] No Euclidean degree distance or 3-D distance
- [ ] Both services registered and completeness tests green
- [ ] Build without warnings
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user (AGENTS.md)
- [ ] No catalog, logging or ordering code added
