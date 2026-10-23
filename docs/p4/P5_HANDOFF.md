# P5 Handoff – DWD Observations

Status: prepared with the P4.12 acceptance record on 2026-10-23; in effect once P4 is accepted and merged ([P4.12 acceptance record](P4_12_ACCEPTANCE.md)); P5 starts only on a separate instruction of the user.

## 1. Accepted foundation

- Verified implementation candidate `98122e560370e9de781bd4e0e3ebe0aca270e3d8` on branch `features/develop/P4`, draft pull request #5 into `develop` (PR base `b7da8288d8746bc6e034222c0467a8fcebba55b3`); P4.11 outcome READY ([P4.11 record](P4_11_INTEGRATED_VERIFICATION.md)). The P5 integration target is the merge commit of pull request #5, recorded after the merge.
- Package `WeatherParser.Parsing` `0.1.0-initial` (not published); contents `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE`, [`schemas/export/v1/weatherparser-export-v1.xsd`](../../schemas/export/v1/weatherparser-export-v1.xsd).
- Native block coverage of `WeatherParser.Parsing.dll`: 6484/6487 (99.95 %) on Windows and on the NanoPi R76S; the next PR's gate compares against the then-remeasured base.
- Contracts: export contract v1 [P2_EXPORT_CONTRACT.md](../p2/P2_EXPORT_CONTRACT.md) (C-01 to C-119; v1 frozen, only additive optional members, C-03, C-105) and the selection contract [P4_SELECTION_CONTRACT.md](P4_SELECTION_CONTRACT.md) (SC-01 to SC-23); output schema [`schemas/export/v1/weatherparser-export-v1.xsd`](../../schemas/export/v1/weatherparser-export-v1.xsd).
- Evidence: [P4_VERIFICATION.md](P4_VERIFICATION.md); platform summary [COMPATIBILITY.md](../COMPATIBILITY.md).

## 2. What P4 provides

- Coordinates and search area: `ICoordinateParser` (decimal degrees and DMS with hemisphere), `ISearchParameterFactory` (one validation policy, SC-11), `IDistanceCalculator` (Vincenty with spherical fallback, SC-06), `ISearchAreaFilter` (inclusive distance and altitude limits).
- CDC: `ICdcStationCatalogReader` (station lists, SC-02, SC-10), `ICdcNowEvidenceReader` (secure ZIP/CSV reading of the `now` files, SC-19), `ICdcAvailabilityDeriver` (two-stage availability with the product mapping of SC-03: `TT_10` temperature, `RF_10` humidity, `PP_10` station pressure, `FF_10` mean wind speed, `DD_10` wind direction, `RWS_10` 10-minute precipitation sum, `GS_10` 10-minute radiation sum).
- MOSMIX: `IMosmixLocationCatalogReader` (SC-04), `IMosmixLEvidenceReader` (secure KMZ/KML reading, SC-20), `IMosmixSEvidenceRefresher` and `IMosmixEvidenceCache` (MOSMIX_S evidence index, SC-22), `IMosmixAvailabilityResolver` (per-product availability with evidence timestamps, SC-05 option A, SC-23).
- Retrieval boundary: `ICatalogDownloader` (consumer `HttpClient`, 30 s per request, size bounds, no retries, classification of SC-09), `ICdcCatalogSource`, `IMosmixCatalogSource`, endpoint options `CdcCatalogEndpoints` and `MosmixCatalogEndpoints` with the SC-18 limits and SC-22 settings, validated at construction (SC-12).
- Selection services: `ICdcStationSelector`, `IMosmixLocationSelector` returning `CdcStationList` / `MosmixLocationList`; notice IDs in `SelectionNoticeIds` and `CatalogDiagnostic`; error IDs per SC-17.
- All services registered by `AddWeatherParsing` (singletons, C-107); consumers register endpoint options, `HttpClient`, `TimeProvider` and logging (README.md section "Station and location selection (library)").

## 3. P5 responsibilities (DEVELOPMENT_PLAN.md P5)

- Station resolution from the explicitly configured station: a missing station fails before any HTTP or MQTT; never the nearest or a substitute station.
- Bounded retrieval with controlled missing-product behavior.
- CSV/ZIP parsing of the DWD 10-minute observation files, verified units, quality codes, period mappings, DWD missing markers (for example `-999`) versus a real zero, distinct measurement times.
- Normalization through the existing [`ISourceTimeNormalizer`](../../src/WeatherParser.Parsing/Time/ISourceTimeNormalizer.cs) and [`IObservationDatasetBuilder`](../../src/WeatherParser.Parsing/Observations/IObservationDatasetBuilder.cs); export through the real serializers [`IJsonExportSerializer`](../../src/WeatherParser.Parsing/Serialization/IJsonExportSerializer.cs) and [`IXmlExportSerializer`](../../src/WeatherParser.Parsing/Serialization/IXmlExportSerializer.cs) under the frozen v1 contract and the output schema.
- Offline tests for headers, formats, wrong IDs, units, `-999` and different times; live-data quality recorded separately (offline versus live evidence).
- CLI configuration precedence and broker transport stay with P3.

## 4. Reuse obligations (SC-21)

- P5 builds its DWD 10-minute observation parsing on the archive and format handling of `ICdcNowEvidenceReader` (SC-19), extending it through shared internal components instead of writing a second parser for the same format.
- P7 builds its MOSMIX parsing on the secure KMZ and XML handling of `IMosmixLEvidenceReader` (SC-20) and the streaming reader of the MOSMIX_S evidence refresh (SC-22), extending them through shared internal components instead of writing a second parser for the same format.

## 5. Responsibilities of later phases

- P6: OsthessenWetter observations with timestamp-quality markers. P7: MOSMIX forecasts (full parsing, time alignment, units; re-verifies SC-20 against the schema). P8: MQTT payloads and topics. P3 (after P8): CLI, configuration, exit codes, broker transport, and the decision whether the example CLI exposes the MOSMIX_S refresh (SC-22, needs a requirement change). P9: publication, package ID and feed, hardware release acceptance (REQ-084).

## 6. Working rules

- qwen3-coder:30b generates through `/api/chat` with real API references and a file allowlist; the orchestrating agent plans, reviews, runs builds, tests and coverage and records authorship (AGENTS.md).
- Contract decisions are reviewed by the OpenAI Codex CLI before the user approves them; implementations get a five-angle Codex review after the agent's own checks.
- Test-first with recorded Red evidence; MSTest, Moq (strict, `VerifyAll()` in `[TestCleanup]`), FluentAssertions; standard tests offline; live checks only explicitly enabled (the P4 live-check runner in `tests/WeatherParser.Parsing.Tests/Live/` shows the pattern).
- The coverage gate compares unrounded native block-coverage ratios (REQ-086): below a 100 % baseline every code PR must strictly improve the ratio against the remeasured target. At the end of P4 only three blocks of `WeatherParser.Parsing.dll` are uncovered (6484/6487), so new code needs tests for every block, including constructor argument checks.
- Draft PR early, CI on every push, Auto-fix on, ready for review at phase end, the user merges.

## 7. Open items and limitations

- Unit conversions, quality codes and full observation parsing for the SC-03 quantities (P5).
- MOSMIX element verification and freshness per product (P7).
- Example-CLI exposure of the MOSMIX_S refresh (P3, user decision).
- The hosted `debian13-arm64` job is a container, not target evidence; target evidence comes from the NanoPi R76S; release hardware acceptance stays with P9 (REQ-084).
- The live check of 2026-10-23 is a single snapshot and is not repeated (SC-16).
- The package is not byte-reproducible across builds; C-116 compares packed and restored hashes within one run.
- Inherited P2 limitations: coarse XML cancellation (C-10); builders and root validation implement the same rules separately (equivalence tests guard divergence).
- Inherited from P2: forecast-only quantities (gusts, probabilities, totals) are not yet in the export contract; P7 adds them through additive contract decisions.
- Inherited from P2: the `de_DE.UTF-8` system locale is not installed on the target; culture checks use the ICU data of .NET.
