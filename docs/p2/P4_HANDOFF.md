# P4 Handoff – Station and Location Selection

Status: prepared with the P2.12 acceptance record on 2026-10-20; in effect since P2 was accepted and merged ([P2.12 acceptance record](P2_12_ACCEPTANCE.md)); P4 started on 2026-10-20 on the user's instruction.

## 1. Accepted foundation

- Verified candidate `bced7be380c3e637fdd042c4165cd074e25f25a6` on branch `features/develop/P2`, draft pull request #3 into `develop` (PR base `64e460b475b4a29b9e3ddd331394076e53982d9e`); P2 is complete: pull request #3 was merged into `develop` on 2026-10-20; the P4 integration target is the merge commit `74bf625eb4b2fbf49cbce7d1cee42b6302f2e47b`.
- Package `WeatherParser.Parsing` `0.1.0-initial` (not published); it contains `lib/net10.0/WeatherParser.Parsing.dll`, `README.md`, `LICENSE` and the output schema `schemas/export/v1/weatherparser-export-v1.xsd`.
- Native block coverage of `WeatherParser.Parsing.dll`: 3035/3037 (99.93 %) on Windows and on the NanoPi R76S; the next PR's gate compares against the then-remeasured base.
- Contract: [P2_EXPORT_CONTRACT.md](P2_EXPORT_CONTRACT.md) (C-01 to C-116), namespace `urn:weatherparser:export:v1`, `schema_version` `"1"`; v1 freezes at P2 acceptance; afterwards only additive optional members (C-03, C-105).
- Evidence: [P2_VERIFICATION.md](P2_VERIFICATION.md).

## 2. Contracts and helpers for P4

- List roots (C-16 to C-27, section 7 of the contract): `CdcStationList` (`cdc_station_list`) with `CdcStationListEntry`, `MosmixLocationList` (`mosmix_location_list`) with `MosmixLocationListEntry` and `MosmixProductAvailability`, shared `SearchParameters`; product enum `MosmixProduct` (`MOSMIX_S`, `MOSMIX_L`) and availability enum `AvailabilityState` (`available`, `unavailable`, `unknown`).
- Rules P4 must respect: IDs kept as strings with leading zeros, MOSMIX IDs never CDC IDs (REQ-101–102); required members tracked (C-27); `available_forecast_elements` empty unless availability is `available`, one item per product (C-23, C-91); no null collections or items (C-91).
- Ordering helper: `IListEntryOrdering`/`ListEntryOrdering` (C-25, distance then ordinal ID, deterministic), registered by `AddWeatherParsing`.
- Serializers: `IJsonExportSerializer`, `IXmlExportSerializer` (five supported roots, C-88); DI entry point `AddWeatherParsing` (all services singletons, C-107).
- Validation: the committed schema and the secure validation settings of C-104 (CONTRIBUTING.md section 3).

## 3. P4 responsibilities

- Coordinate parsing (decimal degrees and DMS with hemisphere, culture-independent range validation), geodesy (distance), inclusive distance and altitude filters.
- Both catalogs: the DWD CDC station catalog and the separate MOSMIX location catalog with its own IDs (REQ-020–024, REQ-100–102).
- Availability derivation per product, marking unknown availability explicitly; exclusion of locations without altitude where the filter needs it.
- The empty-result log notice; filling `SearchParameters` and list metadata; using `IListEntryOrdering` for the order.
- No new P2 contract members without a recorded additive contract decision (C-03).

## 4. Responsibilities of later phases

- P5 (DWD) and P6 (OsthessenWetter): provider extraction, source units, quality codes, time extraction into the P2.04 normalization service (`ISourceTimeNormalizer`), building observations with `IObservationDatasetBuilder`; P6 owns the OsthessenWetter page times and the `estimated_date` inference tests with page fixtures.
- P7 (MOSMIX_S, MOSMIX_L): separate parsers, the DWD input schema `pointforecast_dwd_extension_V1_0.xsd` (REQ-112, kept separate from the output schema), source-reader alignment, element-to-quantity mapping; forecast-only quantities (for example gusts, probabilities, totals) as additive contract changes; runs through `IForecastRunBuilder`.
- P8: the standalone `observation_dataset` and `forecast_run` roots are the MQTT JSON payloads; topics come from consumer configuration.
- P3 (after P8): CLI, configuration, file/stdout output, exit codes, broker transport. P9: publication, package ID and feed, hardware release acceptance (REQ-084).

## 5. Working rules

- Same working rules as in P2: qwen3-coder:30b generates through `/api/chat` with real API references and a file allowlist; the orchestrating agent plans, reviews, runs builds, tests and coverage, and records authorship (AGENTS.md).
- Contract decisions are reviewed by the OpenAI Codex CLI before the user approves them (up to 10 rounds); implementations get a Codex review after the agent's own checks.
- Test-first with recorded Red evidence; MSTest, Moq (strict, `VerifyAll()` in `[TestCleanup]`), FluentAssertions; standard tests offline.
- Draft PR early, CI on every push, Auto-fix on, ready for review at phase end, the user merges.

## 6. Open items and limitations

- Coarse XML cancellation (before serialization and during the copy, C-10).
- Several steps record characterization evidence where the behavior already existed (listed per T-P2 row in the acceptance record).
- The hosted `debian13-arm64` job is a container, not target evidence; target evidence comes from the NanoPi R76S.
- Builders and root validation implement the same rules separately; equivalence tests guard divergence (P2.08).
- The `de_DE.UTF-8` system locale is not installed on the target; culture checks use the ICU data of .NET.
- CONTRIBUTING.md section 7 requires the local `ci-commit`/`ci-target` branches (P2.11 defect D1, fixed).
