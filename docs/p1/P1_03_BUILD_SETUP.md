# P1.03 – Prepare minimal solution, library and test projects

**Status: completed on 2026-10-15. The Visual Studio build limitation has been resolved; clean-checkout reproducibility remains outstanding (see below).** Scope: close the SDK finding from P1.02, prove reproducible restore/build with the selected SDK, verify build/secret exclusions. No new domain implementation, parsers, or CLI in this step.

## Steps performed

| Step | Result |
|---|---|
| SDK availability | `dotnet --list-sdks` shows `10.0.402 [C:\Program Files\dotnet\sdk]`; already installed locally |
| `global.json` | `version` changed from `10.0.303` to `10.0.402`; `rollForward: disable` and `allowPrerelease: false` unchanged |
| `dotnet --version` in repo root | Reports `10.0.402`; SDK resolution via `global.json` confirmed |
| Locked restore | `dotnet restore WeatherParser.slnx --locked-mode` succeeded; both projects' `packages.lock.json` unchanged since no package versions were touched |
| Release build (dotnet CLI) | `dotnet build WeatherParser.slnx -c Release` succeeded for `WeatherParser.Parsing` and `WeatherParser.Parsing.Tests`; 0 errors |
| Build/secret exclusions | `.gitignore` already covers `bin/`, `obj/`, `TestResults/`, `artifacts/`, `*.nupkg`/`*.snupkg`, `*.coverage(xml)`, `.env*`, `secrets/`, `*.pfx`/`*.p12`/`*.key`; no change needed |

## Resolved: Visual Studio build

Building through the MSBuild embedded in Visual Studio Community 2026 (18.11.0) previously failed with `NETSDK1141` (`GlobalJsonSDKResolutionFailed`), even though `dotnet build` via the CLI succeeded with the identical `global.json`. The cause was a stale SDK resolver / build-server state in the running IDE instance.

**Resolution:** running `dotnet build-server shutdown` to terminate cached MSBuild/VBCSCompiler server processes, followed by a full restart of Visual Studio (all `devenv.exe` processes terminated and the IDE restarted), resolved the issue. A subsequent solution build executed through Visual Studio/MSBuild succeeded with no `NETSDK1141` error. This finding is now closed.

## Reproducibility on a clean checkout

The repository has no Git commit yet (`git log` reports "does not have any commits yet", consistent with the P1.01 finding). An isolated worktree/clone proof (matching T-P1-08) is therefore not currently feasible and is not claimed. This evidence is deferred to a future step after the first commit.

## Checks and handoff

- SDK band updated and `global.json` pinned; SDK finding from P1.02 closed.
- Locked restore and CLI Release build proven successful with the new SDK.
- Existing `.gitignore` exclusions reviewed as sufficient; no change required.
- No package versions, test code, or coverage figures were changed or claimed in this step.

**P1.03 complete for the CLI build and the IDE build; clean-checkout reproducibility remains outstanding.** The next prerequisite for P1.04 (isolated package consumer) remains satisfiable since the library builds. The pending clean-checkout evidence does not block P1.04 but should be closed before the overall P1 acceptance (P1.12).
