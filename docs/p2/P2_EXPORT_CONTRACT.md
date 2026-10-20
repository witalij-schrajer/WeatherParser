# WeatherParser Export Contract (v1)

**Status (2026-10-20): P2.02 decisions C-01 to C-15 approved by the user (section 1); v1 stays provisional until P2 acceptance (C-03). Implementation of the slice follows in P2.02 S1–S7.**

This record holds the public export contract decisions of phase P2 ([phase plan](../../PHASE_P2_PLAN.md), [P2.02 plan](P2_02_CONTRACT_AND_FIRST_SLICE_PLAN.md)); later steps add their decisions here. Drafted by qwen3-coder:30b from the orchestrating agent's decision brief and corrected by qwen once; the orchestrating agent removed output markers, restored dropped section 6 bullets and corrected the XML example text. User review (2026-10-20): qwen redefined C-05 (nullable scalars as the single presence source in both formats) and C-03/C-13 (v1 provisional until P2 acceptance); the orchestrating agent removed output markers again.

## 1. Approval

| Date | Scope | Decision | Wording |
|------|-------|----------|---------|
| 2026-10-20 | P2.02 decisions C-01 to C-15 in sections 2–5 | Approved | User selected "Approve as proposed (Recommended)" for the question "Approve the revised P2.02 export contract C-01 to C-15 in docs/p2/P2_EXPORT_CONTRACT.md (C-05: nullable scalars as the single presence source in both formats; C-03/C-13: v1 provisional until P2 acceptance, then frozen; C-14 value-object quantities; C-15 readable umlauts in JSON)?" |

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

- observation `source` format, dataset `measured_at` rule, quality and period members (P2.06)
- list field names, availability shape (P2.03)
- midnight tolerance (P2.04)
- wind-direction range and m/s conversion (P2.05)
- forecast shapes (P2.07)
- envelope error record and partial marker (P2.08)
- schema generator and delivery (P2.09)
