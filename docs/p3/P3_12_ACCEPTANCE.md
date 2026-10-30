# P3.12 – P3 Acceptance Record

**Status (2026-10-31): draft for the user's acceptance; the evidence audit is ACCEPTED (section 5); the automatic Codex review of pull request #16 and the user's decision follow (sections 7 and 8).** Plan: [P3_12_ACCEPTANCE_HANDOFF_PLAN.md](P3_12_ACCEPTANCE_HANDOFF_PLAN.md). Handoff: [P9_HANDOFF.md](P9_HANDOFF.md).

## 1. Review identity (A1)

| Item | Value |
|---|---|
| Implementation candidate | `60bd2a1`, branch `features/develop/P3`: the fixes of the user's pull request review of `b50d3ef` (the CL-06 owner check on the opened credential file; an explicit empty `--StationID` is validated by the library instead of falling back to the configuration; [verification record](P3_VERIFICATION.md) section 12) on top of `b50d3ef` (the P3.11 review fix: the CLI gate excludes the signal test host). The integrated verification ran READY on `5696357`; on `60bd2a1` S1 (Windows) and S2 (hosted CI) were repeated, S3 to S5 (NanoPi R76S, broker, live checks) were not, because a new candidate needs new authorizations ([P3.11 record](P3_11_INTEGRATED_VERIFICATION.md) section 10) |
| Pull request | [#16](https://github.com/witalij-schrajer/WeatherParser/pull/16) into `develop`, draft since the P3.02 approval |
| Integration target | PR base `e623a834bdb2902cec930258a62a48302f626716` (`e623a83`), unchanged since P3.01 |
| Contract | [P3 CLI contract](P3_CLI_CONTRACT.md) CL-01 to CL-22, approved on 2026-10-29 ("Approve all") |
| Verification | [P3 verification record](P3_VERIFICATION.md) sections 1 to 11; [P3.11 record](P3_11_INTEGRATED_VERIFICATION.md) |

## 2. Requirement and test evidence matrix (A2, T-P3-01 – T-P3-20)

Platform evidence for every row: the P3.11 runs on candidate `5696357` (Windows `ci-local.ps1` exit 0, hosted CI run 41114462647 with all jobs green, NanoPi R76S `ci-local.ps1` exit 0) and the runs on the candidate `60bd2a1` recorded in the [P3.11 record](P3_11_INTEGRATED_VERIFICATION.md) section 11: Windows `ci-local.ps1` exit 0 and hosted CI run 41142408523 green on all jobs; library gate 11102/11103 everywhere; CLI gate on `WeatherParser.dll` 2872/2917 (Windows) and 2881/2917 (Ubuntu x64, Debian 13 ARM64); CLI tests 710 (Ubuntu) and 712 (Debian 13 ARM64, root) of 719 executed and passed, the rest the documented skips. "Step" names the section of [P3_VERIFICATION.md](P3_VERIFICATION.md) that holds the Red → Green → Refactor evidence; characterization tests are labelled there.

| T-P3 ID | Requirements | Tests (classes) | Step | Status |
|---|---|---|---|---|
| T-P3-01 | REQ-012, REQ-013, C-112, CL-01 | `CliProjectFileTests`, `CompositionRootTests`, the package-reference restore of the CLI stage, the app-host smoke tests of P3.11 | P3.03, P3.11 | PASSED |
| T-P3-02 | REQ-010, REQ-011, CL-02, CL-03 | `CommandLineParserTests`, `CommandLineParserCommandContextTests`, `ProgramRunTests`, `ProgramUsageCommandTests`, `ProcessTests` | P3.04, P3.06 | PASSED |
| T-P3-03 | REQ-010, REQ-020, REQ-021, REQ-100, REQ-103, CL-03 | `CommandRequestValidatorTests`, `ProcessTests` (zero requests for invalid values), `ProcessOutputTests`, `ProcessStationIdTests` (an explicit empty `--StationID`) | P3.04, P3.07 | PASSED |
| T-P3-04 | REQ-010 | `HelpWriterTests`, `ProgramRunTests`, `ProcessTests`, `ReadmeCliSectionTests` | P3.03, P3.04, P3.10 | PASSED |
| T-P3-05 | REQ-062, REQ-064, CL-04, CL-05 | `ConfigurationLoaderTests`, `ConfigurationRegistrarTests`, `ConfigurationValidator*Tests`, `ExampleConfigurationTests`, `ReadmeCliSectionTests` (the example in JSON and MQTT mode) | P3.05, P3.10 | PASSED |
| T-P3-06 | REQ-030, REQ-031, CL-05 | `ReferenceStationResolverTests`, `ConfigurationValidatorStationAndCredentialTests`, `ProcessTests` (`Process_ParseDwdWithoutStation_ExitsBeforeRetrieval`), `ProcessStationIdTests` | P3.05, P3.12 review fixes | PASSED |
| T-P3-07 | REQ-060, REQ-072, CL-06, CL-08 | `CredentialProviderTests`, `FileModeReaderTests`, `ProcessCredentialFileTests`, `ProcessTests` (`Process_CredentialValues_NeverPrinted`), `MqttPublisherTests` (redaction) | P3.05, P3.08, P3.12 review fixes | PASSED; a foreign owner is exercised on a real file only where the tests run as root (section 4) |
| T-P3-08 | REQ-072, REQ-073, CL-08, CL-10 | `LogLineFormatterTests`, `StandardErrorLoggerProviderTests`, `StandardErrorLoggerSourceTests`, `CliErrorIdsTests`, `LibraryErrorsTests`, process tests (every stderr line a log line) | P3.06 | PASSED |
| T-P3-09 | REQ-007, REQ-063, REQ-068, C-87, CL-11 | `ExitCodePolicyTests`, `FailureClassifierTests`, `CommandExecutorTests`, `CommandExecutorCancellationTests`, `ReadmeCliSectionTests` | P3.06, P3.09, P3.10 | PASSED |
| T-P3-10 | REQ-002, REQ-023, REQ-101, CL-09 | `SelectionHandlerTests` (lists in JSON and XML against the packaged XSD, MQTT), P3.11 live selections | P3.07, P3.11 | PASSED |
| T-P3-11 | REQ-030 to REQ-032, REQ-040, REQ-041, REQ-103, REQ-105, REQ-111, CL-09 | `ParseDwdAndMosmixHandlerTests`, `ParseOstHessenHandlerTests`, `ExportErrorMessagesTests`, `ProcessOutputTests`, P3.11 live parses | P3.07, P3.11 | PASSED |
| T-P3-12 | REQ-065 to REQ-069, CL-12 | `OutputDestinationTests`, `DocumentResultWriterTests`, `FileOperationsTests`, `ProcessOutputTests` | P3.07, P3.09 | PASSED |
| T-P3-13 | REQ-067, REQ-082 | `ParseDwdAndMosmixHandlerTests`, `ParseOstHessenHandlerTests`, `SelectionHandlerTests` (JSON and XML through the real serializers) | P3.07 | PASSED |
| T-P3-14 | REQ-062, REQ-063, MQ-02, CL-13, CL-14 | `MqttPublisherTests`, `MqttPublisherRetryTests`, `MqttPublisherDeadlineTests`, `MqttNetClientAdapterTests`, `MqttNetOptionsBuilderTests`, `MqttPublicationSettingsFactoryTests`, `BrokerIntegrationTests` (local Mosquitto) | P3.08, P3.11 | PASSED; TLS and credentials against a real broker not verified |
| T-P3-15 | REQ-063, CL-15 | `MqttPublicationSummaryTests`, `CommandExecutorPublicationTests`, `PublicationQueueTests` | P3.08 | PASSED |
| T-P3-16 | OW-14, CL-16 | `ConfigurationValidatorOsthessenPublicationTests`, `ProcessOutputTests` (with and without permission), `ReadmeCliSectionTests` (MQTT mode) | P3.07, P3.08, P3.10 | PASSED |
| T-P3-17 | SC-22, CL-17 | `SelectionHandlerTests` (refresh before selection, failure without selection), P3.11 live selection with refresh | P3.07, P3.11 | PASSED |
| T-P3-18 | REQ-006, REQ-007, CL-18 | `ShutdownSignalTests`, `ShutdownSignalRobustnessTests`, `CommandExecutorCancellationTests`, `CommandExecutorStartupSignalTests`, `HandlerTokenTests`, `MqttPublisherCancellationTests`, `LibraryDeadlineResolverTests`, `ProcessSignalTests`, `ProcessTerminationTests`; real Ctrl+C (Windows) and SIGINT and SIGTERM (NanoPi) in P3.11 | P3.09, P3.11 | PASSED; a Windows system shutdown not provoked |
| T-P3-19 | REQ-083, REQ-084, CL-20 | Offline standard runs; `BrokerIntegrationTests` only with `WEATHERPARSER_TEST_BROKER`; live checks only with authorization; NanoPi results recorded separately | P3.08, P3.11 | PASSED |
| T-P3-20 | REQ-017, REQ-085, REQ-086, CL-19 | Red → Green → Refactor per slice; the library gate and the CLI gate per CL-19 C on every platform | all steps, P3.11 | PASSED |

## 3. Contract summary (A3)

All rows CL-01 to CL-22 are approved and implemented as recorded in the verification record, with the differences named in section 4: the CL-20 "fake publisher" of the process tests is a test-owned fake broker endpoint (P3.08); the cleanup disconnect after a cancellation runs outside the command token, bounded by `ConnectTimeout` (documented in the README).

## 4. Limitations and open items (A4)

| Item | Owner |
|---|---|
| CL-06 owner check: implemented after the pull request review (verification record section 12). Changing a file's owner needs root, so `FileModeReaderTests.Read_FileOwnedByAnotherUser_ReportsThatOwner` and `ProcessCredentialFileTests.Process_CredentialFileOwnedByAnotherUser_*` run only as root (the hosted Debian 13 ARM64 container) and are skipped elsewhere; the owner comparison itself is tested everywhere with a test double. Unix systems other than Linux are not supported targets (no `statx`) | Recorded |
| P3.11 S3 to S5 (NanoPi R76S run, broker integration tests, live checks) ran on `5696357`, not on the candidate `60bd2a1`; repeating them needs the user's authorization | The user's disposition with the acceptance |
| The CLI-only package pins in `examples/Directory.Packages.props` (`Microsoft.Extensions.Logging` 10.0.13, MQTTnet 5.2.0.1603) need the user's confirmation | The user |
| CL-19 C asymmetry: the CLI gate has no gated-input exception, so a library-only pull request must strictly improve the CLI gate too; platform-specific CLI code prevents an exact 100 % on every platform | The user |
| CL-20 wording: the standard process tests use a test-owned fake broker endpoint (`ClosingTcpListener`) instead of a "fake publisher", because a separately started CLI process cannot be handed a fake without a test hook in the product | Reported to the user |
| TLS and authenticated broker connections not verified against a real broker (the authorized broker has neither); `Broker_WrongCredentials_Rejected` and `Broker_TlsWithCustomCa` stay opt-in | P9 (broker set-up) |
| A Windows system shutdown (`CTRL_SHUTDOWN_EVENT`, SIGTERM on Windows) is not provoked by any test or check | P9 |
| The broker disconnect after a cancellation or an expired deadline is bounded by `ConnectTimeout` and runs outside the command token (documented) | Recorded |
| MQTT 3.1.1 acknowledgment limitation (CL-13, documented) | Recorded |
| CL-22: SDK 10.0.403 and runtime 10.0.14 (security release) and the `Microsoft.Extensions.*` 10.0.14 update conflict with the 10.0.13 pins; a separate user decision outside P3 | The user |
| The library's `DeleteTemporaryFile` failure block is reachable only on Windows (library gate 11102/11103 on every platform); the REQ-086 amendment of CL-19 C | Recorded |
| P3.11 live checks: the second `--SelectMOSMIX` exceeded the authorized one run per command and repeated the same search on the same day (cause: an incomplete live configuration; [P3.11 record](P3_11_INTEGRATED_VERIFICATION.md) section 10) | The user's disposition with the acceptance |
| P3.11 live checks: the `retrieved_at` of the MQTT run was not recorded (an unmet evidence criterion of the P3.11 plan; the issue time and step count were recorded) | The user's disposition with the acceptance |
| Process deviations, recorded per step: parts written by the orchestrating agent after repeated failed qwen briefs, and small mechanical edits (call sites of changed signatures, comment placement, a restored file) | Recorded |
| OsthessenWetter values must not be published (OW-14); `PublicationPermitted` stays false in the example | The user |
| Inherited: the limitations of the [P8.10 acceptance record](../p8/P8_10_ACCEPTANCE.md) section 4 (coarse XML cancellation, builder and root-validation duplication, the hosted ARM64 container not being target evidence, the package not byte-reproducible) | P9 and the user as recorded there |
| Publication, package ID, feed, release, deployment model | P9 ([P9 handoff](P9_HANDOFF.md)) |

## 5. Audit decision (A5)

**ACCEPTED** (orchestrating agent's evidence audit, 2026-10-30, renewed on 2026-10-31 for candidate `60bd2a1` after the pull request review fixes):
- every T-P3 row is PASSED with tests and run evidence; characterization tests are labelled in the verification record;
- the P3.11 outcome is READY on `5696357` on Windows, hosted CI and the NanoPi R76S with the authorized broker run and live checks; on `60bd2a1` S1 and S2 passed, S3 to S5 await the user's disposition (section 4); no known defect of the CLI is open;
- both coverage gates passed on every platform, the CLI gate on `WeatherParser.dll` alone;
- every contract row is approved and implemented or its difference is named in section 4, and each limitation has an owner.

This audit decision is not the user's acceptance.

## 6. User decision (A7)

Pending: the user is asked after the automatic Codex review of pull request #16 reaches 0 findings.

## 7. Ready for review, review and merge (A9 to A11)

- Pull request review by the user on `b50d3ef` (2026-10-30, verdict "REQUEST CHANGES", submitted as a comment review): 2 findings, both confirmed and fixed test-first in `60bd2a1`: [Major] the CL-06 owner check of the credential file was missing (now owner and mode of the opened file, read through the same handle); [Minor] an explicit empty `--StationID` fell back to `Dwd.ReferenceStationId` (now validated by the library: `STATION-ID-INVALID`, exit 2, zero requests). Evidence: [verification record](P3_VERIFICATION.md) section 12.
- A9: the pull request is marked ready for review under AGENTS.md "Phase implementation" step 2; the automatic review rounds and their dispositions are recorded here.

## 8. Authorship and review

Written by the orchestrating agent on 2026-10-30 from the P3 verification record, the P3.11 record, the P3 CLI contract and the P8.10 acceptance record; not generated by qwen (AGENTS.md).
