using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Selection;
using FluentAssertions;

namespace WeatherParser.Package.Tests;

[TestClass]
public sealed class PackageSelectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task PackageSelection_CdcOffline_ReturnsSchemaValidList()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.ArrangeCdc();
        var list = await host.Provider.GetRequiredService<ICdcStationSelector>().SelectAsync(ConsumerSelectionHost.Parameters(), CancellationToken.None);

        list.Source.Should().Be("dwd:cdc-10min-now");
        list.RetrievedAtWire.Should().Be("2026-10-21T12:00:00");

        list.Stations.Should().HaveCount(1);
        var station = list.Stations[0];
        station.StationId.Should().Be("00044");
        station.Name.Should().Be("Mühlheim & <Süd>");
        station.AvailableMeasurements.Should().Contain("humidity_percent");
        station.AvailableMeasurements.Should().Contain("pressure_hpa");
        station.AvailableMeasurements.Should().Contain("pressure_mmhg");
        station.AvailableMeasurements.Should().Contain("temperature_c");

        var xmlEvents = ConsumerSelectionHost.ValidateAgainstPackagedSchema(await host.XmlAsync(list));
        xmlEvents.Should().BeEmpty();

        using var jsonDoc = JsonDocument.Parse(await host.JsonAsync(list));
        jsonDoc.RootElement.GetProperty("stations").EnumerateArray().Should().HaveCount(1);
        jsonDoc.RootElement.GetProperty("stations").EnumerateArray().First().GetProperty("station_id").GetString().Should().Be("00044");

        host.Handler.Requests.Should().HaveCount(8);
    }

    [TestMethod]
    public async Task PackageSelection_MosmixOffline_ReturnsSchemaValidList()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.ArrangeMosmix();
        await host.Provider.GetRequiredService<IMosmixSEvidenceRefresher>().RefreshAsync(CancellationToken.None);
        var list = await host.Provider.GetRequiredService<IMosmixLocationSelector>().SelectAsync(ConsumerSelectionHost.Parameters(), CancellationToken.None);

        list.Locations.Should().HaveCount(2);
        var loc1 = list.Locations[0];
        var loc2 = list.Locations[1];

        loc1.MosmixId.Should().Be("X123A");
        loc1.Products.Should().HaveCount(2);
        loc1.Products.Should().OnlyContain(p => p.Product == MosmixProduct.MosmixS || p.Product == MosmixProduct.MosmixL);

        var mosmixS = loc1.Products.First(p => p.Product == MosmixProduct.MosmixS);
        mosmixS.Availability.Should().Be(AvailabilityState.Available);
        mosmixS.AvailableForecastElements.Should().Contain("FF");
        mosmixS.AvailableForecastElements.Should().Contain("TTT");

        var xmlEvents = ConsumerSelectionHost.ValidateAgainstPackagedSchema(await host.XmlAsync(list));
        xmlEvents.Should().BeEmpty();
    }

    [TestMethod]
    public async Task PackageSelection_EmptyResult_ValidEmptyLists()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.ArrangeCdc();
        host.ArrangeMosmix();
        await host.Provider.GetRequiredService<IMosmixSEvidenceRefresher>().RefreshAsync(CancellationToken.None);

        var cdcList = await host.Provider.GetRequiredService<ICdcStationSelector>().SelectAsync(ConsumerSelectionHost.Parameters(-50m), CancellationToken.None);
        var mosmixList = await host.Provider.GetRequiredService<IMosmixLocationSelector>().SelectAsync(ConsumerSelectionHost.Parameters(-50m), CancellationToken.None);

        cdcList.Stations.Should().BeEmpty();
        mosmixList.Locations.Should().BeEmpty();

        cdcList.Source.Should().NotBeNull();
        mosmixList.Source.Should().NotBeNull();

        var xmlEvents1 = ConsumerSelectionHost.ValidateAgainstPackagedSchema(await host.XmlAsync(cdcList));
        xmlEvents1.Should().BeEmpty();

        var xmlEvents2 = ConsumerSelectionHost.ValidateAgainstPackagedSchema(await host.XmlAsync(mosmixList));
        xmlEvents2.Should().BeEmpty();

        using var jsonDoc1 = JsonDocument.Parse(await host.JsonAsync(cdcList));
        jsonDoc1.RootElement.GetProperty("stations").EnumerateArray().Should().HaveCount(0);

        using var jsonDoc2 = JsonDocument.Parse(await host.JsonAsync(mosmixList));
        jsonDoc2.RootElement.GetProperty("locations").EnumerateArray().Should().HaveCount(0);

        host.CdcLogger.Count("SELECTION-EMPTY").Should().Be(1);
        host.MosmixLogger.Count("SELECTION-EMPTY").Should().Be(1);
    }

    [TestMethod]
    public async Task PackageSelection_IndependentConfigurations()
    {
        using var a = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        using var b = new ConsumerSelectionHost("https://b.example.invalid/", Now);

        a.ArrangeCdc();
        b.ArrangeCdc();

        var aList = await a.Provider.GetRequiredService<ICdcStationSelector>().SelectAsync(ConsumerSelectionHost.Parameters(), CancellationToken.None);
        var bList = await b.Provider.GetRequiredService<ICdcStationSelector>().SelectAsync(ConsumerSelectionHost.Parameters(), CancellationToken.None);

        aList.Stations.Should().HaveCount(1);
        aList.Stations[0].StationId.Should().Be("00044");

        bList.Stations.Should().HaveCount(1);
        bList.Stations[0].StationId.Should().Be("00044");

        a.Handler.Requests.Should().OnlyContain(r => r.StartsWith("https://a.example.invalid/"));
        b.Handler.Requests.Should().OnlyContain(r => r.StartsWith("https://b.example.invalid/"));

        var aSelector = a.Provider.GetRequiredService<ICdcStationSelector>();
        var bSelector = b.Provider.GetRequiredService<ICdcStationSelector>();
        aSelector.Should().NotBeSameAs(bSelector);

        a.Directory.Should().NotBeSameAs(b.Directory);
    }

    [TestMethod]
    public void PackageSelection_MissingConfiguration_FailsAtValidation()
    {
        var services = new ServiceCollection().AddWeatherParsing();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(TimeProvider.System);

        try
        {
            services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
            Assert.Fail("The container validated without the consumer registrations.");
        }
        catch (AggregateException ex)
        {
            ex.ToString().Should().Contain("HttpClient");
            ex.ToString().Should().Contain("CdcCatalogEndpoints");
            ex.ToString().Should().Contain("MosmixCatalogEndpoints");
        }
    }
}
