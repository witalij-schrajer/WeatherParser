# P8.10 – P8 Acceptance Record

**Status (2026-10-29): P8 and the complete library accepted by the user on 2026-10-29 on candidate `ce166eb` (A7, section 7), after the automatic Codex review of pull request #14 reached 0 findings (round 5, section 8) and the integrated verification was repeated READY on `ce166eb` on Windows, hosted CI and the NanoPi R76S ([P8.09 record](P8_09_INTEGRATED_VERIFICATION.md) section 6); P8 is complete only once the user has merged pull request #14 (A11).** Plan: [P8_10_ACCEPTANCE_HANDOFF_PLAN.md](P8_10_ACCEPTANCE_HANDOFF_PLAN.md).

## 1. Review identity (A1)

| Item | Value |
|---|---|
| Implementation candidate | `290a0eb7975beaebc90e543f10d507822e7ccab7` (`290a0eb`), branch `features/develop/P8`: the P8.08 commit; after the test-only PR #14 review fixes the accepted candidate is `ce166ebdcd82a6d5d92c5a926d9eb02ef0b17221` (`ce166eb`, section 8), with P8.09 repeated READY on it |
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
| **Process deviation (AGENTS.md "AI-assisted development")**: besides the parts written after repeated failed qwen rounds (an allowed exception), the orchestrating agent made several small mechanical test and test-support edits itself without a listed exception (the line insertions of `MqttTopicOptions` into the container setups in P8.05, small test-datum and assertion corrections in P8.04 to P8.07, the round-1 review fix of section 8); each is named in [P8_VERIFICATION.md](P8_VERIFICATION.md) or section 8; the production code was generated by qwen apart from the restored usings, comment stripping and doc summaries recorded per step | Accepted as documented by the user at the acceptance (A7, section 7) |
| Inherited: builders and root validation implement the same rules separately; coarse XML cancellation (C-10); hosted `debian13-arm64` is a container, not target evidence; the package is not byte-reproducible; the MOSMIX_S evidence-refresh CLI exposure; the P7 limitations of [P7_12_ACCEPTANCE.md](../p7/P7_12_ACCEPTANCE.md) section 4 | P3, P9 and the user as recorded there |
| Example CLI, configuration, exit codes, broker transport | P3 ([P3 handoff](P3_HANDOFF.md)) |
| Publication, package ID, feed, release | P9 |

## 5. Audit decision (A5)

**ACCEPTED** (orchestrating agent's evidence audit, 2026-10-29, for candidate `290a0eb`):
- every T-P8 row is PASSED with tests and run evidence on `290a0eb`; characterization rows are labelled as such, as the step plans prescribe;
- the P8.09 outcome is READY on Windows, hosted CI and the NanoPi R76S with no open defect;
- the coverage gate passed with 11102/11103 against 10248/10249 on all three platforms;
- every contract row is approved and implemented, and each limitation has an owner.

The repeated integrated verification on `ce166eb` (READY on all three platforms with the same counters, [P8.09 record](P8_09_INTEGRATED_VERIFICATION.md) section 6) keeps this decision valid for `ce166eb`, whose changes since `290a0eb` touch tests and documentation only.

This audit decision is not the user's acceptance.

## 6. Library acceptance summary (gate for P3)

| Capability | Accepting phase | Record | Candidate |
| --- | --- | --- | --- |
| Export entities, JSON and XML serializers, export schema v1, builders, units, rounding, UTC | P2 (amended additively in P7) | [P2_12_ACCEPTANCE.md](../p2/P2_12_ACCEPTANCE.md) | `bced7be` |
| Station and location selection (CDC, MOSMIX), MOSMIX_S evidence | P4 | [P4_12_ACCEPTANCE.md](../p4/P4_12_ACCEPTANCE.md) | `b6bc47f` |
| DWD observations | P5 | [P5_11_ACCEPTANCE.md](../p5/P5_11_ACCEPTANCE.md) | `fd461c8` |
| OsthessenWetter observations (library; publication open, OW-14) | P6 | [P6_11_ACCEPTANCE.md](../p6/P6_11_ACCEPTANCE.md) | `12e7205` |
| MOSMIX_L and MOSMIX_S forecasts | P7 | [P7_12_ACCEPTANCE.md](../p7/P7_12_ACCEPTANCE.md) | `2f1115b` |
| MQTT message serializer; the complete library through the package, also concurrently | P8 | this record | `ce166eb` (accepted 2026-10-29, A7) |

With the user's acceptance of P8 (A7), the complete library of DEVELOPMENT_PLAN.md is accepted and P3 can start on a separate instruction.

## 7. User decision (A7)

- Question (2026-10-29, chat): "Do you accept P8 and the complete library on candidate ce166eb?", with the options "Accept, rerun NanoPi" and "Accept, no rerun"; the options accepted the process deviation of section 4 as documented.
- Answer (2026-10-29, chat): "Accept, rerun NanoPi".
- Recorded: P8 (the MQTT message serializer and the complete library consumed through the package, also concurrently) and the complete library of section 6 are accepted by the user on candidate `ce166eb`; the process deviation of section 4 is accepted as documented; the two rejected Codex findings of section 4 were reported with this record. The authorized NanoPi rerun on `ce166eb` passed ([P8.09 record](P8_09_INTEGRATED_VERIFICATION.md) section 6).
- The acceptance does not merge pull request #14, does not start P3 and does not lift the OW-14 restriction: P8 is complete once the user has merged the pull request, and P3 starts only on a separate instruction ([P3 handoff](P3_HANDOFF.md)).

## 8. Ready for review, review and merge (A9 to A11)

- A9: the pull request is marked ready for review by the orchestrating agent under AGENTS.md "Phase implementation" step 3 (the end of the phase); the automatic Codex review rounds and their dispositions are recorded below.
- A9 executed on 2026-10-29: pull request #14 marked ready for review on head `5bfddc2` (the P8.09 outcome and these drafts).
- A10 round 1 (automatic review of `5bfddc2`, run 40834636986, implementation brief): 1 finding, verified against REQ-081 and the existing tests and partly confirmed: [P2] test subjects and shared dependencies not built in `[TestInitialize]`. Confirmed for `MqttMessageSerializerTests`: its helper wired the options validator, topic validator and JSON serializer anew in every test; they are now built once in `[TestInitialize]`, and each test only varies its options (data, the serializer's constructor input) (test-only change by the orchestrating agent: a mechanical two-place edit of a 650-line file, which qwen would have to reproduce in full; Parsing tests 3213 of 3220 green). Rejected at first for the container-based tests (`MqttSourceMessageTests`, `MqttForecastMessageTests` and the package tests), on the argument that a shared container would couple tests; that argument was wrong and the rejection was withdrawn in round 3. The pull request was converted to draft while fixing.
- A10 round 2 (automatic review of `2405824`, run 40835157692): 2 findings, both confirmed: [P2] tests with the identical default configuration still built their own serializer (now a default `_serializer` built in `[TestInitialize]` and used by the eleven tests with that configuration; qwen3-coder generated the change, which also moved `SerializeAsync_CancelledBeforeStart_ThrowsOperationCanceled_NoSerialization` to the default serializer, reviewed and accepted as equivalent because cancellation is checked before any configuration is used; Parsing tests 3213 of 3220 green); [P2] the round-1 test edit was made by the orchestrating agent without an AGENTS.md exception ("mechanical" and "large file" are not listed exceptions) and is recorded here as a process deviation for the user's disposition; the round-2 fix went through qwen as required. The same reasoning applied to several earlier mechanical test edits of P8.03 to P8.07 that the verification record names; they are part of the same deviation (see section 4).
- A10 round 3 (automatic review of `56acbbc`, run 40836576709): 1 finding, verified and confirmed: [P2] the package tests (and, by the same reasoning, the other container-based test classes) still built their default host inside each test; the round-1 rejection argued that a shared container couples tests, but MSTest runs `[TestInitialize]` before every test, so a fresh default host per test keeps the isolation. The round-1 rejection is therefore withdrawn. Fixed through qwen3-coder (one request per file, each diff reviewed): `PackageMqttSerializationTests`, `PackageLibraryIntegrationTests` and `PackageConcurrencyTests` build their default host (and its services) in `[TestInitialize]` and dispose it in `[TestCleanup]`; `MqttForecastMessageTests` and `MqttSourceMessageTests` build their default container and serializer there. Tests with another configuration keep their own construction: two package tests with other topic options, the two-container test, the OsthessenWetter and the custom-template observation tests, and the two station-list tests with their own clock and catalog fixtures. Parsing tests 3213 of 3220 and package consumer 61/61 green; the concurrency tests passed three consecutive runs. The hosted CI run of `56acbbc` (40836576650) passed.
- A10 round 4 (automatic review of `d542a2b`, run 40838047561): 1 finding, confirmed and fixed: [P3] the README project-status paragraph still said "P8 is not yet verified on the target" from P8.08 (now: verified on `290a0eb` in P8.09, the test-only review fixes' target rerun and the user's acceptance pending); README package check passed (README.md SHA-256 `88F45E1537916FF424C6CE85299FF8238F000A472530A97524B92FB6DD2B0D9A`, package consumer 61/61). The hosted CI run of `d542a2b` passed.
- A10 round 5 (automatic review of `ce166eb`, run 40838662078): 0 findings; the review loop ended and the user was asked for the acceptance (section 7). The commit recording the acceptance goes through one more review round, recorded in its pull request comment only.
- New candidate: the fix changes tests only (the library is unchanged since `290a0eb`), but under the P8.09 plan any later code change makes a new candidate; S1 and S2 were repeated on `ce166eb`, and S3 (NanoPi R76S) after the user's authorization with the acceptance; all READY ([P8.09 record](P8_09_INTEGRATED_VERIFICATION.md) section 6).
- Acceptance commit (2026-10-29): records section 7, the P8.09 rerun and the status passages; README package check on its working tree (README status passage changed): README.md SHA-256 `CCFAADEDA7818A71375DA1BB4DB96170067933782D9BA08273DF7F83CEDEE7E4` identical in the package with LICENSE and the export schema, packed SHA-512 equals the restored hash, package consumer 61/61; `ci-local.ps1` on the committed revision is run before the push.
- A11: the merge by the user: pending.

## 9. Authorship and review

Written by the orchestrating agent on 2026-10-29 from the P8 verification record, the P8.09 record, the P8 MQTT contract and the acceptance records of P2 and P4 to P7; not generated by qwen (AGENTS.md).
