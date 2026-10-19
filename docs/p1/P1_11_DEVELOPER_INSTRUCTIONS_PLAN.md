# P1.11 – Complete English Developer Instructions and Evidence: Implementation Plan

**Status: plan only (2026-10-19); nothing is implemented or verified yet.** Drafted with the local qwen3-coder:30b model over three review rounds and finalized by review against [AGENTS.md](../../AGENTS.md) (language and documentation rules), the [P1 detailed plan](../../PHASE_P1_PLAN.md) (P1.11: restore/build/pack/test/coverage procedures and limits; ongoing) and the [P1.08 closure](P1_08B_COVERAGE_GATE_COMPLETION_PLAN.md), [P1.09](P1_09_LOCAL_CI_PLAN.md) and [P1.10](P1_10_DEBIAN_ARM64_PLAN.md) plans, whose procedures it documents.

## 1. Goal and Scope

Give developers one English guide with every restore, build, pack, test, coverage, local-CI and Debian/ARM64 procedure. Each procedure is tagged with what proves it, and every fact is kept in exactly one document. The guide documents only what a package actually verifies; everything else is marked Planned or OPEN.

In scope:
- A new developer guide, with each item's status.
- Moving procedures out of `README.md`.
- The single-source mapping between documents.
- A consistency rule set for future changes.
- Documentation checks: walkthrough, links, language/privacy, status, requirement IDs and comment language.

Out of scope:
- Implementing P1.08 closure, P1.09 or P1.10.
- Changing the historical content of `docs/p1` records. Their original statements, inventories and checksums stay unchanged (AGENTS.md); later evidence is only appended as dated additions, as the P1.08 closure did in `P1_08_COVERAGE_EVALUATION.md` section 8.
- Documentation tooling such as linters or CI link checkers; no package plans it.
- P2–P9 content.

## 2. Preconditions and Inputs

- Commit `b6100e2`: P1.08 closure implemented, with acceptance pending review. The runner, the pinned `dotnet-coverage` 18.11.0 in `.config/dotnet-tools.json`, the README "Coverage gate" block and the result record in `P1_08_COVERAGE_EVALUATION.md` section 8 exist.
- Historical records exist for these procedures (each keeps its original scope; the current commands are verified by the part (a) walkthrough):
  - P1.03: locked restore, Release build, NETSDK1141 remedy.
  - P1.04: pack, local feed, isolated consumer.
  - P1.05/P1.06: offline unit tests.
  - P1.07: coverage collection.
  - P1.08: evaluator, runner, pinned conversion, stale-package remedy.
- P1.09 (local CI script) and P1.10 (run settings, Debian/ARM64) are planned only.
- P1.11 is "ongoing": it can start now (part a) but completes only after the other packages report (part b).

## 3. Decisions Required from the User

**Documentation structure.** Options:
- (A) Keep the procedures spread over `README.md`, `DEPENDENCIES.md` and `docs/p1` records.
- (B) A new `docs/DEVELOPER_GUIDE.md`.
- (D) `CONTRIBUTING.md`, the GitHub convention, in place of the guide, with the same content and mapping.

**Recommended: B.** The content is a full developer manual, broader than contribution rules. A future `CONTRIBUTING.md` can then link to the guide. Option A keeps today's duplication.

Under option B, the single-source mapping is:

| Document | Holds | Does not hold |
|---|---|---|
| `README.md` | Purpose, current status summary, quickstart of at most three commands, links | Detailed procedures, dated status history |
| `docs/DEVELOPER_GUIDE.md` (new) | Every developer procedure with its status; version literals only inside executable commands that require them (see "Version literals" below) | Version statements in prose, licenses (links to `DEPENDENCIES.md`), evidence values such as test counts or coverage figures (links to records) |
| `DEPENDENCIES.md` | Versions and licenses of packages, SDK and local tools | Procedures |
| `docs/COMPATIBILITY.md` (planned by P1.10) | Platform matrix | Procedures |
| `docs/p1/*` records | Evidence history: original content preserved, later evidence appended as dated additions | Rewritten historical statements, inventories or checksums |
| `tests/WeatherParser.Package.Tests/README.md` | One-line pointer to the guide's package section (recommended) | A second copy of the procedure |
| `AGENTS.md` | Rules, including the consistency table (section 5, step 5) | Procedures |

The "Coverage gate" block added to `README.md` in `b6100e2` moves to the guide; the README keeps a link.

## 4. Artifacts

| Path | New/Changed | Responsibility |
|---|---|---|
| `docs/DEVELOPER_GUIDE.md` | New | All procedures, each item with exactly one status |
| `README.md` | Changed | Reduced to the mapping above; links to the guide. It is also the NuGet package readme (`PackageReadmeFile`, packed by `src/WeatherParser.Parsing/WeatherParser.Parsing.csproj`), so it must remain a meaningful package description |
| `DEVELOPMENT_PLAN.md` | Changed | Receives dated status paragraphs moved out of `README.md` that no record already contains |
| `tests/WeatherParser.Package.Tests/README.md` | Changed | Pointer to the guide |
| `AGENTS.md` | Changed | New "Documentation" subsection with the consistency table |
| `PHASE_P1_PLAN.md` | Changed | P1.11 row: plan link and status "ongoing" |
| `docs/p1/P1_11_DEVELOPER_INSTRUCTIONS.md` | New | Result record: check results and walkthrough logs |

### Guide outline and initial status

Status values:
- **Verified** — the current documented command passed against a named revision. On Windows this is the P1.11 walkthrough (section 6), or a package record that ran exactly that command on the current project structure. On Debian/ARM64 it is the recorded P1.10 target execution. The guide names the revision and the record.
- **Planned** — the package or walkthrough that will verify it is named.
- **OPEN** — required but not achievable yet; the reason is given.

Historical records keep their original scope. A record proving an earlier project structure, such as P1.03 before the coverage and runner projects existed, is linked as historical evidence; it never makes the current command Verified on its own.

Every item also names its platform: **Windows**, **Debian/ARM64**, or **any** for commands proven on both.

Section 9 contains statements, not procedures, so it carries no status.

| Section | Item | Status basis (after the part (a) walkthrough unless stated) |
|---|---|---|
| 1 Prerequisites | SDK selection via `global.json` (rollForward disable) | Windows: walkthrough; historical: P1_03_BUILD_SETUP.md |
| | Git (clone, checkout) | Planned – P1.09 (first record of a fresh-clone run) |
| | PowerShell 7 | Planned – P1.09 |
| | `dotnet tool restore` (pinned `dotnet-coverage`) | Windows: P1_08_COVERAGE_EVALUATION.md §8 at `b6100e2`, rechecked by the walkthrough (P1.08 acceptance pending) |
| | Telemetry opt-out variables | Verified as documented – P1_02_TOOLING_REVIEW.md; use in CI Planned – P1.09 |
| 2 Restore and build | `dotnet restore WeatherParser.slnx --locked-mode`, Release build of the current solution (seven projects in `WeatherParser.slnx`) | Windows: walkthrough; historical: P1_03_BUILD_SETUP.md (two-project structure) |
| | Pack before restoring the package consumer | Windows: walkthrough; historical: P1_04_PACKAGE_CONSUMER.md |
| 3 Tests | Offline per-project test runs (all four test projects) | Windows: walkthrough; historical: P1_05_TDD_BASELINE.md, P1_06_DI_CONTRACTS.md, P1_08_COVERAGE_EVALUATION.md §8 |
| | Test conventions | Link to AGENTS.md "Tests"; not repeated |
| 4 Pack and consumer | Local feed `artifacts/local-nuget`, isolated consumer | Windows: walkthrough; historical: P1_04_PACKAGE_CONSUMER.md |
| | Stale cached package: symptom and manual refresh (repack, cache entry, consumer lock) | Windows: step 3 of part (a) performs it; historical: P1_08_COVERAGE_EVALUATION.md §8 |
| | Automated prevention (isolated `NUGET_PACKAGES`, `contentHash` rewrite) | Planned – P1.09 |
| 5 Coverage | Collection | Windows: walkthrough; historical: P1_07_COVERAGE_CONTRACT.md |
| | Conversion with the pinned tool | Windows: walkthrough; P1_08_COVERAGE_EVALUATION.md §8 |
| | Runner `evaluate`/`write-baseline`, JSON contract, exit codes, baseline rules | Windows: walkthrough; P1_08_COVERAGE_EVALUATION.md §8 at `b6100e2` (P1.08 acceptance pending) |
| | Two-pass baseline and collection-settings comparison | Planned – P1.09 |
| 6 Local CI | `scripts/ci-local.ps1` parameters, exit codes, run layout, fault injection, lock-file drift detection | Planned – P1.09 |
| 7 Debian/ARM64 | Target preparation, `-RunSettings`, `-CollectOnly`/`-ResumeFrom`, target run | Debian/ARM64: Planned – P1.10 recorded target execution (Class B may end OPEN); `-ResumeFrom` on Windows: Planned – P1.10 |
| 8 Troubleshooting | NETSDK1141 in Visual Studio (`dotnet build-server shutdown` plus restart) | Windows: remedy recorded in P1_03_BUILD_SETUP.md; not reproducible on demand, so documented as a historical remedy without Verified status |
| | Stale cached package | Windows: see section 4 |
| | Lock-file drift | Planned – P1.09 |
| 9 Limits | Local runs are not hosted CI; Windows evidence is not ARM64 evidence; coverage contract summary linking PHASE_P1_PLAN.md §5 | Statements, no status |

The guide links to records for test counts and coverage figures and never copies them. Commands use repository-relative paths only.

**Version literals.** Prose never restates a version; it links to `DEPENDENCIES.md`. An executable command may contain a version literal only when the command requires it and the version cannot be derived without extra tooling, such as the runner's `--tool-version 18.11.0`. Commands that the tools resolve themselves do not repeat versions: `global.json` selects the SDK, and `dotnet tool restore` reads `.config/dotnet-tools.json`. The guide ends with a "Version literals" table listing every literal, the command using it and its source of truth (for `--tool-version`: `Microsoft.CodeCoverage` in `Directory.Packages.props`). The single-source check (section 6) compares each listed literal with its source.

## 5. Procedure

### Part (a) – now (interim documentation delivery)

Part (a) produces an **interim delivery**: the guide, the README reduction and the checks for what exists today. It does not complete P1.11.

1. **Create `docs/DEVELOPER_GUIDE.md`** with the nine sections and the statuses above. Each Verified item contains the exact command and a link to its record; each Planned item names its package and the expected command from that package's plan, marked as planned.
2. **Reduce `README.md`** to the mapping. For every paragraph removed:
   - **A procedure** moves to the guide.
   - **A dated status paragraph** is either replaced by a link to the record that already contains it, or moved to `DEVELOPMENT_PLAN.md` if no record contains it.
   - **Nothing is lost:** list every removed paragraph and its destination in the result record.
3. **Repack and verify the package after the README change.** `README.md` is packed into `WeatherParser.Parsing.0.1.0-initial.nupkg`, so the reduced README changes the package content and hash. Until P1.09 automates this, do it manually, in this order:
   1. Review the reduced README as a standalone NuGet package description: library purpose, current implemented/planned scope, usage entry point and limitations. Keep detailed developer procedures in the guide. Use verified public repository documentation links where available, without inventing a published URL or relying on unpackaged local files.
   2. `dotnet pack src/WeatherParser.Parsing -c Release -o artifacts/local-nuget`. List the `.nupkg` entries: only `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE` and the NuGet metadata. Check the nuspec's canonical `README.md` reference and confirm the packed README/LICENSE are byte-identical to the source at the recorded revision. Record the package's base64 SHA-512.
   3. Remove only the stale cache entry `<global packages folder>/weatherparser.parsing/0.1.0-initial` (`dotnet nuget locals global-packages --list` shows the folder). The constant version would otherwise restore the old package.
   4. `dotnet restore tests/WeatherParser.Package.Tests --force-evaluate` rewrites the consumer lock file. Its diff must change only the `contentHash` of `WeatherParser.Parsing`; any other change stops the step and is reported.
   5. `dotnet restore tests/WeatherParser.Package.Tests --locked-mode` must succeed. The restored `.nupkg.sha512` must equal the recorded hash, and `dotnet test tests/WeatherParser.Package.Tests` must pass.
   6. Commit the README change and the updated consumer lock file together. The committed lock file then matches exactly the package artifact built and verified in this recorded run, identified by the SHA-512 recorded in item 2. Packing is not proven deterministic, so a later pack of the same commit may produce a different hash. Every later pack and consumer restore is therefore verified again: manually as in this step, or by P1.09's `contentHash` rewrite and hash check once it exists. No guarantee is made for future packages.
   7. **Gate classification:** this commit is **not** documentation-only for the coverage gate. Under the binding rule (PHASE_P1_PLAN.md section 5, T-P1-23), only Markdown documentation qualifies, and any package or lock-file change makes the whole commit a code change. `packages.lock.json` changes the package, so the gate must classify the commit as code. (The P1.08 `PullRequestClassifier` at `b6100e2` was broader than this rule; the defect was corrected test-first afterwards and is described in the correction note of `P1_08_COVERAGE_EVALUATION.md`. This step relies on the binding rule either way.) Below a 100% baseline it must therefore strictly improve block coverage, or the `Comparison` rule fails (exit 1 from the runner, 90 from the P1.09 script).
      - Either combine the README change with meaningful coverage improvements (for example the tests of P1.10 decision h), or obtain a separately authorized, narrowly scoped rule change.
      - Never relabel the commit as documentation-only, and never split the lock-file update into a later commit. The committed lock file would then point to an outdated package.
      - Record the gate result of this commit in the result record.
4. **Point `tests/WeatherParser.Package.Tests/README.md`** to the guide's package section.
5. **Add the consistency table to `AGENTS.md`** under "Documentation". It is enforced by the documentation-consistency review that AGENTS.md already requires, with no new tooling:

   | Change in | Must update |
   |---|---|
   | `global.json` | Guide §1, `DEPENDENCIES.md` |
   | `Directory.Packages.props`, `packages.lock.json` | `DEPENDENCIES.md` |
   | `.config/dotnet-tools.json` | `DEPENDENCIES.md`, guide §5 |
   | Runner commands, JSON contract or exit codes | Guide §5, the P1.09 script (shared contract) |
   | `scripts/ci-local.ps1` parameters, exit codes or run layout | Guide §6 |
   | `coverage.runsettings` | Guide §5 and §7, `docs/COMPATIBILITY.md` |
   | Project added to or removed from `WeatherParser.slnx` or `WeatherParser.Ci.slnf` | Guide §2 and §3 |
   | Phase status | README status summary, `PHASE_P1_PLAN.md`, `DEVELOPMENT_PLAN.md` |
   | `README.md` (packed package readme) | Repack, consumer lock `contentHash` and consumer tests as in part (a) step 3, in the same commit (until P1.09 automates it) |
   | `Directory.Packages.props` version used in a guide command | The guide's "Version literals" table and the commands listing it |

6. **Update `PHASE_P1_PLAN.md`:** the P1.11 row gets the plan link and the status "ongoing". The existing section 8 checkbox "English output/comments and complete English documentation" is mapped to this package; no new checkbox is added.
7. **Run the documentation checks** (section 6) on the changed documents and record them.

### Part (b) – after each of P1.08 closure, P1.09 and P1.10 reports

1. Switch the affected items to Verified, or to OPEN with a reason, and link the record and commit.
2. Rerun the walkthrough for those sections from a fresh clone, and record it.
3. **Completion.** P1.11 is complete only when part (b) is done for P1.08 closure, P1.09 and P1.10. At that point no item may remain Planned.
   - The only items allowed to stay OPEN are P1.10 Class B platform items (Debian/ARM64 target preparation and target run) whose P1.10 record is OPEN, with that record's reason.
   - Any other unverified item means P1.11 is not complete.
   - If a package has not reported, P1.11 stays ongoing, and P1.12 records it as a blocker; it is not handed over as complete.
   - Name each permitted OPEN platform item in the result record's P1.12 handover, with its P1.10 evidence/reason, owner and acceptance impact. P1.12 consumes this record and audits those items and subsequent changes; it does not repeat P1.11's full audit.

## 6. Tests and Test IDs

PHASE_P1_PLAN.md defines no test ID for P1.11, so none is invented.

| Check | Evidence | Expected result |
|---|---|---|
| P1.11 check: Windows walkthrough | Every Windows and "any" item's commands, copy-pasted from the guide, run in a fresh clone of a named revision with PowerShell 7 on Windows | The documented result is reproduced; deviations are fixed in the guide or recorded; passing items become Verified with that revision |
| P1.11 check: Debian/ARM64 evidence | Every Debian/ARM64 item compared with the P1.10 record of the recorded target execution. Repeated on the target only if decision b of P1.10 authorizes it again | Each item matches a recorded, passing target command (Verified), or carries the P1.10 OPEN reason; never Verified from a Windows run |
| P1.11 check: links | Every relative Markdown link and anchor in the changed documents | All resolve |
| P1.11 check: language/privacy | Changed documents searched for non-English prose, absolute user paths (`C:\Users\`, `/home/`), host names, IP addresses, user names and tokens | None found; historical content of `docs/p1` records unchanged, additions dated |
| P1.11 check: status | Every guide item compared with the records | Exactly one status per item, consistent with its record; section 9 has none |
| P1.11 check: package readme | Part (a) step 3 | Allowed entries only; packed README byte-identical to the committed one; consumer lock diff limited to the `WeatherParser.Parsing` `contentHash`; locked restore and consumer tests pass; restored hash equals the packed hash |
| P1.11 check: requirement IDs | The sequence of `**REQ-nnn**` identifiers in `REQUIREMENTS.md` at the walked-through revision, compared with reference commit `b6100e2` | Exactly 84 unique IDs (range REQ-001 to REQ-112, with gaps), in the identical established document order. That order is deliberate and not numeric, for example REQ-050 and REQ-057 are not in ascending position, so it is never "corrected" by sorting. Any added, removed, renumbered or moved ID is a finding unless a recorded requirement decision explains it |
| P1.11 check: comment language | All inline and block comments and XML documentation comments in `src/`, `tests/` and `tools/` C# files, and comments in scripts and configuration examples (`scripts/`, `*.ps1`, `*.sh`, `*.props`, `*.csproj`, `*.json` examples, `*.runsettings`, `*.slnf`), at the walked-through revision | English only (AGENTS.md, REQ-009). Every non-English comment is listed with file and line, and fixed in its owning package. Technical identifiers, CLI options, data fields, source names and URLs are exempt |
| P1.11 check: preservation | Reference revision `b6100e2` compared with the walked-through revision; both must be commits. Hash the **committed content** of each file at each revision (`git rev-parse <rev>:<path>` for the blob ID, or `git show <rev>:<path>` piped to SHA-256), never working-tree bytes: with `core.autocrlf=true` the checked-out files carry CRLF line endings and hash differently although unchanged. **Byte-identical set:** `LICENSE`, `docs/p1/P1_01_BASELINE.json`, `docs/p1/P1_02_DEPENDENCY_INVENTORY.json`. **Append-only set:** the evidence records `docs/p1/P1_01_BASELINE.md` to `P1_07_COVERAGE_CONTRACT.md` and `P1_08_COVERAGE_EVALUATION.md`. Living plan files (`*_PLAN.md`) are excluded, because they change through review | Byte-identical set: equal blob IDs (or SHA-256) at both revisions. Append-only set: the reference blob is an unchanged byte prefix of the current blob, so the change consists only of additions at the end, under a dated heading. Any other change is a finding. The packed-LICENSE comparison in step 3 does not replace this check, because it only compares the package with the current source |
| P1.11 check: single source | Versions, test counts and coverage figures searched for in README and the guide | Test counts and coverage figures only in records; versions only in `DEPENDENCIES.md`, configuration files and the guide's "Version literals" table, where each literal equals its source |

## 7. Evidence to Record

`docs/p1/P1_11_DEVELOPER_INSTRUCTIONS.md` records:
- The revision walked through, and the SDK and PowerShell versions used.
- The package SHA-512 after the README change, and the consumer lock diff.
- The package entry list, canonical nuspec README reference, README/LICENSE byte comparisons, packed/restored hash equality, locked-consumer test result and standalone NuGet-description review.
- The walkthrough log per section: commands, date, result.
- The link, language/privacy, status, single-source, requirement-ID and comment-language check results. For the requirement IDs, record the count and a checksum of the ID sequence for both revisions. For comments, record the scanned file set and every finding.
- The coverage-gate result of the README and lock commit (part (a) step 3).
- The preservation check: reference and walked-through revisions, the file sets, the blob IDs (or SHA-256 of committed content) of the byte-identical set at both revisions, and for each append-only record the prefix result with the size of its dated additions.
- The status table of all guide items.
- The list of paragraphs removed from `README.md` with their destination.

Part (b) appends one entry per package report.

## 8. Risks and Limitations

- `README.md` mixes dated status paragraphs with procedures; the move must not lose information (step 2 lists every paragraph and its destination).
- `README.md` is the packed package readme: every README edit changes the package and its hash, so it needs the repack and consumer check of step 3. P1.12 likewise requires a new pack/content check for the final candidate after any README change.
- Because of the lock-file update, every README commit counts as a code change for the coverage gate (step 3, item 7). It cannot pass at equal coverage below 100%, so README work has to be scheduled together with real coverage improvements, or the gate result must be expected and recorded. An exact 100% baseline is the exception: an exact 100% result then passes at equality.
- The comment-language check scans files that other packages own; P1.11 reports findings but does not fix code outside documentation.
- Planned items describe intended commands from plans that may still change; they are re-checked in part (b).
- The guide reflects the record state at its commit; the consistency table relies on reviews and is not enforced automatically.
- Debian/ARM64 sections may remain OPEN if P1.10 Class B cannot run; P1.12 decides how to treat that.
- The NETSDK1141 remedy cannot be reproduced on demand, so it stays a documented historical remedy rather than a Verified procedure.

## 9. Acceptance Criteria

- [ ] The documentation-structure decision is recorded.
- [ ] `docs/DEVELOPER_GUIDE.md` has all nine sections; every item names its platform and carries a status whose basis matches section 4. Historical records are linked with their original scope.
- [ ] The "Version literals" table exists, and each literal equals its source.
- [ ] `README.md` follows the mapping, and every removed paragraph has a recorded destination.
- [ ] The consistency table is in `AGENTS.md` under "Documentation".
- [ ] All documentation checks in section 6 pass and are recorded, including the package-readme repack, requirement-ID, comment-language and preservation checks.
- [ ] The README and lock commit has a recorded gate result. It is classified as a code change, never relabelled as documentation-only.
- [ ] Interim delivery (part a): the checks above pass for the current revision.
- [ ] Completion (part b): done for P1.08 closure, P1.09 and P1.10; no item is Planned; only P1.10 Class B platform items may be OPEN, with the P1.10 reason. Until then P1.11 stays ongoing.
