# P2.10 – DI and Actual Package Consumption: Implementation Plan

**Status: completed (2026-10-20, marked completed by the user after the hosted CI passed).** Implemented test-first in commits `f7b9b9e` and `186f900` (C-109 amendment for coverage-instrumentation types); hosted CI run 39487633043 of draft pull request #3 passed on Windows x64, Ubuntu x64 and Debian 13 ARM64, and the S6 `ci-local.ps1` run on `186f900` passed with exact package entries, matching packed and restored SHA-512 and the single permitted consumer-lock rewrite. Contract decisions C-107 to C-116 refine this plan (reflection-based service list, `decimal` and `Regex` on the static-field allowlist, schema packed at `schemas/export/v1/` and consumed through `GeneratePathProperty`, mandatory README status correction); evidence in [P2_VERIFICATION.md](P2_VERIFICATION.md) section 10. Original planning note: this document started as a plan on 2026-10-20. This plan refines the P2.10 row of section 5 and the package paragraphs of sections 7 and 8 of [PHASE_P2_PLAN.md](../../PHASE_P2_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief and corrected by qwen in one review round (S1 Green, S2 compiler-generated exclusion, scope lists, artifact descriptions, evidence destination); the orchestrating agent split the csproj/lock-file artifact row, restored backticks and fixed one parenthesis that qwen did not apply. User review (2026-10-20): qwen replaced the readonly-only static-field check with an immutable-type allowlist and a direct cache-isolation test in one round.

## 1. Goal and Scope
Goal: prove that every P2 service is wired centrally and resolvable through real dependency injection, that missing dependencies fail visibly, that independent containers share no state, and that an independent consumer using the same-run versioned `.nupkg` through a `PackageReference` can resolve the services and serialize representative graphs of all five roots through the public API — with the schema delivered as decided in P2.09 and the package contents, hashes and locks verified exactly.

In scope:
- DI completeness and validation tests
- No-shared-state checks
- Consumer tests in `tests/WeatherParser.Package.Tests`
- The schema-delivery implementation
- Package-content allowlist change (only if the XSD ships)
- README/package-metadata review
- Documentation sync

Out of scope (owner):
- The integrated two-pass coverage gate and Windows/ARM64 platform evidence for the final candidate → P2.11
- Publication, package ID, feed and release versioning → P9
- CLI usage example → P3
- Parser registrations → P4–P7

## 2. Preconditions and Inputs
- Prerequisite steps: P2.08 and P2.09 complete; the P2.09 schema-delivery decision recorded.
- Existing repository facts used: `tests/WeatherParser.Package.Tests/WeatherParser.Package.Tests.csproj` references `WeatherParser.Parsing` through a versioned `PackageReference` from the local feed `artifacts/local-nuget`; `PackageConsumptionTests` checks that the deps file records a package (not a project) and that no CLI/MQTT assembly is referenced. It does not reference `Microsoft.Extensions.DependencyInjection` (the concrete container) yet.
- Required user authorization: decisions on schema delivery path, README update.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| XML serializer cache scope and static-state rule | per serializer instance (singleton per container) and no static fields except `const` or `static readonly` of an allowlisted immutable type / process-wide static cache | First option. Allowlist: `string`, the primitive numeric types, `bool`, `char`, enums, `TimeSpan`, `DateTime`; arrays, collections (`Dictionary`, `ConcurrentDictionary`, `List`), `XmlSerializer` and every other reference type are prohibited as static fields, because `readonly` freezes only the reference, not the contents | Orchestrator, recorded | S2 |
| Container validation in tests | `BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })` / default options | Validation on: missing dependencies fail when the container is built | Orchestrator, recorded | S1 |
| Consumer DI dependency | add `Microsoft.Extensions.DependencyInjection` (already pinned centrally at 10.0.0) to the consumer test project / test without a container | Add it (test-only; lock file and `DEPENDENCIES.md` updated) | Orchestrator, recorded | S3 |
| Schema package path (only if P2.09 chose shipping) | Proposed `schemas/export/v1/weatherparser-export-v1.xsd` inside the package / `contentFiles/any/any/...` | OPEN until the P2.09 decision; the path is fixed before the Red of S4 | User | S4 |
| README/package description update | describe the implemented export entities and serializers as implemented, parsers still planned / no change | Update, because README must distinguish planned and implemented features (REQ-090); the description in the csproj stays accurate ("Parser implementations are not available yet") | User | S5 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `tests/WeatherParser.Parsing.Tests/ServiceCollectionExtensionsTests.cs` | Change | Complete registration and validation tests |
| `tests/WeatherParser.Parsing.Tests/Architecture/NoSharedStateTests.cs` | Create | Proposed: container independence, direct cache isolation and the static-field allowlist |
| `tests/WeatherParser.Package.Tests/WeatherParser.Package.Tests.csproj` | Change | Add `Microsoft.Extensions.DependencyInjection` |
| `tests/WeatherParser.Package.Tests/packages.lock.json` | Change | Locked restore entry for the new dependency |
| `tests/WeatherParser.Package.Tests/PackageServiceResolutionTests.cs` | Create | Proposed: real DI from the package |
| `tests/WeatherParser.Package.Tests/PackageSerializationTests.cs` | Create | Proposed: all five roots in both formats through the package |
| `tests/WeatherParser.Package.Tests/PackageSchemaDeliveryTests.cs` | Create | Proposed: schema delivery as decided |
| `DEPENDENCIES.md` | Change | New direct test dependency of the consumer project |
| `src/WeatherParser.Parsing/WeatherParser.Parsing.csproj` | Change (only if XSD ships) | Pack the schema at the decided path |
| `scripts/ci-local.ps1` | Change (only if XSD ships) | `$expectedEntries` gains exactly that path |
| `CONTRIBUTING.md` | Change (only if XSD ships) | Package check entries |
| `README.md` | Change | Status of export features |
| `docs/p2/P2_VERIFICATION.md` | Change | Evidence records |

## 5. Procedure
### S0 – Delivery and consumer decisions
**Action:** Confirm the P2.09 delivery decision and the section 3 rows; record them.
**Roles:** The orchestrator records the user's decisions; qwen drafts prose.

### S1 – Complete registration with validation
**Red:** `AddWeatherParsing_ResolvesEveryP2ServiceWithValidation` (serializers, graph validator, ordering, time, quantity, builder services) and `ServiceProvider_MissingDependency_FailsOnBuild` (a collection that registers a builder without its quantity services). Expected failure: one service registered in an earlier step is missing from `AddWeatherParsing` or a dependency is registered with the wrong lifetime — confirm when the Red runs; if everything passes immediately, record the first test as characterization and keep the second as the missing-dependency Red (expected failure: the test is written before the validation options are applied).
**Green:** add any missing registration or correct the lifetime in `ServiceCollectionExtensions.AddWeatherParsing`; the tests build the provider with `ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }`.
**Refactor:** none expected.
**Roles:** qwen generates tests and minimal implementation; orchestrator writes briefs, runs Red and confirms failure reason, runs build, pack and tests, reviews diff and returns defects to qwen, performs repository operations and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S2 – No shared state between containers
**Red:** `TwoContainers_ProduceIndependentInstances`; `TwoContainers_DoNotShareSerializerCache` (resolve `IXmlExportSerializer` from two containers, serialize a root with each, and assert by reflection on the instances that their cache objects are different objects); and `LibraryTypes_DeclareNoStaticState` (reflection over the library assembly: every static field outside compiler-generated types, i.e. types marked with `CompilerGeneratedAttribute` such as lambda caches, must be `const` or a `static readonly` field whose type is on the section 3 allowlist). Expected failure: a process-wide `static readonly` dictionary cache fails both the allowlist check and the cache-isolation test, although it would pass a plain `readonly` check.
**Green:** Per-instance cache held in an instance field of the serializer; no static cache fields.
**Refactor:** none expected.
**Roles:** qwen generates tests and minimal implementation; orchestrator writes briefs, runs Red and confirms failure reason, runs build, pack and tests, reviews diff and returns defects to qwen, performs repository operations and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`.

### S3 – Package consumer resolves and serializes
**Red:** `Package_AddWeatherParsing_ResolvesSerializers` and `Package_SerializesAllFiveRootsInBothFormats` in `tests/WeatherParser.Package.Tests`, consuming the same-run `.nupkg`. Expected failure: the consumer project lacks the DI container dependency and the P2 types are not in the previously restored package; after adding the dependency and repacking, the tests run against the new package.
**Green:** Add `Microsoft.Extensions.DependencyInjection` to `tests/WeatherParser.Package.Tests/WeatherParser.Package.Tests.csproj`; repack per CONTRIBUTING.md section 4.
**Refactor:** none expected.
**Roles:** qwen generates tests and minimal implementation; orchestrator writes briefs, runs Red and confirms failure reason, runs build, pack and tests, reviews diff and returns defects to qwen, performs repository operations and records authorship.
**Check:** Pack and consumer procedure of CONTRIBUTING.md section 4.

### S4 – Schema delivery
**Red:** If shipped: `Package_ContainsExactlyTheVersionedSchemaEntry` and `Package_Schema_ValidatesConsumerExport`; expected failure: the schema is not packed and the allowlist rejects it (exit 60) — then add the exact path to the csproj, `$expectedEntries` and CONTRIBUTING.md section 4, nothing broader. If delivered separately: `Consumer_Export_ValidatesAgainstRepositorySchemaOfMatchingVersion`; document how consumers obtain the matching version.
**Green:** Add schema to package if shipped; otherwise document delivery method.
**Refactor:** none expected.
**Roles:** qwen generates tests and minimal implementation; orchestrator writes briefs, runs Red and confirms failure reason, runs build, pack and tests, reviews diff and returns defects to qwen, performs repository operations and records authorship.
**Check:** Pack and consumer procedure of CONTRIBUTING.md section 4.

### S5 – README and documentation sync
**Action (no Red: documentation):** Update `README.md` status, `DEPENDENCIES.md`, CONTRIBUTING.md where affected; run the CONTRIBUTING.md section 4 local package check; discard the local consumer-lock refresh as documented.
**Roles:** qwen drafts documentation text only; orchestrator runs every command and records the evidence.

### S6 – Package identity and hash evidence
**Action (run evidence, not a test change):** For a committed checkpoint supplied when the user requests a commit, run `pwsh ./scripts/ci-local.ps1 -Commit <checkpoint-sha> -TargetRef <PR-base-sha> -RunSettings coverage.runsettings` and record exact package entries, packed and restored SHA-512, the single permitted consumer-lock `contentHash` rewrite and unchanged other locked dependencies.
**Roles:** orchestrator runs command and records evidence.

## 6. Tests and Test IDs
| Step-local case | T-P2 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P2-19 | REQ-070–071, REQ-077–078 | Real DI resolves every interface-based P2 service with validation; missing dependencies fail (completes T-P2-19 with S2) |
| S2 | T-P2-19 | REQ-070–071 | Two containers share no instances or mutable static state |
| S3 | T-P2-24 | REQ-012, REQ-077, REQ-082 | Independent consumer uses the same-run versioned package, not a ProjectReference; real DI and serializers for all five roots |
| S4 | T-P2-24, T-P2-25 | REQ-079, REQ-082, REQ-086 | Schema delivered as decided; allowlist widened only by the exact path |
| S5 | T-P2-25 | REQ-082, REQ-086, REQ-090 | README change followed by the package check; documentation consistent |
| S6 | T-P2-25 | REQ-082, REQ-086 | Exact package entries; packed and restored hashes match; only the permitted consumer `contentHash` rewrite; other dependencies locked |

## 7. Evidence to Record
- All entries go to `docs/p2/P2_VERIFICATION.md`: Red names with observed failure messages, characterization results, commands, test counts per project, package file name, exact entries, packed and restored SHA-512, lock diff summary, checkpoint commit and script revision for S6, authorship; the user's decisions from S0 go to `docs/p2/P2_EXPORT_CONTRACT.md`.

## 8. Risks and Limitations
- (a) The consumer lock is stale by design because the package embeds the commit ID — only the documented isolated-clone rewrite is allowed, never a committed lock refresh.
- (b) Widening the allowlist is a CI-script change with no documentation-only exemption — exact path only, TDD first.
- (c) A static cache would leak state between consumers, and a readonly-only check would miss a static readonly collection — S2 uses a type allowlist plus a direct cache-isolation test.
- (d) Package tests run against the package built in the same run — a stale cached package gives false results, so the CONTRIBUTING.md stale-cache procedure applies.
- (e) The consumer's package assembly must not be counted twice in coverage (phase plan section 8).

## 9. Acceptance Criteria
- [x] S0 decisions recorded.
- [x] Red failures or characterization recorded for S1–S4.
- [x] The consumer resolves and serializes all five roots from the same-run package.
- [x] Missing dependencies fail visibly.
- [x] No static fields in the library except `const` or allowlisted immutable static readonly fields; serializer caches proven per instance.
- [x] Package entries exactly as decided.
- [x] Hashes and lock rewrite verified in S6.
- [x] README, DEPENDENCIES.md and CONTRIBUTING.md consistent.
- [x] All tests pass.
