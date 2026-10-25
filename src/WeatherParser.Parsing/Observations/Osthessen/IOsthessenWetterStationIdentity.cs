using System;
using System.Collections.Generic;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// Derives station IDs from OsthessenWetter overview names and detects ID collisions (contract OW-03).
/// </summary>
public interface IOsthessenWetterStationIdentity
{
    /// <summary>
    /// Returns the station ID slug of an overview station name: Unicode normalization form C, invariant lower case, the German umlauts and sharp s transliterated (ae, oe, ue, ss), every run of other characters outside a-z and 0-9 replaced by one hyphen, leading and trailing hyphens removed.
    /// </summary>
    /// <param name="name">The overview station name.</param>
    /// <exception cref="ArgumentNullException">When name is null.</exception>
    /// <exception cref="OsthessenWetterException">OSTHESSEN-FORMAT-CHANGED when the name contains no letter or digit a-z or 0-9 after these rules.</exception>
    string ToStationId(string name);

    /// <summary>
    /// Checks that all overview station names map to distinct station IDs.
    /// </summary>
    /// <param name="names">All overview station names of the page.</param>
    /// <exception cref="ArgumentNullException">When names is null.</exception>
    /// <exception cref="OsthessenWetterException">OSTHESSEN-STATION-ID-COLLISION when two names, including two equal names, map to the same station ID; OSTHESSEN-FORMAT-CHANGED as in ToStationId.</exception>
    void EnsureUnique(IEnumerable<string> names);
}
