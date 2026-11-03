using System.Collections.Generic;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Observations.Osthessen;

/// <summary>
/// The datasets and station failures of one OsthessenWetter parse, both in the overview order of the page (contract OW-08).
/// </summary>
public sealed record OsthessenWetterParseResult(IReadOnlyList<ObservationDataset> Datasets, IReadOnlyList<OsthessenWetterStationFailure> StationFailures);
