using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace WeatherParser.Cli.Tests;

/// <summary>
/// Verifies the entry point of the example CLI: exactly one command per process and the container disposed afterwards (CL-07, REQ-006).
/// </summary>
[TestClass]
public sealed class ProgramTests
{
    private MockRepository _mocks = null!;
    private Mock<ICommandRunner> _runner = null!;

    /// <summary>
    /// Initializes the test fixtures.
    /// </summary>
    [TestInitialize]
    public void Initialize()
    {
        _mocks = new MockRepository(MockBehavior.Strict);
        _runner = _mocks.Create<ICommandRunner>();
    }

    /// <summary>
    /// Cleans up the test fixtures.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        _mocks.VerifyAll();
    }

    /// <summary>
    /// Verifies that the program runs exactly one command and disposes the provider.
    /// </summary>
    [TestMethod]
    public async Task Program_RunsExactlyOneCommandAndDisposesProvider()
    {
        var args = new[] { "--Help" };
        _runner.Setup(r => r.RunAsync(args, It.IsAny<CancellationToken>())).ReturnsAsync(7);

        ServiceProvider? provider = null;
        ServiceProvider Build() 
        { 
            var services = new ServiceCollection();
            services.AddSingleton(_runner.Object);
            provider = services.BuildServiceProvider();
            return provider;
        }

        var exitCode = await Program.RunAsync(args, Build, CancellationToken.None);
        exitCode.Should().Be(7);
        _runner.Verify(r => r.RunAsync(args, It.IsAny<CancellationToken>()), Times.Once());

        Action resolveAfterDispose = () => provider!.GetService<ICommandRunner>();
        resolveAfterDispose.Should().Throw<ObjectDisposedException>();
    }
}
