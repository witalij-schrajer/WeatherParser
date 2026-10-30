namespace WeatherParser.Cli.Tests.Lifetime;

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeatherParser.Cli.Arguments;
using WeatherParser.Cli.Configuration;
using WeatherParser.Cli.Lifetime;
using WeatherParser.Cli.Tests.Configuration;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Geography;

/// <summary>
/// Tests for the <see cref="LibraryDeadlineResolver"/>.
/// </summary>
[TestClass]
[TestCategory("T-P3-18")]
public class LibraryDeadlineResolverTests
{
    private LibraryDeadlineResolver _subject = null!;
    private CliConfiguration _configuration = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new LibraryDeadlineResolver();
        _configuration = TestConfigurations.Valid();
    }

    // Registers the configuration as the CLI does and resolves the deadline of the request.
    private TimeSpan Resolve(CommandRequest request)
    {
        var services = new ServiceCollection();
        new ConfigurationRegistrar().Register(services, _configuration, request);
        using var provider = services.BuildServiceProvider();
        return _subject.Resolve(provider, request);
    }

    private static CommonOptions Common => new(OutputFormat.Json, null, null, false);

    /// <summary>
    /// Tests that <see cref="LibraryDeadlineResolver.Resolve"/> throws <see cref="ArgumentNullException"/> when arguments are null.
    /// </summary>
    [TestMethod]
    public void Resolve_NullArguments_Throw()
    {
        ((Action)(() => _subject.Resolve(null!, TestConfigurations.Request(CliCommand.ParseDwd, OutputFormat.Json)))).Should().Throw<ArgumentNullException>().WithParameterName("services");
        var services = new ServiceCollection().BuildServiceProvider();
        ((Action)(() => _subject.Resolve(services, null!))).Should().Throw<ArgumentNullException>().WithParameterName("request");
    }

    /// <summary>
    /// Tests that <see cref="LibraryDeadlineResolver.Resolve"/> returns the library default deadline for each command when no custom configuration is provided.
    /// </summary>
    /// <param name="command">The command to test.</param>
    /// <param name="seconds">The expected seconds.</param>
    [DataRow(CliCommand.SelectDwd, 120)]
    [DataRow(CliCommand.SelectMosmix, 120)]
    [DataRow(CliCommand.ParseDwd, 120)]
    [DataRow(CliCommand.ParseOstHessen, 60)]
    [DataRow(CliCommand.ParseMosmix, 60)]
    [TestMethod]
    public void Resolve_DefaultOptions_LibraryDefaultOfEachCommand(CliCommand command, int seconds)
    {
        Resolve(TestConfigurations.Request(command, OutputFormat.Json)).Should().Be(TimeSpan.FromSeconds(seconds));
    }

    /// <summary>
    /// Tests that <see cref="LibraryDeadlineResolver.Resolve"/> returns the default 5 minutes for parsing MOSMIX S.
    /// </summary>
    [TestMethod]
    public void Resolve_ParseMosmixS_DefaultFiveMinutes()
    {
        Resolve(new ParseMosmixRequest(Common, MosmixProduct.MosmixS, "10637")).Should().Be(TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Tests that <see cref="LibraryDeadlineResolver.Resolve"/> adds the refresh deadline when refreshing MOSMIX S evidence.
    /// </summary>
    [TestMethod]
    public void Resolve_SelectMosmixWithRefresh_AddsTheRefreshDeadline()
    {
        Resolve(new SelectMosmixRequest(Common, new SearchParameterFactory().Create(50.5m, 9.7m, 300m, 50m, 200m), true)).Should().Be(TimeSpan.FromMinutes(12));
    }

    /// <summary>
    /// Tests that <see cref="LibraryDeadlineResolver.Resolve"/> uses configured deadlines.
    /// </summary>
    [TestMethod]
    public void Resolve_ConfiguredDeadlines_Used()
    {
        _configuration.Dwd!.Catalog!.SelectionDeadline = TimeSpan.FromSeconds(31);
        _configuration.Dwd.Observation!.ObservationDeadline = TimeSpan.FromSeconds(32);
        _configuration.OsthessenWetter!.PageDeadline = TimeSpan.FromSeconds(33);
        _configuration.Mosmix!.Catalog!.SelectionDeadline = TimeSpan.FromSeconds(34);
        _configuration.Mosmix.Catalog.MosmixSRefreshDeadline = TimeSpan.FromSeconds(35);
        _configuration.Mosmix.L!.ParseDeadline = TimeSpan.FromSeconds(36);
        _configuration.Mosmix.S!.ParseDeadline = TimeSpan.FromSeconds(37);

        Resolve(TestConfigurations.Request(CliCommand.SelectDwd, OutputFormat.Json)).Should().Be(TimeSpan.FromSeconds(31));
        Resolve(TestConfigurations.Request(CliCommand.ParseDwd, OutputFormat.Json)).Should().Be(TimeSpan.FromSeconds(32));
        Resolve(TestConfigurations.Request(CliCommand.ParseOstHessen, OutputFormat.Json)).Should().Be(TimeSpan.FromSeconds(33));
        Resolve(TestConfigurations.Request(CliCommand.SelectMosmix, OutputFormat.Json)).Should().Be(TimeSpan.FromSeconds(34));
        Resolve(new SelectMosmixRequest(Common, new SearchParameterFactory().Create(50.5m, 9.7m, 300m, 50m, 200m), true)).Should().Be(TimeSpan.FromSeconds(69));
        TestConfigurations.Request(CliCommand.ParseMosmix, OutputFormat.Json).Should().BeOfType<ParseMosmixRequest>().Which.Product.Should().Be(MosmixProduct.MosmixL);
        Resolve(TestConfigurations.Request(CliCommand.ParseMosmix, OutputFormat.Json)).Should().Be(TimeSpan.FromSeconds(36));
        Resolve(new ParseMosmixRequest(Common, MosmixProduct.MosmixS, "10637")).Should().Be(TimeSpan.FromSeconds(37));
    }
}
