using System.IO;

namespace WeatherParser.Cli.Configuration;

/// <summary>
/// The owner and permission bits of an opened file on Linux, read for the credential-file check (CL-06).
/// </summary>
/// <param name="OwnerId">The user ID of the file owner.</param>
/// <param name="Mode">The permission bits of the file.</param>
public readonly record struct UnixFileStatus(uint OwnerId, UnixFileMode Mode);
