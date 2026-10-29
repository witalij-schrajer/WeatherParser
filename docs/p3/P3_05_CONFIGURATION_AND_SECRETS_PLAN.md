# P3.05 – Configuration and Secrets: Implementation Plan

**Status: planned on 2026-10-29; under review, not yet accepted.** Refines the P3.05 row of section 5 and the "Configuration" row of section 4 of [PHASE_P3_PLAN.md](../../PHASE_P3_PLAN.md).

## 1. Goal and Scope
Goal: load one configuration deterministically, validate it before any retrieval, forward its values unchanged to the library's consumer registrations, resolve the DWD station by the approved precedence and keep credentials out of arguments, files in Git and logs.
In scope: the configuration loader (Proposed: `IConfigurationLoader`) and the typed entities of CL-04; the path resolution (`--Config`, then `WEATHERPARSER_CONFIG`); unknown-member rejection; mapping to the thirteen consumer registrations and to `MqttTopicOptions` (templates and `MaxPayloadBytes` only; QoS and retain members rejected, CL-05); the DWD station resolution (`--StationID` over `Dwd.ReferenceStationId`, CL-05); the credential source of CL-06 (Proposed: `ICredentialProvider`); the broker and limit settings of CL-13, CL-14 and CL-18 (read and range-checked here, used in P3.08 and P3.09); `.gitignore` entries; the example configuration with placeholders.
Out of scope with owner: the use of the settings in commands → P3.07; the transport → P3.08; log formatting → P3.06.

## 2. Preconditions and Inputs
- P3.04 complete; P3.02 approved CL-04, CL-05, CL-06, CL-13, CL-14, CL-16 (the permission setting) and CL-18.
- Inputs: REQ-014, REQ-016, REQ-030, REQ-060, REQ-062, REQ-064; the consumer registrations and their validation rules from the P3.01 extraction (endpoint validation `ENDPOINT-INVALID`, `SELECTION-LIMIT-INVALID`, `EVIDENCE-CACHE-SETTINGS-INVALID`, `MQTT-OPTIONS-INVALID` and the options of P5 to P7).

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| When library validation runs | At container build / on first resolution (as the library does) / eagerly after loading | Eagerly after loading, before the command starts: the CLI resolves the services whose constructors validate their options (for `MqttTopicOptions` the serializer, as the README recommends) inside a configuration-validation phase, so a configuration error is a configuration error (exit 2) and never a run-time failure after retrieval began; in JSON/XML mode the MQTT settings are not required (REQ-065) | Orchestrator, recorded (CL-05) | S2 |
| Credential file permission check | Linux mode bits / no check | On Linux, the file must not be readable by group or others (mode checked through `File.GetUnixFileMode`); on Windows no mode check, documented | Orchestrator, recorded (CL-06) | S3 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `examples/WeatherParser.Cli/Configuration/…` | Create | Entities, loader, mapping, station resolution, credential provider |
| `examples/weatherparser.example.json` (Proposed) | Create | Complete example with placeholders only (`example.invalid` endpoints, invented station IDs, no credentials) |
| `.gitignore` | Change | Local configuration and credential file names (before they are documented) |
| `examples/WeatherParser.Cli/CompositionRoot.cs` | Change | Registrations from the configuration |
| `tests/WeatherParser.Cli.Tests/Configuration/…` | Create | Unit and process tests |
| `docs/p3/P3_VERIFICATION.md` | Change | P3.05 section |

## 5. Procedure
Every slice: roles and checks as in P3.03 section 5.

### S1 Loading and precedence
**Red**: `Load_ConfigOptionPath_Used`, `Load_EnvironmentVariablePath_UsedWhenNoOption`, `Load_OptionOverridesEnvironmentVariable`, `Load_NoPath_ConfigurationMissing`, `Load_FileMissing_ConfigurationMissing`, `Load_MalformedJson_ConfigurationInvalid`, `Load_UnknownMember_ConfigurationInvalid` (top level and nested), `Load_MissingRequiredSection_ConfigurationInvalid`, `Load_NoImplicitSearch` (a configuration file in the working directory is ignored without a path), `Load_ExampleConfiguration_IsValid` (the committed example loads and maps).
**Green**: loader and entities.

### S2 Mapping to the library
**Red**: `Map_EveryConsumerRegistration_ForwardedUnchanged` (each endpoint `Uri`, limit and option equals the configured value), `Map_InvalidEndpoint_ConfigurationInvalidWithLibraryId`, `Map_InvalidLimit_ConfigurationInvalidWithLibraryId`, `Map_TopicTemplates_ToMqttTopicOptions`, `Map_QosOrRetainMember_ConfigurationInvalid` (QoS 0 and a retained station list), `Map_InvalidTopicTemplate_ConfigurationInvalidWithMqttOptionsInvalid`, `Map_JsonMode_MqttSectionNotRequired`, `Map_MqttMode_MqttSectionRequired`, `Map_BrokerAndLimitRanges` (each CL-14 and CL-18 value at both bounds accepted and one step outside rejected).
**Green**: mapping and the configuration-validation phase.

### S3 DWD station and secrets
**Red**: `ResolveStation_ArgumentOverConfiguration`, `ResolveStation_ConfigurationWhenNoArgument`, `ResolveStation_NeitherPresent_DwdStationMissing`, `Process_ParseDwdWithoutStation_ExitsBeforeRetrieval` (zero requests at the loopback fake server, no broker connection, no file), `Credentials_FromEnvironmentVariables`, `Credentials_FromCredentialFile`, `Credentials_FileReadableByOthers_Rejected` (Linux only, skipped with a recorded reason on Windows), `Credentials_InConfigurationFile_ConfigurationInvalid`, `Credentials_AsArgument_UsageError`, `Diagnostics_NeverContainCredentialMarker` (a distinctive marker as password: absent from every exception message and every captured stdout and stderr line of the process tests of this step).
**Green**: station resolution and credential provider; `.gitignore`.

**Refactor**: with all tests green.

## 6. Tests and Test IDs
| Step-local case | T-P3 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P3-05 | REQ-064 | Location, format, unknown members |
| S2 | T-P3-05 | REQ-014, REQ-016, REQ-062, REQ-065 | Forwarding, QoS and retain, mode-dependent sections |
| S3 | T-P3-06, T-P3-07 | REQ-030, REQ-060, REQ-072 | Station precedence, secrets |
| all | T-P3-20 | REQ-085, REQ-086 | Red evidence; coverage |

## 7. Evidence to Record
- Red names and failures, counts, coverage counters, authorship, the `.gitignore` diff, Codex rounds

## 8. Risks and Limitations
- (a) Environment variables are visible to other processes of the same user on some systems; the README names the credential file as the alternative (CL-06)
- (b) Unknown-member rejection makes configuration files strict; adding a member later is a CLI contract change recorded in the README

## 9. Acceptance Criteria
- [ ] Every listed test green with recorded Red
- [ ] Every configuration error detected before retrieval; no credential in arguments, Git, logs or exceptions
- [ ] Example configuration valid and free of private data; coverage gate passed; Codex 0 findings or reported
