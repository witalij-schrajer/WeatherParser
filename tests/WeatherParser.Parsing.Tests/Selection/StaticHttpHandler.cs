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
    private readonly Dictionary<string, HttpStatusCode> _statuses = new(StringComparer.Ordinal);
    public List<string> Requests { get; } = new();
    public Action<string>? OnRequest { get; set; }

    public void Add(Uri uri, byte[] body)
    {
        _responses[uri.AbsoluteUri] = body;
    }

    public void AddStatus(Uri uri, HttpStatusCode status) => _statuses[uri.AbsoluteUri] = status;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (Requests)
        {
            Requests.Add(request.RequestUri!.AbsoluteUri);
        }

        OnRequest?.Invoke(request.RequestUri!.AbsoluteUri);

        if (_statuses.TryGetValue(request.RequestUri.AbsoluteUri, out var status))
        {
            return Task.FromResult(new HttpResponseMessage(status));
        }

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
