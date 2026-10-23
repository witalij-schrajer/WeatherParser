using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Parsing.Tests.Selection;

internal sealed class StaticHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, byte[]> _responses = new(StringComparer.Ordinal);
    public List<string> Requests { get; } = new();
    public Action<string>? OnRequest { get; set; }

    public void Add(Uri uri, byte[] body)
    {
        _responses[uri.AbsoluteUri] = body;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (Requests)
        {
            Requests.Add(request.RequestUri!.AbsoluteUri);
        }

        OnRequest?.Invoke(request.RequestUri!.AbsoluteUri);

        if (_responses.TryGetValue(request.RequestUri.AbsoluteUri, out var body))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(body)
            });
        }
        else
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
