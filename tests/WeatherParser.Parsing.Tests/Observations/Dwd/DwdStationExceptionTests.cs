using System;
using FluentAssertions;
using WeatherParser.Parsing.Catalogs;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Tests.Observations.Dwd;

[TestClass]
public sealed class DwdStationExceptionTests
{
    [TestMethod]
    [DataRow("STATION-ID-INVALID")]
    [DataRow("STATION-UNKNOWN")]
    [DataRow("STATION-METADATA-CONFLICT")]
    public void DwdStationException_AcceptsEachContractId(string id)
    {
        // Act
        var ex = new DwdStationException(id, "Message.");

        // Assert
        ex.ErrorId.Should().Be(id);
        ex.Message.Should().Be("Message.");
        ex.Diagnostic.Should().BeNull();
    }

    [TestMethod]
    public void DwdStationException_KeepsDiagnostic()
    {
        // Arrange
        var diagnostic = new CatalogDiagnostic(CatalogDiagnostic.StationConflictId, "tu-list", 3, "00044", "Reason.");

        // Act
        var ex = new DwdStationException(DwdStationException.StationMetadataConflictErrorId, "Message.", diagnostic);

        // Assert
        ex.Diagnostic.Should().BeSameAs(diagnostic);
    }

    [TestMethod]
    public void DwdStationException_UnknownErrorId_ThrowsArgumentException()
    {
        // Act
        FluentActions.Invoking(() => new DwdStationException("OTHER", "Message."))
            .Should().Throw<ArgumentException>();
    }
}
