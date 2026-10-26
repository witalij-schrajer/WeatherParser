using System;
using System.Collections.Generic;
using System.Text;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Default implementation of <see cref="IOsthessenWetterStationMetadataLookup"/>; validates and copies the options at construction.
/// </summary>
public sealed class OsthessenWetterStationMetadataLookup : IOsthessenWetterStationMetadataLookup
{
    private readonly Dictionary<string, OsthessenWetterStationMetadata> _stations;

    /// <summary>
    /// Validates the station metadata entries and keeps a copy of them.
    /// </summary>
    /// <param name="options">The consumer options.</param>
    /// <exception cref="ArgumentNullException">When options is null.</exception>
    /// <exception cref="OsthessenWetterException">OSTHESSEN-OPTIONS-INVALID when the list is null or contains a null entry, an empty or whitespace-only name, a latitude outside -90 to 90, a longitude outside -180 to 180, or two names equal after Unicode normalization form C.</exception>
    public OsthessenWetterStationMetadataLookup(OsthessenWetterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Stations == null)
        {
            throw new OsthessenWetterException(OsthessenWetterException.OptionsInvalidErrorId, "The OsthessenWetter station metadata list is null (contract OW-04).");
        }

        _stations = new Dictionary<string, OsthessenWetterStationMetadata>(StringComparer.Ordinal);

        for (int i = 0; i < options.Stations.Count; i++)
        {
            var entry = options.Stations[i];

            if (entry == null)
            {
                throw new OsthessenWetterException(OsthessenWetterException.OptionsInvalidErrorId, $"OsthessenWetter station metadata entry {i} is null (contract OW-04).");
            }

            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                throw new OsthessenWetterException(OsthessenWetterException.OptionsInvalidErrorId, $"OsthessenWetter station metadata entry {i} has an empty name (contract OW-04).");
            }

            if (entry.Latitude < -90m || entry.Latitude > 90m)
            {
                throw new OsthessenWetterException(OsthessenWetterException.OptionsInvalidErrorId, $"OsthessenWetter station metadata entry {i} has a latitude outside -90 to 90 (contract OW-04, C-51).");
            }

            if (entry.Longitude < -180m || entry.Longitude > 180m)
            {
                throw new OsthessenWetterException(OsthessenWetterException.OptionsInvalidErrorId, $"OsthessenWetter station metadata entry {i} has a longitude outside -180 to 180 (contract OW-04, C-51).");
            }

            var nfcName = entry.Name.Normalize(NormalizationForm.FormC);

            if (_stations.ContainsKey(nfcName))
            {
                throw new OsthessenWetterException(OsthessenWetterException.OptionsInvalidErrorId, $"OsthessenWetter station metadata entry {i} repeats the name of an earlier entry (contract OW-04).");
            }

            _stations[nfcName] = new OsthessenWetterStationMetadata
            {
                Name = nfcName,
                Latitude = entry.Latitude,
                Longitude = entry.Longitude,
                AltitudeM = entry.AltitudeM
            };
        }
    }

    /// <inheritdoc />
    public OsthessenWetterStationMetadata? Find(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var nfcName = name.Normalize(NormalizationForm.FormC);

        return _stations.TryGetValue(nfcName, out var metadata) 
            ? new OsthessenWetterStationMetadata
            {
                Name = metadata.Name,
                Latitude = metadata.Latitude,
                Longitude = metadata.Longitude,
                AltitudeM = metadata.AltitudeM
            }
            : null;
    }
}
