using System;

namespace WeatherParser.Parsing.Tests.Serialization;

internal sealed class FailingWriteStream : MemoryStream
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FailingWriteStream"/> class.
    /// </summary>
    public FailingWriteStream()
        : base()
    {
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new IOException("Simulated destination write failure.");
    }

    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        throw new IOException("Simulated destination write failure.");
    }

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return Task.FromException(new IOException("Simulated destination write failure."));
    }

    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
    {
        return new ValueTask(Task.FromException(new IOException("Simulated destination write failure.")));
    }
}
