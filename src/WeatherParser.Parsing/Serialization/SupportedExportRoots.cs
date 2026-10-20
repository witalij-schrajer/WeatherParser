using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Serialization;

/// <summary>
/// The fixed set of export root types that both serializers accept (contract C-88).
/// </summary>
internal static class SupportedExportRoots
{
    /// <summary>
    /// True exactly for the five sealed root types of contract C-88; no static collection holds them (contract C-109).
    /// </summary>
    private static bool IsSupported(Type rootType) => rootType == typeof(ExportDocument) || rootType == typeof(CdcStationList) || rootType == typeof(MosmixLocationList) || rootType == typeof(ObservationDataset) || rootType == typeof(ForecastRun);

    /// <summary>
    /// Throws <see cref="ExportSerializationException"/> when <paramref name="rootType"/> is not exactly one of the five root types.
    /// </summary>
    /// <param name="rootType">The type to check.</param>
    public static void ThrowIfUnsupported(Type rootType)
    {
        if (!IsSupported(rootType))
        {
            throw new ExportSerializationException(
                $"The type '{rootType.Name}' is not a supported export root; supported roots are ExportDocument, CdcStationList, MosmixLocationList, ObservationDataset and ForecastRun (contract C-88).");
        }
    }
}
