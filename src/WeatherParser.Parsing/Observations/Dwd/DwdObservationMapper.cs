using System.Globalization;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Observations.Dwd;

/// <summary>
/// Default implementation of <see cref="IDwdObservationMapper"/>; stateless.
/// </summary>
public sealed class DwdObservationMapper : IDwdObservationMapper
{
    private readonly IQuantityValidator _validator;
    private readonly ISourceTimeNormalizer _normalizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DwdObservationMapper"/> class.
    /// </summary>
    /// <param name="validator">The quantity validator to use.</param>
    /// <param name="normalizer">The time normalizer to use.</param>
    public DwdObservationMapper(IQuantityValidator validator, ISourceTimeNormalizer normalizer)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
    }

    /// <inheritdoc />
    public DwdMappingResult Map(IReadOnlyList<CdcNowEvidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        var ordered = new CdcNowEvidence?[4];
        foreach (var item in evidence)
        {
            var rank = ProductRank(item.ProductCode);
            if (rank < 0)
            {
                throw new ArgumentException("Only the CDC products tu, ff, rr and sd are supported (contract OC-03).", nameof(evidence));
            }

            if (ordered[rank] is not null)
            {
                throw new ArgumentException("Each CDC product may be supplied only once (contract OC-03).", nameof(evidence));
            }

            ordered[rank] = item;
        }

        var readings = new List<ObservationReading>();
        var omissions = new List<DwdMappingOmission>();
        foreach (var item in ordered)
        {
            if (item is null)
            {
                continue;
            }

            foreach (var (column, kind, hasPeriod, factor) in Columns(item.ProductCode))
            {
                var (reading, omission) = MapColumn(item, column, kind, hasPeriod, factor);
                if (reading is not null)
                {
                    readings.Add(reading);
                }

                if (omission is not null)
                {
                    omissions.Add(omission);
                }
            }
        }

        return new DwdMappingResult(readings, omissions);
    }

    private static int ProductRank(string productCode) => productCode switch { "tu" => 0, "ff" => 1, "rr" => 2, "sd" => 3, _ => -1 };

    /// <summary>
    /// The mapped columns of a product (contract OC-05); every other column is not exported.
    /// The period is 10 minutes ending at the measurement time for wind speed, wind direction, precipitation and radiation energy.
    /// The factor converts J/cm2 to J/m2 for radiation energy (10000).
    /// </summary>
    /// <param name="productCode">The product code.</param>
    /// <returns>The mapped columns, their kinds, whether they have a period, and the conversion factor.</returns>
    private static (string Column, QuantityKind Kind, bool HasPeriod, decimal Factor)[] Columns(string productCode) => productCode switch { "tu" => [("TT_10", QuantityKind.Temperature, false, 1m), ("RF_10", QuantityKind.RelativeHumidity, false, 1m), ("PP_10", QuantityKind.Pressure, false, 1m)], "ff" => [("FF_10", QuantityKind.MeanWindSpeed, true, 1m), ("DD_10", QuantityKind.WindDirection, true, 1m)], "rr" => [("RWS_10", QuantityKind.PrecipitationAccumulation, true, 1m)], _ => [("GS_10", QuantityKind.RadiationEnergy, true, 10000m)] };

    /// <summary>
    /// Chooses the newest row whose value the validator accepts; -999 in any spelling is missing; when no row is accepted, the newest non-missing value is passed on with its row so that the builder records the rejection reason, and when every row is missing a reading without value is passed on (contract OC-04, SC-03).
    /// For DD_10 a row is excluded when FF_10 of the same row is 0 (CalmWind) or DD_10 is 0 (ZeroDirection); when no row yields a reading and the newest non-missing row is excluded, no reading and one omission with that reason are returned and the raw value is never passed on (contract OC-06).
    /// </summary>
    /// <param name="item">The evidence item.</param>
    /// <param name="column">The column to map.</param>
    /// <param name="kind">The kind of quantity to map to.</param>
    /// <param name="hasPeriod">Whether the reading has a period.</param>
    /// <param name="factor">The factor to apply to the value.</param>
    /// <returns>The mapped observation reading, or a default reading if no valid value was found.</returns>
    private (ObservationReading? Reading, DwdMappingOmission? Omission) MapColumn(CdcNowEvidence item, string column, QuantityKind kind, bool hasPeriod, decimal factor)
    {
        CdcNowEvidenceRow? fallbackRow = null;
        double? fallbackValue = null;
        DwdMappingOmissionReason? newestExclusion = null;
        var newestSeen = false;

        foreach (var row in item.Rows.OrderByDescending(r => r.MeasuredAtUtc))
        {
            if (!row.Values.TryGetValue(column, out var token) || !decimal.TryParse(token, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number))
            {
                continue;
            }

            if (number == -999m)
            {
                continue;
            }

            var exclusion = kind == QuantityKind.WindDirection ? DirectionExclusion(row, number) : null;
            if (exclusion is not null)
            {
                if (!newestSeen)
                {
                    newestSeen = true;
                    newestExclusion = exclusion;
                }

                continue;
            }

            var candidate = (double)(number * factor);
            if (_validator.Validate(kind, candidate).IsValid)
            {
                return (Reading(kind, row, candidate, hasPeriod), null);
            }

            if (!newestSeen)
            {
                newestSeen = true;
                fallbackRow = row;
                fallbackValue = candidate;
            }
        }

        if (newestExclusion is not null)
        {
            return (null, new DwdMappingOmission(kind, newestExclusion.Value));
        }

        return (Reading(kind, fallbackRow, fallbackValue, hasPeriod), null);
    }

    /// <summary>
    /// The OC-06 exclusion of a wind-direction row: CalmWind when the mean wind speed of the same row is 0, which wins, otherwise ZeroDirection when the direction is 0; a missing or unparsable speed never excludes the direction.
    /// </summary>
    private static DwdMappingOmissionReason? DirectionExclusion(CdcNowEvidenceRow row, decimal direction)
    {
        if (row.Values.TryGetValue("FF_10", out var speedToken)
            && decimal.TryParse(speedToken, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var speed)
            && speed == 0m)
        {
            return DwdMappingOmissionReason.CalmWind;
        }

        return direction == 0m ? DwdMappingOmissionReason.ZeroDirection : null;
    }

    /// <summary>
    /// Creates an observation reading with the specified parameters.
    /// </summary>
    /// <param name="kind">The kind of quantity.</param>
    /// <param name="row">The source row, or null if no valid value was found.</param>
    /// <param name="value">The value, or null if no valid value was found.</param>
    /// <param name="hasPeriod">Whether the reading has a period.</param>
    /// <returns>The observation reading.</returns>
    private ObservationReading Reading(QuantityKind kind, CdcNowEvidenceRow? row, double? value, bool hasPeriod) => new()
    {
        Kind = kind,
        Value = value,
        Time = row is null ? null : _normalizer.NormalizeComplete(new DateTimeOffset(row.MeasuredAtUtc)),
        Period = hasPeriod && row is not null ? new AveragingPeriod(row.MeasuredAtUtc.AddMinutes(-10), row.MeasuredAtUtc) : null,
        PressureUnit = kind == QuantityKind.Pressure ? Quantities.PressureUnit.Hectopascal : null,
        PressureReference = kind == QuantityKind.Pressure ? Quantities.PressureReference.Sensor : null,
        WindSpeedUnit = kind == QuantityKind.MeanWindSpeed ? Observations.WindSpeedUnit.MetresPerSecond : null,
        Quality = row is null || row.QualityLevel.Length == 0 ? null : row.QualityLevel
    };
}
