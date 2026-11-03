# P6.08 – Export Equivalence and Schema Validation: Implementation Plan

**Status: executed on 2026-10-26 ([record](../../p6/P6_VERIFICATION.md) section 8): 11 characterization tests green, no product defect, export contract unchanged; coverage gate passed (8475/8478).** Refines the P6.08 row of section 5 and the "Export" row of section 4 of [PHASE_P6_PLAN.md](../PHASE_P6_PLAN.md). Reconciled on 2026-10-25 with the OsthessenWetter contract approved by the user on 2026-10-25 ([contract](../../p6/P6_OSTHESSEN_CONTRACT.md), option A in every row): OW-06 and OW-16 as planned.

## 1. Goal and Scope
Goal: prove that the datasets produced by the OsthessenWetter parser serialize through both real serializers equivalently and validate against the v1 output schema, with the P6-specific wire features: `estimated_date` markers, `source_timezone`, the wind period, umlauts and the rounding of the shown values.
In scope: contract tests from synthetic fixtures through the composed parser (HTTP handler mocked) into `IJsonExportSerializer` and `IXmlExportSerializer`; the shared `ExportEquivalence` helper; schema validation; the builder/root-validation duplication guard (inherited limitation, P6.01 mapping).
Out of scope with owner: MQTT payloads → P8; package-level serialization → P6.09.

## 2. Preconditions and Inputs
- P6.07 complete.
- Existing: `ExportEquivalence` helper, `ObservationExportEquivalenceTests`, the v1 schema `schemas/export/v1/weatherparser-export-v1.xsd`, the P5.08 tests as the pattern.

## 3. Decisions Required
| Decision | Options | Recommended | Decided by | Blocks |
|---|---|---|---|---|
| Test home (Proposed) | `tests/WeatherParser.Parsing.Tests/Observations/Osthessen/OsthessenWetterExportContractTests.cs` / inside the existing export tests | Own file beside the DWD contract tests | Orchestrator, recorded | S1 |
| Export contract | Unchanged (OW-16) / additive member | Unchanged; any need for a change stops the step and goes to the user as an additive decision (C-03, C-105) | User (OW-16) | S1 |

## 4. Artifacts
| Path | Action | Content |
|---|---|---|
| `tests/WeatherParser.Parsing.Tests/Observations/Osthessen/OsthessenWetterExportContractTests.cs` | Create | Contract tests |
| `tests/WeatherParser.Parsing.Tests/Fixtures/Osthessen/*.html` | Change if needed | Fixtures with rounding ties and umlauts |
| `docs/p6/P6_VERIFICATION.md` | Change | P6.08 section |

## 5. Procedure
Roles and checks as in P6.03 section 5. Production code is expected to stay unchanged; if a test fails because of a defect, the defect is fixed test-first in the owning step's code through qwen and the step's verification record names it.

### S1 Equivalence, schema and wire form
**Characterization** (as in P5.08: the tests are expected to pass on first run; each first run is recorded honestly as characterization, not as Red; a test that fails because of a product defect keeps its correct expectation as the reproducing Red test, and the defect is fixed test-first through qwen)
- `Export_Dataset_JsonXmlEquivalent`
- `Export_Xml_ValidAgainstV1Schema`
- `Export_Markers_EstimatedDateOnReadingsAndDataset_SourceTimezoneOnReadingsOnly`
- `Export_WindPeriod_StartAndEndUtc`
- `Export_UtcForm_NoOffsetNoZ`
- `Export_OmittedMembers_Absent` (no pressure, direction, irradiance, rain rate, illuminance; no `quality`)
- `Export_Rounding_TiesAwayFromZero` (20.25 → 20.3, −20.25 → −20.3, wind 12.5 → 13)
- `Export_Humidity_BoundariesKept` (0 and 100)
- `Export_Umlauts_JsonAndXmlEscapedCorrectly` (`Müs-Hohhütte`, `Roßbach` in `location.name`)
- `Export_SeveralDatasets_EachSerializedSeparately` (one document per dataset; no combined document)
- `Export_SameGraph_NoSecondRetrieval` (request count stays one)

## 6. Tests and Test IDs
| Step-local case | T-P6 ID | Requirement IDs | Behavior and boundary |
|---|---|---|---|
| S1 | T-P6-12 | REQ-051, REQ-067–069, REQ-079 | Equivalence, schema, UTC, markers, period, escaping |
| S1 | T-P6-04 (wire) | REQ-057 | Rounding on the wire |
| S1 | Inherited guard | C-27, C-49 | Builder and root validation agree (equivalence of the built graph through both serializers) |
| S1 | T-P6-15 | REQ-085 | Characterization runs recorded; Red only for a reproduced defect |

## 7. Evidence to Record
- First-run outcome per test (characterization), any reproduced defect with its Red, counts, coverage, authorship, Codex rounds

## 8. Risks and Limitations
- (a) A test reveals a needed export change — stops the step; the user decides an additive change
- (b) XML cancellation remains coarse (C-10, inherited)

## 9. Acceptance Criteria
- [ ] Every listed test green; first runs recorded as characterization; real serializers only
- [ ] Export contract unchanged or an approved additive decision recorded
- [ ] Coverage gate passed; Codex 0 findings or reported
