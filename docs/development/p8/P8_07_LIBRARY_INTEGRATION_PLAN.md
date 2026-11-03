# P8.07 – Complete Library Integration through the Package: Implementation Plan

**Status: planned on 2026-10-28; accepted by the user on 2026-10-28 with the phase plan; reconciled on 2026-10-28 with the P8.02 contract MQ-01 to MQ-15 approved by the user on 2026-10-28 (P8.02 S5); complete on 2026-10-29 ([verification record](../../p8/P8_VERIFICATION.md#7-p807--complete-library-integration-through-the-package)).** Refines the P8.07 row of section 5 and the "Integration" row of section 4 of [PHASE_P8_PLAN.md](../PHASE_P8_PLAN.md).

## 1. Goal and Scope
Goal: prove the DEVELOPMENT_PLAN.md P8 acceptance — the complete library functions for DWD observations, OsthessenWetter and both MOSMIX products, with station selection, export and MQTT serialization, consumed independently from the same-run package without CLI or broker — and close the inherited "DI under load" item (MQ-12).
In scope: package-consumer tests in `tests/WeatherParser.Package.Tests` using only the public API of the same-run `.nupkg`: one container with every endpoint and options type registered; every parser and both selection services run offline through a consumer `HttpClient` handler; every result serialized as JSON, as XML validated against the packaged XSD (C-114) and as MQTT message; two independent containers with different endpoints and topic options; concurrent use of one container; the package boundary (no CLI or broker-transport assembly).
Out of scope with owner: README → P8.08; platform runs → P8.09.

## 2. Preconditions and Inputs
- P8.06 complete; MQ-09 and MQ-12 approved.
- Existing: `PackageConsumptionTests`, `PackageServiceResolutionTests`, `PackageSerializationTests`, `PackageSchemaDeliveryTests`, `PackageDwdObservationTests`, `PackageOsthessenWetterTests`, `PackageMosmixForecastTests`, `PackageSelectionTests`, `ConsumerHttpHandler`, `TestConsumerServices`, `ConsumerSelectionSupport`, `ConsumerFixtures`; the package build and isolated restore of `scripts/ci-local.ps1` (C-116).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Scope of "load" | Parallel calls on one container (MQ-12) / stress with many iterations | 16 parallel tasks mixing every parser and the three serializers on one container, each with its own handler-served fixture, results compared with a sequential run; no timing assertions | User (MQ-12) | S3 |
| Consumer fixtures | Public API only (C-112) / internal test helpers | Public API only; synthetic fixture bytes built inside the consumer project as in P5 to P7 | Orchestrator, recorded (C-112) | S1 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `tests/WeatherParser.Package.Tests/PackageLibraryIntegrationTests.cs` | Create | End-to-end consumer tests |
| `tests/WeatherParser.Package.Tests/PackageMqttSerializationTests.cs` | Create | MQTT serializer from the package |
| `tests/WeatherParser.Package.Tests/PackageConcurrencyTests.cs` | Create | Concurrent use |
| `tests/WeatherParser.Package.Tests/TestConsumerServices.cs`, `ConsumerSelectionSupport.cs` | Change | `MqttTopicOptions` registration (if not already done in P8.05) |
| `docs/p8/P8_VERIFICATION.md` | Change | P8.07 section |

## 5. Procedure
Every slice: roles and checks as in P8.03 section 5. The consumer builds against the same-run package, which contains the P8 types since P8.05, so the tests of this step exercise existing behavior: they are recorded as characterization tests (the P7.09 and P7.10 precedent), never with a manufactured Red; a failure they find is fixed test-first in the library.

### S1 MQTT serializer from the package
**Tests**: `Package_ResolvesMqttMessageSerializer`, `Package_MqttMessage_ForEveryRoot`, `Package_MqttOptionsInvalid_FailsOnResolution`, `Package_EmptyMqttOptions_MappingMissing`.

### S2 Complete library end to end
**Tests**: `Library_SelectDwd_ToJsonXmlMqtt`, `Library_SelectMosmix_ToJsonXmlMqtt`, `Library_ParseDwd_ToJsonXmlMqtt`, `Library_ParseOsthessenWetter_Synthetic_ToJsonXmlMqtt`, `Library_ParseMosmixL_ToJsonXmlMqtt`, `Library_ParseMosmixS_ToJsonXmlMqtt` (each: one container, the consumer handler counts requests, the XML validates against the packaged XSD, the MQTT payload equals the JSON document), `Library_TwoContainers_IndependentEndpointsAndTopics`, `Library_NoBrokerOrCliAssembly` (extends the existing boundary test).

### S3 Concurrent use (MQ-12)
**Tests**: `Library_ParallelParsesAndSerializations_EqualSequentialResults`, `Library_ParallelUse_NoCrossTalkBetweenRequests` (each task's handler sees only its own request URIs).

## 6. Tests and Test IDs
| Step-local case | T-P8 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1, S2 | T-P8-13 | REQ-012, REQ-013, REQ-016 | Complete library from the package |
| S3 | T-P8-14 | REQ-016, REQ-070 | Concurrent use |
| all | T-P8-16 | REQ-085, REQ-086 | Evidence; coverage |

## 7. Evidence to Record
- Test names and classification, counts, the `ci-local.ps1` package identity and hashes of the run, request counts per source, authorship, Codex rounds

## 8. Risks and Limitations
- (a) Concurrency tests show the absence of cross-talk in the tested mix, not under every load; recorded as such
- (b) The package is not byte-reproducible across builds (inherited, owner P9)

## 9. Acceptance Criteria
- [ ] Every source runs end to end from the package without CLI or broker
- [ ] JSON, schema-valid XML and MQTT message for every result
- [ ] Concurrent use equal to sequential use; "DI under load" closed or its remaining part carried with an owner
- [ ] Coverage gate passed; Codex 0 findings or reported
