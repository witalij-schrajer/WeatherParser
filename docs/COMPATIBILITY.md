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

## P2 scope (P2.11, 2026-10-20)

Candidate `bced7be380c3e637fdd042c4165cd074e25f25a6` against PR base `64e460b475b4a29b9e3ddd331394076e53982d9e`; details in the [P2.11 record](p2/P2_VERIFICATION.md#11-p211--integrated-coverage-and-platform-verification).

| Platform | Evidence class | Result |
|---|---|---|
| Windows x64 (developer machine), SDK 10.0.402, runtime 10.0.13 | Local two-pass `ci-local.ps1` | Exit 0; tests 880, 99, 56, 43, consumer 18; block coverage 3035/3037 against baseline 54/56; gate passed |
| NanoPi R76S, Debian 13 (trixie), `linux-arm64`, SDK 10.0.402, runtime 10.0.13, ICU 76, tzdata `2025b-4+deb13u1` | Class B target run (CONTRIBUTING.md section 7) | Exit 0; same tests and gate result; serializers, `Europe/Berlin` handling, `en-US`/`de-DE` culture checks with ICU data (system locale only `C.utf8`), schema regeneration byte-identical to Windows, package consumer with schema delivery; all standard test projects also pass in a network namespace with loopback only |
| GitHub-hosted `windows-x64`, `ubuntu-x64`, `debian13-arm64` container | Hosted workflow (run 39493310294) | All jobs succeeded; recorded separately and not counted as target evidence |

## P4 scope (P4.11, rerun 2026-10-24)

Candidate `b6bc47f89147c05bf04e06cf0f87c1497bc651f3` (after the A10 review fixes) against PR base `b7da8288d8746bc6e034222c0467a8fcebba55b3`. Details are in the [P4.11 record](p4/P4_11_INTEGRATED_VERIFICATION.md), section 9. The first verification of candidate `98122e5` (2026-10-23) is in sections 3 to 8 of that record.

| Platform | Evidence class | Result |
|---|---|---|
| Windows x64 (developer machine), SDK 10.0.402, runtime 10.0.13 | Local two-pass `ci-local.ps1` | Exit 0; tests 1662 passed (live test not executed), 99, 56, 46, consumer 31; block coverage 6811/6814 against baseline 3035/3037; gate passed |
| NanoPi R76S, Debian 13 (trixie), `linux-arm64`, SDK 10.0.402, runtime 10.0.13, ICU 76 | Class B target run (CONTRIBUTING.md section 7) | Exit 0; same tests, outcomes and gate result as Windows (geodesy reference distances within their test tolerances: 1 mm for converging Vincenty pairs, 0.6 % for spherical calculations and the near-antipodal fallback (SC-06)) |
| GitHub-hosted `windows-x64`, `ubuntu-x64`, `debian13-arm64` container | Hosted workflow (run 40041773136) | All jobs succeeded; recorded separately and not counted as target evidence |
| Windows x64 (developer machine), live DWD sources | Explicitly enabled live check (`WEATHERPARSER_LIVE_CHECK=1`), one run on candidate `98122e5` | 14 requests of a budget of 56, all HTTP 200; both documents schema-valid. Recorded as a snapshot of 2026-10-23 and not repeated for `b6bc47f` (SC-16). Offline, the `b6bc47f` MOSMIX_S parser builds the same index from a stored real file as `98122e5` |

## P5 scope (P5.10, rerun 2026-10-25)

Candidate `fd461c8cededddf53a039c31adad68a110c1c7c9` (after the PR #7 review fixes) against PR base `1e604dc7b3f17826d19207cbec9a699e40e5a30f`. Details are in the [P5.10 record](p5/P5_10_INTEGRATED_VERIFICATION.md), section 9. The first verification of candidate `a87c7bb` is in sections 3 to 8 of that record.

| Platform | Evidence class | Result |
|---|---|---|
| Windows x64 (developer machine), SDK 10.0.402, runtime 10.0.13 | Local two-pass `ci-local.ps1` | Exit 0; tests 1937 passed (two live tests not executed), 99, 56, 46, consumer 36; block coverage 7491/7494 against baseline 6811/6814; gate passed |
| NanoPi R76S, Debian 13 (trixie), `linux-arm64`, SDK 10.0.402, runtime 10.0.13, ICU 76 | Class B target run (CONTRIBUTING.md section 7) | Exit 0; same tests, outcomes, block counters and gate result as Windows |
| GitHub-hosted `windows-x64`, `ubuntu-x64`, `debian13-arm64` container | Hosted workflow (run 40170729670) | All jobs succeeded; recorded separately and not counted as target evidence |
| Windows x64 (developer machine), live DWD sources | Explicitly enabled observation live check (`WEATHERPARSER_LIVE_CHECK=1`, `WEATHERPARSER_LIVE_CHECK_MODE=observation`), one run on candidate `a87c7bb` | Station 01526: 6 requests of a budget of 8, all HTTP 200; dataset schema-valid, JSON and XML equivalent. Recorded as a snapshot of 2026-10-25 and not repeated for `fd461c8` (SC-16 pattern) |

## P6 scope (P6.10, 2026-10-26)

Candidate `4373399b278e65097452fe18d7029fee5461bdb1` against PR base `8d8079a4238514f7c9bcfc3846829e2408938ae3`. Details are in the [P6.10 record](p6/P6_10_INTEGRATED_VERIFICATION.md).

| Platform | Evidence class | Result |
|---|---|---|
| Windows x64 (developer machine), SDK 10.0.402, runtime 10.0.13 | Local two-pass `ci-local.ps1` | Exit 0; tests 2295 passed (three live tests not executed), 99, 56, 46, consumer 41; block coverage 8475/8478 against baseline 7491/7494; gate passed |
| NanoPi R76S, Debian 13 (trixie), `linux-arm64`, SDK 10.0.402, runtime 10.0.13, ICU 76 | Class B target run (CONTRIBUTING.md section 7) | Exit 0; same tests, outcomes, block counters and gate result as Windows, including the `Europe/Berlin` time-zone tests |
| GitHub-hosted `windows-x64`, `ubuntu-x64`, `debian13-arm64` container | Hosted workflow (run 40255069498) | All jobs succeeded; recorded separately and not counted as target evidence |
| Windows x64 (developer machine), live OsthessenWetter page | Explicitly enabled live check (`WEATHERPARSER_LIVE_CHECK=1`, `WEATHERPARSER_LIVE_CHECK_MODE=osthessen`), one authorized run | 1 request of a budget of 1, HTTP 200; 20 stations matched, 16 datasets, 4 offline station failures; every dataset schema-valid, JSON and XML equivalent; placeholder coordinates; snapshot of 2026-10-26, not repeated (OW-15) |

## Limits

- Class B proves the package foundation on ARM64 at P1 scope only, not release readiness (P9).
- P2 scope (serializers, output schema, time-zone handling, DI and package consumption) is verified on the target by P2.11 (section below); DI under load, CLI, MQTT and network sources are not yet covered (later phases).
- P4 scope (coordinates, geodesy, CDC and MOSMIX catalogs and availability, the retrieval boundary with mocked HTTP, the MOSMIX_S evidence cache, selection services, DI and package consumption) is verified on the target by P4.11; live retrieval was checked once on Windows only, not on the target; CLI and MQTT are not yet covered (later phases).
- P5 scope (DWD station resolution, observation file reading, quantity mapping, the observation retrieval boundary with mocked HTTP, the parser service, export equivalence, DI and package consumption) is verified on the target by P5.10; the live observation parse was checked once on Windows only, not on the target; CLI and MQTT are not yet covered (later phases).
- P6 scope (station name patterns, OsthessenWetter page reading, quantity mapping, identity and times, the page retrieval boundary with mocked HTTP, the parser service, export equivalence, DI and package consumption) is verified on the target by P6.10; the live check ran once on Windows only, not on the target; production use of OsthessenWetter values is open until the source conditions are clarified (OW-14); CLI and MQTT are not yet covered (later phases).
- Windows-versus-ARM64 block counters are descriptive only, gating always uses a baseline measured on the same platform.
- Emulation or x64 results never count as ARM64 evidence.
