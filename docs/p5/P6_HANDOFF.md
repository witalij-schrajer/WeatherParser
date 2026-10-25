# P6 Handoff – OsthessenWetter Observations

Status: prepared with the P5.11 acceptance record on 2026-10-25 after P5 was accepted by the user on candidate `fd461c8` ([P5.11 acceptance record](P5_11_ACCEPTANCE.md)). In effect since pull request #7 merged into `develop` on 2026-10-25 (merge commit `29fd553`); P6 starts only on a separate instruction of the user.

## 1. Accepted foundation

- **Candidate**: verified implementation candidate `fd461c8cededddf53a039c31adad68a110c1c7c9` on branch `features/develop/P5`, pull request #7 into `develop` (PR base `1e604dc7b3f17826d19207cbec9a699e40e5a30f`). P5.10 outcome READY after the rerun on this candidate ([P5.10 record](P5_10_INTEGRATED_VERIFICATION.md) section 9).
- **P6 integration target**: the merge commit `29fd5533449898f1b14513b11c65a0afa1cf049a` (`29fd553`) of pull request #7 on `develop`, or its documentation-only successor on `develop` at P6 branch creation (this post-merge documentation pull request); P6.01 records the `develop` head it branches from and classifies every commit since `29fd553`.
- **Package**: `WeatherParser.Parsing` `0.1.0-initial` (not published); contents `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE`, [`schemas/export/v1/weatherparser-export-v1.xsd`](../../schemas/export/v1/weatherparser-export-v1.xsd).
- **Coverage**: native block coverage of `WeatherParser.Parsing.dll` is 7491/7494 (99.96 %) on Windows and on the NanoPi R76S; the next PR's gate compares against the then-remeasured base.
- **Contracts**:
  - export contract v1 [P2_EXPORT_CONTRACT.md](../p2/P2_EXPORT_CONTRACT.md) (C-01 to C-119; v1 frozen, only additive optional members, C-03, C-105; unchanged by P5)
  - selection contract [P4_SELECTION_CONTRACT.md](../p4/P4_SELECTION_CONTRACT.md) (SC-01 to SC-23)
  - observation contract [P5_OBSERVATION_CONTRACT.md](P5_OBSERVATION_CONTRACT.md) (OC-01 to OC-14)
- **Evidence**: [P5_VERIFICATION.md](P5_VERIFICATION.md); platform summary [COMPATIBILITY.md](../COMPATIBILITY.md).

## 2. What P5 provides

- **DWD observation parser**: `IDwdObservationParser` / `DwdObservationParser` with `Task<ObservationDataset> ParseAsync(string stationId, CancellationToken cancellationToken)` (OC-08), and the services it composes:
  - `IDwdStationResolver` (ID grammar and station-list lookup, OC-01, OC-02)
  - `IDwdObservationSource` (bounded retrieval of lists and `now` archives, OC-03, OC-11)
  - `IDwdObservationMapper` (units, periods, missing markers, calm wind, quality, OC-04 to OC-07)
  - `ICdcRequestComposer` (escaped list and archive URIs)
- **Options and errors**:
  - consumer-registered `DwdObservationOptions` (`ObservationDeadline`, default 120 s, permitted (0, 1 h], OC-09)
  - error IDs on `DwdStationException` and `ObservationLimitException`
  - notice IDs in `DwdObservationNoticeIds` (OC-10)
- **Reader extension**: the extended `ICdcNowEvidenceReader` (`QualityLevel` and every data column, OC-14).
- **Shared building blocks used for the first time by a provider flow**:
  - `IObservationDatasetBuilder` (dataset assembly, omissions and rejection with `ObservationDatasetRejectedException.OmittedReadings`)
  - `ISourceTimeNormalizer.NormalizeComplete`
  - the real `IJsonExportSerializer` and `IXmlExportSerializer`
- **Registration**: all services are registered by `AddWeatherParsing` (singletons, C-107). Consumers register endpoint options, `DwdObservationOptions`, `HttpClient`, `TimeProvider` and logging (README.md section "DWD observations (library)").
- **Test patterns**:
  - the composed pipeline tests with only the downloader mocked (`ComposedDwdObservationPipelineTests`)
  - the export contract tests through the real serializers with the shared `ExportEquivalence` helper
  - the package-consumer tests over `ConsumerSelectionHost`
  - the opt-in live check with a request budget (`LiveCheckRunner.RunObservationAsync`, `WEATHERPARSER_LIVE_CHECK_MODE=observation`)

## 3. P6 responsibilities (DEVELOPMENT_PLAN.md P6)

- **Page structure**: use the OsthessenWetter page structure as encountered at implementation time. Document the implementation date, the source URL and the structural assumptions relevant to parsing. Support applies to that documented revision.
- **Endpoint**: the parser receives its source URL explicitly as a constructor parameter (`Uri`); the consumer supplies it, and there is no built-in default (AGENTS.md).
- **Retrieval failure**: if the configured page cannot be retrieved, the parser throws an exception with an actionable English diagnostic; there is no replacement data and no simulated success (P0 clarification).
- **Parsing**: parse the displayed measurement timestamps, with no freshness filter and no correction of source data. Log detectable format deviations (REQ-044).
- **Date inference**: when only a time of day is shown, infer the date with `ISourceTimeNormalizer.InferDate` and mark it `estimated_date` (REQ-042), in `Europe/Berlin` converted to UTC. Test midnight and DST gaps and ambiguity, and never replace a complete source timestamp.
- **Tests**: use synthetic HTML fixtures for glob filters, multiple results, umlauts and collision-free sources. Never relabel gusts or rain totals.
- **Source conditions**: clarify them (REQ-092). Source access without clarified conditions is not production acceptance.

## 4. Reuse obligations

- `ISourceTimeNormalizer.InferDate` and `NormalizeComplete` for every source time; no second time-zone path.
- `IObservationDatasetBuilder` for dataset assembly, omissions and rejection; no parallel builder (P2 limitation: builders and root validation implement the same rules separately, guarded by the equivalence tests).
- The real serializers and the v1 schema for export; an export contract change only as a recorded additive decision (C-03, C-105).
- **The parser pattern of P5.07**:
  - validate the input before any request
  - one deadline per parse from the consumer's options, created from the injected `TimeProvider`
  - failure precedence: caller cancellation, then the deadline, then any other failure
  - stable error and notice IDs, with log messages that carry the station or source and the retrieval time in UTC, never measurement values
  - a bounded request count
- The retrieval boundary (`ICatalogDownloader` or an equivalent bounded downloader) with consumer `HttpClient`, size and time bounds and no retries.

## 5. Responsibilities of later phases

- P7: MOSMIX forecasts (full parsing, time alignment, units; re-verifies SC-20 against the schema).
- P8: MQTT payloads and topics, including the observation datasets of P5 and P6.
- P3 (after P8):
  - CLI `--ParseDWD` and `--ParseOstHessen`
  - configuration, exit codes and broker transport
  - the decision whether the example CLI exposes the MOSMIX_S refresh (SC-22, needs a requirement change)
- P9: publication, package ID and feed, hardware release acceptance (REQ-084).

## 6. Working rules

- **Roles**: qwen3-coder:30b generates code, tests, scripts and data through `/api/chat` with real API references and a file allowlist. The orchestrating agent plans, writes all documentation (user decision dated 2026-10-24), reviews, runs builds, tests and coverage and records authorship (AGENTS.md).
- **Reviews**: contract decisions are reviewed by the OpenAI Codex CLI before the user approves them; implementations and records get a five-angle Codex review after the agent's own checks.
- **Tests**: test-first with recorded Red evidence; MSTest, Moq (strict, `VerifyAll()` in `[TestCleanup]`), FluentAssertions; standard tests offline; live checks only explicitly enabled.
- **Coverage**: the gate compares unrounded native block-coverage ratios (REQ-086). Below a 100 % baseline every code PR must strictly improve the ratio against the remeasured target. At the end of P5 only three blocks of `WeatherParser.Parsing.dll` are uncovered (7491/7494), so new code needs tests for every block, including argument checks. Unreachable statements fail the strict gate.
- **Pull requests**: draft PR early, CI on every push, Auto-fix on, ready for review at phase end, the user merges.

## 7. Open items and limitations

- The limitations of the [P5.11 acceptance record](P5_11_ACCEPTANCE.md) section 4 stay open with their owners. Relevant to P6:
  - the builder and root-validation duplication (equivalence tests guard it)
  - the coarse XML cancellation (C-10)
  - the hosted `debian13-arm64` container is not target evidence
  - the package is not byte-reproducible
- OsthessenWetter source conditions and attribution are not yet clarified (P6, REQ-092).
