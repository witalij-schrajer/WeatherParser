# P8.05 – MQTT Message Serializer: Implementation Plan

**Status: planned on 2026-10-28; accepted by the user on 2026-10-28 with the phase plan, the failure-precedence change of review round 9 accepted on 2026-10-28; reconciled on 2026-10-28 with the P8.02 contract MQ-01 to MQ-15 approved by the user on 2026-10-28 (P8.02 S5); not started.** Refines the P8.05 row of section 5 and the "Genericity", "Payload" and "Failures" rows of section 4 of [PHASE_P8_PLAN.md](../../PHASE_P8_PLAN.md).

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
| Failure precedence | Approved in MQ-13 (2026-10-28), replacing the planning order that checked the payload size before the placeholder values: A: null root → caller cancellation → unsupported type → missing mapping → serialization (root validation and payload) → placeholder value → topic → payload size (last, because the bound 268 435 450 − T depends on the resolved topic, contract section 4) / B: placeholder values and topic before serialization | A: the unsupported-type and missing-mapping checks read no root member, so an unmapped type never serializes; the JSON serializer then validates the root (C-90) before any byte is produced, so placeholder values are read only from a validated root. B is rejected: the export entities throw `InvalidOperationException` from the getter of an unassigned required member (`RequiredMember<T>.Get`), so reading `Source`, `Location` or `Product` for a placeholder before validation would bypass the `MQTT-SERIALIZATION-FAILED` contract. As a defense, any `InvalidOperationException` while reading a placeholder value is mapped to `MQTT-SERIALIZATION-FAILED` with the exception as inner exception. Cancellation is observed before any work and passed to the JSON serializer; a topic failure therefore costs one serialization, which is accepted | Orchestrator, recorded (MQ-13) | S1, S2 |
| Size enforcement | A: a capped destination stream whose cap is the largest bound any topic of the mapping permits (the smaller of the configured `MaxPayloadBytes`, null meaning no configured limit, and 268 435 450 − the minimum resolved topic length of the template); it never throws: past the cap it stops storing and records the overflow, so the placeholder and topic checks still run first, and the exact effective bound (the smaller of `MaxPayloadBytes` and 268 435 450 − T for the resolved topic) is enforced last, with an overflow always failing (MQ-11, MQ-13) / B: changing `JsonExportSerializer` to enforce the bound while serializing | A, with its limit stated: `JsonExportSerializer` serializes the whole document into its own buffer before copying it to the destination (C-10 buffer-then-copy, so nothing partial is ever written), so `MaxPayloadBytes` bounds the accepted message and the second copy, not the memory used during JSON serialization. That memory is bounded by the root the caller supplies; the library's own parsers produce bounded roots (MF-07 retained-size bounds, the P5 and P6 limits). B would change the export serializer contract (C-10, C-92) and is out of P8's scope; it needs a recorded contract decision if a consumer need appears | Orchestrator, recorded (MQ-11) | S3 |
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
**Red**: `SerializeAsync_Observation_TopicFromSourcePlaceholder`, `SerializeAsync_Forecast_TopicFromProductAndMosmixId`, `SerializeAsync_StationLists_TopicFromLiteralAndProvider`, `SerializeAsync_QosAndRetainFromMapping`, `SerializeAsync_MessageTypeFromRootType`, `SerializeAsync_ExportDocument_ThrowsMessageTypeUnsupported`, `SerializeAsync_OtherTypes_ThrowMessageTypeUnsupported` (`object`, `string`, `ForecastStep`), `SerializeAsync_UnmappedType_ThrowsMappingMissing_WithoutSerializing` (the strict JSON serializer mock has no setup), `SerializeAsync_UnsafePlaceholderValue_ThrowsPlaceholderValueInvalid` (a consumer-built root whose station ID contains `/`), `SerializeAsync_ResolvedTopicTooLong_ThrowsTopicInvalid`, `SerializeAsync_ListSourceWithoutProviderPrefix_ThrowsPlaceholderValueInvalid` (consumer-built `CdcStationList` roots with `source` `catalog` and `:catalog` and a template using `{provider}`), `SerializeAsync_NullRoot_ThrowsArgumentNull`, `Constructor_InvalidOptions_ThrowsOptionsInvalid`, `AddWeatherParsing_ResolvesMessageSerializer`; `ServiceRegistrationCompletenessTests` red while unregistered.
**Green**: serializer and registration; the container setups updated in the same slice.

### S2 Payload and failures
**Red**: `SerializeAsync_PayloadEqualsJsonExportBytes` (each of the four roots; the real `JsonExportSerializer` writes the comparison bytes), `SerializeAsync_PayloadUtf8WithoutBom`, `SerializeAsync_InvalidRoot_ThrowsSerializationFailed_WithInnerException` (an unassigned required member), `SerializeAsync_UnassignedPlaceholderMembers_ThrowSerializationFailed_BeforeTopicResolution` (an observation with unassigned `Source` and template `o/{source}`; an observation with unassigned `Location` and template `o/{provider}/{station_id}`; a forecast with unassigned `Product` and template `f/{product}/{mosmix_id}`: each fails with `MQTT-SERIALIZATION-FAILED` and an inner `ExportSerializationException`, never with an unwrapped `InvalidOperationException`, and no topic is resolved), `SerializeAsync_PlaceholderReadThrows_MappedToSerializationFailed` (the defensive mapping, with a root whose validation passes through an injected JSON serializer double), `SerializeAsync_CancelledBeforeStart_ThrowsOperationCanceled_NoSerialization`, `SerializeAsync_CancelledDuringSerialization_ThrowsOperationCanceled`, `SerializeAsync_SameRootTwice_IdenticalMessages`.
**Green**: payload buffering and failure mapping.

### S3 Payload bound and diagnostics
**Red**: `SerializeAsync_PayloadAtBound_Accepted`, `SerializeAsync_PayloadOneByteAbove_ThrowsPayloadTooLarge` (both with a configured `MaxPayloadBytes`), `EffectiveBound_ProtocolFormula` (the internal bound computation: 268 435 450 − T for topics of 1 byte, of 2-byte characters and of 65 535 bytes, and the smaller of it and a configured value, tested through `InternalsVisibleTo`), `SerializeAsync_OversizedPayloadAndInvalidTopic_ThrowsTopicInvalid` (the MQ-13 order: topic before size), `SerializeAsync_PayloadTooLarge_DestinationNeverExceedsCapPlusOne` (the capped destination stores at most cap + 1 bytes; JSON serialization itself still buffers the whole document, as documented), `SerializeAsync_NullMaxPayloadBytes_UsesProtocolBound` (no configured limit: a normal payload is accepted and the cap equals the protocol bound of the template), `SerializeAsync_OversizedPayloadAndInvalidPlaceholder_ThrowsPlaceholderValueInvalid` (`MaxPayloadBytes` 1 with an unsafe station ID: the placeholder failure wins over the size), `SerializeAsync_FailureMessages_NoPayloadContent` (a root with a distinctive marker in the location name: the marker appears in no exception message), `MqttMessageSerializer_HasNoLoggerDependency`.
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
- (a) The JSON serializer buffers the complete document before the bound is checked (C-10), so `MaxPayloadBytes` limits message acceptance, not peak serialization memory; peak memory follows the size of the caller's root. The largest payload the library's parsers can produce (a MOSMIX_L run at the MF-07 bounds) is measured and recorded in P8.06, and the README states the limit (P8.08)

## 9. Acceptance Criteria
- [ ] Every listed test green with recorded Red
- [ ] No payload without a valid topic; no topic without a valid payload
- [ ] Registration and architecture tests green, every validated container still builds; coverage gate passed; Codex 0 findings or reported
