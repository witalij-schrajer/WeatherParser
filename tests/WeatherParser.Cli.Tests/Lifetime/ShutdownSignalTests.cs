using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Lifetime;
using WeatherParser.Cli.Tests.Mqtt;

namespace WeatherParser.Cli.Tests.Lifetime;

/// <summary>
/// Tests the <see cref="ShutdownSignal"/> class.
/// </summary>
[TestClass]
[TestCategory("T-P3-18")]
public sealed class ShutdownSignalTests
{
    private MockRepository _mocks = null!;
    private Mock<IPosixSignalRegistrar> _registrar = null!;
    private Mock<IProcessTerminator> _terminator = null!;
    private Mock<IDisposable> _sigintRegistration = null!;
    private Mock<IDisposable> _sigtermRegistration = null!;
    private Dictionary<PosixSignal, Action<PosixSignalContext>> _handlers = null!;
    private ManualTimeProvider _time = null!;
    private StringWriter _stderr = null!;
    private ILoggerFactory _loggerFactory = null!;
    private int _terminations;
    private ShutdownSignal _subject = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _registrar = _mocks.Create<IPosixSignalRegistrar>();
        _terminator = _mocks.Create<IProcessTerminator>();
        _sigintRegistration = _mocks.Create<IDisposable>();
        _sigtermRegistration = _mocks.Create<IDisposable>();
        _handlers = new Dictionary<PosixSignal, Action<PosixSignalContext>>();
        _time = new ManualTimeProvider();
        _stderr = new StringWriter();
        _loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new StandardErrorLoggerProvider(_stderr, TimeProvider.System, new LogLineFormatter(), LogLevel.Information, "ParseDWD")));
        _terminations = 0;
        _registrar.Setup(r => r.Register(PosixSignal.SIGINT, It.IsAny<Action<PosixSignalContext>>()))
            .Callback<PosixSignal, Action<PosixSignalContext>>((signal, handler) => _handlers[signal] = handler)
            .Returns(_sigintRegistration.Object);
        _registrar.Setup(r => r.Register(PosixSignal.SIGTERM, It.IsAny<Action<PosixSignalContext>>()))
            .Callback<PosixSignal, Action<PosixSignalContext>>((signal, handler) => _handlers[signal] = handler)
            .Returns(_sigtermRegistration.Object);
        _subject = new ShutdownSignal(_registrar.Object, _terminator.Object, _time);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
        _loggerFactory.Dispose();
        _stderr.Dispose();
    }

    // Raises the signal through the registered handler and returns the context the handler saw.
    private PosixSignalContext Raise(PosixSignal signal)
    {
        var context = new PosixSignalContext(signal);
        _handlers[signal](context);
        return context;
    }

    private void ExpectTermination() => _terminator.Setup(t => t.Terminate(ExitCodes.Cancelled)).Callback(() => _terminations++);

    private void ExpectDisposal()
    {
        _sigintRegistration.Setup(r => r.Dispose());
        _sigtermRegistration.Setup(r => r.Dispose());
    }

    /// <summary>
    /// Tests that the constructor throws when arguments are null.
    /// </summary>
    [TestMethod]
    public void Constructor_NullArguments_Throw()
    {
        ((Action)(() => new ShutdownSignal(null!, _terminator.Object, _time))).Should().Throw<ArgumentNullException>().WithParameterName("registrar");
        ((Action)(() => new ShutdownSignal(_registrar.Object, null!, _time))).Should().Throw<ArgumentNullException>().WithParameterName("terminator");
        ((Action)(() => new ShutdownSignal(_registrar.Object, _terminator.Object, null!))).Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    /// <summary>
    /// Tests that the constructor registers SIGINT and SIGTERM.
    /// </summary>
    [TestMethod]
    public void Constructor_RegistersSigintAndSigterm()
    {
        _handlers.Keys.Should().BeEquivalentTo(new[] { PosixSignal.SIGINT, PosixSignal.SIGTERM });
        _subject.Token.CanBeCanceled.Should().BeTrue();
        _subject.Token.IsCancellationRequested.Should().BeFalse();
    }

    /// <summary>
    /// Tests that the first SIGINT cancels the token and default termination but does not terminate.
    /// </summary>
    [TestMethod]
    public void FirstSigint_CancelsTokenAndDefaultTermination_NoTermination()
    {
        var context = Raise(PosixSignal.SIGINT);
        context.Cancel.Should().BeTrue();
        _subject.Token.IsCancellationRequested.Should().BeTrue();
        _terminations.Should().Be(0);
    }

    /// <summary>
    /// Tests that the first SIGTERM cancels the token and default termination but does not terminate.
    /// </summary>
    [TestMethod]
    public void FirstSigterm_CancelsTokenAndDefaultTermination()
    {
        var context = Raise(PosixSignal.SIGTERM);
        context.Cancel.Should().BeTrue();
        _subject.Token.IsCancellationRequested.Should().BeTrue();
        _terminations.Should().Be(0);
    }

    /// <summary>
    /// Tests that the first signal logs a cancelled warning.
    /// </summary>
    [TestMethod]
    public void FirstSignal_LogsCancelledWarning()
    {
        _subject.Configure(TimeSpan.FromSeconds(5), _loggerFactory.CreateLogger("WeatherParser.Cli"));
        Raise(PosixSignal.SIGINT);
        _stderr.ToString().Should().Contain(" WARNING CLI-CANCELLED command=ParseDWD ");
    }

    /// <summary>
    /// Tests that the second signal terminates immediately.
    /// </summary>
    [TestMethod]
    public void SecondSignal_TerminatesImmediately_Exit4()
    {
        ExpectTermination();
        Raise(PosixSignal.SIGINT);
        var context = Raise(PosixSignal.SIGTERM);
        context.Cancel.Should().BeTrue();
        _terminations.Should().Be(1);
    }

    /// <summary>
    /// Tests that the grace period expires and terminates.
    /// </summary>
    [TestMethod]
    public void GracePeriodExpired_Terminates_Exit4()
    {
        ExpectTermination();
        _subject.Configure(TimeSpan.FromSeconds(2), _loggerFactory.CreateLogger("WeatherParser.Cli"));
        Raise(PosixSignal.SIGINT);
        _time.Advance(TimeSpan.FromSeconds(2) - TimeSpan.FromTicks(1));
        _terminations.Should().Be(0);
        _time.Advance(TimeSpan.FromTicks(1));
        _terminations.Should().Be(1);
    }

    /// <summary>
    /// Tests that the default grace period is five seconds.
    /// </summary>
    [TestMethod]
    public void GracePeriod_DefaultFiveSeconds()
    {
        ShutdownSignal.DefaultGracePeriod.Should().Be(TimeSpan.FromSeconds(5));
        ExpectTermination();
        Raise(PosixSignal.SIGINT);
        _time.Advance(ShutdownSignal.DefaultGracePeriod - TimeSpan.FromTicks(1));
        _terminations.Should().Be(0);
        _time.Advance(TimeSpan.FromTicks(1));
        _terminations.Should().Be(1);
    }

    /// <summary>
    /// Tests that disposal before grace expiry prevents termination and disposes registrations.
    /// </summary>
    [TestMethod]
    public void Dispose_BeforeGraceExpiry_NoTermination_RegistrationsDisposed()
    {
        ExpectDisposal();
        Raise(PosixSignal.SIGINT);
        _subject.Dispose();
        _time.ActiveTimers.Should().Be(0);
        _time.Advance(TimeSpan.FromMinutes(1));
        _terminations.Should().Be(0);
    }

    /// <summary>
    /// Tests that signals after disposal are ignored.
    /// </summary>
    [TestMethod]
    public void SignalAfterDispose_Ignored()
    {
        ExpectDisposal();
        _subject.Dispose();
        var context = Raise(PosixSignal.SIGINT);
        context.Cancel.Should().BeFalse();
        _subject.Token.IsCancellationRequested.Should().BeFalse();
        _time.ActiveTimers.Should().Be(0);
    }
}
