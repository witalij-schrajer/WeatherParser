# P9.10 – ARM64 Release Acceptance: Run Record

**Status (2026-11-02): outcome READY on release candidate `86e77eb` on the NanoPi R76S (S1 to S5), with the deviations of section 8.** Plan: [P9_10_ARM64_RELEASE_ACCEPTANCE_PLAN.md](P9_10_ARM64_RELEASE_ACCEPTANCE_PLAN.md). Contract: [P9_RELEASE_CONTRACT.md](P9_RELEASE_CONTRACT.md) RL-06 A, RL-07 A, RL-13 A, RL-15 A. Summary and history (the superseded first candidate `2a084be`, the CL-09 fix): [P9 verification record](P9_VERIFICATION.md) section 11. Written by the orchestrating agent; no address, credential, key, certificate or OsthessenWetter value is recorded here.

## 1. Authorizations and decisions (verbatim)

- 2026-10-31: "I authorize all required runs"; for the root parts option 2: "Ok. We make it as purposed in Option 2" (the user runs a root script, `p910-root.sh setup` and `finish`; everything else runs as the service user); the request list with the one-time live runs and the 24-hour series: "Approved, setup done".
- 2026-10-31, after the defect on the first candidate: "Fix it now" (CL-09 amendment, verification record section 11.2).
- 2026-11-01, after a first series on `86e77eb` that the user ended after 15 minutes: "Rerun for 24 hours"; then "Setup started". A later "Finish done" one minute after the setup was checked against the device (no finish log, cron file present) and not acted on; the user confirmed that finish had not run. The user ran `finish` on 2026-11-02 at 17:36Z and reported "finish done".
- 2026-11-02, after the Codex review found the guide's update and rollback example unexercised and the user was offered a short root check or an exception: "Run a short root check"; then "Check done".
- Candidate (plan section 3): `86e77eb430326a066c4cf09921c8145b4d44acbe`, the phase head after the fix and its records. Fake sources on the target: the P9.07 fixture host with its synthetic fixture set, started as the service user outside the installed CLI (`127.0.0.1:18080`).
- Peak memory (plan S4): measured per invocation with `/usr/bin/time -v` (maximum resident set size).

## 2. S1 – Candidate build

- `release-build.yml` run 41239327847 on `86e77eb`, success; evidence record: tree `d6804df0fabd6f70c0c68c85c7bd054a87b107be`, version 0.1.0, SDK 10.0.402, `linux-arm64`, `DOTNET_ROLL_FORWARD` Disable.
- `SHA256SUMS`: `WeatherParser.Parsing.0.1.0.nupkg` `ca05b9d76eed76915ab0a75d37953f74ee86bd69f27941c54b0de4685b57666f`; `WeatherParser.Parsing.0.1.0.snupkg` `deb896b507b2a293d136a210fc86c7abeb916fa0294753a8b010c009e74ae267`; `WeatherParser.Cli.0.1.0.linux-arm64.zip` `ec875a70058d13302b245921a4e57f1be8e0649a89e0d178b078cbb548784898`. Checked `OK` on Windows after the download and on the NanoPi by the setup script (every setup).
- Hosted CI on `86e77eb`: run 41239331632 success (documentation only; the code jobs ran green on `dbe7460`, run 41239010160).

## 3. S2 – Package consumption without source checkout

Method as for the first candidate (verification record section 11.1): a `git archive` of the candidate without `src/` except the library's project file and `PublicApi.txt`, the release `.nupkg` as the only package of a local folder feed, a fresh NuGet cache, the restored package hash equal to the released one. `tests/WeatherParser.Package.Tests`: Windows (SDK 10.0.402) 131 of 131 passed; NanoPi R76S (system SDK 10.0.403, runtime 10.0.14, the export's `global.json` removed) 131 of 131 passed.

## 4. S3 – CLI runs on the NanoPi R76S

Device: Linux 6.1.141 aarch64, Debian 13.7, `Microsoft.NETCore.App` 10.0.14 under `/usr/share/dotnet` (Microsoft APT feed). The setup ran twice on this candidate (2026-11-01 04:35Z to 04:38Z and 04:55Z to 04:58Z, section 6); both runs gave the same results except where noted. The results below are those of the second setup.

**Layout (RL-07 A)**: system user `weatherparser` with `/usr/sbin/nologin`; `/opt/weatherparser/0.1.0` and the symlink `/opt/weatherparser/current`; `/etc/weatherparser` `0750 root:weatherparser`, configuration files `0640 root:weatherparser`, the credential file `0600 weatherparser:weatherparser`; `/var/lib/weatherparser/mosmix-evidence` and `/var/log/weatherparser` owned by `weatherparser`. Every run as `weatherparser`, without a terminal, standard input from `/dev/null`.

**Offline against the fixture host** (exit code, stdout bytes, the relevant log ID):

| Run | Exit | Result |
|---|---|---|
| `--Help` | 0 | 2780 bytes of help on stdout, stderr empty |
| unknown option `--Bogus` | 2 | `CLI-ARGUMENT-INVALID`, stdout empty |
| `--SelectDWD` | 0 | document with `stations` empty (the static fixtures contain no active station), `SELECTION-EMPTY` |
| `--ParseDWD` station `90001` (all `now` archives 404) | 1 | document with one error `OBSERVATION-NO-USABLE-READING` for `dwd:90001`, four `OBSERVATION-PRODUCT-MISSING` warnings (the fix of verification record section 11.2) |
| `--ParseOstHessen` pattern `M*` | 0 | one observation `osthessenwetter:mues-hohhuette`, `measured_at` `2026-10-31T08:30:00` (09:30 `Europe/Berlin`), `timestamp_quality` `estimated_date`, three quantities |
| `--ParseOstHessen` with the offline station | 3 | one observation and the error `OSTHESSEN-STATION-OFFLINE` for `osthessenwetter:langenbieber` |
| `--ParseOstHessen --Output xml` to a file | 0 | 1339 bytes owned by `weatherparser`, no temporary file |
| `--SelectMOSMIX` | 1 | `MOSMIX-S-EVIDENCE-MISSING` (no evidence index on the fresh install, SC-22) |
| `--ParseMOSMIX --Product MOSMIX_L` | 1 | `MOSMIX-ID-UNKNOWN` (the fixture set has no MOSMIX files) |
| `--ParseMOSMIX --Product MOSMIX_S` | 1 | `MOSMIX-RETRIEVAL-FAILED` (same reason) |

Standard output carried only the documents and standard error only the log lines in every run; the timestamps are UTC without suffix.

**Credential file (CL-06)**: the file of the layout accepted (`published=1`); a copy readable by the group rejected with exit 2 and `CLI-CREDENTIALS-INVALID`; the CLI run as root against the `0600` file of `weatherparser` (a foreign owner) rejected with exit 2 and the same ID.

**Signals** during a request held by a local server: SIGINT exit 4 after 141 ms, SIGTERM exit 4 after 160 ms; each with `CLI-CANCELLED`, no stdout, no output file, no temporary file, no success line.

**MQTT (RL-13 A)**: the P9.09 test broker on the Windows machine (Mosquitto 2.1.2, TLS with the test CA, the write-only identity `wp-pub` from the credential file), reached through an SSH reverse tunnel on the device's `127.0.0.1:18883`. The two publications of the setup (the credential check and the MQTT run) ended with exit 0 and `CLI-PUBLICATION-SUMMARY … published=1 failed=0 not_attempted=0`; a live observer on Windows (`wp-obs`, QoS 1, stale retained messages ignored) received both on `weatherparser-test/cli/measurements/osthessenwetter:mues-hohhuette/state` with QoS 1, 788 bytes each. The retained test message was cleared before the second setup.

**Live, one run per command in JSON mode** (the documents were summarized and not kept; OsthessenWetter values are not recorded, OW-14):

| Command | Exit | Result |
|---|---|---|
| `--SelectDWD` | 0 | 21 stations, `retrieved_at` `2026-11-01T04:56:18` |
| `--ParseDWD` station `01526` | 0 | one observation, `measured_at` `2026-11-01T04:20:00`, `timestamp_quality` `source`, three quantities; pressure omitted as missing (`OBSERVATION-READING-OMITTED`) |
| `--ParseOstHessen` Wasserkuppe | 1 | the page marked the station offline: item failure `OSTHESSEN-STATION-OFFLINE` (OW-12); in the first setup on this candidate the same command gave exit 0 with one observation (`timestamp_quality` `estimated_date`) |
| `--SelectMOSMIX --RefreshMosmixSEvidence` | 0 | 50 locations (one catalog record skipped, `CATALOG-RECORD-SKIPPED`, SC-04); the MOSMIX_S evidence index refreshed into the cache directory |
| `--ParseMOSMIX --Product MOSMIX_L` location `10637` | 0 | one run, `issued_at` `2026-11-01T03:00:00`, 247 steps, `run_age_exceeded` false; 107 `MOSMIX-ELEMENT-NOT-EXPORTED` lines |
| `--ParseMOSMIX --Product MOSMIX_S` location `10637` | 0 | one run, `issued_at` `2026-11-01T04:00:00`, 240 steps, `run_age_exceeded` false; 33 `MOSMIX-ELEMENT-NOT-EXPORTED` lines |

## 5. S4 – Scheduled series (RL-15 A)

- Cron file `/etc/cron.d/weatherparser-p910` (host time zone `Europe/Berlin`, user `weatherparser`), each line one CLI invocation through a recording wrapper: offline `--ParseOstHessen` every 10 minutes; offline `--SelectDWD` at minute 5; live `--ParseDWD` (`01526`) at minute 15; live `--ParseOstHessen` (Wasserkuppe) at minute 20; live `--ParseMOSMIX` MOSMIX_S (`10637`) at minute 50; live MOSMIX_L at `30 0,6,12,18` (after each run's publication, OPERATIONS section 3). Installed 2026-11-01 04:58Z, removed by `finish` 2026-11-02 17:36Z.
- The wrapper recorded for every invocation the UTC start, exit code, duration, peak resident memory, the number of CLI processes of the user before the start, the stderr line count and the stdout size and a short hash; the documents themselves were deleted after hashing (OW-14). stderr files were collected by `finish` without the `MOSMIX-ELEMENT-NOT-EXPORTED` lines.
- 374 invocations from 2026-11-01 05:00Z to 2026-11-02 17:30Z (36.5 hours): every scheduled invocation is present, none missing.

| Kind | Runs | Exit 0 | Exit 1 | Max duration | Max peak memory |
|---|---|---|---|---|---|
| offline `--ParseOstHessen` | 220 | 220 | 0 | 2.5 s | 78,868 KB |
| offline `--SelectDWD` | 37 | 37 | 0 | 1.5 s | 58,776 KB |
| live `--ParseDWD` | 37 | 37 | 0 | 39.9 s | 94,608 KB |
| live `--ParseOstHessen` | 37 | 35 | 2 | 12.9 s | 94,752 KB |
| live MOSMIX_S | 36 | 33 | 3 | 181.1 s | 202,492 KB |
| live MOSMIX_L | 7 | 7 | 0 | 2.4 s | 86,912 KB |

- The failures, each a reported item failure with a document and exit 1, none unexpected: live `--ParseOstHessen` at 2026-11-02 16:20Z and 17:20Z `OSTHESSEN-STATION-OFFLINE` (the page marked Wasserkuppe offline, OW-12); live MOSMIX_S at 2026-11-01 22:50Z, 23:50Z and 2026-11-02 16:50Z `MOSMIX-RETRIEVAL-FAILED` with the cause `CATALOG-RETRIEVAL-FAILED` after 19 to 30 s (the all-stations file of opendata.dwd.de could not be retrieved). The first two failures were consecutive and the run at 2026-11-02 00:50Z succeeded again; the failure at 16:50Z was the last MOSMIX_S invocation before `finish`, so no recovery after it was observed. Both OsthessenWetter failures were in the last two runs before `finish`. The diagnostic does not name the underlying cause (HTTP status, timeout or transport error), so the reason cannot be told from the log (section 9).
- Recurring stderr lines of successful runs: `OBSERVATION-READING-OMITTED` for the DWD pressure (37 times) and the OsthessenWetter humidity (35 times), `SELECTION-EMPTY` for the offline selection (37 times), `MOSMIX-ELEMENT-NOT-EXPORTED` (107 per MOSMIX_L run, 33 per MOSMIX_S run). No `CLI-UNEXPECTED`, no warning or error outside these IDs.
- Overlap: 80 pairs of invocations with overlapping recorded intervals (start time plus duration): the offline parse at minutes 20, 30 and 50 together with the live OsthessenWetter (37), MOSMIX_L (7) and MOSMIX_S (36) runs, each pair with the same start second, so the start order within a pair is not known at the log's one-second resolution; every run of such a pair ended with its usual exit code. The process count before the start was 0 in every invocation, i.e. no invocation was still running when a later minute's invocations started; the wrapper counts before its own CLI starts, so runs started in the same minute do not see each other.
- Memory: the peak resident set of the successful runs stayed between 58,284 and 94,752 KB, except MOSMIX_S (up to 202,492 KB while reading the about 36 MB all-stations file); every invocation is a new process, and the offline parse stayed between 77,740 and 78,868 KB over all 220 runs.
- Cleanup by `finish`: the cron file removed (checked absent); CLI processes left 0; temporary files left 0; the fixture host (399 requests over setup and series) stopped, processes left 0; every path created by the setup removed (0 left) and the user `weatherparser` removed. With no CLI process left, no open handle remains; open handles were not inspected per run.

## 6. Series attempts on this candidate

1. First setup 2026-11-01 04:35Z to 04:38Z: S3 and S5 as above (the live OsthessenWetter run exit 0); the Windows observer had exited on a retained message left by the first candidate, so the live deliveries were not observed, while a later subscriber received the new retained message (`retrieved_at` `2026-11-01T04:36:30`, QoS 1, 788 bytes). The user ran `finish` at 04:53Z after 3 series invocations (all exit 0); asked whether to rerun or accept a deviation from RL-15 A, the user chose "Rerun for 24 hours". Its logs are kept on the device with the suffix `-86e77eb-run1` until the cleanup.
2. Second setup 2026-11-01 04:55Z to 04:58Z and the series of section 5.

## 7. S5 – Operating-guide examples

- Deployment and credential file set-up ([OPERATIONS.md](../OPERATIONS.md) sections 1 and 2): executed by the setup script as root, the result as in section 4 (layout and CL-06 checks).
- Cron line (section 3), run by `/bin/sh` offline with the expected exit 1: the failure reached the journal through `logger` ("ParseDWD failed with exit code 1"), and the log file received the stderr lines (6 lines).
- Journal variant (section 5): the document on stdout, the log lines in the journal.
- logrotate stanza (section 5): `logrotate --debug` exit 0 without errors (a dry run; no rotation performed).
- n8n command line (section 3): executed as a shell command in its offline `--SelectDWD` form (JSON on stdout, exit code); n8n itself was not installed or run.
- Updates and rollback (section 1, "Updates"): checked separately on 2026-11-02 17:47Z by a root script run by the user (`p910-update.sh`, offline), after the Codex review of this record found the example unexercised (section 11). The candidate installed per the guide into `/opt/weatherparser/0.1.0` with `current` pointing to it; the same checksummed artifact unpacked into a second version folder `/opt/weatherparser/0.1.0-updatecheck` and `current` moved there with `ln -sfn` (update), then moved back (rollback), then the second folder removed. After each switch `--Help` ran as `weatherparser` without a terminal with exit 0 (2780 bytes) from the folder the symlink named. The guide's credential command created the file at the first installation (exit 0) and refused to replace it during the update (exit 1, file kept); the modes, owners and content hashes of `/etc/weatherparser` were unchanged after the update and after the rollback. Cleanup: 0 paths left, user removed.

## 8. Deviations

- The series ran 36.5 hours instead of the approved 24 hours, because `finish` was run at 17:36Z on 2026-11-02; the hourly live runs therefore exceed the 24-hour share: 37 runs each of DWD, OsthessenWetter and (36) MOSMIX_S instead of about 24, and 7 MOSMIX_L runs instead of 4; one OsthessenWetter page request per hour stays within the documented intervals (OPERATIONS section 7).
- The setup ran twice on this candidate (section 6); together with the one setup on `2a084be` (verification record section 11.1) the one-time live runs of the authorization were made three times.
- The root parts were run by the user as one script prepared by the orchestrating agent (option 2), not as individual guide commands.

## 9. Observations and limitations

- MOSMIX_S diagnostic: `MosmixSForecastSource` and `MosmixLForecastSource` translate a `CatalogRetrievalException` into `MOSMIX-RETRIEVAL-FAILED` with only its error ID; the underlying HTTP status or timeout is not in the message, so the three MOSMIX_S failures of the series cannot be explained from the log. Reported to the user; no change made.
- `--SelectMOSMIX` offline ran only its failure path (no MOSMIX fixtures, no evidence index on a fresh install, `MOSMIX-S-EVIDENCE-MISSING`); the live `--SelectMOSMIX` ran with `--RefreshMosmixSEvidence` into the evidence cache directory `/var/lib/weatherparser/mosmix-evidence` of the guide and passed, so the cache directory and the refresh are shown live.
- The broker ran on Windows and was reached through the SSH tunnel (as in P9.09); the series contains no MQTT runs.
- The series measures this device and these 36.5 hours (plan risk b); it is not a load test.
- The update and rollback check used the same artifact in both version folders, so it shows the symlink mechanics and that configuration and credentials stay untouched, not a change between two different versions; `--Help` was the command run after each switch.
- The packed `README.md` of the candidate still lists "Release acceptance of the candidate on the NanoPi R76S" as Planned (P9.10) in its section "Release and installation". It was not changed with this record, because the README is a package input: a change gives a new package and needs a new candidate (RL-06). Left to the user's decision in P9.11; recorded as a known limitation in [CHANGELOG.md](../../CHANGELOG.md).

## 10. Outcome

READY: S2 to S5 passed on the candidate's checksummed artifacts (S5 completed by the update and rollback check); every failure was a reported item failure of a live source with its contracted error ID; the deviations of section 8 and the observations of section 9 are for the user's acceptance in P9.11.

## 11. Review

- Per-step Codex review, round 1 (the uncommitted record on `86e77eb`): 4 findings, each verified against the raw logs and the plan and confirmed: [P2] S5 was declared passed although the guide's update and rollback example had not run (S5 requires every guide example): checked by the user's root run on the user's decision (section 7); [P2] the sentence that the next MOSMIX_S run succeeded after every failure was wrong (two consecutive failures, and none after the last one): corrected (section 5); [P3] the overlap was described as 80 later-starting invocations, while the log only shows 80 pairs of overlapping intervals with the same start second: corrected (section 5); [P3] the changelog's Debian compatibility row still said that the release acceptance follows in P9.10: corrected. While fixing the first finding the orchestrating agent found that the record wrongly called the MOSMIX_S evidence refresh unexercised; the live `--SelectMOSMIX` ran with `--RefreshMosmixSEvidence` (sections 4 and 9 corrected).
- Per-step Codex review, round 2 (the corrected record including the update and rollback check): 0 findings.
