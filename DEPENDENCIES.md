# Development dependencies

Review date: 2026-10-15. P1.02 review complete; SDK update remains open. The library has no external runtime packages. Tests resolve six direct and ten transitive packages. All direct versions match the latest stable NuGet releases at review time. Exact resolved versions/hashes remain in the project lock files.

| Package | Version | Scope | Declared license |
|---|---|---|---|
| [FluentAssertions](https://www.nuget.org/packages/FluentAssertions/8.11.0) | 8.11.0 | Direct test | Xceed Community License (non-commercial) |
| [Microsoft.CodeCoverage](https://www.nuget.org/packages/Microsoft.CodeCoverage/18.11.0) | 18.11.0 | Direct test | MIT |
| [Microsoft.NET.Test.Sdk](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.11.0) | 18.11.0 | Direct test | MIT |
| [Moq](https://www.nuget.org/packages/Moq/4.21.0) | 4.21.0 | Direct test | BSD-3-Clause |
| [MSTest.TestAdapter](https://www.nuget.org/packages/MSTest.TestAdapter/4.5.1) | 4.5.1 | Direct test | MIT |
| [MSTest.TestFramework](https://www.nuget.org/packages/MSTest.TestFramework/4.5.1) | 4.5.1 | Direct test | MIT |
| [Castle.Core](https://www.nuget.org/packages/Castle.Core/5.2.1) | 5.2.1 | Transitive test | Apache-2.0 |
| [Microsoft.ApplicationInsights](https://www.nuget.org/packages/Microsoft.ApplicationInsights/2.23.0) | 2.23.0 | Transitive test | MIT |
| [Microsoft.Testing.Extensions.Telemetry](https://www.nuget.org/packages/Microsoft.Testing.Extensions.Telemetry/2.5.1) | 2.5.1 | Transitive test | MIT |
| [Microsoft.Testing.Extensions.TrxReport.Abstractions](https://www.nuget.org/packages/Microsoft.Testing.Extensions.TrxReport.Abstractions/2.5.1) | 2.5.1 | Transitive test | MIT |
| [Microsoft.Testing.Platform](https://www.nuget.org/packages/Microsoft.Testing.Platform/2.5.1) | 2.5.1 | Transitive test | MIT |
| [Microsoft.Testing.Platform.MSBuild](https://www.nuget.org/packages/Microsoft.Testing.Platform.MSBuild/2.5.1) | 2.5.1 | Transitive test | MIT |
| [Microsoft.TestPlatform.ObjectModel](https://www.nuget.org/packages/Microsoft.TestPlatform.ObjectModel/18.11.0) | 18.11.0 | Transitive test | MIT |
| [Microsoft.TestPlatform.TestHost](https://www.nuget.org/packages/Microsoft.TestPlatform.TestHost/18.11.0) | 18.11.0 | Transitive test | MIT |
| [MSTest.Analyzers](https://www.nuget.org/packages/MSTest.Analyzers/4.5.1) | 4.5.1 | Transitive test | MIT |
| [System.Diagnostics.EventLog](https://www.nuget.org/packages/System.Diagnostics.EventLog/6.0.0) | 6.0.0 | Transitive test | MIT |

## CI prerequisites

`scripts/ci-local.ps1` (P1.09) requires PowerShell 7 (verified with 7.6.7 on Windows and on Debian 13 ARM64) and Git (verified with 2.55), plus the pinned .NET SDK and network access to nuget.org. No additional packages are used.

- Debian 13 ARM64 target (P1.10): `scripts/prepare-debian-arm64.sh` installs the pinned .NET SDK 10.0.402 with Microsoft's `dotnet-install.sh` and PowerShell 7.6.7 from the official `linux-arm64` binary archive (SHA-256 checked against the release `hashes.sha256`) into user directories only; it installs no system packages. The system must already provide git, curl, tar, iconv and the .NET dependencies Microsoft lists for Debian 13 (ca-certificates, libc6, libgcc-s1, libgssapi-krb5-2, libicu76, libssl3t64, libstdc++6, tzdata). Platform evidence and vendor support: [COMPATIBILITY.md](docs/COMPATIBILITY.md).
- Run settings: `coverage.runsettings` enables static and disables dynamic managed instrumentation of Microsoft.CodeCoverage (the dynamic profiler is unsupported on Linux ARM64); `scripts/ci-local.ps1 -RunSettings coverage.runsettings` copies it once per run and records its SHA-256 for both passes. No new package is involved.
- Each CI run isolates the NuGet package folder and the .NET CLI home (`DOTNET_CLI_HOME`, which holds the tool resolver cache), so the pinned `dotnet-coverage` is always restored into and run from the run's own folders.

## GitHub Actions

The repository uses a GitHub Actions workflow `.github/workflows/ci.yml` which runs the local CI script with pinned actions by full commit SHA and has no secrets or write permissions.

| Action | Version | Commit | License |
|---|---|---|---|
| actions/checkout | v7.0.1 | 3d3c42e5aac5ba805825da76410c181273ba90b1 | MIT |
| actions/setup-dotnet | v6.0.0 | a98b56852c35b8e3190ac28c8c2271da59106c68 | MIT |
| actions/upload-artifact | v7.0.2 | cf430e030ddbb5b0abf93d22962f4752f3646cd9 | MIT |

Versions and commits checked on 2026-10-20 against the GitHub releases.

## Local .NET tools

Pinned in `.config/dotnet-tools.json` (restore with `dotnet tool restore`); the global `dotnet-coverage` installation is not used.

| Tool | Version | Scope | Declared license |
|---|---|---|---|
| [dotnet-coverage](https://www.nuget.org/packages/dotnet-coverage/18.11.0) | 18.11.0 | Developer tool: converts raw `.coverage` reports to native XML | Microsoft Software License Terms – Microsoft .NET Library (proprietary; use to develop and test applications; not redistributed) |

Version 18.11.0 was chosen on 2026-10-19 to match `Microsoft.CodeCoverage` 18.11.0 (published on nuget.org; 18.11.2 and 18.12.0 also exist). Converting the P1.07 raw report with it reproduces the earlier counters exactly (57/33 and 189/4).

## Decision and limits

FluentAssertions 8 is not MIT. Current personal/noncommercial OpenSource test usage was checked against the [versioned license](https://www.nuget.org/packages/FluentAssertions/8.11.0/License); commercial contexts and redistribution require separate assessment. Preserve third-party licenses/notices. These tools remain test-only (`PrivateAssets="all"`, nonpackable test project), outside library runtime dependencies.

NuGet vulnerability/deprecation queries succeeded and reported no affected packages on the review date. Transitive versions follow parent requirements and remain locked; no independent upgrades are forced. New dependencies require a fresh version/license review.

Installed and pinned SDK: 10.0.303, whose feature band has left servicing. Selected update: **10.0.402**, not installed. Keep `global.json` unchanged until the update is installed and verified. Close this finding before P1.03 toolchain acceptance. [Microsoft servicing matrix](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs), [SDK downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

Visual Studio 2026 18.8.3 satisfies the documented net10.0 minimum; an IDE build after SDK update remains unverified. Debian 13 ARM64 is officially supported by .NET 10. Microsoft.CodeCoverage documents managed static collection there; dynamic Linux ARM64 collection is not supported. Actual project execution and coverage evidence remain future work, not established by compatible NuGet assets.

Full evidence, compatible asset selections, licensing boundaries, telemetry settings and follow-ups: [P1.02 review](docs/p1/P1_02_TOOLING_REVIEW.md), [inventory](docs/p1/P1_02_DEPENDENCY_INVENTORY.json).
