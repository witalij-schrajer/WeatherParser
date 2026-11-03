using Microsoft.Win32.SafeHandles;

namespace WeatherParser.Cli.Configuration;

/// <summary>
/// Reads the owner and permission bits of an opened file, so that the credential file is checked and read through the same handle (CL-06).
/// </summary>
public interface IFileModeReader
{
    /// <summary>
    /// Reads the owner and permission bits of the opened file.
    /// </summary>
    /// <param name="handle">The handle of the opened file.</param>
    /// <returns>The owner and permission bits, or null on Windows, where no Unix owner and modes exist.</returns>
    UnixFileStatus? Read(SafeFileHandle handle);

    /// <summary>
    /// Gets the effective user ID of the current process; not supported on Windows.
    /// </summary>
    /// <returns>The effective user ID.</returns>
    uint GetEffectiveUserId();
}
