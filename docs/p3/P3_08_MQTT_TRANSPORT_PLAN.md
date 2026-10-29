# P3.08 – MQTT Transport: Implementation Plan

**Status: planned on 2026-10-29; under review, not yet accepted.** Refines the P3.08 row of section 5 and the "Transport" row of section 4 of [PHASE_P3_PLAN.md](../../PHASE_P3_PLAN.md).

## 1. Goal and Scope
Goal: the example broker transport that publishes the library's `MqttMessage` unchanged, over TLS by default, with credentials from the approved source, reports success only after the QoS-1 acknowledgment, bounds connection and publication time and attempts, and summarizes partial failures.
In scope: the publisher of CL-13 (implementing the P3.07 seam) over the approved MQTT client package, added to `Directory.Packages.props`, the lock files and `DEPENDENCIES.md` with its licence; connection settings (host, port, protocol version, client ID, TLS with the system trust store and an optional CA file, `AllowUnencrypted` with a warning, keep-alive, clean session); credentials from P3.05; timeouts, attempts and the "never republish an acknowledged message" rule (CL-14); the per-message QoS and retain check (CL-05, REQ-062); partial-failure handling and summary (CL-15); the explicitly enabled broker integration test (CL-20).
Out of scope with owner: signals and the command deadline → P3.09; running the integration test against the authorized test broker and the NanoPi → P3.11.

## 2. Preconditions and Inputs
- P3.07 complete; P3.02 approved CL-13, CL-14, CL-15 and CL-20, with the client package facts recorded.
- Inputs: REQ-060, REQ-062, REQ-063, REQ-072, REQ-083; MQ-01, MQ-02, MQ-08, MQ-11, MQ-15; the P8.10 limitation "broker behavior is not shown by message tests".

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Client seam | The publisher talks to the client package directly / through a thin adapter interface over the client (Proposed: `IMqttClientAdapter`) | The thin adapter, so that unit tests use a strict mock and the real client is exercised only in the adapter's own tests and in the broker integration test; the adapter exposes connect, publish (returning the acknowledgment result) and disconnect with cancellation tokens | Orchestrator, recorded (CL-13) | S1 |
| Retry classification | Retry every failure / retry only transient failures | Retry only when no acknowledgment was received for the message (timeout or lost connection before PUBACK), never after a negative acknowledgment or an authorization failure; reconnect within the attempt bound; an already acknowledged message is never republished | Orchestrator, recorded (CL-14) | S2 |
| Broker integration test | A test broker in the CI jobs / only explicitly enabled runs | Only explicitly enabled runs (environment variable of CL-20) against the test broker the user names and authorizes; the standard CI jobs skip it with a recorded reason | User (CL-20) | S4 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `examples/WeatherParser.Cli/Mqtt/…` | Create | Publisher, client adapter, TLS and credential wiring, summary |
| `Directory.Packages.props`, `examples/WeatherParser.Cli/packages.lock.json`, `tests/WeatherParser.Cli.Tests/packages.lock.json`, `DEPENDENCIES.md` | Change | The approved client package, pinned, with licence |
| `tests/WeatherParser.Cli.Tests/Mqtt/…` | Create | Unit tests with a strict adapter mock; adapter tests; the opt-in broker integration test |
| `CONTRIBUTING.md` | Change | How to enable and run the broker integration test (no broker address or credential in the repository) |
| `docs/p3/P3_VERIFICATION.md` | Change | P3.08 section |

## 5. Procedure
Every slice: roles and checks as in P3.03 section 5.

### S1 Publication and acknowledgment
**Red**: `Publish_SendsTopicPayloadQosRetainUnchanged` (byte comparison with the `MqttMessage`), `Publish_SuccessOnlyAfterAcknowledgment` (the adapter mock completes the acknowledgment later: no success is reported before it), `Publish_NegativeAcknowledgment_FailsWithMqttPublishFailed`, `Publish_MessageWithQosOtherThanOne_RefusedBeforeConnect`, `Publish_StationListRetained_RefusedBeforeConnect`, `Publish_ObservationNotRetained_RefusedBeforeConnect`, `Connect_TlsByDefault`, `Connect_Unencrypted_OnlyWithAllowUnencrypted_AndWarning`, `Connect_CustomCaFile_Used`, `Connect_CertificateValidationNeverDisabled` (no configuration path sets an accept-all validation callback), `Connect_CredentialsFromProvider_NeverLogged`.
**Green**: publisher and adapter.

### S2 Timeouts and attempts
**Red**: `Connect_Timeout_FailsWithMqttConnectFailed`, `Publish_Timeout_RetriedWithinBound`, `Publish_AttemptsExhausted_FailsWithMqttPublishTimeout`, `Publish_AcknowledgedMessage_NeverRepublished` (a connection drop after the acknowledgment of message 1 of 3: messages 2 and 3 are retried, message 1 is not), `Publish_AuthorizationFailure_NotRetried`, `Delays_UseInjectedTimeProvider` (no real sleeping in unit tests).
**Green**: bounded retry logic.

### S3 Partial failures and summary
**Red**: `Command_OneOfThreePublicationsFails_SummaryAndExitPartial`, `Command_ConnectionFails_NoPublication_ExitFailure`, `Command_AuthenticationFails_ReportedWithId_ExitFailure`, `Command_TlsFailure_ReportedWithId_ExitFailure`, `Summary_LogLineCountsPublishedAndFailed`, `Process_MqttMode_FakeAdapter_ExitCodesPerCl11` (process tests with a test-only adapter registration).
**Green**: summary and outcome wiring.

### S4 Broker integration test (opt-in)
**Red** (run with the variable set against a local test broker on the developer machine, as authorized): `Broker_PublishAndSubscribe_RetainedObservationReceivedOnce`, `Broker_StationList_NotRetained`, `Broker_SecondRunSameSource_ReplacesRetainedMessage`, `Broker_WrongCredentials_Rejected`, `Broker_TlsWithCustomCa` (if the authorized broker offers TLS); without the variable every test is skipped with the reason.
**Green**: the adapter against the real client.

**Refactor**: with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P3 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P3-14, T-P3-07 | REQ-060, REQ-062, REQ-063, MQ-02 | Unchanged publication, acknowledgment, TLS, QoS and retain |
| S2 | T-P3-14 | REQ-063 | Timeouts and attempts |
| S3 | T-P3-15 | REQ-063 | Partial failure |
| S4 | T-P3-19, T-P3-11 | REQ-062, REQ-083 | Real broker behavior, opt-in |
| all | T-P3-20 | REQ-085, REQ-086 | Red evidence; coverage |

## 7. Evidence to Record
- Red names and failures, counts, coverage counters, authorship, the package version and licence, the broker integration run (broker product and version, platform, date; no address or credential), Codex rounds

## 8. Risks and Limitations
- (a) Broker limits (maximum packet size, topic permissions) differ between brokers; a rejection is reported with the broker's reason code and is not retried
- (b) The adapter's own tests need the real client; they run without network by using the client's in-process or loopback facilities where the package offers them, otherwise only in the opt-in run (recorded)

## 9. Acceptance Criteria
- [ ] Every listed test green with recorded Red (S4 in an authorized opt-in run)
- [ ] Success only after acknowledgment; TLS by default; bounded timeouts and attempts; QoS and retain enforced
- [ ] Package pinned and documented; coverage gate passed; Codex 0 findings or reported
