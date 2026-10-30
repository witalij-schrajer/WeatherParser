namespace WeatherParser.Cli.Output;

using System.Collections.Generic;
using WeatherParser.Parsing.Export;

/// <summary>
/// The fixed English <see cref="ExportError"/> texts of the example CLI (CL-10).
/// </summary>
public sealed class ExportErrorMessages : IExportErrorMessages
{
    private static readonly IReadOnlyDictionary<string, string> Texts = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["STATION-UNKNOWN"] = "The DWD station is not in the station catalog.",
        ["STATION-METADATA-CONFLICT"] = "The DWD station catalogs give conflicting station metadata.",
        ["CATALOG-RETRIEVAL-FAILED"] = "A source file could not be retrieved.",
        ["CATALOG-TIMEOUT"] = "Retrieving a source file timed out.",
        ["CATALOG-TOO-LARGE"] = "A source file exceeded its size limit.",
        ["CATALOG-FORMAT-INVALID"] = "A source file has an unexpected format.",
        ["OBSERVATION-DEADLINE-EXCEEDED"] = "The observation retrieval exceeded its deadline.",
        ["MOSMIX-FORMAT-INVALID"] = "The MOSMIX forecast file has an unexpected format.",
        ["MOSMIX-ISSUE-TIME-MISSING"] = "The MOSMIX forecast file has no issue time.",
        ["MOSMIX-LOCATION-MISMATCH"] = "The MOSMIX forecast file describes a different location.",
        ["MOSMIX-ID-UNKNOWN"] = "The MOSMIX location is not in the forecast file.",
        ["MOSMIX-TOO-LARGE"] = "The MOSMIX forecast file exceeded its size limit.",
        ["MOSMIX-RETRIEVAL-FAILED"] = "The MOSMIX forecast file could not be retrieved.",
        ["MOSMIX-DEADLINE-EXCEEDED"] = "The MOSMIX forecast parse exceeded its deadline.",
        ["MOSMIX-NO-USABLE-VALUE"] = "The MOSMIX forecast has no usable value for the location.",
        ["OSTHESSEN-STATION-OFFLINE"] = "The OsthessenWetter station is reported offline.",
        ["OSTHESSEN-STATION-METADATA-MISSING"] = "No metadata is configured for the OsthessenWetter station.",
        ["OSTHESSEN-STATION-NO-READING"] = "The OsthessenWetter station shows no usable reading.",
        ["OSTHESSEN-FORMAT-CHANGED"] = "The OsthessenWetter page deviates from the supported revision."
    };

    private const string Generic = "The requested item failed; the error ID names the cause.";

    /// <inheritdoc/>
    public string For(string errorId)
    {
        ArgumentNullException.ThrowIfNull(errorId);
        return Texts.TryGetValue(errorId, out var text) ? text : Generic;
    }
}
