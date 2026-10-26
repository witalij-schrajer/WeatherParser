using System;
using WeatherParser.Parsing.Observations.Osthessen;

namespace WeatherParser.Parsing.Tests.Live;

// Live check only: returns placeholder coordinates for every station name, so the real page can be parsed without consumer metadata; the coordinates are not real station locations and are never published (contract OW-04, OW-15).
internal sealed class PlaceholderMetadataLookup : IOsthessenWetterStationMetadataLookup
{
    public OsthessenWetterStationMetadata? Find(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return new OsthessenWetterStationMetadata { Name = name, Latitude = 50.5m, Longitude = 9.7m };
    }
}
