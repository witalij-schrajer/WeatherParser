# Changelog

All notable changes to WeatherParser are recorded here, one section per version, in the structure of [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [Semantic Versioning 2.0](https://semver.org/spec/v2.0.0.html). In the 0.x series the public API may change between minor versions.

## [0.1.0] – not yet released

**Status (2026-10-31): release candidate in preparation (phase P9).** The package `WeatherParser.Parsing` 0.1.0 is built and checksummed by the release build (`.github/workflows/release-build.yml`); the target acceptance on the NanoPi R76S (P9.10) and the user's acceptance (P9.11) are still open. The package is **not published** on nuget.org or any other feed (user decision of 2026-10-31), and the repository stays private (RL-19 A), so the package carries no repository or project URL. The date is set when the user releases the version.

### Added

Library `WeatherParser.Parsing` (`net10.0`):

- Station and location selection around a reference point: DWD CDC stations with availability evidence, MOSMIX forecast locations with product availability (`MOSMIX_S`, `MOSMIX_L`), decimal and DMS coordinates, geodesic distance and altitude filters (P4).
- DWD 10-minute observations of one CDC station: temperature, humidity, pressure (hPa and mmHg from the same value, with the pressure reference), wind, precipitation and solar quantities, with per-value UTC measurement times and quality metadata; missing values omitted, `0` kept (P5).
- OsthessenWetter observations from the measurement page, with the inferred date marked `estimated_date` (P6).
- MOSMIX_L and MOSMIX_S forecast parsers with separate interfaces and endpoints, a secure KMZ/KML reader, element mapping, issue, retrieval and validity times and a freshness marker (P7).
- Export entities, JSON (`System.Text.Json`) and XML (`XmlSerializer`) serialization of the same entities, and the output schema `schemas/export/v1/weatherparser-export-v1.xsd` in the package (P2).
- Generic MQTT message serializer with consumer-configured topic templates, topic validation, QoS and retain, without broker access (P8).
- Dependency-injection registration `AddWeatherParsing` with consumer-supplied endpoints (P1 to P8).
- XML documentation, a symbols package (`.snupkg`), deterministic build paths and a checked-in public API listing (P9.05).

Example CLI `WeatherParser` (not part of the package; built from source against the package, RL-08 A):

- Commands `--SelectDWD`, `--ParseDWD`, `--ParseOstHessen`, `--SelectMOSMIX`, `--ParseMOSMIX --Product MOSMIX_S|MOSMIX_L`; JSON, XML and MQTT output; MQTT transport with TLS and certificate validation, credentials from a protected file or the environment; one command per invocation with exit codes 0 to 4; cancellation by SIGINT, SIGTERM and Ctrl+C and a command deadline (P3).
- Operating guide [docs/OPERATIONS.md](docs/OPERATIONS.md) for Debian 13 ARM64 (P9.07, P9.08).

### Compatibility

| Platform | Status |
|---|---|
| .NET 10 runtime 10.0.14 or newer (`Microsoft.NETCore.App`) | Required. The library and the CLI were tested on runtime 10.0.13 (Windows, hosted CI, NanoPi R76S); 10.0.14 on the target is checked in P9.10 |
| Debian 13 (trixie), `linux-arm64`, NanoPi R76S | Verified per phase on the target: P1.10 to P7.11 in [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md), P8.09 in [its record](docs/p8/P8_09_INTEGRATED_VERIFICATION.md) and P3.11 in [its record](docs/p3/P3_11_INTEGRATED_VERIFICATION.md); the release acceptance on the candidate follows in P9.10 |
| Windows x64 | Development platform; tested, not a release target |
| GitHub-hosted `ubuntu-x64`, `windows-x64`, `debian13-arm64` container | CI only; never counted as target evidence |

Build: .NET SDK 10.0.402 (pinned). The update to SDK 10.0.403 is an open item of the user, to be made in its own pull request after the release (RL-09 A).

### Data sources and licences

- The MIT licence covers the code only, not weather data or websites.
- DWD data (observations, MOSMIX, catalogs) are open data under CC BY 4.0; keep the attribution "Deutscher Wetterdienst (DWD)" when republishing. The DWD open-data server stores the requesting IP address for at most 7 days.
- OsthessenWetter publishes no licence for its values and forbids reuse in other publications without the author's consent: production use and redistribution of its values are not accepted until the operator's consent or other clarified conditions are recorded (OW-14); `PublicationPermitted` stays `false` in the example configuration.

### Known limitations

| Limitation | Owner |
|---|---|
| Not published on any feed; repository private, no repository URL in the package metadata (RL-19 A) | The user |
| Runtime 10.0.14 on the target and the release acceptance on the NanoPi R76S are still open (P9.10) | P9.10, the user (runtime installation) |
| TLS and authentication were verified against one broker product (Mosquitto 2.1.2) with a self-signed test CA given in `Tls.CaFile` (P9.09); the system trust store, a public CA and an ACL denial of a single message were not shown | Recorded |
| MQTT 3.1.1: a broker can acknowledge a message it then drops; only 5.0 reports such a refusal (`CLI-MQTT-MESSAGE-REFUSED`) | Recorded (CL-13) |
| After a cancellation or an expired deadline the broker disconnect is bounded by `ConnectTimeout` and runs outside the command token | Recorded |
| A Windows system shutdown (`CTRL_SHUTDOWN_EVENT`) is not tested; Windows is not a release target (RL-14 A) | Recorded |
| The package is not byte-identical across builds: NuGet writes entry timestamps of four package-metadata members at pack time; every packed file is identical, compared member by member (RL-03 A) | Recorded |
| The CLI archive is not reproducible; acceptance evidence always names the one stored archive by its SHA-256 | Recorded |
| Coarse XML cancellation: cancellation is observed before serialization and during the copy to the destination, not inside the synchronous `XmlSerializer`; documents are produced into a buffer first, so the MQTT payload bound limits message acceptance, not peak memory (C-10) | Recorded |
| Builders and root validation implement the same rules separately (guarded by equivalence tests) | Recorded |
| MOSMIX_S costs one complete download (about 36 MB) per parse; schedule it at most once per hourly run | Recorded (operating guide) |
| MOSMIX elements not exported in v1 (probabilities, longer totals and gusts, weather codes, cloud cover, visibility, sunshine); MOSMIX `pressure_reference` is `unknown`; the first interval period may start before `issued_at` | Recorded decisions of P7 |
| OsthessenWetter: only the documented page revision is supported; the date is inferred (`estimated_date`); production use and redistribution of its values are not accepted (OW-14) | The user (OW-14) |
| The MOSMIX_S evidence refresher (P4) has no CRC-32 member check; only the forecast paths check it (P7.04) | Recorded; a P4 amendment only by a new decision |
| Consumers that do not use MOSMIX forecasts still register its four option and endpoint types; option failures surface on first resolution, not at `ValidateOnBuild` | Recorded (README) |
| Schema compatibility: consumers that validated against the committed v1 schema of P2 must take the schema packaged with this version (C-105); exhaustive switches over `IntervalAggregation` must handle `maximum` and `sum` (C-121) | Consumers (README) |
| Resource evidence: MOSMIX_S duration and peak memory were measured once with a synthetic workload on an idle NanoPi; the live parses are single snapshots on Windows (MF-17) | Recorded |
| DI under load: shown only for the tested mix of P8.07; other loads and long runs are not shown | P9.10 (long run), recorded |
| The public API inventory does not record nullable annotations (P9.05) | The user |
| `debian13-arm64` in the hosted CI is a container on GitHub's runners, not target evidence | Recorded |
| The example CLI's offline fixture host is not covered by the CLI gate's gated-input list (P9.07, open decision) | The user |
