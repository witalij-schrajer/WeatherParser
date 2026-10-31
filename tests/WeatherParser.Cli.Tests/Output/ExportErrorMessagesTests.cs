using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WeatherParser.Cli.Output;
using WeatherParser.Parsing.Export;

namespace WeatherParser.Cli.Tests.Output;

[TestClass]
[TestCategory("T-P3-11")]
public class ExportErrorMessagesTests
{
    private IExportErrorMessages _messages = null!;

    [TestInitialize]
    public void Initialize()
    {
        _messages = new ExportErrorMessages();
    }

    [TestMethod]
    [DataRow("STATION-UNKNOWN", "The DWD station is not in the station catalog.")]
    [DataRow("STATION-METADATA-CONFLICT", "The DWD station catalogs give conflicting station metadata.")]
    [DataRow("CATALOG-RETRIEVAL-FAILED", "A source file could not be retrieved.")]
    [DataRow("CATALOG-TIMEOUT", "Retrieving a source file timed out.")]
    [DataRow("CATALOG-TOO-LARGE", "A source file exceeded its size limit.")]
    [DataRow("CATALOG-FORMAT-INVALID", "A source file has an unexpected format.")]
    [DataRow("OBSERVATION-DEADLINE-EXCEEDED", "The observation retrieval exceeded its deadline.")]
    [DataRow("MOSMIX-FORMAT-INVALID", "The MOSMIX forecast file has an unexpected format.")]
    [DataRow("MOSMIX-ISSUE-TIME-MISSING", "The MOSMIX forecast file has no issue time.")]
    [DataRow("MOSMIX-LOCATION-MISMATCH", "The MOSMIX forecast file describes a different location.")]
    [DataRow("MOSMIX-ID-UNKNOWN", "The MOSMIX location is not in the forecast file.")]
    [DataRow("MOSMIX-TOO-LARGE", "The MOSMIX forecast file exceeded its size limit.")]
    [DataRow("MOSMIX-RETRIEVAL-FAILED", "The MOSMIX forecast file could not be retrieved.")]
    [DataRow("MOSMIX-DEADLINE-EXCEEDED", "The MOSMIX forecast parse exceeded its deadline.")]
    [DataRow("MOSMIX-NO-USABLE-VALUE", "The MOSMIX forecast has no usable value for the location.")]
    [DataRow("OSTHESSEN-STATION-OFFLINE", "The OsthessenWetter station is reported offline.")]
    [DataRow("OSTHESSEN-STATION-METADATA-MISSING", "No metadata is configured for the OsthessenWetter station.")]
    [DataRow("OSTHESSEN-STATION-NO-READING", "The OsthessenWetter station shows no usable reading.")]
    [DataRow("OSTHESSEN-FORMAT-CHANGED", "The OsthessenWetter page deviates from the supported revision.")]
    [DataRow("OBSERVATION-NO-USABLE-READING", "The DWD station has no usable reading.")]
    public void For_KnownErrorId_FixedEnglishText(string errorId, string expected)
    {
        _messages.For(errorId).Should().Be(expected);
    }

    [TestMethod]
    public void For_UnknownErrorId_GenericFixedText()
    {
        _messages.For("SOME-OTHER-ID").Should().Be("The requested item failed; the error ID names the cause.");
    }

    [TestMethod]
    public void For_EveryText_IsAValidExportErrorMessage()
    {
        var id = "MOSMIX-ID-UNKNOWN";
        var error = new ExportError { ErrorId = id, Message = _messages.For(id), Source = "dwd-mosmix-l:10637" };
        error.Message.Should().NotContain("\n").And.NotContain("\r");

        id = "SOME-OTHER-ID";
        error = new ExportError { ErrorId = id, Message = _messages.For(id), Source = "dwd-mosmix-l:10637" };
        error.Message.Should().NotContain("\n").And.NotContain("\r");
    }
}
