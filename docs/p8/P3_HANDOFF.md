# P3 Handoff – From the Complete Library to the Example CLI

**Status (2026-10-29): written with the P8.10 acceptance record; the user accepted P8 and the complete library on 2026-10-29 on candidate `ce166eb`; in effect once pull request #14 is merged into `develop`. P3 starts only on a separate instruction of the user.**

## 1. Foundation

- Integration target for P3: the `develop` merge commit of pull request #14 (recorded after the merge) or its documentation-only successor; the accepted implementation candidate is named in the [P8.10 acceptance record](P8_10_ACCEPTANCE.md).
- Platform: .NET SDK 10.0.402 (`global.json`), `net10.0`; verified on Windows x64, in hosted CI and on the NanoPi R76S (Debian 13, `linux-arm64`), [P8.09 record](P8_09_INTEGRATED_VERIFICATION.md).
- Contracts in force: the [P2 export contract](../p2/P2_EXPORT_CONTRACT.md) (v1 frozen, additive changes only), the [P4 selection contract](../p4/P4_SELECTION_CONTRACT.md), the [P5 observation contract](../p5/P5_OBSERVATION_CONTRACT.md), the [P6 OsthessenWetter contract](../p6/P6_OSTHESSEN_CONTRACT.md), the [P7 forecast contract](../p7/P7_FORECAST_CONTRACT.md) and the [P8 MQTT contract](P8_MQTT_CONTRACT.md) MQ-01 to MQ-15.
- Package: `WeatherParser.Parsing` `0.1.0-initial`, unpublished; the CLI consumes it as a package reference (REQ-012, C-112), never as copied source.

## 2. What the library provides

- Station and location selection (`ICdcStationSelector`, `IMosmixLocationSelector`, the MOSMIX_S evidence refresh `IMosmixSEvidenceRefresher`), DWD observations (`IDwdObservationParser`), OsthessenWetter observations (`IOsthessenWetterParser`), MOSMIX forecasts (`IMosmixLParser`, `IMosmixSParser`), the export entities with `IJsonExportSerializer` and `IXmlExportSerializer` and the packaged XSD, and since P8 the MQTT message serializer `IMqttMessageSerializer`; everything registered by `AddWeatherParsing` with the consumer registrations listed in the README ("MQTT serializer (library)", "Consumer registration").
- `IMqttMessageSerializer.SerializeAsync<TRoot>` returns an `MqttMessage` (topic from the consumer's template, the JSON export bytes as payload, QoS, retain, message type) or fails with an `MqttMessageException` ID of MQ-13; topic templates, the uniqueness rules, the payload bound and the duplicate rule are documented in the README and the MQTT contract.

## 3. P3 responsibilities (DEVELOPMENT_PLAN.md P3, REQ-060 to REQ-073)

- The example CLI as a consumer of the package: exactly one command per invocation (`--SelectDWD`, `--ParseDWD`, `--ParseOstHessen`, `--SelectMOSMIX`, `--ParseMOSMIX --Product MOSMIX_S|MOSMIX_L`), options validated before retrieval, deterministic configuration and CLI precedence, the configuration format, location and environment variables (REQ-064).
- `--Output mqtt|json|xml` (default `mqtt`) and `--OutputFile` (REQ-065, REQ-066): JSON and XML need no broker; one complete document; no silent overwrite; logs on stderr only (REQ-073).
- The example MQTT transport (REQ-060, REQ-062, REQ-063): broker, port, TLS (never disabled generally), credentials outside Git and process lists, publication of the library's `MqttMessage` unchanged (topic, payload, QoS, retain), success only after the QoS-1 acknowledgment, bounded connection and publication timeouts, controlled cancellation, bounded retries, partial-failure summaries and a nonzero exit when any requested station failed; the topic configuration supplied to `MqttTopicOptions` (the REQ-061 examples are configuration, not defaults).
- Exit codes, CLI cancellation and timeout bounds, error logs with UTC, severity, stable ID, command and source (REQ-072).
- The MOSMIX_S evidence-refresh exposure in the CLI (SC-22, owner the user and P3).

## 4. Reuse obligations

- Use the library's services through the package only; no second serializer, no hand-built JSON, no topic building outside `IMqttMessageSerializer`; the same export entities for JSON, XML and MQTT.
- The transport publishes `MqttMessage` as produced; it neither changes the topic nor re-serializes the payload; a message is never split, so a forecast run stays one retained message.
- Duplicate handling follows MQ-15 (byte-identical payloads); consumers are told that timestamps alone are no duplicate key.

## 5. Responsibilities of later phases

- P9: publication, package ID and feed, hardware release acceptance, release documentation of the source conditions and limitations, package reproducibility.

## 6. Working rules

- The AGENTS.md rules apply unchanged, including "Phase implementation" (work alone between approvals; the pull request is marked ready for review and its automatic Codex review is fixed to 0 findings before every user approval or acceptance) and "Plan generation" for the P3 plans.
- qwen3-coder (Ollama configuration `qwen3-coder-custom`) generates code, tests, scripts and data from briefs with the real API declarations; P8 lessons: give qwen every declaration it uses (missing declarations led to invented members), check every HTTP test double (one generated test registered a real `HttpClient`), lone surrogates are never `[DataRow]` values (attribute strings are UTF-8 metadata), write non-ASCII test strings as `\u` escapes, check for weakened assertions (`ContainInOrder`), and remove unreachable defensive arms, which fail the block-coverage gate.
- Standard tests stay offline; broker tests are explicitly enabled integration tests against a test broker and never publish production messages (AGENTS.md "Tests").

## 7. Open items and limitations

- **OsthessenWetter values must not be published (OW-14)** until the user records the operator's consent or other clarified conditions; the CLI must not publish them by default.
- The limitations of the [P8.10 acceptance record](P8_10_ACCEPTANCE.md) section 4 stay open with their owners, among them the residual of "DI under load" beyond the tested mix, the coarse XML cancellation (C-10), the builder and root-validation duplication, the hosted ARM64 container not being target evidence and the package not being byte-reproducible.
