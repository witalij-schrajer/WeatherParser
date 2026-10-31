using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

if (args.Length != 2)
{
    Console.Error.WriteLine("usage: WeatherParser.Cli.FixtureHost <root directory> <port>");
    return 2;
}

var root = Path.GetFullPath(args[0]);
if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"root directory not found: {root}");
    return 2;
}

if (!int.TryParse(args[1], CultureInfo.InvariantCulture, out var port) || port < 0 || port > 65535)
{
    Console.Error.WriteLine("usage: WeatherParser.Cli.FixtureHost <root directory> <port>");
    return 2;
}

if (!root.EndsWith(Path.DirectorySeparatorChar))
{
    root += Path.DirectorySeparatorChar;
}

var listener = new TcpListener(IPAddress.Loopback, port);
listener.Start();

// The shutdown handlers are registered before the announcement, so a signal sent right after it ends the loop with exit code 0.
using var stopping = new CancellationTokenSource();

using var sigint = PosixSignalRegistration.Create(PosixSignal.SIGINT, context => { context.Cancel = true; stopping.Cancel(); });
using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context => { context.Cancel = true; stopping.Cancel(); });

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    stopping.Cancel();
};

var actualPort = ((IPEndPoint)listener.LocalEndpoint).Port;
Console.Out.WriteLine($"LISTENING http://127.0.0.1:{actualPort}/");
Console.Out.Flush();

while (!stopping.IsCancellationRequested)
{
    TcpClient? client = null;
    try
    {
        client = await listener.AcceptTcpClientAsync(stopping.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }

    _ = Task.Run(() => HandleAsync(client, root, stopping.Token));
}

listener.Stop();
Console.Error.WriteLine("STOPPED");
return 0;

static async Task HandleAsync(TcpClient client, string root, CancellationToken token)
{
    try
    {
        using var stream = client.GetStream();
        var (requestHead, tooLarge) = await ReadHeadAsync(stream, token);
        if (tooLarge)
        {
            await WriteResponseAsync(stream, 400, "Bad Request");
            Console.Error.WriteLine("- - 400");
            return;
        }

        if (requestHead == null)
        {
            return;
        }

        var firstLine = requestHead.Split("\r\n")[0];
        var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || !parts[2].StartsWith("HTTP/"))
        {
            await WriteResponseAsync(stream, 400, "Bad Request");
            Console.Error.WriteLine("- - 400");
            return;
        }

        var method = parts[0];
        var target = parts[1];

        if (method != "GET")
        {
            await WriteResponseAsync(stream, 405, "Method Not Allowed");
            Console.Error.WriteLine($"{method} {target} 405");
            return;
        }

        if (!target.StartsWith('/'))
        {
            await WriteResponseAsync(stream, 400, "Bad Request");
            Console.Error.WriteLine($"{method} {target} 400");
            return;
        }

        if (target.Contains('?') || target.Contains('#'))
        {
            await WriteResponseAsync(stream, 400, "Bad Request");
            Console.Error.WriteLine($"{method} {target} 400");
            return;
        }

        var segments = target[1..].Split('/', StringSplitOptions.RemoveEmptyEntries);
        var decodedSegments = new string[segments.Length];
        for (int i = 0; i < segments.Length; i++)
        {
            var decoded = Uri.UnescapeDataString(segments[i]);
            if (decoded == ".." || decoded == "." || decoded.Contains('/') || decoded.Contains('\\') || decoded.Contains(':') || decoded.Contains('\0'))
            {
                await WriteResponseAsync(stream, 400, "Bad Request");
                Console.Error.WriteLine($"{method} {target} 400");
                return;
            }

            decodedSegments[i] = decoded;
        }

        var combinedPath = Path.Combine(new[] { root }.Concat(decodedSegments).ToArray());
        var fullPath = Path.GetFullPath(combinedPath);

        if (!fullPath.StartsWith(root, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
        {
            await WriteResponseAsync(stream, 400, "Bad Request");
            Console.Error.WriteLine($"{method} {target} 400");
            return;
        }

        if (!File.Exists(fullPath))
        {
            await WriteResponseAsync(stream, 404, "Not Found");
            Console.Error.WriteLine($"{method} {target} 404");
            return;
        }

        var content = await File.ReadAllBytesAsync(fullPath, token);
        await WriteResponseAsync(stream, 200, "OK", content);
        Console.Error.WriteLine($"{method} {target} 200");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"ERROR {ex.Message}");
    }
    finally
    {
        client?.Close();
    }
}

static async Task<(string? Head, bool TooLarge)> ReadHeadAsync(NetworkStream stream, CancellationToken token)
{
    var buffer = ArrayPool<byte>.Shared.Rent(8192);
    try
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        var bytes = new List<byte>();
        int read;
        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
        {
            for (int i = 0; i < read; i++)
            {
                bytes.Add(buffer[i]);
                if (bytes.Count > 8192)
                {
                    return (null, true);
                }

                if (bytes.Count >= 4)
                {
                    var lastFour = bytes[^4..];
                    if (lastFour[0] == 13 && lastFour[1] == 10 && lastFour[2] == 13 && lastFour[3] == 10)
                    {
                        return (Encoding.ASCII.GetString(bytes.ToArray()), false);
                    }
                }
            }
        }

        return (null, false);
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
}

static async Task WriteResponseAsync(NetworkStream stream, int code, string reason, byte[]? body = null)
{
    var response = $"HTTP/1.1 {code} {reason}\r\nContent-Length: {(body?.Length ?? 0)}\r\nContent-Type: application/octet-stream\r\nConnection: close\r\n\r\n";
    var responseBytes = Encoding.ASCII.GetBytes(response);
    await stream.WriteAsync(responseBytes);

    if (body != null)
    {
        await stream.WriteAsync(body);
    }
}
