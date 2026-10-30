# WeatherParser

The phase status (acceptances, merges and the binding phase sequence) is kept in [PHASE_STATUS.md](PHASE_STATUS.md).

WeatherParser is primarily a C# NuGet library for DWD/OsthessenWetter observations and MOSMIX forecasts (implemented; not yet published). The published product is the package containing parsers, output entities, a generic MQTT serializer and DI integration. The CLI is only a usage example consuming that package.

## NuGet primary product and example CLI

The intended published product is a reusable NuGet library. It contains DWD/OsthessenWetter/MOSMIX parsers with required retrieval/normalization, public output entities, a generic MQTT serializer and dependency-injection integration. The CLI is only a usage example and consumes a versioned package reference; other applications can use the package without the CLI.

Each parser receives its required source URLs explicitly as constructor parameters, preferably typed as `Uri`. Name multiple endpoints separately. DI integration forwards consumer-supplied endpoints to the corresponding constructors, without invented built-in sources or global URI singletons. HTTP, time and other services remain constructor-injected. The library does not independently read CLI arguments or application-specific configuration files/environment variables.

The MQTT serializer is source-independent: it serializes suitable output entities and creates a message containing topic, JSON payload, QoS and retain setting. Topics/templates and their mapping to message types come from consumer configuration, not hardcoded weather paths. Validate resolved publication topics; missing mappings and unresolved placeholders produce defined failures. Existing `haus/weather/...` topics are configurable examples only. Serialization and broker transport are separate; serialization needs neither a broker connection nor credentials.

Provide documented DI registration for library parsers, serializer and required services. Consumers supply endpoints, topic configuration and external dependencies. Every doing class has an interface; output entities remain dedicated `[Serializable]` types. Preserve testability, UTC and JSON/XML compatibility.

The example CLI owns arguments, application configuration, single-command process execution, exit codes, file/stdout export and optional MQTT transmission. It accesses parsing only through the package, never copied implementations. Acceptance prioritizes the public API, package versioning and independent consumption. The local test package `WeatherParser.Parsing` (version in `Directory.Packages.props`) was first built and consumed in [P1.04](docs/p1/P1_04_PACKAGE_CONSUMER.md). Public package identity, feed and publication procedure remain release decisions; no public package publication is claimed.
## Languages

English is the sole maintained language for project documentation and instructions. Use one canonical English file at each standard unsuffixed path; do not maintain German or `.en.md` counterparts. Preserve requirement IDs, commands, data fields, units, examples and original source names. This decision dated 2026-10-19 supersedes the earlier bilingual policy.

**All code comments must be written in English**, including inline/block comments, XML documentation comments, and comments in scripts and configuration examples. All program-generated runtime messages must be English. The binding MIT license text in `LICENSE` remains in English.

## Project status

As of 2026-10-20, the solution contains the library, source-reference tests, isolated package-consumer tests, the coverage tooling and the export-schema generator. The library implements parser-endpoint validation, the `AddWeatherParsing` DI entry point, the export entities of the five export roots (envelope, CDC station list, MOSMIX location list, observation dataset, forecast run), the JSON and XML serializers, the observation-dataset and forecast-run builders, UTC source-time normalization, unit conversion and output rounding, and the output schema `schemas/export/v1/weatherparser-export-v1.xsd`, which the package contains. The contract is recorded in [P2_EXPORT_CONTRACT.md](docs/p2/P2_EXPORT_CONTRACT.md); its version 1 is frozen since the P2 acceptance on 2026-10-20 (only additive optional changes, C-03). Test and coverage evidence is kept in the [P1 records](docs/p1/) and the [P2 verification record](docs/p2/P2_VERIFICATION.md). Since P4.09 (2026-10-23) it also selects CDC stations and MOSMIX forecast locations around a reference point (see [Station and location selection](#station-and-location-selection-library)). Since P5.07 (2026-10-25) it also parses the DWD 10-minute observations of one CDC station into an observation dataset (see [DWD observations](#dwd-observations-library)). Since P6.07 (2026-10-26) it also parses the OsthessenWetter measurement page into one observation dataset per matching station (see [OsthessenWetter observations](#osthessenwetter-observations-library)). Since P3 (2026-10-30) the repository also contains the [example CLI](#example-cli) with its five commands, JSON/XML output and the MQTT broker transport, verified offline with local fake sources and, on Windows, against a local test broker; its live and target-hardware runs follow in P3.11. No public package publication is claimed. Since P7 (2026-10-28) the library also contains the MOSMIX_L and MOSMIX_S forecast parsers (`IMosmixLParser`, `IMosmixSParser`) with their retrieval sources, the secure KMZ/KML reader, the element mapping and the shared parse pipeline. Since P8 (2026-10-29) the library also contains the generic MQTT message serializer (`IMqttMessageSerializer`) with consumer-configured topic templates, topic and placeholder validation, QoS and retain, without any broker access (see [MQTT serializer](#mqtt-serializer-library)).

## Workflow role and execution

The example CLI is an independently callable component of the overall weather-data and automation workflow. Its responsibilities are retrieval, parsing, station selection, normalization and the requested output. Downstream storage, display and other actions belong to other components.

**Each invocation executes exactly one command and then exits.** Scheduling and retries across invocations are external responsibilities, for example in n8n or cron. WeatherParser has no built-in scheduler and does not run as a persistent service. One command may return results for multiple stations. The CLI must support noninteractive execution, meaningful exit codes and separate data and log output.

Example workflows:

- n8n or cron → WeatherParser → MQTT → dashboard or other consumers.
- External workflow → WeatherParser → JSON/XML file → downstream processing.

There is no hard dependency on n8n; other programs and shell scripts can invoke the component.

## Target platform

Binding platform policy (2026-10-14): .NET version 10 or newer and Debian version 13 Trixie or newer. The initial library/example target framework is `net10.0`; the minimum architecture is ARM64 (`linux-arm64`), particularly NanoPi R76S. Add newer stable .NET/Debian releases to the verified compatibility matrix; previews are not automatically guaranteed. The pinned SDK and the verified toolchain are listed in [DEPENDENCIES.md](DEPENDENCIES.md), the developer setup in [CONTRIBUTING.md](CONTRIBUTING.md) and the verified platforms in [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md); extra ARM variants and the deployment model remain undecided. Verify the library, actual package consumer and example on Debian 13/ARM64 with .NET 10. Distinguish platform requirements from verified compatibility; never claim runtime success without documented tests.

Sources (checked 2026-10-14): [.NET support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [.NET 10 OS matrix](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md), [Debian releases](https://www.debian.org/releases/).

## Example CLI

The example CLI `WeatherParser` (project `examples/WeatherParser.Cli`, assembly `WeatherParser.dll`) shows how an application consumes the package: it references `WeatherParser.Parsing` as a versioned package and contains no copied library code. Its contract is the [P3 CLI contract](docs/p3/P3_CLI_CONTRACT.md) (CL-01 to CL-22, approved on 2026-10-29). All commands, output modes, the MQTT transport and the cancellation behavior are implemented. The tests run offline with local fake sources and a fake broker endpoint on Windows and in the hosted CI jobs (Windows x64, Ubuntu x64, Debian 13 ARM64); the broker integration tests ran only on Windows, against a local Mosquitto without TLS and without credentials, so TLS and authenticated connections are not yet verified against a real broker ([P3 verification record](docs/p3/P3_VERIFICATION.md)). The runs against the live sources and on the NanoPi R76S follow in P3.11. Build and run it as described in [CONTRIBUTING.md](CONTRIBUTING.md#example-cli-examplesweatherparsercli-p3); after a Release build it starts with `dotnet <output>/WeatherParser.dll`.

```text
WeatherParser --<command> [options]
```

Each invocation runs exactly one command and then exits with an exit code (REQ-006). The CLI has no scheduler, no service mode and no retries across invocations; schedule it externally (see [Scheduling](#scheduling)). It reads nothing from standard input.

### Commands and options

Commands and options are written with two leading dashes and are matched without regard to case; each option is given at most once, and a value never starts with `--`. Exactly one command is required.

| Command | Required options | Optional options |
|---|---|---|
| `--SelectDWD` | `--Latitude`, `--Longitude`, `--Altitude`, `--MaxDistance`, `--MaxAltDifference` | – |
| `--ParseDWD` | – | `--StationID` |
| `--ParseOstHessen` | `--StationNamePattern` | – |
| `--SelectMOSMIX` | `--Latitude`, `--Longitude`, `--Altitude`, `--MaxDistance`, `--MaxAltDifference` | `--RefreshMosmixSEvidence` |
| `--ParseMOSMIX` | `--Product`, `--MOSMIXID` | – |

- `--SelectDWD` finds CDC stations around a reference point; `--SelectMOSMIX` finds MOSMIX forecast locations with the same filters in the MOSMIX catalog. `--Latitude` and `--Longitude` accept decimal degrees or degrees, minutes and seconds with hemisphere (one argument, for example `"50° 33′ 18″ N"`); `--Altitude` and `--MaxAltDifference` are metres, `--MaxDistance` kilometres, all parsed independently of the culture. An empty selection is a success.
- `--ParseDWD` parses the 10-minute observations of one DWD station: `--StationID` (five digits with leading zeros) or, without it, `Dwd.ReferenceStationId` of the configuration. Without both the command fails with `CLI-DWD-STATION-MISSING` before any request; the CLI never picks a station itself. A given `--StationID` always takes precedence, also an empty value (for example an unset shell variable): it fails with `STATION-ID-INVALID` (exit code 2) before any request instead of falling back to the configuration.
- `--ParseOstHessen` parses every OsthessenWetter station whose name matches the pattern (`*` and `?`); the coordinates and altitude of each station come from `OsthessenWetter.Stations`.
- `--ParseMOSMIX` parses the latest forecast run of one MOSMIX location; `--Product` is `MOSMIX_S` or `MOSMIX_L` (no fallback to the other product).
- `--RefreshMosmixSEvidence` first refreshes the stored MOSMIX_S evidence index (a download of the complete MOSMIX_S file, bounded by `Mosmix.Catalog.MosmixSRefreshDeadline`), then selects; a failed refresh fails the command with the library's ID and no selection runs. Without it the selection uses the stored index: without a usable index it fails with `MOSMIX-S-EVIDENCE-MISSING` before any request (exit code 1); an index older than `Mosmix.Catalog.MosmixSEvidenceMaxAge` is still used and logs one `MOSMIX-S-EVIDENCE-STALE` warning (CL-17). Run the selection with the flag once to create the index.

Station IDs, MOSMIX IDs and patterns are checked by the library before any request; such a failure is a usage error (exit code 2) with the library's ID.

### Global options

| Option | Meaning |
|---|---|
| `--Output mqtt\|json\|xml` | Output mode; default `mqtt` |
| `--OutputFile <path>` | Write the JSON or XML document to a new file (only with `json` or `xml`); without it the document goes to standard output |
| `--Config <path>` | Configuration file; otherwise the environment variable `WEATHERPARSER_CONFIG` |
| `--Verbose` | Also log debug messages |
| `--Help` | Show the help, alone or with one command, and exit with 0 without reading a configuration |

```text
WeatherParser --SelectDWD --Latitude 50.5549 --Longitude 9.6738 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200 --Output json
WeatherParser --SelectDWD --Latitude "50° 33′ 18″ N" --Longitude "9° 40′ 26″ E" --Altitude 300 --MaxDistance 50 --MaxAltDifference 200
WeatherParser --ParseDWD --StationID <DWD-STATION-ID> --Output json --OutputFile weather.json
WeatherParser --ParseDWD
WeatherParser --ParseOstHessen --StationNamePattern "Fulda*" --Output xml --OutputFile weather.xml
WeatherParser --SelectMOSMIX --Latitude 50.5549 --Longitude 9.6738 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200 --RefreshMosmixSEvidence
WeatherParser --ParseMOSMIX --Product MOSMIX_L --MOSMIXID <MOSMIX-ID> --Output json
WeatherParser --ParseMOSMIX --Product MOSMIX_S --MOSMIXID <MOSMIX-ID>
```

The example coordinates show the two input formats and are not exactly equivalent; the pattern is illustrative and does not guarantee a match.

### Output

- **JSON and XML** (`--Output json|xml`): one complete document per invocation, UTF-8 without byte-order mark, produced by the library's serializers from the same entities as the MQTT payloads. The selection commands write their standalone list (`cdc_station_list`, `mosmix_location_list`); the parse commands write one `export_document` with the observations or forecast runs and an `errors` list for failed items (a failed station with its `source`). A command-level failure writes no document. With `--OutputFile`, the destination must not exist and its directory must exist (checked before any request); the document is written to a temporary file `<destination>.<random>.tmp` in the same directory and then moved to the destination without overwriting, so a destination file is always complete. After a failure or cancellation no destination appears and the temporary file is deleted; if that deletion fails, a `CLI-OUTPUT-FAILED` warning names the remaining temporary file, which then has to be removed by hand. The document is held in memory completely before it is written. JSON and XML mode do not use the broker settings, read no credentials and never contact a broker; the `Mqtt` section is still read as part of the configuration file, so it must be well-formed.
- **MQTT** (`--Output mqtt`, default): one message per observation dataset, per complete forecast run or per station list, serialized by the library's MQTT serializer with the configured topic templates (see [Topic templates and placeholders](#topic-templates-and-placeholders)), QoS 1, retained for observations and forecasts, not retained for station lists. The topic mapping of the command's message type is checked before any request (`MQTT-MAPPING-MISSING` → `CLI-CONFIG-INVALID`). The broker connection opens only after the results exist; a message counts as published only after the broker's acknowledgment with a success reason code. After the last message one `CLI-PUBLICATION-SUMMARY` line names the counts published, failed and not attempted.

Logs always go to standard error, never to standard output.

### Configuration

The configuration is one JSON file, given by `--Config <path>` or, without it, the environment variable `WEATHERPARSER_CONFIG`; without both the command fails with `CLI-CONFIG-MISSING`. Member names are the property names of the library's option types and are case-sensitive; unknown members, comments and trailing commas are rejected (`CLI-CONFIG-INVALID`); an absent member keeps the library default. The committed example `examples/weatherparser.example.json` shows every section with placeholder hosts (`example.invalid`); copy it to a local file outside the repository or to an ignored name (`weatherparser.json`, `*.local.json`) and set the real endpoints there. Its file paths (`EvidenceCacheDirectory`, `CredentialsFile`) are Linux paths for the Debian target; on Windows use absolute Windows paths.

| Section | Members | Used by |
|---|---|---|
| `Dwd` | `ReferenceStationId`; `Catalog` (the CDC station-list and directory URIs and limits of `CdcCatalogEndpoints`); `Observation` (`DwdObservationOptions`, for example `ObservationDeadline`) | `--SelectDWD`, `--ParseDWD` |
| `OsthessenWetter` | `PageUri`, `PageDeadline`, `Stations` (name, latitude, longitude, optional `AltitudeM`), `PublicationPermitted` | `--ParseOstHessen` |
| `Mosmix` | `Catalog` (`MosmixCatalogEndpoints`, including `MosmixSRefreshDeadline` and the evidence cache settings; `EvidenceCacheDirectory` must be an absolute path to an existing, writable directory); `L` (`SingleStationsDirectory`, `ParseDeadline`, `MaxRunAge`); `S` (`AllStationsUri`, `ParseDeadline`, `MaxRunAge`); `MaxRunAge` is required for `--ParseMOSMIX` of that product (greater than zero, at most 240 hours) | `--SelectMOSMIX`, `--ParseMOSMIX` |
| `Mqtt` | `Broker` (below); `Topics` with `Observation`, `Forecast`, `CdcStationList`, `MosmixLocationList`, each only with `TopicTemplate`; `MaxPayloadBytes` | MQTT mode only |
| `Limits` | `ConnectTimeout` (default 10 s, 1 s to 2 min), `PublishTimeout` (10 s per attempt, 1 s to 2 min), `MaxPublishAttempts` (3, 1 to 5), `RetryDelay` (1 s, 0 to 30 s), `CommandDeadline` (10 min, 1 min to 1 h), `ShutdownGracePeriod` (5 s, 1 s to 30 s) | every command |
| `Http` | `UserAgent` (1 to 200 printable ASCII characters, sent exactly as written; default `WeatherParser/<version>`) | every retrieving command |

The library validates its own settings before any request (for example `ENDPOINT-INVALID`, `OSTHESSEN-OPTIONS-INVALID`, `MOSMIX-OPTIONS-INVALID`); such an ID is logged unchanged with `CLI-CONFIG-INVALID` and exit code 2. QoS and retain are not configurable. HTTP: one client per process without automatic redirects (a redirect is a retrieval failure), without automatic decompression and without a client-side timeout; every request is bounded by the library's own timeouts and deadlines and by `Limits.CommandDeadline`.

### MQTT broker and credentials

`Mqtt.Broker` has `Host` (required in MQTT mode), `Port` (default 8883 with TLS, 1883 without), `ProtocolVersion` (`5.0`, default, or `3.1.1`), `ClientId` (1 to 23 letters and digits; default `weatherparser` followed by 10 random hexadecimal characters per invocation), `KeepAlive` (default 30 s, 5 s to 5 min), `CredentialsFile`, `Tls.CaFile` and `AllowUnencrypted`. The client is MQTTnet 5.2.0.1603; every connection starts with a clean session.

- **TLS** is on by default and validates the certificate chain and the host name; there is no setting that disables the validation. Without `Tls.CaFile` the system trust store is used; with it (a PEM file) that CA is the only trust anchor. `AllowUnencrypted: true` turns TLS off and logs one warning per invocation; use it only on a trusted local network.
- **Credentials** come either from the environment variables `WEATHERPARSER_MQTT_USERNAME` and `WEATHERPARSER_MQTT_PASSWORD` (both or neither) or from the JSON file named by `Mqtt.Broker.CredentialsFile` with exactly the members `Username` and `Password`; both sources at once fail with `CLI-CREDENTIALS-INVALID`. On Linux the credential file must be owned by the user the CLI runs as (the effective user ID) and have no permission bits for group or others (for example mode `0600`), otherwise the command fails with `CLI-CREDENTIALS-INVALID` before any request or broker connection ([CL-06](docs/p3/P3_CLI_CONTRACT.md)); the CLI opens the file once, checks the owner and mode of the opened file and reads the same file, so it cannot be replaced between the check and the read. On Windows no owner or permission check is made. Credentials are never accepted as arguments, never written into the configuration file, never logged and are read only in MQTT mode. Name the file `*.credentials.json` (ignored by Git) or keep it outside the repository.
- **Attempts**: the first connection gets at most `MaxPublishAttempts` attempts, each bounded by `ConnectTimeout`; a refused connection (for example wrong credentials) is final. Each message gets at most `MaxPublishAttempts` attempts, each bounded by `PublishTimeout`, with `RetryDelay` in between and one reconnection when the connection was lost; a refusal by the broker (a failure reason code) is final; an acknowledged message is never sent again. A retried QoS 1 message may reach subscribers twice.
- **MQTT 3.1.1 limitation**: a 3.1.1 broker acknowledges without a reason code, so it may acknowledge a message that it then drops (for example for a missing permission); with 5.0 such a refusal is reported as `CLI-MQTT-MESSAGE-REFUSED`. Use 5.0 where the broker supports it.

Give the broker user of the CLI only the publish permissions for its configured topics.

### OsthessenWetter values

The source conditions of OsthessenWetter are not clarified (OW-14). `--ParseOstHessen --Output mqtt` therefore fails with `CLI-OSTHESSEN-PUBLICATION-NOT-PERMITTED` (exit code 2) before any request unless `OsthessenWetter.PublicationPermitted` is `true`. Set it only after the operator's consent or other clarified conditions are recorded. JSON and XML output stay possible for the local user; the values must not be redistributed.

### Exit codes

| Exit code | Meaning |
|---|---|
| `0` | Complete success, also an empty selection and `--Help` |
| `1` | Complete failure at run time: no requested item succeeded (retrieval, command-level, output, connection or publication failure, an expired command deadline without a published message, an unexpected failure) |
| `2` | Usage or configuration error, detected before any retrieval |
| `3` | Partial failure: at least one requested item was exported or published and at least one failed |
| `4` | Cancelled by SIGINT, SIGTERM or Ctrl+C, also when some items succeeded before the signal |

An item whose output or publication failed counts as failed. Notices (for example `SELECTION-EMPTY`, `MOSMIX-RUN-STALE`) are logged and do not change the exit code.

### Error IDs

Every log line carries an ID. The CLI's own IDs are listed below; the IDs of the library (for example `STATION-ID-INVALID`, `OSTHESSEN-PAGE-UNAVAILABLE`, `MQTT-MAPPING-MISSING`) are passed through unchanged, in the logs and in `errors[].error_id` of the export document.

| Error ID | Level | Meaning |
|---|---|---|
| `CLI-ARGUMENT-INVALID` | Error | Unknown, repeated or misplaced option, missing or invalid value (exit code 2) |
| `CLI-COMMAND-MISSING` | Error | No command given (exit code 2) |
| `CLI-COMMAND-CONFLICT` | Error | More than one command given (exit code 2) |
| `CLI-CONFIG-MISSING` | Error | No configuration file given, or the file does not exist (exit code 2) |
| `CLI-CONFIG-INVALID` | Error | The configuration is unreadable, malformed or out of range, or the library rejects a setting (exit code 2) |
| `CLI-CREDENTIALS-INVALID` | Error | Broker credentials missing, incomplete, given twice or not protected (exit code 2) |
| `CLI-DWD-STATION-MISSING` | Error | `--ParseDWD` without `--StationID` and without `Dwd.ReferenceStationId` (exit code 2) |
| `CLI-OSTHESSEN-PUBLICATION-NOT-PERMITTED` | Error | `--ParseOstHessen` in MQTT mode without `OsthessenWetter.PublicationPermitted` (exit code 2) |
| `CLI-OUTPUT-EXISTS` | Error | The output file exists (exit code 2), or was created while the command ran (exit code 1) |
| `CLI-OUTPUT-FAILED` | Error, Warning | Invalid path or missing directory (exit code 2), write failure (exit code 1); a warning names a temporary file that could not be deleted |
| `CLI-MQTT-CONNECT-FAILED` | Error, Warning | No connection to the broker after the attempts, or refused; a warning per failed attempt |
| `CLI-MQTT-PUBLISH-FAILED` | Error, Warning | A message was not published (connection lost or not re-established); a warning per failed attempt |
| `CLI-MQTT-PUBLISH-TIMEOUT` | Error, Warning | No acknowledgment within `PublishTimeout` in the last attempt; a warning per failed attempt |
| `CLI-MQTT-MESSAGE-REFUSED` | Error | The broker refused a message with a failure reason code (MQTT 5.0) |
| `CLI-CANCELLED` | Error, Warning | A shutdown signal cancelled the command (exit code 4); a warning when the signal arrives |
| `CLI-DEADLINE-EXCEEDED` | Error, Warning | `Limits.CommandDeadline` expired; a warning before the retrieval when it is shorter than the command's library deadline |
| `CLI-UNEXPECTED` | Error | An unexpected failure, for example a message with an unexpected QoS or retain flag (exit code 1) |
| `CLI-PUBLICATION-SUMMARY` | Information | The counts published, failed and not attempted after an MQTT publication |

Log lines have the form `<UTC time> UTC <LEVEL> <ID> command=<command> [source=<source>] <message>`, one line per entry (line breaks in a message are replaced by spaces), for example `2026-10-30T06:00:00 UTC ERROR CLI-CONFIG-MISSING command=ParseDWD No configuration file given; use --Config <path> or the environment variable WEATHERPARSER_CONFIG.` A line without an ID shows `-`. Exceptions appear as type and message, never with a stack trace; credentials and configuration objects are never logged. The minimum level is Information; `--Verbose` adds Debug.

### Cancellation and deadlines

SIGINT, SIGTERM and Ctrl+C cancel the running command through one token passed to every library, output and transport call: the temporary file is deleted (or named in a warning if it cannot be deleted), the publication summary is logged, `CLI-CANCELLED` is logged and the exit code is 4; an interrupted export or publication is never reported as successful. A signal that arrives only after the output is complete (the output file moved into place, the document written to stdout, or every message acknowledged by the broker, for example during the final broker disconnect) interrupts nothing: the signal's `CLI-CANCELLED` warning is logged, but the completed result and its exit code stay unchanged. A second signal, or the end of `Limits.ShutdownGracePeriod` after the first, ends the process at once with 4. On Windows, .NET delivers a system shutdown (`CTRL_SHUTDOWN_EVENT`) as SIGTERM; the CLI then holds the shutdown event until its process ends, so that the command can finish its cleanup. `Limits.CommandDeadline` bounds retrieval, serialization, output and publication together; when it expires, `CLI-DEADLINE-EXCEEDED` is logged and the exit code is 1, or 3 when messages were already published. The library's own deadlines stay in force; a command deadline shorter than the command's library deadline is reported as a warning before the retrieval. One cleanup step runs after a cancellation or an expired deadline: an open broker connection is closed normally, bounded by `Limits.ConnectTimeout`; the process can therefore end up to that long after the command deadline (a second signal or the grace period ends it at once).

### Scheduling

Run the CLI from a scheduler and evaluate its exit code; no scheduler has to be installed for WeatherParser itself (REQ-007, REQ-008). Example cron entry (every 10 minutes, placeholder paths) that keeps the log and reports every exit code other than 0 to the system log:

```text
*/10 * * * * WEATHERPARSER_CONFIG=/etc/weatherparser/weatherparser.json dotnet /opt/weatherparser/WeatherParser.dll --ParseDWD >> /var/log/weatherparser.log 2>&1; rc=$?; [ $rc -eq 0 ] || logger -t weatherparser "--ParseDWD ended with exit code $rc"
```

In n8n, an "Execute Command" node can run `dotnet /opt/weatherparser/WeatherParser.dll --ParseDWD --Output json`, read the JSON document from standard output and branch on the exit code (0 success, 3 partial, 1, 2 or 4 failure). Choose the interval per source: DWD 10-minute data every 10 minutes at most, `MOSMIX_L` after its runs at 03, 09, 15 and 21 UTC, `MOSMIX_S` hourly, and a MOSMIX_S evidence refresh rarely (it downloads the complete MOSMIX_S file).

## JSON and XML export

All five commands of the [example CLI](#example-cli) support `--Output mqtt|json|xml`, with MQTT as the default. JSON and XML export require no MQTT broker. `--OutputFile <path>` writes a UTF-8 file; without a path, the complete document goes to standard output. Logs remain on standard error. Existing files are never overwritten.

```text
WeatherParser --ParseDWD --StationID <DWD-STATION-ID> --Output json --OutputFile weather.json
WeatherParser --ParseOstHessen --StationNamePattern "Fulda*" --Output xml --OutputFile weather.xml
WeatherParser --SelectDWD --Latitude 50.5549 --Longitude 9.6738 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200 --Output json
```

Both export formats contain the same observation or forecast data and UTC timestamps as MQTT, including estimated-date markers. Multiple station results, station lists and forecast steps are grouped into one complete document. The library serializer MQTT payloads remain JSON.

### Export entities and serialization

Each functional export object type has its own strongly typed C# entity marked `[Serializable]`, including nested export objects. This includes station lists and entries, observation datasets, forecast runs and steps, and quality/period metadata. Reusable value entities may be shared.

JSON uses `System.Text.Json.JsonSerializer`; XML uses `System.Xml.Serialization.XmlSerializer`. Both formats and MQTT JSON use the same prepared export entities. Serializer-specific rules control field names, omitted values and UTC without a timezone suffix. Serializer services use constructor injection. The library implements these entities and both serializers (P2.02 to P2.08); the XML output schema is generated from the same mappings and ships in the package at `schemas/export/v1/weatherparser-export-v1.xsd` (P2.09).

## Shared observation data

All sources use the same data contract. Timestamps are always **UTC in `YYYY-MM-DDTHH:mm:ss` format, without `Z` or an offset**. Consumers must explicitly interpret this convention; local-time conversion happens only for display.

Quantities include temperature in °C, pressure in hPa and mmHg, relative humidity in %, wind direction in degrees, wind speed in km/h, illuminance in lux, solar irradiance in W/m² and rain intensity in mm/h. Missing values are omitted. `0` represents an actual measurement.

hPa and mmHg refer to the same original value and pressure reference. Lux and W/m² are separate quantities. Averaged values retain their measurement periods. `--ParseMOSMIX` produces forecasts separately, never as observations.

The inspected OsthessenWetter overview provides only a time of day per station. The project permits deriving the date from retrieval time and the source timezone `Europe/Berlin`, then converting to UTC and **marking the date as estimated**. Daylight-saving transitions and ambiguous times require explicit handling; the retrieval date does not prove that the observation is current.

## MOSMIX forecasts

The library parsers for both products are implemented and documented in [MOSMIX forecasts (library)](#mosmix-forecasts-library) (P7; verified on Windows, hosted CI and the NanoPi R76S in P7.11 and accepted by the user on 2026-10-28 on candidate `2f1115b`; it is complete: pull request #11 merged into `develop` on 2026-10-28 (merge commit `20b2ef0`)). The CLI commands `--SelectMOSMIX` and `--ParseMOSMIX` are part of the [example CLI](#example-cli).

`--SelectMOSMIX` uses the same location/filter parameters as `--SelectDWD` but searches the DWD MOSMIX station catalog. Results include MOSMIX ID, name, coordinates, altitude, distance, altitude difference and verified forecast elements. MOSMIX IDs are independent, potentially alphanumeric identifiers and are not derived from CDC observation-station IDs.

`--ParseMOSMIX --Product MOSMIX_S|MOSMIX_L --MOSMIXID <id>` retrieves the latest available forecast run for the location. Issue time (`issued_at`), retrieval time (`retrieved_at`) and forecast times (`forecasts[].valid_at`) remain separate and are output as UTC without a timezone suffix. Interval quantities retain their reference periods. Both product parsers are required; their element mappings and freshness limits are documented in [MOSMIX forecasts (library)](#mosmix-forecasts-library).

MQTT forecasts use `haus/weather/forecasts/<source>/state`; MOSMIX station lists use `haus/weather/stations/mosmix/selection`. The prefix is configurable. The example CLI configures the library serializer for one retained message per complete run, location and product; separate broker transport sends it. JSON and XML exports use the same normalized forecast contract.

```text
WeatherParser --ParseMOSMIX --Product MOSMIX_L --MOSMIXID <MOSMIX-ID> --Output json --OutputFile forecast.json
WeatherParser --ParseMOSMIX --Product MOSMIX_L --MOSMIXID <MOSMIX-ID> --Output xml --OutputFile forecast.xml
```

### Separate MOSMIX parsers and update frequency

The library provides two distinct parsers with separate interfaces: `MosmixSParser` / `IMosmixSParser` for `MOSMIX_S`, and `MosmixLParser` / `IMosmixLParser` for `MOSMIX_L`. Both receive their own endpoints and options through dependency injection and have independent configuration. Shared KML/KMZ helpers may be reused, but one switchable parser cannot replace the two public contracts. Missing data never triggers silent fallback to the other product.

| Product | DWD output frequency | Run times |
|---|---|---|
| `MOSMIX_S` | Hourly | A new product run each hour |
| `MOSMIX_L` | Four times daily | 03, 09, 15 and 21 UTC |

These describe provider cycles, not an internal scheduler or guaranteed file availability exactly at run time. Keep issue, retrieval and validity times separate. Read the actual forecast horizon from the file timeline; a new file does not prove all contained steps were recalculated. Define freshness limits and external retrieval plans per product.

The example keeps `--ParseMOSMIX` and additionally requires `--Product MOSMIX_S|MOSMIX_L` to select the library parser. Reject missing/unknown products before retrieval. `--SelectMOSMIX` keeps the existing geographic/altitude options and distinguishes known availability/elements per product; explicitly mark unknown availability. Output metadata, source identifiers and configured MQTT topics must distinguish S/L to prevent accidental retained-state replacement.

Source, checked 2026-10-14: [DWD procedure description, section 2.1.3/table 2.1](https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_verfahrenbeschreibung_gesamt.pdf?__blob=publicationFile&v=6). The cycles were rechecked against the files of 2026-10-26 in P7.02 ([P7 forecast contract](docs/p7/P7_FORECAST_CONTRACT.md), section 6); recheck them before release.

## Architecture and tests

- C# and .NET 10 or newer (initial TFM `net10.0`); pin SDK patch and package versions before implementation.
- Each class with executable logic (a “doing class”) implements a suitable interface. Dependencies are constructor-injected through these interfaces and wired centrally in the composition root. Pure data/export entities remain separate.
- Separate responsibilities for CLI, configuration, retrieval, parsing, station selection, normalization, serialization and MQTT.
- TDD follows Red → Green → Refactor, including bug fixes. Use MSTest, Moq and FluentAssertions; construct test objects in `[TestInitialize]` and call `VerifyAll()` for mocks in `[TestCleanup]`.
- Automated tests use synthetic fixtures and mocks, without mandatory live websites or production MQTT brokers.
- Code-block coverage (as defined above) must reach at least 80%. During initial bootstrap without a measurable target-branch baseline, only this minimum applies; target-branch comparison is not applicable. An unexpectedly missing, stale or incomparable baseline in subsequent operation remains an error. Documentation-only PRs may retain the same coverage for the block metric as the target branch; decreases are forbidden. Other PRs must strictly improve the block metric below a 100% baseline; an exact 100% baseline accepts an exact 100% result. Use current commit IDs, comparable collection conditions and unrounded counter ratios. Missing PR measurements, invalid counts and zero denominators cannot pass. Microsoft.CodeCoverage reports, including the code-block breakdown, are the basis for verification; Cobertura line-rate, branch-rate or any Cobertura-derived value must never be used as or substituted for the block metric; missing or unreadable native block counters fail. Verify metric mapping and report format against real reports in P1.

See the [development plan](DEVELOPMENT_PLAN.md) for phases, dependencies and acceptance gates. The [P1 detailed plan](PHASE_P1_PLAN.md) breaks the first implementation phase into work packages and tests. See [REQUIREMENTS.md](REQUIREMENTS.md) for detailed requirements and acceptance criteria, and [AGENTS.md](AGENTS.md) for contributor rules. Developer procedures (restore, build, tests, pack, coverage, local CI, Debian/ARM64) are in [CONTRIBUTING.md](CONTRIBUTING.md).

## Station and location selection (library)

The library selects CDC 10-minute stations (`ICdcStationSelector`) and MOSMIX forecast locations (`IMosmixLocationSelector`) around a reference point. `SelectAsync(SearchParameters parameters, CancellationToken cancellationToken)` returns a `CdcStationList` or `MosmixLocationList` (export roots of contract v1, serializable with `IJsonExportSerializer` / `IXmlExportSerializer`). Search parameters come from `ISearchParameterFactory.Create(latitude, longitude, altitudeM, maxDistanceKm, maxAltitudeDifferenceM)`; coordinate strings (decimal degrees or degrees, minutes and seconds with hemisphere) are parsed by `ICoordinateParser`.

### Consumer registration

```csharp
services.AddWeatherParsing();
services.AddLogging();                       // or any ILogger<T> registration
services.AddSingleton(new HttpClient { Timeout = Timeout.InfiniteTimeSpan });
services.AddSingleton(TimeProvider.System);
services.AddSingleton(new CdcCatalogEndpoints
{
    AirTemperatureStationList = new Uri("https://example.invalid/cdc/air_temperature/now/stations.txt"),
    AirTemperatureNowDirectory = new Uri("https://example.invalid/cdc/air_temperature/now/"),
    WindStationList = new Uri("https://example.invalid/cdc/wind/now/stations.txt"),
    WindNowDirectory = new Uri("https://example.invalid/cdc/wind/now/"),
    PrecipitationStationList = new Uri("https://example.invalid/cdc/precipitation/now/stations.txt"),
    PrecipitationNowDirectory = new Uri("https://example.invalid/cdc/precipitation/now/"),
    SolarStationList = new Uri("https://example.invalid/cdc/solar/now/stations.txt"),
    SolarNowDirectory = new Uri("https://example.invalid/cdc/solar/now/"),
});
services.AddSingleton(new MosmixCatalogEndpoints
{
    CatalogUri = new Uri("https://example.invalid/mosmix/stations.cfg"),
    MosmixLSingleStationsDirectory = new Uri("https://example.invalid/mosmix/single_stations/"),
    MosmixSAllStationsUri = new Uri("https://example.invalid/mosmix/MOSMIX_S_LATEST_240.kmz"),
    EvidenceCacheDirectory = "/var/lib/weatherparser/evidence",
    MosmixSEvidenceMaxAge = TimeSpan.FromHours(6),
});
```

- `AddWeatherParsing` registers every library service as a singleton but no `Uri`, endpoint options, `HttpClient`, `TimeProvider` or logger; the consumer supplies them and a missing registration fails at container validation (`ValidateOnBuild`).
- `services.AddLogging()` needs the `Microsoft.Extensions.Logging` package in the consuming application; the library itself depends only on `Microsoft.Extensions.Logging.Abstractions`.
- List and catalog endpoints are absolute HTTP(S) URIs used unchanged (a query is allowed); directory endpoints must end with `/` and have no query or fragment, otherwise `ENDPOINT-INVALID` at construction; per-station file names are appended to the directory.
- Limits per catalog: `MaxEvidenceCandidates` (default 100, 1 to 10000) and `SelectionDeadline` (default 120 s, greater than zero and at most 1 hour), validated at construction (`SELECTION-LIMIT-INVALID`); more candidates inside the search area than `MaxEvidenceCandidates` fail with `SELECTION-TOO-MANY-CANDIDATES` before any per-station request.

### HTTP transport

- The library makes one GET per file, no retries, bounded to 30 s per request and to 8 MiB per list or catalog, 1 MiB per per-station file and 64 MiB for the MOSMIX_S all-stations file.
- Handler settings (redirects, decompression, proxy, header limits) are the consumer's responsibility.
- Set `HttpClient.Timeout` to `Timeout.InfiniteTimeSpan` or above 30 s — a shorter client timeout is permitted and its expiry is reported as `CATALOG-TIMEOUT`.

### MOSMIX_S evidence cache

- MOSMIX_S availability comes from a stored index that `IMosmixSEvidenceRefresher.RefreshAsync(CancellationToken)` builds from the all-stations file.
- The consumer calls the refresh on its own external schedule (for example a cron job or an n8n workflow; neither is a dependency, the library has no scheduler).
- Settings `MosmixSAllStationsUri` (absolute HTTP(S) file URI), `EvidenceCacheDirectory` (absolute local path of a directory used only by the cache), `MosmixSEvidenceMaxAge` (required, no default, 1 minute to 365 days) and `MosmixSRefreshDeadline` (default 10 minutes, greater than zero and at most 1 hour); a missing or out-of-range setting fails at construction with `EVIDENCE-CACHE-SETTINGS-INVALID`, without file-system access.
- The directory must exist and be writable: a missing or non-writable directory is detected only when used, by the refresh as `EVIDENCE-CACHE-FAILED` and by a selection as a missing index (`MOSMIX-S-EVIDENCE-MISSING`).
- A selection without a usable index fails with `MOSMIX-S-EVIDENCE-MISSING` before any request.
- An index older than `MosmixSEvidenceMaxAge` is still used and one `MOSMIX-S-EVIDENCE-STALE` warning names its age and the limit.
- MOSMIX_L files are fetched per location inside the search area, HTTP 404 meaning `unavailable`.

### Error and notice IDs

Exceptions (`ErrorId`):
- `CoordinateFormatException` `COORDINATE-INVALID`
- `SearchParameterException` `SEARCH-PARAMETER-INVALID`
- `CatalogFormatException` `CATALOG-FORMAT-INVALID`, `CDC-EVIDENCE-INVALID`, `MOSMIX-EVIDENCE-INVALID`
- `CatalogRetrievalException` `CATALOG-RETRIEVAL-FAILED`, `CATALOG-TIMEOUT`, `CATALOG-TOO-LARGE`
- `SelectionLimitException` `SELECTION-TOO-MANY-CANDIDATES`, `SELECTION-DEADLINE-EXCEEDED`, `SELECTION-LIMIT-INVALID`
- `EvidenceCacheException` `EVIDENCE-CACHE-SETTINGS-INVALID`, `EVIDENCE-CACHE-FAILED`, `MOSMIX-S-REFRESH-DEADLINE-EXCEEDED`, `MOSMIX-S-EVIDENCE-MISSING`
- `ParserEndpointValidationException` `ENDPOINT-INVALID`

Notices (log event names):
- `SELECTION-EMPTY` (Information)
- `MOSMIX-LOCATION-NO-ALTITUDE`, `CDC-STATION-NO-ALTITUDE`, `CATALOG-RECORD-SKIPPED`, `CDC-STATION-CONFLICT`, `MOSMIX-S-EVIDENCE-STALE` (Warning)

A caller cancellation surfaces as `OperationCanceledException` and wins over the selection deadline.

### Source conditions and retrieval guidance

- The data come from the Deutscher Wetterdienst (DWD) open-data server [opendata.dwd.de](https://opendata.dwd.de/) under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) per the [DWD legal notice](https://www.dwd.de/EN/service/legal_notice/legal_notice_node.html).
- Consumers who republish selection results or other DWD-derived data (for example via MQTT or file export) must keep the attribution "Deutscher Wetterdienst (DWD)".
- DWD stores the requesting IP address for at most 7 days.
- A selection is a configuration-time operation — run it when a station or location is chosen or reviewed, not on a schedule, and if repeated at most once per day per search.
- The per-station evidence requests are part of that run and not intended for polling (project recommendation, not a provider guarantee).

## DWD observations (library)

The library parses the DWD 10-minute `now` observations of one explicitly given CDC station (`IDwdObservationParser`). `ParseAsync(string stationId, CancellationToken cancellationToken)` returns an `ObservationDataset` (export root of contract v1, serializable with `IJsonExportSerializer` / `IXmlExportSerializer`). The station ID has exactly five ASCII digits with leading zeros, for example `00044`; it is checked before any request, and the library never substitutes another or the nearest station. One call is one parse: the library reads the four CDC station lists, resolves the station and reads the station's `now` archive of every product whose list contains it. There is no scheduler and no retry; scheduling stays external. The example CLI runs it with `--ParseDWD` (see [Example CLI](#example-cli); verified with local fake sources, the live runs follow in P3.11).

### Consumer registration

The registration of [Station and location selection](#station-and-location-selection-library) is extended by the observation options:

```csharp
services.AddSingleton(new DwdObservationOptions
{
    ObservationDeadline = TimeSpan.FromSeconds(120),
});
```

- `DwdObservationOptions` must be registered by every consumer of `AddWeatherParsing`, also one that only selects stations, because the container validation (`ValidateOnBuild`) checks the parser's dependencies; `new DwdObservationOptions()` gives the default.
- `ObservationDeadline` (default 120 s) bounds one parse: it must be greater than zero and at most 1 hour, otherwise construction fails with `OBSERVATION-LIMIT-INVALID` (zero, negative values, `Timeout.InfiniteTimeSpan` and values above 1 hour are rejected); its expiry fails the parse with `OBSERVATION-DEADLINE-EXCEEDED`.
- The parser reuses the eight URIs of `CdcCatalogEndpoints`; its `SelectionDeadline` and `MaxEvidenceCandidates` do not apply to observations.
- At most eight requests per parse: the four station lists and at most four archives (`10minutenwerte_TU_<id>_now.zip`, `..._wind_...`, `..._nieder_...`, `..._SOLAR_...` below the `now` directories), at most four in parallel, with the bounds of [HTTP transport](#http-transport). An archive answering HTTP 404 is a missing product (`OBSERVATION-PRODUCT-MISSING`), and the other products are still used; every other retrieval or format failure fails the parse.

### Exported quantities

| DWD column | Export field | Conversion and period |
| --- | --- | --- |
| `TT_10` (°C, 2 m, instantaneous) | `temperature_c` | none; no period |
| `RF_10` (%) | `humidity_percent` | none; no period |
| `PP_10` (hPa at station altitude) | `pressure_hpa`, `pressure_mmhg` | `pressure_reference` `sensor`; both from one value; no period |
| `FF_10` (m/s, mean of the previous 10 minutes) | `wind_speed_kmh` | × 3.6; period of the previous 10 minutes |
| `DD_10` (degrees, mean of the previous 10 minutes) | `wind_direction_deg` | 360 exported as 0 (north); period of the previous 10 minutes |
| `RWS_10` (mm, sum of the previous 10 minutes) | `rain_rate_mm_h` | mm per 10 minutes × 6; period of the previous 10 minutes |
| `GS_10` (J/cm², sum of the previous 10 minutes) | `solar_irradiance_w_m2` | × 10000 to J/m², ÷ 600 s; period of the previous 10 minutes |

- Every quantity takes the newest row with a usable value and keeps its own `measured_at` (UTC, from `MESS_DATUM`); the dataset `measured_at` is the newest of them. There is no freshness filter: the archive of a station that stopped reporting still yields its old values, so consumers evaluate `measured_at`.
- `-999` in any spelling is a missing value and the quantity is omitted (`OBSERVATION-READING-OMITTED`); `0` is a real value.
- Wind direction is omitted when the wind speed of the same row is 0 (calm) or the direction is 0, because the DWD data write north as 360 and document no calm marker (a project assumption).
- `quality` is the DWD quality level `QN` of the row, unchanged: `1` only formal control, `2` controlled with individually defined criteria, `3` automatic control and correction ([DWD dataset descriptions](#data-sources)); data with `QN` 1 may contain significant errors.
- Not exported: `TM5_10`, `TD_10`, `RWS_DAU_10`, `RWS_IND_10`, `DS_10`, `SD_10`, `LS_10` (no export field) and `illuminance_lux` (no DWD 10-minute source).
- Limitations: the quality control of the `now` data is not yet completed, so values may still change; the time stamp of `RWS_10` is instrument dependent (with a Pluvio Ott gauge the physical interval ends about 5 minutes earlier) and is not corrected — the exported period is the nominal one.

### Error and notice IDs

Exceptions (`ErrorId`):
- `DwdStationException` `STATION-ID-INVALID` (before any request), `STATION-UNKNOWN` (the station is in no list; no archive request), `STATION-METADATA-CONFLICT` (the station's records in the lists differ, so its location cannot be stated)
- `ObservationLimitException` `OBSERVATION-LIMIT-INVALID`, `OBSERVATION-DEADLINE-EXCEEDED`
- reused from selection: `CatalogRetrievalException` `CATALOG-RETRIEVAL-FAILED`, `CATALOG-TIMEOUT`, `CATALOG-TOO-LARGE`; `CatalogFormatException` `CATALOG-FORMAT-INVALID`, `CDC-EVIDENCE-INVALID`; `ParserEndpointValidationException` `ENDPOINT-INVALID`
- `ObservationDatasetRejectedException` when no reading is usable (no `ErrorId`; `OmittedReadings` lists the omitted quantities with their reasons)

Notices (log event names, Warning): `OBSERVATION-PRODUCT-MISSING`, `OBSERVATION-READING-OMITTED` (quantity and reason, including `CalmWind` and `ZeroDirection`), `CDC-STATION-CONFLICT`, `CATALOG-RECORD-SKIPPED`. Log messages carry the station ID and the retrieval time in UTC, never measurement values.

A caller cancellation surfaces as `OperationCanceledException` and wins over the observation deadline, which wins over every other failure.

### Source conditions and retrieval guidance

- The attribution and privacy notes of [Source conditions and retrieval guidance](#source-conditions-and-retrieval-guidance) apply: data of the Deutscher Wetterdienst (DWD) under CC BY 4.0, republished observations keep the attribution "Deutscher Wetterdienst (DWD)", and DWD stores the requesting IP address for at most 7 days.
- The values are 10-minute values; DWD updates the `now` data at a frequency of less than one hour (DWD dataset descriptions).
- Project recommendation, not a provider rule (DWD documents no request-rate limit): schedule parses externally (for example cron or an n8n workflow; neither is a dependency) and parse a station at most once every 10 minutes.

## OsthessenWetter observations (library)

The library parses the OsthessenWetter measurement page (`IOsthessenWetterParser`). `ParseAsync(string stationNamePattern, CancellationToken cancellationToken)` returns an `OsthessenWetterParseResult`: `Datasets` holds one `ObservationDataset` (export root of contract v1, serializable with `IJsonExportSerializer` / `IXmlExportSerializer`) per usable matched station, and `StationFailures` lists every matched station without a dataset (`StationName`, `ErrorId`, `Reason`); both follow the order of the page's overview table. The pattern is matched case-insensitively against the whole overview station name: `*` matches any number of characters, `?` exactly one, every other character itself; it must not be empty or whitespace only and has at most 128 characters, and it is checked before any request. Every matching station is returned, never only the first. One call is one retrieval of the page; there is no scheduler and no retry, so scheduling stays external. The example CLI runs it with `--ParseOstHessen` (see [Example CLI](#example-cli); verified with local fake sources, the live runs follow in P3.11).

### Consumer registration

The registration of [Station and location selection](#station-and-location-selection-library) and [DWD observations](#dwd-observations-library) is extended by the OsthessenWetter endpoint and options:

```csharp
services.AddSingleton(new OsthessenWetterEndpoints
{
    PageUri = new Uri("https://example.org/messwerte/"), // the measurement page, see Data sources
});
services.AddSingleton(new OsthessenWetterOptions
{
    PageDeadline = TimeSpan.FromSeconds(60),
    Stations =
    {
        new OsthessenWetterStationMetadata { Name = "<overview station name>", Latitude = 50.55m, Longitude = 9.68m, AltitudeM = 300m },
    },
});
```

- `OsthessenWetterEndpoints` and `OsthessenWetterOptions` must be registered by every consumer of `AddWeatherParsing`, also one that does not use OsthessenWetter, because the container validation (`ValidateOnBuild`) checks the parser's dependencies; `new OsthessenWetterOptions()` gives the defaults without stations.
- `PageUri` must be an absolute HTTP or HTTPS URI without user information; otherwise resolving the parser fails with `OSTHESSEN-OPTIONS-INVALID`. Diagnostics name the endpoint and its host, never the query.
- `PageDeadline` (default 60 s) bounds one parse: it must be greater than zero and at most 1 hour, otherwise resolving the parser fails with `OSTHESSEN-OPTIONS-INVALID`; its expiry fails the parse with `OSTHESSEN-DEADLINE-EXCEEDED`. The request itself is also bounded by the 30-second request timeout of [HTTP transport](#http-transport): with a `PageDeadline` above 30 s, such as the default, a stalled request fails as `OSTHESSEN-PAGE-UNAVAILABLE` first; with a shorter `PageDeadline`, the deadline expires first and the parse fails with `OSTHESSEN-DEADLINE-EXCEEDED`.
- `Stations` supplies the location of each overview station, because the page shows no coordinates: `Name` is the overview name as shown on the page (matched exactly after Unicode normalization form C), `Latitude` from -90 to 90, `Longitude` from -180 to 180 (decimal degrees), `AltitudeM` optional (absent when null, never replaced by 0); names are unique. An invalid entry fails the resolution of the parser with `OSTHESSEN-OPTIONS-INVALID`, before any request. A matched station without an entry is a station failure `OSTHESSEN-STATION-METADATA-MISSING`.
- One GET per parse, at most 2 MiB, no retries. A non-success status (including 404), a transport failure, an oversized body and the request timeout fail the parse with `OSTHESSEN-PAGE-UNAVAILABLE`. The HTML is parsed with AngleSharp without scripts and without loading any resource.

### Exported quantities

| Page column | Export field | Time and period |
| --- | --- | --- |
| Overview `Temp.2m` (primary value, °C) | `temperature_c` | time of the overview row; no period |
| Overview `Luftfeuchte` (%) | `humidity_percent` | time of the overview row; no period |
| Wind table `Wind Ø 10 Min.` (km/h) | `wind_speed_kmh` | time of the wind row; period of the previous 10 minutes |

- Station ID: the overview name in lower case with ä, ö, ü and ß written as ae, oe, ue and ss and every other run of characters outside a–z and 0–9 replaced by one `-` (none at the start or end), for example `Müs-Hohhütte` → `mues-hohhuette`; `source` is `osthessenwetter:<station ID>`, and `location.name` keeps the original name. Two overview names with the same station ID fail the whole parse with `OSTHESSEN-STATION-ID-COLLISION`; all overview names are checked, not only the matched ones, so an ID never depends on the pattern.
- Times: the page shows only a time of day (`HH:mm Uhr`). The date is inferred in `Europe/Berlin` from `retrieved_at`, which is sampled once after the page has arrived; a displayed time more than 5 minutes later than the local retrieval time belongs to the previous day. Every reading and every dataset carries `timestamp_quality` `estimated_date`, every reading `source_timezone` `Europe/Berlin`. A displayed time in a daylight-saving gap or overlap cannot be resolved (`OSTHESSEN-TIME-UNRESOLVED`), and the readings of that row are not exported. Older displays cannot be detected and are not filtered: consumers evaluate `measured_at`.
- Values: a number with an optional minus sign and decimal point, read independently of the culture, followed by the column's unit. An empty cell is a missing value and the quantity is omitted (`OBSERVATION-READING-OMITTED`); any other text is unreadable (`OSTHESSEN-VALUE-UNREADABLE`). `0` is a real value. Output rounding as for all observations (0.1 °C, 1 %, 1 km/h, ties away from zero).
- The wind table is joined to the overview by name (case-insensitive, ß equal to ss, a trailing `;` ignored). A missing wind table or a changed wind header gives `OSTHESSEN-WIND-TABLE-MISSING` and no wind readings; a station with no, more than one or a deviating wind row gives `OSTHESSEN-WIND-ROW-UNMATCHED` and no wind reading, while the other stations keep theirs.
- Station failures: a station the page marks offline, even if values are shown (`OSTHESSEN-STATION-OFFLINE`); a station without metadata (`OSTHESSEN-STATION-METADATA-MISSING`); a station without any usable reading (`OSTHESSEN-STATION-NO-READING`). A station failure is never a dataset.
- There is no `quality` member, because the page shows no quality code.
- Not exported: the gusts, the 60-minute mean wind, the precipitation, the temperature change and trend, the daily maximum and minimum and every other section of the page.
- Limitation: only the page revision documented in the [OsthessenWetter contract](docs/p6/P6_OSTHESSEN_CONTRACT.md) is supported; a changed overview table fails the parse with `OSTHESSEN-FORMAT-CHANGED`.

### Error and notice IDs

Exceptions: `OsthessenWetterException` with `ErrorId` `OSTHESSEN-PATTERN-INVALID` (before any request), `OSTHESSEN-PATTERN-NO-MATCH`, `OSTHESSEN-PAGE-UNAVAILABLE`, `OSTHESSEN-DEADLINE-EXCEEDED`, `OSTHESSEN-FORMAT-CHANGED`, `OSTHESSEN-STATION-ID-COLLISION`, `OSTHESSEN-NO-USABLE-STATION` (matched stations, but none yields a dataset; `StationFailures` lists them) and `OSTHESSEN-OPTIONS-INVALID`.

Station failures (in `StationFailures` and as log event names): `OSTHESSEN-STATION-OFFLINE`, `OSTHESSEN-STATION-METADATA-MISSING`, `OSTHESSEN-STATION-NO-READING`.

Notices (log event names, Warning): `OSTHESSEN-WIND-TABLE-MISSING`, `OSTHESSEN-WIND-ROW-UNMATCHED`, `OSTHESSEN-VALUE-UNREADABLE`, `OSTHESSEN-TIME-UNRESOLVED`, `OBSERVATION-READING-OMITTED` (quantity and reason). Log messages carry the ID, the station name, the source `osthessenwetter` and the retrieval time in UTC, never measurement values, page text or the endpoint. Notices for the whole page are always logged, station notices only for matched stations.

A caller cancellation surfaces as `OperationCanceledException` and wins over the page deadline, which wins over every other failure.

### Source conditions and retrieval guidance

- OsthessenWetter publishes no licence for its measured values and no machine-readable interface. The site's imprint states that reproducing or using its texts and other content in other publications is not permitted without the author's express consent, and it gives no warranty for currency, correctness or completeness.
- **Production use and redistribution of the values, for example over MQTT, are not accepted by this project until the operator's consent or other clarified conditions are recorded** ([contract OW-14](docs/p6/P6_OSTHESSEN_CONTRACT.md)). The library is implemented and tested with synthetic pages only.
- Attribution: name the source "Osthessen-Wetter.de" wherever values are shown or republished.
- Project recommendation, not a provider rule (the site documents none): schedule parses externally (for example cron or an n8n workflow; neither is a dependency), retrieve the page at most once every 10 minutes, and set an identifying User-Agent on the consumer's `HttpClient` (the library sets none).

## MOSMIX forecasts (library)

The library parses the DWD MOSMIX point forecasts of one location (`IMosmixLParser` for `MOSMIX_L`, `IMosmixSParser` for `MOSMIX_S`). `ParseAsync(string mosmixId, CancellationToken cancellationToken)` returns a `MosmixParseResult`: `Run` holds one `ForecastRun` (export root of contract v1, serializable with `IJsonExportSerializer` / `IXmlExportSerializer`), `OmittedValues` the values the run builder omitted with their reasons, `NotExportedElements` the names of the file's elements that are not mapped, `AbsentElements` the mapped elements the file does not contain, `Freshness` (`Fresh` or `Stale`) and `RunAge` (`retrieved_at − issued_at`). The two parsers are separate services behind separate interfaces: neither takes a product parameter, and a failure of one product never falls back to the other ([P7 forecast contract](docs/p7/P7_FORECAST_CONTRACT.md)).

### Consumer registration

The registration of [Station and location selection](#station-and-location-selection-library), [DWD observations](#dwd-observations-library) and [OsthessenWetter observations](#osthessenwetter-observations-library) is extended by one endpoint and one options type per product:

```csharp
services.AddSingleton(new MosmixLForecastEndpoints
{
    SingleStationsDirectory = new Uri("https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/"),
});
services.AddSingleton(new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = TimeSpan.FromHours(8) });
services.AddSingleton(new MosmixSForecastEndpoints
{
    AllStationsUri = new Uri("https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_S/all_stations/kml/MOSMIX_S_LATEST_240.kmz"),
});
services.AddSingleton(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = TimeSpan.FromHours(2) });
```

- All four types must be registered by every consumer of `AddWeatherParsing`, also one that does not use MOSMIX forecasts, because the container validation (`ValidateOnBuild`) checks the parsers' dependencies. They are independent of the selection's `MosmixCatalogEndpoints`.
- `SingleStationsDirectory` is the `single_stations/` directory of `MOSMIX_L`: an absolute HTTP or HTTPS URI ending with `/`, without user information, query or fragment; the file of a location is composed beneath it as `<id>/kml/MOSMIX_L_LATEST_<id>.kmz`. `AllStationsUri` is the `MOSMIX_S` all-stations file, used unchanged (a query is allowed but never logged). An invalid URI fails the resolution of the source with `MOSMIX-OPTIONS-INVALID`. Diagnostics name the endpoint as scheme, host and path.
- `ParseDeadline` (`MOSMIX_L` default 60 s, `MOSMIX_S` default 5 minutes; greater than zero, at most 1 hour) bounds one parse including the download and the stream read; its expiry fails the parse with `MOSMIX-DEADLINE-EXCEEDED`. For `MOSMIX_S` the request bound of [HTTP transport](#http-transport) equals the parse deadline instead of the fixed 30 s, because the all-stations file (about 36 MB) is streamed to its end for every parse; for `MOSMIX_L` the 30-second request bound applies.
- `MaxRunAge` has no default and must be set (greater than zero, at most 240 hours); an unset value fails the resolution of the parser with `MOSMIX-OPTIONS-INVALID`. Guidance (not a default): `MOSMIX_S` 2 h, `MOSMIX_L` 8 h. A run whose age (`retrieved_at − issued_at`) is above the limit is still returned, with `Freshness` `Stale`, the wire member `run_age_exceeded` `true` and one `MOSMIX-RUN-STALE` warning naming the age and the limit; at or below the limit the run is `Fresh` and the member is `false`. The P7 parsers always set `run_age_exceeded`; the member is omitted only by producers that do not know it.
- The MOSMIX ID must match `^[A-Z0-9]{4,5}$` (the catalog form, kept exactly, never upper-cased); any other value fails with `MOSMIX-ID-INVALID` before any request and without the ID in logs or messages. For `MOSMIX_L` an HTTP 404 of the location file is `MOSMIX-ID-UNKNOWN`; for `MOSMIX_S` a location absent from the all-stations file is `MOSMIX-ID-UNKNOWN` after the whole file has been read.
- One GET per parse, no retries: `MOSMIX_L` at most 1 MiB compressed and 16 MiB unpacked, `MOSMIX_S` at most 64 MiB compressed and 2 GiB streamed, never held in memory as a whole. The archive must contain exactly one `.kml` member whose CRC-32 is verified; DTDs and external entities are rejected. Non-success status, transport failure and the request bound → `MOSMIX-RETRIEVAL-FAILED`; size → `MOSMIX-TOO-LARGE`; an invalid file → `MOSMIX-FORMAT-INVALID`, a missing issue time → `MOSMIX-ISSUE-TIME-MISSING`, a `MOSMIX_L` file of another location → `MOSMIX-LOCATION-MISMATCH`.

### Exported quantities

| MOSMIX element (unit) | Export field | Conversion and period |
| --- | --- | --- |
| `TTT` (K) | `temperature_c` | K − 273.15; no period |
| `PPPP` (Pa) | `pressure_hpa`, `pressure_mmhg` | Pa ÷ 100, both from the same unrounded value; `pressure_reference` `unknown` |
| `DD` (°) | `wind_direction_deg` | unchanged; 0 to 360 |
| `FF` (m/s) | `wind_speed_kmh` | × 3.6; no period |
| `FX1` (m/s, maximum of the last hour) | `wind_gust_kmh` | × 3.6; `[valid_at − 1 h, valid_at]`, aggregation `maximum` |
| `Rad1h` (kJ/m² of the last hour) | `solar_irradiance_w_m2` | kJ/m² per 1 h → W/m²; `[valid_at − 1 h, valid_at]`, aggregation `mean` |
| `RR1c` (kg/m² of the last hour) | `rain_rate_mm_h`, `precipitation_amount_mm` | mm per 1 h (`mean`) and the total (`sum`); `[valid_at − 1 h, valid_at]` |

- `issued_at` is the file's issue time, `retrieved_at` is sampled once after the file has been read (whole seconds), `valid_at` is each time step; all UTC without suffix. A forecast run never has `measured_at`. The run's `source` is `dwd-mosmix-l:<id>` or `dwd-mosmix-s:<id>`, `product` `MOSMIX_L` or `MOSMIX_S`; `location` (name and coordinates) comes from the file, never from the catalog.
- A `-` token is a missing value at its own step and shifts nothing; a value outside its plausible range is omitted with a reason (`FORECAST-VALUE-OMITTED`); `0` is a real value. Output rounding as for all quantities (0.1 °C, 1 hPa and 1 mmHg, 1 km/h for the wind speed and the gust; ties away from zero); `precipitation_amount_mm` is unrounded like `rain_rate_mm_h` (C-122, C-123).
- No humidity and no illuminance are exported (the files carry neither relative humidity nor lux); they are never zero-filled. Every other element of the file (probabilities, cloud cover, visibility, weather codes, longer totals) is listed in `NotExportedElements` and logged once as `MOSMIX-ELEMENT-NOT-EXPORTED`; a mapped element absent from the file is listed in `AbsentElements` and logged as `MOSMIX-ELEMENT-ABSENT`. A run without any exportable value fails with `MOSMIX-NO-USABLE-VALUE`, which carries the omissions and the not-exported names.
- The horizon is the file's own time line (the files of 2026-10-26 held 240 hourly steps for `MOSMIX_S` and 247 for `MOSMIX_L`; DWD updates the first 24 hours of every run hourly and the later steps only in the four main runs); a new file does not prove that every step was recalculated.
- Schema compatibility: `run_age_exceeded`, `wind_gust_kmh` and `precipitation_amount_mm` are additive optional members of schema v1 appended after the existing ones, and `IntervalAggregation` gained the members `maximum` and `sum` (C-120 to C-127). Documents without them stay valid against every v1 schema, but documents that contain them validate only against the regenerated schema: the v1 schema has no `xs:any`, so a consumer must validate with the `schemas/export/v1/weatherparser-export-v1.xsd` packaged with the producing package version, not with an older copy, and consumers with exhaustive switches over the aggregation enum must handle the two new members.

### Error and notice IDs

Exceptions: `MosmixForecastException` with `ErrorId` `MOSMIX-ID-INVALID` (before any request), `MOSMIX-ID-UNKNOWN`, `MOSMIX-RETRIEVAL-FAILED`, `MOSMIX-DEADLINE-EXCEEDED`, `MOSMIX-TOO-LARGE`, `MOSMIX-FORMAT-INVALID`, `MOSMIX-ISSUE-TIME-MISSING`, `MOSMIX-LOCATION-MISMATCH`, `MOSMIX-NO-USABLE-VALUE` (with `OmittedValues` and `NotExportedElements`) and `MOSMIX-OPTIONS-INVALID`; `Product` names the product. Every failure of a parse is logged once as an error with its ID, the product, the MOSMIX ID (not for an invalid ID), the sanitized endpoint and the UTC time; `MOSMIX-OPTIONS-INVALID` is thrown when the source or the parser is constructed, that is on its first resolution, and is not logged by the library, so the consumer reports it. `ValidateOnBuild` detects a missing registration of the four option types at build time but does not run the constructors, so an invalid URI or an unset `MaxRunAge` surfaces only on resolution; a consumer that wants this at startup resolves `IMosmixLParser` and `IMosmixSParser` once after building the container.

Notices (log event names, Warning): `MOSMIX-RUN-STALE`, `MOSMIX-ELEMENT-ABSENT`, `MOSMIX-ELEMENT-NOT-EXPORTED`, `FORECAST-VALUE-OMITTED` (valid time, quantity and reason). Log messages never contain forecast values, file text, archive member names or query strings.

A caller cancellation surfaces as `OperationCanceledException` with the caller's token and wins over the parse deadline, which wins over every other failure.

### Source conditions and retrieval guidance

- Data: Deutscher Wetterdienst (DWD), open data without registration, re-usable under CC BY 4.0 with acknowledgement of the source; name "Deutscher Wetterdienst" wherever forecasts are shown or republished. DWD documents no request-rate limit and stores the requester's IP address for at most 7 days.
- Update cycles (provider cycles, not a schedule of this library): `MOSMIX_S` one run per hour, published about 45 minutes after the full hour; `MOSMIX_L` runs at 03, 09, 15 and 21 UTC, available at about 04:20, 10:15, 16:20 and 22:15 UTC. `..._LATEST_...` names the newest published file, which shortly after a run time may still be the previous run; the freshness marker reports it, the library never waits or retries.
- Project recommendation: schedule parses externally (for example cron or an n8n workflow; neither is a dependency): `MOSMIX_S` at most once per hourly run after about 45 minutes past the hour — every parse downloads the whole all-stations file (about 36 MB) for one location; `MOSMIX_L` at most once per main run and location (about 17 KB per location). Set an identifying User-Agent on the consumer's `HttpClient` (the library sets none).

## MQTT serializer (library)

The library turns one export root into one MQTT message without any broker access: `IMqttMessageSerializer.SerializeAsync<TRoot>(TRoot root, CancellationToken cancellationToken)` returns an immutable `MqttMessage` with `Topic`, `Payload` (UTF-8 JSON, a private copy), `QualityOfService` (`AtMostOnce` = 0, `AtLeastOnce` = 1, `ExactlyOnce` = 2), `Retain` and `MessageType`. The serializer is generic: it contains no provider logic and no hardcoded topic, and it has no MQTT client, broker connection, TLS setting or credential ([P8 MQTT contract](docs/p8/P8_MQTT_CONTRACT.md)). Publishing the message is the consumer's job; the example broker transport belongs to the example CLI (P3).

| Root type (`TRoot`) | Message type | Default QoS | Default retain |
| --- | --- | --- | --- |
| `ObservationDataset` | `Observation` | 1 | yes |
| `ForecastRun` | `Forecast` | 1 | yes |
| `CdcStationList` | `CdcStationList` | 1 | no |
| `MosmixLocationList` | `MosmixLocationList` | 1 | no |

The message type follows the static type `TRoot` exactly; `ExportDocument` (the multi-result envelope) and every other type, also a supported root passed as `object`, fail with `MQTT-MESSAGE-TYPE-UNSUPPORTED`. The defaults are the agreed semantics: observations and complete forecast runs QoS 1 and retained, station lists QoS 1 and not retained, so that an old search response is never mistaken for the current selection.

### Consumer registration

Every consumer of `AddWeatherParsing` registers one `MqttTopicOptions`, also one that does not use MQTT (then an empty `new MqttTopicOptions()`, which makes every serialization fail with `MQTT-MAPPING-MISSING`). The topics below are the REQ-061 examples written as consumer configuration; they are not library defaults:

```csharp
services.AddSingleton(new MqttTopicOptions
{
    Mappings = new Dictionary<MqttMessageType, MqttTopicMapping>
    {
        [MqttMessageType.Observation] = new() { TopicTemplate = "haus/weather/measurements/{source}/state" },
        [MqttMessageType.Forecast] = new() { TopicTemplate = "haus/weather/forecasts/{source}/state" },
        [MqttMessageType.CdcStationList] = new() { TopicTemplate = "haus/weather/stations/dwd/selection" },
        [MqttMessageType.MosmixLocationList] = new() { TopicTemplate = "haus/weather/stations/mosmix/selection" },
    },
});
```

- A mapping has `TopicTemplate` (required), `QualityOfService` and `Retain` (both optional; null takes the default of the table above). `MaxPayloadBytes` (optional, 1 to 268 435 450) lowers the payload bound.
- The options are validated completely when `IMqttMessageSerializer` is first resolved and kept as a snapshot; later changes to the registered object have no effect. Any violation fails with `MQTT-OPTIONS-INVALID`, which the library does not log. `ValidateOnBuild` detects a missing registration but does not run the constructor, so a consumer that wants configuration errors at startup resolves `IMqttMessageSerializer` once after building the container.
- The complete list of types a consumer registers next to `AddWeatherParsing`: `HttpClient`, `TimeProvider`, `ILogger<>`, `CdcCatalogEndpoints`, `MosmixCatalogEndpoints`, `DwdObservationOptions`, `OsthessenWetterOptions`, `OsthessenWetterEndpoints`, `MosmixLForecastEndpoints`, `MosmixLForecastOptions`, `MosmixSForecastEndpoints`, `MosmixSForecastOptions` and `MqttTopicOptions`; the container validation checks every service's dependencies, so all of them are needed even for a consumer that uses only one source.

### Topic templates and placeholders

A template is literal text with placeholders `{name}`; there is no escape syntax, so `{` and `}` occur only around a known placeholder. The values come only from the root:

| Message type | Placeholders |
| --- | --- |
| `Observation` | `{source}` (`dwd:00044`, `osthessenwetter:<slug>`), `{provider}` (the text of `source` before the first `:`), `{station_id}` |
| `Forecast` | `{source}` (`dwd-mosmix-l:<id>`, `dwd-mosmix-s:<id>`), `{provider}` (`dwd-mosmix-l`, `dwd-mosmix-s`), `{mosmix_id}`, `{product}` (`MOSMIX_L`, `MOSMIX_S`) |
| `CdcStationList`, `MosmixLocationList` | `{provider}` (the text of the list `source` before the first `:`; a `source` without such a prefix fails with `MQTT-PLACEHOLDER-VALUE-INVALID`) |

Rules checked at options validation (`MQTT-OPTIONS-INVALID`):
- an unknown placeholder, or one that the message type does not have; an unbalanced, nested or empty brace; an empty template;
- `+`, `#`, U+0000, control characters (U+0001 to U+001F, U+007F to U+009F), Unicode non-characters or unpaired surrogates in the literal text; a template starting with `$`; empty topic levels that always arise (`//`, a leading or a trailing `/`); literal bytes plus one byte per placeholder above 65 535;
- uniqueness within a message type, so that retained state of one source, product or location is never replaced by another: an `Observation` template contains `{source}`, or `{provider}` and `{station_id}`, each as a whole topic level; a `Forecast` template contains `{source}`, or `{product}` and `{mosmix_id}`, each as a whole topic level (a placeholder that shares its level with other text, as in `o/x{source}`, does not count);
- uniqueness across message types: the literal text before the first placeholder of each mapped template must differ from that of every other template at a position inside both, so that no prefix is a prefix of another (`weather/{source}` together with `weather/{product}/{mosmix_id}` is rejected; the REQ-061 examples are accepted).

At serialization, every placeholder value must be non-empty and free of `/`, `+`, `#`, U+0000, control characters, non-characters and unpaired surrogates (`MQTT-PLACEHOLDER-VALUE-INVALID`); values are inserted unchanged, never escaped. The resolved topic must have at most 65 535 UTF-8 bytes, no wildcard, no leading `$` and no empty level (`MQTT-TOPIC-INVALID`).

### Payload

- The payload is exactly the bytes of the JSON export of the same root (`IJsonExportSerializer`: UTF-8 without byte-order mark, the C-11, C-15 and C-92 rules); MQTT never carries XML. A forecast run is always a `Forecast` message and has no `measured_at`.
- The root is validated by the JSON serializer before any placeholder value is read, so an invalid root (an unassigned required member, a dataset without usable readings, a run without steps) fails with `MQTT-SERIALIZATION-FAILED` and the `ExportSerializationException` as inner exception, and never becomes a message.
- Bound: a payload may have at most 268 435 450 − T bytes for a topic of T UTF-8 bytes (the MQTT remaining-length maximum minus the topic, the packet identifier and an empty MQTT 5.0 property length), or `MaxPayloadBytes` when that is smaller; above it the serializer fails with `MQTT-PAYLOAD-TOO-LARGE`. Properties a transport adds and broker limits (for example a configured maximum packet size) reduce the room further and are the transport's concern. The bound limits message acceptance, not memory: the JSON serializer buffers the whole document before the bound is checked, so peak memory follows the size of the root. As a measured example, not a capacity bound: a synthetic `MOSMIX_L` run at the 1000-step bound with every mapped element gave a payload of 724 316 bytes; longer names or other values change the size.
- The payload of a valid root is never empty, so a library message never clears a retained message.

### Duplicates

QoS 1 can deliver a message more than once. A message is a duplicate of another exactly when the payloads are byte-identical (identical input gives identical bytes). Timestamps alone are no duplicate key: neither the dataset `measured_at` nor the per-quantity `measured_at` values identify an observation revision, because a corrected value can keep its timestamp and the dataset `measured_at` is only the latest reading. The same readings retrieved later carry another `retrieved_at` and are a new message, not a duplicate. For forecasts, `source` and `issued_at` identify the run; a later message of the same run with another `retrieved_at` is no duplicate and replaces the earlier retained message.

### Error IDs

`MqttMessageException` carries `ErrorId` and, where known, `MessageType`; the serializer writes no log entries, so the caller logs failures. Precedence of one call: a null root (`ArgumentNullException`), caller cancellation (`OperationCanceledException`, never wrapped), `MQTT-MESSAGE-TYPE-UNSUPPORTED`, `MQTT-MAPPING-MISSING`, `MQTT-SERIALIZATION-FAILED`, `MQTT-PLACEHOLDER-VALUE-INVALID`, `MQTT-TOPIC-INVALID`, `MQTT-PAYLOAD-TOO-LARGE`; configuration errors are `MQTT-OPTIONS-INVALID`. Messages name the message type and the rule (and the placeholder name), never payload content, placeholder values or template text.

### Restrictions

- **OsthessenWetter values (OW-14):** the serializer accepts every valid observation dataset, also of provider `osthessenwetter`, but OsthessenWetter values must not be published, over MQTT or otherwise, until the operator's consent or other clarified conditions are recorded. The project's tests use synthetic OsthessenWetter data only.
- DWD data keeps the attribution "Deutscher Wetterdienst (DWD)" when it is republished (see the source conditions of the sections above).
- Schema compatibility: MQTT payloads are JSON documents of export contract v1; consumers that validate the XML export use the schema packaged with the producing package version (see [MOSMIX forecasts](#mosmix-forecasts-library)), and exhaustive switches over `IntervalAggregation` must handle `maximum` and `sum`.

## Configuration and publication

Station selection, source settings, MQTT connection and topics are configurable. Credentials belong in environment variables or local configuration excluded from Git. The public repository contains only examples without secrets or private operational data.

The example CLI's broker transport uses dedicated credentials and scoped topic permissions. Sources have separate observation or forecast messages; observations, forecasts and both station-list types use separate topics. The CLI's configuration file, environment variables and credential handling are described in [Example CLI](#example-cli).

## Data sources

- [DWD Climate Data Center: 10-minute observations](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/)
- [DWD: temperature, humidity and pressure dataset description](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/air_temperature/DESCRIPTION_obsgermany_climate_10min_air_temperature_en.pdf)
- [DWD: wind dataset description](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/wind/DESCRIPTION_obsgermany_climate_10min_wind_en.pdf)
- [DWD: precipitation dataset description](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/precipitation/DESCRIPTION_obsgermany-climate-10min-precipitation_en.pdf)
- [DWD: solar dataset description](https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/solar/DESCRIPTION_obsgermany_climate_10min_solar_en.pdf)
- [OsthessenWetter observations](https://osthessen-wetter.de/messwerte/)
- [DWD MOSMIX: catalog, formats and element definitions](https://www.dwd.de/DE/leistungen/opendata/hilfe.html)
- [DWD MOSMIX_L: individual forecast locations](https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/)

Code is licensed under [MIT](LICENSE). This does not grant rights to third-party weather data or websites. Source conditions, attribution and permitted retrieval frequency must be documented separately for each adapter and checked before production use. A publicly available OsthessenWetter API is not assumed.

## Binding MOSMIX schema source

[DWD pointforecast_dwd_extension_V1_0.xsd](https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd) is the binding basis for both parsers' MOSMIX source/XML data structures. Derive types, namespaces, elements and cardinalities from this schema; verify additional KML structures before implementation. Record schema revision and checksum for reproducible type generation/derivation. Map source types to separate public output entities; they do not replace the normalized forecast contract. Follow TDD for schema/mapping tests. No C# data structures have been generated yet.


## Decisions dated 2026-10-14

The additional decisions and P0 clarifications dated 2026-10-14 (development environment, English output, measurement timestamps, rounding, output XSDs, test frameworks, coverage exception and metric, deferred CLI questions, dependencies, coverage bootstrap, sources, OsthessenWetter behaviour) are maintained in [AGENTS.md](AGENTS.md#additional-decisions-dated-2026-10-14) and [REQUIREMENTS.md](REQUIREMENTS.md). The current toolchain is listed in [DEPENDENCIES.md](DEPENDENCIES.md).


## Development
The solution [WeatherParser.slnx](WeatherParser.slnx) contains twelve projects:

```text
examples/
  WeatherParser.Cli/                    # Example CLI `WeatherParser`, consumes the package (P3)
src/
  WeatherParser.Parsing/                # Packable net10.0 library (local test package)
tests/
  WeatherParser.Parsing.Tests/          # MSTest project with source reference
  WeatherParser.Package.Tests/          # Isolated NuGet PackageReference consumer tests
  WeatherParser.Cli.Tests/              # Example CLI tests (unit and process tests)
  WeatherParser.Cli.SignalHost/         # Signal test host of the CLI process tests (not packed, not a test project)
  WeatherParser.Coverage.Tests/         # Coverage evaluator tests
  WeatherParser.Coverage.Runner.Tests/  # Coverage runner tests
  WeatherParser.SchemaGeneration.Tests/ # Export schema generator, golden-file and refinement tests
tools/
  WeatherParser.Coverage/               # Non-packable coverage evaluator library
  WeatherParser.Coverage.Runner/        # Non-packable console runner for the coverage gate
  WeatherParser.SchemaGeneration/       # Non-packable export schema (XSD) generator library
```

Quickstart (Windows, PowerShell 7, from the repository root):

```powershell
New-Item -ItemType Directory -Force artifacts/local-nuget
dotnet build WeatherParser.Ci.slnf --configuration Release
pwsh ./scripts/ci-local.ps1 -TargetRef <comparison branch> -RunSettings coverage.runsettings
```

Every developer procedure, its platform and its verification status are in [CONTRIBUTING.md](CONTRIBUTING.md); contributor rules are in [AGENTS.md](AGENTS.md), versions and licenses in [DEPENDENCIES.md](DEPENDENCIES.md), and the verified platform matrix in [docs/COMPATIBILITY.md](docs/COMPATIBILITY.md). Phase history and acceptance records: [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md), [PHASE_P1_PLAN.md](PHASE_P1_PLAN.md) and [docs/p1](docs/p1/).
