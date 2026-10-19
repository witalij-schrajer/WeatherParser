# P1.09 – Local CI Job Sequence: Result Record

**Status (2026-10-19): implemented, verified on Windows x64 and accepted by the user ([acceptance](#p109-acceptance-2026-10-19)); the genuine initial bootstrap seed is established on commit `0a2dabe`.** Implements the [P1.09 plan](P1_09_LOCAL_CI_PLAN.md). The script was generated and repaired by the local qwen3-coder:30b model; Claude reviewed each version, ran every build and verification run, and described each defect to qwen, which wrote all script fixes.

## Decisions (user, 2026-10-19)

All five decisions of the plan's section 3 were taken as recommended:
- **a)** PowerShell 7 script.
- **b)** Two-pass baseline measured from `-TargetRef`, with `-BaselinePath` as an explicit override.
- **c)** The consumer lock file is kept; only the `WeatherParser.Parsing` `contentHash` is rewritten, then restored in locked mode.
- **d)** Build warnings are recorded, not fatal.
- **e)** `-TargetRef` is a required parameter.

## Delivered

| Path | Content |
|---|---|
| `scripts/ci-local.ps1` | 935-line PowerShell 7 script: every stage of the plan's section 5, the parameter and exit-code tables, the `current/` and `target/` pass directories, `collection-settings.json`, seven fault injections, two-pass baseline, `-BaselinePath` (with its sibling `collection-settings.json`), `-Bootstrap` with seed output, and the finish stage |
| `WeatherParser.Ci.slnf` | Solution filter with the six projects other than the package consumer |
| `tests/WeatherParser.Coverage.Tests/PullRequestClassifierTests.cs` | Line-ending fix found by the clean-clone run (see findings) |

## Verification runs (Windows x64, PowerShell 7.6.7, Git 2.55, .NET SDK 10.0.402)

Script revision of these runs: `1b6e6d0`; the review fixes (`7333ba4`, `532bd71`) were verified separately (sections "Review fixes" and "Second review fixes"). Run directories are under `artifacts/ci/` (gitignored, kept locally).

| Run | Command | Exit | Result |
|---|---|---|---|
| Two-pass | `-TargetRef b6100e2` | 90 | Every stage passes; the gate rejects equal coverage 52/56 (92.86%) for a code change: Comparability, Measurement, Minimum and Baseline passed, Comparison failed. Correct gate behaviour (see "Two-pass result" below) |
| Bootstrap (mechanics check) | `-TargetRef HEAD -Bootstrap` | 0 | First fully green run; `current/bootstrap-baseline.json` written and read back (commit `1b6e6d0…`, 52/4 blocks). Run on a WIP commit, so it is **not** the accepted seed |
| Validation | `-TargetRef HEAD` (equals `-Commit`, no `-Bootstrap`) | 20 | "TargetRef equals Commit without Bootstrap" |
| Validation | unknown ref; `-Bootstrap -BaselinePath`; `IncomparableTarget` with `-Bootstrap` | 20 each | English diagnostics, before any clone |

Evidence of the two-pass and bootstrap runs:
- **Test counts:** WeatherParser.Parsing.Tests 8, WeatherParser.Coverage.Tests 91, WeatherParser.Coverage.Runner.Tests 54, WeatherParser.Package.Tests 2; all passed.
- **Build warnings:** 0.
- **Package:** `WeatherParser.Parsing.0.1.0-initial.nupkg` contains exactly `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE`, `WeatherParser.Parsing.nuspec`, `[Content_Types].xml`, `_rels/.rels` and `package/services/metadata/core-properties/nuget.psmdcp`. SHA-512 of the bootstrap run's package: `eiMOX84RfKw+imDZfn5dBLr11gNEWDth3cJQNRzIuDcTnXowmc3myijgZTDyCnAXhC8+U/wU6N0W226NKKU+nA==`; the consumer restored the same hash (T-P1-21).
- **Consumer lock:** after the `contentHash` rewrite, `git diff --numstat` shows exactly one changed line, and a JSON comparison confirms no other difference; the locked consumer restore passes.
- **Collection settings (both passes identical except `commit`):** SDK 10.0.402, `Microsoft.CodeCoverage` 18.11.0, `dotnet-coverage` 18.11.0 (manifest and tool), collector "Code Coverage", run settings `none`, exclusions `*.Tests.dll` and `WeatherParser.Coverage*.dll`, RID `win-x64`, configuration Release.
- **Durations:** two-pass 27 s, bootstrap 20 s.

## Genuine initial bootstrap seed (2026-10-19)

Run by user instruction on the committed revision `0a2dabe` (the reviewed script `1b6e6d0` plus the documentation commit), after the verification runs above:

```powershell
pwsh ./scripts/ci-local.ps1 -Commit 0a2dabe -TargetRef 0a2dabe -Bootstrap
```

- **Justification:** no accepted integrated pipeline baseline existed before this run. The runner tests and the 57/63 measurement at `b6100e2` were evaluator/runner evidence in a Debug build, not an accepted pipeline baseline; the earlier bootstrap run on `1b6e6d0` was a mechanics check on a WIP commit. This is the first accepted, fault-free full-pipeline run of the branch (plan section 4, initial-bootstrap decision).
- **Result:** exit 0 in 20 s; run directory `artifacts/ci/0a2dabe-20261019T171822Z/` (local, gitignored). Every stage passed: 0 build warnings; tests 8 / 91 / 54 / 2 passed; package-content check passed; the consumer restored the same-run package (SHA-512 `eR1GVAXBWRwNCyMqYpt7IWcLGYFlQvhIivbUMJYjEKnvz62VuJ0NHJ5flMBhgZcjR/OYmGb9twvJVFPfflJu9g==`). The gate passed in bootstrap mode: Measurement, Minimum (52/56 = 92.86%) and Baseline ("Initial bootstrap").
- **Seed baseline** `current/bootstrap-baseline.json`, written by `write-baseline` and read back by the script (commit and counters checked against the gate result):

  ```json
  {"commitId":"0a2dabeaa4b4baa836b87d7a4aa1a456e3dcb552","toolName":"Microsoft.CodeCoverage","toolVersion":"18.11.0","format":"native-xml","exclusions":["*.Tests.dll","WeatherParser.Coverage*.dll"],"blocksCovered":52,"blocksNotCovered":4}
  ```

- **Collection settings of the seed:** SDK 10.0.402, `Microsoft.CodeCoverage` 18.11.0, `dotnet-coverage` 18.11.0 (manifest and tool), collector "Code Coverage", run settings `none`, exclusions as above, RID `win-x64`, configuration Release.
- **Provenance (SHA-256 of the run artifacts):**
  - `current/bootstrap-baseline.json`: `06c6ade34a0b7d19b1e899ac42f41e0723ebd7880acf8022470c034e444dde65`
  - `current/collection-settings.json`: `865f19a0c8b09d01b0b942163c328cec0089b02d695730e46a69d850a304edb9`
  - `current/coverage/coverage.xml`: `067a24c5e7599d6ac7da213153bbc0f4f498f9ef1b1c65607bc68f08c045c1a7`
  - `gate-result.json`: `040ef8d2592972b5f72f6847cbdcaf4046b4bf5c9cb23b85823f410699712c67`
  - `commits.json`: `cc70431da647a9e7083ed1ec1c6fc228d92dd22c91514f81b2e9c6492f6daea5`
- **Consequence:** the bootstrap exception is used up for this branch. Every later candidate is compared normally with a comparable baseline, for example `0a2dabe` remeasured by the two-pass procedure (`-TargetRef 0a2dabe`). Below 100%, a code change must strictly increase block coverage; documentation-only changes may keep it equal. Losing these local artifacts, changing platform or lacking older tooling never justifies another bootstrap. Because the run directory is local, the baseline values above are the durable record; the two-pass procedure remeasures the target anyway.

## Review fixes (2026-10-19)

An external review of the seed evidence confirmed the five artifact hashes, the package hash and the 155 passing tests, and found three script defects. qwen fixed them from Claude's descriptions (script revision `7333ba4`); Claude reviewed the diffs and returned wrong or unapplied edits to qwen before verification.

1. **[P1] External baselines bypassed the settings comparison.** `-BaselinePath` passed the baseline straight to the runner, so a Release report 52/56 "improved" on a synthetic Debug baseline 57/63. An external baseline now requires a sibling `collection-settings.json`, as written next to `target/baseline.json` and `current/bootstrap-baseline.json`. Before any clone the script checks that this file exists and that the baseline `commitId`, the settings `commit` and the `-TargetRef` SHA are equal (exit 20). The gate stage copies both files to `baseline-input/`, logs their SHA-256 and compares the settings with the current pass through the same `Assert-ComparableSettings` function as the two-pass mode (exit 85).
2. **[P2] SDK failures lacked `commits.json`.** The initial `commits.json` is now written right after the run directory is created, before the clone and SDK check; the `finally` block adds `finishedUtc` and the final `exitCode`. Validation errors before the run directory still create no run directory.
3. **[P2] Relative `-BaselinePath` values.** The path is resolved to an absolute path right after the existence check, so the runner inside the temporary clone receives the file that was validated.

Verification with candidate `ef9b9ac`, a documentation-only change against `0a2dabe` (`DEVELOPMENT_PLAN.md`, `PHASE_P1_PLAN.md`, `docs/p1/P1_09_LOCAL_CI.md`):

| Run | Exit | Result |
|---|---|---|
| `-TargetRef 0a2dabe -FaultInjection SdkMismatch` | 10 | `commits.json` present with `faultInjection` `SdkMismatch` and `exitCode` 10 |
| `-BaselinePath` without sibling settings | 20 | "External baseline requires a sibling collection-settings.json: …", before any clone |
| `-TargetRef b6100e2 -BaselinePath` (seed of `0a2dabe`) | 20 | "External baseline commit '0a2dabe…' does not match TargetRef commit 'b6100e2…'" |
| `-TargetRef 0a2dabe -BaselinePath` synthetic Debug baseline 57/63 with Debug settings | 85 | "field 'configuration' differs (current: 'Release', baseline: 'Debug')": the reviewer's false improvement is rejected |
| `-TargetRef 0a2dabe -BaselinePath artifacts/ci/0a2dabe-20261019T171822Z/current/bootstrap-baseline.json` (relative path to the genuine seed) | 0 | All five rules passed; 52/56 equals the baseline 52/56, which is allowed for a documentation-only change. The logged SHA-256 of the copied baseline (`06c6ade3…`) and settings (`865f19a0…`) equal the seed provenance |
| `-TargetRef 0a2dabe` (two-pass) | 0 | First passing two-pass run: all five rules passed, 52/56 against the remeasured target 52/56, documentation-only change |
| `-TargetRef 0a2dabe -FaultInjection IncomparableTarget` | 85 | Two-pass comparison unchanged after the refactoring |

## Second review fixes (2026-10-19)

A second external review confirmed the recorded results (155 passing tests, 52/56 blocks, the five seed hashes, package hashes and entries, fault-injection exits, the three earlier fixes) and found two more defects. qwen fixed them from Claude's descriptions (script revision `532bd71`); Claude returned one StrictMode defect in qwen's first version before verification.

1. **[P2] A matching consumer lock file failed.** The consumer stage required exactly one changed line, so a committed lock that already held the packed hash exited 70. The stage now accepts either no change at all (empty `git diff --numstat` and `git status --porcelain`) or exactly the one-line `contentHash` change, logs which case applied, and keeps the JSON comparison and hash check for both.
2. **[P3] Incomplete settings comparison.** `Assert-ComparableSettings` compared a fixed list that omitted `dotnetCoverageManifestVersion`. It now compares the union of all recorded fields except `commit` (arrays joined with `,`), so a field present on one side only is also a difference.

Verification:

| Check | Result |
|---|---|
| `Assert-ComparableSettings` extracted from the script, against the seed settings with one change each | identical and `commit`-only: pass; `dotnetCoverageManifestVersion` 18.10.0, shortened `exclusions`, an extra field, a missing `rid`: exit 85 naming the field |
| Consumer-lock check extracted from the script, in a scratch Git repository with the real lock file | unchanged lock: pass ("already matches"); one-line hash change: pass ("rewritten"); additional `resolved` change: exit 70; unchanged lock with a different packed hash: exit 70 |
| `-Commit 532bd71 -TargetRef ef9b9ac` (full pipeline) | exit 90: the consumer stage passes with the one-line rewrite and comparability passes; Comparison fails because the script change keeps coverage equal |
| `-Commit 532bd71 -Bootstrap -FaultInjection ConsumerLockDrift` | exit 70 with NU1102, unchanged |

The unchanged-lock case cannot be reached in a full pipeline run, because the package embeds the commit ID (see finding 5); a temporary commit whose lock held the hash of the previous commit's package was still rewritten (one line), as expected.

## Fault injections (T-P1-08, T-P1-19)

| Fault | Expected | Actual | Diagnostic |
|---|---|---|---|
| `SdkMismatch` | 10 | 10 | Names the version required by `global.json` and the installed SDKs |
| `LockDrift` | 30 | 30 | Locked restore fails |
| `FailingTest` | 50 | 50 | Quotes the test error; its TRX is kept in `current/trx` |
| `MissingCoverage` | 80 | 80 | "Expected exactly one collector .coverage file, found 0" |
| `ConsumerLockDrift` | 70 | 70 | Quotes NuGet's NU1102 error naming FluentAssertions |
| `GateFailure` (with `-Bootstrap`) | 90 | 90 | `gate-result.json` shows `current` 57/33 and a failed Minimum rule; no seed file written |
| `IncomparableTarget` (two-pass) | 85 | 85 | Names the `dotnetCoverageToolVersion` field and both values |

In every fault run the run directory and `ci.log` were kept. Correction: with script revision `1b6e6d0` the `SdkMismatch` runs had no `commits.json`, because the file was written only after the SDK check. The review fix writes it before the clone, so every fault run now keeps `commits.json` with `faultInjection` and `exitCode`.

## Findings

1. **Clean-checkout defect: missing local package source (T-P1-08).** `NuGet.Config` lists the gitignored `artifacts/local-nuget`, which does not exist in a fresh clone; every restore failed with NU1301. The script now creates the empty folder before the first restore of each clone. Developers following the current README in a fresh clone hit the same error unless they pack first; P1.11 should document this.
2. **Clean-checkout defect: line-ending-dependent test.** With `core.autocrlf=true` a fresh clone checks the changed-file fixtures out with CRLF. `Classify_FromFixtureFiles_ExpectedResult` split only on LF and failed there, while passing in the working tree. Fixed in the test, plus a new CRLF test.
3. **Block counts depend on the build configuration.** The same code measures 57/63 blocks in Debug (P1.08 figures) and 52/56 in Release (CI). Baselines are therefore only comparable within one configuration. The script builds both passes in Release, and `collection-settings.json` now records and compares `configuration` (an addition to the plan's field list).
4. **Duplicate coverage file.** The TRX logger stores a byte-identical copy of the raw report as a test attachment. The script accepts it only after a SHA-256 comparison with the collector's report, then removes the attachment folder.
5. **The package depends on the commit ID.** The packed `.nuspec` contains `<repository type="git" commit="…"/>`, and the assembly's informational version ends in `+<commit SHA>` (for example `0.1.0-initial+8f9f33c1…`). Two commits that differ only in the consumer lock file therefore produce packages with different `contentHash` values. The CI script handles this by rewriting the hash in its clone; a committed lock file can never hold the hash of the package built from its own commit.

## Deviations from the plan

- **Target-pass scope:** the target pass restores, builds and tests only `tests/WeatherParser.Parsing.Tests` (the product coverage run), because target commits older than this package have no `WeatherParser.Ci.slnf`.
- **Additional setting:** `configuration` in `collection-settings.json` (finding 3).
- **Additional file:** `dotnet-coverage-version.txt` per pass (raw tool output).

## Open items

- **Passing two-pass runs for code changes:** a code change with unchanged coverage correctly exits 90 (for example the script against `b6100e2`). The first passing two-pass run is the documentation-only candidate `ef9b9ac` against `0a2dabe` (section "Review fixes"). A passing two-pass run for a code change requires a strict coverage improvement through meaningful tests (P1.10 plan decision h, P1.12 plan).
- **Stale committed consumer lock file:** `README.md` is the packed package readme. The README edits since `2f8c670` (including this package's procedure section) change the package hash, so the committed `tests/WeatherParser.Package.Tests/packages.lock.json` no longer matches a freshly packed package. The CI script is unaffected because it rewrites the `contentHash` in its clone, but a manual locked consumer restore in the working tree fails until the lock is refreshed as described in step 3 of the [P1.11 plan](P1_11_DEVELOPER_INSTRUCTIONS_PLAN.md). Because the package embeds the commit ID (finding 5), a refreshed lock matches only packages built from the commit the refresh was packed from, not the commit that contains the refreshed lock; the P1.11 plan step 3 should take this into account.
- **No hosted CI:** a local run is not a GitHub Actions run; creating a workflow needs separate authorization.
- **Platform:** Windows x64 only; Debian/ARM64 is P1.10.

## Acceptance criteria (plan section 9)

- [x] Decisions a–e recorded with the user's choice.
- [x] A run of the verified commit from a fresh clone exits 0 with every listed file: the bootstrap runs (mechanics check and seed) and the two-pass run of `ef9b9ac` against `0a2dabe` (section "Review fixes").
- [x] Each `-FaultInjection` value and the TargetRef=Commit case exit with the documented code; run directory and `ci.log` kept.
- [x] `-Bootstrap -FaultInjection GateFailure` exits 90 and keeps `ci.log` and `gate-result.json`.
- [x] Two-pass runs write `collection-settings.json` for both passes; `IncomparableTarget` exits 85; an external `-BaselinePath` is compared with its sibling settings (exit 85 on a Debug/Release mismatch).
- [x] Separate `current/` and `target/` directories; the gate evaluates the current pass's report.
- [x] Locked consumer restore; only the `contentHash` changes; restored hash equals the packed hash.
- [x] Genuine initial bootstrap seed identified once on `0a2dabe`, with a passing full pipeline, justification, `current/bootstrap-baseline.json` and settings/provenance (section "Genuine initial bootstrap seed").
- [x] The `.nupkg` contains only the allowed entries.
- [x] No step uses the global `dotnet-coverage`, a secret or the developer working tree.
- [x] This record and the documents named in section 7 are updated.

### P1.09 acceptance (2026-10-19)

The user accepted the P1.09 results on 2026-10-19, after the third external review reported no new actionable defects. That review verified on script revision `532bd71`:
- matching consumer locks and permitted hash rewrites pass, and unrelated dependency changes fail;
- the settings comparison covers the manifest version, missing and extra fields, and exclusions;
- the latest retained pipeline run has 155 passing tests, zero build warnings, matching package hashes and the expected package contents;
- its exit 90 is correct, because coverage stays at 52/56 (92.86%) while this code change requires a strict improvement.

Remaining outside P1.09: a passing coverage comparison for a code change (a strict block-coverage improvement through meaningful tests) is still required for overall P1 acceptance; Debian/ARM64 (P1.10), developer instructions (P1.11, including the commit-dependent package hash of finding 5) and hosted CI remain open.
