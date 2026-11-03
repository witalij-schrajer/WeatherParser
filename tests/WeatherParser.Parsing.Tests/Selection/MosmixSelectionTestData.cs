using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;
using WeatherParser.Parsing.Selection;

namespace WeatherParser.Parsing.Tests.Selection;

internal static class MosmixSelectionTestData
{
    public static readonly DateTimeOffset Reference = new(2026, 10, 21, 12, 34, 56, 789, TimeSpan.Zero);

    public static SearchParameters Parameters() => new() { Latitude = 50.5m, Longitude = 9.5m, AltitudeM = 300m, MaxDistanceKm = 25m, MaxAltitudeDifferenceM = 200m };

    public static MosmixLocationRecord Record(string id, decimal? altitudeM, decimal latitude) => new(id, "Name " + id, latitude, 9.25m, altitudeM, 3);

    public static MosmixSEvidenceIndex Index(DateTime downloadedAtUtc) => new("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz", downloadedAtUtc, new DateTime(2026, 10, 21, 9, 0, 0, DateTimeKind.Utc), new string('a', 64), 1024, new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal) { ["X123A"] = new[] { "TTT" } });

    public static MosmixSEvidenceIndex FreshIndex() => Index(new DateTime(2026, 10, 21, 10, 0, 0, DateTimeKind.Utc));

    public static MosmixCatalogEndpoints Options() => new() { MaxEvidenceCandidates = 3, SelectionDeadline = TimeSpan.FromSeconds(120), MosmixSEvidenceMaxAge = TimeSpan.FromHours(6) };

    public static MosmixLocationSelector Create(MosmixCatalogEndpoints options, IMosmixCatalogSource source, IMosmixEvidenceCache cache, IMosmixAvailabilityResolver resolver, ISearchAreaFilter filter, IListEntryOrdering ordering, TimeProvider clock, ILogger<MosmixLocationSelector> logger) => new(options, source, cache, resolver, new SearchParameterFactory(), filter, ordering, clock, logger);

    public static List<MosmixProductAvailability> Items(string marker) => new() { new MosmixProductAvailability { Product = MosmixProduct.MosmixS, Availability = AvailabilityState.Available, AvailableForecastElements = new List<string> { marker } }, new MosmixProductAvailability { Product = MosmixProduct.MosmixL, Availability = AvailabilityState.Unavailable, AvailableForecastElements = new List<string>() } };
}
