# P7 Handoff – MOSMIX Forecasts

Status: prepared with the P6.11 acceptance record on 2026-10-26 for the verified candidate `4373399` ([P6.11 acceptance record](P6_11_ACCEPTANCE.md)). It takes effect only after the user has accepted P6 and merged pull request #9 into `develop`; P7 starts only on a separate instruction of the user.

## 1. Foundation

- **Candidate**: verified implementation candidate `4373399b278e65097452fe18d7029fee5461bdb1` on branch `features/develop/P6`, pull request #9 into `develop` (PR base `8d8079a4238514f7c9bcfc3846829e2408938ae3`). P6.10 outcome in the [P6.10 record](P6_10_INTEGRATED_VERIFICATION.md) section 7.
- **P7 integration target**: the merge commit of pull request #9 on `develop`, or its documentation-only successor at P7 branch creation; P7.01 records the `develop` head it branches from and classifies every commit since that merge.
- **Package**: `WeatherParser.Parsing` `0.1.0-initial` (not published); contents `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE`, [`schemas/export/v1/weatherparser-export-v1.xsd`](../../schemas/export/v1/weatherparser-export-v1.xsd). New dependency since P5: AngleSharp 1.8.5 (MIT, [DEPENDENCIES.md](../../DEPENDENCIES.md)).
- **Coverage**: native block coverage of `WeatherParser.Parsing.dll` is 8475/8478 (99.96 %) on candidate `4373399`; the next PR's gate compares against the then-remeasured base.
- **Contracts**:
  - export contract v1 [P2_EXPORT_CONTRACT.md](../p2/P2_EXPORT_CONTRACT.md) (C-01 to C-119; v1 frozen, only additive optional members, C-03, C-105; unchanged by P6)
  - selection contract [P4_SELECTION_CONTRACT.md](../p4/P4_SELECTION_CONTRACT.md) (SC-01 to SC-23)
  - observation contract [P5_OBSERVATION_CONTRACT.md](../p5/P5_OBSERVATION_CONTRACT.md) (OC-01 to OC-14)
  - OsthessenWetter contract [P6_OSTHESSEN_CONTRACT.md](P6_OSTHESSEN_CONTRACT.md) (OW-01 to OW-16, OW-11 amended)
- **Evidence**: [P6_VERIFICATION.md](P6_VERIFICATION.md); platform summary [COMPATIBILITY.md](../COMPATIBILITY.md).

## 2. What P6 provides

- **OsthessenWetter parser**: `IOsthessenWetterParser` / `OsthessenWetterParser` with `Task<OsthessenWetterParseResult> ParseAsync(string stationNamePattern, CancellationToken cancellationToken)` (OW-08), returning datasets and station failures, and the services it composes:
  - `IStationNamePatternMatcher` (case-insensitive glob over station names, OW-09)
  - `IOsthessenWetterPageSource` (one bounded request through `ICatalogDownloader` with the additive `CatalogDeadlineKind.OsthessenWetter`, OW-10)
  - `IOsthessenWetterPageReader` (AngleSharp without scripts or resource loading, OW-01, OW-02, OW-05, OW-11, OW-12)
  - `IOsthessenWetterStationIdentity`, `IOsthessenWetterStationMetadataLookup`, `IOsthessenWetterValueReader`, `IOsthessenWetterTimeReader`, `IOsthessenWetterObservationMapper` (OW-03 to OW-07)
- **Options and errors**: consumer-registered `OsthessenWetterEndpoints` (`PageUri`) and `OsthessenWetterOptions` (`PageDeadline`, default 60 s, permitted (0, 1 h]; `Stations` with consumer-supplied coordinates); `OsthessenWetterException` with its error IDs and `StationFailures`; notice and station-failure IDs in `OsthessenWetterNoticeIds` (OW-13).
- **Shared building blocks used by a second provider flow**: `IObservationDatasetBuilder`, `ISourceTimeNormalizer.InferDate`, the real serializers, `ICatalogDownloader`.
- **Registration**: all services are registered by `AddWeatherParsing` (singletons, C-107). Every consumer of `AddWeatherParsing` now also registers `OsthessenWetterEndpoints` and `OsthessenWetterOptions` (README section "OsthessenWetter observations (library)").
- **Patterns new in P6**:
  - a parse result with per-item failures beside the successful datasets, and a rejection error that carries the failures (`OSTHESSEN-NO-USABLE-STATION`)
  - the additive deadline kind of the shared downloader, mapped to a provider-specific error ID while the existing kinds stay unchanged
  - `retrieved_at` sampled after the response has arrived (OW-16)
  - page-level notices logged before any later failure can hide them
  - the opt-in live check with a budget of one request, a value-free and escaped summary, placeholder metadata in the test project only, and a usable-result check (`WEATHERPARSER_LIVE_CHECK_MODE=osthessen`)

## 3. P7 responsibilities (DEVELOPMENT_PLAN.md P7)

- Two separate parsers with separate interfaces, `MosmixSParser` / `IMosmixSParser` and `MosmixLParser` / `IMosmixLParser`, each with its own constructor URLs and DI configuration; shared KML/KMZ helpers allowed, no switchable single parser, no silent fallback between products.
- KML/KMZ with namespaces, location identity, issue metadata, parameter series and the forecast timeline, derived from the binding [pointforecast_dwd_extension_V1_0.xsd](https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd) (record revision and checksum); missing values must not shift timestamps.
- Separate `issued_at`, `retrieved_at` and `valid_at`, never `measured_at`; the actual horizon from the file timeline; freshness limits per product; energy and rain periods only with justified semantics.
- XML without DTDs or external entities; bounded archives.
- Both exports equivalent; tests for wrong IDs, stale runs, corrupt structure and missing parameters.

## 4. Reuse obligations

- `ICatalogDownloader` (or an equivalent bounded downloader) with the consumer's `HttpClient`, size and time bounds, no retries; a new deadline kind only additively, with the existing mappings re-tested.
- The forecast-run builder of P2 for assembly; no parallel builder. The real serializers and the v1 schema; an export contract change only as a recorded additive decision (C-03, C-105).
- The parser pattern of P5.07 and P6.07: input validation before any request; one deadline per parse from the consumer's options and the injected `TimeProvider`; precedence caller cancellation, then the deadline, then every other failure, at every checkpoint; stable IDs; logs without values, page or file text and without endpoint queries.
- The existing MOSMIX catalog, evidence and selection services of P4 for the location catalog and the MOSMIX_S evidence cache.

## 5. Responsibilities of later phases

- P8: MQTT payloads and topics, including the observation datasets of P5 and P6. **OsthessenWetter values must not be redistributed (for example over MQTT) until the user records the operator's consent or other clarified conditions (OW-14).**
- P3 (after P8): CLI `--ParseDWD`, `--ParseOstHessen`, `--SelectMOSMIX`, `--ParseMOSMIX --Product`; configuration, exit codes and broker transport.
- P9: publication, package ID and feed, hardware release acceptance (REQ-084).

## 6. Working rules

- **Roles**: qwen3-coder:30b generates code, tests, scripts and data through `/api/chat` with real API references and a file allowlist; the orchestrating agent plans, writes all documentation, reviews, runs builds, tests and coverage and records authorship (AGENTS.md). In P6 qwen repeatedly implemented members briefed as stubs, wrote literal non-ASCII characters and `\u` escapes where literal backslashes were needed, and weakened list assertions (`ContainInOrder`); check every result for these.
- **Reviews**: contract decisions are reviewed by Codex before the user approves them; implementations and records get a five-angle Codex review after the agent's own checks.
- **Tests**: test-first with recorded Red evidence; MSTest, Moq (strict, `VerifyAll()` in `[TestCleanup]`), FluentAssertions 8 (exact lists with `Equal`); standard tests offline; live checks only explicitly enabled and authorized.
- **Coverage**: below a 100 % baseline every code PR must strictly improve the native block ratio against the remeasured target; with three uncovered blocks left, new code needs tests for every block, including compiler-generated pattern-matching branches.
- **Pull requests**: draft PR early, CI on every push, Auto-fix on, ready for review at phase end, the user merges.

## 7. Open items and limitations

- The limitations of the [P6.11 acceptance record](P6_11_ACCEPTANCE.md) section 4 stay open with their owners. Relevant to P7: the builder and root-validation duplication (equivalence tests guard it), the coarse XML cancellation (C-10), the hosted `debian13-arm64` container is not target evidence, the package is not byte-reproducible, DI under load.
- OsthessenWetter source conditions are not clarified (OW-14): the library is accepted only as implemented and tested with synthetic pages; production use and redistribution stay open.
