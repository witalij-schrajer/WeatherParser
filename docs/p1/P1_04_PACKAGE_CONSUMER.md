# P1.04 – Local NuGet pack and isolated package consumer

**Status: completed on 2026-10-16.** Scope: prove the actual package build, create an isolated consumer without a source reference, and secure it with a real `PackageReference` against a locally built `.nupkg`. No public NuGet publication, no CLI, no weather-source parsers in this step.

## Steps performed

| Step | Result |
|---|---|
| Package version | `WeatherParser.Parsing.csproj`: `Version` changed from `0.1.0-local` to `0.1.0-initial` (last segment of the current branch name `features/develop/initial`) |
| Local package folder | `artifacts/local-nuget/` created; already excluded via the `.gitignore` entry `artifacts/` |
| Local pack | `dotnet pack src/WeatherParser.Parsing/WeatherParser.Parsing.csproj -c Release -o artifacts/local-nuget` succeeded; produced `WeatherParser.Parsing.0.1.0-initial.nupkg` |
| Local package source | `nuget.config` extended with a project-local source `local-weatherparser` (`artifacts/local-nuget`); no global `nuget sources add`, no change to the user environment |
| Central package version | `Directory.Packages.props` extended with `PackageVersion Include="WeatherParser.Parsing" Version="0.1.0-initial"` since central package management is enabled |
| New test project | `tests/WeatherParser.Package.Tests/WeatherParser.Package.Tests.csproj` created; references the library exclusively via `PackageReference`, **no** `ProjectReference` |
| Solution | Project added via `dotnet sln WeatherParser.slnx add ...` |
| Restore | `dotnet restore WeatherParser.slnx` resolves `WeatherParser.Parsing` from the local source; `dotnet restore WeatherParser.slnx --locked-mode` succeeded, `packages.lock.json` for the new project generated automatically |
| Build (CLI) | `dotnet build WeatherParser.slnx -c Release` succeeded for all three projects, 0 errors |
| Tests | `dotnet test tests/WeatherParser.Package.Tests -c Release`: 2 of 2 tests passed |

## Evidence tests (T-P1-21, T-P1-22)

`PackageConsumptionTests.cs`:

- **`Deps_file_declares_the_library_as_a_restored_package_not_a_project_reference`** (T-P1-21): Reads the test host's `WeatherParser.Package.Tests.deps.json` and verifies that the `WeatherParser.Parsing/0.1.0-initial` entry carries `"type": "package"` with a `.nupkg.sha512` hash path. A `ProjectReference` would instead produce `"type": "project"` with no package hash. An earlier attempt that checked the assembly path for the word "packages" was unreliable (PackageReference assemblies are also copied locally) and was discarded.
- **`Package_exposes_only_the_expected_library_assembly`** (T-P1-22): Verifies that the loaded assembly is named exactly `WeatherParser.Parsing` and references no assembly with a CLI/MQTT name.

## Checks and handoff

- Package version, local source, and test project are configured entirely project-locally; no global NuGet or user settings were changed.
- Locked restore and Release build succeeded via the `dotnet` CLI; the IDE build finding from P1.03 (`NETSDK1141`) has since been resolved (see [P1.03 record](P1_03_BUILD_SETUP.md)).
- No coverage figures claimed; coverage collection remains assigned to P1.07/P1.08.
- No Git commit created in this step; the repository still has no initial commit.

**P1.04 complete.** The next prerequisite for P1.05 (TDD test foundation) and P1.06 (package/DI contracts) is satisfied, since both source and package tests now coexist and build.
