namespace WeatherParser.Parsing.Mqtt;

/// <summary>
/// Write-only buffer for the MQTT payload: it never throws on writes, stores at most Cap + 1 bytes and records an overflow, so the serializer can check the placeholder values and the topic before it reports PayloadTooLarge (contract MQ-11, MQ-13).
/// </summary>
internal sealed class MqttPayloadBuffer : Stream
{
    private readonly int _cap;
    private readonly MemoryStream _stored = new();
    public bool Overflowed { get; private set; }

    public MqttPayloadBuffer(int cap)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cap, 1);
        _cap = cap;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Write(buffer.AsSpan(offset, count));
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        var room = _cap + 1 - (int)_stored.Length;
        _stored.Write(buffer[..Math.Min(buffer.Length, room)]);
        if (buffer.Length > room || _stored.Length > _cap)
        {
            Overflowed = true;
        }
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Write(buffer.Span);
        return ValueTask.CompletedTask;
    }

    public override void Flush() { }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => _stored.Length;
    public override long Position
    {
        get => _stored.Length;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public byte[] ToArray() => _stored.ToArray();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stored.Dispose();
        }
        base.Dispose(disposing);
    }
}
