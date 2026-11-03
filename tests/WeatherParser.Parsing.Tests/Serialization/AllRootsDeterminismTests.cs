using FluentAssertions;
using WeatherParser.Parsing.Serialization;
using WeatherParser.Parsing.Tests.Export;

namespace WeatherParser.Parsing.Tests.Serialization;

[TestClass]
public sealed class AllRootsDeterminismTests
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
    [DynamicData(nameof(AllRootsTestData.Roots), typeof(AllRootsTestData))]
    public async Task SerializeTwice_IdenticalInput_ProducesIdenticalBytes(string rootName)
    {
        var first = AllRootsTestData.Build(rootName);
        var second = AllRootsTestData.Build(rootName);

        var json1 = await AllRootsTestData.JsonBytesAsync(first, _json);
        var json2 = await AllRootsTestData.JsonBytesAsync(second, _json);
        var xml1 = await AllRootsTestData.XmlBytesAsync(first, _xml);
        var xml2 = await AllRootsTestData.XmlBytesAsync(second, _xml);

        json1.Should().Equal(json2);
        xml1.Should().Equal(xml2);
        
        (await AllRootsTestData.JsonBytesAsync(first, _json)).Should().Equal(json1);
        (await AllRootsTestData.XmlBytesAsync(first, _xml)).Should().Equal(xml1);
    }

    [TestMethod]
    [DynamicData(nameof(AllRootsTestData.Roots), typeof(AllRootsTestData))]
    public async Task SameInstance_SerializesToBothFormatsWithoutMutation(string rootName)
    {
        var instance = AllRootsTestData.Build(rootName);

        var jsonBefore = await AllRootsTestData.JsonBytesAsync(instance, _json);
        var xml = await AllRootsTestData.XmlBytesAsync(instance, _xml);

        var jsonAfter = await AllRootsTestData.JsonBytesAsync(instance, _json);

        jsonAfter.Should().Equal(jsonBefore);

        var xmlFresh = await AllRootsTestData.XmlBytesAsync(AllRootsTestData.Build(rootName), _xml);
        xmlFresh.Should().Equal(xml);

        // Test that serializing fresh instance in XML first then JSON gives same result as original
        var freshInstance = AllRootsTestData.Build(rootName);
        var xmlFirst = await AllRootsTestData.XmlBytesAsync(freshInstance, _xml);
        var jsonFromXmlFirst = await AllRootsTestData.JsonBytesAsync(freshInstance, _json);
        jsonFromXmlFirst.Should().Equal(jsonBefore);
    }

    [TestMethod]
    public void Serializers_HaveNoMappingDependency()
    {
        typeof(JsonExportSerializer).GetConstructors().Should().ContainSingle().Which.GetParameters().Should().BeEmpty();
        typeof(XmlExportSerializer).GetConstructors().Should().ContainSingle().Which.GetParameters().Should().BeEmpty();
    }
}
