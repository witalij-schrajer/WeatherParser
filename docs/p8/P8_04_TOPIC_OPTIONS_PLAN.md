# P8.04 – Topic Options and Validation: Implementation Plan

**Status: planned on 2026-10-28; accepted by the user on 2026-10-28 with the phase plan; reconciled on 2026-10-28 with the P8.02 contract MQ-01 to MQ-15 approved by the user on 2026-10-28 (P8.02 S5); not started.** Refines the P8.04 row of section 5 and the "Configuration" row of section 4 of [PHASE_P8_PLAN.md](../../PHASE_P8_PLAN.md).

## 1. Goal and Scope
Goal: the consumer-registered topic configuration and its complete validation without broker access, so that a configuration mistake fails when the serializer is first resolved, not when the first message is built.
In scope: `MqttTopicOptions` and `MqttTopicMapping` (MQ-09, MQ-08); `IMqttTopicOptionsValidator`/`MqttTopicOptionsValidator` that parses every template through `IMqttTopicTemplateParser`, applies the uniqueness rules (MQ-05), the QoS and retain defaults and overrides (MQ-08) and the payload bound (MQ-11 option part), and returns a validated, immutable configuration (Proposed `ValidatedMqttTopicConfiguration`); `MQTT-OPTIONS-INVALID`; registration.
Out of scope with owner: serialization → P8.05; consumer registration in the package consumer → P8.07; README → P8.08.

## 2. Preconditions and Inputs
- P8.03 complete; P8.02 approved MQ-05, MQ-08, MQ-09 and MQ-11.
- Existing: the MOSMIX options validation pattern (`MosmixLForecastOptions` checked in the constructor of the consuming service, `MOSMIX-OPTIONS-INVALID`), `IMqttTopicTemplateParser`, `IMqttTopicValidator`.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Options shape | Approved by MQ-09 (Proposed: `Mappings` keyed by `MqttMessageType`, each with `TopicTemplate`, optional `QualityOfService`, optional `Retain`; `MaxPayloadBytes`) | As approved; the options type is a plain data object with public settable properties, like the existing options types | User (MQ-09) | S1 |
| Where validation runs | In the serializer constructor through the validator / at registration | In the serializer constructor through the injected validator (first resolution, the MF-04 pattern); the validator is also usable on its own | Orchestrator, recorded | S2 |
| Copy semantics | Validate and keep a snapshot / read the options on every call | Snapshot at validation; later changes to the consumer's options object have no effect (tested) | Orchestrator, recorded | S2 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Mqtt/MqttTopicOptions.cs`, `MqttTopicMapping.cs`, `ValidatedMqttTopicConfiguration.cs` | Create | Options and validated snapshot |
| `src/WeatherParser.Parsing/Mqtt/IMqttTopicOptionsValidator.cs`, `MqttTopicOptionsValidator.cs` | Create | Validation |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration |
| `tests/WeatherParser.Parsing.Tests/Mqtt/MqttTopicOptionsValidatorTests.cs` | Create | Unit tests with the real parser and validator (no mocks of pure rule services) |
| `docs/p8/P8_VERIFICATION.md` | Change | P8.04 section |

## 5. Procedure
Every slice: roles and checks as in P8.03 section 5.

### S1 Defaults, overrides and empty options
**Red**: `Validate_EmptyOptions_ValidWithoutMappings`, `Validate_ObservationAndForecastDefaults_QosOneRetained`, `Validate_StationListDefaults_QosOneNotRetained`, `Validate_Overrides_Kept` (QoS 0 and 2, retain inverted), `Validate_QosOutsideRange_ThrowsOptionsInvalid` (−1, 3, an undefined enum value), `Validate_NullOptionsOrMapping_Throws`, `Validate_SnapshotIgnoresLaterChanges`.
**Green**: options types and the default table of MQ-08.

### S2 Template and uniqueness rules
**Red**: `Validate_InvalidTemplate_ThrowsOptionsInvalidWithMessageType` (the parser's failure wrapped with the message type), `Validate_ObservationWithoutDistinguishingPlaceholder_Throws` (`weather/obs`, `weather/{provider}`), `Validate_ObservationWithSource_Accepted` (`o/{source}/state`), `Validate_SourceNotWholeLevel_Throws` (`o/x{source}`, `o/{source}-state`, `o/{station_id}{source}{provider}`, whose two valid sources `bc:a` and `c:ab` would both resolve to `o/abc:abc`), `Validate_ExtraPlaceholdersInOtherLevels_Accepted` (`o/{provider}/{source}`), `Validate_ObservationWithProviderAndStationId_Accepted`, `Validate_ForecastWithoutProductAndId_Throws` (`weather/{mosmix_id}`, `weather/{product}`), `Validate_ForecastWithSource_Accepted`, `Validate_ForecastWithProductAndMosmixId_Accepted` (`f/{product}/{mosmix_id}`), `Validate_PairPlaceholdersNotWholeLevels_Throws` (`o/{provider}-{station_id}`, `f/{product}{mosmix_id}`), `Validate_IdenticalTemplatesForTwoTypes_Throws`, `Validate_DifferentTemplatesThatCanCollide_Throws` (`weather/{source}` for `observation` with `weather/{provider}:{mosmix_id}` for `forecast`; `w/{source}` with `w/{source}/x`), `Validate_LiteralPrefixIsPrefixOfAnother_Throws` (`a/b` for `cdc_station_list` with `a/b/{source}` for `observation`), `Validate_DivergingPrefixes_Accepted`, `Validate_AcceptedConfigurations_ResolveDistinctTopics` (for each accepted configuration of this test class, every pair of distinct generated roots across all message types resolves to different topics, through the real parser and validator), `Validate_ExampleConfiguration_Accepted` (the four REQ-061 examples written as templates in the test, never as library defaults), `AddWeatherParsing_ResolvesOptionsValidator`.
**Green**: the rules of MQ-05.

### S3 Payload bound
**Red** (MQ-11 as approved: `MaxPayloadBytes` is `int?`, null means the protocol bound): `Validate_MaxPayloadBytesNull_MeansProtocolBound`, `Validate_MaxPayloadBytesAtProtocolLimit_Accepted` (268 435 450), `Validate_MaxPayloadBytesAbove_ThrowsOptionsInvalid` (268 435 451), `Validate_MaxPayloadBytesZeroOrNegative_ThrowsOptionsInvalid` (0, −1; 1 accepted).
**Green**: the bound.

**Refactor**: with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P8 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P8-05, T-P8-06 | REQ-016, REQ-062 | Defaults, overrides, empty options |
| S2 | T-P8-01, T-P8-04 | REQ-015, REQ-061 | Templates and uniqueness |
| S3 | T-P8-08 (bound configuration) | REQ-015 | Payload bound |
| all | T-P8-16 | REQ-078, REQ-085, REQ-086 | Registration; Red evidence; coverage |

## 7. Evidence to Record
- Red names and failures, counts, coverage counters, authorship, Codex rounds

## 8. Risks and Limitations
- (a) The uniqueness rules cannot detect collisions with topics of other publishers on the same broker; that stays with the broker's access control (REQ-061, P3)

## 9. Acceptance Criteria
- [ ] Every listed test green with recorded Red
- [ ] Every configuration mistake of the MQ rows fails with `MQTT-OPTIONS-INVALID` naming the message type and the rule
- [ ] Registration and architecture tests green; coverage gate passed; Codex 0 findings or reported
