using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Export;

/// <summary>
/// Represents a forecast run.
/// </summary>
[Serializable]
[XmlRoot("forecast_run", Namespace = ExportNamespaces.V1)]
public sealed class ForecastRun : IExportRootValidation
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

    private static readonly string DatasetTypeValue = "forecast";

    /// <summary>
    /// Gets or sets the dataset type.
    /// </summary>
    [XmlElement("dataset_type")]
    [JsonPropertyName("dataset_type")]
    public string DatasetType
    {
        get => DatasetTypeValue;
        set
        {
            if (value != DatasetTypeValue)
            {
                throw new ArgumentException($"dataset_type must be '{DatasetTypeValue}'.", nameof(DatasetType));
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
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("source must be non-empty (contract C-68).", nameof(Source));
            }

            _source = RequiredMember<string>.Of(value);
        }
    }

    private RequiredMember<string> _mosmixId;

    /// <summary>
    /// Gets or sets the MOSMIX ID of the forecast run.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("mosmix_id")]
    [JsonPropertyName("mosmix_id")]
    public string MosmixId
    {
        get => _mosmixId.Get("mosmix_id");
        set
        {
            if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsWhiteSpace) || value.Contains(':'))
            {
                throw new ArgumentException("mosmix_id must be non-empty without whitespace or ':' (contract C-69).", nameof(MosmixId));
            }

            _mosmixId = RequiredMember<string>.Of(value);
        }
    }

    private RequiredMember<ForecastLocation> _location;

    /// <summary>
    /// Gets or sets the location of the forecast run.
    /// Reading an unassigned value throws, so no default is exported.
    /// </summary>
    [XmlElement("location")]
    [JsonPropertyName("location")]
    public ForecastLocation Location
    {
        get => _location.Get("location");
        set => _location = RequiredMember<ForecastLocation>.Of(value);
    }

    private RequiredMember<MosmixProduct> _product;

    /// <summary>
    /// Gets or sets the product type of the forecast run.
    /// Reading an unassigned value throws, so no default is exported.
    /// Undefined enum values are rejected.
    /// </summary>
    [XmlElement("product")]
    [JsonPropertyName("product")]
    public MosmixProduct Product
    {
        get => _product.Get("product");
        set => _product = Enum.IsDefined(value)
            ? RequiredMember<MosmixProduct>.Of(value)
            : throw new ArgumentException($"Undefined product value '{value}'.", nameof(Product));
    }

    private ExportTimestamp _issuedAt;

    /// <summary>
    /// Gets or sets the UTC instant at which the forecast run was issued.
    /// Rejects non-UTC kinds and fractional seconds.
    /// </summary>
    [JsonIgnore]
    [XmlIgnore]
    public DateTime IssuedAtUtc
    {
        get => _issuedAt.Utc;
        set => _issuedAt = ExportTimestamp.FromUtc(value, nameof(IssuedAtUtc));
    }

    /// <summary>
    /// Gets or sets the suffix-free UTC wire form (YYYY-MM-DDTHH:mm:ss) used by both serializers.
    /// </summary>
    [JsonPropertyName("issued_at")]
    [XmlElement("issued_at")]
    public string IssuedAtWire
    {
        get => _issuedAt.ToWire("issued_at");
        set => _issuedAt = ExportTimestamp.ParseWire(value, nameof(IssuedAtWire));
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

    /// <summary>
    /// Gets or sets the list of forecast steps.
    /// </summary>
    [JsonPropertyName("forecasts")]
    [XmlArray("forecasts")]
    [XmlArrayItem("forecast")]
    public List<ForecastStep> Forecasts { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the run is older than the consumer's age limit (contract MF-09); omitted when unset (C-05).
    /// </summary>
    [XmlElement("run_age_exceeded")]
    [JsonPropertyName("run_age_exceeded")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? RunAgeExceeded { get; set; }

    /// <summary>
    /// Controls XML output of run_age_exceeded; true exactly when RunAgeExceeded has a value.
    /// </summary>
    [XmlIgnore]
    [JsonIgnore]
    public bool RunAgeExceededSpecified
    {
        get => RunAgeExceeded.HasValue;
        set
        {
            if (!value)
            {
                RunAgeExceeded = null;
            }
        }
    }

    private static readonly decimal HpaToMmHg = 0.7500617m;
    private static readonly decimal MmHgToHpa = 1.3332239m;

    private static InvalidOperationException Violation(string message) => new($"The forecast run violates contract C-79: {message}");

    void IExportRootValidation.ValidateForExport()
    {
        if (Forecasts is null || Forecasts.Count == 0)
            throw Violation("forecasts contains no step");

        if (Forecasts.Any(step => step is null))
            throw Violation("forecasts contains a missing step");

        var expectedSource = (Product == MosmixProduct.MosmixS ? "dwd-mosmix-s:" : "dwd-mosmix-l:") + MosmixId;
        if (!string.Equals(Source, expectedSource, StringComparison.Ordinal))
            throw Violation($"source '{Source}' must be '{expectedSource}'");

        for (int i = 1; i < Forecasts.Count; i++)
        {
            if (Forecasts[i].ValidAtUtc <= Forecasts[i - 1].ValidAtUtc)
                throw Violation("forecasts must be strictly ascending by valid_at");
        }

        foreach (var step in Forecasts)
        {
            var v = step.Values;

            if (v.TemperatureC is null && v.PressureHpa is null && v.PressureMmHg is null && v.PressureReference is null &&
                v.HumidityPercent is null && v.WindDirectionDeg is null && v.WindSpeedKmh is null && v.IlluminanceLux is null &&
                v.SolarIrradianceWm2 is null && v.RainRateMmH is null && v.WindGustKmh is null && v.PrecipitationAmountMm is null)
                throw Violation("a step has no value");

            var anyPressure = v.PressureHpa.HasValue || v.PressureMmHg.HasValue || v.PressureReference.HasValue;
            var allPressure = v.PressureHpa.HasValue && v.PressureMmHg.HasValue && v.PressureReference.HasValue;
            if (anyPressure && !allPressure)
                throw Violation("pressure_hpa, pressure_mmhg and pressure_reference must be present together");

            if (allPressure)
            {
                if (v.PressureHpa!.Value < 0 || v.PressureMmHg!.Value < 0)
                    throw Violation("a value is outside its output range");

                var h = v.PressureHpa!.Value;
                var m = v.PressureMmHg!.Value;

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
                    throw Violation("pressure_hpa and pressure_mmhg cannot come from one original value");
            }

            ValidateInterval(v.SolarIrradianceWm2, IntervalAggregation.Mean);
            ValidateInterval(v.RainRateMmH, IntervalAggregation.Mean);
            ValidateInterval(v.WindGustKmh, IntervalAggregation.Maximum);
            ValidateInterval(v.PrecipitationAmountMm, IntervalAggregation.Sum);

            if (v.HumidityPercent is not null && (v.HumidityPercent < 0 || v.HumidityPercent > 100))
                throw Violation("a value is outside its output range");

            if (v.WindDirectionDeg is not null && (v.WindDirectionDeg < 0 || v.WindDirectionDeg >= 360))
                throw Violation("a value is outside its output range");

            if (v.WindSpeedKmh is not null && v.WindSpeedKmh < 0)
                throw Violation("a value is outside its output range");

            if (v.IlluminanceLux is not null && v.IlluminanceLux < 0)
                throw Violation("a value is outside its output range");

            if (v.TemperatureC is not null && v.TemperatureC.Value.Scale != 1)
                throw Violation("a value does not have the output scale of contract C-39");

            if (v.PressureHpa is not null && v.PressureHpa.Value.Scale != 0)
                throw Violation("a value does not have the output scale of contract C-39");

            if (v.PressureMmHg is not null && v.PressureMmHg.Value.Scale != 0)
                throw Violation("a value does not have the output scale of contract C-39");

            if (v.HumidityPercent is not null && v.HumidityPercent.Value.Scale != 0)
                throw Violation("a value does not have the output scale of contract C-39");

            if (v.WindSpeedKmh is not null && v.WindSpeedKmh.Value.Scale != 0)
                throw Violation("a value does not have the output scale of contract C-39");

            if (v.WindGustKmh is not null && v.WindGustKmh.Value.Scale != 0)
                throw Violation("a value does not have the output scale of contract C-39");
        }
    }

    /// <summary>
    /// Checks one interval member against contract C-79: period order, a value of at least zero and the aggregation of its member (MF-11).
    /// </summary>
    private static void ValidateInterval(IntervalValue? interval, IntervalAggregation expected)
    {
        if (interval is null)
            return;

        if (interval.PeriodEndUtc <= interval.PeriodStartUtc)
            throw Violation("an interval must end after it starts");

        if (interval.Value < 0)
            throw Violation("an interval value is outside its output range");

        if (interval.Aggregation != expected)
            throw Violation("an interval has the wrong aggregation for its member");
    }
}
