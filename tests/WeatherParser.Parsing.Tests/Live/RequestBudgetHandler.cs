using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WeatherParser.Parsing.Tests.Live;

internal sealed class RequestBudgetHandler : DelegatingHandler
{
    private readonly int _budget;
    private readonly List<LiveRequest> _requests = new();
    private int _sent = 0;

    public RequestBudgetHandler(int budget, HttpMessageHandler innerHandler) : base(innerHandler)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(budget);
        _budget = budget;
    }

    public IReadOnlyList<LiveRequest> Requests
    {
        get
        {
            lock (_requests)
            {
                return new List<LiveRequest>(_requests);
            }
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        lock (_requests)
        {
            if (_sent >= _budget)
            {
                throw new InvalidOperationException($"The live-check request budget of {_budget} requests is exhausted (contract SC-16).");
            }
            _sent++;
        }

        var response = await base.SendAsync(request, cancellationToken);

        var record = new LiveRequest(request.RequestUri!.AbsoluteUri, (int)response.StatusCode, response.Content.Headers.ContentLength);
        lock (_requests)
        {
            _requests.Add(record);
        }

        var original = response.Content;
        var body = await original.ReadAsStreamAsync(cancellationToken);
        var counted = new StreamContent(new CountingStream(body, record));
        foreach (var header in original.Headers)
        {
            counted.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        response.Content = counted;
        return response;
    }

    private sealed class CountingStream(Stream inner, LiveRequest record) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = inner.Read(buffer, offset, count);
            record.AddBytes(read);
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var read = await inner.ReadAsync(buffer, cancellationToken);
            record.AddBytes(read);
            return read;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
