# Compatibility

German translation: [de/COMPATIBILITY.md](de/COMPATIBILITY.md).

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
| MSTest/VSTest on `linux-arm64` | test execution on the target | MSTest as pinned in `Directory.Packages.props` (4.5.1 at the P1.10 evidence; 4.5.2 since P9.03 on 2026-10-31) / Microsoft.NET.Test.Sdk as pinned | Class B: all four test projects ran on the target; `-FaultInjection FailingTest` exits 50 with the TRX kept ([P1.10 record](p1/P1_10_DEBIAN_ARM64.md)) |
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

Candidate `4373399b278e65097452fe18d7029fee5461bdb1` against PR base `8d8079a4238514f7c9bcfc3846829e2408938ae3`; after the PR #9 review fixes S1 to S3 were repeated on candidate `12e7205385d1ad6ca1eb45cb4fcb1f7b50699554` with the same outcomes (tests 2301 passed, block coverage 8486/8489; hosted run 40284717931). Details are in the [P6.10 record](p6/P6_10_INTEGRATED_VERIFICATION.md), sections 2 to 8.

| Platform | Evidence class | Result |
|---|---|---|
| Windows x64 (developer machine), SDK 10.0.402, runtime 10.0.13 | Local two-pass `ci-local.ps1` | Candidate `12e7205`: exit 0; tests 2301 passed (three live tests not executed), 99, 56, 46, consumer 41; block coverage 8486/8489 against baseline 7491/7494; gate passed (first candidate `4373399`: 2295 passed, 8475/8478) |
| NanoPi R76S, Debian 13 (trixie), `linux-arm64`, SDK 10.0.402, runtime 10.0.13, ICU 76 | Class B target run (CONTRIBUTING.md section 7) | Exit 0; same tests, outcomes, block counters and gate result as Windows, including the `Europe/Berlin` time-zone tests |
| GitHub-hosted `windows-x64`, `ubuntu-x64`, `debian13-arm64` container | Hosted workflow (run 40284717931 on `12e7205`; run 40255069498 on `4373399`) | All jobs succeeded; recorded separately and not counted as target evidence |
| Windows x64 (developer machine), live OsthessenWetter page | Explicitly enabled live check (`WEATHERPARSER_LIVE_CHECK=1`, `WEATHERPARSER_LIVE_CHECK_MODE=osthessen`), one authorized run on candidate `4373399`, not repeated for `12e7205` | 1 request of a budget of 1, HTTP 200; 20 stations matched, 16 datasets, 4 offline station failures; every dataset schema-valid, JSON and XML equivalent; placeholder coordinates; snapshot of 2026-10-26, not repeated (OW-15) |

## P7 scope (P7.11, 2026-10-28)

Candidate `a36d15a538e6ddc9cdf01d47b2ff45073a36cd95` against PR base `8009f1c413ead6b150f2c71bec8f669e81be5558` (pull request #11); after the PR #11 review fixes S1 to S3 were repeated on candidate `2f1115b8b89275b42408a6ee7b61b55c73219440` with the same outcomes (tests 2946 passed, block coverage 10246/10249; hosted run 40736417671; NanoPi resource check 51,505 ms and 142,491,648 bytes). Details are in the [P7.11 record](p7/P7_11_INTEGRATED_VERIFICATION.md), sections 3 to 7 and 9.

| Platform | Evidence class | Result |
|---|---|---|
| Windows x64 (developer machine), SDK 10.0.402 | Local two-pass `ci-local.ps1` | Exit 0; tests 2935 passed (7 opt-in live and resource tests not executed), 99, 56, 46, consumer 47; block coverage 10228/10231 against baseline 8486/8489; gate passed |
| Windows x64 (developer machine) | Offline MOSMIX_S resource check (synthetic file with the recorded 5648 placemarks × 40 elements × 240 steps, measured alone in a fresh process) | Duration 6,900 ms; process peak 117,379,072 bytes (informative; the readiness figure is the target measurement) |
| NanoPi R76S, Debian 13.3 (trixie), `linux-arm64`, SDK 10.0.402, ICU 76 | Class B target run (CONTRIBUTING.md section 7), authorized by the user | Exit 0; same tests, outcomes, block counters and gate result as Windows |
| NanoPi R76S | Offline MOSMIX_S resource check on the target (MF-05, MF-07 readiness) | Duration 58,710 ms of the 300,000 ms deadline; process peak 143,413,248 bytes of the 268,435,456-byte threshold; within both |
| GitHub-hosted `windows-x64`, `ubuntu-x64`, `debian13-arm64` container | Hosted workflow (run 40615010706 on `a36d15a`) | All jobs succeeded; recorded separately and not counted as target evidence |
| Windows x64 (developer machine), live DWD MOSMIX files | Explicitly enabled live checks (`WEATHERPARSER_LIVE_CHECK=1`, `WEATHERPARSER_LIVE_CHECK_MODE=mosmix-l` and `mosmix-s`), one authorized run per product on candidate `a36d15a` | MOSMIX_L: 1 request of a budget of 1, HTTP 200, 247 steps, fresh, schema-valid, no omissions; MOSMIX_S: 1 request, HTTP 200, 35,966,201 bytes, 240 steps, fresh, schema-valid, no omissions, parse 8,967 ms; forecast values not recorded (MF-17) |

## Release 1.0.0 scope (P9.10, verified 2026-11-01 to 2026-11-02 on the 0.1.0 build of the same library and CLI sources)

Rows prepared in P9.08 and filled in by P9.10 on release candidate `86e77eb` (RL-15 A, [P9.10 record](p9/P9_10_ARM64_RELEASE_ACCEPTANCE.md)); the CLI deployment check without root in P9.07 ([P9 verification record](p9/P9_VERIFICATION.md) section 8) is superseded by these rows.

| Platform | Evidence class | Result |
|---|---|---|
| NanoPi R76S, Debian 13 (trixie), `linux-arm64`, runtime 10.0.14 or newer | Class B: candidate CLI installed per RL-07 A from its checksummed artifact, as the service user | Verified on `86e77eb` with runtime 10.0.14 on Debian 13.7: checksums `OK`, the RL-07 A layout and modes, every run as `weatherparser` without a terminal ([P9.10 record](p9/P9_10_ARM64_RELEASE_ACCEPTANCE.md) section 4) |
| NanoPi R76S | Class B: package consumer against the release `.nupkg` without a source checkout | Verified on `86e77eb`: `WeatherParser.Package.Tests` 131/131 on the NanoPi and on Windows ([P9.10 record](p9/P9_10_ARM64_RELEASE_ACCEPTANCE.md) section 3) |
| NanoPi R76S | Class B: every CLI command once offline against the fixture host and once live in JSON mode (OsthessenWetter JSON only, OW-14) | Verified on `86e77eb`: offline exit codes 0, 2, 1 and 3 as expected (the MOSMIX commands only on their failure paths, no MOSMIX fixtures); live every command exit 0 except OsthessenWetter in the second setup (station reported offline, `OSTHESSEN-STATION-OFFLINE`, exit 1) ([P9.10 record](p9/P9_10_ARM64_RELEASE_ACCEPTANCE.md) section 4) |
| NanoPi R76S | Class B: one MQTT run against the test broker with TLS and credentials (RL-13 A) | Verified in P9.09 and on `86e77eb`: published with QoS 1 over TLS through an SSH tunnel to the Windows test broker and observed live ([P9.10 record](p9/P9_10_ARM64_RELEASE_ACCEPTANCE.md) section 4) |
| NanoPi R76S | Class B: signals and cleanup; cron every 10 minutes over 24 hours against the fixture host plus one live invocation per source per hour | Verified on `86e77eb`: SIGINT and SIGTERM exit 4 without output; 374 invocations over 36.5 hours, 369 exit 0 and 5 reported live item failures, peak memory at most 202,492 KB, no leftover process, file or user ([P9.10 record](p9/P9_10_ARM64_RELEASE_ACCEPTANCE.md) section 5) |
| GitHub-hosted `ubuntu-x64` | Release build of the candidate (`release-build.yml`) | Verified: run 41239327847 on `86e77eb` (SDK 10.0.402); dry runs on `a563fc4` recorded in P9.06 |

## Limits

- Class B proves the package foundation on ARM64 at P1 scope only, not release readiness (P9).
- P2 scope (serializers, output schema, time-zone handling, DI and package consumption) is verified on the target by P2.11 (section below); DI under load, CLI, MQTT and network sources are not yet covered (later phases).
- P4 scope (coordinates, geodesy, CDC and MOSMIX catalogs and availability, the retrieval boundary with mocked HTTP, the MOSMIX_S evidence cache, selection services, DI and package consumption) is verified on the target by P4.11; live retrieval was checked once on Windows only, not on the target; CLI and MQTT are not yet covered (later phases).
- P5 scope (DWD station resolution, observation file reading, quantity mapping, the observation retrieval boundary with mocked HTTP, the parser service, export equivalence, DI and package consumption) is verified on the target by P5.10; the live observation parse was checked once on Windows only, not on the target; CLI and MQTT are not yet covered (later phases).
- P6 scope (station name patterns, OsthessenWetter page reading, quantity mapping, identity and times, the page retrieval boundary with mocked HTTP, the parser service, export equivalence, DI and package consumption) is verified on the target by P6.10; the live check ran once on Windows only, not on the target; production use of OsthessenWetter values is open until the source conditions are clarified (OW-14); CLI and MQTT are not yet covered (later phases).
- P7 scope (MOSMIX forecast reading with the secure KML/KMZ reader, element mapping, the retrieval boundary with mocked HTTP, the MOSMIX_L and MOSMIX_S parser services, product independence, export equivalence, DI and package consumption) is verified on the target by P7.11, including the MOSMIX_S duration and peak-memory measurement on the target; the live parses ran once per product on Windows only, not on the target; CLI and MQTT are not yet covered (later phases).
- Windows-versus-ARM64 block counters are descriptive only, gating always uses a baseline measured on the same platform.
- Emulation or x64 results never count as ARM64 evidence.
