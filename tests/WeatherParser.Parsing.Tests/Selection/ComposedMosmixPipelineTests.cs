using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Selection;
using WeatherParser.Parsing.Tests.Catalogs;

namespace WeatherParser.Parsing.Tests.Selection;

[TestClass]
public sealed class ComposedMosmixPipelineTests
{
    [TestMethod]
    public async Task ComposedMosmixPipeline_AllLocationsWithoutAltitude_NoticeCountsByIdAndEmptyList()
    {
        using var fixture = new SelectionPipelineFixture(new DateTimeOffset(2026, 10, 21, 12, 0, 0, 500, TimeSpan.Zero));
        await fixture.ArrangeMosmixAsync(
            MosmixCatalogBuilder.Line("X123A", name: "OHNEHOEHE", latitude: "50.06", longitude: "9.30", elevation: null),
            MosmixCatalogBuilder.Line("B456", name: "AUCHOHNE", latitude: "50.12", longitude: "9.36", elevation: null));

        var result = await fixture.SelectMosmixAsync(SelectionPipelineFixture.Parameters());

        result.Locations.Should().BeEmpty();
        result.Source.Should().Be("dwd:mosmix-stationskatalog");

        fixture.ReaderLogger.Count("MOSMIX-LOCATION-NO-ALTITUDE").Should().Be(2);
        fixture.ReaderLogger.Entries.Should().Contain(e => e.Message.Contains("X123A"));
        fixture.ReaderLogger.Entries.Should().Contain(e => e.Message.Contains("B456"));

        fixture.MosmixLogger.Entries.Should().ContainSingle();
        fixture.MosmixLogger.Entries[0].Level.Should().Be(LogLevel.Information);
        fixture.MosmixLogger.Entries[0].EventName.Should().Be("SELECTION-EMPTY");

        fixture.Handler.Requests.Should().ContainSingle();
        fixture.Handler.Requests[0].Should().NotContain("MOSMIX_L_LATEST");
    }

    [TestMethod]
    public async Task ComposedMosmixPipeline_ExclusionAndOtherDiagnostic_EachLoggedOnce()
    {
        using var fixture = new SelectionPipelineFixture(new DateTimeOffset(2026, 10, 21, 12, 0, 0, 500, TimeSpan.Zero));
        await fixture.ArrangeMosmixAsync(
            MosmixCatalogBuilder.Line("X123A", name: "MUSTERHAUSEN", latitude: "50.06", longitude: "9.30", elevation: "250"),
            MosmixCatalogBuilder.Line("B456", name: "OHNEHOEHE", latitude: "50.12", longitude: "9.36", elevation: null),
            MosmixCatalogBuilder.Line("C789", name: "KAPUTT", latitude: "50.6x", longitude: "9.36", elevation: "300"));

        var result = await fixture.SelectMosmixAsync(SelectionPipelineFixture.Parameters());

        result.Locations.Should().ContainSingle();
        result.Locations[0].MosmixId.Should().Be("X123A");

        fixture.ReaderLogger.Count("MOSMIX-LOCATION-NO-ALTITUDE").Should().Be(1);
        fixture.MosmixLogger.Count("MOSMIX-LOCATION-NO-ALTITUDE").Should().Be(0);
        fixture.MosmixLogger.Count("CATALOG-RECORD-SKIPPED").Should().Be(1);
        fixture.MosmixLogger.Entries.Should().Contain(e => e.Message.Contains("CatalogUri"));

        fixture.MosmixLogger.Count("SELECTION-EMPTY").Should().Be(0);
    }

    [TestMethod]
    public async Task ComposedMosmixPipeline_StaleIndex_UsedWithOneNotice()
    {
        using var fixture = new SelectionPipelineFixture(new DateTimeOffset(2026, 10, 21, 16, 0, 1, TimeSpan.Zero));
        await fixture.ArrangeMosmixAsync();

        var result = await fixture.SelectMosmixAsync(SelectionPipelineFixture.Parameters());

        result.Locations.Should().HaveCount(2);

        fixture.MosmixLogger.Count("MOSMIX-S-EVIDENCE-STALE").Should().Be(1);
        fixture.MosmixLogger.Entries.Should().Contain(e => e.Level == LogLevel.Warning && e.EventName == "MOSMIX-S-EVIDENCE-STALE");

        result.Locations[0].Products.Single(p => p.Product == MosmixProduct.MosmixS).EvidenceRetrievedAtUtc.Should().Be(new DateTime(2026, 10, 21, 10, 0, 0, DateTimeKind.Utc));
    }

    [TestMethod]
    public async Task ComposedMosmixPipeline_MissingIndex_FailsBeforeAnyRequest()
    {
        using var fixture = new SelectionPipelineFixture(new DateTimeOffset(2026, 10, 21, 12, 0, 0, 500, TimeSpan.Zero));

        Func<Task> act = async () => await fixture.SelectMosmixAsync(SelectionPipelineFixture.Parameters());

        (await act.Should().ThrowAsync<EvidenceCacheException>())
            .Which.ErrorId.Should().Be(EvidenceCacheException.EvidenceMissingErrorId);

        fixture.Handler.Requests.Should().BeEmpty();
    }
}
