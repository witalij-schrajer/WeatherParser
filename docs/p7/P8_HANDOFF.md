# P8 Handoff – From the MOSMIX Forecasts to the MQTT Serializer and Library Integration

**Status (2026-10-28): written with the P7.12 acceptance record; P7 accepted by the user on 2026-10-28 on candidate `2f1115b`; it becomes the P8 foundation once the user merges pull request #11. P8 starts only on a separate instruction of the user.**

## 1. Foundation

- Integration target after the merge: the `develop` merge commit of pull request #11 (recorded in the [P7.12 acceptance record](P7_12_ACCEPTANCE.md) section 8 once the user has merged). Until then the P7 candidate `a36d15a` on branch `features/develop/P7` is the reference ([P7.11 record](P7_11_INTEGRATED_VERIFICATION.md)).
- Platform: .NET SDK 10.0.402 (`global.json`), `net10.0`; verified on Windows x64, in hosted CI and on the NanoPi R76S (Debian 13.3, `linux-arm64`), [compatibility matrix](../COMPATIBILITY.md) "P7 scope".
- Coverage: the gate on the P7 candidate is 10228/10231 blocks against the P7 baseline 8486/8489; the P8 baseline is remeasured on the merged target in P8.01, as every phase does.
- Contracts in force: the [P2 export contract](../p2/P2_EXPORT_CONTRACT.md) with C-120 to C-127 (schema SHA-256 `6288f1a7dad2909477f9e104621bd8fd04706e09e115742832efba8e2628a0d2`), the [P4 selection contract](../p4/P4_SELECTION_CONTRACT.md), the [P5 observation contract](../p5/P5_OBSERVATION_CONTRACT.md), the [P6 OsthessenWetter contract](../p6/P6_OSTHESSEN_CONTRACT.md) and the [P7 forecast contract](P7_FORECAST_CONTRACT.md) MF-01 to MF-18.

## 2. What P7 provides

- **Two forecast parsers** behind separate interfaces: `IMosmixLParser` / `MosmixLParser` and `IMosmixSParser` / `MosmixSParser`, each `Task<MosmixParseResult> ParseAsync(string mosmixId, CancellationToken cancellationToken)` (MF-02), validating the ID first (`^[A-Z0-9]{4,5}$`, MF-03), one bounded request per parse without retries (MF-05), no fallback between products (REQ-109).
- **Results**: `MosmixParseResult` (`Run` as the P2 `ForecastRun`, `OmittedValues`, `NotExportedElements`, `AbsentElements`, `Freshness` as `MosmixRunFreshness`, `RunAge`); the run carries `product` (`MOSMIX_L` / `MOSMIX_S`), `source` `dwd-mosmix-l:<id>` or `dwd-mosmix-s:<id>` (C-68), `issued_at`, `retrieved_at`, `location`, `forecasts` with `valid_at` and `values`, and the additive `run_age_exceeded` marker (C-120).
- **Options and endpoints** (consumer-registered, MF-04): `MosmixLForecastEndpoints.SingleStationsDirectory`, `MosmixLForecastOptions` (`ParseDeadline` default 60 s, `MaxRunAge` required, (0, 240 h]), `MosmixSForecastEndpoints.AllStationsUri`, `MosmixSForecastOptions` (`ParseDeadline` default 5 min, also the request bound, `MaxRunAge` required). Invalid values fail with `MOSMIX-OPTIONS-INVALID` on first resolution.
- **Errors and notices**: `MosmixForecastException` (`ErrorId`, `Product`, `OmittedValues`, `NotExportedElements`) with the MF-14 IDs in `MosmixForecastErrorIds`; `MosmixForecastFormatException` from the reader; notices in `MosmixNoticeIds` (`MOSMIX-RUN-STALE`, `MOSMIX-ELEMENT-ABSENT`, `MOSMIX-ELEMENT-NOT-EXPORTED`, `FORECAST-VALUE-OMITTED`); every log entry with the stable ID, UTC, product, MOSMIX ID and the sanitized endpoint (MF-15).
- **Services it composes** (all behind interfaces, registered by `AddWeatherParsing`): `IMosmixLForecastSource`, `IMosmixSForecastSource` (with `SanitizedEndpoint`), `IMosmixForecastDocumentReader` (secure KMZ/KML reading: no DTD or external entities, bounded archive and member, CRC-32, placemark selection while streaming), `IMosmixElementMapper`, `IMosmixUnitScaler`, `IMosmixParsePipeline` (the shared, product-neutral parse flow configured per product through `MosmixParseRequest`), the P2 `IForecastRunBuilder` and the real serializers.
- **Export contract additions** (P7.03): `run_age_exceeded`, `wind_gust_kmh` (maximum), `precipitation_amount_mm` (sum), `IntervalAggregation.Maximum`/`.Sum`, `ForecastRunRejectedException.OmittedValues`; schema regenerated.
- **Patterns new in P7**: a product-neutral pipeline service configured by a request object instead of a product switch; the deadline doubling as the request bound for the large product; the reader selecting one placemark while reading the whole member to its end; the offline resource check (generate the recorded workload, measure alone in a fresh process, SHA-256-bound companion, repository output guard) beside the opt-in live checks (`WEATHERPARSER_LIVE_CHECK_MODE=mosmix-l` / `mosmix-s`).

## 3. P8 responsibilities (DEVELOPMENT_PLAN.md P8)

- The generic, source-independent MQTT serializer: topic templates and their mapping to message types from consumer configuration; validation of resolved topics; defined failures for missing mappings and unresolved placeholders; one message with topic, JSON payload, QoS and retain; no broker connection and no credentials in serialization.
- Forecast runs over MQTT: separate topics per product (C-68 sources distinguish `MOSMIX_L` and `MOSMIX_S` so that retained state is never replaced across products); the complete run retained as one message (REQ-062); never a forecast as an observation and never with `measured_at`.
- Observation datasets of P5 and P6 over MQTT under the same serializer; **OsthessenWetter values must not be redistributed until the user records the operator's consent or other clarified conditions (OW-14)**.
- Complete library integration through actual package consumption: all parsers, output entities, JSON/XML/XSD and DI, without CLI or broker; the acceptance of the whole library before P3.

## 4. Reuse obligations

- The same export entities for JSON, XML and the MQTT JSON payload (`System.Text.Json`, no manually concatenated documents); the v1 schema stays the XML contract; an export change only as a recorded additive decision (C-03, C-105).
- `MosmixParseResult.Run` and the observation datasets as the payload sources; no re-parsing and no second model.
- The parser pattern of P5.07, P6.07 and P7.07 for any new doing class: an interface, constructor injection, registration in `AddWeatherParsing`, validation before any side effect, stable IDs, logs without values or secrets (broker credentials never enter the library).
- Tests offline with the real serializers; MQTT transport is not part of P8 (P3 example transport).

## 5. Responsibilities of later phases

- P3 (after P8): CLI `--ParseDWD`, `--ParseOstHessen`, `--SelectMOSMIX`, `--ParseMOSMIX --Product MOSMIX_S|MOSMIX_L`, configuration, exit codes, example broker transport with QoS-1 acknowledgment; the MOSMIX_S evidence-refresh exposure.
- P9: publication, package ID and feed, hardware release acceptance (REQ-084), release documentation of source conditions and limitations.

## 6. Working rules

- **Roles**: qwen3-coder (Ollama configuration `qwen3-coder-custom`, the same 30B weights) generates code, tests, scripts and data through `/api/chat` with real API references and a file allowlist; the orchestrating agent plans, writes all documentation, reviews, runs builds, tests and coverage and records authorship (AGENTS.md). In P7 qwen repeatedly dropped `DataRow` attributes and using directives, invented API members (`Status`, `MosmixProduct.L`), hard-coded one product in shared helpers, inverted a guard condition and dropped planned tests from a Red brief; check every result for these, and count a brief as failed after two precise rounds.
- **Reviews**: contract decisions are reviewed by Codex before the user approves them; implementations and records get a five-angle Codex review after the agent's own checks; the review text is copied verbatim into every prompt.
- **Tests**: test-first with recorded Red evidence (the P7.06 deviation shows the cost of noticing a gap only after Green: name every planned test in the Red brief and compare the generated file against the plan before Green); MSTest, Moq (strict, `VerifyAll()` in `[TestCleanup]`), FluentAssertions 8; standard tests offline; live checks only explicitly enabled and authorized.
- **Coverage**: below a 100 % baseline every code PR must strictly improve the native block ratio against the remeasured target; with three uncovered blocks left, new code needs tests for every block, including compiler-generated pattern-matching and lifted-nullable branches.
- **Pull requests**: draft PR early, CI on every push, Auto-fix on, ready for review at phase end on the user's instruction, the user merges.
- **Device runs**: a plain clone on the NanoPi R76S builds the test project only with an empty `artifacts/local-nuget` directory present (the NuGet configuration names it; `ci-local.ps1` creates it in its own work directory).

## 7. Open items and limitations

- The limitations of the [P7.12 acceptance record](P7_12_ACCEPTANCE.md) section 4 stay open with their owners; relevant to P8: the product-specific sources for topics, the complete retained run, the registration consequence of the four MOSMIX option types for every consumer, the schema compatibility note (C-105, C-121), the OsthessenWetter redistribution restriction (OW-14).
- The P7.06 TDD deviation was disposed of by the user's phase acceptance of 2026-10-28 (covered as recorded, [acceptance record](P7_12_ACCEPTANCE.md) section 6); it stays documented in the verification record section 6.
- Inherited: the builder and root-validation duplication (equivalence tests guard it), the coarse XML cancellation (C-10), the hosted `debian13-arm64` container is not target evidence, the package is not byte-reproducible, DI under load.
