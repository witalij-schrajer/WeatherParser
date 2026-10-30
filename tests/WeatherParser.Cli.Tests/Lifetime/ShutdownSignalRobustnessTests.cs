using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Lifetime;
using WeatherParser.Cli.Tests.Mqtt;

namespace WeatherParser.Cli.Tests.Lifetime;

[TestClass]
[TestCategory("T-P3-18")]
public sealed class ShutdownSignalRobustnessTests
{
    /// <summary>
    /// Tests the robustness of shutdown signal handling, ensuring that signals are handled correctly under various conditions.
    /// </summary>
    private MockRepository _mocks = null!;
    private Mock<IPosixSignalRegistrar> _registrar = null!;
    private Mock<IPosixSignalRegistrar> _holdingRegistrar = null!;
    private Mock<IProcessTerminator> _terminator = null!;
    private Mock<IDisposable> _registration = null!;
    private Dictionary<PosixSignal, Action<PosixSignalContext>> _handlers = null!;
    private Dictionary<PosixSignal, Action<PosixSignalContext>> _holdingHandlers = null!;
    private ManualTimeProvider _time = null!;
    private StringWriter _stderr = null!;
    private ILoggerFactory _loggerFactory = null!;
    private int _terminations;
    private ShutdownSignal _subject = null!;
    private ShutdownSignal _holdingSubject = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _registrar = _mocks.Create<IPosixSignalRegistrar>();
        _holdingRegistrar = _mocks.Create<IPosixSignalRegistrar>();
        _terminator = _mocks.Create<IProcessTerminator>();
        _registration = _mocks.Create<IDisposable>();
        _handlers = new Dictionary<PosixSignal, Action<PosixSignalContext>>();
        _holdingHandlers = new Dictionary<PosixSignal, Action<PosixSignalContext>>();
        _time = new ManualTimeProvider();
        _stderr = new StringWriter();
        _loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new StandardErrorLoggerProvider(_stderr, TimeProvider.System, new LogLineFormatter(), LogLevel.Information, "ParseDWD")));
        _terminations = 0;
        _registrar.Setup(r => r.Register(It.IsAny<PosixSignal>(), It.IsAny<Action<PosixSignalContext>>()))
            .Callback<PosixSignal, Action<PosixSignalContext>>((signal, handler) => _handlers[signal] = handler)
            .Returns(_registration.Object);
        _holdingRegistrar.Setup(r => r.Register(It.IsAny<PosixSignal>(), It.IsAny<Action<PosixSignalContext>>()))
            .Callback<PosixSignal, Action<PosixSignalContext>>((signal, handler) => _holdingHandlers[signal] = handler)
            .Returns(_registration.Object);
        _subject = new ShutdownSignal(_registrar.Object, _terminator.Object, _time, false);
        _holdingSubject = new ShutdownSignal(_holdingRegistrar.Object, _terminator.Object, _time, true);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
        _loggerFactory.Dispose();
        _stderr.Dispose();
    }

    private PosixSignalContext Raise(PosixSignal signal) => Raise(_handlers, signal);

    private static PosixSignalContext Raise(Dictionary<PosixSignal, Action<PosixSignalContext>> handlers, PosixSignal signal)
    {
        var context = new PosixSignalContext(signal);
        handlers[signal](context);
        return context;
    }

    private void ExpectTermination() => _terminator.Setup(t => t.Terminate(ExitCodes.Cancelled)).Callback(() => Interlocked.Increment(ref _terminations));

    // A dedicated background thread, because a held handler may never return.
    private static Task<PosixSignalContext> RaiseOnOwnThread(Dictionary<PosixSignal, Action<PosixSignalContext>> handlers, PosixSignal signal) =>
        Task.Factory.StartNew(() => Raise(handlers, signal), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    /// <summary>
    /// Tests that a signal received during registration is handled without failure.
    /// </summary>
    [TestMethod]
    [Timeout(30000)]
    public void Signal_DuringRegistration_HandledWithoutFailure()
    {
        var registrar = _mocks.Create<IPosixSignalRegistrar>();
        registrar.Setup(r => r.Register(It.IsAny<PosixSignal>(), It.IsAny<Action<PosixSignalContext>>()))
            .Callback<PosixSignal, Action<PosixSignalContext>>((signal, handler) =>
            {
                if (signal == PosixSignal.SIGINT)
                {
                    handler(new PosixSignalContext(PosixSignal.SIGINT));
                }
            })
            .Returns(_registration.Object);
        var subject = new ShutdownSignal(registrar.Object, _terminator.Object, _time, false);
        subject.Token.IsCancellationRequested.Should().BeTrue();
        _time.ActiveTimers.Should().Be(1);
    }

    /// <summary>
    /// Tests that when the first signal is received and the logger blocks, the token is cancelled and the grace timer is started.
    /// </summary>
    [TestMethod]
    [Timeout(30000)]
    public async Task FirstSignal_LoggerBlocks_TokenCancelledAndGraceTimerStarted()
    {
        var logger = new BlockingLogger();
        _subject.Configure(TimeSpan.FromSeconds(5), logger);
        var raising = RaiseOnOwnThread(_handlers, PosixSignal.SIGINT);
        logger.Entered.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
        _subject.Token.IsCancellationRequested.Should().BeTrue();
        _time.ActiveTimers.Should().Be(1);
        logger.Release.Set();
        await raising;
    }

    /// <summary>
    /// Tests that when the grace period expires and the logger blocks, the process terminates without waiting for the log.
    /// </summary>
    [TestMethod]
    [Timeout(30000)]
    public async Task GracePeriodExpired_LoggerBlocks_TerminatesWithoutWaitingForTheLog()
    {
        ExpectTermination();
        var logger = new BlockingLogger();
        _subject.Configure(TimeSpan.FromSeconds(2), logger);
        var raising = RaiseOnOwnThread(_handlers, PosixSignal.SIGINT);
        logger.Entered.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
        var watch = Stopwatch.StartNew();
        _time.Advance(TimeSpan.FromSeconds(2));
        watch.Stop();
        _terminations.Should().Be(1);
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500));
        logger.Release.Set();
        await raising;
    }

    /// <summary>
    /// Tests that when a second signal is received and the logger blocks, the process terminates without waiting for the log.
    /// </summary>
    [TestMethod]
    [Timeout(30000)]
    public async Task SecondSignal_LoggerBlocks_TerminatesWithoutWaitingForTheLog()
    {
        ExpectTermination();
        var logger = new BlockingLogger();
        _subject.Configure(TimeSpan.FromSeconds(30), logger);
        var first = RaiseOnOwnThread(_handlers, PosixSignal.SIGINT);
        logger.Entered.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
        var watch = Stopwatch.StartNew();
        Raise(PosixSignal.SIGTERM);
        watch.Stop();
        _terminations.Should().Be(1);
        watch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500));
        logger.Release.Set();
        await first;
    }

    /// <summary>
    /// Tests that signals are still handled correctly when the log output is closed.
    /// </summary>
    [TestMethod]
    [Timeout(30000)]
    public void LogOutputClosed_SignalsStillHandled()
    {
        ExpectTermination();
        _subject.Configure(TimeSpan.FromSeconds(5), new ThrowingLogger());
        var first = Raise(PosixSignal.SIGINT);
        first.Cancel.Should().BeTrue();
        _subject.Token.IsCancellationRequested.Should().BeTrue();
        Raise(PosixSignal.SIGTERM);
        _terminations.Should().Be(1);
    }

    /// <summary>
    /// Tests that after releasing the logger, later signals are not logged.
    /// </summary>
    [TestMethod]
    [Timeout(30000)]
    public void ReleaseLogger_LaterSignalNotLogged()
    {
        _subject.Configure(TimeSpan.FromSeconds(5), _loggerFactory.CreateLogger("WeatherParser.Cli"));
        _subject.ReleaseLogger();
        Raise(PosixSignal.SIGINT);
        _subject.Token.IsCancellationRequested.Should().BeTrue();
        _stderr.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a held SIGTERM signal stays held after Dispose and the registrations are kept.
    /// </summary>
    [TestMethod]
    [Timeout(30000)]
    public async Task HoldTerminationSignal_SigtermStaysHeldAfterDispose_RegistrationsKept()
    {
        var raising = RaiseOnOwnThread(_holdingHandlers, PosixSignal.SIGTERM);
        await Task.Delay(200);
        raising.IsCompleted.Should().BeFalse();
        _holdingSubject.Token.IsCancellationRequested.Should().BeTrue();
        _holdingSubject.Dispose();
        await Task.Delay(300);
        raising.IsCompleted.Should().BeFalse("the handler is held until the process ends, so Windows cannot end the process before Main returns its exit code");
    }

    /// <summary>
    /// Tests that a SIGINT signal is not held after Dispose and the registrations are disposed.
    /// </summary>
    [TestMethod]
    [Timeout(30000)]
    public void HoldTerminationSignal_SigintNotHeld_RegistrationsDisposed()
    {
        _registration.Setup(r => r.Dispose());
        Raise(_holdingHandlers, PosixSignal.SIGINT).Cancel.Should().BeTrue();
        _holdingSubject.Dispose();
    }

    /// <summary>
    /// Tests that a SIGTERM signal is not held when not holding termination signals.
    /// </summary>
    [TestMethod]
    [Timeout(30000)]
    public void NotHolding_SigtermNotHeld()
    {
        Raise(PosixSignal.SIGTERM).Cancel.Should().BeTrue();
        _subject.Token.IsCancellationRequested.Should().BeTrue();
    }
}
