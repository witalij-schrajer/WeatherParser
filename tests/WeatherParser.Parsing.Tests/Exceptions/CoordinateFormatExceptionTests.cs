using FluentAssertions;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Tests.Exceptions;

[TestClass]
public sealed class CoordinateFormatExceptionTests
{
    [TestMethod]
    public void Constructor_SetsMessageAndErrorId()
    {
        // Arrange
        const string message = "Latitude is out of range.";

        // Act
        var exception = new CoordinateFormatException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorId.Should().Be("COORDINATE-INVALID");
    }

    [TestMethod]
    public void ErrorId_IsReadOnly()
    {
        // Act
        var canWrite = typeof(CoordinateFormatException).GetProperty("ErrorId")!.CanWrite;

        // Assert
        canWrite.Should().BeFalse();
    }

    [TestMethod]
    public void Type_IsPublicSealedException()
    {
        // Act
        var type = typeof(CoordinateFormatException);

        // Assert
        type.IsPublic.Should().BeTrue();
        type.IsSealed.Should().BeTrue();
        type.IsAssignableTo(typeof(Exception)).Should().BeTrue();
    }
}
