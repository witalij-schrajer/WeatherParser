namespace WeatherParser.Parsing.Forecasts.Mosmix;

using System;
using System.Globalization;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;

/// <summary>
/// Maps a MOSMIX forecast document to a forecast run input.
/// </summary>
public sealed class MosmixElementMapper : IMosmixElementMapper
{
    private readonly IMosmixUnitScaler _scaler;

    /// <summary>
    /// Initializes a new instance of the <see cref="MosmixElementMapper"/> class.
    /// </summary>
    /// <param name="scaler">The unit scaler.</param>
    public MosmixElementMapper(IMosmixUnitScaler scaler)
    {
        _scaler = scaler ?? throw new ArgumentNullException(nameof(scaler));
    }

    /// <inheritdoc />
    public MosmixMappingResult Map(MosmixForecastDocument document, MosmixProduct product, DateTime retrievedAtUtc, bool? runAgeExceeded)
    {
        ArgumentNullException.ThrowIfNull(document);

        var seriesMap = new Dictionary<string, MosmixForecastSeries>(StringComparer.Ordinal);
        foreach (var series in document.Series)
        {
            seriesMap[series.ElementName] = series;
        }

        var absent = new List<string>();
        var forecastSeries = new List<ForecastSeries>();

        foreach (var row in MosmixElementTable.Rows)
        {
            if (seriesMap.TryGetValue(row.ElementName, out var series))
            {
                var values = new double?[series.Values.Count];
                for (var i = 0; i < series.Values.Count; i++)
                {
                    if (series.Values[i] is { } token)
                    {
                        values[i] = Scale(row.ElementName, double.Parse(token, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture));
                    }
                }

                var periods = row.PeriodHours.HasValue
                    ? new AveragingPeriod[document.Timeline.Count]
                    : null;

                if (periods != null)
                {
                    for (var i = 0; i < document.Timeline.Count; i++)
                    {
                        var t = document.Timeline[i];
                        var period = TimeSpan.FromHours(row.PeriodHours!.Value);

                        // Contract MF-13: the period [valid_at - period, valid_at] must be representable; the readers accept any valid UTC instant, so guard the subtraction instead of leaking ArgumentOutOfRangeException.
                        if (t.Ticks < period.Ticks)
                        {
                            throw new MosmixForecastFormatException(MosmixForecastErrorIds.FormatInvalid, $"The {row.ElementName} period of the time step at {t:yyyy-MM-dd'T'HH:mm:ss} UTC starts before the supported date range (contract MF-13).");
                        }

                        periods[i] = new AveragingPeriod(t - period, t);
                    }
                }

                forecastSeries.Add(new ForecastSeries
                {
                    Kind = row.Kind,
                    Values = values,
                    Periods = periods,
                    PressureUnit = row.PressureUnit,
                    PressureReference = row.PressureReference,
                    WindSpeedUnit = row.WindSpeedUnit,
                    Product = product,
                    MosmixId = document.MosmixId,
                    IssuedAtUtc = document.IssuedAtUtc
                });
            }
            else
            {
                absent.Add(row.ElementName);
            }
        }

        var tableNames = new HashSet<string>(MosmixElementTable.Rows.Select(r => r.ElementName), StringComparer.Ordinal);
        var notExported = document.Series.Select(s => s.ElementName).Where(name => !tableNames.Contains(name)).ToList();

        return new MosmixMappingResult
        {
            Input = new ForecastRunInput
            {
                Product = product,
                MosmixId = document.MosmixId,
                Location = new ForecastLocation
                {
                    Name = document.LocationName,
                    Latitude = document.Latitude,
                    Longitude = document.Longitude,
                    AltitudeM = document.AltitudeM
                },
                IssuedAtUtc = document.IssuedAtUtc,
                RetrievedAtUtc = retrievedAtUtc,
                Timeline = document.Timeline,
                Series = forecastSeries,
                RunAgeExceeded = runAgeExceeded
            },
            AbsentElements = absent,
            NotExportedElements = notExported
        };
    }

    private double Scale(string elementName, double value)
    {
        return elementName switch
        {
            "TTT" => _scaler.KelvinToCelsius(value),
            "PPPP" => _scaler.PascalToHectopascal(value),
            "Rad1h" => _scaler.KilojoulesToJoules(value),
            "RR1c" => _scaler.KilogramsPerSquareMetreToMillimetres(value),
            _ => value
        };
    }
}
