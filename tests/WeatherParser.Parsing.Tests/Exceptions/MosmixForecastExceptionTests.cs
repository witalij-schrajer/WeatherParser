using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Forecasts;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Observations;
using WeatherParser.Parsing.Quantities;

namespace WeatherParser.Parsing.Tests.Exceptions;

[TestClass]
public sealed class MosmixForecastExceptionTests
{
    [TestMethod]
    [DataRow("MOSMIX-ID-INVALID")]
    [DataRow("MOSMIX-ID-UNKNOWN")]
    [DataRow("MOSMIX-RETRIEVAL-FAILED")]
    [DataRow("MOSMIX-DEADLINE-EXCEEDED")]
    [DataRow("MOSMIX-TOO-LARGE")]
    [DataRow("MOSMIX-FORMAT-INVALID")]
    [DataRow("MOSMIX-ISSUE-TIME-MISSING")]
    [DataRow("MOSMIX-LOCATION-MISMATCH")]
    [DataRow("MOSMIX-NO-USABLE-VALUE")]
    [DataRow("MOSMIX-OPTIONS-INVALID")]
    public void MosmixForecastException_AcceptsEachContractId(string id)
    {
        // Act
        var ex = new MosmixForecastException(id, MosmixProduct.MosmixL, "Message.");

        // Assert
        ex.ErrorId.Should().Be(id);
        ex.Product.Should().Be(MosmixProduct.MosmixL);
        ex.Message.Should().Be("Message.");
        ex.OmittedValues.Should().BeEmpty();
        ex.NotExportedElements.Should().BeEmpty();
    }

    [TestMethod]
    public void MosmixForecastException_ConstantsMatchContract()
    {
        // Act + Assert
        MosmixForecastErrorIds.IdInvalid.Should().Be("MOSMIX-ID-INVALID");
        MosmixForecastErrorIds.IdUnknown.Should().Be("MOSMIX-ID-UNKNOWN");
        MosmixForecastErrorIds.RetrievalFailed.Should().Be("MOSMIX-RETRIEVAL-FAILED");
        MosmixForecastErrorIds.DeadlineExceeded.Should().Be("MOSMIX-DEADLINE-EXCEEDED");
        MosmixForecastErrorIds.TooLarge.Should().Be("MOSMIX-TOO-LARGE");
        MosmixForecastErrorIds.FormatInvalid.Should().Be("MOSMIX-FORMAT-INVALID");
        MosmixForecastErrorIds.IssueTimeMissing.Should().Be("MOSMIX-ISSUE-TIME-MISSING");
        MosmixForecastErrorIds.LocationMismatch.Should().Be("MOSMIX-LOCATION-MISMATCH");
        MosmixForecastErrorIds.NoUsableValue.Should().Be("MOSMIX-NO-USABLE-VALUE");
        MosmixForecastErrorIds.OptionsInvalid.Should().Be("MOSMIX-OPTIONS-INVALID");
    }

    [TestMethod]
    [DataRow("OTHER")]
    [DataRow("OSTHESSEN-OPTIONS-INVALID")]
    [DataRow("")]
    [DataRow(null)]
    public void MosmixForecastException_UnknownOrNullErrorId_ThrowsArgumentException(string? id)
    {
        // Act
        FluentActions.Invoking(() => new MosmixForecastException(id!, MosmixProduct.MosmixL, "Message."))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void MosmixForecastException_UndefinedProduct_ThrowsArgumentException()
    {
        // Act
        FluentActions.Invoking(() => new MosmixForecastException(MosmixForecastErrorIds.IdInvalid, (MosmixProduct)99, "Message."))
            .Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void MosmixForecastException_WithLists_CopiesOmissionsAndElements()
    {
        // Arrange
        var omitted = new OmittedForecastValue(new DateTime(2026, 10, 27, 6, 0, 0, DateTimeKind.Utc), QuantityKind.Temperature, OmissionReason.OutOfRange);
        var omittedValues = new List<OmittedForecastValue> { omitted };
        var notExportedElements = new List<string> { "SunD1" };

        // Act
        var ex = new MosmixForecastException(MosmixForecastErrorIds.NoUsableValue, MosmixProduct.MosmixS, "Message.", omittedValues, notExportedElements);

        // Assert
        omittedValues.Clear();
        notExportedElements.Clear();
        ex.OmittedValues.Should().ContainSingle().Which.Should().Be(omitted);
        ex.NotExportedElements.Should().Equal("SunD1");
        ex.ErrorId.Should().Be(MosmixForecastErrorIds.NoUsableValue);
        ex.Product.Should().Be(MosmixProduct.MosmixS);
    }

    [TestMethod]
    public void MosmixForecastException_WithLists_NullOmittedValues_ThrowArgumentNullException()
    {
        // Act
        FluentActions.Invoking(() => new MosmixForecastException(MosmixForecastErrorIds.NoUsableValue, MosmixProduct.MosmixS, "Message.", null!, Array.Empty<string>()))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("omittedValues");
    }

    [TestMethod]
    public void MosmixForecastException_WithLists_NullElements_ThrowArgumentNullException()
    {
        // Act
        FluentActions.Invoking(() => new MosmixForecastException(MosmixForecastErrorIds.NoUsableValue, MosmixProduct.MosmixS, "Message.", Array.Empty<OmittedForecastValue>(), null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("notExportedElements");
    }

    [TestMethod]
    public void MosmixForecastException_WithLists_UnknownId_ThrowsArgumentException()
    {
        // Act
        FluentActions.Invoking(() => new MosmixForecastException("OTHER", MosmixProduct.MosmixS, "Message.", Array.Empty<OmittedForecastValue>(), Array.Empty<string>()))
            .Should().Throw<ArgumentException>();
    }
}
