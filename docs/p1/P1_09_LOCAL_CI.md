# P1.09 – Local CI Job Sequence: Result Record

**Status (2026-10-19): implemented and verified on Windows x64; the genuine initial bootstrap seed is not yet established, and P1.09 acceptance is pending review.** Implements the [P1.09 plan](P1_09_LOCAL_CI_PLAN.md). The script was generated and repaired by the local qwen3-coder:30b model; Claude reviewed each version, ran every build and verification run, and described each defect to qwen, which wrote all script fixes.

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
| `scripts/ci-local.ps1` | 878-line PowerShell 7 script: every stage of the plan's section 5, the parameter and exit-code tables, the `current/` and `target/` pass directories, `collection-settings.json`, seven fault injections, two-pass baseline, `-BaselinePath`, `-Bootstrap` with seed output, and the finish stage |
| `WeatherParser.Ci.slnf` | Solution filter with the six projects other than the package consumer |
| `tests/WeatherParser.Coverage.Tests/PullRequestClassifierTests.cs` | Line-ending fix found by the clean-clone run (see findings) |

## Verification runs (Windows x64, PowerShell 7.6.7, Git 2.55, .NET SDK 10.0.402)

Final script revision: `1b6e6d0`. Run directories are under `artifacts/ci/` (gitignored, kept locally).

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

In every fault run the run directory, `ci.log`, `commits.json` (with `faultInjection` and `exitCode`) were kept.

## Findings

1. **Clean-checkout defect: missing local package source (T-P1-08).** `NuGet.Config` lists the gitignored `artifacts/local-nuget`, which does not exist in a fresh clone; every restore failed with NU1301. The script now creates the empty folder before the first restore of each clone. Developers following the current README in a fresh clone hit the same error unless they pack first; P1.11 should document this.
2. **Clean-checkout defect: line-ending-dependent test.** With `core.autocrlf=true` a fresh clone checks the changed-file fixtures out with CRLF. `Classify_FromFixtureFiles_ExpectedResult` split only on LF and failed there, while passing in the working tree. Fixed in the test, plus a new CRLF test.
3. **Block counts depend on the build configuration.** The same code measures 57/63 blocks in Debug (P1.08 figures) and 52/56 in Release (CI). Baselines are therefore only comparable within one configuration. The script builds both passes in Release, and `collection-settings.json` now records and compares `configuration` (an addition to the plan's field list).
4. **Duplicate coverage file.** The TRX logger stores a byte-identical copy of the raw report as a test attachment. The script accepts it only after a SHA-256 comparison with the collector's report, then removes the attachment folder.

## Deviations from the plan

- **Target-pass scope:** the target pass restores, builds and tests only `tests/WeatherParser.Parsing.Tests` (the product coverage run), because target commits older than this package have no `WeatherParser.Ci.slnf`.
- **Additional setting:** `configuration` in `collection-settings.json` (finding 3).
- **Additional file:** `dotnet-coverage-version.txt` per pass (raw tool output).

## Open items

- **Verification run 1 of the plan ("a two-pass run which passes") cannot pass today.** Every target with the runner (`b6100e2` or later) has the same library coverage, and the script itself is a code change, so the gate correctly reports equal coverage below 100% (exit 90). This is the situation described in the P1.10 plan (decision h) and the P1.12 plan: a passing two-pass run requires a strict coverage improvement through meaningful tests.
- **Genuine initial bootstrap seed:** not established. The plan requires it on a committed, accepted revision; the verification commits are local WIP commits. The seed run (`-Commit C -TargetRef C -Bootstrap` on the accepted commit `C`) and its record are still to be done.
- **Stale committed consumer lock file:** `README.md` is the packed package readme. The README edits since `2f8c670` (including this package's procedure section) change the package hash, so the committed `tests/WeatherParser.Package.Tests/packages.lock.json` no longer matches a freshly packed package. The CI script is unaffected because it rewrites the `contentHash` in its clone, but a manual locked consumer restore in the working tree fails until the lock is refreshed as described in step 3 of the [P1.11 plan](P1_11_DEVELOPER_INSTRUCTIONS_PLAN.md).
- **No hosted CI:** a local run is not a GitHub Actions run; creating a workflow needs separate authorization.
- **Platform:** Windows x64 only; Debian/ARM64 is P1.10.

## Acceptance criteria (plan section 9)

- [x] Decisions a–e recorded with the user's choice.
- [ ] A run of the verified commit from a fresh clone exits 0 with every listed file: met by the bootstrap mechanics run only; the two-pass run correctly exits 90 (see open items).
- [x] Each `-FaultInjection` value and the TargetRef=Commit case exit with the documented code; run directory and `ci.log` kept.
- [x] `-Bootstrap -FaultInjection GateFailure` exits 90 and keeps `ci.log` and `gate-result.json`.
- [x] Two-pass runs write `collection-settings.json` for both passes; `IncomparableTarget` exits 85.
- [x] Separate `current/` and `target/` directories; the gate evaluates the current pass's report.
- [x] Locked consumer restore; only the `contentHash` changes; restored hash equals the packed hash.
- [ ] Genuine initial bootstrap seed: open.
- [x] The `.nupkg` contains only the allowed entries.
- [x] No step uses the global `dotnet-coverage`, a secret or the developer working tree.
- [x] This record and the documents named in section 7 are updated.
