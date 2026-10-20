# P2.09 – Reproducible Output XSDs: Implementation Plan

**Status: plan only (2026-10-20); nothing is implemented or verified yet.** This plan refines the P2.09 row of section 5, section 7 and the "XSD delivery and generator" decision of section 3 of [PHASE_P2_PLAN.md](../../PHASE_P2_PLAN.md). Drafted by the local qwen3-coder:30b model from the orchestrating agent's brief and corrected by qwen in one review round (technical-facts subsection, artifact rows, evidence destination); the orchestrating agent added one pair of path backticks. User review (2026-10-20): qwen changed S0 to build before the filtered spike run and to accept it only with TRX evidence of execution, over two rounds; the orchestrating agent added one period.

## 1. Goal and Scope
Goal: deterministic, versioned XSD files for every public XML export root, generated from the entity XML mappings by a recorded, reproducible procedure, refined only by deterministic steps inside that procedure, and proven by validating real exports of all five roots plus negative fixtures offline with DTDs and external resolution prohibited.

In scope:
- Generator feasibility spike
- Generator and refinement procedure
- Generated schema files under `schemas/export/v1/`
- Reproducibility check
- Positive and negative validation tests
- Secure offline validation settings
- The delivery decision
- Developer procedure in CONTRIBUTING.md
- Records

Out of scope (owner):
- Package contents and the package-content allowlist change → P2.10 (implements the delivery decision taken here)
- DWD MOSMIX input schema `pointforecast_dwd_extension_V1_0.xsd` and its types → P7 (REQ-112; kept strictly separate)
- CLI schema commands → never in P2 (the phase plan forbids a CLI under the name of a schema tool)

## 2. Preconditions and Inputs
- P2.08 complete (all roots and their XML mappings final)
- Existing repository facts used:
  - `src/WeatherParser.Parsing/Export/` contains entity types with XML mappings
  - `tests/WeatherParser.Parsing.Tests/` project for tests
  - `CONTRIBUTING.md` for developer procedures
  - `docs/p2/P2_EXPORT_CONTRACT.md` for contract decisions

### Technical facts to verify in S0 (not yet verified)
- `xsd.exe` is a .NET Framework, Windows-only tool and is not assumed to be installed; it is not used.
- `System.Xml.Serialization.XmlReflectionImporter` plus `XmlSchemaExporter` (both in .NET) export schemas from the `XmlSerializer` mappings of the entity types. Microsoft documents `XmlSchemaExporter` as infrastructure; the spike assesses its suitability ([XmlSchemaExporter documentation](https://learn.microsoft.com/en-us/dotnet/api/system.xml.serialization.xmlschemaexporter?view=net-10.0)).
- Expected exporter behavior to confirm in the spike: reference-type properties such as `string` are exported with `minOccurs="0"`, so required string fields are not enforced; properties with a `{Name}Specified` companion are exported as optional; the suffix-free timestamp wire strings are exported as plain `xs:string`; enums become `xs:enumeration` with their wire values; `decimal` becomes `xs:decimal`.
- Consequence: the procedure needs deterministic refinements — `minOccurs="1"` for required fields (driven by a declarative marker such as a Proposed `[ExportRequired]` attribute on entity properties, which carries no logic) and a pattern facet for timestamps, for example `\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01])T([01]\d|2[0-3]):[0-5]\d:[0-5]\d`. A pattern cannot reject impossible dates such as `2026-02-30T12:00:00`; a semantic test rejects them (phase plan section 7: schema validation and semantic tests together).
- Determinism: write schemas with fixed `XmlWriterSettings` (UTF-8 without BOM, `\n` line endings, fixed indentation), stable type/element order and one file per namespace; compare two clean generations byte by byte and by SHA-256.
- Secure validation: `XmlReaderSettings` with `DtdProcessing = Prohibit`, `XmlResolver = null`; `XmlSchemaSet` with `XmlResolver = null`; local schemas loaded explicitly; no network schema discovery. See [XmlSchemaSet.Add](https://learn.microsoft.com/en-us/dotnet/api/system.xml.schema.xmlschemaset.add?view=net-10.0) for its separate resolver behavior.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Generator approach | `XmlReflectionImporter` + `XmlSchemaExporter` with deterministic refinements / a pinned third-party generator after license and maintenance review | OPEN until the S0 spike result; Recommended first option if the spike confirms it, because it adds no dependency | User (after spike) | S1 |
| Generator location | (a) generator in the test project with golden-file comparison and a documented regeneration switch; (b) generator inside the library behind an interface; (c) separate tool project under `tools/` with its own tests, lock file and solution/CI-filter entries | (a): no new project, no shipped generator code; (c) only if (a) proves insufficient, because it triggers the AGENTS.md consistency updates (CONTRIBUTING.md sections 2 and 3, README project list) | User | S1 |
| Schema file layout | one file per namespace, Proposed `schemas/export/v1/weatherparser-export-v1.xsd` / one file per root | One file per namespace (one namespace `urn:weatherparser:export:v1`) | User (contract review) | S1 |
| Required-field marker | Proposed `[ExportRequired]` attribute on entity properties / a separate list in the generator | Attribute: keeps requiredness next to the mapping and is reviewed with the entity | User (contract review) | S3 |
| Schema delivery | ship the XSD in the package at a fixed versioned path / publish separately with a documented version match | OPEN – user decides; P2.10 implements the choice and narrows the package allowlist by TDD only for exact paths | User | P2.10 |
| Regeneration switch | environment variable Proposed `WEATHERPARSER_REGENERATE_SCHEMAS=1` that makes the generation test write files instead of comparing / a separate command | Environment variable used only by developers; CI always compares | Orchestrator, recorded | S1 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `schemas/export/v1/weatherparser-export-v1.xsd` | Create | Generated, committed XSD file |
| `tests/WeatherParser.Parsing.Tests/Schemas/ExportSchemaGenerator.cs` | Create | Generator and refinements |
| `tests/WeatherParser.Parsing.Tests/Schemas/SchemaGenerationTests.cs` | Create | Tests for schema generation |
| `tests/WeatherParser.Parsing.Tests/Schemas/SchemaValidationTests.cs` | Create | Tests for schema validation |
| `tests/WeatherParser.Parsing.Tests/Schemas/TimestampSemanticTests.cs` | Create | Tests for timestamp semantics |
| `tests/WeatherParser.Parsing.Tests/Schemas/Fixtures/` | Create | Negative XML documents |
| `src/WeatherParser.Parsing/Export/ExportRequiredAttribute.cs` | Create | Proposed attribute for required fields |
| `src/WeatherParser.Parsing/Export/*.cs` (entities with required fields) | Change | Add the required-field marker to required properties |
| `tools/<project>/`, `WeatherParser.slnx`, `WeatherParser.Ci.slnf`, lock files, README project list, `DEPENDENCIES.md` | Change (only if generator location (c) is chosen) | Tool project and the AGENTS.md consistency updates |
| `CONTRIBUTING.md` | Change | New subsection in section 3: schema generation and regeneration procedure |
| `docs/p2/P2_EXPORT_CONTRACT.md` | Change | Schema mapping, required fields, evolution rules applied to the schema |
| `docs/p2/P2_VERIFICATION.md` | Change | Records for this step |

## 5. Procedure
### S0 – Generator feasibility spike
**Action:** in a throwaway test, export the schema of the P2.08 envelope with `XmlReflectionImporter`/`XmlSchemaExporter` on Windows and on the Debian 13 ARM64 target (or the hosted ARM64 job if target access is not authorized; record which); record the observed behavior for each technical fact in section 2, the SDK version and the output. The throwaway spike test is removed after its findings are recorded, and its removal is part of the S1 diff.
**Roles:** qwen writes the spike test from the brief; the orchestrator runs it on each platform, records the findings and asks the user for the generator decisions.
**Check:** `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, then `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~<SpikeTestClass>" --logger "trx;LogFileName=p2-09-spike.trx"`. The spike counts as executed only if the TRX file lists the spike test by name with at least one executed (passed or failed) result. Zero executed tests means the spike did not run, which is recorded as such and never as a finding about the exporter.

### S1 – Generation and golden file
**Red:** `GeneratedSchemas_MatchCommittedFiles` fails because `schemas/export/v1/` does not exist yet.
**Green:** generator with fixed writer settings; the orchestrator runs the regeneration switch once and commits the file only when the user requests a commit.
**Refactor:** isolate writer settings in one place.
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, performs repository operations and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S2 – Reproducibility
**Characterization (not a Red):** `TwoCleanGenerations_ProduceIdenticalBytesAndSha256` generates twice into empty temporary directories and compares bytes and SHA-256. A difference is a bug: regression test first, then fix ordering.
**Roles:** qwen generates tests; the orchestrator runs them and records the result.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S3 – Deterministic refinements
**Red:** `Schema_RejectsDocumentMissingRequiredString` (for example an observation dataset without `source`) fails because the exporter emits `minOccurs="0"`; `Schema_RejectsTimestampWithZSuffix` fails because timestamps are plain `xs:string`.
**Green:** refinement step applying `minOccurs="1"` from the marker and the timestamp pattern facet; regenerate.
**Refactor:** none expected.
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, performs repository operations and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S4 – Positive validation of real exports
**Red:** `RealExports_OfAllFiveRoots_ValidateAgainstSchema` — expected to fail at first for any mapping/namespace mismatch found; if it passes immediately, record it as characterization.
**Green:** correct the generator or mapping, never hand-edit the XSD.
**Refactor:** none expected.
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, performs repository operations and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S5 – Negative fixtures and semantic checks
**Red:** `Schema_RejectsWrongNamespace`, `Schema_RejectsUnknownElement`, `Schema_RejectsMalformedTimestamps` (`2026-07-15 12:00:00`, `2026-07-15T12:00:00+02:00`), and `TimestampSemantics_RejectImpossibleCalendarDate` (`2026-02-30T12:00:00`, accepted by the pattern, rejected by the semantic check using `DateTime.ParseExact` with `CultureInfo.InvariantCulture`). Expected failure: the semantic check does not exist yet.
**Green:** semantic checker in the test project.
**Refactor:** none expected.
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, performs repository operations and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S6 – Secure offline validation
**Red:** `Validation_DocumentWithDtd_IsRejected` and `Validation_SchemaWithRemoteImport_IsNotFetched` (a schema with `xs:import schemaLocation="http://example.invalid/x.xsd"` fails locally without any network access). Expected failure: default reader settings process the DTD.
**Green:** prohibited DTD and null resolvers in one shared validation-settings helper.
**Refactor:** none expected.
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, performs repository operations and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

### S7 – Contract mapping guard
**Red:** `Schema_ContainsNoXsAnyAndMatchesContractRecord` (no `xs:any`/`xs:anyAttribute`; `schema_version` literal, namespace, empty collections and error elements as recorded). Expected failure: confirm when run; if it passes immediately, record as characterization.
**Green:** none expected.
**Refactor:** none expected.
**Roles:** qwen generates tests, minimal implementation and refactoring; the orchestrator writes the briefs, runs each Red and confirms the failure reason, runs build and tests, reviews the diff and returns defects to qwen, performs repository operations and records authorship.
**Check:** `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf -c Release --no-restore`, `dotnet test tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj -c Release --no-build`

## 6. Tests and Test IDs
| Step-local case | T-P2 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1, S2 | T-P2-20 | REQ-079 | Two clean generations from the same mappings and toolchain produce identical named files and checksums |
| S3, S4 | T-P2-21 | REQ-067, REQ-076, REQ-079 | Real exports of every root validate; required fields enforced |
| S5 | T-P2-21 | REQ-051, REQ-079 | Wrong namespace, unknown elements and malformed timestamps fail; impossible dates fail the semantic check |
| S6 | T-P2-22 | REQ-079, REQ-083 | Offline validation with DTDs and external resolution prohibited; remote imports never fetched |
| S7 | T-P2-23 | REQ-074–079 | Version, null, empty and error mappings match the record; no `xs:any` escape |

## 7. Evidence to Record
- All entries below go to `docs/p2/P2_VERIFICATION.md`, except the last two items, whose destinations are named.
- Spike findings per platform with SDK version
- Generator source and version
- Exact generation command and switch
- Writer settings
- SHA-256 of each schema file from two clean generations
- Red names with observed failure messages
- Characterization results
- Negative-fixture outcomes
- Authorship
- Mapping and requiredness decisions in `docs/p2/P2_EXPORT_CONTRACT.md`
- Regeneration procedure in `CONTRIBUTING.md`
- For S0: the build command, the test command, the TRX file name and the executed spike test name and count per platform.

## 8. Risks and Limitations
- (a) `XmlSchemaExporter` is documented as infrastructure — the spike decides, and a reviewed third-party alternative needs license and maintenance checks first
- (b) schema output could differ between Windows and Linux (line endings, ordering) — fixed writer settings and a cross-platform comparison in P2.11
- (c) a hand-edited XSD would drift from the mappings — CI always compares against fresh generation
- (d) the delivery decision is OPEN and blocks P2.10
- (e) test-project generator code is outside the product coverage gate (`*.Tests.dll` is excluded), so the generator needs its own meaningful tests rather than coverage evidence
- (f) A stale test assembly could hide the spike — S0 builds first and accepts the spike only when the TRX shows it executed.

## 9. Acceptance Criteria
- [ ] Spike findings and generator decisions recorded before S1
- [ ] Schema file generated, never hand-edited
- [ ] Two clean generations identical
- [ ] Real exports of all five roots validate
- [ ] Every negative fixture rejected
- [ ] DTD and remote imports blocked offline
- [ ] No `xs:any`
- [ ] Regeneration procedure in `CONTRIBUTING.md`
- [ ] MOSMIX input schema untouched
- [ ] All tests pass
