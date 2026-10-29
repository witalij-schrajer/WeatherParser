# P8.10 – P8 Acceptance Record

**Status (2026-10-29): the evidence audit (section 5) accepts the implementation candidate `290a0eb`; the user's acceptance of P8 and of the complete library is pending (A7, section 7). Before the user is asked, pull request #14 goes through the automatic Codex review to 0 findings (AGENTS.md "Phase implementation" step 3; section 8).** Plan: [P8_10_ACCEPTANCE_HANDOFF_PLAN.md](P8_10_ACCEPTANCE_HANDOFF_PLAN.md).

## 1. Review identity (A1)

| Item | Value |
|---|---|
| Implementation candidate | `290a0eb7975beaebc90e543f10d507822e7ccab7` (`290a0eb`), branch `features/develop/P8`: the P8.08 commit |
| Pull request | [#14](https://github.com/witalij-schrajer/WeatherParser/pull/14) into `develop`, draft since the P8.02 approval |
| Integration target | PR base `8ed51c725333168d6e072369343c4eb449f8356c`, unchanged since P8.01 |
| Head before this record | `3c3d1c7` (the P8.09 record, documentation only on top of `290a0eb`); this record, the P8.09 outcome and the P3 handoff change documentation only |
| Verification | [P8.09 record](P8_09_INTEGRATED_VERIFICATION.md): READY on `290a0eb` on Windows, hosted CI run 40800979162 and the NanoPi R76S |

## 2. Requirement and test evidence matrix (A2, T-P8-01 – T-P8-17)

Platform evidence for every row: the P8.09 runs on candidate `290a0eb` (Windows `ci-local.ps1` exit 0, hosted CI run 40800979162 with all jobs green, NanoPi R76S `ci-local.ps1` exit 0): Parsing 3213 of 3220 passed (the seven opt-in live and resource checks are not executed in standard runs), package consumer 61, gate 11102/11103 against the baseline 10248/10249. "Red" names the step whose section in [P8_VERIFICATION.md](P8_VERIFICATION.md) holds the Red → Green → Refactor evidence; "characterization" marks first-run passes over behavior implemented test-first earlier (P8.06, P8.07), as the step plans prescribe.

| T-P8 ID | Requirements | Tests | TDD evidence | Status |
|---|---|---|---|---|
| T-P8-01 | REQ-015, MQ-04 | `MqttTopicTemplateParserTests` (grammar, placeholders per message type, braces, literal characters, `$`, empty levels, length on both sides), `MqttTopicOptionsValidatorTests` (`Validate_InvalidTemplate_ThrowsOptionsInvalidWithMessageType`) | Red, P8.03 S2; P8.04 S2 | PASSED |
| T-P8-02 | REQ-015, MQ-06 | `MqttTopicValidatorTests` (unsafe and boundary values, unpaired surrogates), `MqttMessageSerializerTests` (`SerializeAsync_UnsafePlaceholderValue_ThrowsPlaceholderValueInvalid`, `SerializeAsync_ListSourceWithoutProviderPrefix_ThrowsPlaceholderValueInvalid`) | Red, P8.03 S3; P8.05 S1 (the list case a characterization within S2) | PASSED |
| T-P8-03 | REQ-015, MQ-01, MQ-07 | `MqttTopicValidatorTests` (65 535 and 65 536 bytes with multi-byte characters, wildcards, disallowed code points, `$`, empty levels), `SerializeAsync_ResolvedTopicTooLong_ThrowsTopicInvalid` | Red, P8.03 S3; P8.05 S1 | PASSED |
| T-P8-04 | REQ-061, REQ-062, MQ-05 | `MqttTopicOptionsValidatorTests` (whole-level and prefix rules, collisions, the property-style `Validate_AcceptedConfigurations_ResolveDistinctTopics` over eleven configurations), `MqttForecastMessageTests` (distinct product and location topics) | Red, P8.04 S2; characterization, P8.06 | PASSED |
| T-P8-05 | REQ-062, MQ-08 | `MqttTopicOptionsValidatorTests` (defaults, overrides, undefined QoS), `SerializeAsync_QosAndRetainFromMapping` | Red, P8.04 S1; P8.05 S1 | PASSED |
| T-P8-06 | REQ-016, MQ-09 | `MqttTopicOptionsValidatorTests` (empty options, snapshot, null handling), `Constructor_InvalidOptions_ThrowsOptionsInvalid`, `AddWeatherParsing_ResolvesMessageSerializer`, `Package_EmptyMqttOptions_MappingMissing` | Red, P8.04 S1; P8.05 S1; characterization, P8.07 | PASSED |
| T-P8-07 | REQ-050, REQ-069, REQ-075, MQ-11 | `SerializeAsync_PayloadEqualsJsonExportBytes` (four roots), `SerializeAsync_PayloadUtf8WithoutBom`, the payload equalities of `MqttSourceMessageTests`, `MqttForecastMessageTests` and the package tests | P8.05 S2 (characterization within the slice); characterization, P8.06 and P8.07 | PASSED |
| T-P8-08 | REQ-050, C-27, C-90, MQ-11, MQ-13 | `MqttMessageSerializerTests` (unsupported types, invalid and unassigned roots, placeholder-read failure, cancellation, the bound at and above the limit, `EffectiveBound_ProtocolFormula`, `PayloadCap_SubtractsMinimumTopicLength`, precedence) | Red, P8.05 S1 to S3 and its Codex round 1 | PASSED |
| T-P8-09 | REQ-050, REQ-061, REQ-105 | `MqttForecastMessageTests` (`ForecastPayload_HasNoMeasuredAt`, `ForecastRun_AlwaysForecastType`, `ObservationAndForecastTopics_NeverEqual_ForExampleConfiguration`) | Characterization, P8.06 | PASSED |
| T-P8-10 | REQ-061, REQ-062, C-68 | `MosmixLRun_OneMessageWithEveryStep`, `MosmixSRun_OneMessageWithEveryStep`, `SameIdBothProducts_DistinctTopics`, `TwoLocations_DistinctTopics`, `LargestRunPayloadSize_Recorded` (1000 steps, 724 316 bytes) | Characterization, P8.06 | PASSED |
| T-P8-11 | REQ-015, MQ-10, OW-14 | `MqttSerializerGenericityTests` (literal and comparison scanners with positive cases, constructor and dependency checks), `OsthessenWetterObservation_Synthetic_GenericMessage`, the list messages | Characterization, P8.06 | PASSED |
| T-P8-12 | REQ-072, MQ-13 | `Parse_FailureMessage_NamesRuleNotTemplateValue`, `Failures_NameRuleNotValueContent`, `Validate_FailureMessages_NameTypeAndRuleNotTemplate`, `SerializeAsync_FailureMessages_NoPayloadContent`, `MqttMessageSerializer_HasNoLoggerDependency` | Red, P8.03 to P8.05 (P8.04 test added test-first after its Codex round 1) | PASSED |
| T-P8-13 | REQ-012, REQ-013, REQ-016 | `PackageMqttSerializationTests`, `PackageLibraryIntegrationTests` (every source end to end from one consumer container, schema-valid XML, MQTT payload equal to the JSON, request counting, two containers, no broker or CLI assembly) | Characterization, P8.07 | PASSED |
| T-P8-14 | REQ-016, REQ-070, MQ-12 | `PackageConcurrencyTests` (16 parallel workloads equal to the sequential run including the MQTT envelope, no cross-talk with distinct fixtures, overlap of at least two calls proven), `NoSharedStateTests`, `ServiceRegistrationCompletenessTests` | Characterization, P8.07 | PASSED |
| T-P8-15 | REQ-090, REQ-092 | `ReadmeMqttSectionTests`; README package check | Red, P8.08 | PASSED |
| T-P8-16 | REQ-017, REQ-084, REQ-085, REQ-086 | Every step's recorded evidence; coverage 11102/11103 strictly above 10248/10249 on Windows, hosted CI and the NanoPi R76S | P8.03 to P8.09 | PASSED |
| T-P8-17 | REQ-062, C-55, C-92, MQ-15 | `IdenticalDatasets_ByteIdenticalPayloads`, `SameDatasetMeasuredAt_ChangedQuantityMeasuredAt_DifferentPayloads`, `CorrectedValueSameQuantityMeasuredAt_DifferentPayloads`, `SameReadingsLaterRetrievedAt_DifferentPayloads`, `SameForecastRun_DifferentRetrievedAt_SameSourceAndIssuedAt`; the README duplicate rule (`Readme_MqttSection_StatesDuplicateRuleAndRestrictions`) | Characterization, P8.06; Red, P8.08 | PASSED |

## 3. Contract summary (A3)

MQ-01 to MQ-15 of the [P8 MQTT contract](P8_MQTT_CONTRACT.md) were approved by the user on 2026-10-28 with the recommended option in every row and are implemented as approved. No export contract member changed (C-03, C-105); no new package dependency was added (no MQTT client library).

## 4. Limitations and open items (A4)

| Item | Owner |
|---|---|
| OsthessenWetter values must not be published until the operator's consent or other clarified conditions are recorded (OW-14); the serializer accepts them generically (MQ-10) | The user; P3 must not publish them by default |
| Two Codex findings rejected with reasons ([P8_VERIFICATION.md](P8_VERIFICATION.md) sections 3 and 5): the internal static character helper without an interface (precedent of the accepted library); template text not included in failure messages (each message type has one template, MQ-03) | Reported to the user with this record |
| "DI under load": closed for the tested mix (P8.07); behavior beyond that mix (other loads, long runs) is not shown | P3 and P9 |
| The payload bound limits message acceptance, not peak memory: the JSON serializer buffers the whole document (C-10) | Recorded (README) |
| Broker behavior (retained-state replacement, acknowledgment, TLS, broker limits such as a maximum packet size) is not shown by message tests | P3 |
| Implementation deviations from the step plans, recorded: the P8.06 tests split into three files; parts written by the orchestrating agent after repeated failed qwen rounds (named per step in the verification record) | Recorded |
| Inherited: builders and root validation implement the same rules separately; coarse XML cancellation (C-10); hosted `debian13-arm64` is a container, not target evidence; the package is not byte-reproducible; the MOSMIX_S evidence-refresh CLI exposure; the P7 limitations of [P7_12_ACCEPTANCE.md](../p7/P7_12_ACCEPTANCE.md) section 4 | P3, P9 and the user as recorded there |
| Example CLI, configuration, exit codes, broker transport | P3 ([P3 handoff](P3_HANDOFF.md)) |
| Publication, package ID, feed, release | P9 |

## 5. Audit decision (A5)

**ACCEPTED** (orchestrating agent's evidence audit, 2026-10-29, for candidate `290a0eb`):
- every T-P8 row is PASSED with tests and run evidence on `290a0eb`; characterization rows are labelled as such, as the step plans prescribe;
- the P8.09 outcome is READY on Windows, hosted CI and the NanoPi R76S with no open defect;
- the coverage gate passed with 11102/11103 against 10248/10249 on all three platforms;
- every contract row is approved and implemented, and each limitation has an owner.

This audit decision is not the user's acceptance.

## 6. Library acceptance summary (gate for P3)

| Capability | Accepting phase | Record | Candidate |
| --- | --- | --- | --- |
| Export entities, JSON and XML serializers, export schema v1, builders, units, rounding, UTC | P2 (amended additively in P7) | [P2_12_ACCEPTANCE.md](../p2/P2_12_ACCEPTANCE.md) | `bced7be` |
| Station and location selection (CDC, MOSMIX), MOSMIX_S evidence | P4 | [P4_12_ACCEPTANCE.md](../p4/P4_12_ACCEPTANCE.md) | `b6bc47f` |
| DWD observations | P5 | [P5_11_ACCEPTANCE.md](../p5/P5_11_ACCEPTANCE.md) | `fd461c8` |
| OsthessenWetter observations (library; publication open, OW-14) | P6 | [P6_11_ACCEPTANCE.md](../p6/P6_11_ACCEPTANCE.md) | `12e7205` |
| MOSMIX_L and MOSMIX_S forecasts | P7 | [P7_12_ACCEPTANCE.md](../p7/P7_12_ACCEPTANCE.md) | `2f1115b` |
| MQTT message serializer; the complete library through the package, also concurrently | P8 | this record | `290a0eb` (pending A7) |

With the user's acceptance of P8 (A7), the complete library of DEVELOPMENT_PLAN.md is accepted and P3 can start on a separate instruction.

## 7. User decision (A7)

Pending.

## 8. Ready for review, review and merge (A9 to A11)

- A9: the pull request is marked ready for review by the orchestrating agent under AGENTS.md "Phase implementation" step 3 (the end of the phase); the automatic Codex review rounds and their dispositions are recorded below.
- A9 executed on 2026-10-29: pull request #14 marked ready for review on head `5bfddc2` (the P8.09 outcome and these drafts).
- A10 round 1 (automatic review of `5bfddc2`, run 40834636986, implementation brief): 1 finding, verified against REQ-081 and the existing tests and partly confirmed: [P2] test subjects and shared dependencies not built in `[TestInitialize]`. Confirmed for `MqttMessageSerializerTests`: its helper wired the options validator, topic validator and JSON serializer anew in every test; they are now built once in `[TestInitialize]`, and each test only varies its options (data, the serializer's constructor input) (test-only change by the orchestrating agent: a mechanical two-place edit of a 650-line file, which qwen would have to reproduce in full; Parsing tests 3213 of 3220 green). Rejected for the container-based tests (`MqttSourceMessageTests`, `MqttForecastMessageTests` and the package tests): each test builds its own `AddWeatherParsing` container because its HTTP fixtures and configuration are its data, a shared container would couple tests through singletons and handler state, and the accepted composed-pipeline and package tests of P4 to P7 follow the same per-test host pattern (for example `PackageDwdObservationTests`). The pull request was converted to draft while fixing.
- New candidate: the fix changes tests only (the library is unchanged since `290a0eb`), but under the P8.09 plan any later code change makes a new candidate; S1 and S2 are repeated on it, and S3 (NanoPi R76S) needs a new authorization of the user.
- A11: the merge by the user: pending.

## 9. Authorship and review

Written by the orchestrating agent on 2026-10-29 from the P8 verification record, the P8.09 record, the P8 MQTT contract and the acceptance records of P2 and P4 to P7; not generated by qwen (AGENTS.md).
