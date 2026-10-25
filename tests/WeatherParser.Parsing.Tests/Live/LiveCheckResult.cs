using WeatherParser.Parsing.Export;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;

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
