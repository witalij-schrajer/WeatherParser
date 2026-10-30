using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace WeatherParser.Cli.Tests;

internal sealed class LoopbackHttpServer : IAsyncDisposable
{
    private readonly HttpListener _listener;
    private readonly Task _loop;
    private readonly string _baseUri;
    private int _requestCount;
    private readonly ConcurrentQueue<string> _paths = new();

    public LoopbackHttpServer()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        _listener = new HttpListener();
        _baseUri = $"http://127.0.0.1:{port}/";
        _listener.Prefixes.Add(_baseUri);
        _listener.Start();
        _loop = Task.Run(AcceptLoopAsync);
    }

    public Uri BaseUri => new(_baseUri);

    public int RequestCount => Volatile.Read(ref _requestCount);

    public IReadOnlyList<string> RequestedPaths => _paths.ToArray();

    public Func<string, (int StatusCode, byte[] Body)> Responder { get; set; } = _ => (404, Array.Empty<byte>());

    public Func<string, string?> Redirect { get; set; } = _ => null;

    private async Task AcceptLoopAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception) when (!_listener.IsListening)
            {
                return;
            }

            Interlocked.Increment(ref _requestCount);
            var path = context.Request.Url!.PathAndQuery;
            _paths.Enqueue(path);
            
            var redirectTarget = Redirect(path);
            if (redirectTarget != null)
            {
                context.Response.StatusCode = 302;
                context.Response.Headers.Add("Location", redirectTarget);
                await context.Response.OutputStream.WriteAsync(Array.Empty<byte>());
                context.Response.Close();
            }
            else
            {
                var (status, body) = Responder(path);
                context.Response.StatusCode = status;
                await context.Response.OutputStream.WriteAsync(body);
                context.Response.Close();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _listener.Stop();
        _listener.Close();
        try
        {
            await _loop;
        }
        catch (Exception)
        {
        }

        // Ensure the task is awaited even if it's already completed.
        await _loop.ContinueWith(_ => { }, TaskScheduler.Default);
    }
}
