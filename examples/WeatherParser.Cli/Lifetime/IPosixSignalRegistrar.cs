using System;
using System.Runtime.InteropServices;

namespace WeatherParser.Cli.Lifetime;

/// <summary>
/// Registers handlers for POSIX signals; the seam over PosixSignalRegistration (CL-18).
/// </summary>
public interface IPosixSignalRegistrar
{
    /// <summary>
    /// Registers a handler for the signal until the returned registration is disposed.
    /// </summary>
    /// <param name="signal">The signal to register.</param>
    /// <param name="handler">The handler to invoke when the signal occurs.</param>
    /// <returns>The registration.</returns>
    IDisposable Register(PosixSignal signal, Action<PosixSignalContext> handler);
}
