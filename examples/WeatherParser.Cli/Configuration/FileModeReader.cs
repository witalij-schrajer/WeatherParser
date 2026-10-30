using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace WeatherParser.Cli.Configuration;

/// <summary>
/// Reads the owner and permission bits of an opened file through Linux statx on its descriptor, so that the credential file is checked and read through the same handle; on Windows no Unix owner and modes exist and no status is read (CL-06).
/// </summary>
public sealed class FileModeReader : IFileModeReader
{
    // statx(2): AT_EMPTY_PATH makes statx describe the descriptor itself; STATX_MODE and STATX_UID request the mode and the owner.
    private const int AtEmptyPath = 0x1000;
    private const uint StatxMode = 0x2;
    private const uint StatxUid = 0x8;

    // struct statx has the same layout on every Linux architecture: stx_mask at offset 0, stx_uid at 20, stx_mode at 28; 256 bytes in total.
    private const int StatxSize = 256;
    private const int UidOffset = 20;
    private const int ModeOffset = 28;
    private const int PermissionBits = 0xFFF;

    /// <inheritdoc />
    public UnixFileStatus? Read(SafeFileHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        if (OperatingSystem.IsWindows())
        {
            return null;
        }

        var buffer = new byte[StatxSize];
        var added = false;
        handle.DangerousAddRef(ref added);
        try
        {
            if (Statx((int)handle.DangerousGetHandle(), string.Empty, AtEmptyPath, StatxMode | StatxUid, buffer) != 0)
            {
                throw new IOException($"The file status cannot be read (error {Marshal.GetLastPInvokeError()}).");
            }
        }
        finally
        {
            if (added)
            {
                handle.DangerousRelease();
            }
        }

        if ((BitConverter.ToUInt32(buffer, 0) & (StatxMode | StatxUid)) != (StatxMode | StatxUid))
        {
            throw new IOException("The file system did not report the owner and mode of the file.");
        }

        return new UnixFileStatus(BitConverter.ToUInt32(buffer, UidOffset), (UnixFileMode)(BitConverter.ToUInt16(buffer, ModeOffset) & PermissionBits));
    }

    /// <inheritdoc />
    public uint GetEffectiveUserId() => OperatingSystem.IsWindows()
        ? throw new PlatformNotSupportedException("Windows has no Unix user IDs.")
        : NativeGetEffectiveUserId();

    [DllImport("libc", EntryPoint = "statx", SetLastError = true)]
    private static extern int Statx(int directoryDescriptor, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags, uint mask, byte[] buffer);

    [DllImport("libc", EntryPoint = "geteuid")]
    private static extern uint NativeGetEffectiveUserId();
}
