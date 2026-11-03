# P2.12 – P2 Acceptance Record

Status (2026-10-20): audit decision ACCEPTED (section 8); P2 accepted by the user on 2026-10-20 (section 10). P2 is complete: accepted by the user (A7) and pull request #3 merged into `develop` on 2026-10-20 (A11, section 12). Recorded by the orchestrating agent from [P2_VERIFICATION.md](P2_VERIFICATION.md) at document revision `8fd3b15`.

## 1. Review identity

| Item | Value |
|---|---|
| Verified candidate | `bced7be380c3e637fdd042c4165cd074e25f25a6` (branch `features/develop/P2`) |
| Pull request | witalij-schrajer/WeatherParser#3 (draft) into `develop` |
| PR base | `64e460b475b4a29b9e3ddd331394076e53982d9e` |
| Records inspected | `docs/p2/P2_VERIFICATION.md` and `docs/p2/P2_EXPORT_CONTRACT.md` at `8fd3b15` (documentation-only commit on top of the candidate) |
| Toolchain | .NET SDK 10.0.402, `Microsoft.NETCore.App` 10.0.13, PowerShell 7.6.7 (target), Microsoft.CodeCoverage and `dotnet-coverage` 18.11.0 |
| Run settings | `coverage.runsettings`, SHA-256 `DC6F662E4F6FAAA853C33ED5497F182A31527FD126961411A604B683FFCF05D2` |

## 2. Candidate run results

| Run | Exit | Tests (Parsing, Coverage, Runner, SchemaGeneration, consumer, target pass) | Warnings | Package SHA-512 | Block coverage and gate |
|---|---|---|---|---|---|
| Windows x64, `artifacts/ci/bced7be-20261020T203551Z` | 0 | 880, 99, 56, 43, 18, 15 | 0 | `nTYtQEeriukG4BQnYfybiDaTpqHEiRQtuZqGCZY26g+BUEJT3Lwc7tMT++x1PqpsrK2vCYHJFRCI6SYjVUlMdg==` | 3035/3037 (99.93 %) against 54/56 (96.43 %); all five rules passed |
| NanoPi R76S, `artifacts/ci/arm64-bced7be-20261020T203606Z` | 0 | 880, 99, 56, 43, 18, 15 | 0 | `B96skjTa2IfCHhtVeOHDYUhHID8y6wTelfDOo6zIbjcd+WYAJ4wBpBfArnb4HcgRs2PgEw2GX3nP+XVZTXhNaA==` | 3035/3037 against 54/56; all five rules passed |
| Hosted workflow run 39493310294 | success | `windows-x64`, `ubuntu-x64`, `debian13-arm64` | — | — | per job (recorded separately) |

The package digest differs per run because the package embeds the commit and build metadata; within each run the restored hash equals the packed hash and the consumer lock changes only in the `WeatherParser.Parsing` `contentHash`. Package entries: `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE`, `schemas/export/v1/weatherparser-export-v1.xsd` and NuGet metadata. Output schema SHA-256: `4817672EE025B1F62FF71A0DC91FC92DFD90C8FDE230F44DCABCD6FC91D0A42B` on Windows, on the target and in the package.

## 3. Platform evidence

The NanoPi R76S (Debian 13 trixie, `linux-arm64`, ICU 76, tzdata `2025b-4+deb13u1`, system locale `C.utf8`) ran the corrected CONTRIBUTING.md section 7 procedure and the P2-specific checks (serializers, `Europe/Berlin`, `en-US`/`de-DE` cultures through ICU data, byte-identical schema regeneration, package consumer). All standard test projects also passed in a network namespace with loopback only. The hosted `debian13-arm64` container is not counted as target evidence. Details: [P2.11 record](P2_VERIFICATION.md#11-p211--integrated-coverage-and-platform-verification) and [COMPATIBILITY.md](../COMPATIBILITY.md).

## 4. Requirement and test evidence matrix (T-P2-01 – T-P2-28)

| T-P2 ID | Requirements | Tests | Red or characterization evidence | Candidate and runs | Status |
|---|---|---|---|---|---|
| T-P2-01 | REQ-074, REQ-076–077 | `ExportEntitySerializableTests` (traversal from all five roots) | Red in sections 2 and 8.2 (S8: "found ... ExportDocument, ExportError"); sections 3, 6, 7 characterization | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-02 | REQ-023, REQ-067–068 | `CdcStationListContractTests`, `ListEntryOrderingTests`, `ListRequiredMemberGuardTests`, `ListReadBackTests` | Red in section 3 | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-03 | REQ-101–102, REQ-067 | `MosmixLocationListContractTests`, `ListEntryOrderingTests`, `EnumVocabularyGuardTests`, `ListRequiredMemberGuardTests` | Red in section 3 | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-04 | REQ-050, REQ-052–053 | `QuantityValidatorTests`, `ObservationDatasetBuilderTests`, `ObservationBuilderRulesTests`, `ObservationDatasetContractTests`, `ObservationGraphValidationTests` | Red in sections 5 and 6; section 8.2 characterization for the existing graph rules | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-05 | REQ-051, REQ-067 | `UtcTimestampContractTests`, `CultureInvarianceContractTests`, `NormalizedTimeSerializationTests`, `AllRootsCultureTests` | Red in sections 2 and 4; section 8.2 characterization (all roots) | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-06 | REQ-042–043, REQ-056 | `SourceTimeNormalizerTests`, `ObservationBuilderRulesTests` | Red in sections 4 and 6 | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-07 | REQ-042–043, REQ-082 | `SourceTimeNormalizerTests`, `TimeZoneResolverTests` | Red in section 4; target tzdata run in V4 | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-08 | REQ-050, REQ-056 | `ObservationDatasetBuilderTests`, `ObservationBuilderRulesTests`, `ObservationGraphValidationTests` | Red in section 6 | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-09 | REQ-054 | `PressureConverterTests`, `ObservationPressurePairValidationTests` | Red in sections 5 and 6 | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-10 | REQ-057 | `OutputRoundingTests`, `WindSpeedConverterTests` | Red in sections 5 and 6 | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-11 | REQ-055, REQ-106 | `PeriodRateConverterTests`, `AveragingPeriodTests` | Red in section 5 | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-12 | REQ-052, REQ-055, REQ-106 | `QuantityKindSeparationTests`, `ObservationBuilderRulesTests`, `ForecastRunBuilderRulesTests` | Red in sections 5, 6 and 7 | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-13 | REQ-105, REQ-111 | `ForecastRunContractTests`, `ForecastRunBuilderTests`, `ForecastGraphValidationTests` | Red in section 7 (S2 characterization) | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-14 | REQ-105–107 | `ForecastRunBuilderTests`, `ForecastRunBuilderRulesTests`, `ForecastGraphValidationTests` | Red in section 7; section 8.2 characterization | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-15 | REQ-062, REQ-067–069, REQ-074–075 | `ExportDocumentContractTests`, `ExportErrorRuleTests`, `AllRootsDeterminismTests` | Red in section 8.2 (S9 characterization) | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-16 | REQ-067, REQ-075, REQ-077 | `ObservationExportEquivalenceTests`, `ForecastRunExportEquivalenceTests`, `AllRootsEquivalenceTests`, `AllRootsDeterminismTests`, `ExportDocumentReadBackTests` | Red in section 2; sections 6, 7 and 8.2 characterization | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-17 | REQ-067, REQ-076–077 | `EscapingContractTests`, `ObservationDatasetJsonContractTests`, `AllRootsEquivalenceTests`, `LineBreakPreservationTests` | Red in sections 2, 3 and 8.2 (C-92 regression) | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-18 | REQ-068–069, REQ-071 | `ExportSerializerStreamContractTests`, `JsonFailureWrappingTests`, `SupportedRootTests`, `SerializerStreamBehaviourTests`, `ExportGraphValidationTests`, `ListGraphValidationTests` | Red in sections 2 and 8.2 (S6 characterization, mutation-checked) | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-19 | REQ-070–071, REQ-077–078 | `ServiceCollectionExtensionsTests`, `ServiceRegistrationCompletenessTests`, `NoSharedStateTests` | Red in sections 2 to 7 (registration) and 10.2 (static state); S1 characterization, mutation-checked | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-20 | REQ-079 | `GoldenFileTests`, `ReproducibilityTests`, `ExportSchemaGeneratorTests` | Red in section 9.3 (S2 characterization); golden file matched on three hosted platforms | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-21 | REQ-067, REQ-076, REQ-079 | `SchemaRefinementTests`, `SchemaRefinementValidationTests`, `RealExportValidationTests`, `SchemaNegativeTests` | Red in section 9.3 (S4 characterization) | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-22 | REQ-079, REQ-083 | `SecureValidationTests` | Characterization on .NET 10 (section 9.3), mutation-checked; Red for the semantic DTD path in section 9.5 | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-23 | REQ-074–079 | `ContractMappingGuardTests`, `ClassificationGuardTests`, `RefinerMappingGuardTests` | Characterization (S7) and Red for the review fixes in section 9.3, mutation-checked | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-24 | REQ-012, REQ-077, REQ-082 | `PackageConsumptionTests`, `PackageServiceResolutionTests`, `PackageSerializationTests`, `PackageSchemaDeliveryTests` | Red in section 10.2 | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-25 | REQ-082, REQ-086 | `PackageSchemaDeliveryTests` and the ci-local.ps1 package check | Red in section 10.2; S6 run in section 10.10 | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-26 | REQ-080–083, REQ-085 | all standard test projects | this audit, the mock rule check and the network-blocked target run | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-27 | REQ-086 | ci-local.ps1 coverage gate | V2 gate result | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |
| T-P2-28 | REQ-005, REQ-017, REQ-084 | target run and P2-specific checks | V4 results | `bced7be`: Windows `bced7be-20261020T203551Z`, target `arm64-bced7be-20261020T203606Z`, hosted 39493310294 | PASSED |

## 5. Contract and version decisions

The accepted public contract is [P2_EXPORT_CONTRACT.md](P2_EXPORT_CONTRACT.md), decisions C-01 to C-116 (with the C-109 amendment):

- Roots (C-01, C-88): `export_document`, `cdc_station_list`, `mosmix_location_list`, `observation_dataset`, `forecast_run`; one complete document per serialization.
- Version (C-02, C-03, C-105): `schema_version` `"1"`, XML namespace `urn:weatherparser:export:v1`; v1 freezes at P2 acceptance, afterwards only additive optional members; any breaking change needs `schema_version` `"2"` and namespace `urn:weatherparser:export:v2`.
- Omission, zeros and nil (C-04, C-05, C-100): missing values omitted in both formats, zeros written, no `null` or `xsi:nil`, empty collections written.
- Time (C-06, C-28 to C-35, C-102): UTC `YYYY-MM-DDTHH:mm:ss` without suffix; `Europe/Berlin` inference only with `estimated_date`.
- Numbers and units (C-07, C-36 to C-47): invariant decimals, REQ-057 output rounding with ties away from zero, pressure pair from one original.
- Observation, forecast, envelope and error graphs (C-48 to C-94); output schema generated and delivered in the package (C-95 to C-106); DI and package consumption (C-107 to C-116).
- User approvals: section 1 of the contract records every approval with date and wording (P2.02 to P2.10 decisions and the C-109 amendment, all 2026-10-20); each was preceded by read-only Codex reviews where AGENTS.md requires them.

## 6. Limitations and open items

| Item | Owner |
|---|---|
| XML cancellation is coarse (before serialization and during the copy, C-10) | Accepted limitation; revisit if a later phase needs finer cancellation |
| Several rows rely on characterization evidence where behavior already existed (section 4) | Recorded; no rework planned |
| Builders and root validation implement the same rules separately; equivalence tests guard divergence | P5–P7 (when builders are used by parsers) |
| Hosted ARM64 is a container; target evidence exists only for the NanoPi R76S | P9 for release and hardware acceptance (REQ-084) |
| `de_DE.UTF-8` system locale not installed on the target; culture checks use ICU data | Recorded platform fact |
| Forecast-only quantities (gusts, probabilities, totals) not yet in the contract | P7 (additive contract decisions) |
| Coordinate parsing, geodesy, catalogs and availability derivation | P4 ([P4 handoff](P4_HANDOFF.md)) |
| Provider extraction and source mappings | P5, P6, P7 |
| MQTT payloads and topics | P8 |
| CLI, configuration, exit codes, broker transport | P3 |
| Publication, package ID, feed, release | P9 |
| P1 limitations ([P1 acceptance record](../p1/P1_12_ACCEPTANCE.md)) | Unchanged |

## 7. Phase plan checklist (PHASE_P2_PLAN.md section 10)

| Checklist item | Evidence |
|---|---|
| P2.01 revalidation and implementation authorization | P2.01 record (section 1 of P2_VERIFICATION.md) |
| Contract decisions before implementation | Contract section 1 approvals precede each step's first Red test |
| Typed `[Serializable]` graphs | T-P2-01 to T-P2-04, T-P2-13 to T-P2-15 |
| Real JSON/XML equivalence, UTC, culture, omissions, escaping, ordering | T-P2-05, T-P2-16, T-P2-17 |
| Normalization, times, units, periods, pressure, rounding | T-P2-06 to T-P2-12 |
| Serializer failure, cancellation, streams, DI and independent consumers | T-P2-18, T-P2-19 |
| Deterministic XSD generation and validation | T-P2-20 to T-P2-23 |
| Same-run package consumption, hashes, contents, schema delivery | T-P2-24, T-P2-25 |
| TDD evidence and T-P2 mapping | Section 4; T-P2-26 |
| Coverage minimum and comparison without bootstrap or bypass | T-P2-27; section 2 |
| P2 platform behavior on the target | T-P2-28; section 3 |
| Result record with SHAs, versions, counts, hashes, authorship, limitations | This record and P2_VERIFICATION.md sections 2 to 11 |
| Pull request ready for review, review resolved, merged by the user | Done (A9 to A11, section 12) |
| P4 receives contracts, helpers and responsibilities | [P4_HANDOFF.md](P4_HANDOFF.md); plans updated after acceptance (A8) |

## 8. Audit decision

**ACCEPTED** (orchestrating agent's evidence audit, 2026-10-20): every T-P2 row is PASSED with tests and run evidence for candidate `bced7be`; P2.11 reported READY with no open defect; every contract decision is approved; each limitation has an owner. This audit decision is not the user's acceptance.

## 9. Authorship

The evidence matrix rows were formatted by qwen3-coder:30b from the mapping extracted and verified by the orchestrating agent (section 11.7 of P2_VERIFICATION.md); qwen drafted the [P4 handoff](P4_HANDOFF.md) from the orchestrating agent's facts. The orchestrating agent wrote this record from its verified records and run results. Authorship of every step is recorded in P2_VERIFICATION.md sections 2 to 11.

## 10. User decision

**P2 accepted by the user on 2026-10-20.** The user selected "Accept P2" for the question "P2.12 A7: do you accept P2?" after the orchestrating agent presented this record (candidate `bced7be`, all 28 T-P2 rows PASSED, gates 3035/3037 against 54/56 on Windows and the NanoPi R76S, hosted CI green, no open defect, every contract decision approved, limitations with owners, audit decision ACCEPTED) and the [P4 handoff](P4_HANDOFF.md). No dispositions were added. P2 is complete after the merge of pull request #3 into `develop` (A11); status documents follow in A8.

## 11. Status updates after acceptance (A8)

On 2026-10-20, after the user's acceptance: AGENTS.md (phase-status line and dated line), README.md (status line, dated line, contract-version sentence), `DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md`, `PHASE_P2_PLAN.md` (status and the section 10 checklist; the pull-request item stays open), the P2.11 plan (completed) and the P2.12 plan (A9 to A11 pending) were updated; qwen drafted the AGENTS.md, README.md and `PHASE_P2_PLAN.md` passages (each verified exact), the orchestrating agent made the remaining status edits. Because README.md is packed, the CONTRIBUTING.md section 4 package check ran on the working tree: package entries as in section 2, `README.md`, `LICENSE` and the schema byte-identical to the working tree, restored hash equal to the packed hash, 18 consumer tests passed; the consumer-lock refresh was discarded.

## 12. Ready for review, review and merge (A9 to A11)

- On 2026-10-20, pull request #3 was marked ready for review; its `ready_for_review` CI run 39495576541 on head commit `eea3803` (`eea38035ac8bc800f70c21e06b1e2722404fc4b8`) succeeded.
- The review produced no findings (no review comments, no review submissions).
- On 2026-10-20 at 21:03:47 UTC, the user (`witalij-schrajer`) merged pull request #3 into `develop` (merge commit `74bf625eb4b2fbf49cbce7d1cee42b6302f2e47b`).

These records were written for the first commit of the P4 branch `features/develop/P4` (user decision 2026-10-20), because the P2 branch was deleted after the merge; qwen drafted the status texts, and the orchestrating agent verified the run, review and merge facts with GitHub.