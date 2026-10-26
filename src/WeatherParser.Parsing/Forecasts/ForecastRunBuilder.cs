using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Forecasts;

/// <summary>
/// Builds forecast runs from input series.
/// </summary>
public sealed class ForecastRunBuilder : IForecastRunBuilder
{
    private const string BerlinZoneId = "Europe/Berlin";

    private readonly IQuantityValidator _validator;
    private readonly IOutputRounding _rounding;
    private readonly IPressureConverter _pressureConverter;
    private readonly IPeriodRateConverter _periodRateConverter;
    private readonly IWindSpeedConverter _windSpeedConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="ForecastRunBuilder"/> class.
    /// </summary>
    /// <param name="validator">The quantity validator.</param>
    /// <param name="rounding">The output rounding service.</param>
    /// <param name="pressureConverter">The pressure converter.</param>
    /// <param name="periodRateConverter">The period rate converter.</param>
    /// <param name="windSpeedConverter">The wind speed converter.</param>
    public ForecastRunBuilder(
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
    public ForecastRunBuildResult Build(ForecastRunInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Product is null || !Enum.IsDefined(input.Product.Value))
            throw new ArgumentException("The product must be a defined enum value (contract C-76).", nameof(input));
        if (!IsValidMosmixId(input.MosmixId))
            throw new ArgumentException("The MOSMIX ID must not be null, empty, contain whitespace or colons (contract C-76).", nameof(input));
        if (input.Location is null)
            throw new ArgumentException("The location must not be null (contract C-76).", nameof(input));
        if (input.Timeline is null)
            throw new ArgumentException("The timeline must not be null (contract C-76).", nameof(input));
        if (input.Series is null)
            throw new ArgumentException("The series must not be null (contract C-76).", nameof(input));
        if (input.IssuedAtUtc.HasValue && (input.IssuedAtUtc.Value.Kind != DateTimeKind.Utc || input.IssuedAtUtc.Value.Ticks % TimeSpan.TicksPerSecond != 0))
            throw new ArgumentException("The issued time, when present, must be in UTC and have whole seconds (contract C-76).", nameof(input));
        if (input.IssuedAtUtc is null)
            throw new ForecastRunRejectedException("The forecast run has no issued time (REQ-107).");

        foreach (var instant in input.Timeline)
        {
            if (instant.Kind != DateTimeKind.Utc || instant.Ticks % TimeSpan.TicksPerSecond != 0)
                throw new ArgumentException("All timeline instants must be in UTC and have whole seconds (contract C-76).", nameof(input));
        }

        // Step 3: Validate series provenance
        foreach (var series in input.Series)
        {
            if (series is null)
                throw new ArgumentException("The series must not contain null entries (contract C-76).", nameof(input));

            if (series.Kind is null || !Enum.IsDefined(series.Kind.Value))
                throw new ArgumentException("The series kind must be a defined enum value (contract C-76).", nameof(input));
            if (series.Values is null)
                throw new ArgumentException("The series values must not be null (contract C-76).", nameof(input));

            // Check for missing provenance
            if (series.Product is null || series.MosmixId is null || series.IssuedAtUtc is null)
                throw new ArgumentException("The series must have complete provenance (contract C-76).", nameof(input));
            if (!Enum.IsDefined(series.Product!.Value))
                throw new ArgumentException("The series product must be a defined enum value (contract C-76).", nameof(input));

            if (!IsValidMosmixId(series.MosmixId))
                throw new ArgumentException("The series MOSMIX ID must be non-empty without whitespace or ':' (contract C-69, C-76).", nameof(input));

            if (series.IssuedAtUtc.HasValue && (series.IssuedAtUtc.Value.Kind != DateTimeKind.Utc || series.IssuedAtUtc.Value.Ticks % TimeSpan.TicksPerSecond != 0))
                throw new ArgumentException("The series issued time must be in UTC and have whole seconds (contract C-76).", nameof(input));

            if (series.Kind == QuantityKind.Pressure && (!series.PressureUnit.HasValue || !Enum.IsDefined(series.PressureUnit.Value)))
                throw new ArgumentException("The pressure unit must be a defined enum value when kind is Pressure (contract C-76).", nameof(input));
            if (series.Kind == QuantityKind.Pressure && (!series.PressureReference.HasValue || !Enum.IsDefined(series.PressureReference.Value)))
                throw new ArgumentException("The pressure reference must be a defined enum value when kind is Pressure (contract C-76).", nameof(input));

            if (series.Kind == QuantityKind.MeanWindSpeed && (!series.WindSpeedUnit.HasValue || !Enum.IsDefined(series.WindSpeedUnit.Value)))
                throw new ArgumentException("The wind speed unit must be a defined enum value when kind is MeanWindSpeed (contract C-76).", nameof(input));

            if (series.PressureUnit.HasValue && !Enum.IsDefined(series.PressureUnit.Value))
                throw new ArgumentException("The pressure unit must be a defined enum value (contract C-76).", nameof(input));
            if (series.PressureReference.HasValue && !Enum.IsDefined(series.PressureReference.Value))
                throw new ArgumentException("The pressure reference must be a defined enum value (contract C-76).", nameof(input));
            if (series.WindSpeedUnit.HasValue && !Enum.IsDefined(series.WindSpeedUnit.Value))
                throw new ArgumentException("The wind speed unit must be a defined enum value (contract C-76).", nameof(input));

            var isInterval = series.Kind is QuantityKind.Irradiance or QuantityKind.RadiationEnergy or QuantityKind.PrecipitationRate or QuantityKind.PrecipitationAccumulation;
            if (isInterval && series.Periods is null)
                throw new ArgumentException("The periods must not be null for interval kinds (contract C-76).", nameof(input));
            if (!isInterval && series.Periods is not null)
                throw new ArgumentException("The periods must be null for non-interval kinds (contract C-76).", nameof(input));
        }

        // Step 4: Validate timeline and series consistency
        var distinctTimelineCount = input.Timeline.Distinct().Count();
        if (distinctTimelineCount != input.Timeline.Count)
            throw new ForecastRunRejectedException("The timeline must not contain duplicate instants (contract C-76).");

        var product = input.Product.Value;
        var issuedAt = input.IssuedAtUtc.Value;
        var mosmixId = input.MosmixId;
        var location = input.Location;

        foreach (var series in input.Series)
        {
            if (series is null)
                continue;

            // Check provenance consistency
            if (series.Product != product)
                throw new ForecastRunRejectedException($"The forecast run has inconsistent product; expected '{product}', got series with different product (contract C-76).");
            if (!string.Equals(series.MosmixId, mosmixId, StringComparison.Ordinal))
                throw new ForecastRunRejectedException($"The forecast run has inconsistent MOSMIX ID; expected '{mosmixId}', got series with different MOSMIX ID (contract C-76).");
            if (series.IssuedAtUtc!.Value != issuedAt)
                throw new ForecastRunRejectedException($"The forecast run has inconsistent issued time; expected '{issuedAt}', got series with different issued time (contract C-76).");

            // Check lengths
            if (series.Values!.Count != input.Timeline.Count)
                throw new ForecastRunRejectedException("The timeline and series values must have the same count (contract C-76).");

            if (series.Periods is not null && series.Periods.Count != input.Timeline.Count)
                throw new ForecastRunRejectedException("The timeline and series periods must have the same count (contract C-76).");
        }

        var targetFields = new HashSet<string>();
        foreach (var series in input.Series)
        {
            if (series is null)
                continue;

            var kind = series.Kind!.Value;
            var field = TargetField(kind);
            if (field is not null && !targetFields.Add(field))
            {
                throw new ArgumentException($"The field '{field}' appears more than once in the series (contract C-76).", nameof(input));
            }
        }

        var steps = input.Timeline.Select(t => (ValidAt: t, Values: new ForecastValues())).ToList();
        var omitted = new List<OmittedForecastValue>();

        for (var i = 0; i < input.Series.Count; i++)
        {
            var series = input.Series[i];
            if (series is null)
                continue;

            var kind = series.Kind!.Value;
            var field = TargetField(kind);
            if (field is null)
            {
                for (var j = 0; j < input.Timeline.Count; j++)
                {
                    omitted.Add(new OmittedForecastValue(input.Timeline[j], kind, OmissionReason.NotExportable));
                }
                continue;
            }

            for (var j = 0; j < series.Values!.Count; j++)
            {
                var validAt = input.Timeline[j];
                var value = series.Values[j];

                var validated = _validator.Validate(kind, value);
                if (!validated.IsValid)
                {
                    var reason = validated.RejectionReason switch
                    {
                        QuantityRejectionReason.Missing => OmissionReason.Missing,
                        QuantityRejectionReason.NonFinite => OmissionReason.NonFinite,
                        _ => OmissionReason.OutOfRange
                    };
                    omitted.Add(new OmittedForecastValue(validAt, kind, reason));
                    continue;
                }

                var number = validated.Value!.Value;

                if (kind is QuantityKind.Irradiance or QuantityKind.RadiationEnergy or QuantityKind.PrecipitationRate or QuantityKind.PrecipitationAccumulation)
                {
                    if (series.Periods![j] is null)
                    {
                        omitted.Add(new OmittedForecastValue(validAt, kind, OmissionReason.MissingPeriod));
                        continue;
                    }
                }

                try
                {
                    switch (kind)
                    {
                        case QuantityKind.Temperature:
                            steps[j].Values.TemperatureC = _rounding.Round(QuantityKind.Temperature, number);
                            break;
                        case QuantityKind.Pressure:
                            var p = series.PressureUnit == PressureUnit.Hectopascal
                                ? _pressureConverter.FromHpa(number, series.PressureReference!.Value)
                                : _pressureConverter.FromMmHg(number, series.PressureReference!.Value);
                            steps[j].Values.PressureHpa = p.Hpa;
                            steps[j].Values.PressureMmHg = p.MmHg;
                            steps[j].Values.PressureReference = p.Reference;
                            break;
                        case QuantityKind.RelativeHumidity:
                            steps[j].Values.HumidityPercent = _rounding.Round(QuantityKind.RelativeHumidity, number);
                            break;
                        case QuantityKind.WindDirection:
                            steps[j].Values.WindDirectionDeg = number;
                            break;
                        case QuantityKind.MeanWindSpeed:
                            var kmh = series.WindSpeedUnit == WindSpeedUnit.MetresPerSecond
                                ? _windSpeedConverter.ToKilometresPerHour(number)
                                : number;
                            steps[j].Values.WindSpeedKmh = _rounding.Round(QuantityKind.MeanWindSpeed, kmh);
                            break;
                        case QuantityKind.Illuminance:
                            steps[j].Values.IlluminanceLux = number;
                            break;
                        case QuantityKind.Irradiance:
                            steps[j].Values.SolarIrradianceWm2 = Interval(number, series.Periods![j]!);
                            break;
                        case QuantityKind.RadiationEnergy:
                            steps[j].Values.SolarIrradianceWm2 = Interval(_periodRateConverter.Convert(QuantityKind.RadiationEnergy, QuantityKind.Irradiance, number, series.Periods![j]).Value, series.Periods![j]!);
                            break;
                        case QuantityKind.PrecipitationRate:
                            steps[j].Values.RainRateMmH = Interval(number, series.Periods![j]!);
                            break;
                        case QuantityKind.PrecipitationAccumulation:
                            steps[j].Values.RainRateMmH = Interval(_periodRateConverter.Convert(QuantityKind.PrecipitationAccumulation, QuantityKind.PrecipitationRate, number, series.Periods![j]).Value, series.Periods![j]!);
                            break;
                    }
                }
                catch (QuantityConversionException)
                {
                    omitted.Add(new OmittedForecastValue(validAt, kind, OmissionReason.Unrepresentable));
                }
            }
        }

        var keptSteps = steps.Where(s => s.Values.TemperatureC.HasValue || s.Values.PressureHpa.HasValue || s.Values.PressureMmHg.HasValue || s.Values.PressureReference.HasValue || s.Values.HumidityPercent.HasValue || s.Values.WindDirectionDeg.HasValue || s.Values.WindSpeedKmh.HasValue || s.Values.IlluminanceLux.HasValue || s.Values.SolarIrradianceWm2 is not null || s.Values.RainRateMmH is not null).OrderBy(s => s.ValidAt).ToList();
        if (keptSteps.Count == 0)
        {
            var source = $"{(product == MosmixProduct.MosmixS ? "dwd-mosmix-s:" : "dwd-mosmix-l:")}{mosmixId}";
            throw new ForecastRunRejectedException($"The forecast run for source '{source}' has no usable values (contract C-76, REQ-107).", omitted);
        }

        var run = new ForecastRun
        {
            Source = $"{(product == MosmixProduct.MosmixS ? "dwd-mosmix-s:" : "dwd-mosmix-l:")}{mosmixId}",
            MosmixId = mosmixId,
            Location = location,
            Product = product,
            IssuedAtUtc = issuedAt,
            RetrievedAtUtc = input.RetrievedAtUtc
        };

        foreach (var step in keptSteps)
        {
            run.Forecasts.Add(new ForecastStep { ValidAtUtc = step.ValidAt, Values = step.Values });
        }

        return new ForecastRunBuildResult(run, omitted);
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

    private static IntervalValue Interval(decimal value, AveragingPeriod period) => new()
    {
        Value = value,
        PeriodStartUtc = period.StartUtc,
        PeriodEndUtc = period.EndUtc,
        Aggregation = IntervalAggregation.Mean
    };

    /// <summary>
    /// Checks the MOSMIX ID syntax of contract C-69: non-empty, without whitespace and without ':'.
    /// </summary>
    private static bool IsValidMosmixId([NotNullWhen(true)] string? id) =>
        !string.IsNullOrEmpty(id) && !id.Any(char.IsWhiteSpace) && !id.Contains(':');}
