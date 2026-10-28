using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;

namespace WeatherParser.Package.Tests;

[TestClass]
public sealed class PackageMosmixForecastTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 4, 16, 30, TimeSpan.Zero);

    [TestMethod]
    public async Task Package_ParseMosmixL_RunSerializedBothFormats()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://a.example.invalid/mosmix/single_stations/") }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://a.example.invalid/mosmix/all_stations/kml/MOSMIX_S_LATEST_240.kmz") }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = TimeSpan.FromHours(8) }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = TimeSpan.FromHours(2) }));

        host.Handler.Add(new Uri("https://a.example.invalid/mosmix/single_stations/10637/kml/MOSMIX_L_LATEST_10637.kmz"), ConsumerFixtures.MosmixLForecastKmz("10637", "BEISPIELSTADT", ("TTT", "283.15 284.15 285.15"), ("FF", "0.00 5.00 10.00")));

        var result = await host.Provider.GetRequiredService<IMosmixLParser>().ParseAsync("10637", CancellationToken.None);
        result.Run.Product.Should().Be(MosmixProduct.MosmixL);
        result.Run.Source.Should().Be("dwd-mosmix-l:10637");
        result.Run.Forecasts.Count.Should().Be(3);
        result.Run.RunAgeExceeded.Should().BeFalse();
        result.Freshness.Should().Be(MosmixRunFreshness.Fresh);

        var json = await host.JsonAsync(result.Run);
        var xml = await host.XmlAsync(result.Run);

        var jsonText = Encoding.UTF8.GetString(json);
        jsonText.Should().Contain("\"product\":\"MOSMIX_L\"");
        jsonText.Should().Contain("\"issued_at\":\"2026-10-21T03:00:00\"");
        jsonText.Should().NotContain("measured_at");

        var xmlEvents = ConsumerSelectionHost.ValidateAgainstPackagedSchema(xml);
        xmlEvents.Should().BeEmpty();

        host.Handler.Requests.Should().Equal(new[] { "https://a.example.invalid/mosmix/single_stations/10637/kml/MOSMIX_L_LATEST_10637.kmz" });
    }

    [TestMethod]
    public async Task Package_ParseMosmixS_RunSerializedBothFormats()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://a.example.invalid/mosmix/single_stations/") }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://a.example.invalid/mosmix/all_stations/kml/MOSMIX_S_LATEST_240.kmz") }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = TimeSpan.FromHours(8) }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = TimeSpan.FromHours(2) }));

        host.Handler.Add(new Uri("https://a.example.invalid/mosmix/all_stations/kml/MOSMIX_S_LATEST_240.kmz"), ConsumerFixtures.MosmixSForecastKmz("10637", "BEISPIELSTADT", ("TTT", "283.15 284.15 285.15"), ("FF", "0.00 5.00 10.00")));

        var result = await host.Provider.GetRequiredService<IMosmixSParser>().ParseAsync("10637", CancellationToken.None);
        result.Run.Product.Should().Be(MosmixProduct.MosmixS);
        result.Run.Source.Should().Be("dwd-mosmix-s:10637");
        result.Run.Forecasts.Count.Should().Be(3);
        result.Run.RunAgeExceeded.Should().BeFalse();
        result.Freshness.Should().Be(MosmixRunFreshness.Fresh);

        var json = await host.JsonAsync(result.Run);
        var xml = await host.XmlAsync(result.Run);

        var jsonText = Encoding.UTF8.GetString(json);
        jsonText.Should().Contain("\"product\":\"MOSMIX_S\"");
        jsonText.Should().Contain("\"issued_at\":\"2026-10-21T03:00:00\"");
        jsonText.Should().NotContain("measured_at");

        var xmlEvents = ConsumerSelectionHost.ValidateAgainstPackagedSchema(xml);
        xmlEvents.Should().BeEmpty();

        host.Handler.Requests.Should().Equal(new[] { "https://a.example.invalid/mosmix/all_stations/kml/MOSMIX_S_LATEST_240.kmz" });
    }

    [TestMethod]
    public async Task Package_BothProducts_OneContainer_Independent()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://a.example.invalid/mosmix/single_stations/") }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://a.example.invalid/mosmix/all_stations/kml/MOSMIX_S_LATEST_240.kmz") }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = TimeSpan.FromHours(8) }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = TimeSpan.FromHours(1) })); // Stale after 1h

        host.Handler.Add(new Uri("https://a.example.invalid/mosmix/single_stations/10637/kml/MOSMIX_L_LATEST_10637.kmz"), ConsumerFixtures.MosmixLForecastKmz("10637", "BEISPIELSTADT", ("TTT", "283.15 284.15 285.15"), ("FF", "0.00 5.00 10.00")));
        host.Handler.Add(new Uri("https://a.example.invalid/mosmix/all_stations/kml/MOSMIX_S_LATEST_240.kmz"), ConsumerFixtures.MosmixSForecastKmz("10637", "BEISPIELSTADT", ("TTT", "283.15 284.15 285.15"), ("FF", "0.00 5.00 10.00")));

        var lResult = await host.Provider.GetRequiredService<IMosmixLParser>().ParseAsync("10637", CancellationToken.None);
        var sResult = await host.Provider.GetRequiredService<IMosmixSParser>().ParseAsync("10637", CancellationToken.None);

        lResult.Run.Source.Should().Be("dwd-mosmix-l:10637");
        sResult.Run.Source.Should().Be("dwd-mosmix-s:10637");

        host.Handler.Requests.Count.Should().Be(2);

        lResult.Freshness.Should().Be(MosmixRunFreshness.Fresh);
        lResult.Run.RunAgeExceeded.Should().BeFalse();

        sResult.Freshness.Should().Be(MosmixRunFreshness.Stale);
        sResult.Run.RunAgeExceeded.Should().BeTrue();
    }

    [TestMethod]
    public async Task Package_SchemaInsidePackage_ValidatesRuns()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://a.example.invalid/mosmix/single_stations/") }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://a.example.invalid/mosmix/all_stations/kml/MOSMIX_S_LATEST_240.kmz") }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = TimeSpan.FromHours(8) }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = TimeSpan.FromHours(2) }));

        host.Handler.Add(new Uri("https://a.example.invalid/mosmix/single_stations/10637/kml/MOSMIX_L_LATEST_10637.kmz"), ConsumerFixtures.MosmixLForecastKmz("10637", "BEISPIELSTADT", ("TTT", "283.15 284.15 285.15"), ("FF", "0.00 5.00 10.00")));
        host.Handler.Add(new Uri("https://a.example.invalid/mosmix/all_stations/kml/MOSMIX_S_LATEST_240.kmz"), ConsumerFixtures.MosmixSForecastKmz("10637", "BEISPIELSTADT", ("TTT", "283.15 284.15 285.15"), ("FF", "0.00 5.00 10.00")));

        var lResult = await host.Provider.GetRequiredService<IMosmixLParser>().ParseAsync("10637", CancellationToken.None);
        var sResult = await host.Provider.GetRequiredService<IMosmixSParser>().ParseAsync("10637", CancellationToken.None);

        var lXml = await host.XmlAsync(lResult.Run);
        var sXml = await host.XmlAsync(sResult.Run);

        ConsumerSelectionHost.ValidateAgainstPackagedSchema(lXml).Should().BeEmpty();
        ConsumerSelectionHost.ValidateAgainstPackagedSchema(sXml).Should().BeEmpty();

        // Test that a wrong product name is rejected
        var lXmlWrongProduct = Encoding.UTF8.GetString(lXml).Replace("<product>MOSMIX_L</product>", "<product>MOSMIX_X</product>");
        var wrongEvents = ConsumerSelectionHost.ValidateAgainstPackagedSchema(Encoding.UTF8.GetBytes(lXmlWrongProduct));
        wrongEvents.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task Package_PublicApi_ParsersAndOptionsVisible()
    {
        var exportedTypes = typeof(IMosmixLParser).Assembly.GetExportedTypes().Select(t => t.Name).ToArray();

        exportedTypes.Should().Contain("IMosmixLParser");
        exportedTypes.Should().Contain("MosmixLParser");
        exportedTypes.Should().Contain("IMosmixSParser");
        exportedTypes.Should().Contain("MosmixSParser");
        exportedTypes.Should().Contain("MosmixParseResult");
        exportedTypes.Should().Contain("MosmixRunFreshness");
        exportedTypes.Should().Contain("MosmixNoticeIds");
        exportedTypes.Should().Contain("MosmixForecastErrorIds");
        exportedTypes.Should().Contain("MosmixForecastException");
        exportedTypes.Should().Contain("MosmixLForecastEndpoints");
        exportedTypes.Should().Contain("MosmixSForecastEndpoints");
        exportedTypes.Should().Contain("MosmixLForecastOptions");
        exportedTypes.Should().Contain("MosmixSForecastOptions");
        exportedTypes.Should().Contain("IMosmixParsePipeline");
        exportedTypes.Should().Contain("MosmixParsePipeline");
        exportedTypes.Should().Contain("MosmixParseRequest");

        exportedTypes.Should().NotContain("KmzCrc32Stream");
        exportedTypes.Should().NotContain("KmzCountingStream");
        exportedTypes.Should().NotContain("KmzMarkupGuardStream");
        exportedTypes.Should().NotContain("KmzBoundedNameTable");
        exportedTypes.Should().NotContain("MosmixElementTable");
    }

    [TestMethod]
    public async Task Package_InvalidId_ThrowsWithoutRequest()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://a.example.invalid/mosmix/single_stations/") }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://a.example.invalid/mosmix/all_stations/kml/MOSMIX_S_LATEST_240.kmz") }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixLForecastOptions { ParseDeadline = TimeSpan.FromSeconds(60), MaxRunAge = TimeSpan.FromHours(8) }));
        host.Services.Replace(ServiceDescriptor.Singleton(new MosmixSForecastOptions { ParseDeadline = TimeSpan.FromMinutes(5), MaxRunAge = TimeSpan.FromHours(2) }));

        Func<Task> actL = () => host.Provider.GetRequiredService<IMosmixLParser>().ParseAsync("abc", CancellationToken.None);
        await actL.Should().ThrowAsync<MosmixForecastException>()
            .Where(e => e.ErrorId == MosmixForecastErrorIds.IdInvalid);

        Func<Task> actS = () => host.Provider.GetRequiredService<IMosmixSParser>().ParseAsync("abc", CancellationToken.None);
        await actS.Should().ThrowAsync<MosmixForecastException>()
            .Where(e => e.ErrorId == MosmixForecastErrorIds.IdInvalid);

        host.Handler.Requests.Should().BeEmpty();
    }
}
