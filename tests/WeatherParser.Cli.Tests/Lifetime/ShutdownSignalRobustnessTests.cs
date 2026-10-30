using System;
using System.Collections.Generic;
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
    private MockRepository _mocks = null!;
    private Mock<IPosixSignalRegistrar> _registrar = null!;
    private Mock<IProcessTerminator> _terminator = null!;
    private Mock<IDisposable> _registration = null!;
    private Dictionary<PosixSignal, Action<PosixSignalContext>> _handlers = null!;
    private ManualTimeProvider _time = null!;
    private StringWriter _stderr = null!;
    private ILoggerFactory _loggerFactory = null!;
    private int _terminations;

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _registrar = _mocks.Create<IPosixSignalRegistrar>();
        _terminator = _mocks.Create<IProcessTerminator>();
        _registration = _mocks.Create<IDisposable>();
        _handlers = new Dictionary<PosixSignal, Action<PosixSignalContext>>();
        _time = new ManualTimeProvider();
        _stderr = new StringWriter();
        _loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new StandardErrorLoggerProvider(_stderr, TimeProvider.System, new LogLineFormatter(), LogLevel.Information, "ParseDWD")));
        _terminations = 0;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
        _loggerFactory.Dispose();
        _stderr.Dispose();
    }

    // Registers both signals; a signal raised by the test goes through the captured handler.
    private ShutdownSignal Create(bool holdTerminationSignal)
    {
        _registrar.Setup(r => r.Register(It.IsAny<PosixSignal>(), It.IsAny<Action<PosixSignalContext>>()))
            .Callback<PosixSignal, Action<PosixSignalContext>>((signal, handler) => _handlers[signal] = handler)
            .Returns(_registration.Object);
        return new ShutdownSignal(_registrar.Object, _terminator.Object, _time, holdTerminationSignal);
    }

    private PosixSignalContext Raise(PosixSignal signal)
    {
        var context = new PosixSignalContext(signal);
        _handlers[signal](context);
        return context;
    }

    private void ExpectTermination() => _terminator.Setup(t => t.Terminate(ExitCodes.Cancelled)).Callback(() => Interlocked.Increment(ref _terminations));

    [TestMethod]
    [Timeout(30000)]
    public void Signal_DuringRegistration_HandledWithoutFailure()
    {
        _registrar.Setup(r => r.Register(It.IsAny<PosixSignal>(), It.IsAny<Action<PosixSignalContext>>()))
            .Callback<PosixSignal, Action<PosixSignalContext>>((signal, handler) =>
            {
                if (signal == PosixSignal.SIGINT)
                {
                    handler(new PosixSignalContext(PosixSignal.SIGINT));
                }
            })
            .Returns(_registration.Object);
        var subject = new ShutdownSignal(_registrar.Object, _terminator.Object, _time, false);
        subject.Token.IsCancellationRequested.Should().BeTrue();
        _time.ActiveTimers.Should().Be(1);
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task FirstSignal_LoggerBlocks_TokenCancelledAndGraceTimerStarted()
    {
        var subject = Create(false);
        var logger = new BlockingLogger();
        subject.Configure(TimeSpan.FromSeconds(5), logger);
        var raising = Task.Run(() => Raise(PosixSignal.SIGINT));
        logger.Entered.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
        subject.Token.IsCancellationRequested.Should().BeTrue();
        _time.ActiveTimers.Should().Be(1);
        logger.Release.Set();
        await raising;
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task GracePeriodExpired_LoggerBlocks_StillTerminates()
    {
        ExpectTermination();
        var subject = Create(false);
        var logger = new BlockingLogger();
        subject.Configure(TimeSpan.FromSeconds(2), logger);
        var raising = Task.Run(() => Raise(PosixSignal.SIGINT));
        logger.Entered.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
        _time.Advance(TimeSpan.FromSeconds(2));
        _terminations.Should().Be(1);
        logger.Release.Set();
        await raising;
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task SecondSignal_LoggerBlocks_StillTerminates()
    {
        ExpectTermination();
        var subject = Create(false);
        var logger = new BlockingLogger();
        subject.Configure(TimeSpan.FromSeconds(30), logger);
        var first = Task.Run(() => Raise(PosixSignal.SIGINT));
        logger.Entered.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
        var second = Task.Run(() => Raise(PosixSignal.SIGTERM));
        (await Task.WhenAny(second, Task.Delay(TimeSpan.FromSeconds(10)))).Should().BeSameAs(second);
        _terminations.Should().Be(1);
        logger.Release.Set();
        await first;
    }

    [TestMethod]
    [Timeout(30000)]
    public void LogOutputClosed_SignalsStillHandled()
    {
        ExpectTermination();
        var subject = Create(false);
        subject.Configure(TimeSpan.FromSeconds(5), new ThrowingLogger());
        var first = Raise(PosixSignal.SIGINT);
        first.Cancel.Should().BeTrue();
        subject.Token.IsCancellationRequested.Should().BeTrue();
        Raise(PosixSignal.SIGTERM);
        _terminations.Should().Be(1);
    }

    [TestMethod]
    [Timeout(30000)]
    public void ReleaseLogger_LaterSignalNotLogged()
    {
        var subject = Create(false);
        subject.Configure(TimeSpan.FromSeconds(5), _loggerFactory.CreateLogger("WeatherParser.Cli"));
        subject.ReleaseLogger();
        Raise(PosixSignal.SIGINT);
        subject.Token.IsCancellationRequested.Should().BeTrue();
        _stderr.ToString().Should().BeEmpty();
    }

    [TestMethod]
    [Timeout(30000)]
    public async Task HoldTerminationSignal_SigtermHeldUntilDispose_RegistrationsKept()
    {
        var subject = Create(true);
        var raising = Task.Run(() => Raise(PosixSignal.SIGTERM));
        await Task.Delay(200);
        raising.IsCompleted.Should().BeFalse();
        subject.Token.IsCancellationRequested.Should().BeTrue();
        subject.Dispose();
        (await Task.WhenAny(raising, Task.Delay(TimeSpan.FromSeconds(10)))).Should().BeSameAs(raising);
        (await raising).Cancel.Should().BeTrue();
    }

    [TestMethod]
    [Timeout(30000)]
    public void HoldTerminationSignal_SigintNotHeld_RegistrationsDisposed()
    {
        _registration.Setup(r => r.Dispose());
        var subject = Create(true);
        Raise(PosixSignal.SIGINT).Cancel.Should().BeTrue();
        subject.Dispose();
    }

    [TestMethod]
    [Timeout(30000)]
    public void NotHolding_SigtermNotHeld()
    {
        var subject = Create(false);
        Raise(PosixSignal.SIGTERM).Cancel.Should().BeTrue();
        subject.Token.IsCancellationRequested.Should().BeTrue();
    }
}
