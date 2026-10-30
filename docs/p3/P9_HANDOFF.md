# P9 Handoff – From the Example CLI to Release Preparation

**Status (2026-10-31): written with the P3.12 acceptance record; P3 accepted by the user on 2026-10-31 on candidate `ec915fc`; in effect once the user has merged the P3 pull request #16 into `develop`. P9 starts only on a separate instruction of the user.**

## 1. Foundation

- Integration target for P9: the `develop` merge commit of pull request #16, or its documentation-only successor; the accepted implementation candidate is named in the [P3.12 acceptance record](P3_12_ACCEPTANCE.md).
- Platform: .NET SDK 10.0.402 (`global.json`), `net10.0`; verified on Windows x64, in hosted CI (Windows x64, Ubuntu x64, the Debian 13 ARM64 container) and on the NanoPi R76S (Debian 13.3, `linux-arm64`), [P3.11 record](P3_11_INTEGRATED_VERIFICATION.md).
- Contracts in force: the [P2 export contract](../p2/P2_EXPORT_CONTRACT.md) (v1 frozen, additive changes only), the [P4 selection contract](../p4/P4_SELECTION_CONTRACT.md), the [P5 observation contract](../p5/P5_OBSERVATION_CONTRACT.md), the [P6 OsthessenWetter contract](../p6/P6_OSTHESSEN_CONTRACT.md), the [P7 forecast contract](../p7/P7_FORECAST_CONTRACT.md), the [P8 MQTT contract](../p8/P8_MQTT_CONTRACT.md) and the [P3 CLI contract](P3_CLI_CONTRACT.md) (CL-01 to CL-22, approved on 2026-10-29), with the REQ-086 amendment of CL-19 C (one coverage gate per assembly).
- Package: `WeatherParser.Parsing` `0.1.0-initial`, unpublished and not byte-reproducible; the example CLI consumes it as a versioned package reference from the local feed `artifacts/local-nuget`.

## 2. What P3 provides

- The example CLI `WeatherParser` (`examples/WeatherParser.Cli`): five commands, one per invocation; argument grammar and validation before any request; one JSON configuration file (`--Config` or `WEATHERPARSER_CONFIG`) whose members are the library's option types; broker credentials from environment variables or a protected credential file; JSON and XML output to stdout or a new file without overwriting; the MQTT transport over MQTTnet 5.2.0.1603 with TLS by default, success only after the acknowledgment, bounded attempts and a publication summary; exit codes 0 to 4; log lines with UTC, level, ID, command and source on stderr; cancellation by SIGINT, SIGTERM and Ctrl+C with a grace period and a command deadline. Documented in the README section "Example CLI"; the example configuration `examples/weatherparser.example.json`.
- Verification tooling: the CLI stage of `scripts/ci-local.ps1` with its own coverage gate (CL-19 C), the opt-in broker integration test (`WEATHERPARSER_TEST_BROKER`, CONTRIBUTING.md section 4), the signal test host `tests/WeatherParser.Cli.SignalHost` and the Linux-only signal process tests; the credential-file check of CL-06 (owner and mode of the opened file through Linux `statx`), whose foreign-owner tests run only as root.

## 3. P9 responsibilities (DEVELOPMENT_PLAN.md P9, REQ-004 to REQ-009, REQ-080 to REQ-086, REQ-090 to REQ-093)

- Release artifacts and checksums; package ID, feed and publication procedure (no publication is claimed before P9); package reproducibility or its documented limitation.
- The deployment model of the example CLI on Debian 13 ARM64 (framework-dependent or self-contained, location, user, file permissions of the configuration and credential file), and the ARM64 release acceptance on the NanoPi R76S.
- Operating guides: scheduling (the README's cron and n8n examples), log handling, request volume per source, broker set-up with dedicated credentials, TLS and scoped topic permissions.
- Release documentation of the source conditions and limitations (DWD CC BY 4.0 attribution, OW-14), and the update of the SDK and the `Microsoft.Extensions.*` pins if the user decides it (CL-22).

## 4. Reuse obligations

- The CLI stays a consumer of the package: no library source in the CLI, no second serializer, the library's `MqttMessage` published unchanged, one command per process.
- The coverage gates of CL-19 C apply to every pull request: the library gate with the gated-input rule and the CLI gate on `WeatherParser.dll` alone.
- Live checks, broker runs and target-hardware runs only with the user's authorization, recorded verbatim.

## 5. Working rules

- The AGENTS.md rules apply unchanged, including "Phase implementation" and "Plan generation".
- qwen3-coder generates code, tests, scripts and data from briefs with the real declarations; P3 lessons: qwen often drops requested lines or changes unrelated assertions (for example command names in expected log texts) and re-creates removed members, so every diff is read in full; adding tests to an existing large test file is safer as a new test file with its own fixture; after two failed precise briefs the orchestrating agent makes the change and records it.
- Process tests start child processes; on Linux Microsoft.CodeCoverage also measures them, so a new executable in the test graph must be excluded from the CLI gate.

## 6. Open items and limitations

- The open items and limitations of the [P3.12 acceptance record](P3_12_ACCEPTANCE.md) section 4 stay open with their owners, among them TLS and authenticated broker connections not verified against a real broker, a Windows system shutdown not verified, the broker disconnect after a cancellation bounded by `ConnectTimeout`, the MQTT 3.1.1 acknowledgment limitation, the OW-14 restriction and the inherited library limitations.
