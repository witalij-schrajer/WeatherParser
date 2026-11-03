using System;
using FluentAssertions;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Tests.Exceptions;

[TestClass]
public sealed class SelectionAndEvidenceCacheExceptionTests
{
    [TestMethod]
    [DataRow("SELECTION-TOO-MANY-CANDIDATES")]
    [DataRow("SELECTION-DEADLINE-EXCEEDED")]
    [DataRow("SELECTION-LIMIT-INVALID")]
    public void SelectionLimitException_AcceptsEachContractId(string id)
    {
        // Act
        var ex = new SelectionLimitException(id, "Message.");

        // Assert
        ex.ErrorId.Should().Be(id);
        ex.Message.Should().Be("Message.");
    }

    [TestMethod]
    public void SelectionLimitException_RejectsUnknownId()
    {
        // Act
        FluentActions.Invoking(() => new SelectionLimitException("OTHER", "Message."))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    [DataRow("EVIDENCE-CACHE-SETTINGS-INVALID")]
    [DataRow("EVIDENCE-CACHE-FAILED")]
    [DataRow("MOSMIX-S-REFRESH-DEADLINE-EXCEEDED")]
    [DataRow("MOSMIX-S-EVIDENCE-MISSING")]
    public void EvidenceCacheException_AcceptsEachContractId(string id)
    {
        // Act
        var ex = new EvidenceCacheException(id, "Message.");

        // Assert
        ex.ErrorId.Should().Be(id);
        ex.Message.Should().Be("Message.");
    }

    [TestMethod]
    public void EvidenceCacheException_RejectsUnknownId()
    {
        // Act
        FluentActions.Invoking(() => new EvidenceCacheException("OTHER", "Message."))
            .Should().Throw<ArgumentException>();
    }
}
