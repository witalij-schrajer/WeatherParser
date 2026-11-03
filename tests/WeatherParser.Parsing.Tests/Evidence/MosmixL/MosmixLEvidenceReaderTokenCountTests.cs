using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using WeatherParser.Parsing.Evidence.MosmixL;
using WeatherParser.Parsing.Exceptions;

namespace WeatherParser.Parsing.Tests.Evidence.MosmixL;

[TestClass]
public sealed class MosmixLEvidenceReaderTokenCountTests
{
    private IMosmixLEvidenceReader _subject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _subject = new MosmixLEvidenceReader();
    }

    private async Task ShouldBeInvalid(string forecast)
    {
        var kmz = MosmixKmzBuilder.Kmz(MosmixKmzBuilder.Kml(forecasts: new[] { forecast }));
        Func<Task> act = async () => await _subject.ReadAsync(new MemoryStream(kmz), "X123A", CancellationToken.None);
        await act.Should().ThrowAsync<CatalogFormatException>()
            .WithMessage("*")
            .Where(ex => ex.ErrorId == CatalogFormatException.MosmixEvidenceInvalidErrorId);
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenSeriesHasOneTokenTooFew()
    {
        await ShouldBeInvalid(MosmixKmzBuilder.Forecast("TTT", "1.00 2.00"));
    }

    [TestMethod]
    public async Task Read_ThrowsCatalogFormatException_WhenSeriesHasOneTokenTooMany()
    {
        await ShouldBeInvalid(MosmixKmzBuilder.Forecast("TTT", "1.00 2.00 3.00 4.00"));
    }

    [TestMethod]
    public async Task Read_AcceptsSeriesWithExactlyTheTimeStepCount()
    {
        var kmz = MosmixKmzBuilder.Kmz(MosmixKmzBuilder.Kml(forecasts: new[] { MosmixKmzBuilder.Forecast("TTT", "1.00 2.00 3.00") }));
        (await _subject.ReadAsync(new MemoryStream(kmz), "X123A", CancellationToken.None))
            .Should().NotBeNull();
    }
}
