using System;
using FluentAssertions;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class MosmixEvidenceTimestampWireTests
{
    [TestMethod]
    public void WireNull_ClearsTheValue()
    {
        // Arrange
        var sut = new MosmixProductAvailability();

        var utc = new DateTime(2026, 10, 21, 4, 12, 7, DateTimeKind.Utc);

        sut.EvidenceRetrievedAtUtc = utc;
        sut.EvidenceIssuedAtUtc = utc;

        // Act
        sut.EvidenceRetrievedAtWire = null;
        sut.EvidenceIssuedAtWire = null;

        // Assert
        sut.EvidenceRetrievedAtUtc.Should().BeNull();
        sut.EvidenceIssuedAtUtc.Should().BeNull();
    }

    [TestMethod]
    public void SpecifiedFalse_ClearsTheValue_AndTrueKeepsIt()
    {
        // Arrange
        var sut = new MosmixProductAvailability();

        var utc = new DateTime(2026, 10, 21, 4, 12, 7, DateTimeKind.Utc);

        sut.EvidenceRetrievedAtUtc = utc;
        sut.EvidenceIssuedAtUtc = utc;

        // Act
        sut.EvidenceRetrievedAtWireSpecified = true;
        sut.EvidenceIssuedAtWireSpecified = true;

        // Assert
        sut.EvidenceRetrievedAtUtc.Should().NotBeNull();
        sut.EvidenceIssuedAtUtc.Should().NotBeNull();

        // Act
        sut.EvidenceRetrievedAtWireSpecified = false;
        sut.EvidenceIssuedAtWireSpecified = false;

        // Assert
        sut.EvidenceRetrievedAtUtc.Should().BeNull();
        sut.EvidenceIssuedAtUtc.Should().BeNull();
    }
}
