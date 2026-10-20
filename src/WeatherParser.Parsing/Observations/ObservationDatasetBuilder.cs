namespace WeatherParser.Parsing.Observations;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Time;

/// <summary>
/// Builds observation datasets from input readings.
/// </summary>
public sealed class ObservationDatasetBuilder : IObservationDatasetBuilder
{
    private const string BerlinZoneId = "Europe/Berlin";
    private static readonly Regex ProviderPattern = new(@"\A[a-z][a-z0-9-]*\z", RegexOptions.CultureInvariant);

    private readonly IQuantityValidator _validator;
    private readonly IOutputRounding _rounding;
    private readonly IPressureConverter _pressureConverter;
    private readonly IPeriodRateConverter _periodRateConverter;
    private readonly IWindSpeedConverter _windSpeedConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservationDatasetBuilder"/> class.
    /// </summary>
    /// <param name="validator">The quantity validator.</param>
    /// <param name="rounding">The output rounding service.</param>
    /// <param name="pressureConverter">The pressure converter.</param>
    /// <param name="periodRateConverter">The period rate converter.</param>
    /// <param name="windSpeedConverter">The wind speed converter.</param>
    public ObservationDatasetBuilder(
        IQuantityValidator validator,
        IOutputRounding rounding,
        IPressureConverter pressureConverter,
        IPeriodRateConverter periodRateConverter,
        IWindSpeedConverter windSpeedConverter)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _rounding = rounding ?? throw new ArgumentNullException(nameof(rounding));
        _pressureConverter = pressureConverter ?? throw new ArgumentNullException(nameof(pressureConverter));
        _periodRateConverter = periodRateConverter ?? throw new ArgumentNullException(nameof(periodRateConverter));
        _windSpeedConverter = windSpeedConverter ?? throw new ArgumentNullException(nameof(windSpeedConverter));
    }

    /// <inheritdoc/>
    public ObservationBuildResult Build(ObservationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Provider is null || !ProviderPattern.IsMatch(input.Provider))
            throw new ArgumentException("The provider must be a lowercase alphanumeric string with optional hyphens (contract C-50).", nameof(input));
        if (input.Location is null)
            throw new ArgumentException("The location must not be null (contract C-51).", nameof(input));
        if (input.Readings is null)
            throw new ArgumentException("The readings must not be null (contract C-57).", nameof(input));

        var measurements = new ObservationMeasurements();
        var omitted = new List<OmittedReading>();
        var targetFields = new HashSet<string>();

        // First pass: validate and check for duplicates
        foreach (var reading in input.Readings)
        {
            ValidateReading(reading);

            var kind = reading.Kind!.Value;
            var field = TargetField(kind);
            if (field is not null && !targetFields.Add(field))
            {
                throw new ArgumentException($"The field '{field}' appears more than once in the readings (contract C-57).", nameof(input));
            }
        }

        // Second pass: process readings
        foreach (var reading in input.Readings)
        {
            var kind = reading.Kind!.Value;
            var field = TargetField(kind);
            if (field is null)
            {
                omitted.Add(new OmittedReading(kind, OmissionReason.NotExportable));
                continue;
            }

            var validated = _validator.Validate(kind, reading.Value);
            if (!validated.IsValid)
            {
                var reason = validated.RejectionReason switch
                {
                    QuantityRejectionReason.Missing => OmissionReason.Missing,
                    QuantityRejectionReason.NonFinite => OmissionReason.NonFinite,
                    _ => OmissionReason.OutOfRange
                };
                omitted.Add(new OmittedReading(kind, reason));
                continue;
            }

            if (reading.Time is null)
            {
                omitted.Add(new OmittedReading(kind, OmissionReason.MissingTime));
                continue;
            }

            if ((kind == QuantityKind.RadiationEnergy || kind == QuantityKind.PrecipitationAccumulation) && reading.Period is null)
            {
                omitted.Add(new OmittedReading(kind, OmissionReason.MissingPeriod));
                continue;
            }

            try
            {
                var number = validated.Value!.Value;
                switch (kind)
                {
                    case QuantityKind.Temperature:
                        measurements.TemperatureC = Value(_rounding.Round(QuantityKind.Temperature, number), reading);
                        break;
                    case QuantityKind.Pressure:
                        var p = reading.PressureUnit == PressureUnit.Hectopascal
                            ? _pressureConverter.FromHpa(number, reading.PressureReference!.Value)
                            : _pressureConverter.FromMmHg(number, reading.PressureReference!.Value);
                        measurements.PressureHpa = PressureValue(p.Hpa, p.Reference, reading);
                        measurements.PressureMmHg = PressureValue(p.MmHg, p.Reference, reading);
                        break;
                    case QuantityKind.RelativeHumidity:
                        measurements.HumidityPercent = Value(_rounding.Round(QuantityKind.RelativeHumidity, number), reading);
                        break;
                    case QuantityKind.WindDirection:
                        measurements.WindDirectionDeg = Value(number, reading);
                        break;
                    case QuantityKind.MeanWindSpeed:
                        var kmh = reading.WindSpeedUnit == WindSpeedUnit.MetresPerSecond
                            ? _windSpeedConverter.ToKilometresPerHour(number)
                            : number;
                        measurements.WindSpeedKmh = Value(_rounding.Round(QuantityKind.MeanWindSpeed, kmh), reading);
                        break;
                    case QuantityKind.Illuminance:
                        measurements.IlluminanceLux = Value(number, reading);
                        break;
                    case QuantityKind.Irradiance:
                        measurements.SolarIrradianceWm2 = Value(number, reading);
                        break;
                    case QuantityKind.RadiationEnergy:
                        measurements.SolarIrradianceWm2 = Value(_periodRateConverter.Convert(QuantityKind.RadiationEnergy, QuantityKind.Irradiance, number, reading.Period).Value, reading);
                        break;
                    case QuantityKind.PrecipitationRate:
                        measurements.RainRateMmH = Value(number, reading);
                        break;
                    case QuantityKind.PrecipitationAccumulation:
                        measurements.RainRateMmH = Value(_periodRateConverter.Convert(QuantityKind.PrecipitationAccumulation, QuantityKind.PrecipitationRate, number, reading.Period).Value, reading);
                        break;
                }
            }
            catch (QuantityConversionException)
            {
                omitted.Add(new OmittedReading(kind, OmissionReason.Unrepresentable));
            }
        }

        var exportedValues = new List<MeasurementValue>();
        if (measurements.TemperatureC is not null) exportedValues.Add(measurements.TemperatureC);
        if (measurements.PressureHpa is not null) exportedValues.Add(measurements.PressureHpa);
        if (measurements.PressureMmHg is not null) exportedValues.Add(measurements.PressureMmHg);
        if (measurements.HumidityPercent is not null) exportedValues.Add(measurements.HumidityPercent);
        if (measurements.WindDirectionDeg is not null) exportedValues.Add(measurements.WindDirectionDeg);
        if (measurements.WindSpeedKmh is not null) exportedValues.Add(measurements.WindSpeedKmh);
        if (measurements.IlluminanceLux is not null) exportedValues.Add(measurements.IlluminanceLux);
        if (measurements.SolarIrradianceWm2 is not null) exportedValues.Add(measurements.SolarIrradianceWm2);
        if (measurements.RainRateMmH is not null) exportedValues.Add(measurements.RainRateMmH);

        if (exportedValues.Count == 0)
        {
            var source = $"{input.Provider}:{input.Location.StationId}";
            throw new ObservationDatasetRejectedException($"The observation dataset for source '{source}' has no usable reading (contract C-60, REQ-050); omitted: {string.Join(", ", omitted.Select(o => $"{o.Kind} ({o.Reason})"))}.");
        }

        var maxTime = exportedValues.Max(v => v.MeasuredAtUtc);
        var timestampQuality = exportedValues.Any(v => v.TimestampQuality == TimestampQuality.EstimatedDate)
            ? TimestampQuality.EstimatedDate
            : TimestampQuality.Source;

        var dataset = new ObservationDataset
        {
            Source = $"{input.Provider}:{input.Location.StationId}",
            Location = input.Location,
            MeasuredAtUtc = maxTime,
            RetrievedAtUtc = input.RetrievedAtUtc,
            TimestampQuality = timestampQuality,
            Measurements = measurements
        };

        return new ObservationBuildResult(dataset, omitted);
    }

    private static void ValidateReading(ObservationReading reading)
    {
        if (reading.Kind is null)
            throw new ArgumentException("The kind of the reading must not be null (contract C-57).", nameof(reading));

        if (!Enum.IsDefined(reading.Kind.Value))
            throw new ArgumentException($"The reading kind '{reading.Kind.Value}' is not defined (contract C-57).", nameof(reading));

        if (reading.Kind == QuantityKind.Pressure)
        {
            if (!reading.PressureUnit.HasValue || !Enum.IsDefined(reading.PressureUnit.Value))
                throw new ArgumentException("The pressure unit must be a defined enum value when kind is Pressure (contract C-57).", nameof(reading));
            if (!reading.PressureReference.HasValue || !Enum.IsDefined(reading.PressureReference.Value))
                throw new ArgumentException("The pressure reference must be a defined enum value when kind is Pressure (contract C-57).", nameof(reading));
        }

        if (reading.Kind == QuantityKind.MeanWindSpeed && (!reading.WindSpeedUnit.HasValue || !Enum.IsDefined(reading.WindSpeedUnit.Value)))
            throw new ArgumentException("The wind speed unit must be a defined enum value when kind is MeanWindSpeed (contract C-57).", nameof(reading));

        if (reading.PressureUnit.HasValue && !Enum.IsDefined(reading.PressureUnit.Value))
            throw new ArgumentException("The pressure unit must be a defined enum value (contract C-57).", nameof(reading));
        if (reading.PressureReference.HasValue && !Enum.IsDefined(reading.PressureReference.Value))
            throw new ArgumentException("The pressure reference must be a defined enum value (contract C-57).", nameof(reading));
        if (reading.WindSpeedUnit.HasValue && !Enum.IsDefined(reading.WindSpeedUnit.Value))
            throw new ArgumentException("The wind speed unit must be a defined enum value (contract C-57).", nameof(reading));

        if (reading.Time is not null)
        {
            if (reading.Time.UtcInstant.Kind != DateTimeKind.Utc)
                throw new ArgumentException("The time instant must be in UTC (contract C-57).", nameof(reading));
            if (reading.Time.UtcInstant.Ticks % TimeSpan.TicksPerSecond != 0)
                throw new ArgumentException("The time instant must not have sub-second precision (contract C-57).", nameof(reading));
            if (!Enum.IsDefined(reading.Time.Quality))
                throw new ArgumentException("The timestamp quality must be a defined enum value (contract C-57).", nameof(reading));

            var isEstimatedDate = reading.Time.Quality == TimestampQuality.EstimatedDate;
            if (isEstimatedDate && reading.Time.SourceTimeZone != BerlinZoneId)
                throw new ArgumentException("When the timestamp quality is EstimatedDate, the source timezone must be 'Europe/Berlin' (contract C-57).", nameof(reading));
            if (!isEstimatedDate && reading.Time.SourceTimeZone is not null && reading.Time.SourceTimeZone != BerlinZoneId)
                throw new ArgumentException("When the timestamp quality is not EstimatedDate, the source timezone must be either null or 'Europe/Berlin' (contract C-57).", nameof(reading));
        }
    }

    private static string? TargetField(QuantityKind kind) => kind switch
    {
        QuantityKind.Temperature => "temperature_c",
        QuantityKind.Pressure => "pressure",
        QuantityKind.RelativeHumidity => "humidity_percent",
        QuantityKind.WindDirection => "wind_direction_deg",
        QuantityKind.MeanWindSpeed => "wind_speed_kmh",
        QuantityKind.Illuminance => "illuminance_lux",
        QuantityKind.Irradiance or QuantityKind.RadiationEnergy => "solar_irradiance_w_m2",
        QuantityKind.PrecipitationRate or QuantityKind.PrecipitationAccumulation => "rain_rate_mm_h",
        _ => null
    };

    private static MeasurementValue Value(decimal value, ObservationReading reading)
    {
        return new MeasurementValue
        {
            Value = value,
            MeasuredAtUtc = reading.Time!.UtcInstant,
            TimestampQuality = reading.Time.Quality,
            SourceTimezone = reading.Time.SourceTimeZone,
            Quality = reading.Quality,
            Period = Period(reading.Period)
        };
    }

    private static PressureMeasurementValue PressureValue(decimal value, PressureReference reference, ObservationReading reading)
    {
        return new PressureMeasurementValue
        {
            Value = value,
            MeasuredAtUtc = reading.Time!.UtcInstant,
            TimestampQuality = reading.Time.Quality,
            SourceTimezone = reading.Time.SourceTimeZone,
            Quality = reading.Quality,
            Period = Period(reading.Period),
            PressureReference = reference
        };
    }

    private static MeasurementPeriod? Period(AveragingPeriod? period) => period is null ? null : new MeasurementPeriod { StartUtc = period.StartUtc, EndUtc = period.EndUtc };
}
