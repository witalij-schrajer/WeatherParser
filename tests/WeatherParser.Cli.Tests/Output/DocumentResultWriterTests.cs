using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Cli.Output;
using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Arguments;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Cli.Tests.Commands;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Cli.Tests.Output;

[TestClass]
[TestCategory("T-P3-12")]
public class DocumentResultWriterTests
{
    private MockRepository _mocks = null!;
    private Mock<IJsonExportSerializer> _json = null!;
    private Mock<IXmlExportSerializer> _xml = null!;
    private Mock<IOutputDestination> _destination = null!;
    private DocumentResultWriter _subject = null!;
    private StringWriter _stdout = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _json = _mocks.Create<IJsonExportSerializer>();
        _xml = _mocks.Create<IXmlExportSerializer>();
        _destination = _mocks.Create<IOutputDestination>();
        _subject = new DocumentResultWriter(_json.Object, _xml.Object, _destination.Object);
        _stdout = new StringWriter();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
        _stdout.Dispose();
    }

    private CommandContext Context(OutputFormat output) => new(new CommonOptions(output, null, null, false), null, _stdout);

    [TestMethod]
    public async Task WriteAsync_Json_PassesCompleteSerializedBytesOnce()
    {
        var root = TestExportData.CdcStations("00044");
        byte[] captured = null!;

        _json.Setup(s => s.SerializeAsync(root, It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<CdcStationList, Stream, CancellationToken>((_, stream, _) => stream.WriteAsync(Encoding.UTF8.GetBytes("{\"x\":1}")).AsTask());

        _destination.Setup(d => d.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .Callback<ReadOnlyMemory<byte>, CommandContext, CancellationToken>((bytes, _, _) => captured = bytes.ToArray())
            .Returns(Task.CompletedTask);

        await _subject.WriteAsync(root, Context(OutputFormat.Json), CancellationToken.None);

        Encoding.UTF8.GetString(captured!).Should().Be("{\"x\":1}");
        _destination.Verify(d => d.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [TestMethod]
    public async Task WriteAsync_Xml_UsesXmlSerializer()
    {
        var root = TestExportData.CdcStations("00044");
        byte[] captured = null!;

        _xml.Setup(s => s.SerializeAsync(root, It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<CdcStationList, Stream, CancellationToken>((_, stream, _) => stream.WriteAsync(Encoding.UTF8.GetBytes("<x/>")).AsTask());

        _destination.Setup(d => d.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()))
            .Callback<ReadOnlyMemory<byte>, CommandContext, CancellationToken>((bytes, _, _) => captured = bytes.ToArray())
            .Returns(Task.CompletedTask);

        await _subject.WriteAsync(root, Context(OutputFormat.Xml), CancellationToken.None);

        Encoding.UTF8.GetString(captured!).Should().Be("<x/>");
        _destination.Verify(d => d.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CommandContext>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [TestMethod]
    public async Task WriteAsync_SerializerFailsMidway_NothingWritten()
    {
        var root = TestExportData.CdcStations("00044");

        _json.Setup(s => s.SerializeAsync(root, It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<CdcStationList, Stream, CancellationToken>(async (_, stream, _) =>
            {
                await stream.WriteAsync(Encoding.UTF8.GetBytes("{\"partial\":"));
                throw new InvalidOperationException("broken");
            });

        Func<Task> act = async () => await _subject.WriteAsync(root, Context(OutputFormat.Json), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _stdout.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public async Task WriteAsync_MqttOutput_Rejected()
    {
        var root = TestExportData.CdcStations("00044");

        Func<Task> act = async () => await _subject.WriteAsync(root, Context(OutputFormat.Mqtt), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
