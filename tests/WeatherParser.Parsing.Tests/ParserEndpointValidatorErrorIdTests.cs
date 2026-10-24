namespace WeatherParser.Parsing.Tests;

using FluentAssertions;
using WeatherParser.Parsing.Parsing;

[TestClass]
public sealed class ParserEndpointValidatorErrorIdTests
{
    private ParserEndpointValidator _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new ParserEndpointValidator(new EndpointUriValidator());
    }

    [TestMethod]
    public void ParserEndpointValidationException_HasErrorId_EndpointInvalid()
    {
        var exception = new ParserEndpointValidationException("message");
        exception.ErrorId.Should().Be("ENDPOINT-INVALID");
    }

    [TestMethod]
    public void Validator_InvalidEndpointWithCredentials_MessageIsSanitized()
    {
        Action act = () => _subject.ValidateAndMap(new[] { new ParserEndpointDefinition("Catalog", new Uri("ftp://user:password@example.org/catalog?token=secret")) });
        var exception = act.Should().Throw<ParserEndpointValidationException>().Which;
        exception.ErrorId.Should().Be("ENDPOINT-INVALID");
        exception.Message.Should().Contain("Catalog");
        exception.Message.Should().NotContain("password");
        exception.Message.Should().NotContain("secret");
    }

    [TestMethod]
    public void ParserEndpointValidator_DuplicateEndpointName_ThrowsEndpointInvalid()
    {
        Action act = () => _subject.ValidateAndMap(new[] { new ParserEndpointDefinition("Catalog", new Uri("https://example.org/a")), new ParserEndpointDefinition("Catalog", new Uri("https://example.org/b")) });
        var exception = act.Should().Throw<ParserEndpointValidationException>().Which;
        exception.ErrorId.Should().Be("ENDPOINT-INVALID");
    }
}
