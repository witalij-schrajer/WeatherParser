# WeatherParser OsthessenWetter Contract (P6)

**Status (2026-10-25): approved by the user on 2026-10-25 (section 1): OW-01 to OW-16 with option A in every row, OW-11 amended on the same day (single deviating wind row), after three Codex review rounds and one pull-request review (section 8). Binding for P6.03 to P6.11; the step plans are reconciled with it (section 6.1). Implemented in P6.03 to P6.10 and verified on candidate `4373399` and, after the PR #9 review fixes, on `12e7205` ([P6.10 record](P6_10_INTEGRATED_VERIFICATION.md)).** Plan: [P6_02_SOURCE_FACTS_AND_CONTRACT_PLAN.md](P6_02_SOURCE_FACTS_AND_CONTRACT_PLAN.md); phase plan: [PHASE_P6_PLAN.md](../../PHASE_P6_PLAN.md) section 3.

Written by the orchestrating agent from its own retrieval of 2026-10-25 (section 2), the planning check ([P6_PLANNING_PAGE_CHECK.md](research/P6_PLANNING_PAGE_CHECK.md)), REQUIREMENTS.md, the export contract ([P2_EXPORT_CONTRACT.md](../p2/P2_EXPORT_CONTRACT.md)) and the repository code. The retrieved files are not committed (copyright, REQ-083); this record holds URLs, times, sizes, hashes and structural facts only.

## 1. Approval

| Date | Scope | Decision | Wording |
| ---- | ----- | -------- | ------- |
| 2026-10-25 | OW-01 to OW-16 (after Codex rounds 3, 0, 0 findings and the PR #9 review of `7c2da0f` with 2 findings fixed in `ed7a982`, section 8) | Approved | User message: "I approve the P6 OsthessenWetter contract OW-01 to OW-16 as proposed, with the recommended option A for every row." — among them: AngleSharp 1.8.5 (OW-02), slug IDs with the collision check over all overview names (OW-03), consumer-supplied coordinates (OW-04), precipitation not exported (OW-06), the result type with station failures (OW-08), `PageDeadline` default 60 s (OW-10), offline stations as failures (OW-12), production use and redistribution not accepted until the conditions are clarified (OW-14), `retrieved_at` sampled after the page arrives (OW-16) |
| 2026-10-25 | OW-11 amendment (single deviating wind row; open question from the Codex P6.04 round 2) | Approved | User message: "Decision for P6.04: only the deviating wind row is ignored; its station gets an OSTHESSEN-WIND-ROW-UNMATCHED notice and no wind readings, all other stations keep their wind readings. Record it as an OW-11 amendment, implement it test-first and finish P6.04." |

## 2. Source register

One request per URL with `curl`, User-Agent `WeatherParser-P6.02/0.1 (one-off source fact check)`, no retries, no redirect:

| Retrieved (UTC) | URL | HTTP status | Content type | Bytes | SHA-256 |
| --- | --- | --- | --- | --- | --- |
| 2026-10-25T09:43:38Z | https://osthessen-wetter.de/messwerte/ | 200 | `text/html; charset=UTF-8` | 106534 | `b91eb9f62c56f8f96391f0f6647f9b6d5d83554b695fa47ba0c5bed43e6dd22c` |
| 2026-10-25T09:43:43Z | https://osthessen-wetter.de/robots.txt | 200 | `text/plain; charset=utf-8` | 222 | `bb68faf029190614e64249d19ea5382aaefae6aae962c317ed666a8f811ec14a` |
| 2026-10-25T09:43:45Z | https://osthessen-wetter.de/impressum-datenschutzerklaerung/ | 200 | `text/html; charset=UTF-8` | 96593 | `2fc9ade2e90171b3d17ec00be053cc77c9787230d2407c015a4ea6a6052ab926` |

Package sources (section 5) read on 2026-10-25 from the nuget.org registration API (`https://api.nuget.org/v3/registration5-gz-semver2/<id>/index.json`) and the package licence pages.

## 3. Verified page revision (OW-01)

Implementation date of the supported revision: 2026-10-25 (REQ-045). Every assumption below was checked on the retrieval of section 2 and agrees with the planning check of 2026-10-25T08:34Z (only the values and times differ).

| ID | Structural assumption |
| --- | --- |
| A1 | The page is UTF-8 (`<meta charset="UTF-8">`, HTTP `charset=UTF-8`); WordPress 6.6.2 |
| A2 | The overview table is the first `<table class='tg'>` whose header cells, after whitespace normalization, are in this order: empty, `Wetterstation`, `Temp.2m Änderung`, `Temp.- Trend`, `Tmax/ Tmin`, `Spitzen- böe`, `Nieder- schlag`, `Luft- feuchte` (the labels contain `<br>`) |
| A3 | The wind table is the `<table class='tg'>` whose header cells are: empty, `Wetterstation letzte Daten`, `Spitzen- böe heute`, `Spitzen- böe verg.10Min.`, `Wind Ø 10 Min.`, `Wind Ø 60 Min.` |
| A4 | A third `<table class='tg'>` (snow line, last precipitation, annual precipitation) has neither header and is not read |
| A5 | Each station row has, in this order: a status cell with an `<img>` whose `title` is `Station online` or `Station offline`; a name cell with the name in `<font size='2em'>` and the time in `<font size='1'>`; then one cell per remaining header (overview: 6 value cells, of which `Tmax/Tmin` holds two values; wind: 4 value cells) |
| A6 | The time text is `HH:MM Uhr` (two-digit hour and minute, a space, `Uhr`) or empty; there is no date anywhere in the table |
| A7 | The overview `Temp.2m Änderung` cell contains two separate values: the temperature in `<font size='3'>` with its nested `°C` unit, followed after `<br>` by the change in `<font size='1'>` with a `°C/h` unit and a sign (`+1.1`, `-0.5`). P6.04 extracts only the primary temperature and its unit for OW-06/OW-11; it must not pass the combined cell text to the numeric reader or substitute the change when the temperature is absent or unreadable. The adjacent `Temp.-Trend` cell is an image (its `title` repeats the change) and is not a numeric reading; `Tmax/Tmin` contains two `°C` values and is not exported. The other numeric cells are empty or contain one number with `.` as the decimal separator (optional leading `-`) and a nested unit: overview gust `km/h`, precipitation `mm`, humidity `%`; every wind value column `km/h`. (Corrected on 2026-10-25 after the PR #9 review; the first draft described the temperature cell as a single value and the trend column as numeric.) |
| A8 | Offline rows (3 of 20 on both retrievals) show no time and no values |
| A9 | Names are given with character references; the HTML5 rules decode them, including legacy references without a semicolon: overview `Ro&szligbach` → `Roßbach`; wind `M&uuml;s-Hohh&uumltte;` → `Müs-Hohhütte;` |
| A10 | Both tables list the same 20 stations, but the spelling differs (`Roßbach`/`Rossbach`, `Müs-Hohhütte`/`Müs-Hohhütte;`) and the order differs (`Bad Salzschlirf` and `Gotthards` swapped) |
| A11 | The markup is not well-formed: unclosed `<b>` and `<font>`, stray `</td>`, header cells outside `<tr>`, a header cell closed as `</th</font>` |
| A12 | Other page sections (daily-extreme lists) can contain a database error message with a database user and host name instead of values; they are outside both tables |
| A13 | The page shows no station identifier, no coordinates and no altitude |
| A14 | The page states no reference period for `Niederschlag` and labels the gust columns "heute" (today) and "verg.10Min." (past 10 minutes) and the means "10 Min." and "60 Min." |

## 4. Source conditions (OW-14)

- No licence for the measured values and no machine-readable interface (REQ-092 already states the latter).
- The imprint states that the copyright for objects the author created remains with the author, and that reproducing or using such graphics, sound documents, video sequences and texts in other electronic or printed publications is not permitted without the author's express consent. It has a liability disclaimer (no warranty for currency, correctness, completeness or quality).
- No attribution rule and no request-frequency rule.
- robots.txt: `User-agent: *` with an empty `Disallow:`; it does not restrict crawling and grants no licence.
- The imprint names a private person as the operator with a postal address and an e-mail address; this record does not repeat them. Contacting the operator is the user's decision.

## 5. HTML parser evaluation (OW-02)

| Package | Latest stable (2026-10-25) | Published | Licence | .NET 10 dependencies | Probe on the A9/A11 defects |
| --- | --- | --- | --- | --- | --- |
| AngleSharp | 1.8.5 | 2026-10-15 | MIT (licence expression) | none for `net10.0` and `net8.0` | `Ro&szligbach` → `Roßbach`, `M&uuml;s-Hohh&uumltte;` → `Müs-Hohhütte;` (browser behavior); unclosed and stray tags tolerated (1 table, header and two station rows found); `<script>` content not executed; `title` attributes read |
| HtmlAgilityPack | 1.13.1 | 2026-10-12 | MIT (licence expression) | none (`netstandard2.0`) | `Ro&szligbach` and `&uumltte;` stay undecoded, also with `HtmlEntity.DeEntitize` and `WebUtility.HtmlDecode`; its tree held 2 rows where AngleSharp builds 3 (the implicit header row and both station rows), because the header cells stand outside a `<tr>` |

- Probe: a local console program (outside the repository, SDK 10.0.402) parsed one synthetic snippet with the A9 and A11 defects, a script and a stylesheet and image link to `unreachable.invalid`; results as in the table. The default AngleSharp configuration has no resource loader and no scripting, so no request is issued for linked resources.
- Both packages have no listed deprecation or vulnerability on nuget.org. AngleSharp publishes frequent releases (1.8.1 to 1.8.5 between 2026-09-10 and 2026-10-15).
- Platform assessment (expected compatibility, not execution evidence): the AngleSharp 1.8.5 package contains only managed assemblies (`lib/net10.0`, `lib/net8.0`, `lib/netstandard2.0`, `lib/net462`, `lib/net472`, each `AngleSharp.dll`), no `runtimes/` folder and no native library, and has no dependency for `net10.0`; it is therefore expected to run unchanged on `linux-arm64`. Actual execution on Debian 13 ARM64 is verified in P6.10 (T-P6-17).
- AngleSharp offers `HtmlParser.ParseDocumentAsync(Stream, CancellationToken)`; the reader nevertheless copies the HTTP body asynchronously into memory first (P6.04), so that no synchronous read of the live body can happen inside the package.

## 6. Decision register

Approved on 2026-10-25 with option A in every row (section 1); the other options are rejected alternatives.

| ID | Decision | Options | Recommendation | Facts | Blocks |
| --- | --- | --- | --- | --- | --- |
| OW-01 | Supported page revision | A: the revision of section 3 (assumptions A1 to A14), implementation date 2026-10-25, URL https://osthessen-wetter.de/messwerte/; a later structural change is a format deviation (OW-11) and may need a parser update (REQ-045) / B: no fixed revision | A: REQ-045 requires a documented revision; without it a format deviation could not be defined | Section 3 | P6.04 |
| OW-02 | HTML parsing | A: AngleSharp 1.8.5 (MIT), pinned in `Directory.Packages.props`, `DEPENDENCIES.md` updated, default configuration without scripting and without a resource loader / B: HtmlAgilityPack 1.13.1 (rejected: does not decode the A9 references) / C: own tokenizer (rejected: would reimplement the HTML5 reference and tree rules) | A: the only candidate that decodes the observed markup like a browser; MIT, managed-only, no dependency for `net10.0` | Section 5 | P6.04 |
| OW-03 | Station identity and source ID | A: `station_id` = slug of the overview name: NFC, invariant lower case, `ä`→`ae`, `ö`→`oe`, `ü`→`ue`, `ß`→`ss`, each run of other characters outside `[a-z0-9]` → `-`, leading and trailing `-` removed; a name without any `[a-z0-9]` after these rules → `OSTHESSEN-FORMAT-CHANGED`; `source` `osthessenwetter:<slug>` (C-50); the original name in `location.name`; the collision check runs over all overview names, so two names with one slug fail the parse with `OSTHESSEN-STATION-ID-COLLISION` whatever the pattern / B: the original name as `station_id` / C: collision check only over the matched names | A: ASCII IDs are stable for topics (P8) and files; checking all names makes an ID independent of the pattern (B keeps umlauts and spaces in IDs; C lets the same page give different outcomes per pattern) | A10, A13; on the retrieval the 20 names give 20 distinct slugs | P6.05, P6.07 |
| OW-04 | Coordinates (C-51) | A: consumer-supplied `OsthessenWetterStationMetadata` entries (`Name` exact after NFC, `Latitude`, `Longitude`, optional `AltitudeM`) in `OsthessenWetterOptions`, validated at construction (ranges as C-51, non-empty unique names, no null entry → `OSTHESSEN-OPTIONS-INVALID`); a matched station without an entry is a station failure `OSTHESSEN-STATION-METADATA-MISSING` / B: coordinates built into the library (rejected: unverified, not traceable) / C: optional coordinates in the export contract (rejected: not additive, C-03) | A: the only option that keeps C-51 and uses verified data; the consumer owns its station knowledge | A13; C-51 | P6.05, P6.06, P6.07, P6.09 |
| OW-05 | Joining the two tables | A: the overview table is the station list; a wind row joins when exactly one wind row has the same join key (NFC, invariant case fold, `ß`→`ss`, trailing `;` and whitespace removed); none or several → notice `OSTHESSEN-WIND-ROW-UNMATCHED` and no wind readings for that station / B: join by position (rejected: A10) | A: tolerates the observed spelling differences; an ambiguous join never mixes two stations (REQ-031 analogue) | A10; on the retrieval all 20 wind rows join with key A | P6.04, P6.05 |
| OW-06 | Quantities, units and periods | A: exported `Temp.2m` → `Temperature` (°C, instantaneous, overview time); `Luftfeuchte` → `RelativeHumidity` (%, overview time); `Wind Ø 10 Min.` → `MeanWindSpeed` (`KilometresPerHour`, period `[t − 10 min, t]` at the wind time t); not exported: both gust columns and `Spitzenböe` (gusts, REQ-044), `Wind Ø 60 Min.` (one wind field), `Niederschlag` (period unverified, A14; never a rate, REQ-055), `Temp.-Trend` and `Tmax/Tmin` (no REQ-052 field); the unit suffix is checked per column (A7) / B: as A, but `Niederschlag` as `PrecipitationAccumulation` over `[local midnight, t]` (rejected while the period is unverified) | A: REQ-055 allows a rate only with a known period; A14 gives none | A7, A14 | P6.05 |
| OW-07 | Times | A: `\A([01][0-9]\|2[0-3]):[0-5][0-9] Uhr\z` → `TimeOnly`; `ISourceTimeNormalizer.InferDate(time, retrievedAtUtc)` with the retrieval instant truncated to whole seconds → UTC, `estimated_date`, `Europe/Berlin` (REQ-042, C-30 to C-33); the overview time for the overview readings, the wind time for the wind reading (REQ-056); missing or unreadable time → no reading for that table row, omission `MissingTime`; DST gap or overlap → notice `OSTHESSEN-TIME-UNRESOLVED` and no reading for that table row; no freshness filter (REQ-043) / B: an unresolved or missing time fails the whole station (station failure) instead of omitting that table row's readings / C: an unresolved DST time resolved to the earlier offset (rejected: REQ-043 forbids arbitrary resolution, C-31) | A: keeps the readings of the other table, whose time may be valid (REQ-056 per-value times), and every omission stays visible through its reason; B hides valid wind or overview readings | A6 | P6.05 |
| OW-08 | Public API and partial failure | A: `IOsthessenWetterParser.ParseAsync(string stationNamePattern, CancellationToken)` → `OsthessenWetterParseResult` (`Datasets` in overview order, `StationFailures` with name, error ID and reason); no match → `OsthessenWetterException` `OSTHESSEN-PATTERN-NO-MATCH`; matches without any dataset → `OSTHESSEN-NO-USABLE-STATION` carrying the failures / B: only datasets, failures logged / C: all-or-nothing | A: every match stays visible to the consumer (REQ-041), and a station without readings is never a dataset (REQ-050); B leaves failures only in logs; C lets one offline station hide all others | REQ-041, REQ-050 | P6.07 |
| OW-09 | Pattern grammar | A: `*` any number of characters, `?` exactly one Unicode scalar after NFC, every other character literal; invariant case folding; whole-name match against the NFC overview name; null, empty, whitespace-only or more than 128 characters → `OSTHESSEN-PATTERN-INVALID` before any request; matching in at most O(pattern length × name length) steps / B: regular expressions (rejected, REQ-040) | A: REQ-040 fixes the glob semantics; the bound keeps a 128-character pattern harmless | REQ-040 | P6.03 |
| OW-10 | Retrieval and options | A: `OsthessenWetterEndpoints.PageUri` (absolute HTTP or HTTPS, no user information; diagnostics without the query) and `OsthessenWetterOptions.PageDeadline` with default **60 s**, permitted (0, 1 h], validated at construction; one GET through `ICatalogDownloader` with an additive `CatalogDeadlineKind.OsthessenWetter`, `MaxBytes` 2 MiB (2097152), `NotFoundIsAbsent` false, no retries; non-success status, transport failure, size overflow and the downloader's own 30-second request timeout → `OSTHESSEN-PAGE-UNAVAILABLE`; deadline → `OSTHESSEN-DEADLINE-EXCEEDED`; caller cancellation first / B: as A with default 30 s (equal to the downloader's request timeout, `CatalogDownloader.RequestTimeoutSeconds`; both fire together and the downloader classifies the failure as the deadline, so a stalled request is never reported as unavailable; a tie test pins that) | A: a default above the request timeout lets a stalled request surface as `OSTHESSEN-PAGE-UNAVAILABLE` (timed out) while the deadline bounds the whole parse, as in P5 (120 s deadline, 30 s request timeout) | Section 2: 106534 bytes; `CatalogDownloader` | P6.06, P6.09 |
| OW-11 | Format deviations | A: overview table not found, its header changed, a row without the A5 cells, or a status other than A5 → `OSTHESSEN-FORMAT-CHANGED` (failure); wind table not found or its header changed → notice `OSTHESSEN-WIND-TABLE-MISSING`, no wind readings; a value outside the A7 grammar or with another unit → notice `OSTHESSEN-VALUE-UNREADABLE` and no reading; numbers parsed culture-invariantly; page text other than station names never logged (A12); **Amendment 2026-10-25 (user decision, section 1):** a wind-table row that deviates from A5 (another cell count, no station name, or a status image title other than A5) is ignored; it never fails the parse and never makes the whole wind table deviate. The station the row names (when its name cell can be read) gets the notice `OSTHESSEN-WIND-ROW-UNMATCHED` and no wind readings, even when another valid row has the same join key; every other overview station without exactly one valid wind row gets the same notice; all other stations keep their wind readings. This replaces the interim P6.04 interpretation (one deviating row → `OSTHESSEN-WIND-TABLE-MISSING` for the whole table). / B: a missing wind table also fails the parse / C: an unreadable value fails the station | A: the overview table carries the station list and two of three quantities, so it is required; the wind table and single cells are optional, so their loss is a notice with omitted readings and not a failure (REQ-044 logs, REQ-046 no fabricated values) | A2 to A7, A12 | P6.04, P6.05 |
| OW-12 | Offline and stale stations | A: `Station offline` → station failure `OSTHESSEN-STATION-OFFLINE` even if values are shown; stale displays are not filtered (REQ-043); a displayed time more than 5 minutes later than the local retrieval time goes to the previous day (C-30, after the C-31 checks); older displays are not detectable (limitation) / B: the values of an offline row are exported when present / C: a freshness cutoff for old displays (rejected: REQ-043) | A: the source marks the station offline, so its values are not current observations; B would export what the source itself flags | A5, A8 | P6.04, P6.07 |
| OW-13 | Error and notice IDs | `OsthessenWetterException`: `OSTHESSEN-PATTERN-INVALID`, `OSTHESSEN-PATTERN-NO-MATCH`, `OSTHESSEN-PAGE-UNAVAILABLE`, `OSTHESSEN-DEADLINE-EXCEEDED`, `OSTHESSEN-FORMAT-CHANGED`, `OSTHESSEN-STATION-ID-COLLISION`, `OSTHESSEN-NO-USABLE-STATION`, `OSTHESSEN-OPTIONS-INVALID`; station failures `OSTHESSEN-STATION-OFFLINE`, `OSTHESSEN-STATION-METADATA-MISSING`, `OSTHESSEN-STATION-NO-READING`; notices `OSTHESSEN-WIND-TABLE-MISSING`, `OSTHESSEN-WIND-ROW-UNMATCHED`, `OSTHESSEN-VALUE-UNREADABLE`, `OSTHESSEN-TIME-UNRESOLVED`, `OBSERVATION-READING-OMITTED` (reused, OC-10) / B: reuse the DWD and catalog IDs where they fit (for example `CATALOG-RETRIEVAL-FAILED`) | A: provider-specific IDs keep diagnostics and consumer handling unambiguous, as P5 did with `STATION-*` and `OBSERVATION-*`; only the generic omission notice is shared | OW-03 to OW-12 | P6.03 to P6.07 |
| OW-14 | Source conditions | A: implementation and tests with synthetic fixtures only; README states the conditions of section 4, the attribution "Osthessen-Wetter.de" and a project recommendation of at most one retrieval every 10 minutes, scheduled externally, with the consumer's User-Agent; production use and redistribution (for example MQTT) are not accepted until the user records the operator's consent or other clarified conditions / B: production acceptance without clarified conditions (rejected, DEVELOPMENT_PLAN.md P6 acceptance) | A: the imprint forbids reproduction without consent; implementation with synthetic data reproduces nothing, while production use would | Section 4 | P6.09, P6.11 |
| OW-15 | Live verification | A: optional, `WEATHERPARSER_LIVE_CHECK=1` with `WEATHERPARSER_LIVE_CHECK_MODE=osthessen`, budget one request, summary to the gitignored run directory, the page not written; run only with the user's authorization in P6.10 / B: no live check | A: one request is the minimum that shows the documented revision still holds; the user decides given OW-14 | REQ-083 | P6.10 |
| OW-16 | Dataset values | A: provider `osthessenwetter`; `location` from the overview name and the OW-04 entry; `retrieved_at` from the consumer's `TimeProvider`, sampled once on successful return from the page source after acquisition and reading of the page body, before station mapping or date inference, and truncated to whole seconds; this same instant is passed to every `InferDate` call and reused by every dataset; the parse deadline still starts before the retrieval; P6.07 includes a deterministic delayed-response test that starts before local midnight, returns after midnight with a `00:00` reading, and verifies that the reading receives the response's local date; dataset `measured_at` the latest exported reading (C-55); `timestamp_quality` `estimated_date` (C-56); `source_timezone` `Europe/Berlin` on each reading (C-53); no `quality` member; export contract unchanged / B: `retrieved_at` sampled before the request, as in the P5 parser (`DwdObservationParser` reads the clock before retrieval; rejected for P6: a request starting at Berlin 23:59:55 and returning at 00:00:05 with a `00:00` display would be dated one day early under C-30) / C: an additive optional dataset member, for example a source-revision note (rejected: not needed for REQ-050; would need C-03 and C-105 handling) | A: the inferred date must refer to the moment the displayed times were read; C-49 to C-60 already cover every member the page supplies; the page shows no quality code, so `quality` stays absent rather than invented | C-49 to C-60, C-30 | P6.07, P6.08 |

### 6.1 Items for the plan reconciliation (S5)

Recorded from the PR #9 review of `7c2da0f`; applied to the step plans in S5 on 2026-10-25 together with the other approved values:

- P6.04: the reader returns the primary temperature of the compound `Temp.2m` cell (A7) separately from the change and never passes the combined text on; fixtures reproduce the compound cell and the trend image.
- P6.05: composed reader-plus-mapper tests with positive, negative and zero changes, and with a missing or unreadable primary temperature next to a valid change, proving that the change is never exported as temperature.
- P6.07: `retrieved_at` sampled after the page source returns (OW-16) and the delayed-response test across local midnight; the parse deadline still starts before the retrieval.

## 7. Traceability

| T-P6 ID | OW rows |
| --- | --- |
| T-P6-01 | OW-09 |
| T-P6-02 | OW-01, OW-02, OW-11 |
| T-P6-03 | OW-05 |
| T-P6-04 | OW-06, OW-11, A7 (compound temperature cell) |
| T-P6-05, T-P6-06 | OW-07, OW-12, OW-16 (sampling point of `retrieved_at`) |
| T-P6-07 | OW-03 |
| T-P6-08 | OW-04 |
| T-P6-09 | OW-10 |
| T-P6-10, T-P6-11 | OW-08, OW-12, OW-16 |
| T-P6-12 | OW-16 |
| T-P6-13 | OW-04, OW-10 |
| T-P6-14 | OW-14 |
| T-P6-18 | OW-15 |
| T-P6-19 | OW-10, OW-11, OW-13 |
| T-P6-15 | None: cross-cutting obligation (REQ-085, recorded Red → Green → Refactor per slice), P6.03 to P6.09 |
| T-P6-16 | None: cross-cutting obligation (REQ-086, native block coverage against the remeasured target, baseline 7491/7494 at `8d8079a`), P6.10 |
| T-P6-17 | OW-02 (managed-only package, section 5); otherwise cross-cutting (REQ-017, REQ-084, NanoPi R76S), P6.10 |

## 8. Review rounds

Codex five-angle review, read-only (2026-10-25):

| Round | Output (outside the repository) | Findings | Disposition |
| --- | --- | --- | --- |
| 1 | `p602_codex_r1.md` | 3 (P2: T-P6-15/16/17 missing from section 7; P3: no target-platform assessment of the package; P3: OW-07, OW-11, OW-12, OW-13, OW-16 without alternatives and several recommendations without a reason) | All confirmed and fixed by the orchestrating agent |
| 2 | `p602_codex_r2.md` | 0 | – |
| PR #9 review of `7c2da0f` (2026-10-25, review 5643795568) | GitHub threads on lines 37 and 87 | 2 (P2: A7 described the compound temperature cell as one value; P2: OW-16 left the sampling point of `retrieved_at` open) | Both confirmed by the orchestrating agent against the retrieved page and `DwdObservationParser` and fixed (A7, OW-16, section 6.1, section 7) |
| 3 | `p602_codex_r3.md` | 0 | – |
