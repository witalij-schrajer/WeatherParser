using System;
using System.Runtime.InteropServices;

namespace WeatherParser.Cli.Lifetime;

/// <summary>
/// Registers POSIX signal handlers with PosixSignalRegistration (CL-18).
/// </summary>
public sealed class PosixSignalRegistrar : IPosixSignalRegistrar
{
    /// <inheritdoc/>
    public IDisposable Register(PosixSignal signal, Action<PosixSignalContext> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return PosixSignalRegistration.Create(signal, handler);
    }
}
