using WeatherParser.Parsing.Export;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Forecasts.Mosmix;

using WeatherParser.Parsing.Forecasts;
namespace WeatherParser.Parsing.Tests.Live;

internal sealed record LiveCheckResult(
    MosmixSEvidenceIndex? Index,
    MosmixLocationList MosmixList,
    CdcStationList CdcList,
    byte[] MosmixJson,
    byte[] MosmixXml,
    byte[] CdcJson,
    byte[] CdcXml,
    IReadOnlyList<string> MosmixSchemaEvents,
    IReadOnlyList<string> CdcSchemaEvents,
    IReadOnlyList<LiveRequest> Requests,
    IReadOnlyList<(LogLevel Level, string? EventName, string Message)> Notices);

internal sealed record LiveObservationResult(
    ObservationDataset Dataset,
    byte[] Json,
    byte[] Xml,
    IReadOnlyList<string> SchemaEvents,
    IReadOnlyList<LiveRequest> Requests,
    IReadOnlyList<(LogLevel Level, string? EventName, string Message)> Notices);

internal sealed record LiveOsthessenResult(IReadOnlyList<ObservationDataset> Datasets, IReadOnlyList<OsthessenWetterStationFailure> StationFailures, string? ErrorId, IReadOnlyList<byte[]> Json, IReadOnlyList<byte[]> Xml, IReadOnlyList<string> SchemaEvents, IReadOnlyList<LiveRequest> Requests, IReadOnlyList<(LogLevel Level, string? EventName, string Message)> Notices);

internal sealed record LiveMosmixResult(
    MosmixProduct Product,
    string MosmixId,
    ForecastRun? Run,
    MosmixRunFreshness? Freshness,
    TimeSpan? RunAge,
    IReadOnlyList<OmittedForecastValue> OmittedValues,
    IReadOnlyList<string> NotExportedElements,
    IReadOnlyList<string> AbsentElements,
    string? ErrorId,
    byte[]? Json,
    byte[]? Xml,
    IReadOnlyList<string> SchemaEvents,
    IReadOnlyList<LiveRequest> Requests,
    IReadOnlyList<(LogLevel Level, string? EventName, string Message)> Notices,
    TimeSpan Duration,
    long PeakWorkingSetBytes);
