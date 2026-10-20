using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

/// <summary>
/// Builds one rich, valid instance of each of the five export roots and serializes it with its concrete static type.
/// </summary>
internal static class AllRootsTestData
{
    public const string SpecialText = "Großenkneten <A&B> \"Q\" 'S'";

    public static readonly string[] RootNames = { "export_document", "cdc_station_list", "mosmix_location_list", "observation_dataset", "forecast_run" };

    public static IEnumerable<object[]> Roots => RootNames.Select(name => new object[] { name });

    public static ObservationDataset RichDataset()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Location.Name = SpecialText;
        dataset.Location.AltitudeM = null;
        dataset.Measurements.TemperatureC = ObservationTestData.Value(0.0m, ObservationTestData.MeasuredAtUtc);
        return dataset;
    }

    private static ObservationDataset SecondDataset()
    {
        var dataset = ObservationTestData.ValidDataset();
        dataset.Source = "dwd:01766";
        dataset.Location.StationId = "01766";
        return dataset;
    }

    public static ExportDocument RichDocument() => new()
    {
        Observations =
        {
            RichDataset(),
            SecondDataset()
        },
        ForecastRuns =
        {
            ForecastTestData.ValidRun()
        },
        Errors =
        {
            new ExportError
            {
                ErrorId = "DWD-RETRIEVAL-FAILED",
                Message = "Station <Ä&Ö> \"02000\" failed.",
                Source = "dwd:02000"
            }
        }
    };

    public static object Build(string rootName) => rootName switch
    {
        "export_document" => RichDocument(),
        "cdc_station_list" => ListTestData.ValidCdcList(),
        "mosmix_location_list" => ListTestData.ValidMosmixList(),
        "observation_dataset" => RichDataset(),
        "forecast_run" => ForecastTestData.ValidRun(),
        _ => throw new ArgumentException(rootName, nameof(rootName))
    };

    public static Task WriteJsonAsync(object root, IJsonExportSerializer serializer, Stream destination, CancellationToken cancellationToken) => root switch
    {
        ExportDocument d => serializer.SerializeAsync(d, destination, cancellationToken),
        CdcStationList l => serializer.SerializeAsync(l, destination, cancellationToken),
        MosmixLocationList l => serializer.SerializeAsync(l, destination, cancellationToken),
        ObservationDataset o => serializer.SerializeAsync(o, destination, cancellationToken),
        ForecastRun f => serializer.SerializeAsync(f, destination, cancellationToken),
        _ => throw new ArgumentException($"Unexpected root type {root.GetType().FullName}", nameof(root))
    };

    public static Task WriteXmlAsync(object root, IXmlExportSerializer serializer, Stream destination, CancellationToken cancellationToken) => root switch
    {
        ExportDocument d => serializer.SerializeAsync(d, destination, cancellationToken),
        CdcStationList l => serializer.SerializeAsync(l, destination, cancellationToken),
        MosmixLocationList l => serializer.SerializeAsync(l, destination, cancellationToken),
        ObservationDataset o => serializer.SerializeAsync(o, destination, cancellationToken),
        ForecastRun f => serializer.SerializeAsync(f, destination, cancellationToken),
        _ => throw new ArgumentException($"Unexpected root type {root.GetType().FullName}", nameof(root))
    };

    public static async Task<byte[]> JsonBytesAsync(object root, IJsonExportSerializer serializer)
    {
        using var stream = new MemoryStream();
        await WriteJsonAsync(root, serializer, stream, CancellationToken.None);
        return stream.ToArray();
    }

    public static async Task<byte[]> XmlBytesAsync(object root, IXmlExportSerializer serializer)
    {
        using var stream = new MemoryStream();
        await WriteXmlAsync(root, serializer, stream, CancellationToken.None);
        return stream.ToArray();
    }
}
