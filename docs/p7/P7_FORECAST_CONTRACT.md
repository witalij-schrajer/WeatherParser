# WeatherParser MOSMIX Forecast Contract (P7)

**Status (2026-10-26): approved by the user on 2026-10-26 (section 1): MF-01 to MF-18 with the recommended option in every row, after four Codex review rounds and one pull-request review (section 9). Binding for P7.03 to P7.12; the phase plan and the step plans are reconciled with it (P7.02 S6).** Plan: [P7_02_SOURCE_FACTS_AND_CONTRACT_PLAN.md](../development/p7/P7_02_SOURCE_FACTS_AND_CONTRACT_PLAN.md); phase plan [PHASE_P7_PLAN.md](../development/PHASE_P7_PLAN.md) section 3.

Written by the orchestrating agent from its own retrievals of 2026-10-26 (section 2), REQUIREMENTS.md, the export contract ([P2_EXPORT_CONTRACT.md](../p2/P2_EXPORT_CONTRACT.md)), the selection contract ([P4_SELECTION_CONTRACT.md](../p4/P4_SELECTION_CONTRACT.md)) and the repository code on `features/develop/P7`. The retrieved files and the probe programs are kept outside the repository (REQ-083); this record holds URLs, times, sizes, hashes and structural facts only, never forecast values.

User authorization (2026-10-26, chat): "I authorize the P7.02 S1 retrievals (one GET each, including one MOSMIX_S all-stations file of about 37.5 MB) and the S3 replay probe on the NanoPi R76S. Keep SDK 10.0.402."

## 1. Approval

| Date | Scope | Decision | Wording |
| ---- | ----- | -------- | ------- |
| 2026-10-26 | MF-01 to MF-18 (after Codex rounds 5, 2, 0 and 0 findings and the PR #11 review of `3582936` with 1 finding fixed in `0a1fa17`, section 9) | Approved | User message: "I approve the P7 MOSMIX forecast contract MF-01 to MF-18 as proposed, with the recommended option in every row, including the MF-11 partial-failure rule of 0a1fa17." Open after the approval: adopting the CRC-32 check in the P4 evidence readers (MF-06) stays a proposal for the user; unless the user approves an SC-20/SC-22 amendment, P7.12 records it as a limitation |

## 2. Source register

One GET each on 2026-10-26 with the User-Agent `WeatherParser-P7.02-source-check/1.0`, no retries, all HTTP 200:

| Source | URL | UTC | Bytes | SHA-256 | Use |
| ------ | --- | --- | ----- | ------- | --- |
| Binding extension schema, revision V1_0 (Last-Modified 2018-12-06) | https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd | 20:33:48Z | 6483 | `3365bd2b3463f0833fc981cfaef7553291ff84316acf030286bdab608dc02c1c` (equal to SC-20) | Schema derivation (section 3) |
| DWD element definitions (Last-Modified 2024-10-22) | https://opendata.dwd.de/weather/lib/MetElementDefinition.xml | 20:33:49Z | 26484 | `dc3f5190f86bfeddcd979ab7b7e1ae86a0a64ea26e7062afe1f508d7c8509775` (equal to P4) | Element table (section 4) |
| MOSMIX procedure description (Last-Modified 2025-06-18) | https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_verfahrenbeschreibung_gesamt.pdf?__blob=publicationFile&v=6 | 20:33:49Z | 2108107 | `ff090737148fb9b9ce4c684fd2c7dd26a9e46f999db840f79569fa3e174b5b1e` | Cycles (section 2.1.3, table 2.1), element descriptions (annex) |
| MOSMIX product page (EN) | https://www.dwd.de/EN/ourservices/met_application_mosmix/met_application_mosmix.html | 20:33:49Z | 52782 | `7f76774a5773457170e1d9f855210a2ee5d8e887bb944ee8567adafc65525747` | Product description |
| Open-data README | https://opendata.dwd.de/README.txt | 20:33:49Z | 1280 | `e5f2e884e21cc5517a77969acdba3f2921da3f5b753d52526917ac8134661d3b` | Access and terms reference |
| DWD legal notice (terms of use) | https://www.dwd.de/EN/service/legal_notice/legal_notice_node.html | 20:33:50Z | 45826 | `30322d908d1e90af1daebf82f7851c06d5cf1e2521c159117293e5f41024b0b5` | Source conditions (section 6) |
| MOSMIX_S directory listing | https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_S/all_stations/kml/ | 20:33:50Z | 6854 | `3f7b8b06f3e7c7c72b942a9405c3e7e9c96f26b509d62fda16b0b356476d4e70` | Run file names, publication times |
| MOSMIX_L directory listing (10637) | https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/10637/kml/ | 20:33:50Z | 1490 | `022c76903310ffafa650e9c660fdfaf80505c0132b24b32de91be95535886c09` | File naming |
| MOSMIX_L location file (10637; Last-Modified 16:16:06Z) | https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/10637/kml/MOSMIX_L_LATEST_10637.kmz | 20:33:57Z | 16945 | `a3a5b3d79eff62024f1970bcc226890b51ad52219d62515b9965bf06332bec5c` | File facts (section 5) |
| MOSMIX_S all-stations file (Last-Modified 19:39:50Z) | https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_S/all_stations/kml/MOSMIX_S_LATEST_240.kmz | 20:33:57Z | 36316660 | `003897168257189544a02cd52f63779afc3c88ac6dec2ab83273ccc13226612a` | File facts (section 5), probes (section 7); download 2.92 s on Windows |
| MOSMIX station catalog | https://www.dwd.de/DE/leistungen/met_verfahren_mosmix/mosmix_stationskatalog.cfg?view=nasPublication&nn=16102 | 20:34:00Z | 299502 | `770c7cbbfdc16ef38d156a066b34801d9c1c4c1e7ae2b750f6be75126dcb36f5` (equal to P4) | ID grammar (MF-03) |
| MOSMIX_L `single_stations/` index | https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/ | 20:34:01Z | 687573 | `b0ce5281ddf000e0adad389bd56b699903b9eb77155707fffd4dfeee359f1074` (equal to P4) | ID grammar (MF-03) |
| .NET runtime source `ZipArchiveEntry.cs`, tag `v10.0.13` | https://raw.githubusercontent.com/dotnet/runtime/v10.0.13/src/libraries/System.IO.Compression/src/System/IO/Compression/ZipArchiveEntry.cs | 20:36:13Z | 80092 | `7e43ef556b72e03ec1b5894dceba86bbc104f5bd81adf0d9e4fd7a187bfe0993` | CRC-32 behavior (section 7.1) |
| .NET runtime source `ZipCustomStreams.cs`, tag `v10.0.13` | https://raw.githubusercontent.com/dotnet/runtime/v10.0.13/src/libraries/System.IO.Compression/src/System/IO/Compression/ZipCustomStreams.cs | 20:36:06Z | 20429 | `cb081c58dfa2369d0fb294c9d8cc10f7fc6dea71e212f1a51c99698d3364f3a9` | CRC-32 behavior (section 7.1) |

## 3. Schema derivation (REQ-112)

Manual derivation from `pointforecast_dwd_extension_V1_0.xsd` (target namespace `https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd`, `elementFormDefault` and `attributeFormDefault` `qualified`); no code generation. The schema defines only the `dwd:` extension; the surrounding KML structure is taken from the published files (section 5).

| Element or attribute | Type | Cardinality (schema) | Observed in both files | Use in P7 |
| --- | --- | --- | --- | --- |
| `dwd:ProductDefinition` | sequence | global element | once, in `kml:Document/kml:ExtendedData` | required exactly once |
| `dwd:Issuer` | `xs:string` | 1..1 | `Deutscher Wetterdienst` | not exported; MF-07 |
| `dwd:ProductID` | enumeration (`EZ_MOS`, `ICON_MOS`, `MOSMIX`, `COSMO_DE_EPS_EMOS`, `MODELMIX`, `GPTMOS`, `ICON_DMO`, `ICON_NEST_DMO`) | 1..1 | `MOSMIX` | required, must equal `MOSMIX` |
| `dwd:GeneratingProcess` | `xs:string` | 1..1 | `DWD MOSMIX hourly, Version 1.0` | not exported; MF-07 |
| `dwd:IssueTime` | `xs:dateTime` | 1..1 | `2026-10-26T15:00:00.000Z` (L), `2026-10-26T19:00:00.000Z` (S) | `issued_at` |
| `dwd:ReferencedModel` with `dwd:Model` (attributes `dwd:name`, `dwd:referenceTime`, both required) | sequence | 1..1, `Model` 0..unbounded | two models (`ICON`, `ECMWF/IFS`) | not exported; MF-07 |
| `dwd:ForecastTimeSteps` with `dwd:TimeStep` | `xs:dateTime` list | 1..1, `TimeStep` 0..unbounded | 247 (L), 240 (S) | timeline (`valid_at`) |
| `dwd:FormatCfg/dwd:DefaultUndefSign` | `dwd:UndefChar` (string, length 1), default `-` | `FormatCfg` 1..1 (no `minOccurs`), `DefaultUndefSign` 1..1 | `-` | missing-value marker; MF-07, MF-12 |
| `dwd:MetElementDefinition` | `xs:anyURI` | 1..1 per schema, default the element-definition URL | absent in both files | not used |
| `dwd:Forecast` | sequence with one `dwd:value` | global element | one per element, in `kml:Placemark/kml:ExtendedData` | one series per element |
| `dwd:Forecast/@dwd:elementName` | `xs:string` | optional per schema | present on every forecast | required, unique per placemark |
| `dwd:value` | `dwd:floatList` = list of `xs:float` or a one-character undefined sign | 1..1 | whitespace-separated tokens | tokens per MF-07, MF-12 |

Differences from SC-20 and the files: the schema allows any `xs:float` lexical form (exponents, `INF`, `NaN`), while the files use only `-` and plain decimals; SC-20 restricts tokens to `^-?\d{1,9}(\.\d{1,6})?$`, which P7 keeps (MF-07). The schema makes `dwd:elementName` optional; P7 requires it. The schema's undefined sign is configurable through `FormatCfg`; P7 accepts only `-` (MF-07). `dwd:MetElementDefinition` is required by the schema but absent in the files, so a strict schema validation would reject the published files; P7 therefore validates the enumerated structure (as SC-20) and does not validate against the XSD at runtime.

KML structure (both files): root `kml:kml` (namespace `http://www.opengis.net/kml/2.2`) with one `kml:Document`, whose children are one `kml:ExtendedData` (holding `dwd:ProductDefinition`) and the placemarks; each `kml:Placemark` has the children `kml:name`, `kml:description`, `kml:ExtendedData` (holding the `dwd:Forecast` elements) and `kml:Point/kml:coordinates`.

## 4. Element table (MF-10, MF-11, MF-18)

Units and definitions from `MetElementDefinition.xml` (137 entries); the period wording from the same file or, where it is missing, from the element annex of the procedure description. Presence: `L` in the MOSMIX_L file (114 elements), `S` in the MOSMIX_S file (40 elements, the same set for all 5648 placemarks).

| Element | Unit | Definition (element definitions) | Procedure description annex | Presence | Proposed use |
| --- | --- | --- | --- | --- | --- |
| `TTT` | K | Temperature 2m above surface | Temperatur in 2m (point value) | L, S | `temperature_c` (MF-10) |
| `Td` | K | Dewpoint 2m above surface | Taupunkt in 2m Höhe | L, S | not exported (MF-10 humidity option A) |
| `PPPP` | Pa | Surface pressure, reduced | Luftdruck; the Fimos product list names "Reduzierter Luftdruck (hPa; 12 UTC)" | L, S | `pressure_hpa`, `pressure_mmhg` with the MF-10 reference |
| `DD` | `0°..360°` (the file stores the degree sign UTF-8 encoded inside an ISO-8859-1 document, so it reads `0Â°..360Â°`) | Wind direction | Windrichtung | L, S | `wind_direction_deg` |
| `FF` | m/s | Wind speed | Windgeschwindigkeit in 10m Höhe | L, S | `wind_speed_kmh` (mean wind) |
| `FX1` | m/s | Maximum wind gust within the last hour | Maximale Böe der letzten Stunde | L, S | `wind_gust_kmh` (MF-11 option A) |
| `FX3`, `FXh` | m/s | Maximum wind gust within the last 3 / 12 hours | — | L, S | not exported |
| `Rad1h` | kJ/m2 | Global Irradiance (no period stated) | Absolute Globalstrahlung in der letzten Stunde | L, S | `solar_irradiance_w_m2` as the mean over the last hour (MF-10) |
| `RR1c` | kg/m2 | Total precipitation during the last hour consistent with significant weather | Niederschlagsmenge in der letzten Stunde | L, S | `rain_rate_mm_h` as the mean over the last hour (MF-10) and `precipitation_amount_mm` as the 1-hour total (MF-11 option A) |
| `RR3c`, `RR6c`, `RRhc`, `RRdc` | kg/m2 | Total precipitation during the last 3 / 6 / 12 / 24 hours … | — | `RR3c` L, S; others L | not exported |
| `RRS1c`, `RRS3c`, `RRL1c` | kg/m2 | Snow-rain equivalent / liquid precipitation during the last hour(s) | — | L (S: `RRS1c`, `RRS3c`) | not exported |
| `R101`, `R602`, `R650`, `wwP`, … | % (0..100) | Probabilities | — | L, S (subset) | not exported (MF-11) |
| `N`, `Neff`, `Nh`, `Nm`, `Nl`, `N05` | % (0..100) | Cloud cover | — | L, S | not exported |
| `VV`, `SunD1`, `ww`, `W1W2`, `TX`, `TN`, `T5cm`, … | various | Visibility, sunshine, weather codes, extremes | — | L, S | not exported |

Unit conversions: `TTT` K → °C by subtracting 273.15 before the builder (the builder's `Temperature` kind expects °C); `PPPP` Pa → hPa by dividing by 100 before the builder (the builder accepts hPa or mmHg); `Rad1h` kJ/m² → J/m² by multiplying by 1000, then the builder's `RadiationEnergy` → `Irradiance` conversion over the one-hour period (`IPeriodRateConverter`, REQ-055); `RR1c` kg/m² = mm (water density 1000 kg/m³, 1 kg/m² = 1 mm), passed as `PrecipitationAccumulation` with the one-hour period; `FF` and `FX1` m/s → km/h by the builder (C-77).

Vocabulary check for MF-18 (checked by the orchestrating agent with a script on the retrieved files): every element name of the P4 availability evidence (SC-05: the 114 MOSMIX_L names and the 40 MOSMIX_S names listed in the P4 contract section 3.3) appears in the files of 2026-10-26 with the same names and is defined in `MetElementDefinition.xml` (0 undefined of 114 and of 40), and every name proposed for mapping (`TTT`, `PPPP`, `DD`, `FF`, `FX1`, `Rad1h`, `RR1c`) is among them. The P7.05 test (T-P7-23) takes the complete P4 vocabulary (both lists) and the mapped names as input and asserts that each is in the recorded set of defined element names (the 137 `ShortName` values of the retrieved `MetElementDefinition.xml`, copied as a test constant without definition texts) and that every mapped name is in the vocabulary of its product.

## 5. File facts (MF-03, MF-05, MF-06, MF-07, MF-08)

| Fact | MOSMIX_L (10637) | MOSMIX_S (all stations) |
| --- | --- | --- |
| Archive | one member `MOSMIX_L_2026102615_10637.kml`, deflate | one member `MOSMIX_S_2026102619_240.kml`, deflate |
| Compressed / unpacked bytes | 16773 / 344437 (file 16945) | 36316492 / 627662773 (file 36316660) |
| XML declaration | `<?xml version="1.0" encoding="ISO-8859-1" standalone="yes"?>` | same |
| Issue time and file publication | 15:00Z, published 16:16Z (table 2.1: about 16:20) | 19:00Z, published 19:39:50Z (table 2.1: about 45 minutes after each full hour) |
| Timeline | 247 steps, hourly, 16:00Z to 2026-11-05T22:00Z; form `yyyy-MM-ddTHH:mm:ss.000Z` | 240 steps, hourly, 20:00Z to 2026-11-05T19:00Z; same form |
| Placemarks | 1 (`kml:name` `10637`, `kml:description` `FRANKFURT/M`, coordinates `8.6,50.05,111.0`) | 5648, all names unique; 49 descriptions contain non-ASCII characters (ISO-8859-1) |
| Placemark ID patterns | `99999` | `99999` 2696, `A9999` 1512, `A999` 1440 |
| Coordinates | `lon,lat,alt` decimal degrees with a decimal point | same; negative longitudes, latitudes and altitudes occur (10 placemarks below sea level) |
| Elements per placemark | 114, unique names | 40, unique names, the same set everywhere |
| Tokens | 16276 numeric, 11882 `-`; at most 6 integer and 2 fraction digits; no other form | 29721266 numeric, 24499534 `-`; at most 6 integer and 2 fraction digits; no other form |
| Entirely missing series | `wwT`, `wwT6`, `wwTh`, `wwTd` | for example `T5cm` at 2763, `Rad1h` at 1839, `FX1` at 1405 placemarks |

Observations against the cited documentation: table 2.1 of the procedure description lists MOSMIX_S as "hourly output, 40 parameters, up to +24h", but the file of 2026-10-26 19 UTC holds 240 hourly steps (+1 h to +240 h); the parsers take the horizon from the file timeline (REQ-110, MF-13), so neither length is assumed. The MOSMIX_S directory listing of 2026-10-26 20:33Z (a snapshot, not a documented retention policy) holds 48 timestamped hourly runs, `MOSMIX_S_2026102420_240.kmz` to `MOSMIX_S_2026102619_240.kmz` (36192774 to 37287136 bytes), and `MOSMIX_S_LATEST_240.kmz`, identical in size and time to the newest run.

MOSMIX IDs (MF-03): catalog of 2026-10-26 (5649 IDs; unchanged since P4): patterns `99999` 2696, `A9999` 1513, `A999` 1440; MOSMIX_L `single_stations/` (6045 directories, every catalog ID present, 396 not in the catalog): `99999` 2698, `A999` 1798, `A9999` 1514, `AA999` 35; no directory name outside `[A-Z0-9]`. All observed IDs match `^[A-Z0-9]{4,5}$`, the SC-22 placemark grammar.

## 6. Source conditions (MF-16)

- Open-data access without registration; DWD stores the requester's IP address for at most 7 days (README of the open-data server).
- Terms of use (DWD legal notice): re-use of DWD content is permitted under the Creative Commons licence CC BY 4.0 provided the source is duly acknowledged; open spatial data, spatial data services and high-value datasets may be re-used under CC BY 4.0 with acknowledgement of the source. Unchanged since P4 section 3.4.
- No request-rate limit is documented. Publication times (table 2.1): MOSMIX_S about 45 minutes after each full hour; MOSMIX_L runs 03, 09, 15 and 21 UTC, available at about 04:20, 10:15, 16:20 and 22:15 UTC; the first 24 hours of every run are updated hourly, +25 h to +240 h only in the four main runs.

## 7. Technical checks

### 7.1 Archive integrity (S2)

- Runtime source at `v10.0.13`: `ZipArchiveEntry.Open()` in read mode returns the decompressor over a `SubReadStream` (`GetDataDecompressor`: a `DeflateStream`, a `DeflateManagedStream` or the stored data itself); the CRC-32 is computed only on the write path (`CheckSumAndSizeWriteStream` in `ZipCustomStreams.cs`), and the entry's `Crc32` property only exposes the central-directory value.
- Local probe (synthetic archives, outside the repository, SDK 10.0.402, runtime 10.0.13, Windows): a stored member with one digit changed (still valid XML, same length) and a deflated member whose CRC-32 is changed in the local header and the central directory are both read to the end without an exception.
- Probe source `p702-crcprobe/probe.cs` in the session scratchpad (SHA-256 `c79e1aadf902206ed886f2ad22b472131aa39ce3257ab08464eccebe71ff328c`), run with `dotnet run probe.cs` (file-based app, default configuration) outside the repository.
- Result: the pinned runtime does not verify the entry CRC-32. MF-06 needs an own check.

### 7.2 Replay probe (S3)

A probe outside the repository replays the S1 MOSMIX_S file from disk (no new download): it reads the compressed file into memory (as the P4 refresher does), opens the single member, computes its CRC-32 (IEEE 802.3, table-driven) while streaming, reads the whole member with `XmlReader` (DTD prohibited, no resolver, whitespace, comments and processing instructions ignored, asynchronous reads), checks placemark uniqueness, retains the 40 series of placemark `10637` (9600 tokens) and reports the absolute process peak working set (`Process.PeakWorkingSet64`). Three runs per platform; the file SHA-256 was checked in each run and, on the NanoPi R76S, after the transfer (`sha256sum`, equal to section 2).

| Platform | Parse (stream to the end, CRC-32, retained placemark) | Peak working set | CRC-32 equals the central directory |
| --- | --- | --- | --- |
| Windows 11 x64, runtime 10.0.13 | 2.01, 2.01, 2.02 s | 94486528, 94031872, 93925376 B | yes |
| NanoPi R76S, Debian 13 ARM64, runtime 10.0.13 | 8.98, 8.97, 8.86 s | 106467328, 106582016, 106721280 B | yes |

Commands: Windows `dotnet build -c Release probe.cs`, then three times `dotnet run -c Release --no-build probe.cs -- <path>/MOSMIX_S_LATEST_240.kmz 10637`; NanoPi R76S: `scp` of `probe.cs` and the KMZ to `~/p702-probe`, `sha256sum` of the KMZ, then the same build and run commands with `DOTNET_ROOT=$HOME/.dotnet` (SDK 10.0.402). Probe source `p702-probe/probe.cs` in the session scratchpad, SHA-256 `0e7cce2af2cfa69832601e4f96c508ba63cbf2670d9212e14e618ab1c6580820` (the same file on both platforms); the structural facts of sections 4 and 5 come from `p702/analyze.py` (Python standard library, `iterparse`, nothing extracted to disk), SHA-256 `3280addb0d0ff32555e4e01de86d5c7bd9c34ebcd98117c92949aeaa5c9bc4bc`. The probe files were removed from the NanoPi R76S afterwards. The probe has none of the library's bound checks (markup guard, counting stream, bounded texts), so the library's parse will be slower; P7.11 measures the candidate on the target.

## 8. Decision register

Each row lists the options, the recommendation and the T-P7 IDs it governs; the recommended option of every row is approved (section 1), the other options are rejected alternatives. Names in the rows are the implementation names of P7; P7.03 to P7.10 use them.

| ID | Decision | Options and recommendation | T-P7 |
| --- | --- | --- | --- |
| MF-01 | Source facts and schema record | This record: sources of section 2, schema revision V1_0 with SHA-256 `3365bd2b…dc02c1c` (unchanged since SC-20), the manual derivation of section 3, the element table of section 4 and the file facts of section 5. A: approve this record as the source basis of P7 (recommended) / B: request further sources before approval | T-P7-05, T-P7-23 |
| MF-02 | Public API | A (recommended): `IMosmixLParser.ParseAsync(string mosmixId, CancellationToken cancellationToken)` and `IMosmixSParser.ParseAsync(string mosmixId, CancellationToken cancellationToken)`, each returning `Task<MosmixParseResult>` with `Run` (`ForecastRun`), `OmittedValues` (the C-78 list of the builder: omissions of mapped elements, with their `QuantityKind`), `NotExportedElements` (the names of present elements that are not mapped, in file order; they are not passed to the builder, because most of them have no `QuantityKind`, and the existing `OmittedForecastValue` stays unchanged), `AbsentElements` (mapped element names not in the file), `Freshness` (`MosmixRunFreshness.Fresh` or `Stale`) and `RunAge` (`TimeSpan`); one shared result type is allowed because it carries no product switch (the product is in `Run.Product`). Rejected runs: P7.03 adds the additive member `IReadOnlyList<OmittedForecastValue> OmittedValues` to `ForecastRunRejectedException`, filled by the builder (empty for input failures), and the parsers log every omission before `MOSMIX-NO-USABLE-VALUE` propagates and pass the builder's list and `NotExportedElements` in their exception. Logging: one `FORECAST-VALUE-OMITTED` entry per builder omission (valid time, kind, reason) and one `MOSMIX-ELEMENT-NOT-EXPORTED` notice per unmapped element name, both before success or rejection is returned. B: return `ForecastRun` only and log the rest. C: one generic interface with a product parameter (rejected: REQ-109) | T-P7-11, T-P7-13, T-P7-17 |
| MF-03 | MOSMIX ID validation | Validated first, before any request, timer or log of the ID's content: A (recommended): `^[A-Z0-9]{4,5}$` ordinal for both products (all IDs of section 5 match; the same grammar as the SC-22 placemark names), so C-69 (no whitespace, no `:`) holds; lowercase letters are rejected, never upper-cased; the ID is kept exactly. B: only the C-69 rule (any non-empty string without whitespace and `:`), with an unknown ID reaching the source. A violation → `MOSMIX-ID-INVALID` | T-P7-01 |
| MF-04 | Endpoints and options | A (recommended): consumer-registered per product, independent of `MosmixCatalogEndpoints`: `MosmixLForecastEndpoints.SingleStationsDirectory` (absolute HTTP(S) directory URI, path ending with `/`, no query, no fragment, no user information — the SC-12 directory rules; the file URI is `<directory><id>/kml/MOSMIX_L_LATEST_<id>.kmz` with the ID percent-encoded as a path segment, SC-12) and `MosmixSForecastEndpoints.AllStationsUri` (absolute HTTP(S) file URI without user information, query allowed, used unchanged); options `MosmixLForecastOptions` and `MosmixSForecastOptions` with `ParseDeadline` and `MaxRunAge` (MF-05, MF-09). All validated at construction without retrieval → `MOSMIX-OPTIONS-INVALID` (`MosmixForecastException`). Every consumer of `AddWeatherParsing` registers all four (as P6 for OsthessenWetter; documented in README). B: reuse `MosmixCatalogEndpoints` (rejected: couples selection and forecast settings) | T-P7-02, T-P7-15 |
| MF-05 | Retrieval bounds and deadlines | One GET per parse through `ICatalogDownloader` with the consumer's `HttpClient`, no retries. Bounds: MOSMIX_L 1 MiB compressed (`CatalogRequest.PerStationFileMaxBytes`; observed 16945 B) and 16 MiB unpacked (observed 344437 B); MOSMIX_S 64 MiB compressed (`MosmixSAllStationsMaxBytes`; observed 36.3 MB) and 2 GiB unpacked, streamed (observed 628 MB). `ParseDeadline`: MOSMIX_L default 60 s; MOSMIX_S default 5 minutes (measured: download 2.9 s on Windows, stream parse 9 s on the NanoPi R76S; the library's guarded reader is slower, so the default keeps a wide margin); permitted (0, 1 h] for both; additive `CatalogDeadlineKind.MosmixL` and `MosmixS` with the existing kinds and mappings unchanged and re-tested. Request bound: A (recommended): an additive optional `CatalogRequest` member `RequestTimeout` (`TimeSpan?`, absent = the existing 30 s), set by the MOSMIX_S parser to its `ParseDeadline`, by the MOSMIX_L parser left absent (30 s); every P4, P5 and P6 caller keeps 30 s (re-tested). B: keep 30 s for MOSMIX_S (rejected: download plus guarded parse on a slow link can exceed 30 s, as the downloader's bound covers the reader callback) | T-P7-03 |
| MF-06 | MOSMIX_S reading and archive integrity | The parse streams the whole member to its end; the requested placemark's series are retained, every other placemark is checked against the structural bounds and discarded; duplicate IDs fail wherever they occur; MOSMIX_L uses the same reader with exactly one placemark. CRC-32: the pinned runtime does not verify it (section 7.1); A (recommended): an internal table-driven CRC-32 (IEEE 802.3 polynomial `0xEDB88320`, reflected, initial and final XOR `0xFFFFFFFF`; tested against the check value `CBF43926` of the ASCII string `123456789`) computed while streaming and compared with the entry's central-directory `Crc32` at the end of the member; a mismatch → `MOSMIX-FORMAT-INVALID`; enabled only on the forecast paths (both products); the P4 evidence readers keep their behavior (pinned by characterization tests in P7.04). B: the `System.IO.Hashing` package (a new dependency for about 20 lines of code; not recommended). Adopting the check in the P4 readers is offered to the user as an SC-20/SC-22 amendment proposal and otherwise recorded as a limitation in P7.12. Several IDs per download are not offered | T-P7-06 |
| MF-07 | Document validation | The SC-20 structure (section 3 and SC-20 text) extended for full parsing: namespaces `kml` and `dwd` as in section 3; one `kml:Document`; `dwd:ProductDefinition` exactly once with exactly one `dwd:ProductID` `MOSMIX`, exactly one `dwd:IssueTime` (SC-20 form, valid UTC, zero fraction → whole-second `issued_at`), exactly one `dwd:ForecastTimeSteps` with 1 to 1000 `dwd:TimeStep` children (SC-20 instant form, zero fraction, strictly ascending, so no duplicates), and, if present, `dwd:FormatCfg/dwd:DefaultUndefSign` equal to `-`; A (recommended): `dwd:Issuer`, `dwd:GeneratingProcess`, `dwd:ReferencedModel` and `dwd:MetElementDefinition` are not required and not exported (required by the schema but not used; `dwd:MetElementDefinition` is absent in the published files), their texts bounded as SC-22 (256 characters) / B: require the schema's 1..1 elements (rejected: the published files lack `dwd:MetElementDefinition`). Placemarks: MOSMIX_L exactly one; MOSMIX_S 1 to 20000 (SC-22); trimmed `kml:name` per MF-03 grammar for MOSMIX_S (SC-22) and equal to the requested ID for MOSMIX_L; at most 200 `dwd:Forecast` per placemark with `dwd:elementName` matching `^[A-Za-z0-9_]{1,16}$` and unique; each `dwd:value` with exactly N tokens of the SC-20 grammar (`-` or `^-?\d{1,9}(\.\d{1,6})?$`). Retained bounds for the selected placemark: at most 1000 steps × 200 elements = 200 000 tokens; MOSMIX_S peak-memory threshold for one parse: absolute process peak working set ≤ 256 MiB on the NanoPi R76S (probe: 107 MB; P7.11 verifies). IDs: a violation → `MOSMIX-FORMAT-INVALID`; a missing issue time → `MOSMIX-ISSUE-TIME-MISSING`; a MOSMIX_L placemark with another ID → `MOSMIX-LOCATION-MISMATCH`; a MOSMIX_S file without the requested placemark → `MOSMIX-ID-UNKNOWN`; HTTP 404 for the MOSMIX_L file → `MOSMIX-ID-UNKNOWN`. Encoding as declared (ISO-8859-1 observed) | T-P7-04, T-P7-05, T-P7-06 |
| MF-08 | Location | A (recommended): `ForecastLocation` from the placemark: `name` = `kml:description` kept exactly (required, non-empty, at most 256 characters), longitude, latitude and altitude from `kml:coordinates` (`lon,lat,alt`, three decimal numbers with a `.` separator, culture-independent), validated with the C-51/C-70 ranges; missing or invalid → `MOSMIX-FORMAT-INVALID`; no catalog request. B: from the P4 catalog (rejected: an extra request and a second source; the catalog uses degrees and minutes) | T-P7-07 |
| MF-09 | Freshness | `MaxRunAge` per product in its options, required without default, permitted (0, 240 h]; age = `retrieved_at − issued_at`; age ≤ limit → `Fresh`, above → `Stale`, the run is still returned with one `MOSMIX-RUN-STALE` warning naming the age and the limit. Guidance in README (not a default): MOSMIX_S 2 h, MOSMIX_L 8 h (observed publication about 40 minutes and 1 h 16 minutes after issue; the next run 1 h and 6 h later). Wire marker: A (recommended): an additive optional member `run_age_exceeded` (`bool?`, C-05 presence) of `forecast_run`, appended after `forecasts`; the P7 parsers always set it (`true` or `false`); omitted when unset, so existing v1 documents stay valid; new C decision in P7.03. B: no wire marker (rejected: REQ-104) | T-P7-13 |
| MF-10 | Element mapping v1 fields | `TTT` → `temperature_c` (K − 273.15); `PPPP` → `pressure_hpa` and `pressure_mmhg` from the same Pa value ÷ 100; pressure reference: `unknown` (the only option supported by the evidence: the definition says "reduced" and the procedure description's product list "reduced air pressure", but no retrieved source names the reference level, and REQ-054 forbids guessing); rejected alternative: `sea_level`, admissible only after a new decision citing an authoritative DWD source that states the reduction to mean sea level; `DD` → `wind_direction_deg`; `FF` → `wind_speed_kmh`; `Rad1h` → `solar_irradiance_w_m2` as the mean over `[valid_at − 1 h, valid_at]` (kJ/m² × 1000 as `RadiationEnergy`; the period from the procedure description annex "in der letzten Stunde"); `RR1c` → `rain_rate_mm_h` as the mean over `[valid_at − 1 h, valid_at]` (`PrecipitationAccumulation`). `humidity_percent` and `illuminance_lux`: no source, omitted, never zero-filled; humidity from `TTT` and `Td`: A (recommended) not in P7 / B Magnus formula (needs a cited reference and a new decision) | T-P7-08, T-P7-09 |
| MF-11 | Forecast-only quantities | A (recommended): two additive optional `ForecastValues` members appended after `rain_rate_mm_h`: `wind_gust_kmh` (`IntervalValue`, aggregation `maximum`, from `FX1`, m/s → km/h, rounded like `wind_speed_kmh` to 1 km/h, period `[valid_at − 1 h, valid_at]`) and `precipitation_amount_mm` (`IntervalValue`, aggregation `sum`, from `RR1c`, kg/m² = mm, unrounded as `rain_rate_mm_h`, period `[valid_at − 1 h, valid_at]`); additive `IntervalAggregation` members `maximum` and `sum`; the C-79 graph checks extended (interval order, non-negative values, 1 km/h scale for gusts, aggregation `maximum` for `wind_gust_kmh`, `sum` for `precipitation_amount_mm` and `mean` for the existing interval members); schema regenerated. Builder amendments of C-76, C-77 and C-78, recorded as new C decisions in P7.03: (1) `WindGust` becomes an interval kind: its series requires `Periods` (a `null` list → `ArgumentException`, as the other interval kinds) and a defined `WindSpeedUnit` (missing or undefined → `ArgumentException`, as `MeanWindSpeed`); its target field is `wind_gust_kmh` (so two gust series fail the duplicate-field rule); a `null` period at an index → `MissingPeriod` at that index; the value is validated with the existing `WindGust` validation, converted to km/h when the unit is m/s and rounded as `wind_speed_kmh`; (2) a `PrecipitationAccumulation` series fills both `rain_rate_mm_h` (the existing mean, unchanged) and `precipitation_amount_mm` (the unconverted total over the same period, aggregation `sum`); its duplicate-field rule covers both fields, so a `PrecipitationRate` series together with a `PrecipitationAccumulation` series still fails; a `PrecipitationRate` series fills only `rain_rate_mm_h`; the two outputs of one `PrecipitationAccumulation` value are handled independently: validation (`Missing`, `NonFinite`, `OutOfRange`) and the period check (`MissingPeriod`) apply to the value and omit both outputs with one entry, whereas the rate conversion through `IPeriodRateConverter` can fail alone (for example a total of `1e27` mm over a one-second period, valid under C-42 and C-44, whose rate overflows `decimal`): then `rain_rate_mm_h` is omitted with one `Unrepresentable` entry (kind `PrecipitationAccumulation`) and `precipitation_amount_mm` keeps the validated total, which is always representable because it is the validated `decimal` itself; the step and the run count the kept total (amendment (4)); rejected alternative: atomic omission of both outputs (a valid total would be lost); (3) omission precedence per value stays C-78: `Missing`, `NonFinite`, `OutOfRange` from validation first, then `MissingPeriod`, then `Unrepresentable` from conversion; one omission entry per series and index (for a `PrecipitationAccumulation` value either the shared validation or period entry, or the rate's `Unrepresentable` entry); (4) the step-has-a-value rule (C-79 (1)) and the builder's kept-step rule count the two new members. Existing builder tests whose expected graphs contain a `PrecipitationAccumulation` series gain the additive member under amendment (2); their other expectations stay unchanged. P7.03 adds a builder-to-serializer regression test for the partial failure: one step whose only value is a `PrecipitationAccumulation` total with an overflowing rate succeeds with `precipitation_amount_mm` and without `rain_rate_mm_h`, the omission list holds exactly the one `Unrepresentable` entry, and both real serializers write the step. B: none in P7; `FX1` is then listed in `NotExportedElements` and `RR1c` exported only as the rate of MF-10. C: a larger set. Probabilities, longer totals, cloud cover, visibility, sunshine and weather codes: not exported in every option; they appear in `NotExportedElements` (MF-02), never as `OmittedForecastValue` | T-P7-10 |
| MF-12 | Missing values and run rejection | `-` is missing at its own index (C-77); a token outside the SC-20 grammar → `MOSMIX-FORMAT-INVALID`; a numeric value that fails quantity validation is omitted with the C-78 reason; a series with a token count other than N → `MOSMIX-FORMAT-INVALID`; a run without any exported value → `MOSMIX-NO-USABLE-VALUE` (carrying the omissions, MF-02); required elements: A (recommended): none; every absent mapped element gives one `MOSMIX-ELEMENT-ABSENT` notice / B: a run without `TTT` fails | T-P7-11 |
| MF-13 | Time and period semantics | `valid_at` = each `dwd:TimeStep`; the one-hour elements (`Rad1h`, `RR1c`, `FX1`) get `[valid_at − 1 h, valid_at]`; the first step's period may start before `issued_at` (recorded, not corrected); the horizon comes from the file timeline (section 5); no metadata from another file; `retrieved_at` from the injected `TimeProvider` after the response body has been read completely, truncated to whole seconds (OW-16 pattern) | T-P7-12 |
| MF-14 | Error and notice IDs | `MosmixForecastException` with `ErrorId`, `Product` (`MosmixProduct`), `OmittedValues` and `NotExportedElements` (both empty except for `MOSMIX-NO-USABLE-VALUE`); error IDs `MOSMIX-ID-INVALID`, `MOSMIX-ID-UNKNOWN`, `MOSMIX-RETRIEVAL-FAILED`, `MOSMIX-DEADLINE-EXCEEDED`, `MOSMIX-TOO-LARGE`, `MOSMIX-FORMAT-INVALID`, `MOSMIX-ISSUE-TIME-MISSING`, `MOSMIX-LOCATION-MISMATCH`, `MOSMIX-NO-USABLE-VALUE`, `MOSMIX-OPTIONS-INVALID`; notice IDs `MOSMIX-RUN-STALE`, `MOSMIX-ELEMENT-ABSENT`, `MOSMIX-ELEMENT-NOT-EXPORTED`, `FORECAST-VALUE-OMITTED`; messages and log scopes name the product (`MOSMIX_S` or `MOSMIX_L`); precedence at every checkpoint: caller cancellation (`OperationCanceledException` with the caller's token), then the parse deadline, then every other failure; transport failure, non-success status (except the MOSMIX_L 404) and the request bound → `MOSMIX-RETRIEVAL-FAILED`; size → `MOSMIX-TOO-LARGE` | T-P7-03, T-P7-17 |
| MF-15 | Diagnostics and logging | Stable IDs, UTC, product, MOSMIX ID and the sanitized endpoint (scheme, host, path; no user information, no query); omissions with valid time, kind and reason; unmapped elements by name; no series values, file text or archive member names in logs or messages (REQ-072, SC-09) | T-P7-17 |
| MF-16 | Source conditions and README | Section 6: README attribution "Deutscher Wetterdienst" with CC BY 4.0 and the source paths; retrieval guidance (external scheduling only): MOSMIX_S at most once per hourly run, after about 45 minutes past the hour, about 36 MB per download, each parse downloads the whole file (one location per download); MOSMIX_L at most once per main run (03, 09, 15, 21 UTC, available about 75 to 80 minutes later) and location; the `MaxRunAge` guidance of MF-09; the horizon difference of section 5 | T-P7-19 |
| MF-17 | Live verification | Optional, explicitly enabled with `WEATHERPARSER_LIVE_CHECK_MODE=mosmix-l` or `mosmix-s`, one request per product and run, value-free escaped summary, recorded separately, files not committed; only with the user's authorization in P7.11 | T-P7-22 |
| MF-18 | Selection element vocabulary | Section 4: the P4 availability vocabulary and every mapped element are defined in the element definitions and present in the current files; no mismatch, so no P4 amendment proposal; the P7.05 test pins the mapped names | T-P7-23 |

## 9. Review rounds

Codex five-angle review (read-only) of this draft; every finding verified by the orchestrating agent. The outputs are kept outside the repository (`p702/codex_r<n>.md` in the session scratchpad) and reproduced verbatim in section 9.1.

| Round | Findings | Disposition |
| --- | --- | --- |
| 1 | 5: [P2] the T-P7-23 test covered only mapped names, not the whole P4 vocabulary; [P2] unmapped elements without a `QuantityKind` cannot be `OmittedForecastValue` entries; [P2] the gust proposal conflicted with C-76 (no periods for `WindGust`, unit only for `MeanWindSpeed`); [P2] MF-10 offered `sea_level` without evidence (REQ-054); [P3] probe commands and sources not identified | All confirmed against the plan, the builder code and REQ-054 and fixed: complete vocabulary check (all 154 names defined, verified by script); `NotExportedElements` and `MOSMIX-ELEMENT-NOT-EXPORTED`; builder amendments (1) to (4) in MF-11; `unknown` with `sea_level` rejected pending a source; commands and probe hashes in section 7 |
| 2 | 2: [P3] section 5 said the MOSMIX_S directory holds 12 runs (the listing holds 48, 36192774 to 37287136 bytes); [P3] the review outputs were summarized, not recorded | Both confirmed (listing recounted with a script) and fixed: section 5 corrected and marked as a snapshot; verbatim outputs in section 9.1 |
| 3 | 0 | – |
| PR #11 review of `3582936` (2026-10-26, the user; inline comment on MF-11) | 1: [P2] MF-11 did not define the result when one `PrecipitationAccumulation` value yields a representable total and an unrepresentable rate, so the outcome could depend on assignment order | Confirmed by the orchestrating agent against C-42, C-44 and `ForecastRunBuilder` (an `IPeriodRateConverter` overflow becomes `Unrepresentable`): independent handling adopted as recommended (total kept, rate omitted with `Unrepresentable`, atomic omission rejected), with the P7.03 regression test |
| 4 | 0 (review of the MF-11 fix and the whole draft) | – |

### 9.1 Review outputs (verbatim)

<details><summary>Round 1</summary>

### Angle 1 – Requirements, contract conformance and traceability

- **[P2 should fix] Location: `docs/p7/P7_FORECAST_CONTRACT.md`, section 4 and MF-18.** The proposed T-P7-23 test checks only mapped names. The phase matrix requires checking **every name reported by P4 availability evidence**, alongside mapped names, against the recorded element definitions. A regression involving an unmapped availability name could therefore pass. Specify the complete P4 vocabulary as test input and retain the mapped-name check as an additional assertion.

### Angle 2 – Architecture and API design

- **[P2 should fix] Location: MF-02, MF-11, MF-15.** The promised C-78 omission list cannot represent every unmapped source element with the existing types. `OmittedForecastValue.Kind` requires a defined `QuantityKind`, but cloud cover, visibility, sunshine and weather codes have no corresponding members. For example, an omitted `N` series cannot produce the promised diagnostic without inventing or mislabelling its kind. Define an additive, source-aware diagnostic representation, including source element identity, and specify how parser omissions combine with builder omissions on success and rejection.

- **[P2 should fix] Location: MF-11.** The gust proposal specifies output semantics but leaves incompatible builder input rules unresolved. C-76 and the existing builder prohibit `Periods` on `WindGust`; C-77 treats it as `NotExportable`. Wind-speed unit validation and conversion currently apply only to `MeanWindSpeed`. Consequently, a gust series carrying its required period would fail before mapping. Explicitly record the C-76/C-77 amendments: required unit handling, period-list validation, per-index missing-period behavior and omission precedence. These rules are needed for deterministic Red tests.

### Angle 3 – Correctness and source fidelity

- **[P2 should fix] Location: MF-10, pressure-reference alternatives.** Option A offers `sea_level` even though the row explicitly establishes that the retrieved evidence does not identify the reduction level. Selecting it would contradict REQ-054’s requirement to use `unknown` when unverified. Mark A as rejected pending an authoritative source establishing the reference level; retain `unknown` as the supported choice under the present evidence.

### Angle 4 – Security, legal and operations

No findings.

### Angle 5 – Process, governance and evidence

- **[P3 minor] Location: sections 7.1–7.2.** The probe summaries omit the execution commands required by the P7.02 plan’s evidence section. They also lack a reproducible identification of the probe source. Another reviewer cannot reconstruct which build and invocation produced the timing and memory results. Record the build/run commands, relevant settings and probe source checksum or retained evidence reference, keeping probe code outside the repository as planned.

The draft needs these clarifications before approval. The locally available source-register hashes matched, and the supplied structural analyses support the recorded file facts; no files were changed.

Summary: 5 findings

</details>

<details><summary>Round 2</summary>

### Angle 1 – Requirements, contract conformance and traceability

No findings.

### Angle 2 – Architecture and API design

No findings.

### Angle 3 – Correctness and source fidelity

- **[P3 minor] Location: `docs/p7/P7_FORECAST_CONTRACT.md`, section 5, paragraph after the file-facts table.** The claim that the MOSMIX_S directory contains “the last 12 hourly runs (36.2 to 36.6 MB each)” contradicts the saved directory listing. It contains **48 timestamped runs**, from `2026102420` through `2026102619`, ranging from **36,192,774 to 37,287,136 bytes**, plus `LATEST`. A consumer relying on this source record would infer the wrong historical availability and size range. Correct the count and range, explicitly describing this as the observed snapshot rather than a guaranteed retention policy.

### Angle 4 – Security, legal and operations

No findings.

### Angle 5 – Process, governance and evidence

- **[P3 minor] Location: `docs/p7/P7_FORECAST_CONTRACT.md`, section 9.** Round 1 is recorded only as a finding summary and disposition. P7.02 S4 and AGENTS.md require every round’s output and finding count to be recorded in the contract. The original report exists in the supplied local evidence as `p702/codex_r1.md`, but its full text is absent from the record. Once the temporary evidence disappears, reviewers cannot recover the original findings and verdict. Include the verbatim round 1 report alongside its dispositions, and preserve subsequent reports likewise.

The five round 1 findings are addressed; the source and probe checksums match the local evidence. Two minor documentation corrections remain before closing the review loop; no files were changed.

Summary: 2 findings

</details>

<details><summary>Round 3</summary>

### Angle 1 – Requirements, contract conformance and traceability

No findings.

### Angle 2 – Architecture and API design

No findings.

### Angle 3 – Correctness and source fidelity

No findings.

### Angle 4 – Security, legal and operations

No findings.

### Angle 5 – Process, governance and evidence

No findings.

The round 1 and round 2 fixes are verified. The full draft is suitable for user approval: the local evidence supports the source facts, the proposed amendments are explicit, and the decisions provide testable behavior. Source and probe checksums match; earlier review outputs are reproduced verbatim. No network access or file changes were made.

Summary: 0 findings

</details>

<details><summary>Round 4</summary>

### Angle 1 – Requirements, contract conformance and traceability

No findings.

### Angle 2 – Architecture and API design

No findings.

### Angle 3 – Correctness and source fidelity

No findings.

### Angle 4 – Security, legal and operations

No findings.

### Angle 5 – Process, governance and evidence

No findings.

The full draft is suitable for user approval. MF-11 now defines the partial failure deterministically and specifies a regression through both real serializers; the local evidence and code support the proposed behavior. No network access or file changes were made.

Summary: 0 findings

</details>
