# P1.09 – Local CI Job Sequence and Artifacts: Implementation Plan

**Status (2026-10-19): implemented, verified on Windows x64 and accepted by the user; see the [result and acceptance record](../../p1/P1_09_LOCAL_CI.md#p109-acceptance-2026-10-19).** The original plan below was drafted with the local qwen3-coder:30b model over five review rounds and finalized by review against [AGENTS.md](../../../AGENTS.md), the [P1 detailed plan](../PHASE_P1_PLAN.md) (sections 4 and 7) and the repository state at commit `2f8c670`. Covers T-P1-08, T-P1-19, T-P1-21 and T-P1-22. The original acceptance checklist is retained below; completed checks and deviations are recorded in the result record.

## 1. Goal and Scope

Provide one non-interactive, secret-free script that runs the P1 CI sequence locally from a fresh clone of a given commit, produces all artifacts in one run directory, and fails with a stage-specific exit code. A hosted CI job can later call the same script unchanged.

In scope:
- Clean-checkout reproducibility from a fresh clone with locked restore (closes the open P1.03 item, T-P1-08).
- Restore, build, offline tests with TRX results, pack, isolated package consumption of the package built in the same run (T-P1-21, T-P1-22).
- Microsoft.CodeCoverage collection, conversion to native XML, baseline creation from the target commit, changed-file list and the P1.08 coverage gate.
- Failure propagation with preserved diagnostics (T-P1-19), proven by fault injection.

Out of scope:
- Creating or enabling `.github/workflows/*`, pushes, releases or package publication: each requires separate explicit user authorization. A later hosted workflow may call this script.
- Debian/ARM64 execution (P1.10); runtime claims on the target hardware (P1.10/P9).
- The coverage runner and the pinned `dotnet-coverage` tool themselves (delivered by P1.08 closure).
- P2 entities/serializers, P3 CLI, P4–P8 parsers, P9 release.

## 2. Preconditions and Inputs

- Repository state at `2f8c670`: SDK 10.0.402 pinned in `global.json`; every project has a committed `packages.lock.json`; library block coverage 57/63 = 90.48% after the removal of the unused entity scaffolding.
- **Delivered by P1.08 closure (hard dependency):**
  - `tools/WeatherParser.Coverage.Runner` with commands `evaluate` and `write-baseline`; JSON result on stdout (`passed`, `current {covered, notCovered}`, `rules [{ruleId, verdict, diagnostic}]`); exit codes 0 pass, 1 coverage rule failed, 2 invalid usage or input; plus `tests/WeatherParser.Coverage.Runner.Tests`.
  - `.config/dotnet-tools.json` pinning `dotnet-coverage`; the global tool is not used.
- Package version is constant `0.1.0-initial`. On 2026-10-19 the committed consumer lock file was found pinning the stale P1.04 package, which proves the NuGet cache risk in practice.
- `tests/WeatherParser.Package.Tests/packages.lock.json` pins the package content hash; a fresh pack in a clone produces a different `.nupkg` unless packing is deterministic (unverified).
- Tools on the executing machine: PowerShell 7, Git, .NET SDK 10.0.402, network access to nuget.org.

## 3. Decisions Required from the User

a) **Script language.** Options: PowerShell 7 (`pwsh`) or bash. **Recommended: PowerShell 7.** One script runs on Windows and on Debian (P1.10), and it offers native SHA-512 and ZIP APIs. Bash would need Git Bash on Windows and extra tools.

b) **Baseline source.** Options: (1) two-pass run, where the script measures `-TargetRef` itself under identical conditions and writes the baseline with `runner write-baseline`; (2) an externally supplied `-BaselinePath`. **Recommended: (1).** No committed baseline file can go stale. Two passes alone do not guarantee comparable measurements: step 8 records the actual collection settings of each pass and rejects incompatible ones. Option (2) stays available as an explicit override.

c) **Consumer dependency locking.** The committed consumer lock file freezes every consumer dependency, and it also pins the content hash of the locally built `WeatherParser.Parsing` package, which changes on every pack. Options:
   - (1) Keep the lock file. The script replaces only the `contentHash` of the `WeatherParser.Parsing` entry in the cloned lock file with the hash of the package built in this run, then restores with `--locked-mode`. Every other dependency stays frozen, and NuGet's own hash verification proves package identity.
   - (2) Make packing deterministic, after first proving it by packing the same commit twice in two clones.
   - (3) Disable the consumer lock file and rely on the package hash check alone.

   **Recommended: (1).** The package hash only identifies the WeatherParser artifact; option (3) would stop detecting drift in the consumer's other dependencies. Option (2) is unproven. Under (1), any lock-file difference other than that one `contentHash` value fails the consumer stage.

d) **Warnings as errors.** Options: build with `-warnaserror`, or record the warning count only. **Recommended: record only** for P1, because the current build has 0 warnings. Making warnings fatal is a policy change for the user.

e) **Default `-TargetRef`.** Options: required parameter, or default `origin/features/develop/initial`. **Recommended: required.** The comparison target is a deliberate choice, and a wrong implicit default would compare against the wrong baseline.

## 4. Artifacts

| Path | New/Changed | Responsibility |
|---|---|---|
| `scripts/ci-local.ps1` | New | The CI sequence: parameters, stages, exit codes, logging, artifact layout |
| `WeatherParser.Ci.slnf` | New | Solution filter with every project except the package consumer, so the first locked restore does not need the not-yet-packed package |
| `docs/p1/P1_09_LOCAL_CI.md` | New | Result record of the verification runs |
| `README.md`, `PHASE_P1_PLAN.md`, `DEVELOPMENT_PLAN.md`, `DEPENDENCIES.md` | Changed | Procedure, P1.09 row and T-P1-08 status, phase status, pwsh as a CI prerequisite |

`WeatherParser.Ci.slnf` contains: `src/WeatherParser.Parsing`, `tests/WeatherParser.Parsing.Tests`, `tools/WeatherParser.Coverage`, `tests/WeatherParser.Coverage.Tests`, `tools/WeatherParser.Coverage.Runner`, `tests/WeatherParser.Coverage.Runner.Tests`.

### Script parameters

| Name | Type | Required/Default | Meaning |
|---|---|---|---|
| `-Commit` | string | default `HEAD` of the original repository | Commit to verify |
| `-TargetRef` | string | required (decision e) | Comparison target for the baseline and the changed-file list |
| `-BaselinePath` | string | optional | External baseline JSON; skips the baseline pass |
| `-Bootstrap` | switch | off | Explicit initial bootstrap: no baseline, minimum rule only |
| `-KeepWorkDir` | switch | off | Keep clones and the isolated NuGet folder |
| `-FaultInjection` | `SdkMismatch`, `LockDrift`, `FailingTest`, `MissingCoverage`, `ConsumerLockDrift`, `IncomparableTarget`, `GateFailure` | optional, verification only | Injects one fault inside the clone or the run directory; every injection is logged prominently in `ci.log` and recorded in `commits.json` |

`-Bootstrap` and `-BaselinePath` are mutually exclusive. If `-TargetRef` resolves to `-Commit` without `-Bootstrap`, the run fails with exit 20. Bootstrap is never implied (AGENTS.md: a missing baseline is never treated as bootstrap).

**Initial-bootstrap decision (2026-10-19, shared with the P1.12 plan section 4):** if no accepted integrated baseline exists, the first accepted, fault-free P1.09 full-pipeline run of this branch establishes it. Identify seed commit `C` and run `-Commit C -TargetRef C -Bootstrap`. Record why the earlier `b6100e2` runner tests/57/63 measurement are not an accepted pipeline baseline. Require every normal stage and the minimum rule to pass; preserve unsuccessful attempts. After gate success, call `write-baseline` on the current native report with commit `C` and its actual tool/exclusions, writing `<run>/current/bootstrap-baseline.json`; retain collection settings and provenance. Seed creation is required output of that bootstrap run. Fault-injection runs never establish the seed. If an accepted baseline already exists, do not create another bootstrap; later candidates use a valid comparable baseline and normal improvement rules. Losing a baseline, changing platform or lacking older tooling is not a bootstrap justification.

### Exit codes

| Code | Stage |
|---|---|
| 0 | All stages passed, including the gate |
| 10 | Environment: pwsh, git or `dotnet --version` (inside the clone) differs from the clone's `global.json` |
| 20 | Clone/checkout, unresolved refs, or TargetRef equal to Commit without `-Bootstrap` |
| 30 | Locked restore failed or lock-file drift (`git status --porcelain` not empty after restore) |
| 40 | Build |
| 50 | Tests |
| 60 | Pack or package-content check |
| 70 | Package consumer: restore, hash mismatch, build or tests |
| 80 | Coverage collection or conversion (not exactly one raw `.coverage` file in a pass, conversion error) |
| 85 | Incomparable measurements: the current and target passes differ in a recorded collection setting |
| 90 | Coverage gate: a rule failed (runner exit 1) |
| 91 | Coverage gate: invalid input (runner exit 2) |

### Run directory

The run directory is `<repo>/artifacts/ci/<commit-short>-<yyyyMMddTHHmmssZ>/` (gitignored). It is created first, in the original repository, and it is always kept. It contains `ci.log`, `commits.json` (commit, resolved target commit, both SHAs, any fault injection), `dotnet-info.txt`, `packages.txt` (`dotnet list package --include-transitive`), `WeatherParser.Parsing.0.1.0-initial.nupkg`, `package-hash.txt`, `changed-files.txt` and `gate-result.json`.

Each measurement pass has its own subdirectory, so the passes never share an output path: `current/` always, and `target/` in two-pass mode only. Each pass directory holds `trx/*.trx`, `coverage/raw/*.coverage`, `coverage/coverage.xml` and `collection-settings.json`; `target/` also holds `baseline.json`. `collection-settings.json` records the values actually used in that pass:
- SDK version (`dotnet --version` in that clone).
- `Microsoft.CodeCoverage` version from that commit's `Directory.Packages.props`.
- `dotnet-coverage` version from that commit's `.config/dotnet-tools.json` and from `dotnet tool run dotnet-coverage --version`.
- Collector name and the SHA-256 of the run-settings file used (or `none`).
- The exclusions passed to the runner, and the RID.

Every stage writes into the run directory directly; there is no copy step. The work directory `[IO.Path]::GetTempPath()/weatherparser-ci-<guid>/` holds `src/` (clone), `target/` (target clone) and `nuget-packages/`. It is deleted at the end unless `-KeepWorkDir` is set.

## 5. Procedure

Each step below is one script stage. Implement the stages in this order and verify each one before moving on.

1. **Setup (exit 10/20).** Create the run directory, then start logging every command and its output to `ci.log` and stderr. Check `pwsh` ≥ 7 and `git`. Resolve `-Commit` and `-TargetRef` to SHAs in the original repository. Run `git clone --no-local <repo> <work>/src` and `git -C <work>/src checkout --detach <commitSha>`. Apply `-FaultInjection SdkMismatch` here by changing the version in the clone's `global.json`. Run `dotnet --version` with the clone as working directory and compare it with the clone's `global.json`; a mismatch exits 10. Write `commits.json` and `dotnet --info` output.
2. **Locked restore (exit 30).** Set `$env:NUGET_PACKAGES = <work>/nuget-packages` once; it is an environment variable, never an MSBuild property. Apply `LockDrift` by changing a version string in the clone's `src/WeatherParser.Parsing/packages.lock.json`. Run `dotnet restore WeatherParser.Ci.slnf --locked-mode`, then require an empty `git -C <work>/src status --porcelain`.
3. **Build (exit 40).** Apply `FailingTest` by writing an extra test class with a failing assertion into the cloned `tests/WeatherParser.Parsing.Tests`. Run `dotnet build WeatherParser.Ci.slnf -c Release --no-restore` and record the warning count (decision d).
4. **Tests and coverage (exit 50/80).** For the pass directory `<pass>` (`<run>/current` here): run `dotnet test tests/WeatherParser.Parsing.Tests -c Release --no-build --logger trx --results-directory <pass>/coverage/raw --collect:"Code Coverage"`, the only product coverage run. Run `tests/WeatherParser.Coverage.Tests` and `tests/WeatherParser.Coverage.Runner.Tests` with `--logger trx` and without coverage. Move the TRX files to `<pass>/trx`. A failing test exits 50. Exactly one `*.coverage` file must exist under `<pass>/coverage/raw`, otherwise exit 80; `MissingCoverage` deletes it at this point.
5. **Pack and content check (exit 60).** Run `dotnet pack src/WeatherParser.Parsing -c Release --no-build -o <run>`. Open the `.nupkg` with `System.IO.Compression.ZipFile`. Fail unless it contains exactly `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE`, `WeatherParser.Parsing.nuspec`, `[Content_Types].xml`, `_rels/.rels` and `package/services/metadata/core-properties/*.psmdcp` (T-P1-22). Write the base64 SHA-512 (`[Convert]::ToBase64String([Security.Cryptography.SHA512]::HashData(...))`) to `package-hash.txt`.
6. **Package consumer (exit 70).** Copy the `.nupkg` into the clone's `artifacts/local-nuget/`.
   - In the clone's `tests/WeatherParser.Package.Tests/packages.lock.json`, replace only the `contentHash` of the `WeatherParser.Parsing` entry with `package-hash.txt` (decision c). Parse and rewrite the JSON; a missing entry exits 70. `ConsumerLockDrift` changes the version of another consumer dependency in that file at this point.
   - Run `dotnet restore tests/WeatherParser.Package.Tests --locked-mode`. NuGet verifies the package hash, and every other dependency stays locked.
   - Require that the only working-tree change in the clone is that lock file, and that its only changed value is the one `contentHash`; otherwise exit 70.
   - As an independent check, compare `<work>/nuget-packages/weatherparser.parsing/0.1.0-initial/weatherparser.parsing.0.1.0-initial.nupkg.sha512` with `package-hash.txt`; a mismatch exits 70 (T-P1-21).
   - Build and test the consumer with `--logger trx` (into `<run>/current/trx`) and without coverage, so the library is never counted twice.
7. **Coverage conversion (exit 80).** In the pass's clone, run `dotnet tool restore`, then `dotnet tool run dotnet-coverage merge <pass>/coverage/raw/<raw>.coverage -f xml -o <pass>/coverage/coverage.xml`. Write `<pass>/collection-settings.json` from that clone's own files and tools. `GateFailure` replaces `<run>/current/coverage/coverage.xml` at this point with the clone's stored fixture `tests/WeatherParser.Coverage.Tests/Fixtures/report-real-p1-07.xml` (product module 57/90). The real coverage is never lowered to provoke a failure.
8. **Baseline pass (exit 20/30/40/50/80/85/91).** Skip this step when `-Bootstrap` or `-BaselinePath` is given.
   - Clone `<targetSha>` into `<work>/target` and run steps 2–4 and 7 there with `<pass>` = `<run>/target`, without fault injection. If the target commit lacks the runner or the tool manifest, the run fails with an English diagnostic; it never falls back to bootstrap.
   - Compare `<run>/current/collection-settings.json` and `<run>/target/collection-settings.json` field by field (except the commit). Any difference exits 85 with an English diagnostic naming each differing field and both values. `IncomparableTarget` changes the recorded `dotnet-coverage` version in the target settings file to prove this check.
   - Write the baseline with the current clone's runner, using the target pass's OWN recorded values, never the current ones: `dotnet run --project <work>/src/tools/WeatherParser.Coverage.Runner -c Release --no-build -- write-baseline --report <run>/target/coverage/coverage.xml --commit <targetSha> --tool-name Microsoft.CodeCoverage --tool-version <target Microsoft.CodeCoverage version> --exclude *.Tests.dll --exclude WeatherParser.Coverage*.dll --output <run>/target/baseline.json`. The gate's comparability rule then checks the tool version a second time, independently of step 8's comparison.
9. **Changed files and gate (exit 90/91).** Write `git -C <work>/src diff --name-only <targetSha>...<commitSha>` to `changed-files.txt`. Run `dotnet run --project <work>/src/tools/WeatherParser.Coverage.Runner -c Release --no-build -- evaluate --report <run>/current/coverage/coverage.xml --changed-files <run>/changed-files.txt --tool-name Microsoft.CodeCoverage --tool-version <current Microsoft.CodeCoverage version> --exclude *.Tests.dll --exclude WeatherParser.Coverage*.dll`, followed by exactly one of:
   - two-pass mode: `--baseline <run>/target/baseline.json --expected-baseline-commit <targetSha>`;
   - `-BaselinePath`: `--baseline <BaselinePath> --expected-baseline-commit <targetSha>`;
   - `-Bootstrap`: `--bootstrap` alone. `--expected-baseline-commit` is never passed with `--bootstrap` (runner contract in the P1.08 closure plan, which tests this exact invocation).

   The report passed is always the current pass's report. Redirect stdout to `gate-result.json`. Map runner exit 1 to 90 and runner exit 2 to 91.
   After a successful genuine initial bootstrap, write `<run>/current/bootstrap-baseline.json` from that report using `write-baseline`, commit `<commitSha>` and the actual recorded tool/exclusions. Require successful writing and read-back of its identity/counters before the run can exit 0; a runner input/output failure exits 91. This seed-output step is skipped for fault-injection tests and normal comparison runs.
10. **Finish.** Write `packages.txt`. Log the duration and final exit code. Delete the work directory unless `-KeepWorkDir` is set; the run directory is kept in every case.

Verification runs, recorded in the result record:

1. A real run of `2f8c670`-or-later against a target with the same tooling, which passes.
2. The genuine initial bootstrap run described in section 4, or reference its existing accepted record. Later bootstrap tests exercise fixtures/faults only; they never establish a new accepted branch baseline.
3. One run per `-FaultInjection` value; `IncomparableTarget` needs two-pass mode.

## 6. Tests and Test IDs

| Test ID | Evidence | Expected result |
|---|---|---|
| T-P1-08 | Run from a fresh clone with locked restore | Exit 0 through all stages; no lock-file drift |
| T-P1-08 | `-FaultInjection LockDrift` | Exit 30 |
| T-P1-19 | `-FaultInjection SdkMismatch` | Exit 10, diagnostic names both versions |
| T-P1-19 | `-FaultInjection FailingTest` | Exit 50, TRX of the failing test kept |
| T-P1-19 | `-FaultInjection MissingCoverage` | Exit 80 |
| T-P1-19 | `-Bootstrap -FaultInjection GateFailure` (script level) | Exit 90; `ci.log` kept; `gate-result.json` shows `current` 57/33 and a failed Minimum rule |
| T-P1-19 | `-FaultInjection IncomparableTarget` (two-pass mode) | Exit 85; diagnostic names the `dotnet-coverage` version field and both values |
| T-P1-08 | `-FaultInjection ConsumerLockDrift` | Exit 70; diagnostic names the changed dependency |
| T-P1-08 | Locked consumer restore after the `contentHash` rewrite | Only that one value differs; all other consumer dependencies resolve exactly as locked |
| T-P1-19 | `-TargetRef` equal to `-Commit` without `-Bootstrap` | Exit 20 |
| T-P1-21 | Consumer-restored package hash equals `package-hash.txt` | Equal; a mismatch exits 70 |
| T-P1-21 | Consumer tests pass against the package built in this run | Exit 0 for the consumer stage |
| T-P1-22 | `.nupkg` entry check; consumer has no `ProjectReference` | Only the listed entries; otherwise exit 60 |


## 7. Evidence to Record

`docs/p1/P1_09_LOCAL_CI.md` records:
- The evidence fields below.
- Every limitation from section 8 that still applies.
- The script commit that was verified.

Evidence fields:
- Commit and target SHAs.
- SDK, pwsh, dotnet-coverage and Microsoft.CodeCoverage versions.
- Test counts per project.
- Build warning count.
- Package SHA-512 (base64).
- The `.nupkg` entry list.
- Gate counters and rule verdicts.
- Exit code of each injected fault.
- Run durations.

Updates: the P1.09 row and T-P1-08 status in `PHASE_P1_PLAN.md`; phase status in `DEVELOPMENT_PLAN.md`; procedure in `README.md`; pwsh as a CI prerequisite in `DEPENDENCIES.md`.

## 8. Risks and Limitations

- Clean restore depends on network access to nuget.org; an outage is reported as exit 30 and is not a code defect.
- `git clone --no-local` tests the committed state only; uncommitted or unpushed changes are deliberately excluded.
- The two-pass baseline roughly doubles the run time.
- Target commits that predate the runner cannot produce a baseline (exit 91 or an English diagnostic). Use `-Bootstrap` only for the genuine initial run, or `-BaselinePath`.
- PowerShell 7 must be installed on Debian for P1.10.
- A local run is not a hosted GitHub run, and Windows x64 results are not ARM64 evidence.
- If packing is proven deterministic, the `contentHash` rewrite becomes a no-op; the locked restore and the hash check stay.
- Settings comparison covers only what `collection-settings.json` records; a relevant setting that is not recorded would go unnoticed. Extend the record when collection settings are added (for example the P1.10 run settings).

## 9. Acceptance Criteria

- [ ] Decisions a–e are recorded with the user's choice.
- [ ] A run of the verified commit from a fresh clone exits 0, and its run directory contains every file listed in section 4.
- [ ] Each `-FaultInjection` value and the TargetRef=Commit case exit with the code in section 6, and the run directory and `ci.log` are kept.
- [ ] `-Bootstrap -FaultInjection GateFailure` exits 90 at script level and keeps `ci.log` and `gate-result.json`.
- [ ] A two-pass run writes `collection-settings.json` for both passes; `IncomparableTarget` exits 85.
- [ ] The current and target passes write to separate directories, and the gate always evaluates the current pass's report.
- [ ] The consumer restore runs in locked mode; the only lock-file change is the `WeatherParser.Parsing` `contentHash`, and the restored package hash equals the hash of the package packed in the same run.
- [ ] The genuine initial run is identified once, with a passing full pipeline, justification, `current/bootstrap-baseline.json` and settings/provenance; an existing accepted baseline is reused instead of re-bootstrap.
- [ ] The `.nupkg` contains only the allowed entries.
- [ ] No step uses the global `dotnet-coverage`, a production secret, or the developer working tree.
- [ ] `docs/p1/P1_09_LOCAL_CI.md` and the documents named in section 7 are updated.
