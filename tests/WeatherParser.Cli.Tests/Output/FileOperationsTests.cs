using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Cli.Output;
using WeatherParser.Cli.Tests.Commands;

namespace WeatherParser.Cli.Tests.Output;

[TestClass]
[TestCategory("T-P3-12")]
public sealed class FileOperationsTests
{
    private string _directory = null!;
    private FileOperations _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "weatherparser-fileops-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _subject = new FileOperations();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    [TestMethod]
    public async Task WriteNewAsync_WritesExactBytesWithoutByteOrderMark()
    {
        var content = Encoding.UTF8.GetBytes("{\"a\":\"Ä\"}");
        var path = Path.Combine(_directory, "test.json");

        await _subject.WriteNewAsync(path, new ReadOnlyMemory<byte>(content), default);

        File.ReadAllBytes(path).Should().Equal(content);
    }

    [TestMethod]
    public async Task WriteNewAsync_ExistingFile_FailsAndKeepsIt()
    {
        var path = Path.Combine(_directory, "test.json");
        File.WriteAllText(path, "old");

        Func<Task> act = () => _subject.WriteNewAsync(path, new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("new")), default);

        await act.Should().ThrowAsync<IOException>();
        File.ReadAllText(path).Should().Be("old");
    }

    [TestMethod]
    public async Task Move_ExistingDestination_FailsAndKeepsBoth()
    {
        var source = Path.Combine(_directory, "new");
        var destination = Path.Combine(_directory, "old");

        File.WriteAllText(source, "source content");
        File.WriteAllText(destination, "old content");

        Action act = () => _subject.Move(source, destination);

        act.Should().Throw<IOException>();
        File.ReadAllText(destination).Should().Be("old content");
        File.ReadAllText(source).Should().Be("source content");
    }

    [TestMethod]
    public async Task Move_NewDestination_Moved()
    {
        var source = Path.Combine(_directory, "source");
        var destination = Path.Combine(_directory, "destination");

        File.WriteAllText(source, "content");

        _subject.Move(source, destination);

        File.Exists(destination).Should().BeTrue();
        File.ReadAllText(destination).Should().Be("content");
        File.Exists(source).Should().BeFalse();
    }

    [TestMethod]
    public async Task Delete_RemovesFile()
    {
        var file = Path.Combine(_directory, "test.txt");

        File.WriteAllText(file, "content");
        _subject.Delete(file);

        File.Exists(file).Should().BeFalse();
    }

    [TestMethod]
    public async Task FileExists_DirectoryExists_ReportTheFilesystem()
    {
        var file = Path.Combine(_directory, "test.txt");

        File.WriteAllText(file, "content");

        _subject.FileExists(file).Should().BeTrue();
        _subject.DirectoryExists(_directory).Should().BeTrue();
        _subject.FileExists(Path.Combine(_directory, "missing")).Should().BeFalse();
        _subject.DirectoryExists(Path.Combine(_directory, "missing")).Should().BeFalse();

        _subject.Delete(file);

        _subject.FileExists(file).Should().BeFalse();
    }

    [TestMethod]
    public async Task GetFullPath_RelativePath_ResolvedAgainstWorkingDirectory()
    {
        var result = _subject.GetFullPath("out.json");

        result.Should().Be(Path.Combine(Environment.CurrentDirectory, "out.json"));
    }

    [TestMethod]
    public async Task WriteAsync_IdenticalInput_IdenticalBytes()
    {
        var content = Encoding.UTF8.GetBytes("{\"a\":\"Ä\"}");
        var first = Path.Combine(_directory, "first.json");
        var second = Path.Combine(_directory, "second.json");

        await _subject.WriteNewAsync(first, new ReadOnlyMemory<byte>(content), default);
        await _subject.WriteNewAsync(second, new ReadOnlyMemory<byte>(content), default);

        File.ReadAllBytes(first).Should().Equal(File.ReadAllBytes(second));
    }

    [TestMethod]
    public async Task ReadAllText_ReadsUtf8File()
    {
        var path = Path.Combine(_directory, "test.txt");
        File.WriteAllText(path, "Grüße", Encoding.UTF8);

        var result = _subject.ReadAllText(path);

        result.Should().Be("Grüße");
    }
}
