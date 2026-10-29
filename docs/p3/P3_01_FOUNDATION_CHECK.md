# P3.01 – Foundation Check

**Status (2026-10-29): in progress (draft with the seed commit); outcome in section 7.** Plan: [P3_01_FOUNDATION_CHECK_PLAN.md](P3_01_FOUNDATION_CHECK_PLAN.md).

User instruction (2026-10-29, chat): "P3 Implmentation".

## 1. Records recheck and commit classification (S1)

- P8 and the complete library accepted by the user on 2026-10-29 on candidate `ce166eb` ([P8.10 acceptance record](../p8/P8_10_ACCEPTANCE.md)); pull request #14 merged into `develop` on 2026-10-29, merge commit `21b72106b3fd422c1a04020dd1d4288e6f99df74` ([P3 handoff](../p8/P3_HANDOFF.md) section 1).
- Pull request #15 (P8 post-merge records and the P3 plans) merged by the user on 2026-10-29T20:28:40Z, merge commit `e623a834bdb2902cec930258a62a48302f626716`, head `dca1ff118270ef92f8aeebd30ebed55108e919b0` (read with `gh pr view 15` after `git fetch`). Plan risk (a) did not occur: the P3 plans are on `develop`.
- `git merge-base --is-ancestor 21b7210 origin/develop`: true. `origin/develop` = `e623a83`.
- `git diff --name-only 21b7210 origin/develop -- src/ tests/ tools/ scripts/ .github/ *.props *.json` is empty: no production code, test, tool, script, CI configuration or package pin changed since the P8 merge. The 22 changed files are Markdown only: AGENTS.md, DEVELOPMENT_PLAN.md, PHASE_P1_PLAN.md, PHASE_P3_PLAN.md, PHASE_P8_PLAN.md, README.md, `docs/p3/*_PLAN.md` (twelve step plans) and `docs/p8/*.md`.

| Commit | Subject | Classification |
| --- | --- | --- |
| `9dfa7ca` | P8.10 A11: post-merge checkpoint, P8 complete | Documentation (README.md changed: counts as code for the CI classification, packed readme) |
| `215a6fb` | PR #15 review round 1: record the README package check and ci-local run of the post-merge checkpoint | Documentation |
| `90a34da`, `e9ee284`, `2fbce83`, `7260ac5` | P3 phase plan and its review rounds 1 to 3 | Documentation |
| `5751c45`, `1a3f770`, `8728bf5`, `1ed4e94`, `58d7d29`, `0c12a22` | P3 step plans and their review rounds 1 to 5 | Documentation |
| `4fe7b63`, `97dc507`, `4a029be`, `da4c3b2`, `927a6da`, `b1574f4`, `ca662c1`, `dca1ff1` | Plan acceptance, review rounds 7 to 11 and the acceptance of the round-7, round-9 and round-10 changes | Documentation |
| `e623a83` | Merge pull request #15 | Merge |

- Effect on the contracts: none; no contract row changed. The handoff assumption "`21b7210` or its documentation-only successor" holds: the integration target is `e623a83`, a documentation-only successor of the P8 merge.
- Planning gate (PHASE_P3_PLAN.md section 5), first part: the phase plan and the step plans P3.01 to P3.12 are reviewed through the automatic Codex review of pull request #15 to 0 findings and accepted by the user on 2026-10-29, including the changes of rounds 7, 9 and 10. The second part (reconciliation of P3.03 to P3.12 with the approved contract) belongs to P3.02 S5.

## 2. Extraction (S2)

| Field | Value | Source |
| --- | --- | --- |
| Integration target | `e623a834bdb2902cec930258a62a48302f626716` (`develop` head at branch creation; documentation-only successor of the P8 merge `21b7210`, section 1) | `git rev-parse origin/develop` |
| Package identity | `WeatherParser.Parsing` `0.1.0-initial`, unpublished; contents `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE`, `schemas/export/v1/weatherparser-export-v1.xsd` and NuGet metadata | P3 handoff section 1, package check (section 4) |
| SDK / runtime | SDK 10.0.402 (`global.json`, `rollForward` `disable`); only the SDK is pinned | `global.json` |
| Coverage tools | Microsoft.CodeCoverage and dotnet-coverage as pinned in `Directory.Packages.props` and `.config/dotnet-tools.json` (unchanged since P8) | Files, section 1 |
| Contracts in force | Export v1 C-01 to C-127 (frozen; additive optional members only); selection SC-01 to SC-23; observation OC-01 to OC-14; OsthessenWetter OW-01 to OW-16; forecast MF-01 to MF-18; MQTT MQ-01 to MQ-15 | P3 handoff section 1 |
| CI rules in force | The three CI jobs run only when code changed (Markdown and `docs/` only skip them, root `README.md` counts as code); the automatic Codex review runs when a pull request is marked ready for review | AGENTS.md, CONTRIBUTING.md section 6 |
| P8 coverage at acceptance | 11102/11103 native blocks of `WeatherParser.Parsing.dll` on `ce166eb`; 11102/11103 on the post-merge checkpoint `9dfa7ca` for the target `21b7210` | P8.10 acceptance record, PHASE_P3_PLAN.md section 1 |
| Baseline for P3 | Remeasured in S4 | Section 5 |

### Consumed public API (read from the source on `e623a83`)

| Service | Member the CLI calls | Result |
| --- | --- | --- |
| `ICdcStationSelector` | `SelectAsync(SearchParameters, CancellationToken)` | `CdcStationList` |
| `IMosmixLocationSelector` | `SelectAsync(SearchParameters, CancellationToken)` | `MosmixLocationList` |
| `IMosmixSEvidenceRefresher` | `RefreshAsync(CancellationToken)` | `Task` (stores the MOSMIX_S evidence index through the configured cache) |
| `IDwdObservationParser` | `ParseAsync(string stationId, CancellationToken)` | `ObservationDataset` |
| `IOsthessenWetterParser` | `ParseAsync(string stationNamePattern, CancellationToken)` | `OsthessenWetterParseResult(IReadOnlyList<ObservationDataset> Datasets, IReadOnlyList<OsthessenWetterStationFailure> StationFailures)`; a station failure is `(string StationName, string ErrorId, string Reason)` |
| `IOsthessenWetterStationIdentity` | `ToStationId(string name)` | The OW-03 station ID slug; registered by `AddWeatherParsing` (input of CL-09, section 6) |
| `IMosmixLParser`, `IMosmixSParser` | `ParseAsync(string mosmixId, CancellationToken)` | `MosmixParseResult` with `Run` (`ForecastRun`), `OmittedValues`, `NotExportedElements`, `AbsentElements`, `Freshness`, `RunAge` |
| `ICoordinateParser` | `ParseLatitude(string)`, `ParseLongitude(string)` | `decimal` or `CoordinateFormatException` |
| `IJsonExportSerializer`, `IXmlExportSerializer` | `SerializeAsync<TRoot>(TRoot, Stream, CancellationToken)` | Writes one complete document |
| `IMqttMessageSerializer` | `SerializeAsync<TRoot>(TRoot, CancellationToken)` | `MqttMessage` (`Topic`, `Payload`, `QualityOfService`, `Retain`, `MessageType`) |
| `AddWeatherParsing` | `IServiceCollection` extension | Registers the services above; the consumer registers `HttpClient`, `TimeProvider`, `ILogger<>`, `CdcCatalogEndpoints`, `MosmixCatalogEndpoints`, `DwdObservationOptions`, `OsthessenWetterOptions`, `OsthessenWetterEndpoints`, `MosmixLForecastEndpoints`, `MosmixLForecastOptions`, `MosmixSForecastEndpoints`, `MosmixSForecastOptions` and `MqttTopicOptions` (thirteen types, README "MQTT serializer (library)") |

The README names the same services and the same thirteen consumer registrations; no member differs between the README and the source (plan risk (b) did not occur).

### Public exception types and error IDs

Exception types with an `ErrorId` (13): `CatalogFormatException`, `CatalogRetrievalException`, `CoordinateFormatException`, `DwdStationException`, `EvidenceCacheException`, `ObservationLimitException`, `OsthessenWetterException`, `SearchParameterException`, `SelectionLimitException`, `MosmixForecastException`, `MosmixForecastFormatException`, `MqttMessageException`, `ParserEndpointValidationException`. Exception types without an `ErrorId` (5): `ForecastRunRejectedException`, `ObservationDatasetRejectedException`, `QuantityConversionException`, `ExportSerializationException`, `SourceTimeNormalizationException` (graph or builder violations; for the CLI unexpected failures, CL-10 `CLI-UNEXPECTED`).

Error and notice IDs used in the library source (65, `grep` of the string literals of the form `WORD-WORD…` in `src/WeatherParser.Parsing`):
`CATALOG-FORMAT-INVALID`, `CATALOG-RECORD-SKIPPED`, `CATALOG-RETRIEVAL-FAILED`, `CATALOG-TIMEOUT`, `CATALOG-TOO-LARGE`, `CDC-EVIDENCE-INVALID`, `CDC-STATION-CONFLICT`, `CDC-STATION-NO-ALTITUDE`, `COORDINATE-INVALID`, `ENDPOINT-INVALID`, `EVIDENCE-CACHE-FAILED`, `EVIDENCE-CACHE-SETTINGS-INVALID`, `FORECAST-VALUE-OMITTED`, `MOSMIX-DEADLINE-EXCEEDED`, `MOSMIX-ELEMENT-ABSENT`, `MOSMIX-ELEMENT-NOT-EXPORTED`, `MOSMIX-EVIDENCE-INVALID`, `MOSMIX-FORMAT-INVALID`, `MOSMIX-ID-INVALID`, `MOSMIX-ID-UNKNOWN`, `MOSMIX-ISSUE-TIME-MISSING`, `MOSMIX-LOCATION-MISMATCH`, `MOSMIX-LOCATION-NO-ALTITUDE`, `MOSMIX-NO-USABLE-VALUE`, `MOSMIX-OPTIONS-INVALID`, `MOSMIX-RETRIEVAL-FAILED`, `MOSMIX-RUN-STALE`, `MOSMIX-S-EVIDENCE-MISSING`, `MOSMIX-S-EVIDENCE-STALE`, `MOSMIX-S-REFRESH-DEADLINE-EXCEEDED`, `MOSMIX-TOO-LARGE`, `MQTT-MAPPING-MISSING`, `MQTT-MESSAGE-TYPE-UNSUPPORTED`, `MQTT-OPTIONS-INVALID`, `MQTT-PAYLOAD-TOO-LARGE`, `MQTT-PLACEHOLDER-VALUE-INVALID`, `MQTT-SERIALIZATION-FAILED`, `MQTT-TOPIC-INVALID`, `OBSERVATION-DEADLINE-EXCEEDED`, `OBSERVATION-LIMIT-INVALID`, `OBSERVATION-PRODUCT-MISSING`, `OBSERVATION-READING-OMITTED`, `OSTHESSEN-DEADLINE-EXCEEDED`, `OSTHESSEN-FORMAT-CHANGED`, `OSTHESSEN-NO-USABLE-STATION`, `OSTHESSEN-OPTIONS-INVALID`, `OSTHESSEN-PAGE-UNAVAILABLE`, `OSTHESSEN-PATTERN-INVALID`, `OSTHESSEN-PATTERN-NO-MATCH`, `OSTHESSEN-STATION-ID-COLLISION`, `OSTHESSEN-STATION-METADATA-MISSING`, `OSTHESSEN-STATION-NO-READING`, `OSTHESSEN-STATION-OFFLINE`, `OSTHESSEN-TIME-UNRESOLVED`, `OSTHESSEN-VALUE-UNREADABLE`, `OSTHESSEN-WIND-ROW-UNMATCHED`, `OSTHESSEN-WIND-TABLE-MISSING`, `SEARCH-PARAMETER-INVALID`, `SELECTION-DEADLINE-EXCEEDED`, `SELECTION-EMPTY`, `SELECTION-LIMIT-INVALID`, `SELECTION-TOO-MANY-CANDIDATES`, `STATION-ID-INVALID`, `STATION-METADATA-CONFLICT`, `STATION-UNKNOWN`. Which of them are errors and which notices is documented per source in the README sections "Error and notice IDs" and "Error IDs"; the CLI passes them through unchanged (CL-10). The library prefix `MQTT-` is shared with the proposed CLI transport IDs (`MQTT-CONNECT-FAILED`, `MQTT-PUBLISH-FAILED`, `MQTT-PUBLISH-TIMEOUT`); none of them collides with a library ID (input of CL-10, section 6).

SDK servicing check: recorded in S4 (section 5).

## 3. Inherited items (S2)

Every open item of [P8_10_ACCEPTANCE.md](../p8/P8_10_ACCEPTANCE.md) section 4, [P3_HANDOFF.md](../p8/P3_HANDOFF.md) section 7 and every item inherited with owner P3 (the table of PHASE_P3_PLAN.md section 1 is the minimum):

| Item | Source | P3 disposition | Check |
| --- | --- | --- | --- |
| Example CLI, configuration, exit codes, CLI cancellation and timeout bounds | P2.12, P5.11, P6.11, P7.12, P8.10 section 4 | Done in P3 | CL rows, P3.03 to P3.09 |
| Which command fills which envelope collection; nonzero exit code of a partial result | C-84, C-87 | Decided in P3 | CL-09, CL-11, P3.07 |
| CLI error IDs of `ExportError` records | C-86 | Decided in P3 | CL-10, P3.06, P3.07 |
| Example broker transport (TLS, credentials, acknowledgment, timeouts, retries, partial-failure summary); broker behavior not shown by message tests | REQ-060, REQ-062, REQ-063, P8.10 section 4 | Done in P3 | CL-13 to CL-15, P3.08, P3.11 |
| Topic configuration supplied to `MqttTopicOptions` | P3 handoff section 3, MQ-09 | Done in P3 | CL-05, P3.05, P3.08 |
| OsthessenWetter values not published by default (OW-14) | P6.11, P8.10 section 4, P3 handoff section 7 | Done in P3 (CLI restriction); clarification of the source conditions stays with the user | CL-16, P3.07, P3.08 |
| MOSMIX_S evidence-refresh exposure | SC-22, P4.12 to P8.10 | Decided in P3 | CL-17, P3.07 |
| "DI under load" beyond the tested mix | P8.10 section 4 (owners P3, P9) | P3 part: one container per process, one command per process; long runs and other loads | P3.07; carried to P9 |
| Builder and root-validation duplication; coarse XML cancellation (C-10); hosted `debian13-arm64` container not target evidence; package not byte-reproducible | P5.11 to P8.10 | Carried forward unchanged; P3 adds no library change | Owners as recorded (P9 for reproducibility and release) |
| Package ID, feed, publication, release documentation | P8.10 section 4, P3 handoff section 5 | Carried forward | P9 |

No item is unmapped.

## 4. Branch, status passages and seed commit (S3)

- Branch `features/develop/P3` from `origin/develop` `e623a83`.
- Status passages "P3 started" in AGENTS.md, README.md, DEVELOPMENT_PLAN.md and PHASE_P1_PLAN.md, and the status line of PHASE_P3_PLAN.md.
- README package check on the working tree (CONTRIBUTING.md section 4): README.md SHA-256 `1280DDCF3E9992C913F030DD6B01798AFD771524CDE6E0B34E103B253344D3C3`, LICENSE and the export schema byte-identical in the package; packed SHA-512 `iq18P//yudsoJ2jILu8BJI7mngr1Ntuw+cR09wlZlqvUeo4v5+RkX6JKKLIN7yyhNx+T2COEKppTEDO1jvnqgQ==` equals the restored hash; package consumer 61/61; consumer lock restored afterwards.
- Seed commit, push and draft pull request: recorded in the verification record.

## 5. Baseline and coverage input (S4)

Recorded after the baseline run on the seed commit.

## 6. Plan adjustments (S5)

Recorded after S4.

## 7. Outcome

Pending.
