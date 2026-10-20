# WeatherParser Export Contract (v1)

**Status (2026-10-20): P2.02 decisions C-01 to C-15 approved by the user (section 1); v1 stays provisional until P2 acceptance (C-03). Implementation of the slice follows in P2.02 S1–S7.**

This record holds the public export contract decisions of phase P2 ([phase plan](../../PHASE_P2_PLAN.md), [P2.02 plan](P2_02_CONTRACT_AND_FIRST_SLICE_PLAN.md)); later steps add their decisions here. Drafted by qwen3-coder:30b from the orchestrating agent's decision brief and corrected by qwen once; the orchestrating agent removed output markers, restored dropped section 6 bullets and corrected the XML example text. User review (2026-10-20): qwen redefined C-05 (nullable scalars as the single presence source in both formats) and C-03/C-13 (v1 provisional until P2 acceptance); the orchestrating agent removed output markers again.

## 1. Approval

| Date | Scope | Decision | Wording |
|------|-------|----------|---------|
| 2026-10-20 | P2.02 decisions C-01 to C-15 in sections 2–5 | Approved | User selected "Approve as proposed (Recommended)" for the question "Approve the revised P2.02 export contract C-01 to C-15 in docs/p2/P2_EXPORT_CONTRACT.md (C-05: nullable scalars as the single presence source in both formats; C-03/C-13: v1 provisional until P2 acceptance, then frozen; C-14 value-object quantities; C-15 readable umlauts in JSON)?" |
| 2026-10-20 | P2.03 decisions C-16 to C-27 in section 7 (after one review round: C-27 exempts the constant `schema_version`; explicit zeros stay valid) | Approved | User selected "Approve as proposed (Recommended)" for the question "Approve the revised P2.03 list-contract decisions C-16 to C-27 (C-27 now exempts the constant schema_version and keeps explicit zeros valid; the P2.03 plan gained slice S6 with C-27 regression tests in both formats)?" |
| 2026-10-20 | P2.04 decisions C-28 to C-35 in section 8 (after one review round: DST precedence over the midnight rule, nullable inputs; midnight tolerance 5 minutes chosen by the user) | Approved | User selected "Approve as proposed (Recommended)" for the question "Approve the revised P2.04 time decisions C-28 to C-35 in docs/p2/P2_EXPORT_CONTRACT.md section 8 (nullable inputs with null = missing and 00:00 valid; 5-minute midnight tolerance; DST check before and after the midnight rule; input guards; defined failure for missing time-zone data)?"; tolerance: selected "5 minutes (Recommended)". |
| 2026-10-20 | P2.05 decisions C-36 to C-47 in section 9 (after one review round: pressure bounds per original unit including the factor discrepancy; whole-second averaging-period endpoints; wind-direction range chosen by the user) | Approved | User selected "Approve as revised (Recommended)" for the question "Approve the revised P2.05 decisions C-36 to C-47 (C-41 now gives pressure bounds per original unit including the factor discrepancy 1.00000018491463 and the 100000000 mmHg example; C-44 rejects sub-second period endpoints, shortest period one second; plan S2/S4 Red tests updated)?"; wind direction: selected "0–360, 360 → 0 (Recommended)". |
| 2026-10-20 | P2.06 decisions C-48 to C-65 in section 10 (after six read-only reviews by the OpenAI Codex CLI with 8, 6, 5, 3, 3, 2 and finally 0 findings, each finding confirmed by the orchestrating agent and fixed by qwen; C-50, C-51 and C-55 chosen by the user) | Approved | User selected "Approve (Recommended)" for the question "Approve the P2.06 decisions C-48 to C-65 in docs/p2/P2_EXPORT_CONTRACT.md section 10 (after six Codex review rounds, the last with 0 findings; C-50 provider:station_id, C-51 latitude/longitude required, C-55 latest reading; C-63 and C-65 amend the P2.05 services)?"; earlier choices: C-50 "provider:station_id (Recommended)", C-55 "Latest reading (Recommended)", C-51 "Lat/lon required". |

## 2. Decision register (P2.02)

| ID | Decision | Value | Decided by | Status |
|----|----------|-------|------------|--------|
| C-01 | Roots | Five roots: envelope `export_document` (implemented in P2.08), `cdc_station_list` and `mosmix_location_list` (P2.03), `observation_dataset` (this slice in P2.02, complete in P2.06), `forecast_run` (P2.07). The standalone `observation_dataset` and `forecast_run` roots are also the MQTT JSON payloads of P8 | User | Approved 2026-10-20 |
| C-02 | Version and namespace | JSON field `schema_version` with the string value `"1"`; XML element `schema_version` with value `1`; XML namespace `urn:weatherparser:export:v1` as the default namespace of every root, all elements namespace-qualified | User | Approved 2026-10-20 |
| C-03 | Evolution | v1 is provisional until P2 acceptance (P2.12). Until then, any change — including new required members such as those P2.06 adds to the slice — is allowed but must be recorded here as a decision with its date and approval. At P2 acceptance v1 freezes: afterwards only additive optional members are allowed; consumers must ignore unknown JSON members; XML compatibility of each additive change is checked against the generated schema (P2.09); any removal, rename, new required member, type or meaning change requires `schema_version` `"2"` and namespace `urn:weatherparser:export:v2` | User | Approved 2026-10-20 |
| C-04 | Missing versus empty | A missing or invalid value is omitted: no JSON `null`, no XML `xsi:nil`, no empty element, never a substituted zero. A real zero is written. Empty collections are written as empty JSON arrays or empty XML container elements, never omitted | User | Approved 2026-10-20 |
| C-05 | Omission mechanism | Optional value-type scalars are declared nullable (for example `decimal?`); `HasValue` is the only source of presence. The property carries `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` and its `[XmlElement]` name. A companion `public bool {Name}Specified` marked `[XmlIgnore]` and `[JsonIgnore]` returns `HasValue` (its setter, required by `XmlSerializer` for reading, only clears the value when set to `false`), so `XmlSerializer` omits the element exactly when JSON omits the property. Optional objects are `null` when missing and use the same JSON condition. Tests cover missing, present zero and present non-zero values in both formats and a read-back of each | Orchestrator, recorded | Approved 2026-10-20 |
| C-06 | Time | Wire form `YYYY-MM-DDTHH:mm:ss` (format string `yyyy-MM-dd'T'HH:mm:ss`, `CultureInfo.InvariantCulture`), no `Z`, no offset, no fractional seconds. Each timestamp is a public `string` wire property mapped to the field name; the UTC value is held in a companion `DateTime` property marked `[JsonIgnore]` and `[XmlIgnore]`. A non-UTC `DateTime` kind or a non-zero sub-second part is rejected with an `ArgumentException`; local system time is never converted implicitly | User | Approved 2026-10-20 |
| C-07 | Numbers | Exported quantity values are `decimal`; numbers are written culture-invariantly without unit suffixes; the decimal scale is written as held (P2.05 fixes the output scale per quantity) | User | Approved 2026-10-20 |
| C-08 | Names | Field and element names are snake_case as in REQUIREMENTS.md, set explicitly per property with `[JsonPropertyName]` and `[XmlElement]`; no naming policy | Orchestrator, recorded | Approved 2026-10-20 |
| C-09 | Serializer services | `IJsonExportSerializer` and `IXmlExportSerializer`, each with `Task SerializeAsync<TRoot>(TRoot root, Stream destination, CancellationToken cancellationToken)`; implementations `JsonExportSerializer` and `XmlExportSerializer` registered in `AddWeatherParsing`; `XmlSerializer` instances cached per serializer instance (no static cache) | Orchestrator, recorded | Approved 2026-10-20 |
| C-10 | Stream, cancellation, failures | The caller owns the destination stream; it is never disposed or closed by the serializer. The document is produced into a buffer first and then copied with `CopyToAsync(destination, cancellationToken)`; cancellation is observed before serialization and during the copy, not inside `XmlSerializer` (which is synchronous). Serialization failures are raised as `ExportSerializationException`; write failures of the destination propagate. No success is reported after an exception or cancellation; atomic writes to an arbitrary stream are not promised | Orchestrator, recorded | Approved 2026-10-20 |
| C-11 | Encoding | UTF-8 without BOM for both formats | User | Approved 2026-10-20 |
| C-12 | Invalid XML characters | Strings containing characters that are invalid in XML 1.0 (for example U+0001) make XML serialization fail with `ExportSerializationException`; JSON writes them escaped; the failure is defined, never a partial document reported as success | User | Approved 2026-10-20 |
| C-13 | First slice | `observation_dataset` with `schema_version`, `source`, `retrieved_at` and `measurements` containing only `temperature_c`. The slice is provisional: P2.06 completes the observation graph (including required `location`, dataset `measured_at` and `timestamp_quality`) under the pre-freeze rule of C-03 | User | Approved 2026-10-20 |
| C-14 | Quantity value shape | Each quantity is a value object: `{ "value": <number>, "measured_at": "<UTC>" }`; a missing quantity omits the whole object. P2.06 adds optional quality, timezone and period members | User | Approved 2026-10-20 |
| C-15 | JSON character escaping | `JavaScriptEncoder.Create(UnicodeRanges.All)`: umlauts and other non-ASCII letters are written unescaped (for example `Ä`); HTML-sensitive characters stay escaped (`<` as `\u003C`, `&` as `\u0026`, `>` as `\u003E`). The default encoder would also escape `Ä` as `\u00C4`; both forms are valid JSON with equal meaning, but the chosen form keeps umlauts readable (REQ-067) | User | Approved 2026-10-20 |
## 3. Roots and XML root elements

| Root | JSON | XML root element | Implemented in |
|------|------|------------------|----------------|
| Envelope | object | `export_document` | P2.08 |
| CDC station list | object | `cdc_station_list` | P2.03 |
| MOSMIX location list | object | `mosmix_location_list` | P2.03 |
| Observation dataset | object | `observation_dataset` | P2.02 (slice), P2.06 |
| Forecast run | object | `forecast_run` | P2.07 |

Each document has exactly one root; documents are never concatenated (REQ-068).

## 4. Field mapping of the P2.02 slice (`observation_dataset`)

Provisional slice (C-03, C-13): P2.06 adds required members before v1 freezes at P2 acceptance.

| JSON path | XML path | C# member (Proposed) | Type | Required | Notes |
|-----------|----------|----------------------|------|----------|-------|
| `schema_version` | `/observation_dataset/schema_version` | `ObservationDataset.SchemaVersion` | string `"1"` | yes | constant |
| `source` | `/observation_dataset/source` | `ObservationDataset.Source` | string | yes | provider/station identifier; format decided in P2.06 |
| `retrieved_at` | `/observation_dataset/retrieved_at` | `ObservationDataset.RetrievedAtWire` (wire) with `ObservationDataset.RetrievedAtUtc` (`DateTime`, ignored by both serializers) | string, C-06 form | yes | |
| `measurements` | `/observation_dataset/measurements` | `ObservationDataset.Measurements` (`ObservationMeasurements`) | object | yes | may be empty in the slice; P2.06 rejects datasets without usable readings |
| `measurements.temperature_c` | `/observation_dataset/measurements/temperature_c` | `ObservationMeasurements.TemperatureC` (`MeasurementValue`) | object | no | omitted when missing (C-04) |
| `measurements.temperature_c.value` | `.../temperature_c/value` | `MeasurementValue.Value` | decimal, °C | yes within the object | zero is written |
| `measurements.temperature_c.measured_at` | `.../temperature_c/measured_at` | `MeasurementValue.MeasuredAtWire` with `MeasuredAtUtc` | string, C-06 form | yes within the object | per-quantity timestamp (REQ-056) |

### JSON example

JSON escapes the HTML-sensitive characters per C-15; the XML example uses XML entity escapes.

```json
{
  "schema_version": "1",
  "source": "Fulda-Horas (Ä\u003C\u0026\u003E)",
  "retrieved_at": "2026-07-15T12:05:00",
  "measurements": {
    "temperature_c": {
      "value": 0,
      "measured_at": "2026-07-15T12:00:00"
    }
  }
}
```

### XML example

```xml
<?xml version="1.0" encoding="utf-8"?>
<observation_dataset xmlns="urn:weatherparser:export:v1">
  <schema_version>1</schema_version>
  <source>Fulda-Horas (Ä&lt;&amp;&gt;)</source>
  <retrieved_at>2026-07-15T12:05:00</retrieved_at>
  <measurements>
    <temperature_c>
      <value>0</value>
      <measured_at>2026-07-15T12:00:00</measured_at>
    </temperature_c>
  </measurements>
</observation_dataset>
```

## 5. Deviation from the P2.02 plan

The P2.02 plan's slice S1 assumed `temperature_c` to be a nullable number, with the expected XML Red failure `xsi:nil="true"`. Under C-14 the quantity is a value object; `XmlSerializer` omits a `null` object without `xsi:nil`, so the XML half of S1 is expected to pass immediately and is recorded as characterization, while the JSON half keeps its Red (`"temperature_c": null` without `WhenWritingNull`). The `{Name}Specified` mechanism of C-05 is first needed for optional value-type scalars in later steps (for example optional `altitude_m`) and is tested there. If the user prefers a plain nullable number for the slice, C-14 changes and the plan's S1 applies unchanged. The C-05 mechanism is tested first where an optional scalar appears (missing, present zero, present non-zero, both formats).

## 6. Not decided here

- observation `source` format, dataset `measured_at` rule, quality and period members (P2.06; decided in section 10)
- list field names, availability shape (P2.03)
- midnight tolerance (P2.04)
- wind-direction range and m/s conversion (P2.05)
- forecast shapes (P2.07)
- envelope error record and partial marker (P2.08)
- schema generator and delivery (P2.09)

## 7. P2.03 decisions (CDC and MOSMIX lists)

Approved by the user on 2026-10-20 (section 1); the approval was recorded in section 1 before the first P2.03 Red test; v1 stays provisional (C-03).

| ID   | Decision                                                                 | Value                                                                                                                                                                                                                                                                           | Decided by   | Status   |
|------|--------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------|----------|
| C-16 | Type names                                                               | `CdcStationList`, `CdcStationListEntry`, `MosmixLocationList`, `MosmixLocationListEntry`, `MosmixProductAvailability`, `SearchParameters`                                                                                                                                      | User         | Approved 2026-10-20 |
| C-17 | Roots and collections                                                    | Roots `cdc_station_list` and `mosmix_location_list` (C-01) in namespace `urn:weatherparser:export:v1`. CDC entries in the collection `stations` (XML container `stations`, item element `station`); MOSMIX entries in `locations` (XML `locations` / `location`). Empty collections are written (C-04) | User         | Approved 2026-10-20 |
| C-18 | List metadata | Each list root has `schema_version` (the constant `"1"` as in C-02; it needs no caller assignment and is exempt from C-27), `source` (string identifying the catalog; literal values are verified in P4), `retrieved_at` (C-06 form) and `search_parameters`. | User | Approved 2026-10-20 |
| C-19 | Search parameters                                                        | `search_parameters` object with `latitude`, `longitude` (decimal degrees), `altitude_m`, `max_distance_km`, `max_altitude_difference_m`; all required                                                                                                                                 | User         | Approved 2026-10-20 |
| C-20 | CDC entry fields                                                         | `station_id` (string, leading zeros kept), `name`, `latitude`, `longitude`, `altitude_m`, `distance_km`, `altitude_difference_m` (signed: station minus reference altitude), `available_measurements` (collection of strings; XML item element `measurement`) — all required (REQ-023) | User         | Approved 2026-10-20 |
| C-21 | `available_measurements` vocabulary                                      | REQ-052 field names (for example `temperature_c`); the mapping from DWD products is P4/P5 work                                                                                                                                                                                 | User         | Approved 2026-10-20 |
| C-22 | MOSMIX entry fields                                                      | `mosmix_id` (string; alphanumeric and leading zeros kept; never a CDC ID), `name`, `latitude`, `longitude`, `altitude_m`, `distance_km`, `altitude_difference_m`, and `products` — all required (REQ-101). `available_forecast_elements` is carried per product (C-23) | User         | Approved 2026-10-20 |
| C-23 | Product availability                                                     | `products` collection (XML item element `product_availability`), one item per product with `product` (`MOSMIX_S` or `MOSMIX_L`), `availability` (`available`, `unavailable` or `unknown`) and `available_forecast_elements` (XML item element `element`). The element list contains only verified elements; with `unavailable` or `unknown` it is empty, and consumers must read `availability` to distinguish "none" from "not known" | User         | Approved 2026-10-20 |
| C-24 | MOSMIX element vocabulary                                                | DWD element names as published (for example `TTT`), verified in P4/P7; mapping to REQ-052 names happens in forecast normalization                                                                                                                                              | User         | Approved 2026-10-20 |
| C-25 | Ordering                                                                 | `stations` and `locations` sorted by `distance_km`, then by ID with ordinal comparison (`StringComparer.Ordinal`), through an injectable ordering service `IListEntryOrdering`                                                                                               | Orchestrator, recorded | Approved 2026-10-20 |
| C-26 | Numbers                                                                  | Coordinates, altitudes and distances are `decimal` (C-07); no new rounding rule                                                                                                                                                                                                | Orchestrator, recorded | Approved 2026-10-20 |
| C-27 | Required members | Every required scalar and string of the list graphs tracks whether it was assigned (as `Value` and the timestamps in P2.02); serializing an unassigned required member fails with `ExportSerializationException` and writes nothing, so no fabricated `0`, empty or default value is exported; an explicitly assigned `0` is valid. Exempt: `schema_version`, a constant (C-02, C-18) that needs no caller assignment. | User | Approved 2026-10-20 |
```json
{
  "schema_version": "1",
  "source": "dwd-cdc",
  "retrieved_at": "2026-07-15T12:05:00",
  "search_parameters": {
    "latitude": 50.5549,
    "longitude": 9.6738,
    "altitude_m": 250,
    "max_distance_km": 30,
    "max_altitude_difference_m": 200
  },
  "stations": [
    {
      "station_id": "00044",
      "name": "Fulda-Horas",
      "latitude": 50.567,
      "longitude": 9.653,
      "altitude_m": 242,
      "distance_km": 2.1,
      "altitude_difference_m": -8,
      "available_measurements": [
        "temperature_c",
        "humidity_percent"
      ]
    }
  ]
}
```

```json
{
  "schema_version": "1",
  "source": "dwd-mosmix",
  "retrieved_at": "2026-07-15T12:05:00",
  "search_parameters": {
    "latitude": 50.5549,
    "longitude": 9.6738,
    "altitude_m": 250,
    "max_distance_km": 30,
    "max_altitude_difference_m": 200
  },
  "locations": [
    {
      "mosmix_id": "X123A",
      "name": "Example Location",
      "latitude": 50.567,
      "longitude": 9.653,
      "altitude_m": 242,
      "distance_km": 2.1,
      "altitude_difference_m": -8,
      "products": [
        {
          "product": "MOSMIX_L",
          "availability": "available",
          "available_forecast_elements": [
            "TTT",
            "PPPP"
          ]
        },
        {
          "product": "MOSMIX_S",
          "availability": "unknown",
          "available_forecast_elements": []
        }
      ]
    }
  ]
}
```

The values in the examples above are synthetic and not verified catalog data.

Implementation note (2026-10-20, recorded by the orchestrating agent): the enums carrying the C-23 wire literals are named `MosmixProduct` (`MOSMIX_S`, `MOSMIX_L`) and `AvailabilityState` (`available`, `unavailable`, `unknown`); required members are tracked by the internal value type `RequiredMember<T>` (C-27).

## 8. P2.04 decisions (source-time normalization)

Proposed on 2026-10-20, revised after the user's review (DST precedence over the midnight rule; nullable inputs for missing times; tolerance 5 minutes); approved by the user on 2026-10-20 (section 1) before the first P2.04 Red test. The service prepares times for the observation graph (P2.06) and the source adapters (P5–P7); it does not parse any source format.

| ID   | Decision                                                                                                                                   | Value                                                                                                                                                                                                                                                                                                                                                                               | Decided by     | Status   |
|------|--------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------|----------|
| C-28 | Names                                                                                                                                      | Folder `Time/`, namespace `WeatherParser.Parsing.Time`: `ISourceTimeNormalizer`/`SourceTimeNormalizer`, `ITimeZoneResolver`/`TimeZoneResolver`, result `NormalizedSourceTime`, failure `SourceTimeNormalizationException`, quality enum `TimestampQuality`                                                                                                                                   | User           | Approved 2026-10-20 |
| C-29 | Input model | Three methods accept nullable inputs: `NormalizeComplete(DateTimeOffset? sourceTime)`, `NormalizeCompleteLocal(DateTime? localTime)`, `InferDate(TimeOnly? displayedTime, DateTime retrievalUtc)`. A `null` input means missing and is rejected with `SourceTimeNormalizationException`. The `NormalizeCompleteLocal` method accepts only `DateTimeKind.Unspecified` inputs; a complete timestamp never enters the inference path. | User | Approved 2026-10-20 |
| C-30 | Midnight rule | The date is determined as the Europe/Berlin local date of the retrieval instant. If the displayed time is later than the local retrieval time by more than 5 minutes (user decision 2026-10-20), the reading belongs to the previous local date; otherwise it stays on the retrieval date. `00:00` is valid midnight. The DST precedence of C-31 applies before and after this rule. | User | Approved 2026-10-20 |
| C-31 | DST gap and overlap | Nonexistent or ambiguous local times are rejected with `SourceTimeNormalizationException` unless an explicit offset is provided via `NormalizeComplete`. The service never selects a DST interpretation. For inferred dates, the displayed time is validated on the retrieval's local date before applying the midnight rule and again on the chosen date afterward; a rejection at either stage takes precedence, ensuring that a date rollover cannot mask a DST gap or overlap. | User | Approved 2026-10-20 |
| C-32 | Markers                                                                                                                                    | `timestamp_quality`: `source` for complete source timestamps, `estimated_date` for inferred dates (REQ-042). `source_timezone` `Europe/Berlin` whenever the value was converted from Europe/Berlin local time (complete local or inferred); absent for explicit offsets                                                                                                                                 | User           | Approved 2026-10-20 |
| C-33 | Input guards | A `null` input (missing time), a non-UTC retrieval instant, or a non-zero sub-second part in the input is rejected with `SourceTimeNormalizationException`. The retrieval time is never substituted for a measurement time (REQ-043). | User | Approved 2026-10-20 |
| C-34 | Time-zone data                                                                                                                             | Europe/Berlin is resolved through the injected `ITimeZoneResolver` (system ICU/tzdata); a missing zone raises `SourceTimeNormalizationException` with an actionable English message naming the zone and the missing time-zone data, never a fallback to UTC or local system time                                                                                                                                 | Orchestrator, recorded | Approved 2026-10-20 |
| C-35 | Result                                                                                                                                     | `NormalizedSourceTime` is an immutable value with the UTC instant (`DateTimeKind.Utc`, whole seconds), `TimestampQuality` and the optional source time zone; it serializes through the P2.02 wire form (`YYYY-MM-DDTHH:mm:ss`) when placed into export entities                                                                                                                                 | Orchestrator, recorded | Approved 2026-10-20 |
Worked examples:
- 2026-01-15 12:00 local → `2026-01-15T11:00:00`
- 2026-07-15 12:00 local → `2026-07-15T10:00:00`
- 2026-10-25 02:30 with offset +02:00 → `2026-10-25T00:30:00`, with +01:00 → `2026-10-25T01:30:00`
- 2026-10-25 02:30 local without offset → rejected (ambiguous)
- 2026-03-29 02:30 local → rejected (nonexistent)
- retrieval 2026-07-15 22:05 UTC (2026-07-16 00:05 local) with display 00:02 → `2026-07-15T22:02:00`, `estimated_date`
- same retrieval with display 23:55 → `2026-07-15T21:55:00`, `estimated_date`
- retrieval 2026-10-25 01:15 UTC (02:15 local, after fall-back) with display 02:45 → rejected (ambiguous on the retrieval date, not rolled over)
- retrieval 2026-10-25 23:30 UTC (2026-10-26 00:30 local) with display 02:45 → rejected (previous date 2026-10-25 is ambiguous)
- retrieval 2026-07-15 10:00 UTC (12:00 local) with display 12:05 → `2026-07-15T10:05:00` (within the 5-minute tolerance), with display 12:06 → `2026-07-14T10:06:00` (previous day)

## 9. P2.05 decisions (units, pressure and output rounding)

Proposed on 2026-10-20, revised after the user's review (pressure bounds per original unit including the factor discrepancy; whole-second averaging-period endpoints); approved by the user on 2026-10-20 (section 1) before the first P2.05 Red test. The services are source-independent; mapping source elements and verifying source units belongs to the provider phases (P5–P7), placing values into graphs to P2.06/P2.07.

| ID | Decision | Value | Decided by | Status |
|----|----------|-------|------------|--------|
| C-36 | Names | Folder `Quantities/`, namespace `WeatherParser.Parsing.Quantities`: `IOutputRounding`/`OutputRounding`, `IPressureConverter`/`PressureConverter` with result `PressureValues`, `IQuantityValidator`/`QuantityValidator` with result `ValidatedQuantity` and enum `QuantityRejectionReason`, `IPeriodRateConverter`/`PeriodRateConverter` with result `PeriodQuantity` and value `AveragingPeriod`, `IWindSpeedConverter`/`WindSpeedConverter`, enums `QuantityKind` and `PressureReference`, failure `QuantityConversionException` | User | Approved 2026-10-20 |
| C-37 | Quantity kinds | `QuantityKind`: `Temperature`, `Pressure`, `RelativeHumidity`, `WindDirection`, `MeanWindSpeed`, `WindGust`, `Illuminance`, `Irradiance`, `RadiationEnergy`, `PrecipitationRate`, `PrecipitationAccumulation`, `Probability`. Further kinds need a documented name, unit and meaning in this record | User | Approved 2026-10-20 |
| C-38 | Numeric boundary | Only `IQuantityValidator` accepts `double?` (parser input); it returns `decimal` values. All other services take and return `decimal` | Orchestrator, recorded | Approved 2026-10-20 |
| C-39 | Output rounding | `IOutputRounding.Round(QuantityKind kind, decimal value)` rounds to the REQ-057 increment with `MidpointRounding.AwayFromZero`: `Temperature` 0.1, `Pressure` 1 (hPa and mmHg), `RelativeHumidity` 1, `MeanWindSpeed` and `WindGust` 1 (km/h). The result scale equals the increment's decimal places (20 → `20.0`, 20.25 → `20.3`, −20.25 → `−20.3`, 1000.5 → `1001`); a negative zero result is returned as zero (−0.04 → `0.0`). Kinds without a REQ-057 increment are returned unchanged (no new rounding policy). Rounding is applied only after every conversion | User | Approved 2026-10-20 |
| C-40 | Pressure | `IPressureConverter.FromHpa(decimal hpa, PressureReference reference)` and `FromMmHg(decimal mmHg, PressureReference reference)` return `PressureValues` with rounded `Hpa` and `MmHg`, the unchanged `Reference` (`Sensor`, `SeaLevel`, `Unknown`; wire values `sensor`, `sea_level`, `unknown`) and the `OriginalUnit`. Both outputs are computed from the one unrounded original with `mmHg = hPa × 0.7500617` and `hPa = mmHg × 1.3332239` (REQ-054), then rounded through `IOutputRounding`. Examples: 1000.5 hPa → 1001 hPa / 750 mmHg (not 751); 760 mmHg → 1013 hPa / 760 mmHg | User | Approved 2026-10-20 |
| C-41 | Pressure tolerance | For an hPa original h: the absolute difference between round(h) × 0.7500617 and round(mmHg) is at most 0.87503085 mmHg; the absolute difference between round(mmHg) × 1.3332239 and round(h) is at most 0.5 × 1.3332239 + 0.5 + abs(h) × 0.00000018491463 hPa. For an mmHg original m: the absolute difference between round(m) × 1.3332239 and round(hPa) is at most 1.16661195 hPa; the absolute difference between round(hPa) × 0.7500617 and round(m) is at most 0.5 × 0.7500617 + 0.5 + abs(m) × 0.00000018491463 mmHg. The factors a = 0.7500617 and b = 1.3332239 are not exact reciprocals: a × b = 1.00000018491463. Example: 100000000 mmHg → 133322390 hPa, 133322390 × 0.7500617 − 100000000 = 18.491463 mmHg, within its bound. For realistic pressures (up to 1100 hPa, about 825 mmHg) the discrepancy term is below 0.0003; consumers must not expect the two rounded outputs to convert into each other exactly | User | Approved 2026-10-20 |
| C-42 | Validity and omission | `IQuantityValidator.Validate(QuantityKind kind, double? value)` returns `ValidatedQuantity` with the `decimal?` value or a `QuantityRejectionReason` (`Missing`, `NonFinite`, `OutOfRange`); a rejected value is omitted by the caller (C-04) and its reason is available for logging. `null` is `Missing` and is never defaulted (no north for a missing wind direction, no zero for missing humidity or illuminance). Zero is valid. Ranges: `Temperature` any finite value (negative allowed); `Pressure` greater than 0; `RelativeHumidity` 0 to 100; `WindDirection` see C-43; `MeanWindSpeed`, `WindGust`, `Illuminance`, `Irradiance`, `RadiationEnergy`, `PrecipitationRate`, `PrecipitationAccumulation` 0 or greater; `Probability` 0 to 100 (percent). A finite value outside the `decimal` range is `OutOfRange`. Ranges are checked on the original `double` before the conversion to `decimal`, because the conversion rounds (humidity 100.00000000000001 must not become 100, direction 360.00000000000006 must not become north, −1e-30 must not become zero); a positive pressure that converts to 0 (1e-30) is also `OutOfRange` (clarified after the user's implementation review 2026-10-20) | User | Approved 2026-10-20 |
| C-43 | Wind direction range | Accept 0 to 360 inclusive and normalize 360 to 0 (both mean north under REQ-052; user decision 2026-10-20); reject values below 0 or above 360. Mapping a source's calm marker to an omitted direction belongs to the provider phases | User | Approved 2026-10-20 |
| C-44 | Averaging period | `AveragingPeriod` is an immutable value with `StartUtc` and `EndUtc` (`DateTimeKind.Utc`, whole seconds) and the derived `Duration`; a non-UTC kind, a non-zero sub-second part at either endpoint (never truncated or rounded, so both endpoints stay exportable under C-06) or an end not after the start (zero or negative duration) is rejected with `ArgumentException`; the shortest valid period is one second. The period is carried in every period-derived result so the export can preserve it (REQ-055) | User | Approved 2026-10-20 |
| C-45 | Period-aware conversion | `IPeriodRateConverter.Convert(QuantityKind sourceKind, QuantityKind targetKind, decimal value, AveragingPeriod? period)` returns `PeriodQuantity` (`Value`, `Kind`, `Period`). Only two pairs exist: `RadiationEnergy` (J/m²) → `Irradiance` (W/m²) as value ÷ duration in seconds, and `PrecipitationAccumulation` (mm) → `PrecipitationRate` (mm/h) as value × 3600 ÷ duration in seconds. A missing period, and every other pair (for example gust → mean wind, illuminance → irradiance, probability → accumulation), raise `QuantityConversionException`. No lux ↔ W/m² path exists. Results are unrounded. The rain rate is computed without an avoidable intermediate overflow (1e26 mm over one hour → exactly 1e26 mm/h); a result outside the `decimal` range raises `QuantityConversionException` (clarified after the user's implementation review 2026-10-20). Examples: 360000 J/m² over 600 s → 600 W/m²; 0.5 mm over 10 minutes → 3 mm/h | User | Approved 2026-10-20 |
| C-46 | Wind speed unit | `IWindSpeedConverter.ToKilometresPerHour(decimal metresPerSecond)` multiplies by the exact factor 3.6 and returns the unrounded value; rounding follows C-39. Example: 3.47 m/s → 12.492 km/h → rounded 12. Verifying that a source delivers m/s belongs to P5/P7 | User | Approved 2026-10-20 |
| C-47 | Registration | All services are registered as singletons in `AddWeatherParsing` and resolved through their interfaces; `PressureConverter` receives `IOutputRounding` by constructor | Orchestrator, recorded | Approved 2026-10-20 |

## 10. P2.06 decisions (observation graph)

Proposed on 2026-10-20, revised after six read-only reviews by the OpenAI Codex CLI (first review: eight findings: empty graphs bypassing the builder, assignment tracking for nested enums, converter overflow, duplicate and omission precedence, normalized-time invariants, empty optional strings, nullable coordinates, source grammar; five further rounds until no findings remained); approved by the user on 2026-10-20 (section 1) before the first P2.06 Red test. The builder assembles the observation graph from P2.04 time results and P2.05 quantity services; parsing source formats belongs to P5 (DWD) and P6 (OsthessenWetter).

| ID | Decision | Value | Decided by | Status |
|---|---|---|---|---|
| C-48 | Names | Export entities in `Export/`: `ObservationDataset` and `ObservationMeasurements` (extended from P2.02), `MeasurementValue` (extended), new `PressureMeasurementValue`, `ObservationLocation`, `MeasurementPeriod`. Builder in folder `Observations/`, namespace `WeatherParser.Parsing.Observations`: `IObservationDatasetBuilder`/`ObservationDatasetBuilder`, inputs `ObservationInput` and `ObservationReading`, enum `WindSpeedUnit`, result `ObservationBuildResult` with `OmittedReading` and enum `OmissionReason`, failure `ObservationDatasetRejectedException` | User | Approved 2026-10-20 |
| C-49 | Dataset fields | `observation_dataset` members in this order: `schema_version`, `source`, `location`, `measured_at`, `retrieved_at`, `timestamp_quality`, `measurements` (REQ-050). All are required and tracked as in C-27: serializing an unassigned member fails with `ExportSerializationException` and writes nothing. `source` therefore loses its P2.02 default of an empty string. Serializing a dataset whose `measurements` contains no quantity also fails with `ExportSerializationException` and writes nothing, so a directly assembled graph cannot bypass C-60 (REQ-050). Serializing an `observation_dataset` also checks the graph invariants of C-64, so a directly assembled graph cannot violate what the builder guarantees. | User | Approved 2026-10-20 |
| C-50 | `source` format | One string `<provider>:<station_id>` (user decision 2026-10-20), for example `dwd:00044` or `osthessenwetter:<station id>`; the builder composes it, so it is unique per provider and station (REQ-041, REQ-050). `provider` matches `\A[a-z][a-z0-9-]*\z` (so it never contains `:`); `station_id` is the location's ID, non-empty, not whitespace-only and kept exactly (no trimming, leading zeros kept); the first `:` separates them. Violations raise `ArgumentException` in the builder. Provider-specific ID derivation and uniqueness tests belong to P5 and P6 | User | Approved 2026-10-20 |
| C-51 | Location | `location` object in this order: `station_id` and `name` (strings, required; original names and umlauts kept, REQ-041), `latitude` and `longitude` (decimal degrees, required; user decision 2026-10-20), `altitude_m` (`decimal?`, optional, omitted when unknown through the C-05 `HasValue` mechanism, never a substituted zero). Required members track assignment as in C-27; real zero values are valid and written. A source without verified coordinates (for example OsthessenWetter until P6) cannot produce an observation dataset until P6 supplies them. `latitude` is within −90 to 90 and `longitude` within −180 to 180 (inclusive); `station_id` and `name` are non-empty and not whitespace-only and kept exactly. The entity setters reject violations with `ArgumentException`; zero coordinates, negative altitudes and original names with umlauts stay valid. | User | Approved 2026-10-20 |
| C-52 | Measurement shape | `measurements` object with one optional typed member per REQ-052 field, in this order: `temperature_c`, `pressure_hpa`, `pressure_mmhg`, `humidity_percent`, `wind_direction_deg`, `wind_speed_kmh`, `illuminance_lux`, `solar_irradiance_w_m2`, `rain_rate_mm_h`. A missing quantity omits the whole member (C-04) | User | Approved 2026-10-20 |
| C-53 | Value object | `MeasurementValue` members in this order: `value` (decimal, required), `measured_at` (C-06 form, required), `timestamp_quality` (`source` or `estimated_date`, required, REQ-042), `source_timezone` (optional string, `Europe/Berlin` when converted from Berlin local time, C-32), `period` (optional `MeasurementPeriod` with required `start` and `end` in the C-06 form, REQ-055), `quality` (optional string: the source quality code unchanged; its meaning is verified per provider in P5/P6). Optional members are omitted when absent, never `null`, `xsi:nil` or empty. Required members, including the enum `timestamp_quality`, track assignment as in C-27: an unassigned member fails serialization even though its enum zero value `source` is valid when assigned. `source_timezone` and `quality` are omitted when `null`; the `source_timezone` setter accepts only `Europe/Berlin` (C-32) and the `quality` setter rejects an empty or whitespace-only string, both with `ArgumentException`; non-empty values are kept unchanged (no trimming). `period` follows C-44: both `start` and `end` are required (C-27 tracking) and `end` must be after `start`; a violating period fails serialization (C-49). | User | Approved 2026-10-20 |
| C-54 | Pressure | `pressure_hpa` and `pressure_mmhg` are `PressureMeasurementValue`: the C-53 members plus the required `pressure_reference` (`sensor`, `sea_level` or `unknown`, REQ-054). Both come from one reading through `IPressureConverter` (C-40) and carry the same `measured_at`, markers, quality and reference. `pressure_reference` tracks assignment as in C-27; an unassigned reference fails serialization even though its zero value `sensor` is valid when assigned. | User | Approved 2026-10-20 |
| C-55 | Dataset `measured_at` | The latest `measured_at` of the exported readings (user decision 2026-10-20), documented as the time of the most recent reading; it never implies that readings are aligned, and the per-quantity times stay authoritative (REQ-056). Limitation: the dataset `measured_at` alone does not identify a dataset revision (temperature still at 12:00 while humidity advances from 11:50 to 11:55 gives the same dataset `measured_at`); consumers compare the per-quantity `measured_at` values, and the MQTT duplicate-handling rule of REQ-062 is decided in P8. | User | Approved 2026-10-20 |
| C-56 | Dataset `timestamp_quality` | `estimated_date` if any exported reading is `estimated_date`, otherwise `source`, so an estimate is never hidden at dataset level | User | Approved 2026-10-20 |
| C-57 | Builder input | `IObservationDatasetBuilder.Build(ObservationInput input)` returns `ObservationBuildResult`. `ObservationInput`: `Provider` (lowercase token), `Location` (`ObservationLocation`), `RetrievedAtUtc`, `Readings` (list of `ObservationReading`). `ObservationReading`: `Kind` (`QuantityKind?`; a missing kind raises `ArgumentException`, so it never defaults to `Temperature`), `Value` (`double?`), `Time` (`NormalizedSourceTime?`), `Period` (`AveragingPeriod?`), `Quality` (`string?`), `PressureUnit?` and `PressureReference?` (required for `Pressure`), `WindSpeedUnit?` (`KilometresPerHour` or `MetresPerSecond`, required for `MeanWindSpeed`); these nullable enums make a missing value distinguishable from an assigned zero value such as `Sensor`, and an undefined value of any input enum, including `Kind`, raises `ArgumentException`. A missing unit or reference for its kind, an invalid provider token, and two readings that map to the same export field are caller errors and raise `ArgumentException`; readings are never merged. Duplicates are detected on the input before any validation or omission, independent of order and values: `Irradiance` and `RadiationEnergy` both target `solar_irradiance_w_m2`, `PrecipitationRate` and `PrecipitationAccumulation` both target `rain_rate_mm_h`, and any kind given twice targets its field twice; `WindGust` and `Probability` target no field and are never duplicates. A supplied `NormalizedSourceTime` must have a UTC `UtcInstant` with whole seconds, a defined `Quality`, `SourceTimeZone` `Europe/Berlin` when `Quality` is `estimated_date`, and otherwise `SourceTimeZone` either absent or `Europe/Berlin` (C-32); any other combination raises `ArgumentException`. This amends C-38: the builder also accepts the raw `double?` reading values, but it delegates every numeric check and the conversion to `decimal` to `IQuantityValidator`; it performs no own range checks or casts. | User | Approved 2026-10-20 |
| C-58 | Kind mapping and processing | Each reading is validated with `IQuantityValidator` (C-42), converted, and rounded only for output (C-39). `Temperature` → `temperature_c`; `Pressure` → `pressure_hpa` and `pressure_mmhg` (C-40); `RelativeHumidity` → `humidity_percent`; `WindDirection` → `wind_direction_deg`; `MeanWindSpeed` → `wind_speed_kmh` (m/s converted with C-46, then rounded); `Illuminance` → `illuminance_lux`; `Irradiance` → `solar_irradiance_w_m2`; `RadiationEnergy` → `solar_irradiance_w_m2` through C-45 with its period; `PrecipitationRate` → `rain_rate_mm_h`; `PrecipitationAccumulation` → `rain_rate_mm_h` through C-45 with its period. A given period is exported in `period`. `WindGust` and `Probability` have no observation field and are never written as another quantity (REQ-044, REQ-052). A conversion whose result is outside the `decimal` range (C-63), or an output value that cannot carry its required C-39 scale (C-65), omits that reading with reason `Unrepresentable`; other readings are still exported. | User | Approved 2026-10-20 |
| C-59 | Omission diagnostics | `ObservationBuildResult` holds the `Dataset` and the non-exported list `OmittedReadings` (`OmittedReading` with `Kind` and `Reason`). `OmissionReason`: `Missing`, `NonFinite`, `OutOfRange` (from C-42), `MissingTime` (a value without a time; the retrieval time is never substituted, REQ-043/REQ-044), `MissingPeriod` (energy or accumulation without a period), `NotExportable` (gust, probability), `Unrepresentable` (a validated reading whose converted value is outside the `decimal` range, C-63, or cannot carry its required output scale, C-65). Callers log the omissions (P5/P6); the library does not log. Each omitted reading has exactly one reason, the first that applies in this order: `NotExportable`, `Missing`, `NonFinite`, `OutOfRange`, `MissingTime`, `MissingPeriod`, `Unrepresentable`. | Orchestrator, recorded | Approved 2026-10-20 |
| C-60 | No usable readings | If no reading is exported, `Build` raises `ObservationDatasetRejectedException` with an actionable English message naming the source and each omitted kind with its reason; no dataset is returned (REQ-050) | User | Approved 2026-10-20 |
| C-61 | Enum wire values | `TimestampQuality` (`source`, `estimated_date`) and `PressureReference` (`sensor`, `sea_level`, `unknown`) get the same string mapping as C-23 (`XmlEnum`, `JsonStringEnumMemberName`, `StrictStringEnumConverter`); the entity setters reject undefined values. Assignment tracking for these members follows C-53 and C-54. | Orchestrator, recorded | Approved 2026-10-20 |
| C-62 | Registration | `ObservationDatasetBuilder` is registered as a singleton in `AddWeatherParsing` and receives `IQuantityValidator`, `IOutputRounding`, `IPressureConverter`, `IPeriodRateConverter` and `IWindSpeedConverter` by constructor | Orchestrator, recorded | Approved 2026-10-20 |
| C-63 | Converter overflow | Amends C-40 and C-46: `IPressureConverter` and `IWindSpeedConverter` raise `QuantityConversionException` (with the `OverflowException` as inner exception) when a converted value is outside the `decimal` range, for example 3e28 m/s × 3.6, as `IPeriodRateConverter` already does (C-45); a raw `OverflowException` never leaves these services | User | Approved 2026-10-20 |
| C-64 | Graph validation on serialization | Before either serializer writes an `observation_dataset`, the graph is checked; any violation fails with `ExportSerializationException` and writes nothing. Checks: (1) `measurements` contains at least one quantity (C-60); (2) `source` matches C-50 and its station part equals `location.station_id` exactly; (3) the dataset `measured_at` equals the latest `measured_at` of the present quantities (C-55) and the dataset `timestamp_quality` follows C-56; (4) every value with `timestamp_quality` `estimated_date` has `source_timezone` `Europe/Berlin` (REQ-042); (5) every `period` ends after it starts (C-44); (6) `pressure_hpa` and `pressure_mmhg` are both present or both absent, with the same `measured_at`, markers, quality, `period` and `pressure_reference`, and at least one original unit can produce both rounded outputs under C-40: the absolute difference between `pressure_hpa` × 0.7500617 and `pressure_mmhg` is at most 0.5 × 0.7500617 + 0.5 (an hPa original), or the absolute difference between `pressure_mmhg` × 1.3332239 and `pressure_hpa` is at most 0.5 × 1.3332239 + 0.5 (an mmHg original); these forms are equivalent to the overlap of the rounding intervals and avoid an intermediate overflow: `pressure_hpa` × 0.7500617 is always representable, and when `pressure_mmhg` × 1.3332239 is not, the mmHg direction cannot produce the pair (its converter would already fail under C-63); for example 1001 hPa / 750 mmHg is accepted and 100000000 hPa / 75006160 mmHg is rejected; (7) each value lies in the output range of its field: `humidity_percent` 0 to 100, `wind_direction_deg` 0 to below 360 (360 is normalized to 0, C-43), `pressure_hpa`, `pressure_mmhg`, `wind_speed_kmh`, `illuminance_lux`, `solar_irradiance_w_m2` and `rain_rate_mm_h` 0 or greater (pressure may round to 0 under C-39), `temperature_c` any value; (8) values of fields with a C-39 increment have exactly the scale that C-39 produces: `temperature_c` exactly one decimal place (`20.0`, not `20` or `20.25`), `pressure_hpa`, `pressure_mmhg`, `humidity_percent` and `wind_speed_kmh` no decimal places (`55`, not `55.0` or `55.5`); a directly assembled value must be passed through `IOutputRounding` first | User | Approved 2026-10-20 |
| C-65 | Unrepresentable output scale | Amends C-39: when a value cannot carry the scale that C-39 requires because the scaled coefficient exceeds the `decimal` capacity (for example a temperature of 7e28 cannot be written with one decimal place), `IOutputRounding.Round` raises `QuantityConversionException` instead of returning a value with a smaller scale; the builder omits that reading with reason `Unrepresentable` (C-58, C-59), so check (8) of C-64 never rejects a builder-produced graph. A regression test covers this case from the builder to both serializers | User | Approved 2026-10-20 |

### Field mapping (observation dataset)

| JSON path | XML path | Entity member | Type | Required |
|---|---|---|---|---|
| `schema_version` | `/observation_dataset/schema_version` | `ObservationDataset.SchemaVersion` | string `"1"` | yes |
| `source` | `/observation_dataset/source` | `ObservationDataset.Source` | string (C-50) | yes |
| `location.station_id` | `.../location/station_id` | `ObservationLocation.StationId` | string | yes |
| `location.name` | `.../location/name` | `ObservationLocation.Name` | string | yes |
| `location.latitude`, `location.longitude` | `.../location/latitude`, `.../location/longitude` | `ObservationLocation.Latitude`, `Longitude` | decimal | yes (C-51) |
| `location.altitude_m` | `.../location/altitude_m` | `ObservationLocation.AltitudeM` | `decimal?` | no (omitted when unknown; zero is a real value) |
| `measured_at` | `/observation_dataset/measured_at` | `ObservationDataset.MeasuredAtWire` | string, C-06 form | yes |
| `retrieved_at` | `/observation_dataset/retrieved_at` | `ObservationDataset.RetrievedAtWire` | string, C-06 form | yes |
| `timestamp_quality` | `/observation_dataset/timestamp_quality` | `ObservationDataset.TimestampQuality` | `source` or `estimated_date` | yes |
| `measurements.<q>` | `.../measurements/<q>` | `ObservationMeasurements.<Q>` | value object | no (omitted when missing) |
| `measurements.<q>.value` | `.../<q>/value` | `MeasurementValue.Value` | decimal | yes within the object |
| `measurements.<q>.measured_at` | `.../<q>/measured_at` | `MeasurementValue.MeasuredAtWire` | string, C-06 form | yes within the object |
| `measurements.<q>.timestamp_quality` | `.../<q>/timestamp_quality` | `MeasurementValue.TimestampQuality` | `source` or `estimated_date` | yes within the object |
| `measurements.<q>.source_timezone` | `.../<q>/source_timezone` | `MeasurementValue.SourceTimezone` | string | no |
| `measurements.<q>.period.start`, `.end` | `.../<q>/period/start`, `.../end` | `MeasurementPeriod.StartWire`, `EndWire` | string, C-06 form | object optional, members required |
| `measurements.<q>.quality` | `.../<q>/quality` | `MeasurementValue.Quality` | string | no |
| `measurements.pressure_hpa.pressure_reference`, `measurements.pressure_mmhg.pressure_reference` | `.../pressure_hpa/pressure_reference` etc. | `PressureMeasurementValue.PressureReference` | `sensor`, `sea_level` or `unknown` | yes within the object |

Example (JSON, recommended options): `{"schema_version":"1","source":"dwd:00044","location":{"station_id":"00044","name":"Großenkneten","latitude":52.9336,"longitude":8.2370,"altitude_m":44},"measured_at":"2026-07-15T12:00:00","retrieved_at":"2026-07-15T12:05:00","timestamp_quality":"source","measurements":{"temperature_c":{"value":20.3,"measured_at":"2026-07-15T12:00:00","timestamp_quality":"source"},"humidity_percent":{"value":55,"measured_at":"2026-07-15T11:50:00","timestamp_quality":"source"}}}` – humidity was measured ten minutes before temperature; the dataset `measured_at` is the later time.
