using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Cli.Tests;

/// <summary>
/// A fake broker endpoint for process tests: listens on a free loopback port, accepts every connection and closes it at once, so an MQTT connection fails without any broker (CL-20, REQ-083).
/// </summary>
internal sealed class ClosingTcpListener : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _acceptLoop;
    private int _accepted;

    public ClosingTcpListener()
    {
        _listener.Start();
        _acceptLoop = AcceptAsync();
    }

    /// <summary>Gets the loopback port.</summary>
    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>Gets the number of accepted (and closed) connections.</summary>
    public int AcceptedCount => Volatile.Read(ref _accepted);

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        _listener.Stop();
        try
        {
            await _acceptLoop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (SocketException)
        {
        }

        _stop.Dispose();
    }

    private async Task AcceptAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            using var client = await _listener.AcceptTcpClientAsync(_stop.Token).ConfigureAwait(false);
            Interlocked.Increment(ref _accepted);
        }
    }
}
