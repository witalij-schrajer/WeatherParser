# P1.11 – Developer Instructions and Evidence: Result Record

**Status (2026-10-19): implemented and verified on Windows x64; parts (a) and (b) done together; acceptance pending.** Implements the [P1.11 plan](P1_11_DEVELOPER_INSTRUCTIONS_PLAN.md).

## Decision (user, 2026-10-19)

**Documentation structure: option (D)**, a `CONTRIBUTING.md` at the repository root instead of `docs/DEVELOPER_GUIDE.md`, with the same content and single-source mapping as option B. Wherever the plan says "guide", this record means `CONTRIBUTING.md`.

Because P1.08, P1.09 and P1.10 had all reported and been accepted, part (a) (interim delivery) and part (b) (switching items to Verified after each package report) were done in one pass.

## Delivered

The local qwen3-coder:30b model wrote every documentation change from Claude's briefs. Claude reviewed each output, returned defects to qwen, ran every check and the walkthrough, and wrote this record, because it consists of recorded evidence. qwen needed several correction rounds. Its drafts contained copied brief text, doubled status lines, a typo in a project path and a non-existent cmdlet, and twice it replaced a paragraph instead of adding one. The driver was improved to tolerate trailing whitespace in search text, because qwen drops the Markdown line-break spaces.

| Path | Change |
|---|---|
| `CONTRIBUTING.md` | New: the nine sections of the plan's outline, each procedure with its platform and exactly one status, and the "Version literals" table |
| `README.md` | Reduced to the mapping: status summary, product description, the "Development" section (project list, 3-command quickstart, links); every procedure moved to CONTRIBUTING.md |
| `tests/WeatherParser.Package.Tests/README.md` | Description and one pointer to CONTRIBUTING.md section 4; the duplicated procedure and the obsolete NU1403 paragraph removed |
| `AGENTS.md` | Consistency table under "Documentation" (plan step 5), adapted to CONTRIBUTING.md sections and the P1.10 files |
| `PHASE_P1_PLAN.md`, `DEVELOPMENT_PLAN.md` | P1.11 row; the 2026-10-15 scaffolding paragraph moved from README |

Commits: `0394dda` (guide, README reduction, pointer, consistency table), `ba93447` (single-source fixes), `83579f4` (coverage outputs under `TestResults/`), `1276cd7` (this record, status documents), `8a96f58` (review fixes, see "Review fixes"). All are Markdown-only.

## README paragraphs removed and their destination

| Removed from README.md | Destination |
|---|---|
| "Project structure and local build" (four-project tree, `global.json`/props/NuGet.Config paragraph) | Updated seven-project list in README "Development"; the build facts are in CONTRIBUTING.md sections 1–2 |
| "Coverage gate" block and runner paragraph | CONTRIBUTING.md section 5 |
| "Local CI sequence" and the run-artifacts paragraph | CONTRIBUTING.md section 6 |
| Debian 13 ARM64 target paragraph | CONTRIBUTING.md section 7 |
| Restore/build sequence and the lock-file paragraph | CONTRIBUTING.md section 2 (corrected, finding 1) |
| "Current reproducibility limitation" (NU1403) | Obsolete; superseded by CONTRIBUTING.md section 4 and P1.09 finding 5 (the committed consumer lock is stale by design) |
| Library/consumer status paragraph | Status summary in README "Project status"; procedures in CONTRIBUTING.md section 4 |
| "Historical initial-scaffolding verification on 2026-10-15" | `DEVELOPMENT_PLAN.md` (no record contained it) |
| P1.01 entry audit paragraph | Already in `DEVELOPMENT_PLAN.md`; README links the records |
| P1.02 review paragraph | Already in `DEVELOPMENT_PLAN.md` |
| Test counts in "Project status" | Replaced by a link to the P1 records |
| "Additional decisions dated 2026-10-14" and "P0 clarifications dated 2026-10-14" (review fix, `8a96f58`) | Verbatim copies of the same AGENTS.md sections, which remain the maintained text; README keeps a short "Decisions dated 2026-10-14" pointer to AGENTS.md and REQUIREMENTS.md |
| "Visual Studio 2026 and SDK 10.0.303 are the verified local starting point" in the platform paragraph (review fix) | Replaced by links to DEPENDENCIES.md (pinned SDK, toolchain), CONTRIBUTING.md (setup) and docs/COMPATIBILITY.md |

The requirements-style sections of README.md (planned CLI, export, data contract, MOSMIX, architecture, configuration, sources, dated decisions, phase sequence) are product description, not procedures or status history; they stay, because README.md is also the NuGet package description.

## Windows walkthrough (plan section 6)

Fresh `git clone --no-local` of `ba93447`, with a second clone of the target `ea8040e` for the baseline. Windows x64, .NET SDK 10.0.402, PowerShell 7.6.7. Isolated `NUGET_PACKAGES` and `DOTNET_CLI_HOME`, so the cache-entry removal of section 4 touched only the walkthrough folder. Commands were copied from CONTRIBUTING.md.

| Section | Commands | Result |
|---|---|---|
| 1 | `dotnet --version`, `pwsh --version`, `dotnet tool restore`, telemetry variables | All exit 0 |
| 2 | `New-Item … artifacts/local-nuget`, `dotnet restore WeatherParser.Ci.slnf --locked-mode`, `dotnet build WeatherParser.Ci.slnf --configuration Release --no-restore` | Exit 0; 0 warnings, 0 errors |
| 3 | `dotnet test` of the three source-reference projects | All passed (counts in the CI records) |
| 4 | Pack; consumer test before the refresh | Pack exit 0; consumer exit 1, as documented (stale lock by design) |
| 4 | Stale-cache refresh: cache entry removal, `--force-evaluate`, lock diff, consumer test, `git restore` | Lock diff exactly one line (`contentHash`); 2 consumer tests passed; tree clean afterwards |
| 5 | Collection with `coverage.runsettings`, conversion, changed-file list, `write-baseline` from the target report, `evaluate` | All exit 0; gate passed |
| 5 (`83579f4`) | Same commands with the outputs under `TestResults/product/` | Exit 0; tree clean |

Sections 6 and 7 are Verified by the P1.09 and P1.10 records. Section 6 was also run on the README revision (below). Section 7 was not repeated on the target, because the P1.10 authorization did not extend to P1.11 (plan check "Debian/ARM64 evidence"); every Debian/ARM64 item matches a recorded, passing command in the [P1.10 record](P1_10_DEBIAN_ARM64.md).

## Package readme check (plan step 3)

The README edits were committed before the local package check, which then ran on the committed revision `ba93447` in the walkthrough clone instead of on a pre-commit working tree. The README is unchanged since `ba93447`, so the result is the same.

- **Entries:** only `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE`, the nuspec, `[Content_Types].xml`, `_rels/.rels` and `nuget.psmdcp`. The nuspec has `<readme>README.md</readme>`.
- **README and LICENSE:** byte-identical to the checked-out files, and equal to the committed blobs apart from the CRLF line endings that `core.autocrlf=true` adds at checkout.
- **Local package:** SHA-512 `6RhV0Q23+eYgU2nHOQdqo2ogv3C6KhsA6Lkynr35Cssnb/zC+xD4bX5fqX+0UUNMfy7mD9NvVRW6Vz6tgqEkwQ==`.
- **Consumer:** the lock diff changed only the `WeatherParser.Parsing` `contentHash`. The locked consumer restore succeeded, the restored `.nupkg.sha512` equals the packed hash, and both consumer tests passed. The refresh was discarded with `git restore`.
- **Standalone package description:** reviewed. README.md states the library purpose, the implemented and planned scope with explicit "not implemented yet" markers, the usage entry point (DI registration, endpoints) and its limitations. Developer procedures are only linked. Links point to repository files; no published URL is invented.
- **CI run on the committed revision (step 3, item 6):** `pwsh ./scripts/ci-local.ps1 -Commit 83579f4 -TargetRef ea8040e -RunSettings coverage.runsettings`, exit 0, run directory `artifacts/ci/83579f4-20261019T202924Z` (local, gitignored).
  - Changed files: six Markdown files.
  - Every stage passed; the consumer lock was rewritten in the clone (one line).
  - Package SHA-512: `ghqGsp83tZiaEo9yCeDtbVSUyLN8r9G7MGHVnNM5QInkCOHntCYHczOg8BfX2A3SqmM6ImZgInKG1Qclid0K7g==`.
- **Gate result of the README commit:** classified as **documentation-only**. "Block coverage 54/56 (96.43%) is at or above baseline 54/56 (96.43%)", all five rules passed. SHA-256 of `gate-result.json`: `36aa8f6f6aa5bd9f82b58caad8e2e31368b80b270f25f917b07e11fd944e2916`.

## Documentation checks

| Check | Result |
|---|---|
| Links | Every relative link and anchor in CONTRIBUTING.md, README.md, AGENTS.md, PHASE_P1_PLAN.md, DEVELOPMENT_PLAN.md, the package README, docs/COMPATIBILITY.md, DEPENDENCIES.md and the P1.10 record resolves. Anchors were checked with GitHub's heading rules. The only missing target was this record, created now |
| Language/privacy | No non-English prose added; no absolute user paths, IP addresses, user names or tokens. Hits for "R76S" (the public hardware model named in REQUIREMENTS.md) and "password" (AGENTS.md rule text) are not findings. Historical `docs/p1` content is unchanged |
| Status | Every CONTRIBUTING.md procedure item has exactly one status line (20); the reference subsections of section 6 (parameters, exit codes, run directory) belong to its first item; section 9 has no status |
| Single source | CONTRIBUTING.md commands contain only the literals `18.11.0` and `0.1.0-initial`, both listed in "Version literals" with their source. README prose version statements were replaced by links; after the review fix README.md contains no SDK version and no setup instruction (finding 4, resolved). The binding texts in REQUIREMENTS.md (REQ-004) and AGENTS.md still name SDK 10.0.303 as the historical starting point of 2026-10-14; they are requirements and rules, not README or guide content, so they are outside this check and unchanged |
| Requirement IDs | `b6100e2` and `83579f4`: 84 unique IDs, REQ-001 to REQ-112, identical order; SHA-256 of the ID sequence `54710941fc36deb375dcce1c15b052ca94ca05e4c7093a5b09dc4bdb96639af` for both |
| Comment language | 83 tracked files (`src`, `tests`, `tools` C#; `scripts/*`; `*.props`, `*.csproj`, `*.runsettings`, `*.slnf`, `.config/*.json`, `global.json`, `NuGet.Config`), 715 comment lines: no non-ASCII characters and no German words; no finding |
| Preservation | Committed content, reference `b6100e2`. Byte-identical: `LICENSE` (`1eda1c9d…`), `P1_01_BASELINE.json` (`a8c92586…`), `P1_02_DEPENDENCY_INVENTORY.json` (`cdb0a62e…`). Append-only: `P1_01_BASELINE.md` to `P1_07_COVERAGE_CONTRACT.md` unchanged; `P1_08_COVERAGE_EVALUATION.md` has the reference as an exact prefix plus 3,442 bytes of dated additions (acceptance, correction note) |

## Status table of CONTRIBUTING.md

| Section | Item | Status |
|---|---|---|
| 1 | SDK selection, `dotnet tool restore` | Verified (Windows) – this walkthrough |
| 1 | Git, PowerShell 7, telemetry variables | Verified (Windows, Debian/ARM64) – P1.09, P1.10 |
| 2 | Restore and Release build of `WeatherParser.Ci.slnf` | Verified (Windows) – this walkthrough |
| 3 | Source-reference test projects | Verified (Windows) – this walkthrough |
| 4 | Pack and consumer; stale cache/consumer lock; README changes | Verified (Windows) – this walkthrough and the CI run above |
| 4 | Automated prevention in the CI script | Verified (Windows, Debian/ARM64) – P1.09, P1.10 |
| 5 | Collection, conversion, runner | Verified (Windows) – this walkthrough |
| 5 | Two-pass baseline | Verified (Windows, Debian/ARM64) – P1.09, P1.10 |
| 6 | Local CI, parameters, exit codes, fault injection, lock drift | Verified (Windows) – P1.09; P1.10 parameters Verified (Windows, Debian/ARM64) |
| 7 | Debian/ARM64 target procedure; `-CollectOnly`/`-ResumeFrom` fallback | Verified (Debian/ARM64; fallback also Windows) – P1.10 |
| 8 | NETSDK1141 | Historical remedy (P1.03), not reproducible on demand |
| 8 | Lock-file drift; exit 80 tool resolver cache | Verified – P1.09; P1.10 |
| 9 | Limits | Statements, no status |

No item is Planned or OPEN.

## Findings

1. **The documented whole-solution build could not work.** `dotnet restore`/`build WeatherParser.slnx` always fails with NU1403 on the package consumer, because its committed lock is stale by design (the package embeds the commit ID). CONTRIBUTING.md and the README quickstart therefore use `WeatherParser.Ci.slnf`, as the CI script does, and build the consumer separately (section 4).
2. **Fresh clones lack `artifacts/local-nuget`.** Every restore fails with NU1301 until the folder exists; the first command of section 2 and of the quickstart creates it.
3. **Coverage outputs in the repository root.** The first draft wrote `changed-files.txt` and `baseline.json` to the root, leaving untracked files; they now go to the gitignored `TestResults/product/`.
4. **Dated decision copies in README.md** contained version literals and the outdated SDK 10.0.303 setup instruction. Resolved in `8a96f58` (review fix 3).
5. **CRLF in the packed README/LICENSE** on Windows checkouts with `core.autocrlf=true`; content is otherwise identical to the committed blobs.
6. **Stale coverage report selection** (review fix 1).
7. **Incomplete package procedure** (review fix 2).

## Review fixes (2026-10-19)

An external review of `1276cd7` confirmed the Release build, 162 tests, package creation and consumption, package hash and content, 137 relative links, the requirement-ID order, the preservation checks and the recorded gate hash, and found three defects. qwen fixed them from Claude's briefs in `8a96f58`.

1. **Stale coverage selection.** Section 5 picked the first `.coverage` file under `TestResults/product`, so a repeated run could evaluate an older report. Every run now writes to a new timestamped directory `TestResults/product-<UTC>` and requires exactly one raw report in it.
2. **Incomplete package procedure.** Section 4 lacked the plan's explicit checks. It now lists the package entries, compares the packed README.md and LICENSE with the working-tree files, records the packed SHA-512, runs the locked consumer restore after the refresh, and compares the restored `.nupkg.sha512` with the packed hash, all before the consumer tests and the `git restore`.
3. **Contradictory record and outdated README.** The acceptance list was marked complete although finding 4 was open, and README.md still contained outdated SDK setup text. Both README copies of the AGENTS.md decision sections were replaced by a pointer, and the platform paragraph links DEPENDENCIES.md, CONTRIBUTING.md and docs/COMPATIBILITY.md. Finding 4 is now resolved, and the acceptance list below reflects it.

**Re-verification of `8a96f58`.** The powershell blocks of sections 2, 4 and 5 were extracted verbatim from CONTRIBUTING.md and executed in a fresh clone, with the isolated package folder and CLI home and with only the placeholders filled (target `ea8040e` and its measured report):
- Section 2: 0 warnings, 0 errors.
- Section 4 package check: only the allowed entries; no README/LICENSE mismatch; packed and restored SHA-512 `wP4N8jpqba9omUJxj6WS/s87p1kXOQdteXS3a8o/4UWkW1fbQWk9wPVyLMyUg3kLG5B830LSEzpPwUeDm+QeSA==` equal; 2 consumer tests passed; tree clean.
- Section 5, executed twice: two separate directories (`product-20261019T204455Z`, `product-20261019T204500Z`), each with exactly one raw report; both gates passed; tree clean.

**CI run on the README revision `8a96f58`:** `pwsh ./scripts/ci-local.ps1 -Commit 8a96f58 -TargetRef ea8040e -RunSettings coverage.runsettings`, exit 0, `artifacts/ci/8a96f58-20261019T204516Z`.
- Seven Markdown files changed; all tests passed (15, 91, 54 and 2 consumer).
- Package SHA-512: `wJs4pui3ZKoN+5m4QRHSJYxjlQP32/MqZ7/uavnyuVbBh0Ocd4zAB1szloV+Y7E7erbfl6rDfPbdq1ZIyiMB1A==`.
- Gate documentation-only: "Block coverage 54/56 (96.43%) is at or above baseline 54/56 (96.43%)". The `gate-result.json` SHA-256 `36aa8f6f…` equals that of the `83579f4` run, because the gate content is identical.

## Acceptance criteria (plan section 9)

- [x] The documentation-structure decision is recorded (D, `CONTRIBUTING.md`).
- [x] CONTRIBUTING.md has all nine sections; every item names its platform and carries a status whose basis matches the plan; historical records are linked with their original scope.
- [x] The "Version literals" table exists, and each literal equals its source.
- [x] README.md follows the mapping, and every removed paragraph has a recorded destination.
- [x] The consistency table is in AGENTS.md under "Documentation".
- [x] All documentation checks of plan section 6 pass and are recorded, including the package-readme, requirement-ID, comment-language and preservation checks; finding 4 of the single-source check is resolved in `8a96f58`.
- [x] The README commit contains only Markdown, has a recorded P1.09 run and gate result (documentation-only; latest `8a96f58`), and the consumer lock is not committed with it.
- [x] Interim delivery (part a): the checks pass for the current revision.
- [x] Completion (part b): done for P1.08 closure, P1.09 and P1.10; no item is Planned; no item is OPEN.

## Second review fix (2026-10-19)

A second review of `d29ffb6` confirmed the coverage-selection fix, the removed SDK setup text, all 74 relative links in the changed documents and the recorded CI hashes with 162 passing tests. It found one remaining defect: the section 4 content check compared decoded text with PowerShell's case-insensitive `-ne`, so a package copy whose LICENSE read `mit License` was accepted.

qwen replaced the comparison with SHA-256 hashes of the raw bytes and the case-sensitive `-cne`. The packed entry is read through a byte stream, the working-tree file with `ReadAllBytes`.

Verification: the content-check lines were extracted verbatim from the corrected CONTRIBUTING.md and run in the fresh clone of `8a96f58` against three packages:

| Package | Result |
|---|---|
| Unchanged package | Accepted |
| Copy with LICENSE `MIT License` changed to `mit License` | Rejected: "LICENSE in the package is not byte-identical to the working tree" |
| Copy with README.md line endings changed from CRLF to LF only | Rejected: "README.md in the package is not byte-identical to the working tree" |

The complete section 4 block was then executed again: the packed and restored hashes are equal, the 2 consumer tests passed, and the tree was clean. Only CONTRIBUTING.md changed. README.md is unchanged since `8a96f58`, so the package-readme check and the documentation-only gate result recorded above still apply.
