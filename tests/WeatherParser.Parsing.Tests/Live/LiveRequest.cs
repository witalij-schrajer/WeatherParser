namespace WeatherParser.Parsing.Tests.Live;

using System.Threading;

internal sealed class LiveRequest(string uri, int statusCode, long? contentLength)
{
    public string Uri { get; } = uri;
    public int StatusCode { get; } = statusCode;
    public long? ContentLength { get; } = contentLength;
    private long _bytesRead;

    public long BytesRead => Interlocked.Read(ref _bytesRead);

    internal void AddBytes(long count) => Interlocked.Add(ref _bytesRead, count);
}
