using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Selection;
using FluentAssertions;

namespace WeatherParser.Package.Tests;

[TestClass]
public sealed class PackageMosmixEvidenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task PackageSelection_MosmixOffline_WithRefresh_ReturnsEvidenceTimestamps()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.ArrangeMosmix();
        await host.Provider.GetRequiredService<IMosmixSEvidenceRefresher>().RefreshAsync(CancellationToken.None);
        var list = await host.Provider.GetRequiredService<IMosmixLocationSelector>().SelectAsync(ConsumerSelectionHost.Parameters(), CancellationToken.None);

        // JSON validation
        using var jsonDoc = JsonDocument.Parse(await host.JsonAsync(list));
        var locations = jsonDoc.RootElement.GetProperty("locations").EnumerateArray().ToArray();
        locations.Should().HaveCount(2);

        var loc0 = locations[0];
        loc0.GetProperty("mosmix_id").GetString().Should().Be("X123A");

        var products0 = loc0.GetProperty("products").EnumerateArray().ToArray();
        products0.Should().HaveCount(2);

        var mosmixS0 = products0.First(p => p.GetProperty("product").GetString() == "MOSMIX_S");
        mosmixS0.GetProperty("availability").GetString().Should().Be("available");
        mosmixS0.GetProperty("evidence_retrieved_at").GetString().Should().Be("2026-10-21T12:00:00");
        mosmixS0.GetProperty("evidence_issued_at").GetString().Should().Be("2026-10-21T03:00:00");

        var mosmixL0 = products0.First(p => p.GetProperty("product").GetString() == "MOSMIX_L");
        mosmixL0.GetProperty("availability").GetString().Should().Be("available");
        mosmixL0.GetProperty("evidence_retrieved_at").GetString().Should().Be("2026-10-21T12:00:00");
        mosmixL0.GetProperty("evidence_issued_at").GetString().Should().Be("2026-10-21T03:00:00");

        var loc1 = locations[1];
        loc1.GetProperty("mosmix_id").GetString().Should().Be("01234");

        var products1 = loc1.GetProperty("products").EnumerateArray().ToArray();
        products1.Should().HaveCount(2);

        var mosmixS1 = products1.First(p => p.GetProperty("product").GetString() == "MOSMIX_S");
        mosmixS1.GetProperty("availability").GetString().Should().Be("unavailable");
        mosmixS1.GetProperty("evidence_retrieved_at").GetString().Should().Be("2026-10-21T12:00:00");
        mosmixS1.GetProperty("evidence_issued_at").GetString().Should().Be("2026-10-21T03:00:00");

        var mosmixL1 = products1.First(p => p.GetProperty("product").GetString() == "MOSMIX_L");
        mosmixL1.GetProperty("availability").GetString().Should().Be("unavailable");
        mosmixL1.TryGetProperty("evidence_retrieved_at", out _).Should().BeFalse();
        mosmixL1.TryGetProperty("evidence_issued_at", out _).Should().BeFalse();

        // XML validation
        var xmlEvents = ConsumerSelectionHost.ValidateAgainstPackagedSchema(await host.XmlAsync(list));
        xmlEvents.Should().BeEmpty();

        // XML parsing with XDocument
        XNamespace ns = "urn:weatherparser:export:v1";
        var xml = await host.XmlAsync(list);
        var xDoc = XDocument.Load(new MemoryStream(xml));
        var locationsXml = xDoc.Root!.Element(ns + "locations")!.Elements(ns + "location").ToArray();
        locationsXml.Should().HaveCount(2);

        var loc0Xml = locationsXml[0];
        loc0Xml.Element(ns + "mosmix_id")!.Value.Should().Be("X123A");

        var products0Xml = loc0Xml.Element(ns + "products")!.Elements(ns + "product_availability").ToArray();
        products0Xml.Should().HaveCount(2);

        var mosmixS0Xml = products0Xml.First(p => p.Element(ns + "product")!.Value == "MOSMIX_S");
        mosmixS0Xml.Element(ns + "availability")!.Value.Should().Be("available");
        mosmixS0Xml.Element(ns + "evidence_retrieved_at")!.Value.Should().Be("2026-10-21T12:00:00");
        mosmixS0Xml.Element(ns + "evidence_issued_at")!.Value.Should().Be("2026-10-21T03:00:00");

        var mosmixL0Xml = products0Xml.First(p => p.Element(ns + "product")!.Value == "MOSMIX_L");
        mosmixL0Xml.Element(ns + "availability")!.Value.Should().Be("available");
        mosmixL0Xml.Element(ns + "evidence_retrieved_at")!.Value.Should().Be("2026-10-21T12:00:00");
        mosmixL0Xml.Element(ns + "evidence_issued_at")!.Value.Should().Be("2026-10-21T03:00:00");

        var loc1Xml = locationsXml[1];
        loc1Xml.Element(ns + "mosmix_id")!.Value.Should().Be("01234");

        var products1Xml = loc1Xml.Element(ns + "products")!.Elements(ns + "product_availability").ToArray();
        products1Xml.Should().HaveCount(2);

        var mosmixS1Xml = products1Xml.First(p => p.Element(ns + "product")!.Value == "MOSMIX_S");
        mosmixS1Xml.Element(ns + "availability")!.Value.Should().Be("unavailable");
        mosmixS1Xml.Element(ns + "evidence_retrieved_at")!.Value.Should().Be("2026-10-21T12:00:00");
        mosmixS1Xml.Element(ns + "evidence_issued_at")!.Value.Should().Be("2026-10-21T03:00:00");

        var mosmixL1Xml = products1Xml.First(p => p.Element(ns + "product")!.Value == "MOSMIX_L");
        mosmixL1Xml.Element(ns + "availability")!.Value.Should().Be("unavailable");
        mosmixL1Xml.Element(ns + "evidence_retrieved_at").Should().BeNull();
        mosmixL1Xml.Element(ns + "evidence_issued_at").Should().BeNull();
    }

    [TestMethod]
    public async Task PackageSelection_MosmixOffline_NoRefresh_ThrowsEvidenceMissing()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.ArrangeMosmix();
        Func<Task> act = async () => await host.Provider.GetRequiredService<IMosmixLocationSelector>().SelectAsync(ConsumerSelectionHost.Parameters(), CancellationToken.None);
        var ex = await act.Should().ThrowAsync<EvidenceCacheException>();
        ex.Which.ErrorId.Should().Be(EvidenceCacheException.EvidenceMissingErrorId);
        host.Handler.Requests.Should().BeEmpty();
    }

    [TestMethod]
    public async Task PackageSelection_MosmixOffline_StaleIndex_WritesNoticeOnce()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.ArrangeMosmix();
        await host.Provider.GetRequiredService<IMosmixSEvidenceRefresher>().RefreshAsync(CancellationToken.None);

        host.Clock.Now = Now.AddHours(6).AddSeconds(1); // stale index

        var list = await host.Provider.GetRequiredService<IMosmixLocationSelector>().SelectAsync(ConsumerSelectionHost.Parameters(), CancellationToken.None);
        list.Locations.Should().HaveCount(2);

        host.MosmixLogger.Count("MOSMIX-S-EVIDENCE-STALE").Should().Be(1);
        host.MosmixLogger.Entries.Should().Contain(e => e.Level == LogLevel.Warning && e.EventName == "MOSMIX-S-EVIDENCE-STALE");

        var mosmixS = list.Locations[0].Products.First(p => p.Product == MosmixProduct.MosmixS);
        mosmixS.EvidenceRetrievedAtUtc.Should().Be(new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc));
    }

    [TestMethod]
    public async Task PackageSelection_MosmixOffline_IndexAtMaxAge_NoNotice()
    {
        using var host = new ConsumerSelectionHost("https://a.example.invalid/", Now);
        host.ArrangeMosmix();
        await host.Provider.GetRequiredService<IMosmixSEvidenceRefresher>().RefreshAsync(CancellationToken.None);

        host.Clock.Now = Now.AddHours(6); // exactly at max age

        var list = await host.Provider.GetRequiredService<IMosmixLocationSelector>().SelectAsync(ConsumerSelectionHost.Parameters(), CancellationToken.None);
        list.Locations.Should().HaveCount(2);

        host.MosmixLogger.Count("MOSMIX-S-EVIDENCE-STALE").Should().Be(0);
    }
}
