using WeatherParser.Parsing.Export;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Cli.Diagnostics;
using WeatherParser.Parsing.Exceptions;
using WeatherParser.Parsing.Forecasts.Mosmix;
using WeatherParser.Parsing.Mqtt;
using WeatherParser.Parsing.Parsing;

namespace WeatherParser.Cli.Tests.Diagnostics;

[TestClass]
public class LibraryErrorsTests
{
    private ILibraryErrors _errors = null!;

    [TestInitialize]
    public void Initialize()
    {
        _errors = new LibraryErrors();
    }

    [TestMethod]
    public void Map_EachLibraryExceptionType_KeepsLibraryErrorId()
    {
        // CatalogFormatException
        var ex1 = new CatalogFormatException(CatalogFormatException.CatalogFormatInvalidErrorId, "m");
        Assert.IsTrue(_errors.TryGetErrorId(ex1, out string id1));
        Assert.AreEqual("CATALOG-FORMAT-INVALID", id1);

        // CatalogRetrievalException
        var ex2 = new CatalogRetrievalException(CatalogRetrievalException.RetrievalFailedErrorId, "endpoint", "m");
        Assert.IsTrue(_errors.TryGetErrorId(ex2, out string id2));
        Assert.AreEqual("CATALOG-RETRIEVAL-FAILED", id2);

        // CoordinateFormatException
        var ex3 = new CoordinateFormatException("m");
        Assert.IsTrue(_errors.TryGetErrorId(ex3, out string id3));
        Assert.AreEqual("COORDINATE-INVALID", id3);

        // DwdStationException
        var ex4 = new DwdStationException(DwdStationException.StationIdInvalidErrorId, "m");
        Assert.IsTrue(_errors.TryGetErrorId(ex4, out string id4));
        Assert.AreEqual("STATION-ID-INVALID", id4);

        // EvidenceCacheException
        var ex5 = new EvidenceCacheException(EvidenceCacheException.CacheFailedErrorId, "m");
        Assert.IsTrue(_errors.TryGetErrorId(ex5, out string id5));
        Assert.AreEqual("EVIDENCE-CACHE-FAILED", id5);

        // ObservationLimitException
        var ex6 = new ObservationLimitException(ObservationLimitException.LimitInvalidErrorId, "m");
        Assert.IsTrue(_errors.TryGetErrorId(ex6, out string id6));
        Assert.AreEqual("OBSERVATION-LIMIT-INVALID", id6);

        // OsthessenWetterException
        var ex7 = new OsthessenWetterException(OsthessenWetterException.PageUnavailableErrorId, "m");
        Assert.IsTrue(_errors.TryGetErrorId(ex7, out string id7));
        Assert.AreEqual("OSTHESSEN-PAGE-UNAVAILABLE", id7);

        // SearchParameterException
        var ex8 = new SearchParameterException("m");
        Assert.IsTrue(_errors.TryGetErrorId(ex8, out string id8));
        Assert.AreEqual("SEARCH-PARAMETER-INVALID", id8);

        // SelectionLimitException
        var ex9 = new SelectionLimitException(SelectionLimitException.LimitInvalidErrorId, "m");
        Assert.IsTrue(_errors.TryGetErrorId(ex9, out string id9));
        Assert.AreEqual("SELECTION-LIMIT-INVALID", id9);

        // MosmixForecastException
        var ex10 = new MosmixForecastException(MosmixForecastErrorIds.IdInvalid, MosmixProduct.MosmixL, "m");
        Assert.IsTrue(_errors.TryGetErrorId(ex10, out string id10));
        Assert.AreEqual("MOSMIX-ID-INVALID", id10);

        // MosmixForecastFormatException
        var ex11 = new MosmixForecastFormatException("MOSMIX-FORMAT-INVALID", "m");
        Assert.IsTrue(_errors.TryGetErrorId(ex11, out string id11));
        Assert.AreEqual("MOSMIX-FORMAT-INVALID", id11);

        // MqttMessageException
        var ex12 = new MqttMessageException(MqttMessageErrorIds.MappingMissing, MqttMessageType.Observation, "m");
        Assert.IsTrue(_errors.TryGetErrorId(ex12, out string id12));
        Assert.AreEqual("MQTT-MAPPING-MISSING", id12);

        // ParserEndpointValidationException
        var ex13 = new ParserEndpointValidationException("m");
        Assert.IsTrue(_errors.TryGetErrorId(ex13, out string id13));
        Assert.AreEqual("ENDPOINT-INVALID", id13);
    }

    [TestMethod]
    public void Map_OtherException_NotALibraryError()
    {
        var ex = new InvalidOperationException("x");
        Assert.IsFalse(_errors.TryGetErrorId(ex, out _));
    }
}
