using FluentAssertions;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Tests.Exceptions;

[TestClass]
public sealed class SearchParameterExceptionTests
{
    [TestMethod]
    public void Constructor_SetsMessageAndErrorId()
    {
        // Arrange
        const string message = "Maximum distance must not be negative.";

        // Act
        var exception = new SearchParameterException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorId.Should().Be("SEARCH-PARAMETER-INVALID");
    }

    [TestMethod]
    public void ErrorId_IsReadOnly()
    {
        // Act
        var canWrite = typeof(SearchParameterException).GetProperty("ErrorId")!.CanWrite;

        // Assert
        canWrite.Should().BeFalse();
    }

    [TestMethod]
    public void Type_IsPublicSealedException()
    {
        // Act
        var type = typeof(SearchParameterException);

        // Assert
        type.IsPublic.Should().BeTrue();
        type.IsSealed.Should().BeTrue();
        type.IsAssignableTo(typeof(Exception)).Should().BeTrue();
    }
}
