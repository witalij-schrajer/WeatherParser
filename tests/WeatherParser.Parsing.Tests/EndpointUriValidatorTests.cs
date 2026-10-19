using FluentAssertions;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests;

[TestClass]
public sealed class EndpointUriValidatorTests
{
    private EndpointUriValidator _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _sut = new EndpointUriValidator();
    }

    [TestMethod]
    public void IsValid_WithNullUri_ReturnsFalse()
    {
        _sut.IsValid(null).Should().BeFalse();
    }

    [TestMethod]
    public void IsValid_WithRelativeUri_ReturnsFalse()
    {
        _sut.IsValid(new Uri("weather/mosmix", UriKind.Relative)).Should().BeFalse();
    }

    [TestMethod]
    [DataRow("ftp://example.org/mosmix")]
    [DataRow("file:///C:/weather/mosmix.kml")]
    [DataRow("mailto:weather@example.org")]
    public void IsValid_WithNonHttpScheme_ReturnsFalse(string uriString)
    {
        _sut.IsValid(new Uri(uriString)).Should().BeFalse();
    }

    [TestMethod]
    [DataRow("http://example.org/mosmix")]
    [DataRow("https://example.org/mosmix")]
    public void IsValid_WithHttpOrHttpsUri_ReturnsTrue(string uriString)
    {
        _sut.IsValid(new Uri(uriString)).Should().BeTrue();
    }
}
