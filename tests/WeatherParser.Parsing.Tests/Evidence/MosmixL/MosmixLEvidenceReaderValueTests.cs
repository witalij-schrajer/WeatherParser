using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Tests.Evidence.MosmixL;

[TestClass]
public sealed class MosmixLEvidenceReaderValueTests
{
    private IMosmixLEvidenceReader _subject = null!;
    private const string Id = "X123A";

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new MosmixLEvidenceReader();
    }

    private Task<MosmixLEvidence> Read(byte[] kmz) => _subject.ReadAsync(new MemoryStream(kmz), Id, CancellationToken.None);

    private Task<MosmixLEvidence> ReadKml(string kml) => Read(MosmixKmzBuilder.Kmz(kml));

    private Task<MosmixLEvidence> ReadForecasts(params string[] forecasts) => ReadKml(MosmixKmzBuilder.Kml(forecasts: forecasts));

    private static async Task ShouldBeInvalid(Func<Task> act)
    {
        (await act.Should().ThrowAsync<CatalogFormatException>()).Which.ErrorId.Should().Be(CatalogFormatException.MosmixEvidenceInvalidErrorId);
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenForecastTokenCountDoesNotMatchTimeSteps()
    {
        await ShouldBeInvalid(() => ReadForecasts(MosmixKmzBuilder.Forecast("TTT", "1.00 2.00"), MosmixKmzBuilder.Forecast("FF", "1.00 2.00 3.00 4.00")));
    }

    [TestMethod]
    public async Task Read_AcceptsTokensSeparatedByAnyWhitespace()
    {
        var evidence = await ReadForecasts(MosmixKmzBuilder.Forecast("TTT", "\n  1.00\t2.00   3.00\n"));
        evidence.AvailableElements.Should().Equal("TTT");
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenForecastValueHasInvalidTokenFormat()
    {
        var invalidTokens = new[] { "1.2.3", "+123", "1e3", "1,5", "12.", ".5", "--", "NaN" };
        foreach (var token in invalidTokens)
        {
            await ShouldBeInvalid(() => ReadForecasts(MosmixKmzBuilder.Forecast("TTT", "1.00 2.00 " + token)));
        }
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenIntegerPartExceedsNineDigits()
    {
        await ShouldBeInvalid(() => ReadForecasts(MosmixKmzBuilder.Forecast("TTT", "1234567890 - -")));
        var evidence = await ReadForecasts(MosmixKmzBuilder.Forecast("TTT", "123456789 - -"));
        evidence.AvailableElements.Should().Equal("TTT");
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenFractionalPartExceedsSixDigits()
    {
        await ShouldBeInvalid(() => ReadForecasts(MosmixKmzBuilder.Forecast("TTT", "1.1234567 - -")));
        var evidence = await ReadForecasts(MosmixKmzBuilder.Forecast("TTT", "1.123456 - -"));
        evidence.AvailableElements.Should().Equal("TTT");
    }

    [TestMethod]
    public async Task Read_AcceptsNegativeValues()
    {
        var evidence = await ReadForecasts(MosmixKmzBuilder.Forecast("TTT", "-5.25 - -0.50"));
        evidence.AvailableElements.Should().Equal("TTT");
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenElementNameIsNotUnique()
    {
        await ShouldBeInvalid(() => ReadForecasts(MosmixKmzBuilder.Forecast("TTT", "1.00 2.00 3.00"), MosmixKmzBuilder.Forecast("TTT", "4.00 5.00 6.00")));
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenElementNameAttributeIsInvalid()
    {
        await ShouldBeInvalid(() => ReadForecasts("<dwd:Forecast><dwd:value>1.00 2.00 3.00</dwd:value></dwd:Forecast>"));
        await ShouldBeInvalid(() => ReadForecasts("<dwd:Forecast dwd:elementName=\"\"><dwd:value>1.00 2.00 3.00</dwd:value></dwd:Forecast>"));
        await ShouldBeInvalid(() => ReadForecasts("<dwd:Forecast elementName=\"TTT\"><dwd:value>1.00 2.00 3.00</dwd:value></dwd:Forecast>"));
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenValueChildIsMissingOrDuplicated()
    {
        await ShouldBeInvalid(() => ReadForecasts("<dwd:Forecast dwd:elementName=\"TTT\"></dwd:Forecast>"));
        await ShouldBeInvalid(() => ReadForecasts("<dwd:Forecast dwd:elementName=\"TTT\"><dwd:value>1.00 2.00 3.00</dwd:value><dwd:value>1.00 2.00 3.00</dwd:value></dwd:Forecast>"));
    }

    [TestMethod]
    public async Task Read_AllTokensMissing_ReturnsNoAvailableElementsWithIssueTime()
    {
        var evidence = await ReadForecasts(MosmixKmzBuilder.Forecast("TTT", "- - -"), MosmixKmzBuilder.Forecast("FF", "- - -"));
        evidence.AvailableElements.Should().BeEmpty();
        evidence.IssuedAtUtc.Should().Be(new DateTime(2026, 10, 21, 3, 0, 0, DateTimeKind.Utc));
    }

    [TestMethod]
    public async Task Read_ReturnsElement_WhenOneTokenIsZero()
    {
        var evidence = await ReadForecasts(MosmixKmzBuilder.Forecast("RR1c", "- 0 -"), MosmixKmzBuilder.Forecast("TTT", "- - -"));
        evidence.AvailableElements.Should().Equal("RR1c");
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenForecastTimeStepsIsEmpty()
    {
        await ShouldBeInvalid(() => ReadKml(MosmixKmzBuilder.Kml(timeSteps: 0, forecasts: new[] { MosmixKmzBuilder.Forecast("TTT", "") })));
    }

    [TestMethod]
    public async Task Read_AcceptsArchiveAtExactly1MiBCompressed()
    {
        var probe = MosmixKmzBuilder.Kmz(MosmixKmzBuilder.PadTo(MosmixKmzBuilder.Kml(), 1048576 - 4096), CompressionLevel.NoCompression);
        var overhead = probe.Length - (1048576 - 4096);
        var exact = MosmixKmzBuilder.Kmz(MosmixKmzBuilder.PadTo(MosmixKmzBuilder.Kml(), 1048576 - overhead), CompressionLevel.NoCompression);
        exact.Length.Should().Be(1048576);
        var evidence = await Read(exact);
        evidence.AvailableElements.Should().Equal("FF", "TTT");
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenCompressedSizeExceeds1MiB()
    {
        var probe = MosmixKmzBuilder.Kmz(MosmixKmzBuilder.PadTo(MosmixKmzBuilder.Kml(), 1048576 - 4096), CompressionLevel.NoCompression);
        var overhead = probe.Length - (1048576 - 4096);
        var over = MosmixKmzBuilder.Kmz(MosmixKmzBuilder.PadTo(MosmixKmzBuilder.Kml(), 1048576 - overhead + 1), CompressionLevel.NoCompression);
        over.Length.Should().Be(1048577);
        await ShouldBeInvalid(() => Read(over));
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenUncompressedSizeExceeds16MiB()
    {
        var kmz = MosmixKmzBuilder.Kmz(MosmixKmzBuilder.PadTo(MosmixKmzBuilder.Kml(), 16777217));
        kmz.Length.Should().BeLessThan(1048576);
        await ShouldBeInvalid(() => Read(kmz));
    }

    [TestMethod]
    public async Task Read_AcceptsArchiveAtExactly16MiBUncompressed()
    {
        var kmz = MosmixKmzBuilder.Kmz(MosmixKmzBuilder.PadTo(MosmixKmzBuilder.Kml(), 16777216));
        kmz.Length.Should().BeLessThan(1048576);
        var evidence = await Read(kmz);
        evidence.AvailableElements.Should().Equal("FF", "TTT");
    }
}
