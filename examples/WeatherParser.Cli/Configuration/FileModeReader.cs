using System;
using System.IO;
using System.Runtime.InteropServices;

namespace WeatherParser.Cli.Configuration;

/// <summary>
/// Reads whether a file grants any permission to group or others; on Windows no Unix modes exist and the result is false (CL-06).
/// </summary>
public sealed class FileModeReader : IFileModeReader
{
    private const UnixFileMode GroupOrOther = UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
        | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

    /// <inheritdoc />
    public bool HasGroupOrOtherAccess(string path) =>
        !OperatingSystem.IsWindows() && (File.GetUnixFileMode(path) & GroupOrOther) != 0;
}
