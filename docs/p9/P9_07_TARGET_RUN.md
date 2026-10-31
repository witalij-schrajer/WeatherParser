# P9.07 – NanoPi R76S Target Run (2026-10-31)

Retained procedure and transcript of the S4 target check of [P9.07](P9_07_CLI_DEPLOYMENT_PLAN.md), recorded in [P9_VERIFICATION.md](P9_VERIFICATION.md) section 8. Written by the orchestrating agent; the script below is the one that ran (kept here as evidence, not as a maintained script). The only change to the transcript is the login user's home folder, shown as `~`.

## 1. Inputs

- CLI acceptance artifact and checksum list of the hosted dry run 41225642707 on `a563fc464af3d4188dcba58c289668902b1b7b5a` (downloaded with `gh run download 41225642707`): `WeatherParser.Cli.0.1.0.linux-arm64.zip` (SHA-256 `12439e4d7c1cd759c8e402f76200e812c9895157ab3096d0aaefa2386c7b2831`) and `SHA256SUMS`.
- Fixture host published on Windows from `b670144` (framework-dependent, portable): `dotnet publish tests/WeatherParser.Cli.FixtureHost -c Release -o <folder>/fixturehost`; the published folder contains `WeatherParser.Cli.FixtureHost.dll` and the `fixtures/` tree of the project.
- Transfer: `ssh R76S 'test ! -e ~/weatherparser-p907 && mkdir ~/weatherparser-p907'`, then `scp -r SHA256SUMS WeatherParser.Cli.0.1.0.linux-arm64.zip fixturehost p907-target.sh R76S:weatherparser-p907/`, then `ssh R76S 'bash ~/weatherparser-p907/p907-target.sh'`.
- Cleanup: `ssh R76S 'rm -rf ~/weatherparser-p907 && test ! -e ~/weatherparser-p907 && echo REMOVED'` (output `REMOVED`); no fixture host process remained.

## 2. Script `p907-target.sh`

```bash
#!/bin/bash
# P9.07 S4 target check without root (user decision "Run without root"): RL-07 A layout in a disposable folder, run as the login user.
set -u
B="$(cd "$(dirname "$0")" && pwd)"
D="$B/root"
export DOTNET_ROOT="$HOME/.dotnet" PATH="$HOME/.dotnet:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
log() { printf '%s %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$*"; }
log "device: $(uname -srm); debian $(cat /etc/debian_version); user $(id -un) uid $(id -u)"
log "runtimes: $(dotnet --list-runtimes | grep NETCore | tr '\n' ' ')"
cd "$B"
log "checksum:"; grep ' WeatherParser.Cli.0.1.0.linux-arm64.zip$' SHA256SUMS | sha256sum -c - ; echo "checksum_exit=$?"
mkdir -p "$D/opt/weatherparser/0.1.0" "$D/etc/weatherparser"
python3 -c "import zipfile,sys; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])" WeatherParser.Cli.0.1.0.linux-arm64.zip "$D/opt/weatherparser/0.1.0"
chmod 0755 "$D/opt/weatherparser/0.1.0/WeatherParser"
ln -sfn "$D/opt/weatherparser/0.1.0" "$D/opt/weatherparser/current"
chmod 0750 "$D/etc/weatherparser"
CLI="$D/opt/weatherparser/current/WeatherParser"
log "layout:"; ls -l "$D/opt/weatherparser"; stat -c '%A %U %n' "$D/etc/weatherparser"
# Fixture host on the loopback interface.
dotnet fixturehost/WeatherParser.Cli.FixtureHost.dll fixturehost/fixtures 0 > host.out 2> host.err &
HOST=$!
for i in $(seq 1 100); do grep -q '^LISTENING ' host.out 2>/dev/null && break; sleep 0.1; done
U="$(sed -n 's/^LISTENING //p' host.out)"; log "host pid $HOST at $U"
cat > "$D/etc/weatherparser/dwd.json" <<JSON
{ "Dwd": { "Catalog": {
 "AirTemperatureStationList": "${U}cdc/air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt", "AirTemperatureNowDirectory": "${U}cdc/air_temperature/now/",
 "WindStationList": "${U}cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt", "WindNowDirectory": "${U}cdc/wind/now/",
 "PrecipitationStationList": "${U}cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt", "PrecipitationNowDirectory": "${U}cdc/precipitation/now/",
 "SolarStationList": "${U}cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt", "SolarNowDirectory": "${U}cdc/solar/now/" } } }
JSON
cat > "$D/etc/weatherparser/osthessen.json" <<JSON
{ "OsthessenWetter": { "PageUri": "${U}osthessen/index.html", "PublicationPermitted": true,
  "Stations": [ { "Name": "Müs-Hohhütte", "Latitude": 50.5, "Longitude": 9.9, "AltitudeM": 900 }, { "Name": "Langenbieber", "Latitude": 50.6, "Longitude": 9.8 } ] },
  "Mqtt": { "Broker": { "Host": "127.0.0.1", "Port": 1, "AllowUnencrypted": true, "CredentialsFile": "$D/etc/weatherparser/test.credentials.json" },
    "Topics": { "Observation": { "TopicTemplate": "test/weather/measurements/{source}/state" } } },
  "Limits": { "MaxPublishAttempts": 1, "RetryDelay": "00:00:00" } }
JSON
chmod 0640 "$D/etc/weatherparser/dwd.json" "$D/etc/weatherparser/osthessen.json"
printf '{ "Username": "placeholder-user", "Password": "placeholder-test-value" }\n' > "$D/etc/weatherparser/test.credentials.json"
# Runs one command without a terminal: new session, stdin from /dev/null.
run() { local name="$1"; shift; ( cd "$B/work" && setsid -w "$CLI" "$@" < /dev/null > "$B/$name.out" 2> "$B/$name.err" ); local rc=$?
  log "$name exit=$rc stdout_bytes=$(wc -c < "$B/$name.out") stderr_lines=$(wc -l < "$B/$name.err")"; sed 's/^/    stderr: /' "$B/$name.err" | head -5; }
mkdir -p "$B/work"
run help --Help
run usage --ParseDWD --Bogus x
chmod 0644 "$D/etc/weatherparser/test.credentials.json"
log "credentials mode: $(stat -c '%a %U' "$D/etc/weatherparser/test.credentials.json")"
run cred0644 --ParseOstHessen --StationNamePattern '*' --Config "$D/etc/weatherparser/osthessen.json"
chmod 0600 "$D/etc/weatherparser/test.credentials.json"
log "credentials mode: $(stat -c '%a %U' "$D/etc/weatherparser/test.credentials.json") (foreign-owner case needs root: not run)"
run selectdwd --SelectDWD --Latitude 50.55 --Longitude 9.67 --Altitude 300 --MaxDistance 50 --MaxAltDifference 200 --Output json --Config "$D/etc/weatherparser/dwd.json"
run parseosthessen --ParseOstHessen --StationNamePattern 'M*' --Output json --Config "$D/etc/weatherparser/osthessen.json"
python3 -c "import json,sys; d=json.load(open(sys.argv[1])); print('    selectdwd stations:', len(d['stations']))" "$B/selectdwd.out"
python3 -c "import json,sys; d=json.load(open(sys.argv[1])); print('    parseosthessen observations:', len(d['observations']))" "$B/parseosthessen.out"
kill -TERM "$HOST"; wait "$HOST"; log "host exit after SIGTERM: $?"
log "host log:"; sed 's/^/    /' host.err
```

## 3. Transcript

```text
2026-10-31T18:28:34Z device: Linux 6.1.141 aarch64; debian 13.3; user witalij uid 1002
2026-10-31T18:28:34Z runtimes: Microsoft.NETCore.App 10.0.13 [~/.dotnet/shared/Microsoft.NETCore.App] 
2026-10-31T18:28:34Z checksum:
WeatherParser.Cli.0.1.0.linux-arm64.zip: OK
checksum_exit=0
2026-10-31T18:28:34Z layout:
total 8
drwxrwxr-x 2 witalij witalij 4096 Oct 31 19:28 0.1.0
lrwxrwxrwx 1 witalij witalij   61 Oct 31 19:28 current -> ~/weatherparser-p907/root/opt/weatherparser/0.1.0
drwxr-x--- witalij ~/weatherparser-p907/root/etc/weatherparser
2026-10-31T18:28:35Z host pid 4162025 at http://127.0.0.1:33419/
2026-10-31T18:28:35Z help exit=0 stdout_bytes=2780 stderr_lines=0
2026-10-31T18:28:35Z usage exit=2 stdout_bytes=0 stderr_lines=1
    stderr: 2026-10-31T18:28:35 UTC ERROR CLI-ARGUMENT-INVALID command=ParseDWD Unknown option '--Bogus'. Use --Help for the commands and options.
2026-10-31T18:28:35Z credentials mode: 644 witalij
2026-10-31T18:28:36Z cred0644 exit=2 stdout_bytes=0 stderr_lines=1
    stderr: 2026-10-31T18:28:36 UTC ERROR CLI-CREDENTIALS-INVALID command=ParseOstHessen The credential file must be owned by the current user and must not be accessible by group or others.
2026-10-31T18:28:36Z credentials mode: 600 witalij (foreign-owner case needs root: not run)
2026-10-31T18:28:37Z selectdwd exit=0 stdout_bytes=228 stderr_lines=1
    stderr: 2026-10-31T18:28:37 UTC INFORMATION SELECTION-EMPTY command=SelectDWD The CDC station selection found no active station inside the search area (latitude 50.55, longitude 9.67, maximum distance 50 km, maximum altitude difference 200 m).
2026-10-31T18:28:40Z parseosthessen exit=0 stdout_bytes=859 stderr_lines=0
    selectdwd stations: 0
    parseosthessen observations: 1
2026-10-31T18:28:40Z host exit after SIGTERM: 0
2026-10-31T18:28:40Z host log:
    GET /cdc/air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt 200
    GET /cdc/wind/now/zehn_now_ff_Beschreibung_Stationen.txt 200
    GET /cdc/precipitation/now/zehn_now_rr_Beschreibung_Stationen.txt 200
    GET /cdc/solar/now/zehn_now_sd_Beschreibung_Stationen.txt 200
    GET /cdc/wind/now/10minutenwerte_wind_90001_now.zip 404
    GET /cdc/solar/now/10minutenwerte_SOLAR_90001_now.zip 404
    GET /cdc/air_temperature/now/10minutenwerte_TU_90001_now.zip 404
    GET /cdc/precipitation/now/10minutenwerte_nieder_90001_now.zip 404
    GET /osthessen/index.html 200
    STOPPED
```
