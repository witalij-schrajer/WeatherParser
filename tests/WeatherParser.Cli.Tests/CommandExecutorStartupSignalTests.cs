using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Commands;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Cli.Lifetime;
using WeatherParser.Cli.Tests.Configuration;
using WeatherParser.Cli.Tests.Lifetime;

namespace WeatherParser.Cli.Tests;

/// <summary>
/// Tests the behavior of <see cref="CommandExecutor"/> when a shutdown signal arrives during the command's startup phase (CL-11, CL-18).
/// </summary>
[TestClass]
[TestCategory("T-P3-18")]
public sealed class CommandExecutorStartupSignalTests
{
    private MockRepository _mocks = null!;
    private Mock<IConfigurationLoader> _loader = null!;
    private Mock<IConfigurationRegistrar> _registrar = null!;
    private Mock<ICommandRegistrar> _commandRegistrar = null!;
    private Mock<IConfigurationValidator> _validator = null!;
    private Mock<ILibraryDeadlineResolver> _deadlineResolver = null!;
    private Mock<ICommandHandler<ParseDwdRequest>> _handler = null!;
    private TestShutdownSignal _signal = null!;
    private StringWriter _stdout = null!;
    private StringWriter _stderr = null!;
    private CliConfiguration _configuration = null!;
    private ParseDwdRequest _request = null!;
    private CommandExecutor _subject = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _loader = _mocks.Create<IConfigurationLoader>();
        _registrar = _mocks.Create<IConfigurationRegistrar>();
        _commandRegistrar = _mocks.Create<ICommandRegistrar>();
        _validator = _mocks.Create<IConfigurationValidator>();
        _deadlineResolver = _mocks.Create<ILibraryDeadlineResolver>();
        _handler = _mocks.Create<ICommandHandler<ParseDwdRequest>>();
        _signal = new TestShutdownSignal();
        _stdout = new StringWriter();
        _stderr = new StringWriter();
        _configuration = TestConfigurations.Valid();
        _request = new ParseDwdRequest(new CommonOptions(OutputFormat.Json, null, "c.json", false), "00044");
        _subject = new CommandExecutor(_loader.Object, _registrar.Object, _commandRegistrar.Object, _validator.Object, new ExitCodePolicy(), new LibraryErrors(), new LogLineFormatter(), _signal, _deadlineResolver.Object, TimeProvider.System);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
        _signal.Dispose();
        _stdout.Dispose();
        _stderr.Dispose();
    }

    // The configuration loads and registers; the validator decides how the startup ends.
    private void SetupConfiguration()
    {
        _loader.Setup(l => l.Load(_request.Common.ConfigPath)).Returns(_configuration);
        _registrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), _configuration, _request)).Callback<IServiceCollection, CliConfiguration, CommandRequest>((services, configuration, r) => new ConfigurationRegistrar().Register(services, configuration, r));
        _commandRegistrar.Setup(r => r.Register(It.IsAny<IServiceCollection>(), _request)).Callback<IServiceCollection, CommandRequest>((services, _) => services.AddSingleton(_handler.Object));
    }

    /// <summary>
    /// When a signal arrives during configuration loading, the command fails with exit code 4 (CLI-CANCELLED).
    /// </summary>
    [TestMethod]
    public void Signal_DuringConfigurationLoading_ConfigurationFails_ExitCancelled()
    {
        _loader.Setup(l => l.Load(_request.Common.ConfigPath)).Callback(() => _signal.Raise()).Throws(new CliException(CliErrorIds.ConfigInvalid, "The configuration file is invalid."));
        var exitCode = _subject.Execute(_request, _stdout, _stderr);
        exitCode.Should().Be(4);
        _stderr.ToString().Should().Contain(" ERROR CLI-CONFIG-INVALID command=ParseDWD ");
        _stderr.ToString().Should().Contain(" ERROR CLI-CANCELLED command=ParseDWD ");
        _stdout.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// When a signal arrives during validation, the command fails with exit code 4 (CLI-CANCELLED).
    /// </summary>
    [TestMethod]
    public void Signal_DuringValidation_ValidationFails_ExitCancelled()
    {
        SetupConfiguration();
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, _request)).Callback(() => _signal.Raise()).Throws(new CliException(CliErrorIds.ConfigInvalid, "ENDPOINT-INVALID: bad endpoint"));
        var exitCode = _subject.Execute(_request, _stdout, _stderr);
        exitCode.Should().Be(4);
        _stderr.ToString().Should().Contain(" ERROR CLI-CONFIG-INVALID command=ParseDWD ");
        _stderr.ToString().Should().Contain(" ERROR CLI-CANCELLED command=ParseDWD ");
        _stdout.ToString().Should().BeEmpty();
    }

    /// <summary>
    /// When a signal does not arrive during configuration loading, the normal exit code is preserved.
    /// </summary>
    [TestMethod]
    public void NoSignal_ConfigurationFails_KeepsUsageExit()
    {
        _loader.Setup(l => l.Load(_request.Common.ConfigPath)).Throws(new CliException(CliErrorIds.ConfigInvalid, "The configuration file is invalid."));
        var exitCode = _subject.Execute(_request, _stdout, _stderr);
        exitCode.Should().Be(2);
        _stderr.ToString().Should().NotContain("CLI-CANCELLED");
    }

    /// <summary>
    /// The shutdown signal's logger is attached before configuration loading begins.
    /// </summary>
    [TestMethod]
    public void Execute_SignalLoggerAttachedBeforeConfigurationLoading()
    {
        _loader.Setup(l => l.Load(_request.Common.ConfigPath)).Callback(() =>
        {
            _signal.Logger.Should().NotBeNull();
            _signal.GracePeriod.Should().Be(ShutdownSignal.DefaultGracePeriod);
        }).Throws(new CliException(CliErrorIds.ConfigMissing, "No configuration file was given."));
        var exitCode = _subject.Execute(_request, _stdout, _stderr);
        exitCode.Should().Be(2);
    }

    /// <summary>
    /// The shutdown signal's logger is released before the command returns.
    /// </summary>
    [TestMethod]
    public void Execute_ReleasesSignalLoggerBeforeReturning()
    {
        SetupConfiguration();
        _validator.Setup(v => v.Validate(It.IsAny<IServiceProvider>(), _configuration, _request)).Returns(new ValidatedSettings("00044", null));
        _deadlineResolver.Setup(d => d.Resolve(It.IsAny<IServiceProvider>(), _request)).Returns(TimeSpan.FromMinutes(1));
        _handler.Setup(h => h.HandleAsync(_request, It.IsAny<CommandContext>(), It.IsAny<CancellationToken>())).ReturnsAsync(new CommandOutcome(1, 0, false));
        var exitCode = _subject.Execute(_request, _stdout, _stderr);
        exitCode.Should().Be(0);
        _signal.LoggerReleased.Should().BeTrue();
    }

    /// <summary>
    /// If the command startup fails before validation, the signal's logger is still released.
    /// </summary>
    [TestMethod]
    public void Execute_StartupFails_ReleasesSignalLogger()
    {
        _loader.Setup(l => l.Load(_request.Common.ConfigPath)).Throws(new CliException(CliErrorIds.ConfigMissing, "No configuration file was given."));
        var exitCode = _subject.Execute(_request, _stdout, _stderr);
        exitCode.Should().Be(2);
        _signal.LoggerReleased.Should().BeTrue();
    }
}
