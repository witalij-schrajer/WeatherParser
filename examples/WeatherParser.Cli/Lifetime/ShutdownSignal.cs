using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WeatherParser.Cli.Diagnostics;

namespace WeatherParser.Cli.Lifetime;

/// <summary>
/// Registers SIGINT and SIGTERM at construction: the first signal cancels the token and the default termination, a second signal or the end of the grace period ends the process with exit code 4 (CL-18, REQ-007).
/// </summary>
public sealed class ShutdownSignal : IShutdownSignal
{
    /// <summary>
    /// The grace period until Configure is called (CL-18 default).
    /// </summary>
    public static readonly TimeSpan DefaultGracePeriod = TimeSpan.FromSeconds(5);

    private readonly object _gate = new();
    private readonly CancellationTokenSource _source = new();
    private readonly IDisposable[] _registrations;
    private readonly IProcessTerminator _terminator;
    private readonly TimeProvider _timeProvider;
    private ITimer? _graceTimer;
    private TimeSpan _gracePeriod = DefaultGracePeriod;
    private ILogger _logger = NullLogger.Instance;
    private int _signals;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShutdownSignal"/> class.
    /// </summary>
    /// <param name="registrar">The signal registrar.</param>
    /// <param name="terminator">The process terminator.</param>
    /// <param name="timeProvider">The time provider for the grace period.</param>
    public ShutdownSignal(IPosixSignalRegistrar registrar, IProcessTerminator terminator, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(registrar);
        ArgumentNullException.ThrowIfNull(terminator);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _registrations = new[] { registrar.Register(PosixSignal.SIGINT, OnSignal), registrar.Register(PosixSignal.SIGTERM, OnSignal) };
        _terminator = terminator;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc/>
    public CancellationToken Token => _source.Token;

    /// <inheritdoc/>
    public void Configure(TimeSpan gracePeriod, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        lock (_gate)
        {
            _gracePeriod = gracePeriod;
            _logger = logger;
        }
    }

    private void OnSignal(PosixSignalContext context)
    {
        bool first;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            // The command stops itself and the process ends with exit code 4; the default termination would skip the cleanup (CL-18).
            context.Cancel = true;
            first = ++_signals == 1;
            if (first)
            {
                _logger.LogWarning(new EventId(0, CliErrorIds.Cancelled), "Shutdown signal {Signal} received; the command stops within {GracePeriod}.", context.Signal, _gracePeriod);
                _graceTimer = _timeProvider.CreateTimer(_ => Terminate("The command did not stop within the shutdown grace period; the process ends now."), null, _gracePeriod, Timeout.InfiniteTimeSpan);
            }
        }

        if (first)
        {
            // Cancelled outside the lock: the token's callbacks may run continuations of the command on this thread.
            _source.Cancel();
            return;
        }

        Terminate("A second shutdown signal was received; the process ends now.");
    }

    private void Terminate(string message)
    {
        ILogger logger;
        bool disposed;
        lock (_gate)
        {
            logger = _logger;
            disposed = _disposed;
        }

        if (disposed)
        {
            return;
        }

        logger.LogError(new EventId(0, CliErrorIds.Cancelled), "{Message}", message);
        _terminator.Terminate(ExitCodes.Cancelled);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _graceTimer?.Dispose();
            foreach (var registration in _registrations)
            {
                registration.Dispose();
            }
        }
    }
}
