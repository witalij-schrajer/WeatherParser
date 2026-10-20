using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class ExportErrorRuleTests
{
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("dwd-retrieval-failed")]
    [DataRow("DWD--FAILED")]
    [DataRow("-DWD")]
    [DataRow("DWD-")]
    [DataRow("1DWD")]
    [DataRow("DWD FAILED")]
    [DataRow("DWD_FAILED")]
    [DataRow("DWD-RETRIEVAL-FAILED\n")]
    public void ErrorId_ViolatingGrammar_IsRejected(string value)
    {
        var error = new ExportError();
        Action act = () => error.ErrorId = value;
        act.Should().Throw<ArgumentException>().WithParameterName("ErrorId");
    }

    [TestMethod]
    [DataRow("DWD-RETRIEVAL-FAILED")]
    [DataRow("E1")]
    [DataRow("MOSMIX-KMZ-2")]
    public void ErrorId_MatchingGrammar_IsKept(string value)
    {
        var error = new ExportError();
        error.ErrorId = value;
        error.ErrorId.Should().Be(value);
    }

    [TestMethod]
    public void ErrorId_Null_IsRejected()
    {
        var error = new ExportError();
        Action act = () => error.ErrorId = null!;
        act.Should().Throw<ArgumentException>().WithParameterName("ErrorId");
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("Line one\nline two")]
    [DataRow("Line one\rline two")]
    [DataRow("Line one\r\nline two")]
    public void Message_EmptyWhitespaceOrMultiLine_IsRejected(string value)
    {
        var error = new ExportError();
        Action act = () => error.Message = value;
        act.Should().Throw<ArgumentException>().WithParameterName("Message");
    }

    [TestMethod]
    public void Message_Null_IsRejected()
    {
        var error = new ExportError();
        Action act = () => error.Message = null!;
        act.Should().Throw<ArgumentException>().WithParameterName("Message");
    }

    [TestMethod]
    public void Message_SingleEnglishLine_IsKept()
    {
        var error = new ExportError();
        var value = "Retrieval of station 01766 failed: HTTP 503.";
        error.Message = value;
        error.Message.Should().Be(value);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("dwd")]
    [DataRow("DWD:00044")]
    [DataRow(":00044")]
    [DataRow("dwd:")]
    [DataRow("dwd:   ")]
    [DataRow("1dwd:00044")]
    [DataRow("dwd_x:00044")]
    public void Source_ViolatingProviderIdForm_IsRejected(string value)
    {
        var error = new ExportError();
        Action act = () => error.Source = value;
        act.Should().Throw<ArgumentException>().WithParameterName("Source");
    }

    [TestMethod]
    public void Source_Null_IsRejected()
    {
        var error = new ExportError();
        Action act = () => error.Source = null!;
        act.Should().Throw<ArgumentException>().WithParameterName("Source");
    }

    [TestMethod]
    [DataRow("dwd:00044")]
    [DataRow("dwd-mosmix-l:10637")]
    [DataRow("osthessenwetter:a:b")]
    public void Source_ProviderIdForm_IsKept(string value)
    {
        var error = new ExportError();
        error.Source = value;
        error.Source.Should().Be(value);
    }

    [TestMethod]
    [DataRow("ErrorId")]
    [DataRow("Message")]
    [DataRow("Source")]
    public async Task Serialize_ErrorWithUnassignedMember_FailsInBothFormatsAndWritesNothing(string missing)
    {
        var error = new ExportError();
        if (missing != "ErrorId") error.ErrorId = "DWD-RETRIEVAL-FAILED";
        if (missing != "Message") error.Message = "Retrieval failed.";
        if (missing != "Source") error.Source = "dwd:01766";

        var document = new ExportDocument { Errors = new List<ExportError> { error } };

        using var jsonStream = new MemoryStream();
        Func<Task> jsonAct = () => _json.SerializeAsync(document, jsonStream, CancellationToken.None);
        await jsonAct.Should().ThrowAsync<ExportSerializationException>();
        jsonStream.Length.Should().Be(0);

        using var xmlStream = new MemoryStream();
        Func<Task> xmlAct = () => _xml.SerializeAsync(document, xmlStream, CancellationToken.None);
        await xmlAct.Should().ThrowAsync<ExportSerializationException>();
        xmlStream.Length.Should().Be(0);
    }
}
