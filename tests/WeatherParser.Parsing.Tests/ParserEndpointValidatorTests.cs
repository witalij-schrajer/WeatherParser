using FluentAssertions;
using Moq;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Parsing.Tests;

[TestClass]
public sealed class ParserEndpointValidatorTests
{
    private Mock<IEndpointUriValidator> _uriValidatorMock = null!;
    private ParserEndpointValidator _sut = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _uriValidatorMock = new Mock<IEndpointUriValidator>(MockBehavior.Strict);
        _sut = new ParserEndpointValidator(_uriValidatorMock.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _uriValidatorMock.VerifyAll();
    }

    [TestMethod]
    public void Constructor_WithNullUriValidator_ThrowsArgumentNullException()
    {
        var act = () => new ParserEndpointValidator(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("uriValidator");
    }

    [TestMethod]
    public void ValidateAndMap_WithSingleValidEndpoint_ReturnsMapContainingEndpoint()
    {
        var endpoint = new ParserEndpointDefinition("Mosmix", new Uri("https://example.org/mosmix"));
        _uriValidatorMock.Setup(v => v.IsValid(endpoint.Uri)).Returns(true);

        var result = _sut.ValidateAndMap([endpoint]);

        result.Should().ContainKey("Mosmix")
            .WhoseValue.Should().Be(endpoint);
    }

    [TestMethod]
    public void ValidateAndMap_WithTwoDistinctValidEndpoints_MapsEachWithoutConfusion()
    {
        var mosmix = new ParserEndpointDefinition("Mosmix", new Uri("https://example.org/mosmix"));
        var measurements = new ParserEndpointDefinition("Measurements", new Uri("https://example.org/measurements"));
        _uriValidatorMock.Setup(v => v.IsValid(mosmix.Uri)).Returns(true);
        _uriValidatorMock.Setup(v => v.IsValid(measurements.Uri)).Returns(true);

        var result = _sut.ValidateAndMap([mosmix, measurements]);

        result.Should().HaveCount(2);
        result["Mosmix"].Uri.Should().Be(mosmix.Uri);
        result["Measurements"].Uri.Should().Be(measurements.Uri);
    }

    [TestMethod]
    public void ValidateAndMap_WithInvalidUri_ThrowsParserEndpointValidationException()
    {
        var endpoint = new ParserEndpointDefinition("Mosmix", new Uri("ftp://example.org/mosmix"));
        _uriValidatorMock.Setup(v => v.IsValid(endpoint.Uri)).Returns(false);

        var act = () => _sut.ValidateAndMap([endpoint]);

        act.Should().Throw<ParserEndpointValidationException>()
            .WithMessage("*Mosmix*invalid URI*");
    }

    [TestMethod]
    public void ValidateAndMap_WithDuplicateEndpointName_ThrowsParserEndpointValidationException()
    {
        var first = new ParserEndpointDefinition("Mosmix", new Uri("https://example.org/mosmix-a"));
        var duplicate = new ParserEndpointDefinition("Mosmix", new Uri("https://example.org/mosmix-b"));
        _uriValidatorMock.Setup(v => v.IsValid(first.Uri)).Returns(true);
        _uriValidatorMock.Setup(v => v.IsValid(duplicate.Uri)).Returns(true);

        var act = () => _sut.ValidateAndMap([first, duplicate]);

        act.Should().Throw<ParserEndpointValidationException>()
            .WithMessage("*Mosmix*defined more than once*");
    }
}
