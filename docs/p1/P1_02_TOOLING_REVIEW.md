# P1.02 – Tooling, dependencies and compatibility

**Review completed on 2026-10-15; open finding: SDK update before toolchain approval.** P1.02 inspects and documents tooling. No packages, SDKs or Visual Studio installations were changed. This review is not full P1 or ARM64 runtime acceptance.

## Toolchain decision

| Item | Finding | Consequence |
|---|---|---|
| Installed SDK | 10.0.303, pinned in `global.json` | Existing local build baseline; not approved as currently serviced |
| Supported SDK update | 10.0.402, released 2026-10-13; runtime 10.0.13 | Selected for the next tooling update, not installed yet |
| SDK servicing | 10.0.3xx ended August 2026; 10.0.4xx remains serviced | Close SDK finding before P1.03 acceptance |
| IDE | Visual Studio Community 2026, 18.8.3 | `net10.0` requires VS 18.0+; IDE build with the updated SDK remains unverified |
| Target | .NET ≥10, Debian ≥13, ARM64 | Officially supported combination; actual project runtime remains unverified |

Sources: [Microsoft SDK downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), [SDK/Visual Studio mapping and servicing](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs), [.NET 10 OS matrix](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md). Checked on 2026-10-15. A Visual Studio version shipping with an SDK is not automatically its minimum compatible IDE version.

The working copy remains buildable with SDK 10.0.303. Change `global.json` only after installing and verifying the selected update so the audit itself does not introduce a missing-SDK failure. Preserving this baseline does not approve indefinite use of an expired SDK band. Before P1.03 acceptance: update SDK, pin `global.json`, verify locked restore, Release build and IDE use.

## Complete package graph

The library currently has **no external runtime packages**. The test project resolves **6 direct and 10 transitive packages**. All six direct references use `PrivateAssets="all"`; the test project is not packable. The [dependency list](../../DEPENDENCIES.md) records all 16 packages, versions, licenses and sources. The [machine-readable inventory](P1_02_DEPENDENCY_INVENTORY.json) adds metadata/license hashes and actual compile/runtime asset selections.

All six direct versions match the latest stable versions in the official NuGet index. Resolve transitive versions according to package requirements and pin them through lock files; do not independently promote them beyond parent requirements without need. In particular, Castle.Core brings `System.Diagnostics.EventLog` 6.0.0. NuGet vulnerability/deprecation queries succeeded and reported no affected packages. This is a point-in-time source check, not assurance against unknown vulnerabilities.

## Licenses and distribution

Reviewed `.nuspec` license declarations for all 16 resolved packages and read the bundled FluentAssertions license. Packages use MIT, BSD-3-Clause, Apache-2.0 or the Xceed Community License. Findings account for current personal/noncommercial OpenSource development and test-only tooling; they do not give blanket approval to arbitrary commercial contributors.

FluentAssertions 8.11.0 is **not MIT**. Its [versioned license](https://www.nuget.org/packages/FluentAssertions/8.11.0/License) names open-source/personal development as a permitted context and restricts commercial use and redistribution. WeatherParser's own source may remain MIT; that does not relicense dependencies. Preserve third-party notices in Microsoft.CodeCoverage/TestHost if redistributing those tools later. The current library distributes neither these test packages nor their binaries.

## Platform and test-tool review

| Area | Evidence | Remaining runtime verification |
|---|---|---|
| `net10.0` package resolution | Compatible assets selected from `net6.0`, `net8.0`, `net9.0` and `netstandard2.0` | Meaningful behavior tests in P1.05/P1.06 |
| Test runner | Microsoft.NET.Test.Sdk and MSTest.TestAdapter supply VSTest integration; Microsoft.CodeCoverage is the selected collector | Actual test execution/reports in P1.05/P1.07 |
| Debian/ARM64 | .NET 10 officially supported; current library has no external/native packages | Actual consumption/execution on Debian 13 ARM64 in P1.10/P9 |
| Debian/ARM64 coverage | Managed static instrumentation officially supported; dynamic Linux ARM64 profiler unsupported | Verify static collection and real report metrics in P1.07/P1.10 |
| EventLog | Windows-specific functionality, transitive test-only dependency | Do not require Windows EventLog APIs on Debian |

Sources: [Microsoft.CodeCoverage OS matrix](https://github.com/microsoft/codecoverage/blob/main/docs/supported-os.md), [instrumentation modes](https://github.com/microsoft/codecoverage/blob/main/docs/instrumentation.md). ARM64 DLLs in the inspected package are Windows binaries; they do not establish a Linux ARM64 profiler. Static instrumentation does not need that dynamic profiler. Line/block/branch metrics remain distinct.

MSTest includes the transitive Telemetry → ApplicationInsights chain. Presence alone does not prove transmission. Diagnostics used `DOTNET_CLI_TELEMETRY_OPTOUT=1` and `TESTINGPLATFORM_TELEMETRY_OPTOUT=1`; both are documented for future local/CI tests. [Microsoft telemetry settings](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-telemetry).

## Checks and handoff

- Recorded installed SDK/runtime/Visual Studio; checked servicing against primary sources.
- Inspected both locks, central versions, test-reference boundaries, metadata, licenses and selected assets.
- Queried current direct NuGet versions and vulnerability/deprecation information.
- Did not repeat builds solely for documentation changes: prior successful locked restore/Release build belongs to the unchanged baseline. No test methods or coverage values were fabricated.
- Preserved package/project/SDK files; no commit, push or target-system access.

**P1.02 review complete; SDK finding open.** The next prerequisite for P1.03 is a controlled SDK update and verification. License/version findings apply to captured packages; review new DI/domain dependencies on adoption. Coverage acceptance remains P1.07/P1.08 and actual ARM64 acceptance P1.10/P9.
