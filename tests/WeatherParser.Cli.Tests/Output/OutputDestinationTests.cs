using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherParser.Cli;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Output;

namespace WeatherParser.Cli.Tests.Output;

[TestClass]
[TestCategory("T-P3-12")]
public sealed class OutputDestinationTests
{
    private MockRepository _mocks = null!;
    private Mock<IFileOperations> _files = null!;
    private StringWriter _stdout = null!;
    private StringWriter _stderr = null!;
    private ILoggerFactory _loggerFactory = null!;
    private OutputDestination _subject = null!;

    private static readonly string Directory = Path.Combine(Path.GetTempPath(), "wp-out");
    private static readonly string Destination = Path.Combine(Directory, "out.json");
    private static readonly byte[] Document = Encoding.UTF8.GetBytes("{\"a\":\"Ä\"}");

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _files = _mocks.Create<IFileOperations>();
        _stdout = new StringWriter();
        _stderr = new StringWriter();
        _loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new StandardErrorLoggerProvider(_stderr, TimeProvider.System, new LogLineFormatter(), LogLevel.Information, "ParseDWD")));
        _subject = new OutputDestination(_files.Object, _loggerFactory.CreateLogger<OutputDestination>());
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
        _loggerFactory.Dispose();
        _stdout.Dispose();
        _stderr.Dispose();
    }

    [TestMethod]
    public void Check_NoOutputFile_DoesNothing()
    {
        _subject.Check(new CommonOptions(OutputFormat.Json, null, null, false));
    }

    [TestMethod]
    public async Task Check_ExistingDestination_OutputExists()
    {
        _files.Setup(f => f.GetFullPath("out.json")).Returns(Destination);
        _files.Setup(f => f.FileExists(Destination)).Returns(true);

        _subject.Invoking(s => s.Check(new CommonOptions(OutputFormat.Json, "out.json", null, false))).Should().Throw<CliException>().Which.ErrorId.Should().Be("CLI-OUTPUT-EXISTS");
    }

    [TestMethod]
    public async Task Check_MissingDirectory_OutputFailed()
    {
        _files.Setup(f => f.GetFullPath("out.json")).Returns(Destination);
        _files.Setup(f => f.FileExists(Destination)).Returns(false);
        _files.Setup(f => f.DirectoryExists(Directory)).Returns(false);

        _subject.Invoking(s => s.Check(new CommonOptions(OutputFormat.Json, "out.json", null, false))).Should().Throw<CliException>().Which.ErrorId.Should().Be("CLI-OUTPUT-FAILED");
    }

    [TestMethod]
    public async Task Check_NewFileInExistingDirectory_Accepted()
    {
        _files.Setup(f => f.GetFullPath("out.json")).Returns(Destination);
        _files.Setup(f => f.FileExists(Destination)).Returns(false);
        _files.Setup(f => f.DirectoryExists(Directory)).Returns(true);

        _subject.Check(new CommonOptions(OutputFormat.Json, "out.json", null, false));
    }

    [TestMethod]
    public async Task WriteAsync_NoOutputFile_WholeDocumentToStdout()
    {
        var context = new CommandContext(new CommonOptions(OutputFormat.Json, null, null, false), null, _stdout);

        await _subject.WriteAsync(Document, context, CancellationToken.None);

        _stdout.ToString().Should().Be("{\"a\":\"Ä\"}");
    }

    [TestMethod]
    public async Task WriteAsync_File_TemporaryFileInSameDirectoryThenMoved()
    {
        _files.Setup(f => f.GetFullPath("out.json")).Returns(Destination);
        string? temporary = null;
        byte[]? written = null;
        _files.Setup(f => f.WriteNewAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Callback<string, ReadOnlyMemory<byte>, CancellationToken>((path, content, _) =>
            {
                temporary = path;
                written = content.ToArray();
            })
            .Returns(Task.CompletedTask);
        _files.Setup(f => f.Move(It.IsAny<string>(), Destination));

        await _subject.WriteAsync(Document, new CommandContext(new CommonOptions(OutputFormat.Json, "out.json", null, false), null, _stdout), CancellationToken.None);

        temporary.Should().MatchRegex("^" + Regex.Escape(Destination) + @"\.[0-9a-f]{32}\.tmp$");
        written.Should().Equal(Document);
        _files.Verify(f => f.Move(temporary!, Destination), Times.Once());
        _stdout.ToString().Should().BeEmpty();
    }

    [TestMethod]
    public async Task WriteAsync_File_DestinationCreatedDuringRun_NotOverwritten_TemporaryDeleted()
    {
        _files.Setup(f => f.GetFullPath("out.json")).Returns(Destination);
        _files.Setup(f => f.WriteNewAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _files.Setup(f => f.Move(It.IsAny<string>(), Destination)).Throws(new IOException("exists"));
        _files.Setup(f => f.FileExists(Destination)).Returns(true);
        _files.Setup(f => f.Delete(It.Is<string>(p => p.EndsWith(".tmp")))).Verifiable();

        Func<Task> act = () => _subject.WriteAsync(Document, new CommandContext(new CommonOptions(OutputFormat.Json, "out.json", null, false), null, _stdout), CancellationToken.None);

        (await act.Should().ThrowAsync<CliException>()).Which.ErrorId.Should().Be("CLI-OUTPUT-EXISTS");
    }

    [TestMethod]
    public async Task WriteAsync_File_WriteFailure_OutputFailed_TemporaryDeleted()
    {
        _files.Setup(f => f.GetFullPath("out.json")).Returns(Destination);
        _files.Setup(f => f.WriteNewAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("disk full"));
        _files.Setup(f => f.Delete(It.Is<string>(p => p.EndsWith(".tmp")))).Verifiable();

        Func<Task> act = () => _subject.WriteAsync(Document, new CommandContext(new CommonOptions(OutputFormat.Json, "out.json", null, false), null, _stdout), CancellationToken.None);

        (await act.Should().ThrowAsync<CliException>()).Which.ErrorId.Should().Be("CLI-OUTPUT-FAILED");
    }

    [TestMethod]
    public async Task WriteAsync_File_MoveDenied_OutputFailed()
    {
        _files.Setup(f => f.GetFullPath("out.json")).Returns(Destination);
        _files.Setup(f => f.WriteNewAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _files.Setup(f => f.Move(It.IsAny<string>(), Destination)).Throws(new UnauthorizedAccessException("denied"));
        _files.Setup(f => f.FileExists(Destination)).Returns(false);
        _files.Setup(f => f.Delete(It.IsAny<string>())).Verifiable();

        Func<Task> act = () => _subject.WriteAsync(Document, new CommandContext(new CommonOptions(OutputFormat.Json, "out.json", null, false), null, _stdout), CancellationToken.None);

        (await act.Should().ThrowAsync<CliException>()).Which.ErrorId.Should().Be("CLI-OUTPUT-FAILED");
    }

    [TestMethod]
    public async Task WriteAsync_File_Cancelled_TemporaryDeleted_Rethrown()
    {
        _files.Setup(f => f.GetFullPath("out.json")).Returns(Destination);
        _files.Setup(f => f.WriteNewAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>())).Throws(new OperationCanceledException());
        _files.Setup(f => f.Delete(It.IsAny<string>())).Verifiable();

        Func<Task> act = () => _subject.WriteAsync(Document, new CommandContext(new CommonOptions(OutputFormat.Json, "out.json", null, false), null, _stdout), CancellationToken.None);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task WriteAsync_File_TemporaryDeleteFails_WarningWithPath()
    {
        _files.Setup(f => f.GetFullPath("out.json")).Returns(Destination);
        _files.Setup(f => f.WriteNewAsync(It.IsAny<string>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("disk full"));
        _files.Setup(f => f.Delete(It.IsAny<string>())).Throws(new IOException("locked"));

        Func<Task> act = () => _subject.WriteAsync(Document, new CommandContext(new CommonOptions(OutputFormat.Json, "out.json", null, false), null, _stdout), CancellationToken.None);

        (await act.Should().ThrowAsync<CliException>()).Which.ErrorId.Should().Be("CLI-OUTPUT-FAILED");
        _stderr.ToString().Should().Contain(" WARNING CLI-OUTPUT-FAILED ");
        _stderr.ToString().Should().Contain(".tmp");
    }

    private CommandContext Context(string? outputFile) => new(new CommonOptions(OutputFormat.Json, outputFile, null, false), null, _stdout);
}
