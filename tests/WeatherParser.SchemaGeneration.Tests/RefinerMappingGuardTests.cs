using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WeatherParser.SchemaGeneration.Tests;

[TestClass]
public sealed class RefinerMappingGuardTests
{
    [XmlRoot("implicit_root", Namespace = "urn:weatherparser:test")]
    public sealed class ImplicitMappingRoot
    {
        [XmlElement("name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = "x";

        public string Undeclared { get; set; } = "y";
    }

    [XmlRoot("collection_root", Namespace = "urn:weatherparser:test")]
    public sealed class ImplicitCollectionRoot
    {
        [XmlElement("name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = "x";

        public Collection<string> Undeclared { get; } = new();
    }

    [XmlRoot("explicit_root", Namespace = "urn:weatherparser:test")]
    public sealed class ExplicitMappingRoot
    {
        [XmlElement("name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = "x";

        [XmlIgnore]
        [JsonIgnore]
        public string Ignored { get; set; } = "y";
    }

    [XmlRoot("collection_child_root", Namespace = "urn:weatherparser:test")]
    public sealed class CollectionChildRoot
    {
        [XmlArray("items")]
        [XmlArrayItem("item")]
        [JsonPropertyName("items")]
        public Collection<RequiredChild> Items { get; } = new();
    }

    public sealed class RequiredChild
    {
        [XmlElement("code")]
        [JsonPropertyName("code")]
        public string Code { get; set; } = "x";
    }

    private static XDocument Export(Type root)
    {
        var schemas = new XmlSchemas();
        new XmlSchemaExporter(schemas).ExportTypeMapping(new XmlReflectionImporter().ImportTypeMapping(root));
        using var stream = new MemoryStream();
        schemas[0].Write(stream);
        return XDocument.Parse(Encoding.UTF8.GetString(stream.ToArray()));
    }

    [TestMethod]
    public void Refine_PropertyWithoutExplicitXmlMapping_Throws()
    {
        IExportSchemaRefiner refiner = new ExportSchemaRefiner();
        var schema = Export(typeof(ImplicitMappingRoot));
        FluentActions.Invoking(() => refiner.Refine(schema, new[] { typeof(ImplicitMappingRoot) }))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*ImplicitMappingRoot.Undeclared*");
    }

    [TestMethod]
    public void Refine_GetterOnlyCollectionWithoutExplicitXmlMapping_Throws()
    {
        IExportSchemaRefiner refiner = new ExportSchemaRefiner();
        var schema = Export(typeof(ImplicitCollectionRoot));
        FluentActions.Invoking(() => refiner.Refine(schema, new[] { typeof(ImplicitCollectionRoot) }))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("*ImplicitCollectionRoot.Undeclared*");
    }

    [TestMethod]
    public void Refine_ExplicitAndIgnoredProperties_AreAccepted()
    {
        IExportSchemaRefiner refiner = new ExportSchemaRefiner();
        var schema = Export(typeof(ExplicitMappingRoot));
        FluentActions.Invoking(() => refiner.Refine(schema, new[] { typeof(ExplicitMappingRoot) }))
            .Should().NotThrow();
    }

    [TestMethod]
    public void Refine_EntityInsideNonListCollection_IsRefined()
    {
        IExportSchemaRefiner refiner = new ExportSchemaRefiner();
        var schema = Export(typeof(CollectionChildRoot));
        refiner.Refine(schema, new[] { typeof(CollectionChildRoot) });
        XNamespace xs = "http://www.w3.org/2001/XMLSchema";
        schema.Root!.Elements(xs + "complexType").Single(t => (string?)t.Attribute("name") == "RequiredChild").Descendants(xs + "element").Single(e => (string?)e.Attribute("name") == "code").Attribute("minOccurs")!.Value.Should().Be("1");
    }
}
