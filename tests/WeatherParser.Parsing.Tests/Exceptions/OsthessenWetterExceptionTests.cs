using System;
using System.Collections.Generic;
using FluentAssertions;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Observations.Osthessen;

namespace WeatherParser.Parsing.Tests.Exceptions;

[TestClass]
public sealed class OsthessenWetterExceptionTests
{
    [TestMethod]
    [DataRow("OSTHESSEN-PATTERN-INVALID")]
    [DataRow("OSTHESSEN-PATTERN-NO-MATCH")]
    [DataRow("OSTHESSEN-PAGE-UNAVAILABLE")]
    [DataRow("OSTHESSEN-DEADLINE-EXCEEDED")]
    [DataRow("OSTHESSEN-FORMAT-CHANGED")]
    [DataRow("OSTHESSEN-STATION-ID-COLLISION")]
    [DataRow("OSTHESSEN-NO-USABLE-STATION")]
    [DataRow("OSTHESSEN-OPTIONS-INVALID")]
    public void OsthessenWetterException_AcceptsEachContractId(string id)
    {
        // Act
        var ex = new OsthessenWetterException(id, "Message.");

        // Assert
        ex.ErrorId.Should().Be(id);
        ex.Message.Should().Be("Message.");
    }

    [TestMethod]
    public void OsthessenWetterException_ConstantsMatchContract()
    {
        // Act + Assert
        OsthessenWetterException.PatternInvalidErrorId.Should().Be("OSTHESSEN-PATTERN-INVALID");
        OsthessenWetterException.PatternNoMatchErrorId.Should().Be("OSTHESSEN-PATTERN-NO-MATCH");
        OsthessenWetterException.PageUnavailableErrorId.Should().Be("OSTHESSEN-PAGE-UNAVAILABLE");
        OsthessenWetterException.DeadlineExceededErrorId.Should().Be("OSTHESSEN-DEADLINE-EXCEEDED");
        OsthessenWetterException.FormatChangedErrorId.Should().Be("OSTHESSEN-FORMAT-CHANGED");
        OsthessenWetterException.StationIdCollisionErrorId.Should().Be("OSTHESSEN-STATION-ID-COLLISION");
        OsthessenWetterException.NoUsableStationErrorId.Should().Be("OSTHESSEN-NO-USABLE-STATION");
        OsthessenWetterException.OptionsInvalidErrorId.Should().Be("OSTHESSEN-OPTIONS-INVALID");
    }

    [TestMethod]
    public void OsthessenWetterException_UnknownErrorId_ThrowsArgumentException()
    {
        // Act
        FluentActions.Invoking(() => new OsthessenWetterException("OTHER", "Message."))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void OsthessenWetterException_NullErrorId_ThrowsArgumentException()
    {
        // Act
        FluentActions.Invoking(() => new OsthessenWetterException(null!, "Message."))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void OsthessenWetterException_StationFailures_DefaultEmpty()
    {
        new OsthessenWetterException(OsthessenWetterException.PageUnavailableErrorId, "Message.").StationFailures.Should().BeEmpty();
    }

    [TestMethod]
    public void OsthessenWetterException_StationFailures_Copied()
    {
        var failures = new List<OsthessenWetterStationFailure> { new("Langenbieber", "OSTHESSEN-STATION-OFFLINE", "offline") };
        var ex = new OsthessenWetterException(OsthessenWetterException.NoUsableStationErrorId, "Message.", failures);
        failures.Add(new("Schlitz", "OSTHESSEN-STATION-METADATA-MISSING", "missing"));
        ex.StationFailures.Should().Equal(new OsthessenWetterStationFailure("Langenbieber", "OSTHESSEN-STATION-OFFLINE", "offline"));
        ex.ErrorId.Should().Be(OsthessenWetterException.NoUsableStationErrorId);
    }

    [TestMethod]
    public void OsthessenWetterException_NullStationFailures_ThrowsArgumentNullException()
    {
        FluentActions.Invoking(() => new OsthessenWetterException(OsthessenWetterException.NoUsableStationErrorId, "Message.", null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("stationFailures");
    }
}
