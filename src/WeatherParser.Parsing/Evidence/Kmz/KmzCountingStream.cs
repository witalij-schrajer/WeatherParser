using System.IO;
using System.Threading;
using WeatherParser.Parsing.Catalogs;

namespace WeatherParser.Parsing.Evidence.Kmz;

/// <summary>
/// Counts the unpacked member bytes against a bound and observes cancellation while streaming (shared by the MOSMIX evidence and forecast readers, SC-21, SC-22).
/// </summary>
internal sealed class KmzCountingStream : ReadOnlyStreamBase
{
    private readonly Stream _inner;
    private readonly long _maxBytes;
    private readonly CancellationToken _cancellationToken;
    private readonly Func<string, Exception> _invalid;
    private long _total;

    public KmzCountingStream(Stream inner, long maxBytes, CancellationToken cancellationToken, Func<string, Exception> invalid)
    {
        _invalid = invalid;
        _inner = inner;
        _maxBytes = maxBytes;
        _cancellationToken = cancellationToken;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        var read = _inner.Read(buffer, offset, count);
        _total += read;
        if (_total > _maxBytes)
        {
            throw _invalid("the unpacked .kml member exceeds the size bound");
        }

        return read;
    }
}
