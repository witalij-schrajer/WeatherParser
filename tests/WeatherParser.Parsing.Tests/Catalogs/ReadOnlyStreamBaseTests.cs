namespace WeatherParser.Parsing.Tests.Catalogs;

using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Catalogs;

[TestClass]
public sealed class ReadOnlyStreamBaseTests
{
    private sealed class EmptyStream : ReadOnlyStreamBase
    {
        public override int Read(byte[] buffer, int offset, int count) => 0;
    }

    [TestMethod]
    public void ReadOnlyStreamBase_OnlyReadingIsSupported()
    {
        var stream = new EmptyStream();
        stream.CanRead.Should().BeTrue();
        stream.CanSeek.Should().BeFalse();
        stream.CanWrite.Should().BeFalse();

        FluentActions.Invoking(() => _ = stream.Length).Should().Throw<NotSupportedException>();
        FluentActions.Invoking(() => _ = stream.Position).Should().Throw<NotSupportedException>();
        FluentActions.Invoking(() => stream.Position = 1).Should().Throw<NotSupportedException>();
        FluentActions.Invoking(() => stream.Seek(0, SeekOrigin.Begin)).Should().Throw<NotSupportedException>();
        FluentActions.Invoking(() => stream.SetLength(1)).Should().Throw<NotSupportedException>();
        FluentActions.Invoking(() => stream.Write(new byte[1], 0, 1)).Should().Throw<NotSupportedException>();
        FluentActions.Invoking(() => stream.Flush()).Should().NotThrow();
    }
}
