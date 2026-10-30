using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Cli.Configuration;

namespace WeatherParser.Cli.Tests.Configuration;

[TestClass]
public sealed class FileModeReaderTests
{
    [TestMethod]
    public void HasGroupOrOtherAccess_ReflectsUnixMode()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.IsFalse(new FileModeReader().HasGroupOrOtherAccess("nonexistent"));
            return;
        }

        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            File.WriteAllText(path, "test");
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            Assert.IsFalse(new FileModeReader().HasGroupOrOtherAccess(path));

            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
            Assert.IsTrue(new FileModeReader().HasGroupOrOtherAccess(path));

            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.OtherRead);
            Assert.IsTrue(new FileModeReader().HasGroupOrOtherAccess(path));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
