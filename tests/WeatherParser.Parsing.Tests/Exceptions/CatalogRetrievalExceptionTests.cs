namespace WeatherParser.Parsing.Tests.Exceptions;

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Parsing.Exceptions;

[TestClass]
public sealed class CatalogRetrievalExceptionTests
{
    [DataRow(CatalogRetrievalException.RetrievalFailedErrorId)]
    [DataRow(CatalogRetrievalException.TimeoutErrorId)]
    [DataRow(CatalogRetrievalException.TooLargeErrorId)]
    [TestMethod]
    public void Constructor_AcceptsEachContractId(string errorId)
    {
        var ex = new CatalogRetrievalException(errorId, "WindStationList", "Message.");
        ex.ErrorId.Should().Be(errorId);
        ex.EndpointName.Should().Be("WindStationList");
        ex.Message.Should().Be("Message.");
    }

    [TestMethod]
    public void Constructor_RejectsUnknownId()
    {
        FluentActions.Invoking(() => new CatalogRetrievalException("OTHER", "WindStationList", "m"))
            .Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("errorId");
    }

    [TestMethod]
    public void Constructor_RejectsNullEndpointName()
    {
        FluentActions.Invoking(() => new CatalogRetrievalException("CATALOG-TIMEOUT", null!, "m"))
            .Should().Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("endpointName");
    }
}
