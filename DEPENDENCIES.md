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

## Decision and limits

FluentAssertions 8 is not MIT. Current personal/noncommercial OpenSource test usage was checked against the [versioned license](https://www.nuget.org/packages/FluentAssertions/8.11.0/License); commercial contexts and redistribution require separate assessment. Preserve third-party licenses/notices. These tools remain test-only (`PrivateAssets="all"`, nonpackable test project), outside library runtime dependencies.

NuGet vulnerability/deprecation queries succeeded and reported no affected packages on the review date. Transitive versions follow parent requirements and remain locked; no independent upgrades are forced. New dependencies require a fresh version/license review.

Installed and pinned SDK: 10.0.303, whose feature band has left servicing. Selected update: **10.0.402**, not installed. Keep `global.json` unchanged until the update is installed and verified. Close this finding before P1.03 toolchain acceptance. [Microsoft servicing matrix](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs), [SDK downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).

Visual Studio 2026 18.8.3 satisfies the documented net10.0 minimum; an IDE build after SDK update remains unverified. Debian 13 ARM64 is officially supported by .NET 10. Microsoft.CodeCoverage documents managed static collection there; dynamic Linux ARM64 collection is not supported. Actual project execution and coverage evidence remain future work, not established by compatible NuGet assets.

Full evidence, compatible asset selections, licensing boundaries, telemetry settings and follow-ups: [P1.02 review](docs/p1/P1_02_TOOLING_REVIEW.md), [inventory](docs/p1/P1_02_DEPENDENCY_INVENTORY.json).
