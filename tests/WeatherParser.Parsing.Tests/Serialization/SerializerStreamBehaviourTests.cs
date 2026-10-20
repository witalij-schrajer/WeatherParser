using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Export;
using WeatherParser.Parsing.Tests.Serialization;

namespace WeatherParser.Parsing.Tests.Serialization;

[TestClass]
public sealed class SerializerStreamBehaviourTests
{
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    [TestMethod]
    [DynamicData(nameof(AllRootsTestData.Roots), typeof(AllRootsTestData))]
    public async Task SerializeAsync_AllRoots_WriteUtf8WithoutBom(string rootName)
    {
        var root = AllRootsTestData.Build(rootName);

        // Test JSON
        {
            using var stream = new MemoryStream();
            await AllRootsTestData.WriteJsonAsync(root, _json, stream, CancellationToken.None);
            var bytes = stream.ToArray();
            bytes.Should().NotBeEmpty();
            bytes.Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });
            Action act = () => new UTF8Encoding(false, true).GetString(bytes);
            act.Should().NotThrow();
        }

        // Test XML
        {
            using var stream = new MemoryStream();
            await AllRootsTestData.WriteXmlAsync(root, _xml, stream, CancellationToken.None);
            var bytes = stream.ToArray();
            bytes.Should().NotBeEmpty();
            bytes.Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });
            Action act = () => new UTF8Encoding(false, true).GetString(bytes);
            act.Should().NotThrow();
        }
    }

    [TestMethod]
    [DynamicData(nameof(AllRootsTestData.Roots), typeof(AllRootsTestData))]
    public async Task SerializeAsync_AllRoots_LeaveDestinationOpen(string rootName)
    {
        var root = AllRootsTestData.Build(rootName);

        // Test JSON
        {
            using var stream = new MemoryStream();
            await AllRootsTestData.WriteJsonAsync(root, _json, stream, CancellationToken.None);
            stream.CanWrite.Should().BeTrue();
            stream.CanRead.Should().BeTrue();
        }

        // Test XML
        {
            using var stream = new MemoryStream();
            await AllRootsTestData.WriteXmlAsync(root, _xml, stream, CancellationToken.None);
            stream.CanWrite.Should().BeTrue();
            stream.CanRead.Should().BeTrue();
        }
    }

    [TestMethod]
    [DynamicData(nameof(AllRootsTestData.Roots), typeof(AllRootsTestData))]
    public async Task SerializeAsync_TokenCanceledBeforeSerialization_WritesNothing(string rootName)
    {
        var root = AllRootsTestData.Build(rootName);

        // Test JSON
        {
            using var stream = new MemoryStream();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            Func<Task> act = () => AllRootsTestData.WriteJsonAsync(root, _json, stream, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
            stream.Length.Should().Be(0);
        }

        // Test XML
        {
            using var stream = new MemoryStream();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            Func<Task> act = () => AllRootsTestData.WriteXmlAsync(root, _xml, stream, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
            stream.Length.Should().Be(0);
        }
    }

    [TestMethod]
    [DynamicData(nameof(AllRootsTestData.Roots), typeof(AllRootsTestData))]
    public async Task SerializeAsync_DestinationFailsDuringWrite_PropagatesAndLeavesStreamOpen(string rootName)
    {
        var root = AllRootsTestData.Build(rootName);

        // Test JSON
        {
            using var stream = new FailingWriteStream();
            Func<Task> act = () => AllRootsTestData.WriteJsonAsync(root, _json, stream, CancellationToken.None);
            await act.Should().ThrowAsync<IOException>();
            stream.CanWrite.Should().BeTrue();
        }

        // Test XML
        {
            using var stream = new FailingWriteStream();
            Func<Task> act = () => AllRootsTestData.WriteXmlAsync(root, _xml, stream, CancellationToken.None);
            await act.Should().ThrowAsync<IOException>();
            stream.CanWrite.Should().BeTrue();
        }
    }

    [TestMethod]
    [DynamicData(nameof(AllRootsTestData.Roots), typeof(AllRootsTestData))]
    public async Task SerializeAsync_TokenCanceledDuringCopy_PropagatesAndLeavesStreamOpen(string rootName)
    {
        var root = AllRootsTestData.Build(rootName);

        // Test JSON
        {
            using var cts = new CancellationTokenSource();
            using var stream = new PrefixThenCancelStream(cts);
            Func<Task> act = () => AllRootsTestData.WriteJsonAsync(root, _json, stream, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
            stream.Length.Should().Be(1);
            stream.CanWrite.Should().BeTrue();
        }

        // Test XML
        {
            using var cts = new CancellationTokenSource();
            using var stream = new PrefixThenCancelStream(cts);
            Func<Task> act = () => AllRootsTestData.WriteXmlAsync(root, _xml, stream, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
            stream.Length.Should().Be(1);
            stream.CanWrite.Should().BeTrue();
        }
    }

    [TestMethod]
    [DynamicData(nameof(AllRootsTestData.Roots), typeof(AllRootsTestData))]
    public async Task SerializeAsync_DestinationFailsAfterPartialWrite_PropagatesAndLeavesStreamOpen(string rootName)
    {
        var root = AllRootsTestData.Build(rootName);

        // Test JSON
        {
            using var stream = new PrefixThenFailStream();
            Func<Task> act = () => AllRootsTestData.WriteJsonAsync(root, _json, stream, CancellationToken.None);
            (await act.Should().ThrowAsync<IOException>()).WithMessage("Simulated failure after a partial write.");
            stream.Length.Should().Be(1);
            stream.CanWrite.Should().BeTrue();
        }

        // Test XML
        {
            using var stream = new PrefixThenFailStream();
            Func<Task> act = () => AllRootsTestData.WriteXmlAsync(root, _xml, stream, CancellationToken.None);
            (await act.Should().ThrowAsync<IOException>()).WithMessage("Simulated failure after a partial write.");
            stream.Length.Should().Be(1);
            stream.CanWrite.Should().BeTrue();
        }
    }

    private sealed class PrefixThenCancelStream : Stream
    {
        private readonly CancellationTokenSource _source;
        private readonly MemoryStream _inner = new();

        public PrefixThenCancelStream(CancellationTokenSource source)
        {
            _source = source;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _inner.Length;
        public override long Position
        {
            get => _inner.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            _inner.Flush();
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

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!buffer.IsEmpty)
            {
                _inner.Write(buffer.Span[..1]);
                _source.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
                _inner.Write(buffer.Span[1..]);
            }

            return ValueTask.CompletedTask;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer.AsMemory(offset, count), CancellationToken.None).AsTask().GetAwaiter().GetResult();
        }
    }

    private sealed class PrefixThenFailStream : Stream
    {
        private readonly MemoryStream _inner = new();

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _inner.Length;
        public override long Position
        {
            get => _inner.Position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            _inner.Flush();
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

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!buffer.IsEmpty)
            {
                _inner.Write(buffer.Span[..1]);
            }

            throw new IOException("Simulated failure after a partial write.");
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer.AsMemory(offset, count), CancellationToken.None).AsTask().GetAwaiter().GetResult();
        }
    }
}
