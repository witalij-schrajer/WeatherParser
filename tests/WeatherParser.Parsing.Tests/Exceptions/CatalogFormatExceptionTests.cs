using FluentAssertions;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Tests.Exceptions;

[TestClass]
public sealed class CatalogFormatExceptionTests
{
    [TestMethod]
    public void Constructor_SetsMessageAndErrorId_CatalogFormatInvalid()
    {
        // Arrange
        const string message = "Catalog format is invalid.";

        // Act
        var exception = new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorId.Should().Be("CATALOG-FORMAT-INVALID");
    }

    [TestMethod]
    public void Constructor_SetsMessageAndErrorId_CdcEvidenceInvalid()
    {
        // Arrange
        const string message = "CDC evidence is invalid.";

        // Act
        var exception = new CatalogFormatException(CatalogFormatException.CdcEvidenceInvalidErrorId, message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorId.Should().Be("CDC-EVIDENCE-INVALID");
    }

    [TestMethod]
    public void Constructor_SetsMessageAndErrorId_MosmixEvidenceInvalid()
    {
        // Arrange
        const string message = "MOSMIX evidence is invalid.";

        // Act
        var exception = new CatalogFormatException(CatalogFormatException.MosmixEvidenceInvalidErrorId, message);

        // Assert
        exception.Message.Should().Be(message);
        exception.ErrorId.Should().Be("MOSMIX-EVIDENCE-INVALID");
    }

    [TestMethod]
    public void Constructor_UnknownErrorId_ThrowsArgumentException()
    {
        // Act & Assert
        var act = () => new CatalogFormatException("OTHER-ID", "Some message.");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unknown error ID*")
            .And.ParamName.Should().Be("errorId");
    }

    [TestMethod]
    public void ErrorId_IsReadOnly()
    {
        // Act
        var canWrite = typeof(CatalogFormatException).GetProperty("ErrorId")!.CanWrite;

        // Assert
        canWrite.Should().BeFalse();
    }

    [TestMethod]
    public void Type_IsPublicSealedException()
    {
        // Act
        var type = typeof(CatalogFormatException);

        // Assert
        type.IsPublic.Should().BeTrue();
        type.IsSealed.Should().BeTrue();
        type.IsAssignableTo(typeof(Exception)).Should().BeTrue();
    }
}
