# Contributing to WeatherParser

This guide covers all developer procedures for restoring, building, packing, testing, and evaluating coverage in the WeatherParser repository. It is the single source of truth for development workflows, with each item tagged by platform (Windows, Debian/ARM64, or any) and status. README.md holds only the library purpose, current status summary, quickstart, and links; contributor rules are in AGENTS.md; requirements in REQUIREMENTS.md; versions and licenses in DEPENDENCIES.md; platform matrix in docs/COMPATIBILITY.md.

## Status values

- **Verified** — the command passed against a named revision. On Windows this is the P1.11 walkthrough or a package record that ran exactly that command on the current project structure. On Debian/ARM64 it is the recorded P1.10 target execution.
- **Planned** — the package or walkthrough that will verify it is named.
- **OPEN** — required but not achievable yet; the reason is given.

## 1. Prerequisites

### SDK selection via `global.json` (rollForward disable)

*Status: Verified (Windows) – [P1.11 walkthrough](docs/p1/P1_11_DEVELOPER_INSTRUCTIONS.md); historical: [P1.03](docs/p1/P1_03_BUILD_SETUP.md)*

```powershell
dotnet --version
```

### Git (clone, checkout)

*Status: Verified (Windows, Debian/ARM64) – [P1.09 record](docs/p1/P1_09_LOCAL_CI.md), [P1.10 record](docs/p1/P1_10_DEBIAN_ARM64.md)*

```bash
git clone <repository URL>
cd WeatherParser
```

### PowerShell 7

*Status: Verified (Windows, Debian/ARM64) – [P1.09 record](docs/p1/P1_09_LOCAL_CI.md), [P1.10 record](docs/p1/P1_10_DEBIAN_ARM64.md)*

```powershell
pwsh --version
```

### `dotnet tool restore` for the pinned `dotnet-coverage`

*Status: Verified (Windows) – [P1.11 walkthrough](docs/p1/P1_11_DEVELOPER_INSTRUCTIONS.md); historical: [P1.08](docs/p1/P1_08_COVERAGE_EVALUATION.md)*

```powershell
dotnet tool restore
```

### Telemetry opt-out variables

*Status: Verified (Windows, Debian/ARM64) – [P1.09 record](docs/p1/P1_09_LOCAL_CI.md), [P1.10 record](docs/p1/P1_10_DEBIAN_ARM64.md); historical: [P1.02](docs/p1/P1_02_TOOLING_REVIEW.md)*

```powershell
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:TESTINGPLATFORM_TELEMETRY_OPTOUT = '1'
```

```bash
export DOTNET_CLI_TELEMETRY_OPTOUT=1 TESTINGPLATFORM_TELEMETRY_OPTOUT=1
```

## 2. Restore and build

### Restore and Release build of the solution filter

*Status: Verified (Windows) – [P1.11 walkthrough](docs/p1/P1_11_DEVELOPER_INSTRUCTIONS.md); historical: [P1.03](docs/p1/P1_03_BUILD_SETUP.md)*

```powershell
New-Item -ItemType Directory -Force artifacts/local-nuget
dotnet restore WeatherParser.Ci.slnf --locked-mode
dotnet build WeatherParser.Ci.slnf --configuration Release --no-restore
```

`NuGet.Config` lists the project-local package source `artifacts/local-nuget`, which is gitignored and missing in a fresh clone; restores fail with NU1301 until it exists. `WeatherParser.Ci.slnf` contains every project except the package consumer. Do not restore or build the whole `WeatherParser.slnx` from the command line: the committed consumer lock is stale by design (the package embeds the commit ID), so it fails with NU1403; build the consumer as described in [section 4](#4-pack-and-package-consumer).

Lock files are tracked. For a requested package update, resolve once without `--locked-mode` and review the lock changes.

## 3. Tests

### Offline test runs of the three source-reference test projects

*Status: Verified (Windows) – [P1.11 walkthrough](docs/p1/P1_11_DEVELOPER_INSTRUCTIONS.md); historical: [P1.05](docs/p1/P1_05_TDD_BASELINE.md), [P1.06](docs/p1/P1_06_DI_CONTRACTS.md), [P1.08](docs/p1/P1_08_COVERAGE_EVALUATION.md)*

```powershell
dotnet test tests/WeatherParser.Parsing.Tests --configuration Release --no-build
dotnet test tests/WeatherParser.Coverage.Tests --configuration Release --no-build
dotnet test tests/WeatherParser.Coverage.Runner.Tests --configuration Release --no-build
```

Test conventions (MSTest, Moq, FluentAssertions, TDD) are defined in [AGENTS.md](AGENTS.md#tests).

## 4. Pack and package consumer

### Local feed `artifacts/local-nuget`, isolated consumer with a versioned PackageReference

*Status: Verified (Windows) – [P1.11 walkthrough](docs/p1/P1_11_DEVELOPER_INSTRUCTIONS.md); historical: [P1.04](docs/p1/P1_04_PACKAGE_CONSUMER.md)*

Pack the library into the local feed:

```powershell
dotnet pack src/WeatherParser.Parsing/WeatherParser.Parsing.csproj --configuration Release --no-restore --output artifacts/local-nuget
```

The package consumer `tests/WeatherParser.Package.Tests` references the packed library from the local feed `artifacts/local-nuget` with a versioned `PackageReference` and no `ProjectReference`. After section 2, run it with the command below. A freshly packed library usually needs the refresh below first, because the committed consumer lock is stale by design.

```powershell
dotnet test tests/WeatherParser.Package.Tests/WeatherParser.Package.Tests.csproj --configuration Release --no-restore
```

### Stale cached package / consumer lock

*Status: Verified (Windows) – [P1.11 walkthrough](docs/p1/P1_11_DEVELOPER_INSTRUCTIONS.md)*

The exact local, uncommitted refresh procedure:

```powershell
# Package check: entries, README/LICENSE content and the packed hash
$package = 'artifacts/local-nuget/WeatherParser.Parsing.0.1.0-initial.nupkg'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path $package).Path)
$zip.Entries.FullName   # only lib/net10.0/WeatherParser.Parsing.dll, README.md, LICENSE and NuGet metadata
foreach ($name in 'README.md', 'LICENSE') {
    $entry = $zip.GetEntry($name).Open(); $buffer = [IO.MemoryStream]::new(); $entry.CopyTo($buffer); $entry.Dispose()
    $packedSha = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($buffer.ToArray()))
    $sourceSha = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes((Resolve-Path $name).Path)))
    if ($packedSha -cne $sourceSha) { throw "$name in the package is not byte-identical to the working tree ($packedSha, $sourceSha)" }
}
$zip.Dispose()
$packedHash = [Convert]::ToBase64String([Security.Cryptography.SHA512]::HashData([IO.File]::ReadAllBytes((Resolve-Path $package).Path)))
# Remove the stale cache entry
$globalPackages = (dotnet nuget locals global-packages --list) -replace '^global-packages:\s*', ''
Remove-Item -Recurse -Force -ErrorAction SilentlyContinue (Join-Path $globalPackages 'weatherparser.parsing/0.1.0-initial')
# Refresh the consumer lock; the diff may change only the WeatherParser.Parsing contentHash
dotnet restore tests/WeatherParser.Package.Tests --force-evaluate
git diff tests/WeatherParser.Package.Tests/packages.lock.json
# Locked restore and hash comparison
dotnet restore tests/WeatherParser.Package.Tests --locked-mode
$restoredHash = Get-Content (Join-Path $globalPackages 'weatherparser.parsing/0.1.0-initial/weatherparser.parsing.0.1.0-initial.nupkg.sha512')
if ($restoredHash -ne $packedHash) { throw 'The restored package hash differs from the packed package' }
# Run the consumer tests
dotnet test tests/WeatherParser.Package.Tests --configuration Release --no-restore
# Discard the refresh with git restore
git restore tests/WeatherParser.Package.Tests/packages.lock.json
```

This is required because the package embeds the commit ID, so a committed consumer lock can never match the package built from its own commit.

### README changes

*Status: Verified (Windows) – [P1.11 walkthrough](docs/p1/P1_11_DEVELOPER_INSTRUCTIONS.md)*

README.md is the packed package readme (`PackageReadmeFile`). Check the package locally with the procedure above, commit only the Markdown files (without the consumer lock), then run `pwsh ./scripts/ci-local.ps1 -TargetRef <target branch> -RunSettings coverage.runsettings` on the committed revision. A Markdown-only commit is documentation-only for the coverage gate (equal coverage passes, a decrease fails).

### Automated prevention

*Status: Verified (Windows, Debian/ARM64) – [P1.09 record](docs/p1/P1_09_LOCAL_CI.md), [P1.10 record](docs/p1/P1_10_DEBIAN_ARM64.md)*

The CI script's isolated `NUGET_PACKAGES`/`DOTNET_CLI_HOME` and contentHash rewrite prevent stale package issues.

## 5. Coverage

### Collection, conversion, runner evaluate/write-baseline

*Status: Verified (Windows) – [P1.11 walkthrough](docs/p1/P1_11_DEVELOPER_INSTRUCTIONS.md); historical: [P1.07](docs/p1/P1_07_COVERAGE_CONTRACT.md), [P1.08](docs/p1/P1_08_COVERAGE_EVALUATION.md)*

```powershell
$results = Join-Path TestResults "product-$([DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))"
dotnet test tests/WeatherParser.Parsing.Tests --configuration Release --no-build --collect "Code Coverage" --settings coverage.runsettings --results-directory $results
$raw = @(Get-ChildItem $results -Recurse -Filter *.coverage)
if ($raw.Count -ne 1) { throw "Expected exactly one .coverage file in $results, found $($raw.Count)" }
dotnet tool run dotnet-coverage merge $raw[0].FullName -f xml -o "$results/coverage.xml"
git diff --name-only <target commit sha>...HEAD > "$results/changed-files.txt"
dotnet run --project tools/WeatherParser.Coverage.Runner --configuration Release --no-build -- write-baseline --report <target coverage.xml> --commit <target commit sha> --tool-name Microsoft.CodeCoverage --tool-version 18.11.0 --exclude *.Tests.dll --exclude WeatherParser.Coverage*.dll --output "$results/baseline.json"
dotnet run --project tools/WeatherParser.Coverage.Runner --configuration Release --no-build -- evaluate --report "$results/coverage.xml" --changed-files "$results/changed-files.txt" --tool-name Microsoft.CodeCoverage --tool-version 18.11.0 --exclude *.Tests.dll --exclude WeatherParser.Coverage*.dll --baseline "$results/baseline.json" --expected-baseline-commit <target commit sha>
```

Each run writes to a new, timestamped results directory under the gitignored `TestResults/`, so an older report is never evaluated. JSON result on stdout; runner exit codes: 0 (all rules passed), 1 (a coverage rule failed), 2 (usage or input error). Gate rules:
- Minimum 80% block coverage.
- Below a 100% baseline, a code change must strictly improve.
- A Markdown-only change may stay equal.
- Decreases always fail.
- An exact 100% baseline accepts exactly 100%.

The binding coverage contract is [PHASE_P1_PLAN.md section 5](PHASE_P1_PLAN.md).

`--bootstrap` is reserved for the project's genuine first measurement, which was taken on `0a2dabe` ([P1.09 record](docs/p1/P1_09_LOCAL_CI.md)); later runs always use a baseline.

### Two-pass baseline and collection-settings comparison

*Status: Verified (Windows, Debian/ARM64) – [P1.09 record](docs/p1/P1_09_LOCAL_CI.md), [P1.10 record](docs/p1/P1_10_DEBIAN_ARM64.md)*

The CI script measures the target commit in a second pass with identical, recorded collection settings and compares them before the gate; see [section 6](#6-local-ci).

## 6. Local CI

### Purpose and isolation

*Status: Verified (Windows) – [P1.09 record](docs/p1/P1_09_LOCAL_CI.md); Verified (Windows, Debian/ARM64) for the P1.10 parameters – [P1.10 record](docs/p1/P1_10_DEBIAN_ARM64.md)*

The `scripts/ci-local.ps1` script runs a full local CI pipeline in an isolated environment, including SDK check, locked restore, Release build, tests, pack, package-content check, package consumer, coverage conversion, two-pass baseline from `-TargetRef` with collection-settings comparison, and gate. It tests only the committed state from a fresh clone in a temporary directory, with isolated `NUGET_PACKAGES` and `DOTNET_CLI_HOME`, and never touches the working tree.

### Basic command

```powershell
pwsh ./scripts/ci-local.ps1 -TargetRef <comparison commit or branch> -RunSettings coverage.runsettings
```

### Parameters

| Parameter | Meaning |
|---|---|
| `-Commit` | The commit to run CI on (default HEAD) |
| `-TargetRef` | Required unless `-ResumeFrom`; the reference commit or branch for comparison |
| `-BaselinePath` | External baseline JSON with its sibling `collection-settings.json`; skips the target pass |
| `-Bootstrap` | For the genuine first measurement only |
| `-RunSettings <file>` | Copied once into the run, hash-checked before each pass |
| `-CollectOnly` | Every stage except conversion, baseline and gate; writes the resume package; exit 0 with "collection complete, gate pending" |
| `-ResumeFrom <run directory>` | Alone; verifies the resume package hashes, converts both passes with the pinned tool, writes the baseline and runs the gate with the published runner |
| `-KeepWorkDir` | Do not delete the temporary work directory |
| `-FaultInjection` | Verification only (SdkMismatch, LockDrift, FailingTest, MissingCoverage, ConsumerLockDrift, IncomparableTarget, GateFailure, RunSettingsTamper) |

### Exit codes

| Code | Meaning |
|---|---|
| 0 | Success |
| 10 | Environment/SDK mismatch (`dotnet --version` in the clone differs from `global.json`) |
| 20 | Setup/validation (clone, refs, parameters, resume-package hashes) |
| 30 | Locked restore or lock drift |
| 40 | Build |
| 50 | Tests |
| 60 | Pack or package content |
| 70 | Package consumer |
| 80 | Coverage collection or conversion |
| 85 | Incomparable measurements (collection settings or run-settings copy differ) |
| 90 | Gate rule failed |
| 91 | Gate input invalid |

### Run directory contents

The run directory `artifacts/ci/<commit>-<UTC timestamp>/` contains `ci.log`, `commits.json`, test results, package and hash, coverage reports, `collection-settings.json` per pass, `changed-files.txt`, `gate-result.json`, `resume/`.

### Hosted CI (GitHub Actions)
*Status: Planned – first hosted run pending*

The repository includes a GitHub Actions workflow `.github/workflows/ci.yml` that runs the same `scripts/ci-local.ps1` script as documented in CONTRIBUTING.md section 6, on Windows x64 (`windows-latest`), Ubuntu x64 (`ubuntu-latest`), and Debian 13 ARM64 (`debian:trixie` container on `ubuntu-24.04-arm`) runners.

For pull requests, the workflow compares the PR head commit against the PR base commit; for manual runs, a deliberate target is specified via the `target_ref` input (e.g., the bootstrap seed `0a2dabe` or a remote branch like `origin/develop`). The workflow uses pinned actions by full commit SHA and has read-only permissions with no secrets or publishing. Run directories are uploaded as artifacts named `ci-<job>` (30 days retention) on success or failure.

### Fault injection and lock drift

*Status: Verified (Windows) – [P1.09 record](docs/p1/P1_09_LOCAL_CI.md), [P1.10 record](docs/p1/P1_10_DEBIAN_ARM64.md)*

`-FaultInjection` values exist only to verify the exit codes; lock-file drift after restore fails with 30 (solution) or 70 (consumer).

## 7. Debian/ARM64

### Target preparation, transfer with a git bundle, the run command

*Status: Verified (Debian/ARM64) – [P1.10 record](docs/p1/P1_10_DEBIAN_ARM64.md)*

On Windows, create and copy the bundle:

```powershell
git bundle create weatherparser.bundle <branch> <target branch>
git bundle verify weatherparser.bundle
scp weatherparser.bundle <target>:<directory>/
```

On the target:

```bash
cd <directory>
git clone weatherparser.bundle weatherparser
cd weatherparser
bash scripts/prepare-debian-arm64.sh > env.sh
. ./env.sh
pwsh ./scripts/ci-local.ps1 -Commit origin/<branch> -TargetRef origin/<target branch> -RunSettings coverage.runsettings
```

Back on Windows, copy the run directory:

```powershell
scp -r <target>:<directory>/weatherparser/artifacts/ci/<run> artifacts/ci/arm64-<run>
```

Never `-Bootstrap` on the target.

### `-CollectOnly`/`-ResumeFrom` fallback

*Status: Verified (Windows, Debian/ARM64) – [P1.10 record](docs/p1/P1_10_DEBIAN_ARM64.md)*

If conversion cannot run on the target, use `-CollectOnly` there, copy the run directory back, and run `pwsh ./scripts/ci-local.ps1 -ResumeFrom <copied run directory>` on Windows.

Authorization note: run on a target only with the owner's recorded authorization (AGENTS.md).

## 8. Troubleshooting

### NETSDK1141 in Visual Studio

*Status: historical remedy, not reproducible on demand – [P1.03](docs/p1/P1_03_BUILD_SETUP.md)*

If Visual Studio reports NETSDK1141, run `dotnet build-server shutdown` and restart Visual Studio completely.

### Stale cached package

See [section 4](#4-pack-and-package-consumer).

### Lock-file drift

Exit codes 30 (solution) or 70 (consumer). Restore with `--locked-mode` and review lock changes; see [section 6](#6-local-ci).

*Status: Verified (Windows) – [P1.09 record](docs/p1/P1_09_LOCAL_CI.md)*

### Exit 80 "Run dotnet tool restore"

Older script revisions shared the per-user tool resolver cache, so a deleted run folder broke later runs; the script now isolates `DOTNET_CLI_HOME` per run ([P1.10 record, finding 2](docs/p1/P1_10_DEBIAN_ARM64.md#findings)).

*Status: Verified (Windows, Debian/ARM64) – [P1.10 record](docs/p1/P1_10_DEBIAN_ARM64.md)*

## 9. Limits

* Hosted CI: the GitHub Actions workflow runs the same script for pull requests on hosted runners (see [section 6](#6-local-ci)); its checks cannot be made required on the current plan, and local and hosted runs are recorded separately.
* Windows evidence is not ARM64 evidence.
* The Debian/ARM64 evidence covers the package foundation at P1 scope only, not release readiness.
* Coverage is measured as Microsoft.CodeCoverage native block coverage with the rules of [PHASE_P1_PLAN.md section 5](PHASE_P1_PLAN.md).
* Test counts, coverage figures and hashes are kept in the [records](docs/p1/), not here.

## Version literals

| Literal | Used in | Source of truth |
|---|---|---|
| `18.11.0` | `--tool-version` of the runner commands (section 5) | `Microsoft.CodeCoverage` in `Directory.Packages.props` |
| `0.1.0-initial` | Package file name and stale cache entry path in section 4 | `Version` in `src/WeatherParser.Parsing/WeatherParser.Parsing.csproj` and the `WeatherParser.Parsing` entry in `Directory.Packages.props` |
