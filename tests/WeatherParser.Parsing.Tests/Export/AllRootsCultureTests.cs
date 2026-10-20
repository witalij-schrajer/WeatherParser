using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using WeatherParser.Parsing.Export;
using WeatherParser.Parsing.Serialization;

namespace WeatherParser.Parsing.Tests.Export;

[TestClass]
public sealed class AllRootsCultureTests
{
    private IJsonExportSerializer _json = null!;
    private IXmlExportSerializer _xml = null!;

    private CultureInfo _originalCulture = null!;
    private CultureInfo _originalUICulture = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _json = new JsonExportSerializer();
        _xml = new XmlExportSerializer();

        _originalCulture = CultureInfo.CurrentCulture;
        _originalUICulture = CultureInfo.CurrentUICulture;
    }

    [TestCleanup]
    public void TestCleanup()
    {
        CultureInfo.CurrentCulture = _originalCulture;
        CultureInfo.CurrentUICulture = _originalUICulture;
    }

    private static readonly Regex WireTime = new(@"\A\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\z", RegexOptions.CultureInvariant);

    private async Task<(byte[] Json, byte[] Xml)> SerializeUnder(string cultureName, string rootName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var root = AllRootsTestData.Build(rootName);
        return (await AllRootsTestData.JsonBytesAsync(root, _json), await AllRootsTestData.XmlBytesAsync(root, _xml));
    }

    private static void CollectTimes(JsonElement element, List<string> times, List<string> numbers)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if ((prop.Name.EndsWith("_at", StringComparison.Ordinal) || prop.Name is "period_start" or "period_end") && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        times.Add(prop.Value.GetString()!);
                    }
                    else
                    {
                        CollectTimes(prop.Value, times, numbers);
                    }
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectTimes(item, times, numbers);
                }
                break;
            case JsonValueKind.Number:
                numbers.Add(element.GetRawText());
                break;
        }
    }

    [TestMethod]
    [DynamicData(nameof(AllRootsTestData.Roots), typeof(AllRootsTestData))]
    public async Task AllRoots_UnderEnUsAndDeDe_WriteIdenticalBytes(string rootName)
    {
        var (enJson, enXml) = await SerializeUnder("en-US", rootName);
        var (deJson, deXml) = await SerializeUnder("de-DE", rootName);

        enJson.Should().Equal(deJson);
        enXml.Should().Equal(deXml);
    }

    [TestMethod]
    [DynamicData(nameof(AllRootsTestData.Roots), typeof(AllRootsTestData))]
    public async Task AllRoots_UnderDeDe_WriteInvariantNumbersAndSuffixFreeUtc(string rootName)
    {
        var (_, xmlBytes) = await SerializeUnder("de-DE", rootName);
        var jsonBytes = await AllRootsTestData.JsonBytesAsync(AllRootsTestData.Build(rootName), _json);

        using var json = JsonDocument.Parse(jsonBytes);
        var times = new List<string>();
        var numbers = new List<string>();
        CollectTimes(json.RootElement, times, numbers);

        times.Should().NotBeEmpty();
        foreach (var time in times)
        {
            WireTime.IsMatch(time).Should().BeTrue(time);
        }

        numbers.Should().NotBeEmpty();
        foreach (var number in numbers)
        {
            number.Should().NotContain(",");
        }

        var xml = XDocument.Load(new MemoryStream(xmlBytes));
        var timeElements = xml.Descendants()
            .Where(e => e.Name.LocalName.EndsWith("_at", StringComparison.Ordinal) || e.Name.LocalName is "period_start" or "period_end")
            .Select(e => e.Value);

        timeElements.Should().NotBeEmpty();
        foreach (var time in timeElements)
        {
            WireTime.IsMatch(time).Should().BeTrue(time);
        }
    }
}
