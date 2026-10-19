# P1.10 – Verify Debian/ARM64 Package Foundations: Implementation Plan

**Status: plan only (2026-10-19); nothing is implemented or verified yet.** Drafted with the local qwen3-coder:30b model over three review rounds and finalized by review against [AGENTS.md](../../AGENTS.md), [REQUIREMENTS.md](../../REQUIREMENTS.md) (REQ-004, REQ-005, REQ-017, REQ-084), the [P1 detailed plan](../../PHASE_P1_PLAN.md) and the [P1.09 plan](P1_09_LOCAL_CI_PLAN.md), whose script this package reuses. Covers T-P1-20.

## 1. Goal and Scope

Produce two kinds of evidence for the package foundation, and keep them strictly apart:

- **Class A, build and restore evidence:** collected on the Windows developer machine. It is never runtime evidence (REQ-005).
- **Class B, runtime evidence:** collected on a Debian 13 ARM64 target chosen in decision a, preferably the NanoPi R76S. The P1.09 CI sequence runs the tests, builds the package and runs the package consumer there.

Unperformed checks remain explicitly open (REQ-084).

In scope:
- Class A: RID-specific restore and build for `linux-arm64`, inspection of the package's assets and dependencies.
- Class A: a Windows comparison of static and dynamic coverage collection.
- Class B: locked restore, build, offline tests, pack and package-consumer tests on the target, using the P1.09 script.
- Class B: coverage collection with static instrumentation, the coverage gate and one injected failure.
- A compatibility matrix (REQ-017).

Out of scope:
- Any target action before the user's authorization (decision b); system-wide changes beyond that authorization.
- Serializer, timezone and DI-under-load checks: P2 and later, once those components exist.
- CLI behaviour (P3); network sources and MQTT (P4–P8); REQ-084 release checks and full hardware acceptance (P9).
- Emulation or Debian x64 runs counted as ARM64 evidence.

## 2. Preconditions and Inputs

- **Hard dependency:** P1.08 closure and P1.09 are implemented and accepted on Windows. P1.10 starts after them.
- The P1.09 script needs these additions for this package (section 4):
  - **`-RunSettings <path>`.** The path is resolved to an absolute path relative to the directory the script was started from, never inside a clone. At setup the script copies the file once to `<run>/coverage.runsettings` and records its SHA-256. Both passes then use `--settings <absolute path of that copy>`, so a target commit that predates `coverage.runsettings` is measured with exactly the same settings. Nothing is written into either clone, so both checkouts stay clean. Before each pass the script re-hashes the copy; a changed hash exits 85. `collection-settings.json` records that hash for both passes.
  - **`-CollectOnly`.** Runs every verification stage except coverage conversion, baseline writing and gate. That means P1.09 steps 1–6 for the current pass (including pack, package-content check and package-consumer tests against the package built on the target), then raw collection for the target pass (steps 2–4) and the changed-file list. It writes `collection-settings.json` for both passes, with the converter fields marked as not yet run. Any stage failure exits with its normal P1.09 code. On success it exits 0 with "collection complete, gate pending" in `ci.log`; a run that stops there is never reported as a passed gate.
  - **Resume package.** Before `-CollectOnly` exits, it writes everything a later resume needs into `<run>/resume/`, so resuming does not depend on the ARM64 work directory, which is deleted as usual:
    - `runner/`: `dotnet publish tools/WeatherParser.Coverage.Runner -c Release --no-self-contained` output from the current clone. This is framework-dependent and RID-neutral IL, so it runs on any machine with the .NET 10 runtime.
    - `dotnet-tools.json`: a copy of the current clone's tool manifest with the pinned `dotnet-coverage` version.
    - `resume.json`: commit and target SHAs, SHA-256 of every file in `runner/` and of `dotnet-tools.json`, SHA-256 of the `ci-local.ps1` that collected, the exclusions and tool names/versions of both passes, and the path of `changed-files.txt`. The changed-file list is generated during collection, before any conversion.
  - The full (non-collect) run also writes `<run>/resume/`, so every run directory is self-contained.
  - **`-ResumeFrom <run directory>`.** Used on a machine where the pinned `dotnet-coverage` works, with a checkout of the same commit.
    - It first verifies every hash in `resume.json`, including that its own `ci-local.ps1` matches the collecting script; any mismatch exits 20.
    - It restores the pinned tool from the copied manifest (`dotnet tool restore --tool-manifest <run>/resume/dotnet-tools.json`), converts the raw reports of both passes with that one converter, and records its version and host in both settings files.
    - It then compares the settings, writes the baseline with `dotnet <run>/resume/runner/WeatherParser.Coverage.Runner.dll write-baseline …` from the target pass's own values, and runs `evaluate` with the same published runner and the recorded `changed-files.txt`, exactly as P1.09 steps 8–9 define. It exits with the normal P1.09 codes.
    - It never reads the ARM64 work directory, a clone or a locally built runner.
- Library state at `2f8c670`: 57/63 = 90.48% blocks on Windows; the package depends only on `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.0.
- **Coverage on Linux ARM64:** Microsoft.CodeCoverage supports static managed instrumentation on Linux ARM64, but not its dynamic profiler (P1.02 review).
- **Unknown:**
  - Whether an authorized Debian 13 ARM64 target exists.
  - Whether `pwsh`, the .NET SDK 10.0.402 and `dotnet-coverage` run there.
  - The R76S's current OS, network reachability and access.

## 3. Decisions Required from the User

a) **Target environment.** Options:
   - (1) The NanoPi R76S with Debian 13 ARM64.
   - (2) Another Debian 13 ARM64 machine (board or ARM64 cloud VM).
   - (3) QEMU emulation.
   - (4) Debian 13 x64 (e.g. a WSL Debian distribution) for the OS dimension only.

   **Recommended: (1), else (2).** Options (3) and (4) never count as ARM64 runtime evidence (REQ-005).

b) **Authorization scope.** Which actions on the target the user authorizes:
   - SSH login.
   - Installing git, curl, ca-certificates and libicu with the system package manager, if they are missing.
   - User-directory installs of PowerShell 7 and the .NET SDK.
   - A working directory.
   - Copying results back.
   - Cleanup afterwards.

   It must be recorded before the first target action (AGENTS.md).

c) **Repository transfer.** Options: `git clone` from GitHub (may need credentials on the device), or a `git bundle` copied from the developer machine. **Recommended: bundle.** It needs no credentials on the device and carries exact refs.

d) **.NET SDK installation.** Options: `dotnet-install.sh --version 10.0.402` into a user directory, or apt packages. **Recommended: `dotnet-install.sh`.** The exact patch is required by `global.json` (`rollForward: disable`), and apt may not offer it. Verify the method against Microsoft documentation at implementation time.

e) **PowerShell 7.** Options: install into a user directory from Microsoft's ARM64 release archive, or do not use PowerShell on the target. **Recommended: install.** The second option would contradict the P1.09 single-script decision. Verify Microsoft's supported method for Debian 13 ARM64 at implementation time.

f) **Coverage conversion location.** Options: on the target, if `dotnet-coverage` runs on `linux-arm64`, or on Windows from the copied raw `.coverage` files. **Recommended: try the target first, fall back to Windows, and record which one ran.**

g) **Compatibility matrix location.** Options: a new `docs/COMPATIBILITY.md`, or a section in `DEPENDENCIES.md`. **Recommended: `docs/COMPATIBILITY.md`.**

h) **Coverage improvement for this package (step 3a).** Options:
   - (1) Cover all six remaining blocks. Library coverage becomes exactly 100%, and from then on every code PR must also stay at exactly 100% (exact-100% rule).
   - (2) Cover only the behaviours worth specifying (for example invalid schemes), which is still a strict increase below 100%.
   - (3) Add no tests and accept the expected gate failure (exit 90) for this package. This completes P1.10 with the status EXPECTED-COMPARISON-FAILURE, but it does **not** satisfy the passing coverage gate that P1 acceptance requires (P1.12 plan, section 4). P1 then stays NOT_READY until coverage strictly improves through meaningful tests or a separately authorized policy change.

   **Recommended: (2).** It improves coverage through meaningful tests without silently committing the project to the exact-100% regime; choose (1) only deliberately.

## 4. Artifacts

| Path | New/Changed | Responsibility |
|---|---|---|
| `coverage.runsettings` | New | Microsoft.CodeCoverage settings that enable static managed instrumentation and disable dynamic instrumentation. Take the element names from the official Microsoft.CodeCoverage documentation; never guess them |
| `scripts/ci-local.ps1` | Changed | New `-RunSettings`, `-CollectOnly` and `-ResumeFrom` parameters and the self-contained `<run>/resume/` package (section 2), with fault-injection checks for a changed settings hash (exit 85) and a tampered resume package (exit 20) |
| `scripts/prepare-debian-arm64.sh` | New, only if decisions d/e need a repeatable installer | Checks prerequisites and installs PowerShell 7 and the .NET SDK into user directories; never runs without authorization b |
| `docs/COMPATIBILITY.md` | New (decision g) | Compatibility matrix, see below |
| `docs/p1/P1_10_DEBIAN_ARM64.md` | New | Result record |
| `README.md`, `PHASE_P1_PLAN.md`, `DEVELOPMENT_PLAN.md`, `DEPENDENCIES.md` | Changed | Target procedure, P1.10 row, phase status, run settings and target prerequisites |

**Compatibility matrix.** Columns: Component | Requested support (REQ-004/REQ-017) | Official vendor support (source URL, date checked) | Project evidence (Class A or Class B with record link, or OPEN). Rows:
- .NET SDK 10.0.402 (pinned by `global.json`).
- .NET runtime, recorded with its actual version from `dotnet --list-runtimes` (the SDK version is not a runtime version).
- Debian 13 Trixie.
- `linux-arm64` (target hardware).
- Windows x64 (developer machine).
- Microsoft.CodeCoverage static instrumentation on `linux-arm64`.
- Microsoft.CodeCoverage dynamic instrumentation on `linux-arm64` (vendor: unsupported).
- `dotnet-coverage` on `linux-arm64`.
- PowerShell 7 on Debian ARM64.
- MSTest/VSTest on `linux-arm64`.
- `linux-x64` (Debian or WSL), only if used.

## 5. Procedure

### Class A – build/restore evidence on Windows (not runtime evidence)

1. **RID restore and build.** Run `dotnet restore src/WeatherParser.Parsing -r linux-arm64 --locked-mode` and `dotnet build src/WeatherParser.Parsing -c Release -r linux-arm64 --no-restore`. If the locked restore fails because the lock file lacks a `linux-arm64` section, record that as a finding. Decide with the user whether to add `RuntimeIdentifiers`; never drop locked mode silently.
2. **Package inspection.** From a P1.09 run's `.nupkg`, confirm that the only assembly is `lib/net10.0/WeatherParser.Parsing.dll` and that there are no `runtimes/` or native assets. Record the nuspec dependency list. Check on nuget.org that `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.0 has no native assets.
3. **Static vs. dynamic collection (Windows, neither class).** Run `tests/WeatherParser.Parsing.Tests` once with default collection and once with `--settings coverage.runsettings`, then convert both with the pinned tool and compare the `WeatherParser.Parsing.dll` block counters.
   - **If they are identical:** use `coverage.runsettings` on all platforms (recommended), so every platform collects with the same settings.
   - **If they differ:** record a finding and revisit the coverage contract with the user before `coverage.runsettings` is adopted on Windows.
   - Either way, this comparison is Windows-only evidence. It does not prove that Windows and ARM64 counters are comparable. Windows-versus-ARM64 counters are reported descriptively only, and gating always uses a baseline measured on the same target.

### Coverage change of this package (decision h)

3a. **Gate expectation.** This package changes scripts and coverage configuration, so the gate classifies it as a code change. If the current and target passes both measure 57/63, the evaluator correctly rejects equality below 100%.
   - **Recommended:** before the target run, add meaningful behaviour tests, test-first, for the remaining uncovered library blocks: invalid URI schemes and null arguments in `EndpointUriValidator.IsValid` (12/16) and the null-argument guards of the `ParserEndpointValidator` constructor (3/5). Commit them, so the current commit strictly improves on the target.
   - **Otherwise:** the run is expected to exit 90 at the Comparison rule; record that as the expected result, not as a platform defect.
   - See decision h for what covering all six blocks implies.

### Class B – runtime evidence on the Debian 13 ARM64 target (requires decisions a = 1 or 2, and b)

4. **Record the authorization** (decision b) in the result record before the first target action.
5. **Target preparation.**
   - Check, or install within the authorization: git, curl, ca-certificates, libicu (record the version), PowerShell 7 and the .NET SDK 10.0.402 in user directories, all on `PATH` for the session.
   - Set `DOTNET_CLI_TELEMETRY_OPTOUT=1` and `TESTINGPLATFORM_TELEMETRY_OPTOUT=1`.
   - Verify network access to nuget.org.
   - Record (URL and date) that Debian 13 is on Microsoft's current .NET 10 supported-OS list, and check the official .NET 10 Linux dependency list.
6. **Transfer.** On Windows, run `git bundle create weatherparser.bundle features/develop/initial <targetBranch>` (branch refs, never a bare SHA), then `git bundle verify weatherparser.bundle`. Copy the bundle. On the target, run `git clone weatherparser.bundle weatherparser`; the bundle's branches then appear as `origin/<branch>`.
7. **Run.** In the clone, run `pwsh ./scripts/ci-local.ps1 -Commit <commit current at P1.10> -TargetRef origin/<targetBranch> -RunSettings <absolute path to coverage.runsettings>`.
   - Run in two-pass mode, so the baseline is measured on the same target with identical recorded settings.
   - `<targetBranch>` must point to a commit that already contains the runner and the pinned tool manifest, for example the accepted P1.08 closure and P1.09 commit.
   - **Never use `-Bootstrap` here.** Missing tooling on an older target is not a genuine initial measurement: `-Bootstrap` is reserved for the project's first measurement (P1.09). If no comparable target commit exists, the comparison is recorded as OPEN, and the current pass's coverage is reported descriptively only, without claiming a gate result.
8. **Failure propagation on ARM64.** Run the same command again with `-FaultInjection FailingTest`; expect exit 50.
9. **Coverage conversion (decision f).** If the step 7 run exited 80 because `dotnet-coverage` does not run on `linux-arm64`, record that as a finding. Then:
   - Rerun on the target with `-CollectOnly`. This performs every non-conversion stage on ARM64, including the package-consumer tests, writes `<run>/resume/` and exits 0 with the gate pending. The work directory is deleted as usual.
   - Copy the whole run directory back to Windows.
   - Run `pwsh ./scripts/ci-local.ps1 -ResumeFrom <copied run directory>` there. It converts both raw reports with the pinned tool, writes the baseline and runs the gate.
   - Record that collection ran on ARM64 and conversion and gate on Windows.
10. **Results back.** Copy each complete run directory to `<repo>/artifacts/ci/arm64-<commit-short>-<timestamp>/` (scp or tarball, within authorization b). Clean up the target working directory unless the user wants it kept.
11. **Record.** Write `docs/p1/P1_10_DEBIAN_ARM64.md` (section 7) and `docs/COMPATIBILITY.md`.

If no authorized ARM64 target is available, perform steps 1–3 and 11 only. Mark every Class B row as OPEN with the reason. T-P1-20 runtime success is then not claimed, and P1.12 decides how P1 acceptance treats the open item.

## 6. Tests and Test IDs

| Test ID | Class | Evidence | Expected result |
|---|---|---|---|
| T-P1-20 | A | RID-specific locked restore and build for `linux-arm64` | Succeeds, or the lock-file finding is recorded |
| T-P1-20 | A | `.nupkg` assets and nuspec dependencies | Only `lib/net10.0` assembly; only DI Abstractions 10.0.0 |
| — | Windows | Static vs. dynamic block counters | Identical, or a recorded finding |
| T-P1-20 | B | P1.09 run on the target with `-RunSettings` (or `-CollectOnly`) | Every stage before the gate succeeds, including pack, package-content check and the package consumer against the package built on the target; the gate outcome is classified separately (status table below) |
| T-P1-20 | B | Static-instrumentation coverage on ARM64 | Native block counters for `WeatherParser.Parsing.dll` present; compared with the Windows counters |
| T-P1-20 | B | Coverage gate, two-pass on the target (or `-CollectOnly` + `-ResumeFrom`) | Passes with decision h = (1) or (2); exit 90 at Comparison with h = (3), recorded as expected; OPEN if no comparable target commit exists |
| T-P1-19 | B | Settings copy hash changed between passes (fault injection) | Exit 85; both checkouts clean |
| T-P1-20 | B | Target commit without `coverage.runsettings` measured through the run-directory copy | Both passes record the same settings hash |
| T-P1-19 | B | `-FaultInjection FailingTest` on the target | Exit 50, with the run directory kept |
| T-P1-20 | Windows | `-ResumeFrom` on a run directory whose ARM64 work directory and clones were deleted | Resume completes from `<run>/resume/` alone; hashes verified; the gate result is written |
| T-P1-19 | Windows | `-ResumeFrom` after changing one file in `<run>/resume/runner/` | Exit 20; hash mismatch named |

**Gate evidence status.** The record classifies the gate outcome as exactly one of:

| Status | Condition | Evidence required |
|---|---|---|
| PASSED | Gate exit 0 (full run, or `-ResumeFrom`) | `gate-result.json` with every rule passed |
| EXPECTED-COMPARISON-FAILURE | Decision h = (3), and gate exit 90 with only the `Comparison` rule failed (equality below 100%) | `gate-result.json` showing equal counters for both passes and every other rule passed. Valid for P1.10; it blocks P1 acceptance (decision h) |
| OPEN | No comparable target commit exists (step 7), or Class B could not run | Reason recorded; current-pass counters reported descriptively, without a gate claim |

Any other gate failure is a defect, not one of these statuses.

## 7. Evidence to Record

`docs/p1/P1_10_DEBIAN_ARM64.md` records:
- The authorization and its date.
- Class A results.
- The static/dynamic comparison.
- The target environment.
- The exit codes of both target runs.
- Test counts per project.
- Package SHA-512 built on the target.
- Coverage counters on ARM64 next to the Windows counters.
- The gate result, and where conversion and gate ran.
- Paths of the copied run directories.

Target environment fields:
- `/etc/os-release`, `uname -m`, `/proc/device-tree/model` (if present).
- `dotnet --info` (RID `linux-arm64`), `dotnet --list-runtimes` (actual runtime version, recorded separately from the SDK version), `pwsh --version`, libicu version.
- Commit and target SHAs.

The record ends with an explicit list of checks not performed: serializers, timezones, DI under load, CLI, MQTT and network sources (later phases), and REQ-084 release checks (P9).

Update the P1.10 row in `PHASE_P1_PLAN.md`, the phase status in `DEVELOPMENT_PLAN.md`, the target procedure in `README.md`, and the run settings and target prerequisites in `DEPENDENCIES.md`.

## 8. Risks and Limitations

- No target may be available or authorized. Class B then stays OPEN, and it must not be replaced by emulation or x64 results.
- Static and dynamic instrumentation may count blocks differently on Windows (step 3). Windows-versus-ARM64 counters are descriptive only; they are never used for gating.
- The fallback splits one measurement across two machines. Converting both passes with the same pinned converter, and recording the converter's host and version in both settings files, keeps them comparable.
- `dotnet-coverage` may not run on `linux-arm64`; step 9 is the fallback.
- RID-specific locked restore may need lock-file changes (step 1).
- The target restore depends on network access to nuget.org.
- Bundle refs appear as `origin/<branch>` in the target clone, so `-TargetRef` must use that form.
- Class B proves the package foundation on ARM64 at P1 scope only, not release readiness (P9).

## 9. Acceptance Criteria

- [ ] Decisions a–h are recorded; the authorization is recorded before the first target action.
- [ ] Class A evidence is recorded: RID restore/build result, package entry list, dependency list.
- [ ] The static-vs-dynamic comparison is recorded, with the resulting run-settings decision.
- [ ] Class B: every stage before the gate succeeds on Debian 13 ARM64, including the package consumer against the package built on the target. This holds in a full run, or via `-CollectOnly` exiting 0 on the target.
- [ ] `-FaultInjection FailingTest` exits 50 on the target, and the section 7 environment fields are recorded.
- [ ] The gate outcome is recorded with exactly one status from section 6: PASSED (expected with h = 1 or 2), EXPECTED-COMPARISON-FAILURE (only with h = 3, with its evidence), or OPEN (with its reason). If Class B could not run at all, it is recorded as OPEN with its reason.
- [ ] If h = (3), the result explicitly names the failed P1 passing-gate criterion and the P1.12 NOT_READY consequence. Completing the P1.10 evidence record is not acceptance of P1.
- [ ] If the fallback is used: `-ResumeFrom` completes from `<run>/resume/` after the ARM64 work directory was deleted, and the tampered-runner check exits 20.
- [ ] No `-Bootstrap` is used on the target; the gate uses a baseline measured on the same target.
- [ ] ARM64 block counters and the gate result are recorded, including where conversion and gate ran.
- [ ] `docs/COMPATIBILITY.md` contains every matrix row of section 4, each with source and evidence status.
- [ ] The documents in section 7 are updated.
