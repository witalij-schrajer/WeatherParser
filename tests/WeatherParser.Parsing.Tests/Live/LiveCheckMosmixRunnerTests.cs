using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Quantities;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Forecasts.Mosmix;
using WeatherParser.Parsing.Tests.Live;

namespace WeatherParser.Parsing.Tests.Live;

[TestClass]
public sealed class LiveCheckMosmixRunnerTests
{
    private MockRepository _repository = null!;
    private Mock<HttpMessageHandler> _handler = null!;
    private ManualTimeProvider _clock = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _repository = new(MockBehavior.Strict);
        _handler = _repository.Create<HttpMessageHandler>();
        _clock = new ManualTimeProvider(new DateTimeOffset(2026, 10, 26, 16, 16, 30, TimeSpan.Zero));
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _repository.VerifyAll();
    }

    private static Func<string, string?> Env(string? enable, string? mode) => name => name == LiveCheckRunner.EnableVariable ? enable : name == LiveCheckRunner.ModeVariable ? mode : null;

    [DataRow("1", "mosmix-l")]
    [DataRow("1", "mosmix-s")]
    [TestMethod]
    public void IsMosmixLMode_RequiresEnableAndMode(string? enable, string? mode)
    {
        LiveCheckRunner.IsMosmixLMode(Env(enable, mode)).Should().Be(mode == "mosmix-l" && enable == "1");
    }

    [DataRow("1", "mosmix-l")]
    [DataRow("1", "mosmix-s")]
    [TestMethod]
    public void IsMosmixSMode_RequiresEnableAndMode(string? enable, string? mode)
    {
        LiveCheckRunner.IsMosmixSMode(Env(enable, mode)).Should().Be(mode == "mosmix-s" && enable == "1");
    }

    [DataRow("1", "mosmix-l")]
    [DataRow("1", "mosmix-s")]
    [TestMethod]
    public void MosmixModes_ExcludeOtherLiveChecks(string? enable, string? mode)
    {
        LiveCheckRunner.IsSelectionMode(Env(enable, mode)).Should().BeFalse();
        LiveCheckRunner.IsObservationMode(Env(enable, mode)).Should().BeFalse();
        LiveCheckRunner.IsOsthessenMode(Env(enable, mode)).Should().BeFalse();
    }

    [TestMethod]
    public async Task RunMosmixIfEnabledAsync_ReturnsNullWhenNotEnabled()
    {
        var settings = LiveCheckSettings.Dwd(Path.GetTempPath());

        var result = await LiveCheckRunner.RunMosmixIfEnabledAsync(
            Env(null, null),
            settings,
            _handler.Object,
            _clock,
            CancellationToken.None);

        result.Should().BeNull();
        _handler.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task RunMosmixAsync_MosmixL_OneRequestWithinBudget()
    {
        var settings = LiveCheckSettings.Dwd(Path.GetTempPath());

        var kml = MosmixForecastKmzBuilder.Single("10637", "BEISPIELSTADT", "9.6,50.5,250.0", MosmixForecastKmzBuilder.Forecast("TTT", "283.15 284.15 285.15"));
        var kmz = MosmixForecastKmzBuilder.Kmz(kml);

        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsoluteUri == "https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/10637/kml/MOSMIX_L_LATEST_10637.kmz"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new MemoryStream(kmz)) });

        var result = await LiveCheckRunner.RunMosmixAsync(
            MosmixProduct.MosmixL,
            settings,
            _handler.Object,
            _clock,
            CancellationToken.None);

        result.Product.Should().Be(MosmixProduct.MosmixL);
        result.MosmixId.Should().Be("10637");
        result.Run.Should().NotBeNull();
        result.Run!.Source.Should().Be("dwd-mosmix-l:10637");
        result.Freshness.Should().Be(MosmixRunFreshness.Fresh);
        result.ErrorId.Should().BeNull();
        result.Requests.Should().ContainSingle().And.Subject.Select(r => r.Uri).Should().ContainSingle().And.Equal("https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_L/single_stations/10637/kml/MOSMIX_L_LATEST_10637.kmz");
        result.SchemaEvents.Should().BeEmpty();
        result.Json.Should().NotBeNull();
        result.Xml.Should().NotBeNull();
        result.Duration.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(0);
        result.PeakWorkingSetBytes.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task RunMosmixAsync_MosmixS_OneRequestWithinBudget()
    {
        var settings = LiveCheckSettings.Dwd(Path.GetTempPath());

        var kml = MosmixForecastKmzBuilder.Document(
            MosmixForecastKmzBuilder.ProductDefinition(),
            MosmixForecastKmzBuilder.Placemark("P0001", "OTHER", "8.0,51.0,100.0", MosmixForecastKmzBuilder.Forecast("TTT", "283.15 284.15 285.15")),
            MosmixForecastKmzBuilder.Placemark("10637", "BEISPIELSTADT", "9.6,50.5,250.0", MosmixForecastKmzBuilder.Forecast("TTT", "283.15 284.15 285.15")));
        var kmz = MosmixForecastKmzBuilder.Kmz(kml, memberName: MosmixForecastKmzBuilder.AllStationsMemberName);

        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", 
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsoluteUri == "https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_S/all_stations/kml/MOSMIX_S_LATEST_240.kmz"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new MemoryStream(kmz)) });

        var result = await LiveCheckRunner.RunMosmixAsync(
            MosmixProduct.MosmixS,
            settings,
            _handler.Object,
            _clock,
            CancellationToken.None);

        result.Product.Should().Be(MosmixProduct.MosmixS);
        result.MosmixId.Should().Be("10637");
        result.Run.Should().NotBeNull();
        result.Run!.Source.Should().Be("dwd-mosmix-s:10637");
        result.Freshness.Should().Be(MosmixRunFreshness.Fresh);
        result.ErrorId.Should().BeNull();
        result.Requests.Should().ContainSingle().And.Subject.Select(r => r.Uri).Should().ContainSingle().And.Equal("https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_S/all_stations/kml/MOSMIX_S_LATEST_240.kmz");
        result.SchemaEvents.Should().BeEmpty();
        result.Json.Should().NotBeNull();
        result.Xml.Should().NotBeNull();
        result.Duration.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(0);
        result.PeakWorkingSetBytes.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task RunMosmixAsync_SecondRequestExceedsBudget()
    {
        var settings = LiveCheckSettings.Dwd(Path.GetTempPath()) with { MosmixId = "10637" };

        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await LiveCheckRunner.RunMosmixAsync(
            MosmixProduct.MosmixL,
            settings,
            _handler.Object,
            _clock,
            CancellationToken.None);

        result.ErrorId.Should().Be("MOSMIX-ID-UNKNOWN");
        result.Run.Should().BeNull();
        result.Json.Should().BeNull();
        result.Xml.Should().BeNull();
        result.Requests.Should().ContainSingle();
        result.Requests[0].StatusCode.Should().Be(404);
    }

    [TestMethod]
    public async Task RunMosmixAsync_FormatFailure_RecordedAsErrorId()
    {
        var settings = LiveCheckSettings.Dwd(Path.GetTempPath());

        var kml = MosmixForecastKmzBuilder.Document(
            MosmixForecastKmzBuilder.ProductDefinition(issueTime: null),
            MosmixForecastKmzBuilder.Placemark("10637", "BEISPIELSTADT", "9.6,50.5,250.0", MosmixForecastKmzBuilder.Forecast("TTT", "283.15 284.15 285.15")));
        var kmz = MosmixForecastKmzBuilder.Kmz(kml);

        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new MemoryStream(kmz)) });

        var result = await LiveCheckRunner.RunMosmixAsync(
            MosmixProduct.MosmixL,
            settings,
            _handler.Object,
            _clock,
            CancellationToken.None);

        result.ErrorId.Should().Be("MOSMIX-ISSUE-TIME-MISSING");
        result.Run.Should().BeNull();
        result.Requests.Should().ContainSingle();
        result.Json.Should().BeNull();
        result.Xml.Should().BeNull();
    }

    [TestMethod]
    public async Task Artifacts_WriteAsync_ValueFreeSummaryAndDocuments()
    {
        var settings = LiveCheckSettings.Dwd(Path.GetTempPath());

        var kml = MosmixForecastKmzBuilder.Single("10637", "BEISPIELSTADT", "9.6,50.5,250.0", MosmixForecastKmzBuilder.Forecast("TTT", "283.15 284.15 285.15"));
        var kmz = MosmixForecastKmzBuilder.Kmz(kml);

        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new MemoryStream(kmz)) });

        var result = await LiveCheckRunner.RunMosmixAsync(
            MosmixProduct.MosmixL,
            settings,
            _handler.Object,
            _clock,
            CancellationToken.None);

        var output = Path.Combine(Path.GetTempPath(), "wp-mosmix-" + Guid.NewGuid());
        Directory.CreateDirectory(output);

        await LiveMosmixArtifacts.WriteAsync(result, settings, output, CancellationToken.None);

        var summary = await File.ReadAllTextAsync(Path.Combine(output, "summary.txt"));
        summary.Should().Contain("product=MOSMIX_L");
        summary.Should().Contain("id=10637");
        summary.Should().Contain("requests=1");
        summary.Should().Contain("freshness=Fresh");
        summary.Should().Contain("steps=3");
        summary.Should().NotContain("283.15");
        summary.Should().NotContain("20.0");
        summary.Should().Contain("location=BEISPIELSTADT");

        var json = await File.ReadAllBytesAsync(Path.Combine(output, "forecast.json"));
        json.Should().Equal(result.Json);

        var xml = await File.ReadAllBytesAsync(Path.Combine(output, "forecast.xml"));
        xml.Should().Equal(result.Xml);
    }

    [TestMethod]
    public async Task Artifacts_WriteAsync_ErrorCase()
    {
        var settings = LiveCheckSettings.Dwd(Path.GetTempPath());

        var kml = MosmixForecastKmzBuilder.Document(
            MosmixForecastKmzBuilder.ProductDefinition(issueTime: null),
            MosmixForecastKmzBuilder.Placemark("10637", "BEISPIELSTADT", "9.6,50.5,250.0", MosmixForecastKmzBuilder.Forecast("TTT", "283.15 284.15 285.15")));
        var kmz = MosmixForecastKmzBuilder.Kmz(kml);

        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new MemoryStream(kmz)) });

        var result = await LiveCheckRunner.RunMosmixAsync(
            MosmixProduct.MosmixL,
            settings,
            _handler.Object,
            _clock,
            CancellationToken.None);

        var output = Path.Combine(Path.GetTempPath(), "wp-mosmix-" + Guid.NewGuid());
        Directory.CreateDirectory(output);

        await LiveMosmixArtifacts.WriteAsync(result, settings, output, CancellationToken.None);

        var summary = await File.ReadAllTextAsync(Path.Combine(output, "summary.txt"));
        summary.Should().Contain("error=MOSMIX-ISSUE-TIME-MISSING");
    }

    [TestMethod]
    public async Task RunMosmixAsync_Success_KeepsOmissionsAndNotExported()
    {
        var settings = LiveCheckSettings.Dwd(Path.GetTempPath());

        // Create a forecast with one missing value and one not mapped element
        var kml = MosmixForecastKmzBuilder.Document(
            MosmixForecastKmzBuilder.ProductDefinition(),
            MosmixForecastKmzBuilder.Placemark("10637", "BEISPIELSTADT", "9.6,50.5,250.0",
                MosmixForecastKmzBuilder.Forecast("TTT", "283.15 - 285.15"), // Missing value at step 1
                MosmixForecastKmzBuilder.Forecast("wwP", "1.00 2.00 3.00"))); // Not mapped element
        var kmz = MosmixForecastKmzBuilder.Kmz(kml);

        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new MemoryStream(kmz)) });

        var result = await LiveCheckRunner.RunMosmixAsync(
            MosmixProduct.MosmixL,
            settings,
            _handler.Object,
            _clock,
            CancellationToken.None);

        // Verify the result
        result.ErrorId.Should().BeNull();
        result.NotExportedElements.Should().Equal("wwP");
        result.OmittedValues.Should().ContainSingle();
        var omitted = result.OmittedValues.Single();
        omitted.ValidAtUtc.Should().Be(new DateTime(2026, 10, 26, 17, 0, 0, DateTimeKind.Utc));
        omitted.Kind.Should().Be(QuantityKind.Temperature);
        omitted.Reason.Should().Be(OmissionReason.Missing);
        result.AbsentElements.Should().Contain("PPPP");

        // The summary reports the omission and the not-exported element instead of misleading zero counts.
        var output = Path.Combine(Path.GetTempPath(), "wp-mosmix-" + Guid.NewGuid());
        Directory.CreateDirectory(output);
        try
        {
            await LiveMosmixArtifacts.WriteAsync(result, settings, output, CancellationToken.None);
            var summary = await File.ReadAllTextAsync(Path.Combine(output, "summary.txt"));
            summary.Should().Contain("not_exported=1: wwP");
            summary.Should().Contain("omitted=1");
            summary.Split('\n').Select(line => line.TrimEnd()).Should().Contain(line => line.StartsWith("omitted Temperature", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(output))
            {
                Directory.Delete(output, recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task Artifacts_WriteAsync_RejectsRepositoryDirectory()
    {
        var root = OutputDirectoryGuard.RepositoryRoot();
        if (root is null)
        {
            Assert.Inconclusive("Repository root not detected");
        }

        var settings = LiveCheckSettings.Dwd(Path.GetTempPath());
        var kml = MosmixForecastKmzBuilder.Document(
            MosmixForecastKmzBuilder.ProductDefinition(),
            MosmixForecastKmzBuilder.Placemark("10637", "BEISPIELSTADT", "9.6,50.5,250.0",
                MosmixForecastKmzBuilder.Forecast("TTT", "283.15 284.15 285.15")));
        var kmz = MosmixForecastKmzBuilder.Kmz(kml);
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new MemoryStream(kmz)) });
        var result = await LiveCheckRunner.RunMosmixAsync(MosmixProduct.MosmixL, settings, _handler.Object, _clock, CancellationToken.None);
        result.ErrorId.Should().BeNull();

        Func<Task> act = () => LiveMosmixArtifacts.WriteAsync(result, settings, root, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage($"*{Path.GetFullPath(root)}*");
        File.Exists(Path.Combine(root, "summary.txt")).Should().BeFalse();
    }
}
