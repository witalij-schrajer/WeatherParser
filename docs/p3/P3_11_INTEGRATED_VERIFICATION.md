# P3.11 – Integrated Verification of the Example CLI

**Status (2026-10-30): outcome READY on candidate `5696357` on Windows, hosted CI and the NanoPi R76S, with the authorized broker run and live checks (section 7); two README statements corrected afterwards (section 8).** Plan: [P3_11_INTEGRATED_VERIFICATION_PLAN.md](P3_11_INTEGRATED_VERIFICATION_PLAN.md). Contract: [P3_CLI_CONTRACT.md](P3_CLI_CONTRACT.md).

## 1. Candidate, target and authorization

- Candidate: `5696357` (`569635751044557b146fafc46ada847d2c64a82d`), the head after P3.10 (its code equals `1f95001`; `5696357` changed only the verification record). Integration target: `develop` `e623a83` (`e623a834bdb2902cec930258a62a48302f626716`).
- User authorization (2026-10-30, chat), sent after the orchestrating LLM had stated that the NanoPi run, the live source checks and the broker run of P3.11 need an explicit authorization: "I authorize all three runs". Scope as applied: S3 (the NanoPi R76S run on the candidate), S4 (the broker integration tests against the local Mosquitto that the user authorized for P3.08, "Local Mosquitto, no TLS"; it listens on the Windows machine's loopback interface only, so S4 ran from Windows and not from the NanoPi), S5 (one live run per command in JSON mode and one MQTT run against the test broker).
- Decisions (orchestrator, recorded, plan section 3): the live configuration lived in a temporary directory outside the repository and was deleted after the runs; this record names the public source hosts and the IDs used, never local paths or credentials. Evidence of a live run is the exit code, the root, counts, sources and timestamps; no document is committed (source conditions; OW-14 for OsthessenWetter values).
- Scripts (outside the repository, session scratchpad): the fake source, the Windows checks, the Ctrl+C helper, the NanoPi run and the live checks; the synthetic OsthessenWetter page of the process tests (`ProcessFixtures.OsthessenPage`, 3602 bytes) served as the fake source.

## 2. S1 – Windows

- `pwsh ./scripts/ci-local.ps1 -Commit 5696357 -TargetRef e623a83… -RunSettings coverage.runsettings`: exit 0; library 3213 passed and 7 skipped of 3220, coverage 118, runner 61, schema generation 46, package consumer 61, CLI 700 passed and 9 skipped of 709 (the 5 opt-in broker tests and the 4 Linux-only signal tests); library gate passed with 11102/11103 native blocks (gated inputs unchanged); CLI gate passed with 2855/2875 native blocks (99.30 %, bootstrap rule, the target has no CLI assembly). Run directory `artifacts/ci/5696357-20261030T191825Z` (local, not committed).
- App-host smoke test, `examples/WeatherParser.Cli/bin/Release/net10.0/WeatherParser.exe` built from the candidate as in CONTRIBUTING.md section 4 (SHA-256 `4e37aeb36f6ca1d0d031eaca7909ec2f346c8690c929fabbc4372d3e1e7a83a9`): `--Help` exit 0, 2826 bytes of usage on stdout, nothing on stderr; `--ParseOstHessen --StationNamePattern M* --Output json` against the loopback fake source exit 0, one complete document on stdout (867 bytes, one observation, no error), nothing on stderr.
- Real console Ctrl+C: a helper in its own console started the app host there with `--ParseOstHessen --StationNamePattern M* --Output json --OutputFile out.json` against a fake source that held its response, waited until the request arrived and sent `CTRL_C_EVENT` to the console with `GenerateConsoleCtrlEvent(0, 0)` (method recorded per plan). Result: exit code 4 22 ms after the event; stderr `WARNING CLI-CANCELLED … Shutdown signal SIGINT received; the command stops within 00:00:05.` and `ERROR CLI-CANCELLED … The command was cancelled by a shutdown signal; its output is incomplete and not reported as successful.`; stdout empty; no `out.json` and no temporary file. A first attempt ran into the 30 s request timeout without receiving the event: the helper had inherited a disabled Ctrl+C from the non-interactive parent; the helper now enables Ctrl+C processing before it starts the CLI (a property of the test setup, not of the CLI). A system shutdown (`CTRL_SHUTDOWN_EVENT`, SIGTERM on Windows) was not provoked; it would shut the machine down.

## 3. S2 – Hosted CI

- The candidate push changed only documentation, so the automatic run (41114130056) skipped the jobs; a manual run on the candidate (`workflow_dispatch`, target `e623a83`, run 41114462647): changes, windows-x64, ubuntu-x64 and debian13-arm64 succeeded. The hosted `debian13-arm64` container is not target evidence (S3 is).

## 4. S3 – NanoPi R76S

- Procedure (CONTRIBUTING.md section 7): a Git bundle of `features/develop/P3` and the target (2 708 899 bytes, SHA-256 `4eba564e82d3a450b813a5c36ce58c3ca2551e63fadb97a28df65588f6e353d5`, identical on the device), a fresh clone, detached checkout at `569635751044557b146fafc46ada847d2c64a82d`, `scripts/prepare-debian-arm64.sh`, `ci-local.ps1`.
- Device: NanoPi R76S, Linux 6.1.141 aarch64, Debian 13.3, .NET SDK 10.0.402, runtime 10.0.13, PowerShell 7.6.7.
- `ci-local.ps1` (2026-10-30T19:23:24Z to 19:33:47Z): exit 0; library 3213 passed and 7 skipped of 3220, coverage 118, runner 61, schema generation 46, package consumer 61, CLI 702 passed and 7 skipped of 709 (the 5 opt-in broker tests and the 2 Windows-only file-lock tests); library gate passed with 11102/11103 native blocks; CLI gate passed with 2928/2987 native blocks (98.02 %, bootstrap rule). Run directory copied to `artifacts/ci/arm64-5696357-p311/` (local, not committed). The CLI counters differ from Windows because the instrumented CLI contains platform-specific paths (for example the Unix file-mode check and the Linux-only signal tests reach code that the Windows run does not, and the reverse for the Windows-only tests); the tests shared by both platforms have identical outcomes.
- CLI on the device: the first device build after `ci-local.ps1` compiled against a stale cached `WeatherParser.Parsing` 0.1.0-initial package of an earlier phase in the user's NuGet cache (CONTRIBUTING.md section 4, "Stale cached package"); the build was repeated in the same clone with a separate package folder (`NUGET_PACKAGES` in the run directory, the user's cache untouched): 0 errors. Then:
  - Linux signal process tests (`ProcessSignalTests`, `ProcessTerminationTests`): 6 of 6 passed (`Process_Sigint_DuringSlowRetrieval_Exit4_NoTemporaryFile`, `Process_Sigterm_DuringSlowRetrieval_Exit4`, `Process_OneCommandThenExit_NoLingeringProcess`, `InProcess_SignalDuringSlowRetrieval_Exit4_NoOutputFile`, `Process_SecondSignal_TerminatesImmediately`, `Process_GracePeriodExpired_Terminates`).
  - App host `./WeatherParser` (SHA-256 `15a823805884be5c7d7b69eb3e6d2412d3fbbdaefa6c61c7ffe071e35a8d4766`): `--Help` exit 0, 2780 bytes on stdout (LF line ends; Windows 2826 with CRLF), nothing on stderr; the JSON command against the loopback fake source exit 0, one complete document (859 bytes, one observation).
  - Real signals sent with `kill` to the app host during a held JSON command with `--OutputFile`: SIGINT exit 4 after 98 ms, SIGTERM exit 4 after 117 ms; each with `ERROR CLI-CANCELLED` on stderr, stdout empty, no `out.json` and no temporary file.

## 5. S4 – Test broker

- Broker: Mosquitto 2.1.2 (Windows service, loopback, anonymous, no TLS), as authorized for P3.08.
- `WEATHERPARSER_TEST_BROKER` set to the test configuration outside the repository; `dotnet test tests/WeatherParser.Cli.Tests -c Release --no-build --filter TestCategory=Broker` on the candidate build: `Broker_PublishAndSubscribe_RetainedObservationReceivedOnce`, `Broker_StationList_NotRetained`, `Broker_SecondRunSameSource_ReplacesRetainedMessage` passed; `Broker_WrongCredentials_Rejected` and `Broker_TlsWithCustomCa` skipped (the authorized broker has no credentials and no TLS). TLS and authenticated broker connections therefore remain unverified against a real broker.

## 6. S5 – Live checks

Windows x64, the candidate's app host, 2026-10-30; sources: `opendata.dwd.de` (CDC 10-minute `now` data and station lists, MOSMIX_L single station, MOSMIX_S all stations), `www.dwd.de` (MOSMIX station catalog), `osthessen-wetter.de` (measurement page). Search point of both selections: 50.5549, 9.6738, 300 m, 50 km, 200 m.

| Run | Start (UTC) | Exit | Result |
|---|---|---|---|
| `--SelectDWD … --Output json` | 19:24:19 | 0 | `cdc_station_list`, source `dwd:cdc-10min-now`, 21 stations (first `01526`, `03911`, `02066`), no log line |
| `--ParseDWD --StationID 01526 --Output json` | 19:24:20 | 0 | `export_document` with 1 observation dataset `dwd:01526`, no error; 1 × `OBSERVATION-READING-OMITTED` |
| `--ParseOstHessen --StationNamePattern Wasserkuppe --Output json` | 19:24:20 | 0 | `export_document` with 1 observation dataset `osthessenwetter:wasserkuppe`, no error; 1 × `OBSERVATION-READING-OMITTED`; placeholder coordinates (as in P6); the document was read for this summary only, not kept and not redistributed (OW-14) |
| `--SelectMOSMIX … --RefreshMosmixSEvidence --Output json` | 19:26:12 | 0 | `mosmix_location_list`, source `dwd:mosmix-stationskatalog`, 50 locations (first `10536`, `L585`, `L582`); the MOSMIX_S evidence refreshed; 1 × `CATALOG-RECORD-SKIPPED`; 1 × `CLI-DEADLINE-EXCEEDED` warning (refresh plus selection deadline 12 min above the 10 min command deadline, CL-14) |
| `--ParseMOSMIX --Product MOSMIX_L --MOSMIXID 10637 --Output json` | 19:25:40 | 0 | `export_document` with 1 forecast run `dwd-mosmix-l:10637`, issued 2026-10-30T15:00:00, 247 steps; 107 × `MOSMIX-ELEMENT-NOT-EXPORTED` |
| `--ParseMOSMIX --Product MOSMIX_S --MOSMIXID 10637 --Output json` | 19:25:40 | 0 | `export_document` with 1 forecast run `dwd-mosmix-s:10637`, issued 2026-10-30T18:00:00, 240 steps; 33 × `MOSMIX-ELEMENT-NOT-EXPORTED` |
| `--ParseMOSMIX --Product MOSMIX_L --MOSMIXID 10637` (MQTT) | 19:25:51 | 0 | `Publication summary: published=1 failed=0 not_attempted=0.`; the retained message on `weatherparser-test/p311/forecasts/dwd-mosmix-l:10637/state` read back (172 471 bytes, source `dwd-mosmix-l:10637`, 247 steps) and removed afterwards; one TLS-disabled warning |

- Runs that did not reach a source and were repeated, recorded: the first attempt of the three `--ParseMOSMIX` runs ended with `CLI-CONFIG-INVALID` (`MOSMIX-OPTIONS-INVALID`: `MaxRunAge` must be set) before any request, because the live configuration lacked `MaxRunAge`; the first `--SelectMOSMIX` without `--RefreshMosmixSEvidence` ended with `MOSMIX-S-EVIDENCE-MISSING` (exit 1) before any request, the library's documented behavior without an index; the first run with the flag downloaded the MOSMIX_S file and ended with `EVIDENCE-CACHE-FAILED`, because the live configuration named a cache directory that did not exist (the library requires an existing directory). After the configuration was completed, each command ran once more as listed; in total the MOSMIX_S file was downloaded three times (two refreshes and one MOSMIX_S parse), the other sources once each.
- The live runs found two README defects (section 8); the CLI behaved as contracted in every run.

## 7. Outcome

**READY** on candidate `5696357`: S1 (Windows), S2 (hosted CI, run 41114462647) and S3 (NanoPi R76S) passed with the documented skips only (Windows: the 5 broker tests and the 4 Linux-only signal tests; NanoPi: the 5 broker tests and the 2 Windows-only file-lock tests), both coverage gates on every platform, the app-host smoke tests, the real Windows Ctrl+C and the real SIGINT and SIGTERM on the NanoPi; S4 passed as far as the authorized broker supports (no credentials, no TLS); S5 passed for every command and the MQTT run. No defect of the CLI is open. Not verified: a Windows system shutdown, authenticated and TLS broker connections, and the CL-06 owner check, which is not implemented (an open item of the phase).

## 8. README corrections after the runs

- The live checks showed two wrong or missing statements in the README section "Example CLI" (P3.10): without `--RefreshMosmixSEvidence` and without a usable index `--SelectMOSMIX` fails with `MOSMIX-S-EVIDENCE-MISSING` before any request (exit 1), it does not only log a notice; and `Mosmix.L.MaxRunAge` and `Mosmix.S.MaxRunAge` are required for `--ParseMOSMIX` and the evidence cache directory must exist. The orchestrating LLM corrected both statements; README tests 6 of 6; README package check: README, LICENSE and schema identical in the package, README SHA-256 `cb8843408a9374aca75727ee91eb09d9a697fe5f0df35ab85a990f5609499f6d`, consumer 61/61.
- Effect on the candidate: no source, test or project file changed, only the README text, which the package contains. S1 (`ci-local.ps1`) and S2 run again on the commit with the correction (section 9); S3 to S5 are not repeated, because the compiled CLI and library are unchanged.

## 9. Runs on the README correction and review

- Recorded in [P3_VERIFICATION.md](P3_VERIFICATION.md) section 11.
