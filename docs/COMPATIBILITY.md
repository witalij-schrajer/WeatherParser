# Compatibility

Verified compatibility of the package foundation at P1 scope, as of 2026-10-19. Requested support comes from REQUIREMENTS.md (REQ-004, REQ-017); vendor support is quoted from official sources with the check date; project evidence is Class A (build/restore on Windows developer machine, never runtime evidence) or Class B (runtime evidence on the Debian 13 ARM64 target), or OPEN. 

Status legend:
- **Class A**: build and restore evidence collected on Windows developer machine
- **Class B**: runtime evidence collected on Debian 13 ARM64 target
- **OPEN**: not verified

| Component | Requested support (REQ-004/REQ-017) | Official vendor support (source, checked 2026-10-19) | Project evidence |
|---|---|---|---|
| .NET SDK 10.0.402 (pinned by `global.json`, `rollForward: disable`) | .NET 10 or newer | .NET 10 is an LTS release ([Install .NET without a package manager](https://learn.microsoft.com/dotnet/core/install/linux-scripted-manual)) | Windows x64: [P1.09 record](p1/P1_09_LOCAL_CI.md) and [P1.10 record](p1/P1_10_DEBIAN_ARM64.md) Windows runs; Debian 13 ARM64: Class B, installed with `dotnet-install.sh --version 10.0.402` into the user directory; all runs used 10.0.402 |
| .NET runtime | runtime of .NET 10 | as row 1 | Class B: `Microsoft.NETCore.App` 10.0.13 on the target (`dotnet --list-runtimes`), recorded separately from the SDK version ([P1.10 record](p1/P1_10_DEBIAN_ARM64.md)) |
| Debian 13 Trixie | Debian 13 or newer | Debian 13 supports .NET 10, 9 and 8 ([Install .NET on Debian](https://learn.microsoft.com/dotnet/core/install/linux-debian)); .NET 10 supported-OS list: Debian 13 with Arm32, Arm64, x64 ([.NET 10 supported OS list](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md)) | Class B: Debian GNU/Linux 13.3 (trixie); all eight .NET dependencies listed by Microsoft for Debian 13 present (ca-certificates, libc6, libgcc-s1, libgssapi-krb5-2, libicu76, libssl3t64, libstdc++6, tzdata) ([P1.10 record](p1/P1_10_DEBIAN_ARM64.md)) |
| `linux-arm64` (NanoPi R76S) | minimum architecture ARM64, particularly NanoPi R76S | Arm64 supported for Debian 13 (supported-OS list above) | Class B: FriendlyElec NanoPi R76S, `aarch64`, RID `linux-arm64`: locked restore, Release build (0 warnings), 162 tests passed (15 + 91 + 54 + 2), pack, package-content check, package consumer against the package built on the target. Class A: RID-specific build succeeds; the RID-specific locked restore fails with NU1004 because the lock files have no `linux-arm64` section (finding; the target build is RID-neutral and needs none) ([P1.10 record](p1/P1_10_DEBIAN_ARM64.md)) |
| Windows x64 (developer machine) | development environment | n/a – developer machine | [P1.09 record](p1/P1_09_LOCAL_CI.md) and [P1.10 record](p1/P1_10_DEBIAN_ARM64.md) Windows runs |
| Microsoft.CodeCoverage 18.11.0 static managed instrumentation on `linux-arm64` | coverage on the target | Settings `EnableStaticManagedInstrumentation` / `EnableDynamicManagedInstrumentation` documented in [Code Coverage configuration](https://github.com/microsoft/codecoverage/blob/main/docs/configuration.md) (the page gives no per-platform statement) | Class B: `coverage.runsettings` (static on, dynamic off); native block counters for `WeatherParser.Parsing.dll` present: 54/56 current, 52/56 target. Windows: static and dynamic collection give identical counters (54/56) ([P1.10 record](p1/P1_10_DEBIAN_ARM64.md)) |
| Microsoft.CodeCoverage dynamic instrumentation on `linux-arm64` | not required | Vendor: unsupported on Linux ARM64 ([P1.02 tooling review](p1/P1_02_TOOLING_REVIEW.md)) | Not used; disabled by `coverage.runsettings` |
| `dotnet-coverage` 18.11.0 on `linux-arm64` | conversion of raw reports | Pinned local tool (`.config/dotnet-tools.json`) | Class B: conversion ran on the target (`converterRid` `linux-arm64`, tool version 18.11.0); the Windows fallback (`-CollectOnly` + `-ResumeFrom`) was also verified with ARM64 raw reports ([P1.10 record](p1/P1_10_DEBIAN_ARM64.md)) |
| PowerShell 7 on Debian ARM64 | required by the single CI script (P1.09 decision a) | PowerShell supports Debian 13 until 2030-06-30 ([Install PowerShell on Debian](https://learn.microsoft.com/powershell/scripting/install/install-debian)); the documented Debian packages are amd64; the binary-archive install used on ARM64 is described under alternate methods, which "may work but aren't officially supported by Microsoft" ([Alternate ways to install PowerShell](https://learn.microsoft.com/powershell/scripting/install/alternate-install-methods)) | Class B: PowerShell 7.6.7 `linux-arm64` archive, SHA-256 verified against the release `hashes.sha256`, user-directory install; ran every CI run ([P1.10 record](p1/P1_10_DEBIAN_ARM64.md)) |
| MSTest/VSTest on `linux-arm64` | test execution on the target | MSTest 4.5.1 / Microsoft.NET.Test.Sdk as pinned in `Directory.Packages.props` | Class B: all four test projects ran on the target; `-FaultInjection FailingTest` exits 50 with the TRX kept ([P1.10 record](p1/P1_10_DEBIAN_ARM64.md)) |
| `linux-x64` | only if used | — | Not used ([P1.10 record](p1/P1_10_DEBIAN_ARM64.md)) |

## Limits

- Class B proves the package foundation on ARM64 at P1 scope only, not release readiness (P9).
- Serializers, timezones, DI under load, CLI, MQTT and network sources are not yet covered (later phases).
- Windows-versus-ARM64 block counters are descriptive only, gating always uses a baseline measured on the same platform.
- Emulation or x64 results never count as ARM64 evidence.
