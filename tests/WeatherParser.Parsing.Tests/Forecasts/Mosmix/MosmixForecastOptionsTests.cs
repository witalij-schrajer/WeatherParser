using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests.Catalogs;

[TestClass]
public sealed class MosmixForecastOptionsTests
{
    private MockRepository _mocks = null!;
    private Mock<ICatalogDownloader> _downloader = null!;
    private IEndpointUriValidator _validator = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _mocks = new(MockBehavior.Strict);
        _downloader = _mocks.Create<ICatalogDownloader>();
        _validator = new EndpointUriValidator();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private MosmixLForecastSource CreateL(MosmixLForecastEndpoints endpoints, MosmixLForecastOptions options) => new(endpoints, options, _validator, _downloader.Object);

    private MosmixSForecastSource CreateS(MosmixSForecastEndpoints endpoints, MosmixSForecastOptions options) => new(endpoints, options, _validator, _downloader.Object);

    private static MosmixLForecastOptions ValidL() => new() { MaxRunAge = TimeSpan.FromHours(8) };

    private static MosmixSForecastOptions ValidS() => new() { MaxRunAge = TimeSpan.FromHours(2) };

    private static Uri Dir => new("https://example.org/mosmix/single_stations/");

    private static Uri File => new("https://example.org/mosmix/MOSMIX_S_LATEST_240.kmz");

    private static MosmixForecastException OptionsInvalid(Action act, MosmixProduct product)
    {
        var ex = FluentActions.Invoking(act).Should().Throw<MosmixForecastException>().Which;
        ex.ErrorId.Should().Be("MOSMIX-OPTIONS-INVALID");
        ex.Product.Should().Be(product);
        return ex;
    }

    [DataRow(null)]
    [DataRow("/single_stations/")]
    [DataRow("ftp://example.org/single_stations/")]
    [DataRow("https://user:pw@example.org/single_stations/")]
    [DataRow("https://example.org/single_stations")]
    [DataRow("https://example.org/single_stations/?mirror=1")]
    [DataRow("https://example.org/single_stations/#top")]
    [TestMethod]
    public void ConstructL_DirectoryInvalid_ThrowsOptionsInvalid(string? uri)
    {
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute) };
        var options = ValidL();
        OptionsInvalid(() => CreateL(endpoints, options), MosmixProduct.MosmixL);
    }

    [DataRow(null)]
    [DataRow("/MOSMIX_S_LATEST_240.kmz")]
    [DataRow("ftp://example.org/x.kmz")]
    [DataRow("https://user:pw@example.org/x.kmz")]
    [TestMethod]
    public void ConstructS_FileUriInvalid_ThrowsOptionsInvalid(string? uri)
    {
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute) };
        var options = ValidS();
        OptionsInvalid(() => CreateS(endpoints, options), MosmixProduct.MosmixS);
    }

    [TestMethod]
    public void ConstructS_FileUriWithQuery_Accepted()
    {
        var endpoints = new MosmixSForecastEndpoints { AllStationsUri = new Uri("https://example.org/x.kmz?mirror=1") };
        var options = ValidS();
        FluentActions.Invoking(() => CreateS(endpoints, options)).Should().NotThrow();
    }

    [TestMethod]
    public void Construct_DefaultsAccepted()
    {
        var lOptions = new MosmixLForecastOptions();
        lOptions.ParseDeadline.Should().Be(TimeSpan.FromSeconds(60));
        lOptions.MaxRunAge.Should().BeNull();

        var sOptions = new MosmixSForecastOptions();
        sOptions.ParseDeadline.Should().Be(TimeSpan.FromMinutes(5));
        sOptions.MaxRunAge.Should().BeNull();

        FluentActions.Invoking(() => CreateL(new() { SingleStationsDirectory = Dir }, ValidL())).Should().NotThrow();
        FluentActions.Invoking(() => CreateS(new() { AllStationsUri = File }, ValidS())).Should().NotThrow();
    }

    [DataRow(1)]
    [DataRow(36000000000)]
    [TestMethod]
    public void Construct_DeadlineLimits_Accepted(long ticks)
    {
        var deadline = TimeSpan.FromTicks(ticks);
        FluentActions.Invoking(() => CreateL(new() { SingleStationsDirectory = Dir }, new() { ParseDeadline = deadline, MaxRunAge = TimeSpan.FromHours(8) })).Should().NotThrow();
        FluentActions.Invoking(() => CreateS(new() { AllStationsUri = File }, new() { ParseDeadline = deadline, MaxRunAge = TimeSpan.FromHours(2) })).Should().NotThrow();
    }

    [DataRow("zero")]
    [DataRow("negative")]
    [DataRow("infinite")]
    [DataRow("oneTickAboveHour")]
    [DataRow("max")]
    [TestMethod]
    public void Construct_DeadlineOutOfRange_ThrowsOptionsInvalid(string scenario)
    {
        var deadline = scenario switch
        {
            "zero" => TimeSpan.Zero,
            "negative" => TimeSpan.FromHours(-1),
            "infinite" => Timeout.InfiniteTimeSpan,
            "oneTickAboveHour" => TimeSpan.FromHours(1).Add(TimeSpan.FromTicks(1)),
            "max" => TimeSpan.MaxValue,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        OptionsInvalid(() => CreateL(new() { SingleStationsDirectory = Dir }, new() { ParseDeadline = deadline, MaxRunAge = TimeSpan.FromHours(8) }), MosmixProduct.MosmixL);
        OptionsInvalid(() => CreateS(new() { AllStationsUri = File }, new() { ParseDeadline = deadline, MaxRunAge = TimeSpan.FromHours(2) }), MosmixProduct.MosmixS);
    }

    [DataRow(1)]
    [DataRow(8640000000000)]
    [TestMethod]
    public void Construct_MaxRunAgeLimits_Accepted(long ticks)
    {
        var maxRunAge = TimeSpan.FromTicks(ticks);
        FluentActions.Invoking(() => CreateL(new() { SingleStationsDirectory = Dir }, new() { MaxRunAge = maxRunAge })).Should().NotThrow();
        FluentActions.Invoking(() => CreateS(new() { AllStationsUri = File }, new() { MaxRunAge = maxRunAge })).Should().NotThrow();
    }

    [DataRow("unset")]
    [DataRow("zero")]
    [DataRow("negative")]
    [DataRow("infinite")]
    [DataRow("oneTickAbove240h")]
    [DataRow("max")]
    [TestMethod]
    public void Construct_MaxRunAgeInvalid_ThrowsOptionsInvalid(string scenario)
    {
        var maxRunAge = scenario switch
        {
            "unset" => (TimeSpan?)null,
            "zero" => TimeSpan.Zero,
            "negative" => TimeSpan.FromHours(-1),
            "infinite" => Timeout.InfiniteTimeSpan,
            "oneTickAbove240h" => TimeSpan.FromHours(240).Add(TimeSpan.FromTicks(1)),
            "max" => TimeSpan.MaxValue,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        OptionsInvalid(() => CreateL(new() { SingleStationsDirectory = Dir }, new() { MaxRunAge = maxRunAge }), MosmixProduct.MosmixL);
        OptionsInvalid(() => CreateS(new() { AllStationsUri = File }, new() { MaxRunAge = maxRunAge }), MosmixProduct.MosmixS);
    }

    [TestMethod]
    public void Construct_Message_NamesOptionWithoutQueryOrUserInfo()
    {
        var endpoints = new MosmixLForecastEndpoints { SingleStationsDirectory = new Uri("https://user:secret@example.org/single_stations/?token=abc") };
        var options = ValidL();
        var ex = OptionsInvalid(() => CreateL(endpoints, options), MosmixProduct.MosmixL);
        ex.Message.Should().Contain("SingleStationsDirectory");
        ex.Message.Should().NotContain("secret");
        ex.Message.Should().NotContain("token");
    }

    [TestMethod]
    public void Construct_NullArguments_ThrowArgumentNullException()
    {
        FluentActions.Invoking(() => new MosmixLForecastSource(null!, new(), _validator, _downloader.Object)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new MosmixLForecastSource(new(), null!, _validator, _downloader.Object)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new MosmixLForecastSource(new(), new(), null!, _downloader.Object)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new MosmixLForecastSource(new(), new(), _validator, null!)).Should().Throw<ArgumentNullException>();

        FluentActions.Invoking(() => new MosmixSForecastSource(null!, new(), _validator, _downloader.Object)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new MosmixSForecastSource(new(), null!, _validator, _downloader.Object)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new MosmixSForecastSource(new(), new(), null!, _downloader.Object)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new MosmixSForecastSource(new(), new(), _validator, null!)).Should().Throw<ArgumentNullException>();
    }
}
