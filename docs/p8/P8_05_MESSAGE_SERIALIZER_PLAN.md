# P8.05 – MQTT Message Serializer: Implementation Plan

**Status: planned on 2026-10-28; not started; to be reconciled with the approved P8.02 contract before it starts.** Refines the P8.05 row of section 5 and the "Genericity", "Payload" and "Failures" rows of section 4 of [PHASE_P8_PLAN.md](../../PHASE_P8_PLAN.md).

## 1. Goal and Scope
Goal: the public generic MQTT serializer that turns one supported export root into one `MqttMessage` with the configured topic, the JSON export bytes as payload, QoS and retain, with defined failures and without any broker access.
In scope: `IMqttMessageSerializer`/`MqttMessageSerializer` with `SerializeAsync<TRoot>(TRoot root, CancellationToken)` (MQ-02); constructor dependencies `MqttTopicOptions`, `IMqttTopicOptionsValidator`, `IMqttTopicValidator`, `IJsonExportSerializer`; the message type from the root type (MQ-03); the placeholder values from the root (MQ-04) checked by MQ-06; the resolved topic checked by MQ-07; the payload from `IJsonExportSerializer` into a bounded buffer (MQ-11); failure IDs and precedence (MQ-13); caller cancellation; registration.
Out of scope with owner: per-source message evidence → P8.06; package consumption → P8.07; transport → P3.

## 2. Preconditions and Inputs
- P8.03 and P8.04 complete; P8.02 approved MQ-02, MQ-03, MQ-04, MQ-06, MQ-07, MQ-11 and MQ-13.
- Existing: `IJsonExportSerializer` (C-10 caller-owned stream, C-11 UTF-8 without byte-order mark, C-88 supported roots, C-90 root validation before writing, C-92 deterministic bytes), the export roots and their builders.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Failure precedence | Caller cancellation → unsupported type → missing mapping → placeholder value → topic → serialization → payload size / any other order | As stated: cheap configuration checks before serialization, so an unmapped type never serializes; cancellation observed before any work and passed to the JSON serializer | Orchestrator, recorded (MQ-13) | S2 |
| Size enforcement | A bounded write stream that fails at `MaxPayloadBytes + 1` / measure after writing | A bounded write stream, so an oversized payload is never fully buffered; the bound counts bytes exactly | Orchestrator, recorded (MQ-11) | S3 |
| Test doubles | Strict mocks of the rule services in unit tests / real rule services | Unit tests with the real rule services and a strict mock of `IJsonExportSerializer` only where a failure must be injected; contract tests with the real JSON serializer (REQ-077) | Orchestrator, recorded | S1 to S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Mqtt/IMqttMessageSerializer.cs`, `MqttMessageSerializer.cs` | Create | Serializer |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration |
| `tests/WeatherParser.Parsing.Tests/Mqtt/MqttMessageSerializerTests.cs` | Create | Unit and contract tests |
| Every test container setup that builds `AddWeatherParsing` with validation (`TestConsumerServices`, `ConsumerSelectionSupport`, `Selection/ConsumerBoundary.cs` and the test-project equivalents) | Change | Register `MqttTopicOptions`, because the serializer's constructor depends on it (C-108) |
| `docs/p8/P8_VERIFICATION.md` | Change | P8.05 section |

## 5. Procedure
Every slice: roles and checks as in P8.03 section 5.

### S1 Mapping, topic and message
**Red**: `SerializeAsync_Observation_TopicFromSourcePlaceholder`, `SerializeAsync_Forecast_TopicFromProductAndMosmixId`, `SerializeAsync_StationLists_TopicFromLiteralAndProvider`, `SerializeAsync_QosAndRetainFromMapping`, `SerializeAsync_MessageTypeFromRootType`, `SerializeAsync_ExportDocument_ThrowsMessageTypeUnsupported`, `SerializeAsync_OtherTypes_ThrowMessageTypeUnsupported` (`object`, `string`, `ForecastStep`), `SerializeAsync_UnmappedType_ThrowsMappingMissing_WithoutSerializing` (the strict JSON serializer mock has no setup), `SerializeAsync_UnsafePlaceholderValue_ThrowsPlaceholderValueInvalid` (a consumer-built root whose station ID contains `/`), `SerializeAsync_ResolvedTopicTooLong_ThrowsTopicInvalid`, `SerializeAsync_NullRoot_ThrowsArgumentNull`, `Constructor_InvalidOptions_ThrowsOptionsInvalid`, `AddWeatherParsing_ResolvesMessageSerializer`; `ServiceRegistrationCompletenessTests` red while unregistered.
**Green**: serializer and registration; the container setups updated in the same slice.

### S2 Payload and failures
**Red**: `SerializeAsync_PayloadEqualsJsonExportBytes` (each of the four roots; the real `JsonExportSerializer` writes the comparison bytes), `SerializeAsync_PayloadUtf8WithoutBom`, `SerializeAsync_InvalidRoot_ThrowsSerializationFailed_WithInnerException` (an unassigned required member), `SerializeAsync_CancelledBeforeStart_ThrowsOperationCanceled_NoSerialization`, `SerializeAsync_CancelledDuringSerialization_ThrowsOperationCanceled`, `SerializeAsync_SameRootTwice_IdenticalMessages`.
**Green**: payload buffering and failure mapping.

### S3 Payload bound and diagnostics
**Red**: `SerializeAsync_PayloadAtBound_Accepted`, `SerializeAsync_PayloadOneByteAbove_ThrowsPayloadTooLarge`, `SerializeAsync_PayloadTooLarge_NeverBufferedBeyondBound`, `SerializeAsync_FailureMessages_NoPayloadContent` (a root with a distinctive marker in the location name: the marker appears in no exception message), `MqttMessageSerializer_HasNoLoggerDependency`.
**Green**: bounded stream and messages.

**Refactor**: with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P8 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P8-02, T-P8-03, T-P8-05, T-P8-06 | REQ-015, REQ-062 | Mapping, topic, QoS, retain |
| S2 | T-P8-07, T-P8-08 | REQ-050, REQ-069, REQ-075 | Payload equivalence and failures |
| S3 | T-P8-08, T-P8-12 | REQ-015, REQ-072 | Bound and diagnostics |
| all | T-P8-16 | REQ-078, REQ-085, REQ-086 | Registration; Red evidence; coverage |

## 7. Evidence to Record
- Red names and failures, counts, coverage counters, authorship, Codex rounds; the list of updated container setups

## 8. Risks and Limitations
- (a) The payload is buffered in memory up to the bound; for the largest expected payload (a MOSMIX_L run) the size is recorded in P8.06

## 9. Acceptance Criteria
- [ ] Every listed test green with recorded Red
- [ ] No payload without a valid topic; no topic without a valid payload
- [ ] Registration and architecture tests green, every validated container still builds; coverage gate passed; Codex 0 findings or reported
