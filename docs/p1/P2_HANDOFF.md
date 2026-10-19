# P2 Handoff – Export Entities, Serialization and Output Schemas
**Prepared 2026-10-19 by P1.12. P2 has not started; implementation needs a separate instruction and the user's confirmation of the P1 acceptance.**

## 1. Accepted foundation

| Item | Value |
|---|---|
| Foundation SHA | `6ac88e783ce10fa6d676860ba831ee371abd9cdf` |
| Package ID | `WeatherParser.Parsing` |
| Package version | 0.1.0-initial |
| Baseline | native block counters 54/56 of `WeatherParser.Parsing.dll` |
| Bootstrap seed | `0a2dabe` |
| CI script | `scripts/ci-local.ps1` at the foundation SHA |
| Acceptance record | [P1.12 acceptance record](P1_12_ACCEPTANCE.md) (audit decision ACCEPTED, formal user acceptance pending) |

## 2. Entry points

- Build: [section 2](../../CONTRIBUTING.md#2-restore-and-build)
- Tests: [section 3](../../CONTRIBUTING.md#3-tests)
- Package consumer: [section 4](../../CONTRIBUTING.md#4-pack-and-package-consumer)
- Coverage and runner: [section 5](../../CONTRIBUTING.md#5-coverage)
- Local CI: [section 6](../../CONTRIBUTING.md#6-local-ci)
- Debian ARM64: [section 7](../../CONTRIBUTING.md#7-debianarm64)

## 3. Coverage and baseline policy

- Native block counters only
- Minimum 80%
- Below a 100% baseline, every code change must strictly improve through meaningful tests
- Markdown-only changes may stay equal
- Exact 100% accepts exactly 100%
- Never bootstrap again
- Compare against a deliberate committed target remeasured by the two-pass run
- Link: [PHASE_P1_PLAN.md section 5](../../PHASE_P1_PLAN.md)

## 4. P2 scope

P2 owns public export entities, UTC/unit/rounding logic, real JSON/XML serializer contracts and reproducible output XSD generation. Reintroduce the previously removed entity scaffolding one behavior at a time through tests; do not restore all old types as presumed accepted contracts.

P2 does not introduce parser retrieval, broker transport or CLI. Phase order is P0 → P1 → P2 → P4 → P5 → P6 → P7 → P8 → P3 → P9.

## 5. Initial test-first sequence

1. Select the first required export contract from [REQUIREMENTS.md](../../REQUIREMENTS.md), define exact fields/nullability/units/time semantics and its requirement traceability before naming final types.
2. Write a meaningful failing serializer-contract test for that minimal typed entity graph. Specify present/missing values and true zeros, identifiers including leading zeros, UTC formatting without suffix, and escaping/special characters as applicable to that graph.
3. Implement minimally using `[Serializable]` on every custom export entity, real `System.Text.Json.JsonSerializer` and `System.Xml.Serialization.XmlSerializer`, and equivalent normalized meaning. Executable serializer services use interfaces and constructor injection; pure data objects need no artificial interfaces.
4. Extend tests incrementally for per-value measurement times, estimated-date markers, observation/forecast separation, source/quality/period metadata, pressure conversion/reference and midpoint rounding away from zero. Preserve issue/validity times for forecasts.
5. Define a reproducible output-XSD generation/versioning procedure, then validate real XML exports against generated schemas. Output schemas remain separate from the DWD MOSMIX source XSD.

## 6. Working rules

- TDD Red→Green→Refactor with a recorded failing test first
- MSTest, Moq, FluentAssertions
- TestInitialize/TestCleanup with VerifyAll
- Every custom export entity `[Serializable]`, including nested types
- Real `System.Text.Json.JsonSerializer` and `System.Xml.Serialization.XmlSerializer` on the same entities
- UTC `YYYY-MM-DDTHH:mm:ss` without suffix
- Omit missing values, 0 is real
- Round only for output, midpoint away from zero (20.25 → 20.3, −20.25 → −20.3)
- Pressure hPa and mmHg from the same unrounded original
- Doing classes have interfaces and constructor injection
- Offline tests
- The local qwen3-coder:30b model generates code where applicable, the orchestrating LLM verifies (AGENTS.md "AI-assisted development")

## 7. Inherited limitations and deferred checks

### Inherited limitations

- No hosted CI
- Release readiness, hardware acceptance and REQ-084 in P9
- NU1004 for RID-specific restores (projects stay RID-neutral)
- The package embeds the commit ID, so the committed consumer lock is stale by design; the CI script and [CONTRIBUTING.md section 4](../../CONTRIBUTING.md#4-pack-and-package-consumer) handle it
- Package ID, feed and publication are P9 decisions; `WeatherParser.Parsing` is a local test package
- REQ-004/AGENTS.md still name SDK 10.0.303 as the historical starting point

### Deferred checks

- Serializers and output XSDs (P2 itself)
- Timezones/DST and OsthessenWetter date inference (P4–P7 sources)
- DI under load
- CLI (P3)
- MQTT transport (P3/P8)
- Network sources (P4–P7)
- Release and hardware checks (P9)
