# WeatherParser Operations

**Status (2026-10-31): written in P9.07 (deployment) and P9.08 (the rest of the guide); the examples carry their verification status (section 11); checked on the NanoPi R76S in P9.10 on release candidate `86e77eb` (2026-11-01 to 2026-11-02).** This guide covers the example CLI `WeatherParser` on Debian 13 ARM64 (`linux-arm64`, for example the NanoPi R76S). The library itself is consumed as a NuGet package and needs no deployment of its own; its consumers follow the README sections of each source. Every host, ID, path and topic below is a placeholder.

## 1. Deployment (RL-07 A)

All paths, the user and the group below are documented examples, not defaults: the CLI reads no configuration on its own and needs `--Config <path>` or `WEATHERPARSER_CONFIG` for every command (CL-04, REQ-064).

### Runtime

The CLI is a framework-dependent `linux-arm64` publish and needs the .NET runtime 10.0.14 or newer (`Microsoft.NETCore.App`; the ASP.NET Core runtime is not needed). Install it from the Microsoft package feed for Debian 13 and keep it current with the system updates:

```bash
sudo apt-get install dotnet-runtime-10.0
dotnet --list-runtimes
```

Follow Microsoft's instructions for adding the package feed on Debian 13 first ([Install .NET on Debian](https://learn.microsoft.com/dotnet/core/install/linux-debian)). A runtime installed by other means (for example the `dotnet-install` script into a user folder) works as well when `DOTNET_ROOT` points to it; the installer and the update path are then the operator's responsibility.

### Building the CLI from the tagged source (RL-08 A)

The CLI is not attached to a release. An operator builds it from the release tag against the released package, with the .NET SDK that `global.json` pins (`rollForward` `disable`; CONTRIBUTING.md section 1), on any machine with that SDK; the output is framework-dependent `linux-arm64` and runs on the target with the runtime above. Until the package is published, the released `.nupkg` (verified against the release's `SHA256SUMS`) is the only copy of `WeatherParser.Parsing` in the local package source `artifacts/local-nuget`, which `NuGet.Config` names; after a publication it comes from nuget.org. A fresh package cache prevents a stale earlier `0.1.0` build from being used (CONTRIBUTING.md section 4):

```bash
git clone <repository URL> weatherparser
cd weatherparser
git checkout --detach v<version>
mkdir -p artifacts/local-nuget
cp <release folder>/WeatherParser.Parsing.<version>.nupkg artifacts/local-nuget/
export NUGET_PACKAGES="$PWD/.nuget-packages"
dotnet restore examples/WeatherParser.Cli -r linux-arm64 --force-evaluate
dotnet publish examples/WeatherParser.Cli -c Release -r linux-arm64 --self-contained false --no-restore -o publish
```

Check that the CLI resolved exactly the released package: `examples/WeatherParser.Cli/obj/project.assets.json` must list the library `WeatherParser.Parsing/<version>` with a `sha512` equal to the Base64 SHA-512 of the released `.nupkg` (the release build makes the same check, `PACKAGE-MISMATCH`):

```bash
python3 -c "import json,sys; print(json.load(open(sys.argv[1]))['libraries']['WeatherParser.Parsing/'+sys.argv[2]]['sha512'])" examples/WeatherParser.Cli/obj/project.assets.json <version>
openssl dgst -sha512 -binary artifacts/local-nuget/WeatherParser.Parsing.<version>.nupkg | base64 -w0; echo
```

The two lines must be equal. The folder `publish` then takes the place of the unpacked archive in the commands below (`sudo cp -r publish/. /opt/weatherparser/<version>/` instead of the `python3 … extractall` line). The private CLI acceptance artifact of the release build, `WeatherParser.Cli.<version>.linux-arm64.zip`, is built the same way; it is used for the release acceptance (P9.07 to P9.10) and is not a release download.

### Layout, user and permissions

The CLI, either the folder `publish` built above or, for the release acceptance, the CLI acceptance artifact `WeatherParser.Cli.<version>.linux-arm64.zip` verified against its `SHA256SUMS` line, goes into a versioned folder; a stable symlink names the current version, so an update is an unpack plus a symlink change and a rollback is the reverse:

```bash
sha256sum -c --ignore-missing SHA256SUMS
sudo useradd --system --no-create-home --shell /usr/sbin/nologin weatherparser
sudo install -d -m 0755 /opt/weatherparser/<version>
sudo python3 -c "import zipfile,sys; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])" WeatherParser.Cli.<version>.linux-arm64.zip /opt/weatherparser/<version>
sudo chmod 0755 /opt/weatherparser/<version>/WeatherParser
sudo ln -sfn /opt/weatherparser/<version> /opt/weatherparser/current
sudo install -d -m 0750 -o root -g weatherparser /etc/weatherparser
```

(`unzip -d` works the same way where it is installed; `zipfile` keeps no permission bits, hence the `chmod` of the app host.)

- Configuration files under `/etc/weatherparser/`: owner `root`, group `weatherparser`, mode `0640`. Copy `examples/weatherparser.example.json` and replace the placeholder endpoints; never overwrite an existing configuration.
- Broker credential file (MQTT mode only): owned by `weatherparser`, mode `0600` (CL-06); the CLI rejects a file that another user owns or that the group or others can access, with `CLI-CREDENTIALS-INVALID` and exit code 2, before any request.

  First installation only; the command refuses to replace an existing file, so repeating it during an update never truncates working credentials:

```bash
sudo test ! -e /etc/weatherparser/broker.credentials.json && sudo install -m 0600 -o weatherparser -g weatherparser /dev/null /etc/weatherparser/broker.credentials.json
```

  Then write the members `Username` and `Password` into it with an editor run as root; never put credentials on a command line or into the configuration file.

- MOSMIX_S evidence cache (only for `--SelectMOSMIX`): an existing directory that only the cache uses, writable by the service user, named absolute in `Mosmix.Catalog.EvidenceCacheDirectory`:

```bash
sudo install -d -m 0750 -o weatherparser -g weatherparser /var/lib/weatherparser/mosmix-evidence
```

### Updates

An update unpacks the new version into its own folder and moves the symlink (`sudo ln -sfn /opt/weatherparser/<new version> /opt/weatherparser/current`); it does not touch `/etc/weatherparser/`. Configuration files and the credential file are created only at the first installation and are never copied over an existing file; compare a new example configuration with the existing one by hand. Read the [changelog](../CHANGELOG.md) of the new version first.

### Invocation

Every invocation runs exactly one command and exits; there is no service, timer or daemon to install. A scheduler starts it as the service user without a terminal, for example:

```bash
sudo -u weatherparser /opt/weatherparser/current/WeatherParser --ParseDWD --Output json --Config /etc/weatherparser/weatherparser.json < /dev/null
```

The document goes to standard output (or `--OutputFile`), logs go to standard error, and the exit code reports the result (section 4).

## 2. Configuration and secrets

- One JSON configuration file per use, given by `--Config` or `WEATHERPARSER_CONFIG`; there is no implicit search path (CL-04). Its sections, members, defaults and ranges are listed in the README section "Example CLI", subsection "Configuration". Unknown members, comments and trailing commas are rejected (`CLI-CONFIG-INVALID`, exit code 2), so a typo never silently falls back to a default.
- Several files are fine, for example one per command or per source: each invocation reads exactly the one it is given.
- Secrets are only the broker credentials. They come either from the credential file (section 1) or from the environment variables `WEATHERPARSER_MQTT_USERNAME` and `WEATHERPARSER_MQTT_PASSWORD`, never from both and never from arguments or the configuration file. Prefer the file: environment variables of a scheduled job are visible to the job's owner and to root and may end up in the scheduler's own logs.
- JSON and XML mode read no credentials and never contact a broker.
- `OsthessenWetter.PublicationPermitted` stays `false` (section 9).
- Set `Http.UserAgent` to a value that identifies the operator's installation (for example `WeatherParser/<version> (contact: ops@example.invalid)`); the default is `WeatherParser/<version>`.

## 3. Scheduling

Scheduling and retries across invocations belong to the caller (REQ-007, REQ-008); nothing has to be installed for WeatherParser itself, and neither cron nor n8n is a dependency.

### cron

A file in `/etc/cron.d/` names the user, so the job runs as the service user (placeholder paths and IDs; one command per line):

```text
# /etc/cron.d/weatherparser
WEATHERPARSER_CONFIG=/etc/weatherparser/weatherparser.json
*/10 * * * * weatherparser /opt/weatherparser/current/WeatherParser --ParseDWD --StationID <DWD-STATION-ID> < /dev/null >> /var/log/weatherparser/weatherparser.log 2>&1 || logger -t weatherparser "ParseDWD failed with exit code $?"
50 * * * * weatherparser /opt/weatherparser/current/WeatherParser --ParseMOSMIX --Product MOSMIX_S --MOSMIXID <MOSMIX-ID> < /dev/null >> /var/log/weatherparser/weatherparser.log 2>&1 || logger -t weatherparser "ParseMOSMIX MOSMIX_S failed with exit code $?"
25 4,10,16,22 * * * weatherparser /opt/weatherparser/current/WeatherParser --ParseMOSMIX --Product MOSMIX_L --MOSMIXID <MOSMIX-ID> < /dev/null >> /var/log/weatherparser/weatherparser.log 2>&1 || logger -t weatherparser "ParseMOSMIX MOSMIX_L failed with exit code $?"
```

cron schedules in the system time zone of the host (`timedatectl`), and a `TZ` setting in the crontab does not change that. The MOSMIX_L line above assumes a host in UTC; on a host in `Europe/Berlin` use `30 0,6,12,18 * * *`, which is after the publication of each run in both winter (CET) and summer (CEST) time. The other lines do not depend on the time zone. cron runs each line with `/bin/sh`; `|| logger …` reports every exit code other than 0 to the system log (`$?` is the CLI's exit code there). The log directory is created once with `sudo install -d -m 0750 -o weatherparser -g weatherparser /var/log/weatherparser` and rotated as in section 5; to use the journal instead, replace the redirection by `2>&1 | systemd-cat -t weatherparser` (the exit code is then the pipe's and is not reported). In MQTT mode (the default) standard output stays empty; in JSON or XML mode use `--OutputFile` with a new file name per run, because the CLI never overwrites an existing file (`CLI-OUTPUT-EXISTS`).

### n8n

An "Execute Command" node runs the CLI on the machine that runs n8n, for example `/opt/weatherparser/current/WeatherParser --ParseDWD --StationID <DWD-STATION-ID> --Output json --Config /etc/weatherparser/weatherparser.json`, takes the JSON document from standard output and branches on the exit code (section 4). The node runs as the n8n user: that user then needs read access to the configuration (for example membership in the group `weatherparser`) and, in MQTT mode, must own the credential file (CL-06), so give n8n its own configuration and credential file rather than sharing the cron user's. n8n's schedule trigger replaces cron; WeatherParser itself never schedules or repeats anything.

## 4. Exit codes for callers

| Exit code | Meaning | Typical caller reaction |
|---|---|---|
| `0` | Complete success, also an empty selection and `--Help` | Use the result |
| `1` | Complete failure at run time (retrieval, output, connection or publication; an expired command deadline without a published message) | Retry at the next scheduled run; alert after repeated failures |
| `2` | Usage or configuration error, detected before any retrieval | Fix the configuration or the call; a retry does not help |
| `3` | Partial failure: at least one item succeeded and at least one failed | Use the successful items; the `errors` list of the document and the log name the failed ones |
| `4` | Cancelled by SIGINT, SIGTERM or Ctrl+C, also when some items succeeded before the signal | The command was interrupted: messages acknowledged before the signal stay published (and retained), no output file appears; read the `CLI-PUBLICATION-SUMMARY` line for the counts. The next run starts normally and may publish the same values again |

A partial failure is normal for multi-item commands, for example an OsthessenWetter station the page marks offline. Notices such as `SELECTION-EMPTY`, `MOSMIX-RUN-STALE` or `MOSMIX-S-EVIDENCE-STALE` are logged and leave the exit code unchanged; check freshness in the data (section 8), not in the exit code. The error IDs are listed in the README section "Example CLI", subsection "Error IDs".

## 5. Logs

Logs go to standard error only, one line per entry: `<UTC time> UTC <LEVEL> <ID> command=<command> [source=<source>] <message>`. They contain no credentials, no configuration objects and no stack traces. Keep them where the scheduler puts them:

- the systemd journal (`systemd-cat -t weatherparser`, read with `journalctl -t weatherparser`), whose size limits apply; or
- a file appended by the job, rotated by `logrotate`, for example `/etc/logrotate.d/weatherparser` (one directive per line; check it with `sudo logrotate --debug /etc/logrotate.d/weatherparser`):

```text
/var/log/weatherparser/*.log {
    weekly
    rotate 8
    compress
    missingok
    notifempty
    su weatherparser weatherparser
}
```

`--Verbose` adds debug lines; use it for diagnosis, not in scheduled jobs.

## 6. Request volume and intervals

The project recommends these intervals; they are bounded by the providers' cycles and conditions, not by a rule of the providers (DWD documents no request-rate limit; OsthessenWetter documents none). The schedule is the operator's decision (REQ-008).

| Command | Requests per run | Recommended interval |
|---|---|---|
| `--ParseDWD` | The station lists and the `now` files of one station (a few small files) | At most once every 10 minutes per station; DWD updates the 10-minute `now` data within the hour |
| `--ParseMOSMIX --Product MOSMIX_S` | One download of the complete MOSMIX_S file (about 36 MB) per location | At most once per hourly run, about 45 minutes after the full hour |
| `--ParseMOSMIX --Product MOSMIX_L` | One file per location (about 17 KB) | At most once per main run, after about 04:20, 10:15, 16:20 and 22:15 UTC (runs at 03, 09, 15 and 21 UTC) |
| `--ParseOstHessen` | One page | At most once every 10 minutes; production use is not accepted while OW-14 applies (section 9) |
| `--SelectDWD`, `--SelectMOSMIX` | The catalogs and per-station evidence | Configuration-time only: when a station or location is chosen or reviewed, at most once per day per search |
| `--SelectMOSMIX --RefreshMosmixSEvidence` | One complete MOSMIX_S download | Rarely, for example once per day before a selection |

Several locations of MOSMIX_S each download the whole file; parse only the locations that are needed. DWD stores the requesting IP address for at most 7 days (section 9).

## 7. Broker set-up

- **Dedicated credentials**: one broker user per CLI installation (and a separate one for an n8n-driven installation), with a long random password, stored as in section 1.
- **TLS with validation**: keep TLS on (the default) and give `Mqtt.Broker.Tls.CaFile` the PEM of a private CA if the broker certificate is not in the system trust store; the CLI always validates the chain and the host name and has no switch to turn that off. `AllowUnencrypted: true` is only for a trusted local network and logs a warning per run.
- **Scoped permissions**: allow the CLI's broker user to publish only below its configured topic trees and to subscribe to nothing. For Mosquitto, for example, an ACL file with `user weatherparser` followed by `topic write haus/weather/measurements/#`, `topic write haus/weather/forecasts/#` and `topic write haus/weather/stations/#` (placeholder topics of the example configuration).
- **MQTT 5.0**: prefer protocol 5.0; with 3.1.1 a broker can acknowledge a message it then drops (for example for a missing permission), which the CLI cannot detect (section 10).
- **Retained state**: observations and complete forecast runs are published with retain, station lists without; QoS is 1 for all. A new run of the same source replaces the retained message of its topic, so the topic templates must keep sources and products apart (for example `{source}` in the template, and separate templates for observations and forecasts; MOSMIX_S and MOSMIX_L have different sources). To clear a retained message, publish an empty retained message to its topic with a broker tool. A retried QoS 1 message may reach subscribers twice.

## 8. Estimated dates and freshness

- **DWD observations** carry the measurement time of every value in UTC; the CLI does not filter old values. Consumers compare `measured_at` with their own clock.
- **OsthessenWetter** shows only a time of day. The date is inferred in `Europe/Berlin` from the retrieval time and every reading is marked `timestamp_quality` `estimated_date` (REQ-042); a display older than one day cannot be detected. Readings in a daylight-saving gap or overlap are not exported (`OSTHESSEN-TIME-UNRESOLVED`).
- **MOSMIX** runs keep `issued_at`, `retrieved_at` and each step's validity separate. A run older than `Mosmix.L.MaxRunAge` or `Mosmix.S.MaxRunAge` is still exported, with a `MOSMIX-RUN-STALE` warning and the freshness marker in the document. Guidance, not a default: MOSMIX_S 2 hours, MOSMIX_L 8 hours.
- **MOSMIX_S evidence index**: an index older than `MosmixSEvidenceMaxAge` is still used, with one `MOSMIX-S-EVIDENCE-STALE` warning; refresh it with `--RefreshMosmixSEvidence`.

## 9. Source conditions

- **DWD** (observations, MOSMIX, catalogs): open data under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) with acknowledgement of the source ([DWD legal notice](https://www.dwd.de/EN/service/legal_notice/legal_notice_node.html)). Wherever values are shown or republished, for example over MQTT or in files, keep the attribution "Deutscher Wetterdienst (DWD)". The open-data server stores the requesting IP address for at most 7 days.
- **OsthessenWetter** publishes no licence for its measured values and no interface, and its imprint forbids using its content in other publications without the author's express consent. **Production use and redistribution of its values are not accepted** by this project until the operator's consent or other clarified conditions are recorded (contract OW-14): keep `OsthessenWetter.PublicationPermitted` `false`, which makes `--ParseOstHessen --Output mqtt` fail before any request, do not schedule `--ParseOstHessen` for production, and keep JSON and XML output for the local user only, never redistributed. OW-14 is lifted only by a recorded decision after the operator's consent or other clarified conditions. Name the source "Osthessen-Wetter.de" wherever values are shown.
- The MIT licence of WeatherParser covers the code, not the data or the websites.

## 10. Limitations

The known limitations of the release, with their owners, are listed in the [changelog](../CHANGELOG.md). For operation in particular:

- TLS and authentication were verified against one broker product (Mosquitto 2.1.2) with a self-signed test CA in `Tls.CaFile` ([P9.09 record](p9/P9_09_BROKER_AND_SHUTDOWN_CHECKS.md)); the system trust store and a public CA were not shown.
- With MQTT 3.1.1 a dropped message can look acknowledged; use 5.0.
- After a cancellation or an expired deadline the broker disconnect is bounded by `Limits.ConnectTimeout`, so the process can end up to that long after `Limits.CommandDeadline`.
- Windows is a development platform, not a release target; a Windows system shutdown during a run is not tested.
- The OsthessenWetter parser supports only the documented page revision; a changed page fails with `OSTHESSEN-FORMAT-CHANGED`.

## 11. Verification status of the examples

Status values as in CONTRIBUTING.md ("Status values").

| Example | Status |
|---|---|
| Section 1, checksum, unpack, symlink, configuration and credential modes, invocation without a terminal | Verified on the NanoPi R76S in P9.07 in a disposable home folder as the login user, without root ([P9 verification record](p9/P9_VERIFICATION.md) section 8, [target run](p9/P9_07_TARGET_RUN.md)), and in P9.10 with the service user, the system paths, the credential file and the runtime 10.0.14 ([P9.10 record](p9/P9_10_ARM64_RELEASE_ACCEPTANCE.md) sections 4 and 7) |
| Section 1, building the CLI from the tagged source | Planned for P9.12 (the post-publication check of RL-08 A repeats this build once); the same restore, publish and package check run in every release build (`scripts/build-release.ps1`, verified in P9.06 and P9.10) |
| Section 1, MOSMIX_S evidence cache directory; updates and rollback | Evidence cache directory Verified in P9.10 (created with its owner, filled by the live `--SelectMOSMIX --RefreshMosmixSEvidence`); updates and rollback Verified in P9.10 by moving `current` to a second version folder and back, with `/etc/weatherparser` and the credential file unchanged (the same artifact in both folders; [P9.10 record](p9/P9_10_ARM64_RELEASE_ACCEPTANCE.md) section 7) |
| Section 3, cron entries; n8n | cron Verified in P9.10: the guide's cron line with `logger` under `/bin/sh`, and a 36.5-hour series of the documented entries, MOSMIX_L at `30 0,6,12,18` on a `Europe/Berlin` host ([P9.10 record](p9/P9_10_ARM64_RELEASE_ACCEPTANCE.md) sections 5 and 7); n8n is an example only: its command line ran as a shell command, n8n itself not verified |
| Section 4, exit codes | Verified by the CLI tests (P3) and on the NanoPi in P3.11 and P9.07 |
| Section 5, journal and logrotate | journal Verified in P9.10; the logrotate stanza checked with `logrotate --debug` (exit 0, no errors), no rotation performed ([P9.10 record](p9/P9_10_ARM64_RELEASE_ACCEPTANCE.md) section 7) |
| Section 7, broker set-up with TLS, credentials and ACL | Verified in P9.09 with Mosquitto 2.1.2, a test CA in `Tls.CaFile`, a credential file and a write-only publisher identity, on Windows and on the NanoPi R76S ([P9.09 record](p9/P9_09_BROKER_AND_SHUTDOWN_CHECKS.md)); the system trust store not shown |
