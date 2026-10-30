using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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

    // The longest time the forced termination waits for its log line; a blocked standard error never keeps the process alive (CL-18).
    private static readonly TimeSpan LogBound = TimeSpan.FromSeconds(1);

    private readonly object _gate = new();
    private readonly CancellationTokenSource _source = new();
    private readonly IDisposable[] _registrations;
    private readonly IProcessTerminator _terminator;
    private readonly TimeProvider _timeProvider;
    private readonly bool _holdTerminationSignal;
    // Set by Dispose; a held Windows SIGTERM handler waits for it, so it is never disposed.
    private readonly ManualResetEventSlim _finished = new();
    private ITimer? _graceTimer;
    private TimeSpan _gracePeriod = DefaultGracePeriod;
    private ILogger _logger = NullLogger.Instance;
    private int _signals;
    private bool _disposed;
    // True once a handler holds its thread; its registration is then never removed.
    private bool _holding;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShutdownSignal"/> class.
    /// </summary>
    /// <param name="registrar">The signal registrar.</param>
    /// <param name="terminator">The process terminator.</param>
    /// <param name="timeProvider">The time provider for the grace period.</param>
    /// <param name="holdTerminationSignal">True on Windows: SIGTERM stands for the shutdown and logoff events there, and Windows ends the process as soon as the handler returns, so the handler holds its thread until the command has finished or the grace period ends the process (CL-18).</param>
    public ShutdownSignal(IPosixSignalRegistrar registrar, IProcessTerminator terminator, TimeProvider timeProvider, bool holdTerminationSignal)
    {
        ArgumentNullException.ThrowIfNull(registrar);
        ArgumentNullException.ThrowIfNull(terminator);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _terminator = terminator;
        _timeProvider = timeProvider;
        _holdTerminationSignal = holdTerminationSignal;

        // Every field the handler uses is assigned before the first registration, because a signal can arrive during the registration.
        _registrations = new[] { registrar.Register(PosixSignal.SIGINT, OnSignal), registrar.Register(PosixSignal.SIGTERM, OnSignal) };
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

    /// <inheritdoc/>
    public void ReleaseLogger()
    {
        lock (_gate)
        {
            _logger = NullLogger.Instance;
        }
    }

    private void OnSignal(PosixSignalContext context)
    {
        bool first;
        bool hold;
        ILogger logger;
        TimeSpan gracePeriod;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            // The command stops itself and the process ends with exit code 4; the default termination would skip the cleanup (CL-18).
            context.Cancel = true;
            first = ++_signals == 1;
            logger = _logger;
            gracePeriod = _gracePeriod;
            if (first)
            {
                _graceTimer = _timeProvider.CreateTimer(_ => Terminate("The command did not stop within the shutdown grace period; the process ends now."), null, gracePeriod, Timeout.InfiniteTimeSpan);
            }

            // On Windows SIGTERM is the shutdown or logoff event: the process ends as soon as this handler returns (CL-18).
            hold = _holdTerminationSignal && context.Signal == PosixSignal.SIGTERM;
            _holding |= hold;
        }

        if (!first)
        {
            Terminate("A second shutdown signal was received; the process ends now.");
            return;
        }

        // Cancellation and the grace timer are in place before anything is logged, so a blocked standard error cannot stop them (CL-18).
        _source.Cancel();
        TryLog(logger, l => l.LogWarning(new EventId(0, CliErrorIds.Cancelled), "Shutdown signal {Signal} received; the command stops within {GracePeriod}.", context.Signal, gracePeriod));
        if (hold)
        {
            // Held until the command has finished (Dispose) or the grace timer ends the process.
            _finished.Wait();
        }
    }

    private void Terminate(string message)
    {
        ILogger logger;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            logger = _logger;
        }

        // The log line gets a bounded chance; the process ends even when standard error blocks (CL-18).
        Task.Run(() => TryLog(logger, l => l.LogError(new EventId(0, CliErrorIds.Cancelled), "{Message}", message))).Wait(LogBound);
        _terminator.Terminate(ExitCodes.Cancelled);
    }

    private static void TryLog(ILogger logger, Action<ILogger> write)
    {
        try
        {
            write(logger);
        }
        catch (Exception exception) when (exception is ObjectDisposedException or IOException)
        {
            // Standard error may already be closed when the command has ended; the signal handling never fails because of its log.
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        bool holding;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _graceTimer?.Dispose();
            holding = _holding;
        }

        _finished.Set();
        if (holding)
        {
            // Windows does not unregister a handler while it runs; like the .NET host, the registrations are left to the ending process.
            return;
        }

        foreach (var registration in _registrations)
        {
            registration.Dispose();
        }
    }
}
