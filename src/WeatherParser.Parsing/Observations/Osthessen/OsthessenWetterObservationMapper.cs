using System;
using System.Collections.Generic;
using System.Linq;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Time;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Default implementation of <see cref="IOsthessenWetterObservationMapper"/>; stateless apart from the injected services.
/// </summary>
public sealed class OsthessenWetterObservationMapper : IOsthessenWetterObservationMapper
{
    private readonly IOsthessenWetterStationIdentity _identity;
    private readonly IOsthessenWetterStationMetadataLookup _metadata;
    private readonly IOsthessenWetterValueReader _values;
    private readonly IOsthessenWetterTimeReader _times;

    /// <summary>
    /// Initializes a new instance with the services of the four mapping concerns.
    /// </summary>
    /// <param name="identity">The station identity service.</param>
    /// <param name="metadata">The station metadata lookup service.</param>
    /// <param name="values">The value reader service.</param>
    /// <param name="times">The time reader service.</param>
    public OsthessenWetterObservationMapper(
        IOsthessenWetterStationIdentity identity,
        IOsthessenWetterStationMetadataLookup metadata,
        IOsthessenWetterValueReader values,
        IOsthessenWetterTimeReader times)
    {
        _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        _values = values ?? throw new ArgumentNullException(nameof(values));
        _times = times ?? throw new ArgumentNullException(nameof(times));
    }

    /// <inheritdoc />
    public IReadOnlyList<OsthessenWetterMappedStation> Map(OsthessenWetterPage page, DateTime retrievedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (retrievedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("The retrieval instant must be of kind UTC.", nameof(retrievedAtUtc));

        _identity.EnsureUnique(page.Stations.Select(s => s.Name));

        var result = new List<OsthessenWetterMappedStation>();

        foreach (var row in page.Stations)
        {
            var stationId = _identity.ToStationId(row.Name);
            var metadata = _metadata.Find(row.Name);
            ObservationLocation? location = metadata is null ? null : new ObservationLocation { StationId = stationId, Name = row.Name, Latitude = metadata.Latitude, Longitude = metadata.Longitude, AltitudeM = metadata.AltitudeM };

            if (row.Status == OsthessenWetterStationStatus.Offline)
            {
                result.Add(new OsthessenWetterMappedStation(stationId, row.Name, row.Status, location, Array.Empty<ObservationReading>(), Array.Empty<OsthessenWetterMappingNotice>()));
            }
            else
            {
                var readings = new List<ObservationReading>();
                var notices = new List<OsthessenWetterMappingNotice>();

                var overviewTime = _times.Read(row.OverviewTimeText, retrievedAtUtc);
                AddReading(readings, notices, QuantityKind.Temperature, OsthessenWetterValueColumn.Temperature, row.TemperatureText, overviewTime);
                AddReading(readings, notices, QuantityKind.RelativeHumidity, OsthessenWetterValueColumn.RelativeHumidity, row.HumidityText, overviewTime);

                if (row.WindMeanText is not null)
                {
                    var windTime = _times.Read(row.WindTimeText, retrievedAtUtc);
                    AddReading(readings, notices, QuantityKind.MeanWindSpeed, OsthessenWetterValueColumn.MeanWindSpeed10Minutes, row.WindMeanText, windTime);
                }

                result.Add(new OsthessenWetterMappedStation(stationId, row.Name, row.Status, location, readings, notices));
            }
        }

        return result;
    }

    private void AddReading(List<ObservationReading> readings, List<OsthessenWetterMappingNotice> notices, QuantityKind kind, OsthessenWetterValueColumn column, string? text, OsthessenWetterTimeValue time)
    {
        if (time.Status == OsthessenWetterTimeStatus.Unresolved)
        {
            notices.Add(new OsthessenWetterMappingNotice(OsthessenWetterNoticeIds.TimeUnresolved, kind));
            return;
        }

        var value = _values.Read(text, column);
        if (value.Status == OsthessenWetterCellValueStatus.Unreadable)
        {
            notices.Add(new OsthessenWetterMappingNotice(OsthessenWetterNoticeIds.ValueUnreadable, kind));
            return;
        }

        readings.Add(new ObservationReading
        {
            Kind = kind,
            Value = value.Value,
            Time = time.Time,
            Period = kind == QuantityKind.MeanWindSpeed && time.Time is not null ? new AveragingPeriod(time.Time.UtcInstant - TimeSpan.FromMinutes(10), time.Time.UtcInstant) : null,
            WindSpeedUnit = kind == QuantityKind.MeanWindSpeed ? WindSpeedUnit.KilometresPerHour : null
        });
    }
}
