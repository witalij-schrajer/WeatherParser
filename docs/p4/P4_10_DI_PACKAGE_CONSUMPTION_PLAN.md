# P4.10 – DI Registration and Package Consumption: Implementation Plan

**Status: implemented on 2026-10-23 (S1 to S4, records in [P4_VERIFICATION.md](P4_VERIFICATION.md) section 10).** Original planning note: plan only on 2026-10-21. Refines the P4.10 row of section 5 and T-P4-14 of section 6 of [PHASE_P4_PLAN.md](../../PHASE_P4_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief; reviewed read-only by the OpenAI Codex CLI (rounds: 2, 1, 0); every finding was verified by the orchestrating agent and fixed by qwen unless stated otherwise; a cross-plan Codex review of all P4 plans ended with 0 findings (rounds: 5, 2, 0). Later: placeholders in S3/S4 replaced and joined bullets split (qwen). Reconciled with the approved selection contract (SC-08, SC-12, SC-16, SC-18, SC-22, SC-23) on 2026-10-21 (P4.02 S6). S6 record (2026-10-21): qwen reconciled this plan from the orchestrating agent's change list and fixed the confirmed review findings section by section; the orchestrating agent edited these parts itself after failed qwen attempts, for regressions or for recorded design decisions: S1 registration-boundary tests; S2 fixture paragraph and timestamp test (cross-plan); section 6 rows; README timeout wording. Codex five-angle review rounds: 5, 5, 2, 3, 0 findings; cross-plan five-angle review of all P4 plans: rounds 4, 3, 1, 1, 0 findings.

## 1. Goal and Scope
Goal: prove the integrated DI configuration of all P4 services with consumer-supplied endpoints and dependencies — independent configurations, endpoints forwarded unchanged, no global `Uri` registrations, defined failures for missing configuration — and prove independent consumption of the same-run `.nupkg` by the package consumer `tests/WeatherParser.Package.Tests`, including an offline end-to-end selection for both catalogs serialized with the real serializers and validated against the package-delivered schema; document the consumer configuration in README.md.

In scope:
- parsing-test DI scenarios (two independent containers with different endpoint options do not share endpoints or state; endpoints reach the sources unchanged; missing endpoint options, missing HTTP dependency, missing time source or logger fail at container validation with an actionable message, C-108; no `Uri` or endpoint options registered globally by `AddWeatherParsing`);
- package-consumer scenarios (resolve the selectors from the packed library; offline CDC and MOSMIX selection with a consumer-side HTTP double and embedded synthetic fixtures; serialize with the packaged serializers; validate the XML against the schema shipped in the package; empty selection; independent configurations);
- README consumer section (registration call, endpoint options with placeholder URIs such as `https://example.invalid/...`, HTTP and logging requirements, consumer responsibility for transport settings it owns, notice IDs, no real DWD URLs).

Out of scope (owner):
- CLI and its configuration → P3;
- live retrieval → P4.11;
- publication, package ID and feed → P9;
- new selection behavior → P4.03–P4.09.

## 2. Preconditions and Inputs
- P4.09 complete; the SC rows for registration, retrieval boundary, logging and time source approved.
- Existing DI fixtures were updated in P4.08/P4.09.
- The package consumer currently references MSTest, Microsoft.NET.Test.Sdk, Microsoft.CodeCoverage, FluentAssertions and Microsoft.Extensions.DependencyInjection — it has no Moq reference.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Consumer HTTP double | hand-written `HttpMessageHandler` subclass in the consumer / add Moq to the consumer project | Hand-written handler: no new consumer dependency; recorded | Orchestrator, recorded | S2 |
| Consumer fixtures | embedded resources in the consumer project / files copied to output | Embedded resources, synthetic, small; recorded with provenance | Orchestrator, recorded | S2 |
| README scope | consumer configuration section only / full API reference | Consumer configuration section with placeholders; detailed API docs stay in XML documentation comments | Orchestrator, recorded | S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `tests/WeatherParser.Parsing.Tests/Selection/SelectionDependencyInjectionTests.cs` | Create | Proposed |
| `tests/WeatherParser.Package.Tests/PackageSelectionTests.cs`, `tests/WeatherParser.Package.Tests/ConsumerHttpHandler.cs` and embedded synthetic fixtures under `tests/WeatherParser.Package.Tests/Fixtures/` | Create | Proposed |
| `tests/WeatherParser.Package.Tests/WeatherParser.Package.Tests.csproj` | Change | Embedded resources |
| `tests/WeatherParser.Package.Tests/ConsumerTestData.cs` | Change | If shared data helpers are extended |
| `README.md` | Change | Consumer configuration section and status line if needed |
| `src/WeatherParser.Parsing/` (narrowly scoped files) | Change (only on a defect) | Only for a DI, package-metadata or API defect found by S1 or S2: regression test first, recorded file allowlist, no new selection behavior |
| `docs/p4/P4_VERIFICATION.md` | Append | P4.10 section |

## 5. Procedure
### S1 Integrated DI in the parsing tests
**Red**
- `TwoContainers_DifferentEndpoints_DoNotShareEndpointsOrState` (requests from each container go only to its own synthetic endpoints)
- `Endpoints_ReachSourcesUnchanged` (path, query and trailing slash kept)
- `AddWeatherParsing_RegistersNoGlobalUriEndpointOptionsHttpClientOrTimeProvider` (descriptor inspection of what `AddWeatherParsing` adds: no `Uri`, no `CdcCatalogEndpoints` or `MosmixCatalogEndpoints`, no `HttpClient`, no `TimeProvider`; SC-09, SC-12, SC-13)
- `MissingEndpointOptions_FailsAtValidationWithActionableMessage`
- `MissingHttpDependency_FailsAtValidation`
- `MissingTimeSourceOrLogger_FailsAtValidation` (`TimeProvider` and logging are consumer-supplied, SC-12, SC-13)
- `AllP4ServicesResolveFromOneValidatedContainer`
- `MosmixSEvidenceRefresherAndCache_ResolveFromSameContainer` (both services resolve from the same container as the selectors)
- `TwoContainers_DifferentEvidenceCacheDirectories_KeepSeparateIndexes` (two containers with different `EvidenceCacheDirectory` values keep separate indexes)
- `MissingMosmixSEvidenceMaxAge_FailsAtConstructionWithEVIDENCE_CACHE_SETTINGS_INVALID` (a missing or null `MosmixSEvidenceMaxAge` fails at construction with `EVIDENCE-CACHE-SETTINGS-INVALID`)
- `InvalidSelectionLimit_FailsAtConstructionWithSELECTION_LIMIT_INVALID` (an invalid `MaxEvidenceCandidates` or `SelectionDeadline` fails with `SELECTION-LIMIT-INVALID`)
- `InvalidDirectoryUri_FailsAtConstructionWithENDPOINT_INVALID` (an invalid directory URI fails with `ENDPOINT-INVALID`)
- `ConsumerRegistration_CompleteBoundary_BuildsValidatedContainer` (the consumer registers `CdcCatalogEndpoints` with its eight `Uri` properties and the SC-18 limits, `MosmixCatalogEndpoints` with the station catalog, the MOSMIX_L `single_stations/` directory, the SC-18 limits and the SC-22 settings, an `HttpClient`, the BCL `TimeProvider` and logging through `Microsoft.Extensions.Logging.Abstractions` 10.0.13 (SC-08); the container validates, C-108)

Expected failure: these scenarios are new tests over existing registrations; tests that pass on first run are recorded as existing behavior (characterization); a failing test is a DI defect and is fixed test-first in `ServiceCollectionExtensions.cs`.

**Green**
- Only defect fixes.

**Refactor**
- Remove duplication with all tests green.

**Roles**
- qwen generates tests, the consumer handler and fixtures, and any defect fix, from briefs with the real declarations (all P4 interfaces and options types, `ServiceCollectionExtensions`, the serializers, the existing consumer helpers in `ConsumerTestData.cs`);
- the orchestrating agent writes the briefs, runs build, tests and the package procedure, reviews the diff and records authorship.

**Check**
- `dotnet restore WeatherParser.Ci.slnf --locked-mode` then `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`
- `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S2 Package consumer
**Red**
- `PackageSelection_CdcOffline_ReturnsSchemaValidList` (packed library resolved through `AddWeatherParsing` with consumer endpoint options and the hand-written handler; JSON and XML serialized; XML validated against the schema shipped in the package)
- `PackageSelection_JsonAndXmlEquivalentToSelectedGraph` (for populated and empty lists of both catalogs: both formats are parsed back and compared field by field with the selected graph and with each other — IDs with leading zeros, names, `source`, `retrieved_at` in UTC, all search parameters, entry order, distances, altitude differences, `available_measurements`, per-product availability and elements)
- `PackageSelection_MosmixOffline_ReturnsSchemaValidList` (per-product availability present)
- `PackageSelection_EmptyResult_ValidEmptyLists`
- `PackageSelection_IndependentConfigurations`
- `PackageSelection_MissingConfiguration_FailsAtValidation`
- `PackageSelection_MosmixOffline_WithRefresh_ReturnsEvidenceTimestamps` (the refresh is run first, then the selection reads the index; MOSMIX_S items and MOSMIX_L items with a file carry `evidence_retrieved_at` and `evidence_issued_at`; a MOSMIX_L location whose file answers HTTP 404 is `unavailable` with both members omitted; asserted in the JSON and the schema-validated XML. `unknown` does not occur in this flow (SC-05 option A); its omitted members are covered by the P4.07 export tests)
- `PackageSelection_MosmixOffline_NoRefresh_ThrowsEvidenceMissing` (selection fails with `MOSMIX-S-EVIDENCE-MISSING`); **asserts zero HTTP interactions before index read**
- `PackageSelection_MosmixOffline_StaleIndex_WritesNoticeOnce` (selection succeeds; notice is written once, observed through a consumer test logger)

CDC fixtures: the hand-written handler serves the four synthetic station lists and per-station `now` files of the candidate products; one product's file answers HTTP 404, which makes that product absent, not a failure (SC-03). MOSMIX fixtures: a small synthetic MOSMIX_S all-stations KMZ for the refresh and synthetic MOSMIX_L files. Each test uses its own existing temporary cache directory, removed in cleanup.

Setup first: the hand-written handler and valid synthetic fixtures are added before any behavior is evaluated. Expected outcome: P4.09 already provides the behavior, so tests that pass on first run are recorded as characterization; a failing test that reveals a package or API defect is a real Red, fixed test-first by qwen with regression verification.

**Green**
- Consumer-side handler, fixtures and test code; if S1 or S2 reveals a DI, package-metadata or API defect: a narrowly scoped production or package fix with a reproducing regression test first, the file added to the qwen allowlist and recorded, repack and full verification (build, tests, coverage, package procedure); no new selection behavior (that would reopen the owning step P4.03–P4.09).

**Refactor**
- Remove duplication with all tests green.

**Roles**
- qwen generates tests, the consumer handler and fixtures, and any defect fix, from briefs with the real declarations (all P4 interfaces and options types, `ServiceCollectionExtensions`, the serializers, the existing consumer helpers in `ConsumerTestData.cs`);
- the orchestrating agent writes the briefs, runs build, tests and the package procedure, reviews the diff and records authorship.

**Check**
- Pack and consumer procedure of CONTRIBUTING.md section 4 (`dotnet pack src/WeatherParser.Parsing/WeatherParser.Parsing.csproj --configuration Release --no-restore --output artifacts/local-nuget`, consumer lock refresh, locked restore, `dotnet test tests/WeatherParser.Package.Tests --configuration Release --no-restore`, refresh discarded)

### S3 README consumer configuration
**No Red**
- qwen drafts the README section from the orchestrator's facts (approved SC rows, real type and method names, placeholder URIs);
- the orchestrating agent checks every name against the code and every statement against the SC rows;
- then the CONTRIBUTING.md section 4 package check runs on the working tree (entries, README/LICENSE/schema byte-identical, restored hash equal to the packed hash, consumer tests, consumer-lock refresh discarded).
- README content
  - Attribution: "Deutscher Wetterdienst (DWD)" with links to the data and CC BY 4.0 licence; consumers republishing must keep attribution (MQTT, file export) (SC-16);
  - Privacy note: DWD stores requesting IP address for at most 7 days (SC-16);
  - Retrieval guidance: a selection is a configuration-time operation — run once per station or review, not on schedule; if repeated, at most once per day per search; per-station evidence requests are not for polling, and the library has no scheduler (SC-16);
  - MOSMIX_S refresh is called by consumer on external schedule (e.g., n8n workflow or cron); not a dependency (SC-22);
  - Settings: `MosmixSAllStationsUri`, `EvidenceCacheDirectory`, `MosmixSEvidenceMaxAge` (required, no default), and `MosmixSRefreshDeadline` with ranges;
  - Stale index notice: Warning with age and limit (SC-22);
  - Missing index failure: `MOSMIX-S-EVIDENCE-MISSING` (SC-22);
  - `HttpClient` handler settings (redirects, decompression, proxy, header limits) are consumer's responsibility (SC-09);
  - `HttpClient.Timeout` should be `Timeout.InfiniteTimeSpan` or above the library bound of 30 s (SC-09 recommendation); a shorter client timeout is permitted and its expiry is classified as `CATALOG-TIMEOUT`
  - SC-17 error and notice IDs:
    - Exceptions (`ErrorId`): `CoordinateFormatException` `COORDINATE-INVALID`, `SearchParameterException` `SEARCH-PARAMETER-INVALID`, `CatalogFormatException` `CATALOG-FORMAT-INVALID`, `CDC-EVIDENCE-INVALID`, `MOSMIX-EVIDENCE-INVALID`, `CatalogRetrievalException` `CATALOG-RETRIEVAL-FAILED`, `CATALOG-TIMEOUT`, `CATALOG-TOO-LARGE`, `SelectionLimitException` `SELECTION-TOO-MANY-CANDIDATES`, `SELECTION-DEADLINE-EXCEEDED`, `SELECTION-LIMIT-INVALID`, `EvidenceCacheException` `EVIDENCE-CACHE-SETTINGS-INVALID`, `EVIDENCE-CACHE-FAILED`, `MOSMIX-S-REFRESH-DEADLINE-EXCEEDED`, `MOSMIX-S-EVIDENCE-MISSING`, `ParserEndpointValidationException` `ENDPOINT-INVALID` (SC-17);
    - Notices (log event names): `SELECTION-EMPTY` (Information), `MOSMIX-LOCATION-NO-ALTITUDE` (Warning), `CDC-STATION-NO-ALTITUDE` (Warning), `CATALOG-RECORD-SKIPPED` (Warning), `CDC-STATION-CONFLICT` (Warning), `MOSMIX-S-EVIDENCE-STALE` (Warning) (SC-17);
  - Placeholder URIs only (`https://example.invalid/...`).

**Green**
- Not applicable (no code in this slice).

**Refactor**
- Not applicable (no code in this slice).

**Roles**
- qwen drafts the README section;
- the orchestrating agent reviews and checks against code and SC rows.

**Check**
- CONTRIBUTING.md section 4 package check

### S4 Review and evidence
**No Red**
- Review slice.

**Green**
- Not applicable (no code in this slice).

**Refactor**
- Not applicable (no code in this slice).

**Roles**
- The orchestrating agent runs the pre-commit coverage, package consumer check, Codex review and commit request;
- qwen fixes confirmed findings test-first.

**Check**
- Pre-commit coverage on the working tree per CONTRIBUTING.md section 5 (changed-file list from `git diff --name-only <integration-target-sha>` plus `git ls-files --others --exclude-standard`; baseline via `write-baseline` from a `coverage.xml` of the integration target collected the same way in a separate `git worktree`; runner `evaluate`; native covered/total block counters, baseline counters and runner result; exit 0 before Codex);
- Package consumer per CONTRIBUTING.md section 4 against the freshly packed library;
- Codex reviews the uncommitted diff read-only;
- qwen fixes confirmed findings test-first and the orchestrating agent reruns build, tests, coverage and the package check;
- checkpoint commit when the user requests it;
- `pwsh ./scripts/ci-local.ps1 -Commit <commit-sha> -TargetRef <integration-target-sha> -RunSettings coverage.runsettings` on that commit records the C-116 package evidence (identity, packed and restored SHA-512, entries, consumer-lock comparison); push and hosted CI record.

## 6. Tests and Test IDs
| Step-local case | T-P4 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P4-14 | REQ-014, REQ-016 | Independent containers; endpoints unchanged; no global `Uri`; missing configuration fails at validation; all P4 services resolvable |
| S2 | T-P4-14 | REQ-013, REQ-016, REQ-083 | Same-run package consumed by version; offline selections for both catalogs; schema from the package; empty result; independent configurations; missing configuration |
| S2 | T-P4-13 (package part) | REQ-067, REQ-068 | Package-side JSON and XML compared field by field with the selected graph and each other, populated and empty; schema-valid |
| S1 | T-P4-19 (DI part) | REQ-101, REQ-102, REQ-083 | `IMosmixSEvidenceRefresher` and `IMosmixEvidenceCache` resolve from the selectors' container; separate cache directories keep separate indexes; invalid cache settings fail at construction |
| S2 | T-P4-19 (consumer part) | REQ-101, REQ-102, REQ-083 | Refresh then selection from the package; `MOSMIX-S-EVIDENCE-MISSING` without a refresh and before any request; `MOSMIX-S-EVIDENCE-STALE` notice once for a stale index |
| S2 | T-P4-20 (consumer part) | REQ-067, REQ-068, REQ-079 | `evidence_retrieved_at` and `evidence_issued_at` in the package consumer's JSON and schema-validated XML |
| S1–S2 | T-P4-15 | REQ-085 | Red evidence or recorded characterization per test |

## 7. Evidence to Record
- DI scenarios and their outcome (Red or characterization) in `docs/p4/P4_VERIFICATION.md`
- Package identity, packed and restored SHA-512, entries, consumer test count and lock comparison from the committed `ci-local.ps1` run (C-116)
- README package check result
- Native block counters and runner result
- Codex review and dispositions
- Authorship

## 8. Risks and Limitations
- (a) The consumer accidentally references the project instead of the package — the existing consumer setup and C-116 hash comparison guard it.
- (b) README examples drifting from the API — every name checked against the code.
- (c) Consumer HTTP double hiding transport settings — transport configuration is covered in P4.08 and documented as consumer responsibility.
- (d) Committed consumer lock with a same-run hash — the refresh is discarded and only structural changes are committed.

## 9. Acceptance Criteria
- [ ] All T-P4-14 cases pass in the parsing tests and the package consumer
- [ ] Endpoints forwarded unchanged and never registered globally
- [ ] Missing configuration fails at validation
- [ ] Both selections run offline from the same-run package and validate against the packaged schema
- [ ] Package-side JSON and XML of populated and empty lists match the selected graph field by field
- [ ] README section accurate with placeholders and package check passed
- [ ] C-116 evidence recorded from the committed run
- [ ] Build without warnings
- [ ] Codex review with 0 open findings, or a recorded Codex unavailability or failure reported to the user (AGENTS.md)
- [ ] No CLI or live-retrieval code added
