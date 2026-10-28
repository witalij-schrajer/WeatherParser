# P8.03 – Message Entity and Topic Rules: Implementation Plan

**Status: planned on 2026-10-28; not started; to be reconciled with the approved P8.02 contract before it starts.** Refines the P8.03 row of section 5 and the "Topics" row of section 4 of [PHASE_P8_PLAN.md](../../PHASE_P8_PLAN.md).

## 1. Goal and Scope
Goal: the data types of an MQTT message and the two rule services every later step builds on: parsing and checking a topic template, and checking a placeholder value and a resolved topic against the protocol and the MQ rules.
In scope: `MqttMessage`, `MqttQualityOfService`, `MqttMessageType` (MQ-02, MQ-03); `IMqttTopicTemplateParser`/`MqttTopicTemplateParser` returning a parsed template (literal segments and placeholder names, MQ-04); `IMqttTopicValidator`/`MqttTopicValidator` with the placeholder-value rules (MQ-06) and the resolved-topic rules (MQ-07, MQ-01); `MqttMessageException` and `MqttMessageErrorIds` (MQ-13); registration in `AddWeatherParsing`.
Out of scope with owner: options and their validation → P8.04; the serializer → P8.05.

## 2. Preconditions and Inputs
- P8.02 approved MQ-01 to MQ-04, MQ-06, MQ-07 and MQ-13; this plan reconciled.
- Existing: `ServiceRegistrationCompletenessTests`, `NoSharedStateTests` (C-107, C-109), the exception and error-ID patterns of `MosmixForecastException` and `MosmixForecastErrorIds`.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Visibility of the rule services | Public interfaces (C-107 registration) / internal helpers of the serializer | Public interfaces, so consumers can check a configuration or a topic without serializing; C-107 then requires their registration | Orchestrator, recorded | S2, S3 |
| Parsed template type | A public immutable type / internal | Public immutable `MqttTopicTemplate` (Proposed) with the literal and placeholder segments in order, returned by the parser; no behavior | Orchestrator, recorded | S2 |
| Byte length | UTF-8 byte count / character count | UTF-8 byte count with a strict encoder that throws on unpaired surrogates (MQ-01) | User (MQ-01) | S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `src/WeatherParser.Parsing/Mqtt/MqttMessage.cs`, `MqttQualityOfService.cs`, `MqttMessageType.cs`, `MqttTopicTemplate.cs`, `MqttMessageException.cs`, `MqttMessageErrorIds.cs` | Create | Data types, exception, IDs |
| `src/WeatherParser.Parsing/Mqtt/IMqttTopicTemplateParser.cs`, `MqttTopicTemplateParser.cs`, `IMqttTopicValidator.cs`, `MqttTopicValidator.cs` | Create | Rule services |
| `src/WeatherParser.Parsing/Parsing/ServiceCollectionExtensions.cs` | Change | Registration |
| `tests/WeatherParser.Parsing.Tests/Mqtt/MqttTopicTemplateParserTests.cs`, `MqttTopicValidatorTests.cs`, `MqttMessageTests.cs` | Create | Unit tests |
| `docs/p8/P8_VERIFICATION.md` | Change | P8.03 section |

## 5. Procedure
Every slice: qwen generates the Red tests from a brief that names every planned test below; the orchestrating agent compares the generated file with this list before Green, runs the tests and records the observed failures; qwen generates Green; the agent reviews the diff, builds, tests, measures coverage; Codex reviews the step; findings are fixed test-first.

### S1 Message entity
**Red**: `MqttMessage_Constructor_KeepsTopicPayloadQosRetainAndType`, `MqttMessage_NullOrEmptyTopic_Throws`, `MqttMessage_UndefinedQosOrType_Throws`, `MqttMessage_PayloadIsACopy` (changing the caller's array after construction leaves the message unchanged), `MqttQualityOfService_Values_AreZeroOneTwo`.
**Green**: immutable message type; enums with explicit values.

### S2 Template parser
**Red**: `Parse_LiteralOnly_OneLiteralSegment`, `Parse_PlaceholderAtStartMiddleEnd`, `Parse_AdjacentPlaceholders`, `Parse_UnknownPlaceholder_ThrowsOptionsInvalid`, `Parse_PlaceholderNotAvailableForMessageType_ThrowsOptionsInvalid` (for example `{mosmix_id}` for `observation`, `{station_id}` for `forecast`), `Parse_UnbalancedOrNestedBraces_ThrowsOptionsInvalid` (`{source`, `source}`, `{{source}}`, `{}`), `Parse_LiteralPartInvalidCharacters_ThrowsOptionsInvalid` (`+`, `#`, U+0000, a control character in a literal part), `Parse_LeadingDollar_ThrowsOptionsInvalid` (only when the template starts with a literal), `Parse_EmptyLevelsThatAlwaysArise_ThrowOptionsInvalid` (`a//{source}`, `/a/{source}`, `a/{source}/`, per MQ-07), `Parse_SlashNextToPlaceholder_Accepted` (`haus/weather/measurements/{source}/state`, `{source}/state`, `a/{source}`, `a/{provider}/{station_id}`: literal parts ending or starting with `/` next to a placeholder are valid because placeholder values are never empty), `Parse_PlaceholderFirst_NoDollarCheckOnValue` (`{source}/x` accepted at parse time; a `$` at the start of a resolved topic is a resolved-topic failure, S3), `Parse_Null_ThrowsArgumentNull`, `AddWeatherParsing_ResolvesTemplateParser`; `ServiceRegistrationCompletenessTests` red while unregistered.
**Green**: a single-pass parser with the per-message-type placeholder table of MQ-04.

### S3 Topic and placeholder-value rules
**Red**: `ValidateValue_SafeValues_Accepted` (`dwd:00044`, `osthessenwetter:fulda-mitte`, `dwd-mosmix-l:P0489`, umlauts, leading zeros), `ValidateValue_UnsafeValues_ThrowPlaceholderValueInvalid` (empty, `/`, `+`, `#`, U+0000, U+0001, U+001F, U+007F, U+009F, an unpaired surrogate), `ValidateTopic_MaximumBytes_Accepted` (65 535 bytes with multi-byte characters), `ValidateTopic_OneByteAbove_ThrowsTopicInvalid`, `ValidateTopic_Wildcards_ThrowTopicInvalid`, `ValidateTopic_LeadingDollar_ThrowsTopicInvalid`, `ValidateTopic_EmptyLevels_PerMq07` (`a//b`, leading and trailing `/`), `ValidateTopic_EmptyOrNull_Throws`, `Failures_NameRuleNotValueContent` (the exception message names the rule and the position, never the full value), `AddWeatherParsing_ResolvesTopicValidator`.
**Green**: the validator.

**Refactor**: one character-rule table shared by the literal parts and the resolved topic, with the template-aware level and `$` rules kept separate from the complete-topic rules, with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P8 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S2 | T-P8-01 | REQ-015 | Template grammar |
| S3 | T-P8-02, T-P8-03 | REQ-015 | Value and topic rules on both sides of every bound |
| S1 to S3 | T-P8-12 | REQ-072 | Messages without value content |
| all | T-P8-16 | REQ-078, REQ-085, REQ-086 | Registration; Red evidence; coverage |

## 7. Evidence to Record
- Red names and failures, counts, coverage counters, authorship (qwen: code and tests; agent: corrections with reasons), Codex rounds

## 8. Risks and Limitations
- (a) The rules guard topic structure, not broker access control; scoped write permissions per source (REQ-061) stay with the broker configuration (P3)

## 9. Acceptance Criteria
- [ ] Every listed test green with recorded Red; no manufactured Red for characterization cases
- [ ] Every bound tested on both sides; failures carry the MQ-13 IDs
- [ ] Registration and architecture tests green; coverage gate passed; Codex 0 findings or reported
