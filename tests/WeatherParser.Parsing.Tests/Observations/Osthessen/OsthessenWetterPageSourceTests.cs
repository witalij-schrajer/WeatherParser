using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests.Catalogs;

namespace WeatherParser.Parsing.Tests.Observations.Osthessen;

[TestClass]
public sealed class OsthessenWetterPageSourceTests
{
    private MockRepository _mocks = null!;
    private Mock<HttpMessageHandler> _handler = null!;
    private HttpClient _http = null!;
    private ManualTimeProvider _time = null!;
    private OsthessenWetterPageSource _subject = null!;
    private int _sends;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _handler = _mocks.Create<HttpMessageHandler>();
        _http = new HttpClient(_handler.Object, disposeHandler: false);
        _time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 25, 8, 0, 0, TimeSpan.Zero));
        _subject = Create(new Uri("https://example.org/messwerte/"));
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
        _http.Dispose();
    }

    private OsthessenWetterPageSource Create(Uri pageUri) => new(new OsthessenWetterOptions(), new OsthessenWetterEndpoints { PageUri = pageUri }, new EndpointUriValidator(), new CatalogDownloader(_http, _time));

    private void Respond(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    {
        _handler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns((HttpRequestMessage m, CancellationToken t) =>
            {
                _sends++;
                return respond(m, t);
            });
    }

    private static async Task<byte[]> ReadAll(Stream stream, CancellationToken token)
    {
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy, token);
        return copy.ToArray();
    }

    private static HttpResponseMessage Status(HttpStatusCode code) => new(code) { Content = new ByteArrayContent(Array.Empty<byte>()) };

    private static async Task<OsthessenWetterException> ShouldFail(Func<Task> act, string errorId)
    {
        var ex = await FluentActions.Awaiting(act).Should().ThrowAsync<OsthessenWetterException>();
        ex.Which.ErrorId.Should().Be(errorId);
        return ex.Which;
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_Success_OneRequestStreamRead()
    {
        var sent = new HttpRequestMessage();
        Respond((m, _) =>
        {
            sent = m;
            return Task.FromResult(HttpTestContent.Ok(HttpTestContent.Known(HttpTestContent.Bytes(100))));
        });
        var result = await _subject.ReadAsync(ReadAll, CancellationToken.None, CancellationToken.None);
        result.Should().Equal(HttpTestContent.Bytes(100));
        _sends.Should().Be(1);
        sent.Method.Should().Be(HttpMethod.Get);
        sent.RequestUri!.Should().Be(new Uri("https://example.org/messwerte/"));
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_404_ThrowsPageUnavailable()
    {
        Respond((_, _) => Task.FromResult(Status(HttpStatusCode.NotFound)));
        await ShouldFail(() => _subject.ReadAsync(ReadAll, CancellationToken.None, CancellationToken.None), OsthessenWetterException.PageUnavailableErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_500_ThrowsPageUnavailable()
    {
        Respond((_, _) => Task.FromResult(Status(HttpStatusCode.InternalServerError)));
        await ShouldFail(() => _subject.ReadAsync(ReadAll, CancellationToken.None, CancellationToken.None), OsthessenWetterException.PageUnavailableErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_503_ThrowsPageUnavailable()
    {
        Respond((_, _) => Task.FromResult(Status(HttpStatusCode.ServiceUnavailable)));
        await ShouldFail(() => _subject.ReadAsync(ReadAll, CancellationToken.None, CancellationToken.None), OsthessenWetterException.PageUnavailableErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_TransportFailure_ThrowsPageUnavailable()
    {
        Respond((_, _) => throw new HttpRequestException("connection refused"));
        await ShouldFail(() => _subject.ReadAsync(ReadAll, CancellationToken.None, CancellationToken.None), OsthessenWetterException.PageUnavailableErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_HttpClientTimeout_ThrowsPageUnavailable()
    {
        Respond((_, _) => throw new TaskCanceledException("timeout", new TimeoutException()));
        await ShouldFail(() => _subject.ReadAsync(ReadAll, CancellationToken.None, CancellationToken.None), OsthessenWetterException.PageUnavailableErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_ExactlyMaxBytes_Accepted()
    {
        var bytes = HttpTestContent.Bytes(2097152);
        Respond((_, _) => Task.FromResult(HttpTestContent.Ok(HttpTestContent.Known(bytes))));
        var result = await _subject.ReadAsync(ReadAll, CancellationToken.None, CancellationToken.None);
        result.Should().Equal(bytes);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_OverMaxBytes_ThrowsPageUnavailable()
    {
        Respond((_, _) => Task.FromResult(HttpTestContent.Ok(HttpTestContent.Known(HttpTestContent.Bytes(2097153)))));
        await ShouldFail(() => _subject.ReadAsync(ReadAll, CancellationToken.None, CancellationToken.None), OsthessenWetterException.PageUnavailableErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_DeadlineExpired_ThrowsDeadlineExceeded()
    {
        using var deadline = new CancellationTokenSource();
        Respond((_, t) =>
        {
            deadline.Cancel();
            return Task.FromException<HttpResponseMessage>(new TaskCanceledException());
        });
        await ShouldFail(() => _subject.ReadAsync(ReadAll, deadline.Token, CancellationToken.None), OsthessenWetterException.DeadlineExceededErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_CallerCancelled_ThrowsOperationCanceled()
    {
        using var caller = new CancellationTokenSource();
        using var deadline = new CancellationTokenSource();
        Respond((_, t) =>
        {
            caller.Cancel();
            deadline.Cancel();
            return Task.FromException<HttpResponseMessage>(new TaskCanceledException());
        });
        Func<Task> act = () => _subject.ReadAsync(ReadAll, deadline.Token, caller.Token);
        await FluentActions.Awaiting(act)
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_DefaultOptions_StalledRequestEndsAsPageUnavailableAt30Seconds()
    {
        using var deadlineSource = new CancellationTokenSource(new OsthessenWetterOptions().PageDeadline, _time);
        Respond((_, t) => Task.Delay(Timeout.Infinite, t).ContinueWith<HttpResponseMessage>(_ => throw new TaskCanceledException()));
        var task = _subject.ReadAsync(ReadAll, deadlineSource.Token, CancellationToken.None);
        _time.Advance(TimeSpan.FromSeconds(30));
        await ShouldFail(() => task, OsthessenWetterException.PageUnavailableErrorId);
        deadlineSource.Token.IsCancellationRequested.Should().BeFalse();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_NoRetry()
    {
        Respond((_, _) => Task.FromResult(Status(HttpStatusCode.InternalServerError)));
        await ShouldFail(() => _subject.ReadAsync(ReadAll, CancellationToken.None, CancellationToken.None), OsthessenWetterException.PageUnavailableErrorId);
        _sends.Should().Be(1);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_Diagnostic_NoQueryString()
    {
        _subject = Create(new Uri("https://example.org/messwerte/?token=x1y2"));
        Respond((_, _) => Task.FromResult(Status(HttpStatusCode.InternalServerError)));
        var ex = await FluentActions.Awaiting(() => _subject.ReadAsync(ReadAll, CancellationToken.None, CancellationToken.None))
            .Should().ThrowAsync<OsthessenWetterException>();
        ex.Which.Message.Should().NotContain("token");
        ex.Which.Message.Should().NotContain("x1y2");
        ex.Which.Message.Should().Contain("example.org");

        // The deadline failure must not expose the query either.
        using var deadline = new CancellationTokenSource();
        Respond((_, t) =>
        {
            deadline.Cancel();
            return Task.FromException<HttpResponseMessage>(new TaskCanceledException());
        });
        var task = _subject.ReadAsync(ReadAll, deadline.Token, CancellationToken.None);
        var deadlineEx = await FluentActions.Awaiting(() => task).Should().ThrowAsync<OsthessenWetterException>();
        deadlineEx.Which.ErrorId.Should().Be(OsthessenWetterException.DeadlineExceededErrorId);
        deadlineEx.Which.Message.Should().NotContain("token");
        deadlineEx.Which.Message.Should().NotContain("x1y2");
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_NullRead_ThrowsArgumentNullException()
    {
        Func<Task> act = () => _subject.ReadAsync<byte[]>(null!, CancellationToken.None, CancellationToken.None);
        await FluentActions.Awaiting(act)
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_BodyStalled_CallerCancelled_ThrowsOperationCanceled()
    {
        using var caller = new CancellationTokenSource();
        Respond((_, _) => Task.FromResult(HttpTestContent.Ok(new StreamContent(new OsthessenWetterHtml.PendingStream()))));
        var task = _subject.ReadAsync(ReadAll, CancellationToken.None, caller.Token);
        caller.Cancel();
        await FluentActions.Awaiting(() => task)
            .Should().ThrowAsync<OperationCanceledException>()
            .Where(ex => ex.CancellationToken == caller.Token);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_BodyStalled_PageDeadline_ThrowsDeadlineExceeded()
    {
        using var deadline = new CancellationTokenSource();
        Respond((_, _) => Task.FromResult(HttpTestContent.Ok(new StreamContent(new OsthessenWetterHtml.PendingStream()))));
        var task = _subject.ReadAsync(ReadAll, deadline.Token, CancellationToken.None);
        deadline.Cancel();
        await ShouldFail(() => task, OsthessenWetterException.DeadlineExceededErrorId);
    }

    [TestMethod]
    [Timeout(10000)]
    public async Task Read_BodyStalled_DownloaderRequestTimeout_ThrowsPageUnavailable()
    {
        using var caller = new CancellationTokenSource();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60), _time);
        Respond((_, _) => Task.FromResult(HttpTestContent.Ok(new StreamContent(new OsthessenWetterHtml.PendingStream()))));
        var task = _subject.ReadAsync(ReadAll, deadline.Token, caller.Token);
        _time.Advance(TimeSpan.FromSeconds(30));
        await ShouldFail(() => task, OsthessenWetterException.PageUnavailableErrorId);
    }
}
