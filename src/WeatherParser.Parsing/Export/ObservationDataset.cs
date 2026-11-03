using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents a complete observation dataset.
/// </summary>
[Serializable]
[XmlRoot("observation_dataset", Namespace = ExportNamespaces.V1)]
public sealed class ObservationDataset : IExportRootValidation
{
    /// <summary>
    /// Gets or sets the schema version of this dataset.
    /// </summary>
    [XmlElement("schema_version")]
    [JsonPropertyName("schema_version")]
    public string SchemaVersion
    {
        get => ExportNamespaces.SchemaVersionV1;
        set
        {
            if (value != ExportNamespaces.SchemaVersionV1)
            {
                throw new ArgumentException($"Unsupported schema_version '{value}'; contract v1 requires '{ExportNamespaces.SchemaVersionV1}'.", nameof(SchemaVersion));
            }
        }
    }

    private RequiredMember<string> _source;

    /// <summary>
    /// Gets or sets the source identifier for this dataset.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("source")]
    [JsonPropertyName("source")]
    public string Source
    {
        get => _source.Get("source");
        set => _source = RequiredMember<string>.Of(value);
    }

    private RequiredMember<ObservationLocation> _location;

    /// <summary>
    /// Gets or sets the location of the observation.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("location")]
    [JsonPropertyName("location")]
    public ObservationLocation Location
    {
        get => _location.Get("location");
        set => _location = RequiredMember<ObservationLocation>.Of(value);
    }

    private ExportTimestamp _measuredAt;

    /// <summary>
    /// Gets or sets the UTC instant at which the observation was measured.
    /// Rejects non-UTC kinds and fractional seconds.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public DateTime MeasuredAtUtc
    {
        get => _measuredAt.Utc;
        set => _measuredAt = ExportTimestamp.FromUtc(value, nameof(MeasuredAtUtc));
    }

    /// <summary>
    /// Gets or sets the suffix-free UTC wire form (YYYY-MM-DDTHH:mm:ss) used by both serializers.
    /// </summary>
    [JsonPropertyName("measured_at")]
    [XmlElement("measured_at")]
    public string MeasuredAtWire
    {
        get => _measuredAt.ToWire("measured_at");
        set => _measuredAt = ExportTimestamp.ParseWire(value, nameof(MeasuredAtWire));
    }

    private ExportTimestamp _retrievedAt;

    /// <summary>
    /// Gets or sets the UTC instant at which the dataset was retrieved.
    /// Rejects non-UTC kinds and fractional seconds.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public DateTime RetrievedAtUtc
    {
        get => _retrievedAt.Utc;
        set => _retrievedAt = ExportTimestamp.FromUtc(value, nameof(RetrievedAtUtc));
    }

    /// <summary>
    /// Gets or sets the suffix-free UTC wire form (YYYY-MM-DDTHH:mm:ss) used by both serializers.
    /// </summary>
    [JsonPropertyName("retrieved_at")]
    [XmlElement("retrieved_at")]
    public string RetrievedAtWire
    {
        get => _retrievedAt.ToWire("retrieved_at");
        set => _retrievedAt = ExportTimestamp.ParseWire(value, nameof(RetrievedAtWire));
    }

    private RequiredMember<TimestampQuality> _timestampQuality;

    /// <summary>
    /// Gets or sets the quality of the timestamp.
    /// Reading an unassigned value throws, so no default is exported.
    /// Undefined enum values are rejected.
    /// </summary>
    [XmlElement("timestamp_quality")]
    [JsonPropertyName("timestamp_quality")]
    public TimestampQuality TimestampQuality
    {
        get => _timestampQuality.Get("timestamp_quality");
        set => _timestampQuality = Enum.IsDefined(value)
            ? RequiredMember<TimestampQuality>.Of(value)
            : throw new ArgumentException($"Undefined timestamp_quality value '{value}'.", nameof(TimestampQuality));
    }

    /// <summary>
    /// Gets or sets the measurements included in this dataset.
    /// </summary>
    [XmlElement("measurements")]
    [JsonPropertyName("measurements")]
    public ObservationMeasurements Measurements { get; set; } = new();

    private static readonly Regex ProviderPattern = new(@"\A[a-z][a-z0-9-]*\z", RegexOptions.CultureInvariant);
    private static readonly decimal HpaToMmHg = 0.7500617m;
    private static readonly decimal MmHgToHpa = 1.3332239m;

    private IEnumerable<(string Name, MeasurementValue Value)> PresentQuantities()
    {
        if (Measurements.TemperatureC is not null)
            yield return ("temperature_c", Measurements.TemperatureC);
        if (Measurements.PressureHpa is not null)
            yield return ("pressure_hpa", Measurements.PressureHpa);
        if (Measurements.PressureMmHg is not null)
            yield return ("pressure_mmhg", Measurements.PressureMmHg);
        if (Measurements.HumidityPercent is not null)
            yield return ("humidity_percent", Measurements.HumidityPercent);
        if (Measurements.WindDirectionDeg is not null)
            yield return ("wind_direction_deg", Measurements.WindDirectionDeg);
        if (Measurements.WindSpeedKmh is not null)
            yield return ("wind_speed_kmh", Measurements.WindSpeedKmh);
        if (Measurements.IlluminanceLux is not null)
            yield return ("illuminance_lux", Measurements.IlluminanceLux);
        if (Measurements.SolarIrradianceWm2 is not null)
            yield return ("solar_irradiance_w_m2", Measurements.SolarIrradianceWm2);
        if (Measurements.RainRateMmH is not null)
            yield return ("rain_rate_mm_h", Measurements.RainRateMmH);
    }

    private static InvalidOperationException Violation(string message) => new($"The observation dataset violates contract C-64: {message}");

    void IExportRootValidation.ValidateForExport()
    {
        if (Measurements is null) throw Violation("measurements is missing");
        
        var present = PresentQuantities().ToList();
        if (present.Count == 0)
            throw Violation("measurements contains no quantity");

        var separator = Source.IndexOf(':');
        if (separator <= 0)
            throw Violation($"source '{Source}' must be '<provider>:<station_id>' with station_id '{Location.StationId}'");
        
        var provider = Source[..separator];
        var station = Source[(separator + 1)..];
        if (!ProviderPattern.IsMatch(provider) || !string.Equals(station, Location.StationId, StringComparison.Ordinal))
            throw Violation($"source '{Source}' must be '<provider>:<station_id>' with station_id '{Location.StationId}'");

        var maxTime = present.Max(x => x.Value.MeasuredAtUtc);
        if (MeasuredAtUtc != maxTime)
            throw Violation("measured_at must equal the latest reading time");

        var anyEstimated = present.Any(x => x.Value.TimestampQuality == TimestampQuality.EstimatedDate);
        if (anyEstimated != (TimestampQuality == TimestampQuality.EstimatedDate))
            throw Violation("timestamp_quality must be estimated_date exactly when a reading is estimated");

        foreach (var (name, value) in present)
        {
            if (value.TimestampQuality == TimestampQuality.EstimatedDate && value.SourceTimezone != "Europe/Berlin")
                throw Violation($"{name} is estimated but lacks source_timezone Europe/Berlin");

            if (value.Period is not null && value.Period.EndUtc <= value.Period.StartUtc)
                throw Violation($"{name} period must end after it starts");

            var v = value.Value;
            switch (name)
            {
                case "humidity_percent":
                    if (v < 0 || v > 100)
                        throw Violation($"{name} value {v.ToString(CultureInfo.InvariantCulture)} is outside its output range");
                    break;
                case "wind_direction_deg":
                    if (v < 0 || v >= 360)
                        throw Violation($"{name} value {v.ToString(CultureInfo.InvariantCulture)} is outside its output range");
                    break;
                case "pressure_hpa":
                case "pressure_mmhg":
                case "wind_speed_kmh":
                case "illuminance_lux":
                case "solar_irradiance_w_m2":
                case "rain_rate_mm_h":
                    if (v < 0)
                        throw Violation($"{name} value {v.ToString(CultureInfo.InvariantCulture)} is outside its output range");
                    break;
            }

            switch (name)
            {
                case "temperature_c":
                    if (value.Value.Scale != 1)
                        throw Violation($"{name} value {v.ToString(CultureInfo.InvariantCulture)} does not have the output scale of contract C-39");
                    break;
                case "pressure_hpa":
                case "pressure_mmhg":
                case "humidity_percent":
                case "wind_speed_kmh":
                    if (value.Value.Scale != 0)
                        throw Violation($"{name} value {v.ToString(CultureInfo.InvariantCulture)} does not have the output scale of contract C-39");
                    break;
            }
        }

        var hpa = Measurements.PressureHpa;
        var mmhg = Measurements.PressureMmHg;

        if (hpa is null && mmhg is not null)
            throw Violation("pressure_hpa and pressure_mmhg must be present together");
        if (hpa is not null && mmhg is null)
            throw Violation("pressure_hpa and pressure_mmhg must be present together");

        if (hpa is not null && mmhg is not null)
        {
            if (hpa.MeasuredAtUtc != mmhg.MeasuredAtUtc)
                throw Violation("pressure_hpa and pressure_mmhg must share time, markers, quality, period and reference");
            if (hpa.TimestampQuality != mmhg.TimestampQuality)
                throw Violation("pressure_hpa and pressure_mmhg must share time, markers, quality, period and reference");
            if (hpa.SourceTimezone != mmhg.SourceTimezone)
                throw Violation("pressure_hpa and pressure_mmhg must share time, markers, quality, period and reference");
            if (hpa.Quality != mmhg.Quality)
                throw Violation("pressure_hpa and pressure_mmhg must share time, markers, quality, period and reference");
            if (hpa.PressureReference != mmhg.PressureReference)
                throw Violation("pressure_hpa and pressure_mmhg must share time, markers, quality, period and reference");

            if ((hpa.Period is null) != (mmhg.Period is null))
                throw Violation("pressure_hpa and pressure_mmhg must share time, markers, quality, period and reference");
            else if (hpa.Period is not null)
            {
                if (hpa.Period.StartUtc != mmhg.Period!.StartUtc || hpa.Period.EndUtc != mmhg.Period.EndUtc)
                    throw Violation("pressure_hpa and pressure_mmhg must share time, markers, quality, period and reference");
            }

            var h = hpa.Value;
            var m = mmhg.Value;

            var fromHpa = Math.Abs(h * HpaToMmHg - m) <= 0.5m * HpaToMmHg + 0.5m;
            bool fromMmHg;
            try
            {
                fromMmHg = Math.Abs(m * MmHgToHpa - h) <= 0.5m * MmHgToHpa + 0.5m;
            }
            catch (OverflowException)
            {
                fromMmHg = false;
            }

            if (!fromHpa && !fromMmHg)
                throw Violation($"pressure_hpa {h.ToString(CultureInfo.InvariantCulture)} and pressure_mmhg {m.ToString(CultureInfo.InvariantCulture)} cannot come from one original value (contract C-40, C-64)");
        }
    }
}
