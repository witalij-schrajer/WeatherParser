# P1.01 – Entry audit

[Deutsch](P1_01_BASELINE.md) | [English](P1_01_BASELINE.en.md)

**Status: completed on 2026-10-15.** Scope: inspect working state and P0 completion, confirm P1 boundaries. Existing project scaffolding was checked and preserved; this step did not recreate it.

## Starting state

| Item | Observed state |
|---|---|
| Git branch | `master` |
| HEAD | No commit yet; all 26 captured project files are untracked |
| Git remotes | None |
| Solution | `WeatherParser.slnx`, two projects |
| Library | `src/WeatherParser.Parsing/WeatherParser.Parsing.csproj` |
| Unit test project | `tests/WeatherParser.Parsing.Tests/WeatherParser.Parsing.Tests.csproj`, source reference to the library |
| Package consumer | Reserved area with bilingual documentation; no project yet |
| SDK / framework | Pinned SDK 10.0.303; `net10.0` |
| Restore | Lock files exist for both projects |
| Domain implementation | No versionable C# source files or test methods; generated build files are outside this finding |
| Requirements | 84 unique IDs, identical order in German and English |

The [starting inventory](P1_01_BASELINE.json) records relative paths and SHA-256 hashes of the 26 project files before this audit's documentation edits. It excludes Git metadata and ignored build/test output. This is a working-state snapshot, not a Git commit or immutable code-baseline proof. Later authorized edits are expected to change hashes.

## P0 completion and P1 boundaries

P0 remains complete. Documentation agrees on NuGet as the primary product, .NET ≥10, Debian ≥13/ARM64, constructor endpoints, generic MQTT serialization, DI, dedicated export entities and separate MOSMIX_S/L parsers. Timestamps remain UTC without a suffix, preserve per-value measurement times and round midpoint ties away from zero. Program output/comments are English; documentation is German/English.

Coverage decisions are recorded: at least 80% per required metric; no target comparison when a measurable baseline does not yet exist initially; documentation-only PRs may retain equal coverage; exact 100% may remain at an exact 100% baseline. Verify Microsoft.CodeCoverage reports and actually available metrics in P1.07/P1.08. Never relabel block coverage as branch coverage.

Sequence: **P0 → P1 → P2 → P4 → P5 → P6 → P7 → P8 → P3 → P9**. Create the CLI example only after complete library acceptance. P1 establishes build/test/package/CI foundations; parsers, entities and real serializers follow in their assigned phases. P1.01 requires no target-system access or production service.

## Checks performed

- Read and reconciled repository rules, requirements, overall plan and P1 plan.
- Captured Git branch, commit/remote state and nonignored files.
- Verified matching, unique requirement IDs.
- Checked solution project paths, project XML, unit-test source reference and both `net10.0` lock files.
- Captured original hashes before changes; preserved source/project/build/package files.
- Updated P1 status and next work item in both languages.

Result: no unresolved foundational P0 question blocks entry verification. No commit or push was performed. Restore and Release build succeeded when the structure was created previously; they were not repeated for this entry audit. No new tests were executed and no coverage was measured.

## Handoff

**P1.01 complete; P1 as a whole remains open.** Next is P1.02: complete tooling/dependency/license/compatibility review based on the existing version selection. Then finish P1.03 structure/reproducibility acceptance. Isolated package consumption remains P1.04, TDD behavior tests P1.05, coverage P1.07/P1.08 and Debian/ARM64 evidence P1.10/P9.

Remaining evidence belongs to those phases and does not reopen P0. The current instruction executes P1.01 only; this record does not claim subsequent work packages are implemented or accepted.
