using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Evidence.MosmixS;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests.Catalogs;
using WeatherParser.Parsing.Tests.Evidence.MosmixL;
using WeatherParser.Parsing.Tests.Evidence.MosmixS;
using WeatherParser.Parsing.Tests.Parsing;
using WeatherParser.Parsing.Tests.Selection;

namespace WeatherParser.Parsing.Tests.Live;

[TestClass]
public sealed class LiveCheckRunnerFailureTests
{
    private string _directory = null!;
    private StaticHttpHandler _handler = null!;
    private ManualTimeProvider _clock = null!;

    [TestInitialize]
    public void Initialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "wp-live-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _handler = new StaticHttpHandler();
        _clock = new ManualTimeProvider(LiveCheckTestData.Now);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RunAsync_RefreshExceedsRefreshDeadline_FailsWithRefreshDeadlineExceeded()
    {
        var settings = LiveCheckTestData.Settings(_directory);
        LiveCheckTestData.Arrange(_handler, settings);

        _handler.OnRequest = uri =>
        {
            if (uri == settings.Mosmix.MosmixSAllStationsUri!.AbsoluteUri)
            {
                _clock.Advance(TimeSpan.FromMinutes(11));
            }
        };

        Func<Task> act = async () => await LiveCheckRunner.RunAsync(settings, _handler, _clock, CancellationToken.None);
        await act.Should().ThrowAsync<EvidenceCacheException>()
            .Where(e => e.ErrorId == EvidenceCacheException.RefreshDeadlineExceededErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RunAsync_SelectionExceedsSelectionDeadline_FailsWithSelectionDeadlineExceeded()
    {
        var settings = LiveCheckTestData.Settings(_directory);
        LiveCheckTestData.Arrange(_handler, settings);

        _handler.OnRequest = uri =>
        {
            if (uri == settings.Mosmix.CatalogUri!.AbsoluteUri)
            {
                _clock.Advance(TimeSpan.FromSeconds(121));
            }
        };

        Func<Task> act = async () => await LiveCheckRunner.RunAsync(settings, _handler, _clock, CancellationToken.None);
        await act.Should().ThrowAsync<SelectionLimitException>()
            .Where(e => e.ErrorId == SelectionLimitException.DeadlineExceededErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RunAsync_RequestExceedsThirtySeconds_FailsWithCatalogTimeout()
    {
        var settings = LiveCheckTestData.Settings(_directory);
        LiveCheckTestData.Arrange(_handler, settings);

        _handler.OnRequest = uri =>
        {
            if (uri == settings.Mosmix.CatalogUri!.AbsoluteUri)
            {
                _clock.Advance(TimeSpan.FromSeconds(31));
            }
        };

        Func<Task> act = async () => await LiveCheckRunner.RunAsync(settings, _handler, _clock, CancellationToken.None);
        await act.Should().ThrowAsync<CatalogRetrievalException>()
            .Where(e => e.ErrorId == CatalogRetrievalException.TimeoutErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RunAsync_TooManyCandidates_FailsBeforeAnyMosmixLRequest()
    {
        var settings = LiveCheckTestData.Settings(_directory, mosmixCandidates: 1);
        LiveCheckTestData.Arrange(_handler, settings);

        Func<Task> act = async () => await LiveCheckRunner.RunAsync(settings, _handler, _clock, CancellationToken.None);
        await act.Should().ThrowAsync<SelectionLimitException>()
            .Where(e => e.ErrorId == SelectionLimitException.TooManyCandidatesErrorId);

        _handler.Requests.Should().HaveCount(2);
        _handler.Requests[0].Should().Contain(settings.Mosmix.MosmixSAllStationsUri!.AbsoluteUri);
        _handler.Requests[1].Should().Contain(settings.Mosmix.CatalogUri!.AbsoluteUri);
        _handler.Requests.Should().NotContain(r => r.Contains("MOSMIX_L_LATEST"));
    }

    [TestMethod]
    [Timeout(10000)]
    [DataRow(0)]
    [DataRow(10001)]
    public async Task RunAsync_InvalidSelectionLimit_FailsBeforeAnyRequest(int candidates)
    {
        var settings = LiveCheckTestData.Settings(_directory, cdcCandidates: candidates);
        LiveCheckTestData.Arrange(_handler, settings);

        Func<Task> act = async () => await LiveCheckRunner.RunAsync(settings, _handler, _clock, CancellationToken.None);
        await act.Should().ThrowAsync<SelectionLimitException>()
            .Where(e => e.ErrorId == SelectionLimitException.LimitInvalidErrorId);

        _handler.Requests.Should().BeEmpty();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RunAsync_CatalogServerError_FailsWithRetrievalFailedWithoutRetry()
    {
        var settings = LiveCheckTestData.Settings(_directory);
        LiveCheckTestData.Arrange(_handler, settings);

        _handler.AddStatus(settings.Mosmix.CatalogUri!, HttpStatusCode.InternalServerError);

        Func<Task> act = async () => await LiveCheckRunner.RunAsync(settings, _handler, _clock, CancellationToken.None);
        await act.Should().ThrowAsync<CatalogRetrievalException>()
            .Where(e => e.ErrorId == CatalogRetrievalException.RetrievalFailedErrorId);

        _handler.Requests.Should().ContainSingle(r => r.Contains(settings.Mosmix.CatalogUri!.AbsoluteUri));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RunAsync_CatalogTooLarge_FailsWithTooLarge()
    {
        var settings = LiveCheckTestData.Settings(_directory);
        LiveCheckTestData.Arrange(_handler, settings);

        _handler.Add(settings.Mosmix.CatalogUri!, new byte[8 * 1024 * 1024 + 1]);

        Func<Task> act = async () => await LiveCheckRunner.RunAsync(settings, _handler, _clock, CancellationToken.None);
        await act.Should().ThrowAsync<CatalogRetrievalException>()
            .Where(e => e.ErrorId == CatalogRetrievalException.TooLargeErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RunAsync_CatalogMalformed_FailsWithFormatInvalid()
    {
        var settings = LiveCheckTestData.Settings(_directory);
        LiveCheckTestData.Arrange(_handler, settings);

        _handler.Add(settings.Mosmix.CatalogUri!, System.Text.Encoding.ASCII.GetBytes("not a catalog\n"));

        Func<Task> act = async () => await LiveCheckRunner.RunAsync(settings, _handler, _clock, CancellationToken.None);
        await act.Should().ThrowAsync<CatalogFormatException>()
            .Where(e => e.ErrorId == CatalogFormatException.CatalogFormatInvalidErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task RunAsync_CallerCancellation_ThrowsOperationCanceledExceptionWithCallerToken()
    {
        var settings = LiveCheckTestData.Settings(_directory);
        LiveCheckTestData.Arrange(_handler, settings);

        using var caller = new CancellationTokenSource();
        _handler.OnRequest = uri =>
        {
            if (uri == settings.Mosmix.CatalogUri!.AbsoluteUri)
            {
                caller.Cancel();
            }
        };

        Func<Task> act = async () => await LiveCheckRunner.RunAsync(settings, _handler, _clock, caller.Token);
        await act.Should().ThrowAsync<OperationCanceledException>()
            .Where(e => e.CancellationToken == caller.Token);
    }
}
