using System;
using System.Runtime.InteropServices;

namespace WeatherParser.Cli.Tests;

/// <summary>
/// Sends POSIX signals to a process on Linux through libc kill; test infrastructure for CL-18.
/// </summary>
internal static class PosixSignalSender
{
    public const int Sigint = 2;

    public const int Sigterm = 15;

    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int sig);

    /// <summary>
    /// Sends the specified signal to the specified process.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <param name="signal">The signal number.</param>
    public static void Send(int processId, int signal)
    {
        if (kill(processId, signal) != 0)
        {
            throw new InvalidOperationException($"Sending signal {signal} to process {processId} failed with error {Marshal.GetLastPInvokeError()}.");
        }
    }
}
