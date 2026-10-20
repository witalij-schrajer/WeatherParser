using System.Text;
using System.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.Parsing.Tests.Schemas;

[TestClass]
public sealed class SecureValidationTests
{
    private string _directory = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "weatherparser-p209-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        Directory.Delete(_directory, true);
    }

    [TestMethod]
    public void Validation_DocumentWithDtd_IsRejected()
    {
        var document = Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><!DOCTYPE observation_dataset [<!ENTITY e \"x\">]><observation_dataset xmlns=\"urn:weatherparser:export:v1\">&e;</observation_dataset>");
        FluentActions.Invoking(() => ExportSchemaValidation.Validate(document)).Should().Throw<XmlException>();
    }

    [DataRow("file")]
    [DataRow("http")]
    [TestMethod]
    public void Validation_SchemaImport_IsNeverResolved(string kind)
    {
        var importedContent = @"<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""urn:x""><xs:simpleType name=""ItemType""><xs:restriction base=""xs:string"" /></xs:simpleType></xs:schema>";
        var importedPath = Path.Combine(_directory, "imported.xsd");
        File.WriteAllText(importedPath, importedContent);

        var location = kind == "file"
            ? new Uri(importedPath).AbsoluteUri
            : "http://127.0.0.1:9/imported.xsd";

        var mainContent = $@"<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:x=""urn:x"" targetNamespace=""urn:t""><xs:import namespace=""urn:x"" schemaLocation=""{location}"" /><xs:element name=""t"" type=""x:ItemType"" /></xs:schema>";
        var mainPath = Path.Combine(_directory, "main.xsd");
        File.WriteAllText(mainPath, mainContent);

        var events = new List<string>();
        Action load = () => ExportSchemaValidation.LoadSchemas(mainPath, (_, e) => events.Add(e.Message));
        load.Should().NotThrow();
        events.Should().Contain(message => message.Contains("ItemType"));
    }

    [TestMethod]
    public void TimestampSemantics_DocumentWithDtd_IsRejected()
    {
        var document = Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><!DOCTYPE observation_dataset [<!ENTITY e \"x\">]><observation_dataset xmlns=\"urn:weatherparser:export:v1\">&e;</observation_dataset>");
        FluentActions.Invoking(() => TimestampSemantics.Violations(document)).Should().Throw<XmlException>();
    }
}
