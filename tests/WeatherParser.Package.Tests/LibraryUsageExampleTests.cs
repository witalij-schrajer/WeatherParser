using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Observations.Dwd;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Selection;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Mqtt;
using MyWeatherApp;

namespace WeatherParser.Package.Tests;

/// <summary>
/// Tests the library usage example from docs/LIBRARY_USAGE.md.
/// </summary>
[TestClass]
public sealed class LibraryUsageExampleTests
{
    private ConsumerHttpHandler _handler = null!;
    private ConsumerClock _clock = null!;
    private string _directory = null!;
    private ServiceProvider _provider = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "wp-usage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _handler = new ConsumerHttpHandler();
        _clock = new ConsumerClock(new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero));
        _provider = new ServiceCollection()
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddMyWeatherParsing(new HttpClient(_handler), _clock, _directory)
            .AddSingleton<IDwdJsonExporter, DwdJsonExporter>()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [TestCleanup]
    public void Cleanup()
    {
        _provider.Dispose();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that all entry points can be resolved without making any requests.
    /// </summary>
    [TestMethod]
    public void Example_BuildAndResolveEveryEntryPoint_MakesNoRequest()
    {
        _provider.GetRequiredService<IDwdJsonExporter>().Should().NotBeNull();
        _provider.GetRequiredService<IDwdObservationParser>().Should().NotBeNull();
        _provider.GetRequiredService<IOsthessenWetterParser>().Should().NotBeNull();
        _provider.GetRequiredService<IMosmixLParser>().Should().NotBeNull();
        _provider.GetRequiredService<IMosmixSParser>().Should().NotBeNull();
        _provider.GetRequiredService<IMqttMessageSerializer>().Should().NotBeNull();
        _provider.GetRequiredService<ICdcStationSelector>().Should().NotBeNull();
        _provider.GetRequiredService<IMosmixLocationSelector>().Should().NotBeNull();
        _provider.GetRequiredService<IXmlExportSerializer>().Should().NotBeNull();

        _handler.Requests.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that the export produces the expected JSON output for an offline fixture.
    /// </summary>
    [TestMethod]
    public async Task Example_ExportAsync_OfflineFixture_WritesExpectedJson()
    {
        var cdc = _provider.GetRequiredService<CdcCatalogEndpoints>();
        cdc.AirTemperatureStationList!.AbsoluteUri.Should().Be("https://opendata.dwd.de/climate_environment/CDC/observations_germany/climate/10_minutes/air_temperature/now/zehn_now_tu_Beschreibung_Stationen.txt");

        ArrangeStation00044();

        using var destination = new MemoryStream();
        await _provider.GetRequiredService<IDwdJsonExporter>().ExportAsync("00044", destination, CancellationToken.None);
        using var json = JsonDocument.Parse(destination.ToArray());
        var rootElement = json.RootElement;

        rootElement.GetProperty("source").GetString().Should().Be("dwd:00044");
        rootElement.GetProperty("retrieved_at").GetString().Should().Be("2026-10-21T12:00:00");
        rootElement.GetProperty("measured_at").GetString().Should().Be("2026-10-21T11:50:00");

        var location = rootElement.GetProperty("location");
        location.GetProperty("station_id").GetString().Should().Be("00044");

        var measurements = rootElement.GetProperty("measurements");
        measurements.GetProperty("temperature_c").GetProperty("value").GetDecimal().Should().Be(12.3m);
        measurements.GetProperty("pressure_hpa").GetProperty("value").GetDecimal().Should().Be(1001m);
        measurements.GetProperty("rain_rate_mm_h").GetProperty("value").GetDecimal().Should().Be(0m);
        measurements.GetProperty("solar_irradiance_w_m2").GetProperty("value").GetDecimal().Should().Be(600m);

        measurements.TryGetProperty("illuminance_lux", out _).Should().BeFalse();

        _handler.Requests.Should().HaveCount(8)
            .And.OnlyContain(uri => uri.StartsWith("https://opendata.dwd.de/", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that the documentation contains the example files verbatim.
    /// </summary>
    [TestMethod]
    public void Documentation_ContainsEveryExampleFileVerbatim()
    {
        var guide = Normalize(File.ReadAllText(Path.Combine(PackageArtifacts.RepositoryRoot, "docs", "LIBRARY_USAGE.md")));

        var registrationPath = Path.Combine(PackageArtifacts.RepositoryRoot, "tests", "WeatherParser.Package.Tests", "LibraryUsage", "WeatherParserRegistration.cs");
        var registrationSource = Normalize(File.ReadAllText(registrationPath));
        guide.Should().Contain("```csharp\n" + registrationSource + "\n```", because: "docs/LIBRARY_USAGE.md must show LibraryUsage/WeatherParserRegistration.cs verbatim");

        var exporterPath = Path.Combine(PackageArtifacts.RepositoryRoot, "tests", "WeatherParser.Package.Tests", "LibraryUsage", "IDwdJsonExporter.cs");
        var exporterSource = Normalize(File.ReadAllText(exporterPath));
        guide.Should().Contain("```csharp\n" + exporterSource + "\n```", because: "docs/LIBRARY_USAGE.md must show LibraryUsage/IDwdJsonExporter.cs verbatim");

        var dwdExporterPath = Path.Combine(PackageArtifacts.RepositoryRoot, "tests", "WeatherParser.Package.Tests", "LibraryUsage", "DwdJsonExporter.cs");
        var dwdExporterSource = Normalize(File.ReadAllText(dwdExporterPath));
        guide.Should().Contain("```csharp\n" + dwdExporterSource + "\n```", because: "docs/LIBRARY_USAGE.md must show LibraryUsage/DwdJsonExporter.cs verbatim");
    }

    /// <summary>
    /// Verifies that the JSON output matches the example shown in the documentation.
    /// </summary>
    [TestMethod]
    public async Task Documentation_JsonOutput_EqualsSerializerOutput()
    {
        ArrangeStation00044();

        var bytes = await ExportAsync();
        var guide = Normalize(File.ReadAllText(Path.Combine(PackageArtifacts.RepositoryRoot, "docs", "LIBRARY_USAGE.md")));

        var jsonStart = guide.IndexOf("```json\n", StringComparison.Ordinal);
        jsonStart.Should().BeGreaterThanOrEqualTo(0, "docs/LIBRARY_USAGE.md must contain a json code block");
        var jsonEnd = guide.IndexOf("\n```", jsonStart, StringComparison.Ordinal);
        jsonEnd.Should().BeGreaterThan(jsonStart, "the json code block must be closed");
        var jsonBlock = guide.Substring(jsonStart + 8, jsonEnd - jsonStart - 8);

        JsonNode.DeepEquals(JsonNode.Parse(jsonBlock), JsonNode.Parse(bytes)).Should().BeTrue("the JSON shown in docs/LIBRARY_USAGE.md must be the serializer output of the offline fixture");
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").Trim('\n');

    private void ArrangeStation00044()
    {
        var cdc = _provider.GetRequiredService<CdcCatalogEndpoints>();
        var list = ConsumerFixtures.CdcList(ConsumerFixtures.CdcLine("00044", "44", "52.9336", "8.2370", "Grossenkneten"));
        _handler.Add(cdc.AirTemperatureStationList!, list);
        _handler.Add(cdc.WindStationList!, list);
        _handler.Add(cdc.PrecipitationStationList!, list);
        _handler.Add(cdc.SolarStationList!, list);
        _handler.Add(new Uri(cdc.AirTemperatureNowDirectory!, "10minutenwerte_TU_00044_now.zip"), ConsumerFixtures.CdcNowZip("tu", "00044", ConsumerFixtures.CdcRow("00044", "202610211150", "1001.2", "12.3", "10.1", "85.0", "8.9")));
        _handler.Add(new Uri(cdc.WindNowDirectory!, "10minutenwerte_wind_00044_now.zip"), ConsumerFixtures.CdcNowZip("ff", "00044", ConsumerFixtures.CdcRow("00044", "202610211150", "3.47", "270")));
        _handler.Add(new Uri(cdc.PrecipitationNowDirectory!, "10minutenwerte_nieder_00044_now.zip"), ConsumerFixtures.CdcNowZip("rr", "00044", ConsumerFixtures.CdcRow("00044", "202610211150", "10", "0.0", "0")));
        _handler.Add(new Uri(cdc.SolarNowDirectory!, "10minutenwerte_SOLAR_00044_now.zip"), ConsumerFixtures.CdcNowZip("sd", "00044", ConsumerFixtures.CdcRow("00044", "202610211150", "-999", "36.0", "0.1", "-999")));
    }

    private async Task<byte[]> ExportAsync()
    {
        using var destination = new MemoryStream();
        await _provider.GetRequiredService<IDwdJsonExporter>().ExportAsync("00044", destination, CancellationToken.None);
        return destination.ToArray();
    }
}
