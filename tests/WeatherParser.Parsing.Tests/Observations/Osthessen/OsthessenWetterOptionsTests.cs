using System;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Osthessen;
using WeatherParser.Parsing.Parsing;
using WeatherParser.Parsing.Tests;

namespace WeatherParser.Parsing.Tests.Observations.Osthessen;

[TestClass]
public sealed class OsthessenWetterOptionsTests
{
    private MockRepository _mocks = new(MockBehavior.Strict);
    private Mock<ICatalogDownloader> _downloader = null!;
    private IEndpointUriValidator _validator = new EndpointUriValidator();

    [TestInitialize]
    public void TestInitialize()
    {
        _downloader = _mocks.Create<ICatalogDownloader>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _mocks.VerifyAll();
    }

    private OsthessenWetterPageSource Create(Uri? pageUri, TimeSpan? deadline = null)
    {
        var options = new OsthessenWetterOptions();
        if (deadline.HasValue)
        {
            options.PageDeadline = deadline.Value;
        }

        var endpoints = new OsthessenWetterEndpoints { PageUri = pageUri };
        return new OsthessenWetterPageSource(options, endpoints, _validator, _downloader.Object);
    }

    private OsthessenWetterException AssertInvalid(Uri? pageUri, TimeSpan? deadline = null)
    {
        var ex = FluentActions.Invoking(() => Create(pageUri, deadline))
            .Should().Throw<OsthessenWetterException>().Which;
        ex.ErrorId.Should().Be(OsthessenWetterException.OptionsInvalidErrorId);
        return ex;
    }

    private static readonly Uri Valid = new("https://example.org/messwerte/");

    [TestMethod]
    public void Construct_AbsoluteHttpsUri_Accepted()
    {
        FluentActions.Invoking(() => Create(Valid)).Should().NotThrow();
    }

    [TestMethod]
    public void Construct_HttpUri_Accepted()
    {
        var uri = new Uri("http://example.org/messwerte/");
        FluentActions.Invoking(() => Create(uri)).Should().NotThrow();
    }

    [TestMethod]
    public void Construct_NullUri_ThrowsOptionsInvalid()
    {
        AssertInvalid(null);
    }

    [TestMethod]
    public void Construct_RelativeUri_ThrowsOptionsInvalid()
    {
        var uri = new Uri("/messwerte/", UriKind.Relative);
        AssertInvalid(uri);
    }

    [TestMethod]
    public void Construct_NonHttpScheme_ThrowsOptionsInvalid()
    {
        var uri = new Uri("ftp://example.org/messwerte/?token=abc123");
        var ex = AssertInvalid(uri);
        ex.Message.Should().NotContain("token").And.NotContain("abc123");
    }

    [TestMethod]
    public void Construct_UriWithUserInfo_ThrowsOptionsInvalid()
    {
        var uri = new Uri("https://alice:s3cr3t@example.org/messwerte/");
        var ex = AssertInvalid(uri);
        ex.Message.Should().NotContain("alice").And.NotContain("s3cr3t");
    }

    [TestMethod]
    public void Construct_UriWithUserNameOnly_ThrowsOptionsInvalid()
    {
        var uri = new Uri("https://alice@example.org/messwerte/");
        var ex = AssertInvalid(uri);
        ex.Message.Should().NotContain("alice");
    }

    [TestMethod]
    public void PageDeadline_Default60Seconds()
    {
        new OsthessenWetterOptions().PageDeadline.Should().Be(TimeSpan.FromSeconds(60));
        FluentActions.Invoking(() => Create(Valid)).Should().NotThrow();
    }

    [TestMethod]
    public void PageDeadline_OneTick_Accepted()
    {
        FluentActions.Invoking(() => Create(Valid, TimeSpan.FromTicks(1))).Should().NotThrow();
    }

    [TestMethod]
    public void PageDeadline_OneHour_Accepted()
    {
        FluentActions.Invoking(() => Create(Valid, TimeSpan.FromHours(1))).Should().NotThrow();
    }

    [TestMethod]
    public void PageDeadline_Zero_ThrowsOptionsInvalid()
    {
        AssertInvalid(Valid, TimeSpan.Zero);
    }

    [TestMethod]
    public void PageDeadline_Negative_ThrowsOptionsInvalid()
    {
        AssertInvalid(Valid, TimeSpan.FromTicks(-1));
    }

    [TestMethod]
    public void PageDeadline_Infinite_ThrowsOptionsInvalid()
    {
        AssertInvalid(Valid, Timeout.InfiniteTimeSpan);
    }

    [TestMethod]
    public void PageDeadline_AboveOneHour_ThrowsOptionsInvalid()
    {
        AssertInvalid(Valid, TimeSpan.FromHours(1) + TimeSpan.FromTicks(1));
    }

    [TestMethod]
    public void PageDeadline_MaxValue_ThrowsOptionsInvalid()
    {
        AssertInvalid(Valid, TimeSpan.MaxValue);
    }

    [TestMethod]
    public void PageDeadline_MinValue_ThrowsOptionsInvalid()
    {
        AssertInvalid(Valid, TimeSpan.MinValue);
    }

    [TestMethod]
    public void Construct_InvalidUriAndInvalidDeadline_ThrowsOptionsInvalid()
    {
        AssertInvalid(null, TimeSpan.Zero);
    }

    [TestMethod]
    public void Construct_Valid_NoRequestMade()
    {
        Create(Valid);
        _downloader.Invocations.Should().BeEmpty();
    }

    [TestMethod]
    public void Construct_NullArguments_ThrowArgumentNullException()
    {
        FluentActions.Invoking(() => new OsthessenWetterPageSource(null!, new OsthessenWetterEndpoints(), _validator, _downloader.Object))
            .Should().Throw<ArgumentNullException>().WithParameterName("options");

        FluentActions.Invoking(() => new OsthessenWetterPageSource(new OsthessenWetterOptions(), null!, _validator, _downloader.Object))
            .Should().Throw<ArgumentNullException>().WithParameterName("endpoints");

        FluentActions.Invoking(() => new OsthessenWetterPageSource(new OsthessenWetterOptions(), new OsthessenWetterEndpoints(), null!, _downloader.Object))
            .Should().Throw<ArgumentNullException>().WithParameterName("uriValidator");

        FluentActions.Invoking(() => new OsthessenWetterPageSource(new OsthessenWetterOptions(), new OsthessenWetterEndpoints(), _validator, null!))
            .Should().Throw<ArgumentNullException>().WithParameterName("downloader");
    }

    [TestMethod]
    public void AddWeatherParsing_ResolvesOsthessenWetterPageSource()
    {
        var services = new ServiceCollection().AddWeatherParsing().AddTestConsumerServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        provider.GetRequiredService<IOsthessenWetterPageSource>().Should().BeOfType<OsthessenWetterPageSource>();
    }
}
