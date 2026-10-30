using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Cli.Configuration;

namespace WeatherParser.Cli.Tests.Configuration;

[TestClass]
[TestCategory("T-P3-07")]
public sealed class FileModeReaderTests
{
    private const UnixFileMode Private = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    private FileModeReader _reader = null!;
    private string _path = null!;

    [DllImport("libc", SetLastError = true)]
    private static extern int chown(string path, uint owner, uint group);

    [TestInitialize]
    public void Initialize()
    {
        _reader = new FileModeReader();
        _path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(_path, "test");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    /// <summary>
    /// Verifies that the owner and permission bits are read from the opened file (statx on the descriptor); on Windows there is no Unix status.
    /// </summary>
    [TestMethod]
    public void Read_OpenedFile_ReportsOwnerAndMode()
    {
        using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (OperatingSystem.IsWindows())
        {
            _reader.Read(stream.SafeFileHandle).Should().BeNull();
            return;
        }

        File.SetUnixFileMode(_path, Private);
        _reader.Read(stream.SafeFileHandle).Should().Be(new UnixFileStatus(_reader.GetEffectiveUserId(), Private));

        File.SetUnixFileMode(_path, Private | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
        var status = _reader.Read(stream.SafeFileHandle);
        status.Should().NotBeNull();
        status!.Value.Mode.Should().Be(File.GetUnixFileMode(_path));
        status.Value.Mode.Should().HaveFlag(UnixFileMode.GroupRead).And.HaveFlag(UnixFileMode.OtherRead);
    }

    /// <summary>
    /// Verifies the effective user ID against `id -u`; on Windows the call is not supported.
    /// </summary>
    [TestMethod]
    public void GetEffectiveUserId_MatchesIdCommand()
    {
        if (OperatingSystem.IsWindows())
        {
            FluentActions.Invoking(() => _reader.GetEffectiveUserId()).Should().Throw<PlatformNotSupportedException>();
            return;
        }

        using var process = Process.Start(new ProcessStartInfo("id", "-u") { RedirectStandardOutput = true, UseShellExecute = false })!;
        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        _reader.GetEffectiveUserId().Should().Be(uint.Parse(output, System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that a foreign owner is reported (needs root to give the file away; the owner comparison itself is covered by CredentialProviderTests).
    /// </summary>
    [TestMethod]
    public void Read_FileOwnedByAnotherUser_ReportsThatOwner()
    {
        if (OperatingSystem.IsWindows() || _reader.GetEffectiveUserId() != 0)
        {
            Assert.Inconclusive("Giving a file to another owner needs root on Linux; the owner comparison is covered with a test double by CredentialProviderTests.");
            return;
        }

        File.SetUnixFileMode(_path, Private);
        chown(_path, 65534, 65534).Should().Be(0);
        using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);
        _reader.Read(stream.SafeFileHandle).Should().Be(new UnixFileStatus(65534, Private));
    }
}
